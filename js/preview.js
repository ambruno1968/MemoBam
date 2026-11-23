// Zero-dependency client-side document previewer for MemoBam PWA
// Supports: PDF, Word (.docx), Excel (.xlsx/.xlsm/.csv), TXT/JSON/MD/XML/LOG, HTML, and Images

const MIME_MAP = {
  pdf: 'application/pdf',
  txt: 'text/plain;charset=utf-8',
  log: 'text/plain;charset=utf-8',
  md: 'text/plain;charset=utf-8',
  csv: 'text/csv;charset=utf-8',
  json: 'application/json;charset=utf-8',
  xml: 'application/xml;charset=utf-8',
  html: 'text/html;charset=utf-8',
  htm: 'text/html;charset=utf-8',
  docx: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
  doc: 'application/msword',
  xlsx: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
  xlsm: 'application/vnd.ms-excel.sheet.macroEnabled.12',
  xls: 'application/vnd.ms-excel',
  png: 'image/png',
  jpg: 'image/jpeg',
  jpeg: 'image/jpeg',
  gif: 'image/gif',
  webp: 'image/webp',
  svg: 'image/svg+xml'
};

export function getExtension(fileName = '') {
  const idx = fileName.lastIndexOf('.');
  return idx >= 0 ? fileName.slice(idx + 1).toLowerCase() : '';
}

export function getTypedBlob(blob, fileName) {
  const ext = getExtension(fileName);
  const mime = MIME_MAP[ext] || blob.type || 'application/octet-stream';
  return blob.type === mime ? blob : new Blob([blob], { type: mime });
}

function escapeHtml(str) {
  return String(str || '')
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;');
}

// ===== LIGHTWEIGHT ZIP READER (uses native DecompressionStream) =====

async function unzipEntries(arrayBuffer) {
  const bytes = new Uint8Array(arrayBuffer);
  const view = new DataView(arrayBuffer);
  const decoder = new TextDecoder('utf-8');

  // Find End of Central Directory (0x06054b50)
  let eocdOffset = -1;
  const minOffset = Math.max(0, bytes.length - 65557);
  for (let i = bytes.length - 22; i >= minOffset; i--) {
    if (view.getUint32(i, true) === 0x06054b50) {
      eocdOffset = i;
      break;
    }
  }
  if (eocdOffset < 0) throw new Error('Archivio ZIP/Office non valido');

  const cdOffset = view.getUint32(eocdOffset + 16, true);
  const totalEntries = view.getUint16(eocdOffset + 10, true);
  const entries = new Map();

  let ptr = cdOffset;
  for (let i = 0; i < totalEntries; i++) {
    if (view.getUint32(ptr, true) !== 0x02014b50) break;
    const method = view.getUint16(ptr + 10, true);
    const compSize = view.getUint32(ptr + 20, true);
    const nameLen = view.getUint16(ptr + 28, true);
    const extraLen = view.getUint16(ptr + 30, true);
    const commentLen = view.getUint16(ptr + 32, true);
    const localHeaderOffset = view.getUint32(ptr + 42, true);
    const name = decoder.decode(bytes.subarray(ptr + 46, ptr + 46 + nameLen));
    ptr += 46 + nameLen + extraLen + commentLen;

    entries.set(name, { method, compSize, localHeaderOffset });
  }

  async function readText(name) {
    const entry = entries.get(name);
    if (!entry) return null;
    const lh = entry.localHeaderOffset;
    if (view.getUint32(lh, true) !== 0x04034b50) return null;
    const lNameLen = view.getUint16(lh + 26, true);
    const lExtraLen = view.getUint16(lh + 28, true);
    const dataStart = lh + 30 + lNameLen + lExtraLen;
    const slice = bytes.subarray(dataStart, dataStart + entry.compSize);

    if (entry.method === 0) {
      return decoder.decode(slice);
    }
    if (entry.method === 8) {
      if (typeof DecompressionStream === 'undefined') {
        throw new Error('DecompressionStream non supportato su questo browser');
      }
      const ds = new DecompressionStream('deflate-raw');
      const stream = new Blob([slice]).stream().pipeThrough(ds);
      const outBuf = await new Response(stream).arrayBuffer();
      return decoder.decode(outBuf);
    }
    throw new Error(`Metodo di compressione non supportato (${entry.method})`);
  }

  return { entries, readText };
}

// ===== DOCX PREVIEW =====

function renderDocxRun(rNode) {
  let text = '';
  for (const child of rNode.children) {
    if (child.localName === 't') text += child.textContent || '';
    else if (child.localName === 'tab') text += '\t';
    else if (child.localName === 'br') text += '\n';
  }
  if (!text) return '';
  let html = escapeHtml(text).replaceAll('\n', '<br>');
  const rPr = Array.from(rNode.children).find((c) => c.localName === 'rPr');
  if (rPr) {
    const isBold = Array.from(rPr.children).some((c) => c.localName === 'b');
    const isItalic = Array.from(rPr.children).some((c) => c.localName === 'i');
    if (isBold) html = `<strong>${html}</strong>`;
    if (isItalic) html = `<em>${html}</em>`;
  }
  return html;
}

function renderDocxParagraph(pNode) {
  let inner = '';
  for (const child of pNode.children) {
    if (child.localName === 'r') {
      inner += renderDocxRun(child);
    } else if (child.localName === 'hyperlink') {
      for (const hChild of child.children) {
        if (hChild.localName === 'r') inner += renderDocxRun(hChild);
      }
    }
  }
  if (!inner.trim()) return '<p class="docx-p">&nbsp;</p>';
  const pPr = Array.from(pNode.children).find((c) => c.localName === 'pPr');
  const styleVal = pPr
    ? Array.from(pPr.children).find((c) => c.localName === 'pStyle')?.getAttribute('w:val') || ''
    : '';
  if (/^Heading1|Titolo1/i.test(styleVal)) return `<h2 class="docx-h2">${inner}</h2>`;
  if (/^Heading2|Titolo2/i.test(styleVal)) return `<h3 class="docx-h3">${inner}</h3>`;
  return `<p class="docx-p">${inner}</p>`;
}

function renderDocxTable(tblNode) {
  const rows = [];
  for (const tr of tblNode.children) {
    if (tr.localName !== 'tr') continue;
    const cells = [];
    for (const tc of tr.children) {
      if (tc.localName !== 'tc') continue;
      const cellParts = [];
      for (const p of tc.children) {
        if (p.localName === 'p') cellParts.push(renderDocxParagraph(p));
      }
      cells.push(`<td>${cellParts.join('')}</td>`);
    }
    rows.push(`<tr>${cells.join('')}</tr>`);
  }
  return `<div class="preview-table-wrap"><table class="preview-table docx-table">${rows.join('')}</table></div>`;
}

async function buildDocxPreviewHtml(blob) {
  const zip = await unzipEntries(await blob.arrayBuffer());
  const xmlText = await zip.readText('word/document.xml');
  if (!xmlText) throw new Error('Contenuto word/document.xml non trovato');

  const doc = new DOMParser().parseFromString(xmlText, 'application/xml');
  const body = doc.getElementsByTagNameNS('*', 'body')[0];
  if (!body) throw new Error('Corpo del documento Word vuoto');

  const parts = [];
  for (const node of body.children) {
    if (node.localName === 'p') parts.push(renderDocxParagraph(node));
    else if (node.localName === 'tbl') parts.push(renderDocxTable(node));
  }
  return `<div class="preview-docx">${parts.join('') || '<p>Documento vuoto.</p>'}</div>`;
}

// ===== XLSX / CSV PREVIEW =====

function colLettersToIndex(letters) {
  let idx = 0;
  for (let i = 0; i < letters.length; i++) {
    idx = idx * 26 + (letters.charCodeAt(i) - 64);
  }
  return idx - 1;
}

function indexToColLetters(idx) {
  let s = '';
  let n = idx + 1;
  while (n > 0) {
    const rem = (n - 1) % 26;
    s = String.fromCharCode(65 + rem) + s;
    n = Math.floor((n - 1) / 26);
  }
  return s;
}

async function buildXlsxPreview(blob) {
  const zip = await unzipEntries(await blob.arrayBuffer());

  // Shared strings
  const sharedStrings = [];
  const ssXml = await zip.readText('xl/sharedStrings.xml');
  if (ssXml) {
    const ssDoc = new DOMParser().parseFromString(ssXml, 'application/xml');
    for (const si of ssDoc.getElementsByTagNameNS('*', 'si')) {
      const tNodes = si.getElementsByTagNameNS('*', 't');
      let val = '';
      for (const t of tNodes) val += t.textContent || '';
      sharedStrings.push(val);
    }
  }

  // Sheet names + paths
  const sheetInfos = [];
  const wbXml = await zip.readText('xl/workbook.xml');
  const relsXml = await zip.readText('xl/_rels/workbook.xml.rels');
  const relMap = new Map();
  if (relsXml) {
    const relDoc = new DOMParser().parseFromString(relsXml, 'application/xml');
    for (const rel of relDoc.getElementsByTagNameNS('*', 'Relationship')) {
      relMap.set(rel.getAttribute('Id'), rel.getAttribute('Target'));
    }
  }
  if (wbXml) {
    const wbDoc = new DOMParser().parseFromString(wbXml, 'application/xml');
    for (const sh of wbDoc.getElementsByTagNameNS('*', 'sheet')) {
      const name = sh.getAttribute('name') || 'Foglio';
      const rId = sh.getAttribute('r:id') || sh.getAttributeNS('http://schemas.openxmlformats.org/officeDocument/2006/relationships', 'id');
      let target = relMap.get(rId) || '';
      target = target.replace(/^\/+/, '');
      if (target && !target.startsWith('xl/')) target = `xl/${target}`;
      if (target) sheetInfos.push({ name, path: target });
    }
  }

  if (sheetInfos.length === 0) {
    for (const path of zip.entries.keys()) {
      if (/^xl\/worksheets\/sheet\d+\.xml$/i.test(path)) {
        sheetInfos.push({ name: path.split('/').pop().replace('.xml', ''), path });
      }
    }
  }

  const sheetsHtml = [];
  for (const info of sheetInfos) {
    const sheetXml = await zip.readText(info.path);
    if (!sheetXml) continue;
    const sDoc = new DOMParser().parseFromString(sheetXml, 'application/xml');
    const rowNodes = Array.from(sDoc.getElementsByTagNameNS('*', 'row'));

    let maxCol = 0;
    const parsedRows = [];
    for (const rNode of rowNodes.slice(0, 500)) {
      const rowMap = new Map();
      for (const cNode of rNode.children) {
        if (cNode.localName !== 'c') continue;
        const ref = cNode.getAttribute('r') || '';
        const colMatch = ref.match(/^([A-Z]+)/i);
        const colIdx = colMatch ? colLettersToIndex(colMatch[1].toUpperCase()) : rowMap.size;
        if (colIdx > maxCol) maxCol = colIdx;

        const type = cNode.getAttribute('t');
        let val = '';
        if (type === 'inlineStr') {
          const tNode = cNode.getElementsByTagNameNS('*', 't')[0];
          val = tNode?.textContent || '';
        } else {
          const vNode = Array.from(cNode.children).find((x) => x.localName === 'v');
          const raw = vNode?.textContent || '';
          if (type === 's') {
            val = sharedStrings[Number(raw)] ?? raw;
          } else if (type === 'b') {
            val = raw === '1' ? 'VERO' : 'FALSO';
          } else {
            val = raw;
          }
        }
        rowMap.set(colIdx, val);
      }
      parsedRows.push(rowMap);
    }

    const colLimit = Math.min(maxCol + 1, 60);
    let headHtml = '<tr><th class="excel-row-hdr">#</th>';
    for (let c = 0; c < colLimit; c++) {
      headHtml += `<th>${indexToColLetters(c)}</th>`;
    }
    headHtml += '</tr>';

    const bodyRowsHtml = parsedRows
      .map((rowMap, rIdx) => {
        let tds = `<td class="excel-row-hdr">${rIdx + 1}</td>`;
        for (let c = 0; c < colLimit; c++) {
          tds += `<td>${escapeHtml(rowMap.get(c) ?? '')}</td>`;
        }
        return `<tr>${tds}</tr>`;
      })
      .join('');

    sheetsHtml.push({
      name: info.name,
      html: `<div class="preview-table-wrap"><table class="preview-table excel-table"><thead>${headHtml}</thead><tbody>${bodyRowsHtml || '<tr><td>Foglio vuoto</td></tr>'}</tbody></table></div>`
    });
  }

  return sheetsHtml;
}

function buildCsvPreviewHtml(text) {
  const lines = text.replace(/\r\n/g, '\n').split('\n').filter((l) => l.length > 0).slice(0, 500);
  if (lines.length === 0) return '<div class="empty-state">File CSV vuoto</div>';
  const sep = (lines[0].split(';').length > lines[0].split(',').length) ? ';' : ',';
  const rowsHtml = lines
    .map((line, idx) => {
      const cells = line.split(sep).map((c) => c.replace(/^"|"$/g, ''));
      const tag = idx === 0 ? 'th' : 'td';
      return `<tr>${cells.map((c) => `<${tag}>${escapeHtml(c)}</${tag}>`).join('')}</tr>`;
    })
    .join('');
  return `<div class="preview-table-wrap"><table class="preview-table excel-table">${rowsHtml}</table></div>`;
}

// ===== MAIN PREVIEW RENDERER =====

export async function renderFilePreview(blob, fileName, containerEl) {
  const ext = getExtension(fileName);
  const typedBlob = getTypedBlob(blob, fileName);
  const objectUrl = URL.createObjectURL(typedBlob);

  try {
    if (ext === 'pdf') {
      containerEl.innerHTML = `<iframe class="preview-iframe" src="${objectUrl}#toolbar=1" title="${escapeHtml(fileName)}"></iframe>`;
      return { objectUrl };
    }

    if (['png', 'jpg', 'jpeg', 'gif', 'webp', 'svg', 'bmp'].includes(ext)) {
      containerEl.innerHTML = `<div class="preview-image-wrap"><img class="preview-image" src="${objectUrl}" alt="${escapeHtml(fileName)}"></div>`;
      return { objectUrl };
    }

    if (ext === 'html' || ext === 'htm') {
      const htmlText = await typedBlob.text();
      const iframe = document.createElement('iframe');
      iframe.className = 'preview-iframe';
      iframe.setAttribute('sandbox', 'allow-same-origin');
      iframe.setAttribute('title', fileName);
      iframe.srcdoc = htmlText;
      containerEl.innerHTML = '';
      containerEl.appendChild(iframe);
      return { objectUrl };
    }

    if (ext === 'csv') {
      const csvText = await typedBlob.text();
      containerEl.innerHTML = buildCsvPreviewHtml(csvText);
      return { objectUrl };
    }

    if (['txt', 'log', 'md', 'json', 'xml', 'ini', 'yaml', 'yml', 'css', 'js'].includes(ext)) {
      let text = await typedBlob.text();
      if (ext === 'json') {
        try {
          text = JSON.stringify(JSON.parse(text), null, 2);
        } catch {
          // Keep raw text
        }
      }
      containerEl.innerHTML = `<pre class="preview-text">${escapeHtml(text)}</pre>`;
      return { objectUrl };
    }

    if (ext === 'docx') {
      containerEl.innerHTML = await buildDocxPreviewHtml(typedBlob);
      return { objectUrl };
    }

    if (ext === 'xlsx' || ext === 'xlsm') {
      const sheets = await buildXlsxPreview(typedBlob);
      if (!sheets.length) {
        containerEl.innerHTML = `<div class="empty-state">Nessun foglio trovato nel file Excel.</div>`;
        return { objectUrl };
      }
      if (sheets.length === 1) {
        containerEl.innerHTML = sheets[0].html;
        return { objectUrl };
      }
      const tabsHtml = sheets
        .map((s, i) => `<button type="button" class="btn btn-xs ${i === 0 ? 'btn-primary' : 'btn-neutral'} js-sheet-tab" data-idx="${i}">${escapeHtml(s.name)}</button>`)
        .join('');
      containerEl.innerHTML = `
        <div class="excel-sheet-tabs">${tabsHtml}</div>
        <div class="excel-sheet-body">${sheets[0].html}</div>
      `;
      containerEl.querySelectorAll('.js-sheet-tab').forEach((btn) => {
        btn.addEventListener('click', () => {
          const idx = Number(btn.dataset.idx);
          containerEl.querySelectorAll('.js-sheet-tab').forEach((b) => {
            b.classList.toggle('btn-primary', Number(b.dataset.idx) === idx);
            b.classList.toggle('btn-neutral', Number(b.dataset.idx) !== idx);
          });
          containerEl.querySelector('.excel-sheet-body').innerHTML = sheets[idx].html;
        });
      });
      return { objectUrl };
    }

    containerEl.innerHTML = `
      <div class="preview-unsupported">
        <div class="preview-unsupported-ico">📄</div>
        <p><strong>Anteprima integrata non disponibile per i file <code>.${escapeHtml(ext || 'bin')}</code>.</strong></p>
        <p>Usa <strong>🚀 Apri con app di sistema</strong> oppure <strong>💾 Salva file</strong> qui sopra.</p>
      </div>`;
    return { objectUrl };
  } catch (err) {
    containerEl.innerHTML = `
      <div class="preview-unsupported">
        <div class="preview-unsupported-ico">⚠️</div>
        <p><strong>Impossibile generare l'anteprima:</strong> ${escapeHtml(err?.message || 'formato non leggibile')}</p>
        <p>Usa <strong>🚀 Apri con app di sistema</strong> oppure <strong>💾 Salva file</strong>.</p>
      </div>`;
    return { objectUrl };
  }
}
