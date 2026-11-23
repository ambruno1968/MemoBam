import { MemoBamStorage } from './storage.js';
import { renderFilePreview, getTypedBlob, getExtension } from './preview.js';

const LIGHT_PASTEL = [
  { bg: '#EEF2FF', border: '#C7D2FE' },
  { bg: '#ECFDF5', border: '#A7F3D0' },
  { bg: '#F5F3FF', border: '#DDD6FE' },
  { bg: '#FFF1F2', border: '#FECDD3' },
  { bg: '#F0F9FF', border: '#BAE6FD' }
];

const DARK_PASTEL = [
  { bg: '#1E1B4B', border: '#3730A3' },
  { bg: '#064E3B', border: '#047857' },
  { bg: '#2E1065', border: '#6D28D9' },
  { bg: '#4C0519', border: '#BE123C' },
  { bg: '#0C4A6E', border: '#0369A1' }
];

const storage = new MemoBamStorage();

function getTodayDateStr() {
  const now = new Date();
  const pad = (n) => String(n).padStart(2, '0');
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
}

const state = {
  archive: [],
  diary: [],
  notes: [],
  quickNotes: [],
  activeSection: 'archive',
  globalSearch: '',
  globalDateFilter: '',
  excludeDiaryAttachments: localStorage.getItem('memobam_exclude_diary_att') !== 'false',
  diaryDateFilter: getTodayDateStr(),
  pendingExpanded: false,
  draftAttachments: [], // File[]
  noteDraftAttachments: [], // File[]
  editingArchiveId: null,
  editingQuickNoteId: null,
  detailDiaryId: null,
  detailNoteId: null,
  pendingAttachEntryId: null,
  pendingAttachNoteId: null,
  lastArchiveCount: 0,
  lastDiaryCount: 0,
  lastNotesCount: 0,
  deferredInstallPrompt: null,
  dockMode: localStorage.getItem('memobam_dock_mode') || 'normal',
  autoHide: localStorage.getItem('memobam_auto_hide') === 'true',
  isCollapsedEdge: false
};

const els = {
  appShell: document.getElementById('appShell'),
  collapsedEdgeStrip: document.getElementById('collapsedEdgeStrip'),
  collapsedEdgeLabel: document.getElementById('collapsedEdgeLabel'),
  topbarMenuBtn: document.getElementById('topbarMenuBtn'),
  topbarMenuDropdown: document.getElementById('topbarMenuDropdown'),
  autoHideToggleBtn: document.getElementById('autoHideToggleBtn'),
  storageBadge: document.getElementById('storageBadge'),
  totalCountBadge: document.getElementById('totalCountBadge'),
  connectFolderBtn: document.getElementById('connectFolderBtn'),
  connectFolderLabel: document.getElementById('connectFolderLabel'),
  installBtn: document.getElementById('installBtn'),
  themeToggleBtn: document.getElementById('themeToggleBtn'),
  settingsOpenBtn: document.getElementById('settingsOpenBtn'),
  creditsBtn: document.getElementById('creditsBtn'),

  creditsModal: document.getElementById('creditsModal'),
  closeCreditsModalBtn: document.getElementById('closeCreditsModalBtn'),
  closeCreditsFooterBtn: document.getElementById('closeCreditsFooterBtn'),
  copyPwaUrlBtn: document.getElementById('copyPwaUrlBtn'),

  globalSearchInput: document.getElementById('globalSearchInput'),
  globalDateInput: document.getElementById('globalDateInput'),
  clearGlobalSearchBtn: document.getElementById('clearGlobalSearchBtn'),
  globalSearchCounts: document.getElementById('globalSearchCounts'),
  globalArchiveCountBtn: document.getElementById('globalArchiveCountBtn'),
  globalDiaryCountBtn: document.getElementById('globalDiaryCountBtn'),
  globalNotesCountBtn: document.getElementById('globalNotesCountBtn'),

  pendingBanner: document.getElementById('pendingBanner'),
  pendingSummaryText: document.getElementById('pendingSummaryText'),
  pendingToggleBtn: document.getElementById('pendingToggleBtn'),
  pendingToggleLabel: document.getElementById('pendingToggleLabel'),
  pendingList: document.getElementById('pendingList'),
  navPendingBadge: document.getElementById('navPendingBadge'),

  dropPane: document.getElementById('dropPane'),
  customTitleInput: document.getElementById('customTitleInput'),
  customNotesInput: document.getElementById('customNotesInput'),
  dropZone: document.getElementById('dropZone'),
  dropZoneTitle: document.getElementById('dropZoneTitle'),
  browseFilesBtn: document.getElementById('browseFilesBtn'),
  fileInput: document.getElementById('fileInput'),
  fixedPathText: document.getElementById('fixedPathText'),
  changePathBtn: document.getElementById('changePathBtn'),
  statusBanner: document.getElementById('statusBanner'),

  archiveTabBtn: document.getElementById('archiveTabBtn'),
  diaryTabBtn: document.getElementById('diaryTabBtn'),
  notesTabBtn: document.getElementById('notesTabBtn'),
  diaryTodayCount: document.getElementById('diaryTodayCount'),
  notesCountBadge: document.getElementById('notesCountBadge'),
  archiveTabContent: document.getElementById('archiveTabContent'),
  diaryTabContent: document.getElementById('diaryTabContent'),
  notesTabContent: document.getElementById('notesTabContent'),
  settingsPane: document.getElementById('settingsPane'),

  excludeDiaryAttachmentsChk: document.getElementById('excludeDiaryAttachmentsChk'),
  archiveSummaryText: document.getElementById('archiveSummaryText'),
  archiveList: document.getElementById('archiveList'),

  diaryDateInput: document.getElementById('diaryDateInput'),
  diaryTodayBtn: document.getElementById('diaryTodayBtn'),
  diaryAllDaysBtn: document.getElementById('diaryAllDaysBtn'),
  quickNotesToggleBtn: document.getElementById('quickNotesToggleBtn'),
  quickNotesDropdown: document.getElementById('quickNotesDropdown'),
  quickNotesChips: document.getElementById('quickNotesChips'),
  manageQuickNotesBtn: document.getElementById('manageQuickNotesBtn'),
  fmtBoldBtn: document.getElementById('fmtBoldBtn'),
  fmtItalicBtn: document.getElementById('fmtItalicBtn'),
  fmtBulletBtn: document.getElementById('fmtBulletBtn'),
  fmtCheckBtn: document.getElementById('fmtCheckBtn'),
  draftAttachBtn: document.getElementById('draftAttachBtn'),
  draftAttachInput: document.getElementById('draftAttachInput'),
  diaryEditor: document.getElementById('diaryEditor'),
  saveDiaryBtn: document.getElementById('saveDiaryBtn'),
  draftAttachmentsPanel: document.getElementById('draftAttachmentsPanel'),
  draftAttachmentsList: document.getElementById('draftAttachmentsList'),
  diarySummaryText: document.getElementById('diarySummaryText'),
  diaryList: document.getElementById('diaryList'),

  noteTitleInput: document.getElementById('noteTitleInput'),
  noteFmtBoldBtn: document.getElementById('noteFmtBoldBtn'),
  noteFmtItalicBtn: document.getElementById('noteFmtItalicBtn'),
  noteFmtBulletBtn: document.getElementById('noteFmtBulletBtn'),
  noteFmtCheckBtn: document.getElementById('noteFmtCheckBtn'),
  noteDraftAttachBtn: document.getElementById('noteDraftAttachBtn'),
  noteDraftAttachInput: document.getElementById('noteDraftAttachInput'),
  noteEditor: document.getElementById('noteEditor'),
  saveNoteBtn: document.getElementById('saveNoteBtn'),
  noteDraftAttachmentsPanel: document.getElementById('noteDraftAttachmentsPanel'),
  noteDraftAttachmentsList: document.getElementById('noteDraftAttachmentsList'),
  notesSummaryText: document.getElementById('notesSummaryText'),
  notesList: document.getElementById('notesList'),

  editItemModal: document.getElementById('editItemModal'),
  closeEditItemModalBtn: document.getElementById('closeEditItemModalBtn'),
  editItemTitleInput: document.getElementById('editItemTitleInput'),
  editItemNotesInput: document.getElementById('editItemNotesInput'),
  cancelEditItemBtn: document.getElementById('cancelEditItemBtn'),
  saveEditItemBtn: document.getElementById('saveEditItemBtn'),

  diaryDetailModal: document.getElementById('diaryDetailModal'),
  closeDiaryDetailModalBtn: document.getElementById('closeDiaryDetailModalBtn'),
  detailTimestampText: document.getElementById('detailTimestampText'),
  detailDoneChk: document.getElementById('detailDoneChk'),
  detailBoldBtn: document.getElementById('detailBoldBtn'),
  detailItalicBtn: document.getElementById('detailItalicBtn'),
  detailBulletBtn: document.getElementById('detailBulletBtn'),
  detailCheckBtn: document.getElementById('detailCheckBtn'),
  detailAttachBtn: document.getElementById('detailAttachBtn'),
  detailAttachInput: document.getElementById('detailAttachInput'),
  detailEditor: document.getElementById('detailEditor'),
  detailAttachmentsSection: document.getElementById('detailAttachmentsSection'),
  detailAttachmentsList: document.getElementById('detailAttachmentsList'),
  cancelDiaryDetailBtn: document.getElementById('cancelDiaryDetailBtn'),
  saveDiaryDetailBtn: document.getElementById('saveDiaryDetailBtn'),

  settingsModal: document.getElementById('settingsModal'),
  closeSettingsModalBtn: document.getElementById('closeSettingsModalBtn'),
  settingsContentModal: document.getElementById('settingsContentModal'),
  settingsContentMobile: document.getElementById('settingsContentMobile'),

  quickNotesModal: document.getElementById('quickNotesModal'),
  closeQuickNotesModalBtn: document.getElementById('closeQuickNotesModalBtn'),
  closeQuickNotesFooterBtn: document.getElementById('closeQuickNotesFooterBtn'),
  quickNoteInput: document.getElementById('quickNoteInput'),
  saveQuickNoteBtn: document.getElementById('saveQuickNoteBtn'),
  cancelQuickNoteEditBtn: document.getElementById('cancelQuickNoteEditBtn'),
  quickNotesTableBody: document.getElementById('quickNotesTableBody'),

  globalSearchPopup: document.getElementById('globalSearchPopup'),
  globalSearchPopupSubtitle: document.getElementById('globalSearchPopupSubtitle'),
  closeGlobalPopupTopBtn: document.getElementById('closeGlobalPopupTopBtn'),
  closeGlobalPopupFooterBtn: document.getElementById('closeGlobalPopupFooterBtn'),
  popupArchiveBadge: document.getElementById('popupArchiveBadge'),
  popupDiaryBadge: document.getElementById('popupDiaryBadge'),
  popupNotesBadge: document.getElementById('popupNotesBadge'),
  popupArchiveList: document.getElementById('popupArchiveList'),
  popupDiaryList: document.getElementById('popupDiaryList'),
  popupNotesList: document.getElementById('popupNotesList'),

  noteDetailModal: document.getElementById('noteDetailModal'),
  closeNoteDetailModalBtn: document.getElementById('closeNoteDetailModalBtn'),
  noteDetailTimestampText: document.getElementById('noteDetailTimestampText'),
  noteDetailTitleInput: document.getElementById('noteDetailTitleInput'),
  noteDetailDoneChk: document.getElementById('noteDetailDoneChk'),
  noteDetailBoldBtn: document.getElementById('noteDetailBoldBtn'),
  noteDetailItalicBtn: document.getElementById('noteDetailItalicBtn'),
  noteDetailBulletBtn: document.getElementById('noteDetailBulletBtn'),
  noteDetailCheckBtn: document.getElementById('noteDetailCheckBtn'),
  noteDetailAttachBtn: document.getElementById('noteDetailAttachBtn'),
  noteDetailAttachInput: document.getElementById('noteDetailAttachInput'),
  noteDetailEditor: document.getElementById('noteDetailEditor'),
  noteDetailAttachmentsSection: document.getElementById('noteDetailAttachmentsSection'),
  noteDetailAttachmentsList: document.getElementById('noteDetailAttachmentsList'),
  cancelNoteDetailBtn: document.getElementById('cancelNoteDetailBtn'),
  saveNoteDetailBtn: document.getElementById('saveNoteDetailBtn'),
  noteEntryAttachInput: document.getElementById('noteEntryAttachInput'),

  filePreviewModal: document.getElementById('filePreviewModal'),
  filePreviewTitle: document.getElementById('filePreviewTitle'),
  filePreviewSubtitle: document.getElementById('filePreviewSubtitle'),
  previewOpenSystemBtn: document.getElementById('previewOpenSystemBtn'),
  previewSaveFileBtn: document.getElementById('previewSaveFileBtn'),
  closeFilePreviewTopBtn: document.getElementById('closeFilePreviewTopBtn'),
  filePreviewBody: document.getElementById('filePreviewBody'),

  entryAttachInput: document.getElementById('entryAttachInput'),
  toast: document.getElementById('toast')
};

// ===== HELPERS =====

let toastTimer = null;
function showToast(msg) {
  els.toast.textContent = msg;
  els.toast.classList.remove('hidden');
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => els.toast.classList.add('hidden'), 2800);
}

function showStatus(msg, isError = false) {
  els.statusBanner.textContent = msg;
  els.statusBanner.classList.toggle('error', isError);
  els.statusBanner.classList.remove('hidden');
}

function escapeHtml(str) {
  return String(str || '')
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;');
}

function formatDateIt(iso) {
  if (!iso) return '';
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso.slice(0, 10);
  return d.toLocaleDateString('it-IT', { day: '2-digit', month: '2-digit', year: 'numeric' });
}

function formatDateInputToIt(yyyyMmDd) {
  if (!yyyyMmDd) return '';
  const [y, m, d] = yyyyMmDd.split('-');
  return `${d}/${m}/${y}`;
}

function formatTimeIt(iso) {
  if (!iso) return '';
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso.slice(11, 19);
  return d.toLocaleTimeString('it-IT', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
}

function formatShortTimeIt(iso) {
  return formatTimeIt(iso).slice(0, 5);
}

function formatBytes(bytes) {
  if (!bytes) return '0 B';
  const units = ['B', 'KB', 'MB', 'GB'];
  let val = bytes;
  let i = 0;
  while (val >= 1024 && i < units.length - 1) {
    val /= 1024;
    i++;
  }
  return i === 0 ? `${Math.round(val)} ${units[i]}` : `${val.toFixed(1)} ${units[i]}`;
}

function getFileBadge(fileName, isDir = false) {
  if (isDir) return 'DIR';
  const idx = (fileName || '').lastIndexOf('.');
  if (idx < 0) return 'FILE';
  const ext = fileName.slice(idx + 1).toUpperCase();
  return ext.length > 5 ? ext.slice(0, 5) : (ext || 'FILE');
}

// ===== RICH EDITOR HELPERS =====

function execEditorCmd(editor, cmd) {
  editor.focus();
  document.execCommand(cmd, false, null);
}

function insertSecondaryCheckBox(editor) {
  editor.focus();
  const sel = window.getSelection();
  let blockNode = null;
  let lineText = '';
  if (sel && sel.rangeCount > 0) {
    let node = sel.anchorNode;
    while (node && node !== editor && node.nodeType === Node.TEXT_NODE) {
      node = node.parentNode;
    }
    if (node && node !== editor) {
      blockNode = node;
      lineText = node.textContent || '';
    } else {
      lineText = editor.textContent || '';
    }
  }
  const hasCheckboxInBlock = blockNode
    ? Boolean(blockNode.querySelector?.('input[type="checkbox"]'))
    : Boolean(editor.querySelector('input[type="checkbox"]') && !editor.innerHTML.includes('<br>') && !editor.querySelector('div,p,li'));

  // Prevent repeating secondary checkbox on the same line
  if (hasCheckboxInBlock || /\[\s*[xX ]?\s*\]/.test(lineText)) {
    return;
  }

  const cb = document.createElement('input');
  cb.type = 'checkbox';
  cb.className = 'diary-inline-check';
  const space = document.createTextNode('\u00A0');

  if (sel && sel.rangeCount > 0) {
    const range = sel.getRangeAt(0);
    range.deleteContents();
    range.insertNode(space);
    range.insertNode(cb);
    range.setStartAfter(space);
    range.collapse(true);
    sel.removeAllRanges();
    sel.addRange(range);
  } else {
    editor.appendChild(cb);
    editor.appendChild(space);
  }
}

function serializeEditorHtml(editor) {
  editor.querySelectorAll('input[type="checkbox"]').forEach((cb) => {
    if (cb.checked) cb.setAttribute('checked', '');
    else cb.removeAttribute('checked');
  });
  return editor.innerHTML.trim();
}

function extractEditorPlainText(editor) {
  const clone = editor.cloneNode(true);
  clone.querySelectorAll('input[type="checkbox"]').forEach((cb) => {
    cb.replaceWith(document.createTextNode(cb.checked ? '[x] ' : '[ ] '));
  });
  clone.querySelectorAll('li').forEach((li) => {
    li.prepend(document.createTextNode('• '));
  });
  return (clone.innerText || clone.textContent || '').trim();
}

function convertBracketCheckboxes(html, ownerType, ownerId) {
  const tmp = document.createElement('div');
  tmp.innerHTML = html;

  const walker = document.createTreeWalker(tmp, NodeFilter.SHOW_TEXT);
  const textNodes = [];
  let n;
  while ((n = walker.nextNode())) {
    if (/\[\s*[xX ]?\s*\]/.test(n.nodeValue)) {
      textNodes.push(n);
    }
  }

  for (const tn of textNodes) {
    const frag = document.createDocumentFragment();
    const parts = tn.nodeValue.split(/(\[\s*[xX ]?\s*\])/g);
    for (const part of parts) {
      const m = part.match(/^\[(\s*[xX ]?\s*)\]$/);
      if (m) {
        const cb = document.createElement('input');
        cb.type = 'checkbox';
        cb.className = 'diary-inline-check';
        if (/x/i.test(m[1])) cb.setAttribute('checked', '');
        if (ownerType && ownerId) {
          cb.dataset.ownerType = ownerType;
          cb.dataset.ownerId = ownerId;
        }
        frag.appendChild(cb);
      } else if (part) {
        frag.appendChild(document.createTextNode(part));
      }
    }
    tn.replaceWith(frag);
  }

  tmp.querySelectorAll('input[type="checkbox"]').forEach((cb) => {
    cb.classList.add('diary-inline-check');
    if (ownerType && ownerId) {
      cb.dataset.ownerType = ownerType;
      cb.dataset.ownerId = ownerId;
    }
  });

  return tmp.innerHTML;
}

function renderDiaryBodyHtml(entry, ownerType = 'diary') {
  const rawHtml = entry.RichContentHtml
    ? entry.RichContentHtml
    : escapeHtml(entry.Text || '').replaceAll('\n', '<br>');
  return convertBracketCheckboxes(rawHtml, ownerType, entry.Id);
}

// ===== THEME =====

function applyTheme(theme) {
  document.documentElement.setAttribute('data-theme', theme);
  localStorage.setItem('memobam_theme', theme);
  els.themeToggleBtn.textContent = theme === 'dark' ? '☀️ Chiaro' : '🌙 Scuro';
  renderDiary();
}

function initTheme() {
  const saved = localStorage.getItem('memobam_theme');
  const prefersDark = window.matchMedia?.('(prefers-color-scheme: dark)').matches;
  applyTheme(saved || (prefersDark ? 'dark' : 'light'));
}

// ===== DATA LOADING & STORAGE STATUS =====

async function reloadData() {
  const [archive, diary, notes, quickNotes] = await Promise.all([
    storage.getArchiveItems(),
    storage.getDiaryEntries(),
    storage.getNotes(),
    storage.getQuickNotes()
  ]);
  state.archive = archive;
  state.diary = diary;
  state.notes = notes;
  state.quickNotes = quickNotes;
  updateStorageBadge();
  renderAll();
}

function updateStorageBadge() {
  if (storage.isFolderConnected()) {
    const modeLabel = storage.isReadOnlyFolder() ? 'Sola lettura' : 'Lettura/Scrittura';
    els.storageBadge.textContent = `📁 ${storage.folderName} (${modeLabel})`;
    els.storageBadge.classList.add('folder-active');
    els.connectFolderLabel.textContent = storage.folderName;
    els.fixedPathText.textContent = `Percorso: 📁 ${storage.folderName}`;
    els.fixedPathText.title = `${storage.folderName} (${modeLabel})`;
  } else if (storage.getPendingReconnectFolderName()) {
    const remembered = storage.getPendingReconnectFolderName();
    els.storageBadge.textContent = `📁 ${remembered}`;
    els.storageBadge.classList.remove('folder-active');
    els.connectFolderLabel.textContent = `Riconnetti ${remembered}`;
    els.fixedPathText.textContent = `Percorso memorizzato: 📁 ${remembered}`;
    els.fixedPathText.title = `Ultima cartella collegata: ${remembered}`;
  } else {
    els.storageBadge.textContent = 'Cache Locale (Offline)';
    els.storageBadge.classList.remove('folder-active');
    els.connectFolderLabel.textContent = 'Cartella';
    els.fixedPathText.textContent = 'Percorso: Cache Locale (Offline)';
    els.fixedPathText.title = 'Dati salvati localmente in IndexedDB';
  }
}

// ===== FILTERING & RENDERING =====

function getUnifiedArchive() {
  const items = state.archive.map((item) => ({
    ...item,
    _isDiaryAttachment: false,
    _diarySourceLabel: '',
    _linkedEntry: null,
    _linkedAttachment: null
  }));

  if (!state.excludeDiaryAttachments) {
    for (const entry of state.diary) {
      for (const att of entry.Attachments || []) {
        items.push({
          Id: `att_${att.Id}`,
          Title: att.FileName,
          OriginalFileName: att.FileName,
          StoredRelativePath: att.StoredRelativePath,
          ArchivedAt: att.AddedAt || entry.Timestamp,
          Notes: '',
          FileSizeBytes: 0,
          IsDirectory: Boolean(att.IsDirectory),
          IsDone: entry.IsDone,
          _isDiaryAttachment: true,
          _diarySourceLabel: `Allegato Diario (${formatDateIt(entry.Timestamp)} ${formatShortTimeIt(entry.Timestamp)}): ${entry.Text}`,
          _linkedEntry: entry,
          _linkedAttachment: att
        });
      }
    }
  }

  return items.sort((a, b) => new Date(b.ArchivedAt) - new Date(a.ArchivedAt));
}

function matchesTerms(textFields, isoDate, query) {
  if (!query) return true;
  const terms = query.split(/\s+/).filter(Boolean);
  const dateIt = formatDateIt(isoDate).toLowerCase();
  const dateIso = (isoDate || '').slice(0, 10).toLowerCase();
  const timeIt = formatTimeIt(isoDate).toLowerCase();
  const haystack = [...textFields, dateIt, dateIso, timeIt].map((v) => String(v || '').toLowerCase());

  return terms.every((term) => {
    const t = term.toLowerCase();
    return haystack.some((field) => field.includes(t));
  });
}

function renderAll() {
  renderPending();
  renderArchive();
  renderDiary();
  renderNotes();
  renderQuickNotesChips();
}

function combineQueries(a, b) {
  return [a, b].map((s) => (s || '').trim()).filter(Boolean).join(' ');
}

function updateGlobalSearchCounts() {
  const active = Boolean(state.globalSearch || state.globalDateFilter);
  els.globalSearchCounts.classList.toggle('hidden', !active);
  els.globalArchiveCountBtn.textContent = `📁 ${state.lastArchiveCount} Archivio`;
  els.globalDiaryCountBtn.textContent = `📓 ${state.lastDiaryCount} Diario`;
  els.globalNotesCountBtn.textContent = `📝 ${state.lastNotesCount} Post-IT`;
}

function renderPending() {
  const pendingArchive = state.archive.filter((i) => !i.IsDone);
  const pendingDiary = state.diary.filter((e) => !e.IsDone);
  const pendingNotes = state.notes.filter((n) => !n.IsDone);
  const total = pendingArchive.length + pendingDiary.length + pendingNotes.length;

  els.navPendingBadge.textContent = total;
  els.navPendingBadge.classList.toggle('hidden', total === 0);
  els.pendingBanner.classList.toggle('hidden', total === 0);

  if (total === 0) {
    state.pendingExpanded = false;
    els.pendingList.classList.add('collapsed');
    els.pendingList.innerHTML = '';
    return;
  }

  els.pendingSummaryText.textContent = total === 1
    ? '⚠️ Hai 1 elemento non spuntato da terminare'
    : `⚠️ Hai ${total} elementi non spuntati da terminare`;

  els.pendingToggleLabel.textContent = state.pendingExpanded ? 'Nascondi elenco ▴' : 'Visualizza elenco ▾';
  els.pendingList.classList.toggle('collapsed', !state.pendingExpanded);

  const rows = [
    ...pendingArchive.map((i) => ({
      type: 'archive',
      id: i.Id,
      badge: '📁 Archivio',
      summary: i.Title || i.OriginalFileName,
      stamp: `${formatDateIt(i.ArchivedAt)} ${formatTimeIt(i.ArchivedAt)}`
    })),
    ...pendingDiary.map((e) => ({
      type: 'diary',
      id: e.Id,
      badge: '📓 Diario',
      summary: e.Text,
      stamp: `${formatDateIt(e.Timestamp)} ${formatTimeIt(e.Timestamp)}`
    })),
    ...pendingNotes.map((n) => ({
      type: 'notes',
      id: n.Id,
      badge: '📝 Post-IT',
      summary: n.Title || n.Text,
      stamp: `${formatDateIt(n.CreatedAt)} ${formatTimeIt(n.CreatedAt)}`
    }))
  ];

  els.pendingList.innerHTML = rows
    .map(
      (r) => `
      <div class="pending-row" data-type="${r.type}" data-id="${r.id}">
        <div class="pending-row-left">
          <span class="pending-source-badge">${r.badge}</span>
          <div class="pending-row-info">
            <div class="pending-row-title">${escapeHtml(r.summary)}</div>
            <div class="pending-row-date">${r.stamp}</div>
          </div>
        </div>
        <div class="pending-row-actions">
          <button class="btn btn-xs btn-primary js-pending-open" data-type="${r.type}" data-id="${r.id}" type="button">Apri</button>
          <button class="btn btn-xs btn-success js-pending-done" data-type="${r.type}" data-id="${r.id}" type="button">✓ Fatto</button>
        </div>
      </div>`
    )
    .join('');
}

function getFilteredArchiveItems() {
  const effectiveQuery = state.globalSearch;
  const effectiveDate = state.globalDateFilter;

  return getUnifiedArchive().filter((i) => {
    if (effectiveDate && (i.ArchivedAt || '').slice(0, 10) !== effectiveDate) return false;
    return matchesTerms([i.Title, i.OriginalFileName, i.Notes, i._diarySourceLabel], i.ArchivedAt, effectiveQuery);
  });
}

function getFilteredDiaryEntries(ignoreDayFilterWhenGlobal = true) {
  const effectiveQuery = state.globalSearch;
  const effectiveDate = state.globalDateFilter || (ignoreDayFilterWhenGlobal && state.globalSearch ? '' : state.diaryDateFilter);

  return state.diary.filter((e) => {
    if (effectiveDate && (e.Timestamp || '').slice(0, 10) !== effectiveDate) return false;
    return matchesTerms(
      [e.Text, ...(e.Attachments || []).map((a) => a.FileName)],
      e.Timestamp,
      effectiveQuery
    );
  });
}

function getFilteredNotes() {
  const effectiveQuery = state.globalSearch;
  const effectiveDate = state.globalDateFilter;

  return state.notes.filter((n) => {
    const stamp = n.CreatedAt || n.UpdatedAt || '';
    if (effectiveDate && stamp.slice(0, 10) !== effectiveDate) return false;
    return matchesTerms(
      [n.Title, n.Text, ...(n.Attachments || []).map((a) => a.FileName)],
      stamp,
      effectiveQuery
    );
  });
}

function renderGlobalSearchPopup(openIfActive = false) {
  const hasFilter = Boolean(state.globalSearch || state.globalDateFilter);
  if (!hasFilter) {
    els.globalSearchPopup.classList.add('hidden');
    return;
  }

  const archiveResults = getFilteredArchiveItems();
  const diaryResults = getFilteredDiaryEntries(true);
  const notesResults = getFilteredNotes();

  const parts = [];
  if (state.globalSearch) parts.push(`"${state.globalSearch}"`);
  if (state.globalDateFilter) parts.push(`data ${formatDateInputToIt(state.globalDateFilter)}`);
  els.globalSearchPopupSubtitle.textContent = `Filtro attivo: ${parts.join(' • ')}`;

  els.popupArchiveBadge.textContent = archiveResults.length;
  els.popupDiaryBadge.textContent = diaryResults.length;
  els.popupNotesBadge.textContent = notesResults.length;

  els.popupArchiveList.innerHTML = archiveResults.length === 0
    ? `<div class="empty-state">Nessun documento trovato</div>`
    : archiveResults
      .map((item) => `
        <button type="button" class="parallel-row js-popup-archive-row ${item.IsDone ? '' : 'pending-highlight'}" data-id="${item.Id}">
          <div class="parallel-row-title">${item._isDiaryAttachment ? '📓 ' : '📄 '}${escapeHtml(item.Title || item.OriginalFileName)}</div>
          <div class="parallel-row-meta">📅 ${formatDateIt(item.ArchivedAt)} 🕒 ${formatTimeIt(item.ArchivedAt)} • ${escapeHtml(item.OriginalFileName)}</div>
          ${item.Notes ? `<div class="parallel-row-sub">${escapeHtml(item.Notes)}</div>` : ''}
        </button>`)
      .join('');

  els.popupDiaryList.innerHTML = diaryResults.length === 0
    ? `<div class="empty-state">Nessuna nota diario trovata</div>`
    : diaryResults
      .map((entry) => `
        <button type="button" class="parallel-row js-popup-diary-row ${entry.IsDone ? '' : 'pending-highlight'}" data-id="${entry.Id}">
          <div class="parallel-row-title">${escapeHtml(entry.Text)}</div>
          <div class="parallel-row-meta">📅 ${formatDateIt(entry.Timestamp)} 🕒 ${formatTimeIt(entry.Timestamp)}${(entry.Attachments || []).length ? ` • 📎 ${entry.Attachments.length} allegati` : ''}</div>
        </button>`)
      .join('');

  els.popupNotesList.innerHTML = notesResults.length === 0
    ? `<div class="empty-state">Nessun Post-IT trovato</div>`
    : notesResults
      .map((note) => `
        <button type="button" class="parallel-row js-popup-notes-row ${note.IsDone ? '' : 'pending-highlight'}" data-id="${note.Id}">
          <div class="parallel-row-title">${escapeHtml(note.Title || note.Text)}</div>
          <div class="parallel-row-meta">📅 ${formatDateIt(note.CreatedAt)} 🕒 ${formatTimeIt(note.CreatedAt)}${(note.Attachments || []).length ? ` • 📎 ${note.Attachments.length} allegati` : ''}</div>
          ${note.Title && note.Text ? `<div class="parallel-row-sub">${escapeHtml(note.Text)}</div>` : ''}
        </button>`)
      .join('');

  if (openIfActive) {
    els.globalSearchPopup.classList.remove('hidden');
  }
}

function renderArchive() {
  const all = getFilteredArchiveItems();

  state.lastArchiveCount = all.length;
  updateGlobalSearchCounts();

  const totalCount = state.archive.length;
  els.totalCountBadge.textContent = totalCount === 1 ? '1 scheda' : `${totalCount} schede`;
  els.archiveSummaryText.textContent = all.length === 1 ? '1 scheda trovata' : `${all.length} schede trovate`;

  if (all.length === 0) {
    els.archiveList.innerHTML = `<div class="empty-state">Nessuna scheda trovata.<br>Trascina un documento nella zona di archiviazione per creare la prima scheda.</div>`;
    return;
  }

  els.archiveList.innerHTML = all
    .map((item) => {
      const badge = getFileBadge(item.OriginalFileName, item.IsDirectory);
      const pendingClass = item.IsDone ? '' : 'pending-highlight';
      const sizeText = item._isDiaryAttachment
        ? 'Allegato diario'
        : (item.IsDirectory ? 'Cartella' : formatBytes(item.FileSizeBytes));

      return `
      <article class="item-card ${pendingClass}" data-id="${item.Id}">
        <input type="checkbox" class="done-checkbox js-archive-done" data-id="${item.Id}"
          ${item.IsDone ? 'checked' : ''} title="Fatto (togli la spunta per ricordarti di terminarlo)">
        <div class="file-badge-box">${escapeHtml(badge)}</div>
        <div class="card-main">
          <div class="card-title-row">
            ${item._isDiaryAttachment ? `<span class="diary-source-pill">📓 Diario</span>` : ''}
            <span class="card-title">${escapeHtml(item.Title || item.OriginalFileName)}</span>
          </div>
          ${item._isDiaryAttachment ? `<div class="diary-source-label">${escapeHtml(item._diarySourceLabel)}</div>` : ''}
          <div class="card-meta">
            <span class="date-pill">📅 ${formatDateIt(item.ArchivedAt)} &nbsp;🕒 ${formatTimeIt(item.ArchivedAt)}</span>
            <span>${escapeHtml(item.OriginalFileName)}</span>
            <span>• ${sizeText}</span>
          </div>
          ${item.Notes ? `<div class="card-notes">${escapeHtml(item.Notes)}</div>` : ''}
        </div>
        <div class="card-actions">
          <button class="btn btn-xs btn-primary js-archive-open" data-id="${item.Id}" type="button">Apri</button>
          <button class="btn btn-xs btn-neutral js-archive-edit" data-id="${item.Id}" type="button">Modifica</button>
          <button class="btn btn-xs btn-danger-soft js-archive-delete" data-id="${item.Id}" type="button">Elimina</button>
        </div>
      </article>`;
    })
    .join('');
}

function renderDiary() {
  const todayKey = getTodayDateStr();
  const todayCount = state.diary.filter((e) => (e.Timestamp || '').slice(0, 10) === todayKey).length;
  els.diaryTodayCount.textContent = todayCount;

  const isDark = document.documentElement.getAttribute('data-theme') === 'dark';
  const palette = isDark ? DARK_PASTEL : LIGHT_PASTEL;

  const effectiveDate = state.globalDateFilter || (state.globalSearch ? '' : state.diaryDateFilter);
  const filtered = getFilteredDiaryEntries(true);

  state.lastDiaryCount = filtered.length;
  updateGlobalSearchCounts();

  const dayLabel = effectiveDate
    ? `del ${formatDateInputToIt(effectiveDate)}`
    : 'in tutti i giorni';
  els.diarySummaryText.textContent = filtered.length === 1
    ? `1 attività registrata ${dayLabel}`
    : `${filtered.length} attività registrate ${dayLabel}`;

  if (filtered.length === 0) {
    els.diaryList.innerHTML = `<div class="empty-state">Nessuna attività registrata per questo filtro.<br>Scrivi qui sopra e clicca 💾 Salva per iniziare.</div>`;
    return;
  }

  els.diaryList.innerHTML = filtered
    .map((entry, idx) => {
      const color = palette[idx % palette.length];
      const pendingClass = entry.IsDone ? '' : 'pending-highlight';
      const attachmentsHtml = (entry.Attachments || [])
        .map((att) => {
          const icon = att.IsDirectory ? '📁' : '📎';
          return `
          <span class="attach-chip">
            <button class="attach-open-btn js-diary-att-open" data-entry-id="${entry.Id}" data-att-id="${att.Id}" type="button" title="Apri allegato">
              ${icon} ${escapeHtml(att.FileName)}
            </button>
            <button class="attach-remove-btn js-diary-att-remove" data-entry-id="${entry.Id}" data-att-id="${att.Id}" type="button" title="Rimuovi allegato">✕</button>
          </span>`;
        })
        .join('');

      return `
      <article class="item-card js-diary-row ${pendingClass}" style="background:${color.bg};border-color:${entry.IsDone ? color.border : '#F59E0B'}" data-id="${entry.Id}">
        <input type="checkbox" class="done-checkbox js-diary-done" data-id="${entry.Id}"
          ${entry.IsDone ? 'checked' : ''} title="Fatto (togli la spunta per ricordarti di terminarlo)">
        <span class="diary-date-pill">📅 ${formatDateIt(entry.Timestamp)} &nbsp;🕒 ${formatTimeIt(entry.Timestamp)}</span>
        <div class="card-main">
          <div class="diary-text">${renderDiaryBodyHtml(entry)}</div>
          ${attachmentsHtml ? `<div class="attach-chips">${attachmentsHtml}</div>` : ''}
        </div>
        <div class="card-actions">
          <button class="btn btn-xs btn-primary js-diary-open" data-id="${entry.Id}" type="button" title="Apri nota completa">Apri</button>
          <button class="btn btn-xs btn-neutral js-diary-attach" data-id="${entry.Id}" type="button" title="Allega file a questa riga">📎 Allega</button>
          <button class="btn btn-xs btn-danger-soft js-diary-delete" data-id="${entry.Id}" type="button">Elimina</button>
        </div>
      </article>`;
    })
    .join('');
}

function renderNotes() {
  const isDark = document.documentElement.getAttribute('data-theme') === 'dark';
  const palette = isDark ? DARK_PASTEL : LIGHT_PASTEL;

  const filtered = getFilteredNotes();
  state.lastNotesCount = filtered.length;
  updateGlobalSearchCounts();

  els.notesCountBadge.textContent = state.notes.length;
  els.notesSummaryText.textContent = filtered.length === 1
    ? '1 Post-IT salvato'
    : `${filtered.length} Post-IT salvati`;

  if (filtered.length === 0) {
    els.notesList.innerHTML = `<div class="empty-state">Nessun Post-IT trovato.<br>Scrivi qui sopra e clicca 💾 Salva per creare il primo Post-IT.</div>`;
    return;
  }

  els.notesList.innerHTML = filtered
    .map((note, idx) => {
      const color = palette[idx % palette.length];
      const pendingClass = note.IsDone ? '' : 'pending-highlight';
      const attachmentsHtml = (note.Attachments || [])
        .map((att) => `
          <span class="attach-chip">
            <button class="attach-open-btn js-note-att-open" data-note-id="${note.Id}" data-att-id="${att.Id}" type="button" title="Apri allegato">
              📎 ${escapeHtml(att.FileName)}
            </button>
            <button class="attach-remove-btn js-note-att-remove" data-note-id="${note.Id}" data-att-id="${att.Id}" type="button" title="Rimuovi allegato">✕</button>
          </span>`)
        .join('');

      return `
      <article class="item-card js-note-row ${pendingClass}" style="background:${color.bg};border-color:${note.IsDone ? color.border : '#F59E0B'}" data-id="${note.Id}">
        <input type="checkbox" class="done-checkbox js-note-done" data-id="${note.Id}"
          ${note.IsDone ? 'checked' : ''} title="Fatto (togli la spunta per ricordarti di terminarlo)">
        <span class="diary-date-pill">📅 ${formatDateIt(note.CreatedAt)} &nbsp;🕒 ${formatTimeIt(note.CreatedAt)}</span>
        <div class="card-main">
          ${note.Title ? `<div class="card-title">${escapeHtml(note.Title)}</div>` : ''}
          <div class="diary-text">${renderDiaryBodyHtml(note, 'notes')}</div>
          ${attachmentsHtml ? `<div class="attach-chips">${attachmentsHtml}</div>` : ''}
        </div>
        <div class="card-actions">
          <button class="btn btn-xs btn-primary js-note-open" data-id="${note.Id}" type="button" title="Apri Post-IT completo">Apri</button>
          <button class="btn btn-xs btn-neutral js-note-attach" data-id="${note.Id}" type="button" title="Allega file a questo Post-IT">📎 Allega</button>
          <button class="btn btn-xs btn-danger-soft js-note-delete" data-id="${note.Id}" type="button">Elimina</button>
        </div>
      </article>`;
    })
    .join('');
}

function renderNoteDraftAttachments() {
  if (state.noteDraftAttachments.length === 0) {
    els.noteDraftAttachmentsPanel.classList.add('hidden');
    els.noteDraftAttachmentsList.innerHTML = '';
    return;
  }
  els.noteDraftAttachmentsPanel.classList.remove('hidden');
  els.noteDraftAttachmentsList.innerHTML = state.noteDraftAttachments
    .map((file, idx) => `
      <span class="attach-chip">
        <span>📎 ${escapeHtml(file.name)}</span>
        <button class="attach-remove-btn js-remove-note-draft-att" data-idx="${idx}" type="button" title="Rimuovi allegato dalla bozza">✕</button>
      </span>`)
    .join('');
}

function renderDraftAttachments() {
  if (state.draftAttachments.length === 0) {
    els.draftAttachmentsPanel.classList.add('hidden');
    els.draftAttachmentsList.innerHTML = '';
    return;
  }
  els.draftAttachmentsPanel.classList.remove('hidden');
  els.draftAttachmentsList.innerHTML = state.draftAttachments
    .map((file, idx) => `
      <span class="attach-chip">
        <span>📎 ${escapeHtml(file.name)}</span>
        <button class="attach-remove-btn js-remove-draft-att" data-idx="${idx}" type="button" title="Rimuovi allegato dalla bozza">✕</button>
      </span>`)
    .join('');
}

function renderQuickNotesChips() {
  if (!state.quickNotes || state.quickNotes.length === 0) {
    els.quickNotesChips.innerHTML = `<span style="font-size:0.78rem;color:var(--text-muted);padding:6px 8px">Nessuna nota pronta</span>`;
    return;
  }
  els.quickNotesChips.innerHTML = state.quickNotes
    .map((n) => `
      <button type="button" class="menu-item-btn js-quick-note-chip" data-id="${n.Id}" role="menuitem"
        title="Clicca per inserire questa nota pronta nell'editor">⚡ ${escapeHtml(n.Text)}</button>`)
    .join('');
}

function renderQuickNotesTable() {
  if (!state.quickNotes || state.quickNotes.length === 0) {
    els.quickNotesTableBody.innerHTML = `<tr><td colspan="2" style="text-align:center;color:var(--text-muted);padding:18px">Nessuna nota pronta salvata.</td></tr>`;
    return;
  }
  els.quickNotesTableBody.innerHTML = state.quickNotes
    .map((n) => `
      <tr>
        <td>${escapeHtml(n.Text)}</td>
        <td class="col-actions">
          <div class="table-actions-cell">
            <button type="button" class="btn btn-xs btn-primary js-qn-use" data-id="${n.Id}" title="Inserisci nel Diario">↙️ Usa</button>
            <button type="button" class="btn btn-xs btn-neutral js-qn-edit" data-id="${n.Id}">✏️ Modifica</button>
            <button type="button" class="btn btn-xs btn-danger-soft js-qn-delete" data-id="${n.Id}">🗑️ Elimina</button>
          </div>
        </td>
      </tr>`)
    .join('');
}

function insertTextIntoDiaryEditor(text) {
  if (!text) return;
  const current = extractEditorPlainText(els.diaryEditor);
  if (!current) {
    els.diaryEditor.textContent = text;
  } else {
    els.diaryEditor.innerHTML += `<br>${escapeHtml(text)}`;
  }
  els.diaryEditor.focus();
}

function resetQuickNoteEditMode() {
  state.editingQuickNoteId = null;
  els.quickNoteInput.value = '';
  els.saveQuickNoteBtn.textContent = '➕ Aggiungi';
  els.cancelQuickNoteEditBtn.classList.add('hidden');
}

// ===== ACTIONS =====

async function handleDroppedOrSelectedFiles(fileList) {
  const files = Array.from(fileList || []);
  if (files.length === 0) return;

  if (state.activeSection === 'diary' || state.activeSection === 'notes') {
    const targetList = state.activeSection === 'notes' ? state.noteDraftAttachments : state.draftAttachments;
    for (const file of files) {
      if (!targetList.some((f) => f.name === file.name && f.size === file.size)) {
        targetList.push(file);
      }
    }
    if (state.activeSection === 'notes') {
      renderNoteDraftAttachments();
    } else {
      renderDraftAttachments();
    }
    showStatus(
      files.length === 1
        ? `Allegato pronto: "${files[0].name}" (clicca 💾 Salva)`
        : `${files.length} allegati pronti (clicca 💾 Salva)`
    );
    return;
  }

  try {
    const added = await storage.addArchiveFiles(
      files,
      els.customTitleInput.value,
      els.customNotesInput.value
    );
    if (added.length === 0) return;

    els.customTitleInput.value = '';
    els.customNotesInput.value = '';
    await reloadData();

    if (added.length === 1) {
      const item = added[0];
      showStatus(`Archiviato: "${item.Title}" (${formatDateIt(item.ArchivedAt)} ${formatTimeIt(item.ArchivedAt)})`);
    } else {
      showStatus(`Archiviati ${added.length} elementi`);
    }
  } catch (err) {
    showStatus(err?.message || 'Errore durante l\'archiviazione', true);
  }
}

let currentPreview = {
  blob: null,
  fileName: '',
  objectUrl: null
};

function closeFilePreview() {
  els.filePreviewModal.classList.add('hidden');
  els.filePreviewBody.innerHTML = '';
  if (currentPreview.objectUrl) {
    URL.revokeObjectURL(currentPreview.objectUrl);
  }
  currentPreview = { blob: null, fileName: '', objectUrl: null };
}

function triggerFileDownload(blob, fileName) {
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = fileName || 'documento';
  a.click();
  setTimeout(() => URL.revokeObjectURL(url), 10_000);
}

async function openStoredFile(storedRelativePath, fileName, isDiaryAttachment = false) {
  const blob = await storage.getStoredFileBlob(storedRelativePath, isDiaryAttachment);
  if (!blob) {
    showToast(`File non disponibile localmente (${storedRelativePath || fileName})`);
    return;
  }

  if (currentPreview.objectUrl) {
    URL.revokeObjectURL(currentPreview.objectUrl);
  }

  const ext = getExtension(fileName);
  els.filePreviewTitle.textContent = `📄 ${fileName || 'Anteprima Documento'}`;
  els.filePreviewSubtitle.textContent = `${ext ? ext.toUpperCase() + ' • ' : ''}${formatBytes(blob.size)}`;
  els.filePreviewBody.innerHTML = `<div class="empty-state">Caricamento anteprima...</div>`;
  els.filePreviewModal.classList.remove('hidden');

  const { objectUrl } = await renderFilePreview(blob, fileName, els.filePreviewBody);
  currentPreview = { blob, fileName, objectUrl };
}

function downloadJsonFile(filename, data) {
  const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  a.click();
  setTimeout(() => URL.revokeObjectURL(url), 10_000);
}

function openEditArchiveModal(item) {
  state.editingArchiveId = item.Id;
  els.editItemTitleInput.value = item.Title || '';
  els.editItemNotesInput.value = item.Notes || '';
  els.editItemModal.classList.remove('hidden');
  els.editItemTitleInput.focus();
}

async function openPendingItem(type, id) {
  if (type === 'archive') {
    const item = getUnifiedArchive().find((i) => i.Id === id);
    switchSection('archive');
    const card = els.archiveList.querySelector(`.item-card[data-id="${id}"]`);
    card?.scrollIntoView({ behavior: 'smooth', block: 'center' });
    if (item) {
      await openStoredFile(item.StoredRelativePath, item.OriginalFileName, item._isDiaryAttachment);
    }
    return;
  }

  if (type === 'notes') {
    const note = state.notes.find((n) => n.Id === id);
    if (note) {
      switchSection('notes');
      renderNotes();
      const card = els.notesList.querySelector(`.item-card[data-id="${id}"]`);
      card?.scrollIntoView({ behavior: 'smooth', block: 'center' });
      openNoteDetailModal(note);
    }
    return;
  }

  const entry = state.diary.find((d) => d.Id === id);
  if (entry) {
    state.diaryDateFilter = (entry.Timestamp || '').slice(0, 10);
    els.diaryDateInput.value = state.diaryDateFilter;
    switchSection('diary');
    renderDiary();
    const card = els.diaryList.querySelector(`.item-card[data-id="${id}"]`);
    card?.scrollIntoView({ behavior: 'smooth', block: 'center' });
    openDiaryDetailModal(entry);
  }
}

function openDiaryDetailModal(entry) {
  state.detailDiaryId = entry.Id;
  els.detailTimestampText.textContent = `📅 ${formatDateIt(entry.Timestamp)}   🕒 ${formatTimeIt(entry.Timestamp)}`;
  els.detailDoneChk.checked = Boolean(entry.IsDone);
  els.detailEditor.innerHTML = renderDiaryBodyHtml(entry, 'diary');
  renderDetailAttachments(entry);
  els.diaryDetailModal.classList.remove('hidden');
  els.detailEditor.focus();
}

function openNoteDetailModal(note) {
  state.detailNoteId = note.Id;
  els.noteDetailTimestampText.textContent = `📅 ${formatDateIt(note.CreatedAt)}   🕒 ${formatTimeIt(note.CreatedAt)}`;
  els.noteDetailTitleInput.value = note.Title || '';
  els.noteDetailDoneChk.checked = Boolean(note.IsDone);
  els.noteDetailEditor.innerHTML = renderDiaryBodyHtml(note, 'notes');
  renderNoteDetailAttachments(note);
  els.noteDetailModal.classList.remove('hidden');
  els.noteDetailEditor.focus();
}

function renderNoteDetailAttachments(note) {
  const atts = note.Attachments || [];
  if (atts.length === 0) {
    els.noteDetailAttachmentsSection.classList.add('hidden');
    els.noteDetailAttachmentsList.innerHTML = '';
    return;
  }
  els.noteDetailAttachmentsSection.classList.remove('hidden');
  els.noteDetailAttachmentsList.innerHTML = atts
    .map((att) => `
      <span class="attach-chip">
        <button class="attach-open-btn js-note-att-open" data-note-id="${note.Id}" data-att-id="${att.Id}" type="button">
          📎 ${escapeHtml(att.FileName)}
        </button>
        <button class="attach-remove-btn js-note-detail-att-remove" data-note-id="${note.Id}" data-att-id="${att.Id}" type="button" title="Rimuovi allegato">✕</button>
      </span>`)
    .join('');
}

function renderDetailAttachments(entry) {
  const atts = entry.Attachments || [];
  if (atts.length === 0) {
    els.detailAttachmentsSection.classList.add('hidden');
    els.detailAttachmentsList.innerHTML = '';
    return;
  }
  els.detailAttachmentsSection.classList.remove('hidden');
  els.detailAttachmentsList.innerHTML = atts
    .map((att) => `
      <span class="attach-chip">
        <button class="attach-open-btn js-diary-att-open" data-entry-id="${entry.Id}" data-att-id="${att.Id}" type="button">
          ${att.IsDirectory ? '📁' : '📎'} ${escapeHtml(att.FileName)}
        </button>
        <button class="attach-remove-btn js-detail-att-remove" data-entry-id="${entry.Id}" data-att-id="${att.Id}" type="button" title="Rimuovi allegato">✕</button>
      </span>`)
    .join('');
}

async function renderSettingsPanels() {
  const folderSupported = storage.isFolderApiSupported();
  const hasSaved = await storage.hasSavedFolderHandle();
  const connected = storage.isFolderConnected();

  const html = `
    <div class="settings-group">
      <h4>📁 Cartella MemoBam (ChromeOS / Desktop)</h4>
      <p>${folderSupported
      ? connected
        ? `Collegata a <strong>${escapeHtml(storage.folderName)}</strong> (${storage.isReadOnlyFolder() ? 'Sola lettura' : 'Lettura e Scrittura'}): le modifiche e i file vengono salvati direttamente nella cartella compatibile con MemoBam PC.`
        : 'Collega la tua cartella MemoBam (es. OneDrive / Google Drive) per leggere e scrivere direttamente schede, diario, allegati e file Markdown.'
      : 'Su questo browser mobile i dati e i file sono salvati offline nella memoria locale (IndexedDB). Usa Importa/Esporta JSON qui sotto per sincronizzare.'
    }</p>
      <div class="settings-actions">
        ${folderSupported
      ? `<button class="btn btn-primary btn-sm js-connect-folder" type="button">📁 ${connected ? 'Cambia Cartella' : 'Collega Cartella'}</button>`
      : ''
    }
        ${folderSupported && !connected && hasSaved
      ? `<button class="btn btn-neutral btn-sm js-reconnect-folder" type="button">🔄 Riconnetti ultima cartella</button>`
      : ''
    }
        ${connected
      ? `<button class="btn btn-danger btn-sm js-disconnect-folder" type="button">Scollega</button>`
      : ''
    }
      </div>
    </div>

    <div class="settings-group">
      <h4>⬆️ Importa / ⬇️ Esporta Indici JSON</h4>
      <p>Importa <code>archivio_memobam.json</code> / <code>diario_index.json</code> oppure esporta i tuoi dati per il PC.</p>
      <div class="settings-actions">
        <label class="btn btn-primary btn-sm">
          📥 Importa JSON
          <input type="file" accept=".json,application/json" multiple class="hidden js-import-json">
        </label>
        <button class="btn btn-neutral btn-sm js-export-archive" type="button">📤 archivio_memobam.json</button>
        <button class="btn btn-neutral btn-sm js-export-diary" type="button">📤 diario_index.json</button>
        <button class="btn btn-neutral btn-sm js-export-all" type="button">💾 Backup Completo</button>
      </div>
    </div>

    <div class="settings-group">
      <h4>🧹 Manutenzione Cache Locale</h4>
      <p>Svuota i dati salvati in IndexedDB su questo dispositivo (non tocca eventuali cartelle collegate).</p>
      <div class="settings-actions">
        <button class="btn btn-danger-soft btn-sm js-clear-cache" type="button">Svuota cache locale</button>
      </div>
    </div>
  `;

  els.settingsContentMobile.innerHTML = html;
  els.settingsContentModal.innerHTML = html;
}

// ===== NAVIGATION =====

function switchSection(section) {
  state.activeSection = section;

  // Desktop tabs
  els.archiveTabBtn.classList.toggle('active', section === 'archive');
  els.diaryTabBtn.classList.toggle('active', section === 'diary');
  els.notesTabBtn.classList.toggle('active', section === 'notes');
  els.archiveTabContent.classList.toggle('active', section === 'archive');
  els.diaryTabContent.classList.toggle('active', section === 'diary');
  els.notesTabContent.classList.toggle('active', section === 'notes');

  // Mobile bottom nav & panes
  document.querySelectorAll('.nav-btn').forEach((b) => b.classList.toggle('active', b.dataset.tab === section));
  els.dropPane.classList.toggle('mobile-active', section === 'drop');
  els.archiveTabContent.classList.toggle('mobile-active', section === 'archive' || section === 'pending');
  els.diaryTabContent.classList.toggle('mobile-active', section === 'diary');
  els.notesTabContent.classList.toggle('mobile-active', section === 'notes');
  els.settingsPane.classList.toggle('mobile-active', section === 'settings');

  if (section === 'pending') {
    state.pendingExpanded = true;
    renderPending();
    els.pendingBanner.scrollIntoView({ behavior: 'smooth' });
  } else if (section === 'settings') {
    renderSettingsPanels();
  }
}

// ===== EVENT BINDINGS =====

function bindEvents() {
  // Desktop section tabs
  els.archiveTabBtn.addEventListener('click', () => switchSection('archive'));
  els.diaryTabBtn.addEventListener('click', () => switchSection('diary'));
  els.notesTabBtn.addEventListener('click', () => switchSection('notes'));

  // Mobile bottom nav
  document.querySelectorAll('.nav-btn').forEach((btn) => {
    btn.addEventListener('click', () => switchSection(btn.dataset.tab));
  });

  // Theme toggle
  els.themeToggleBtn.addEventListener('click', () => {
    const curr = document.documentElement.getAttribute('data-theme');
    applyTheme(curr === 'dark' ? 'light' : 'dark');
  });

  // Top-level Global Search + Parallel Results Popup
  const triggerGlobalSearch = () => {
    renderArchive();
    renderDiary();
    renderGlobalSearchPopup(true);
  };
  els.globalSearchInput.addEventListener('input', () => {
    state.globalSearch = els.globalSearchInput.value.trim();
    triggerGlobalSearch();
  });
  els.globalDateInput.addEventListener('change', () => {
    state.globalDateFilter = els.globalDateInput.value;
    triggerGlobalSearch();
  });
  els.clearGlobalSearchBtn.addEventListener('click', () => {
    els.globalSearchInput.value = '';
    els.globalDateInput.value = '';
    state.globalSearch = '';
    state.globalDateFilter = '';
    els.globalSearchPopup.classList.add('hidden');
    renderArchive();
    renderDiary();
  });
  els.globalArchiveCountBtn.addEventListener('click', () => {
    renderGlobalSearchPopup(true);
  });
  els.globalDiaryCountBtn.addEventListener('click', () => {
    renderGlobalSearchPopup(true);
  });
  els.globalNotesCountBtn.addEventListener('click', () => {
    renderGlobalSearchPopup(true);
  });
  els.closeGlobalPopupTopBtn.addEventListener('click', () => els.globalSearchPopup.classList.add('hidden'));
  els.closeGlobalPopupFooterBtn.addEventListener('click', () => els.globalSearchPopup.classList.add('hidden'));
  els.globalSearchPopup.addEventListener('click', (e) => {
    if (e.target === els.globalSearchPopup) els.globalSearchPopup.classList.add('hidden');
  });
  document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') {
      if (!els.topbarMenuDropdown.classList.contains('hidden')) els.topbarMenuDropdown.classList.add('hidden');
      else if (!els.quickNotesDropdown.classList.contains('hidden')) els.quickNotesDropdown.classList.add('hidden');
      else if (!els.filePreviewModal.classList.contains('hidden')) closeFilePreview();
      else if (!els.diaryDetailModal.classList.contains('hidden')) els.diaryDetailModal.classList.add('hidden');
      else if (!els.noteDetailModal.classList.contains('hidden')) els.noteDetailModal.classList.add('hidden');
      else if (!els.creditsModal.classList.contains('hidden')) els.creditsModal.classList.add('hidden');
      else if (!els.globalSearchPopup.classList.contains('hidden')) els.globalSearchPopup.classList.add('hidden');
    }
  });

  // Left Topbar Menu Dropdown
  els.topbarMenuBtn.addEventListener('click', (e) => {
    e.stopPropagation();
    const isHidden = els.topbarMenuDropdown.classList.toggle('hidden');
    els.topbarMenuBtn.setAttribute('aria-expanded', String(!isHidden));
  });
  document.addEventListener('click', (e) => {
    if (!els.topbarMenuDropdown.classList.contains('hidden') && !e.target.closest('.topbar-menu-wrap')) {
      els.topbarMenuDropdown.classList.add('hidden');
      els.topbarMenuBtn.setAttribute('aria-expanded', 'false');
    }
    if (!els.quickNotesDropdown.classList.contains('hidden') && !e.target.closest('.quick-notes-menu-wrap')) {
      els.quickNotesDropdown.classList.add('hidden');
      els.quickNotesToggleBtn.setAttribute('aria-expanded', 'false');
    }
  });

  // Credits & QR Code Modal
  els.creditsBtn.addEventListener('click', () => {
    els.topbarMenuDropdown.classList.add('hidden');
    els.creditsModal.classList.remove('hidden');
  });
  els.closeCreditsModalBtn.addEventListener('click', () => els.creditsModal.classList.add('hidden'));
  els.closeCreditsFooterBtn.addEventListener('click', () => els.creditsModal.classList.add('hidden'));
  els.creditsModal.addEventListener('click', (e) => {
    if (e.target === els.creditsModal) els.creditsModal.classList.add('hidden');
  });
  els.copyPwaUrlBtn.addEventListener('click', async () => {
    const url = 'https://ambruno1968.github.io/MemoBam/';
    try {
      await navigator.clipboard.writeText(url);
      showToast('Link PWA copiato negli appunti!');
    } catch {
      showToast(url);
    }
  });

  // Document Preview Modal actions
  els.closeFilePreviewTopBtn.addEventListener('click', closeFilePreview);
  els.filePreviewModal.addEventListener('click', (e) => {
    if (e.target === els.filePreviewModal) closeFilePreview();
  });
  els.previewSaveFileBtn.addEventListener('click', async () => {
    if (!currentPreview.blob) return;
    if (typeof window.showSaveFilePicker === 'function') {
      try {
        const ext = getExtension(currentPreview.fileName);
        const typedBlob = getTypedBlob(currentPreview.blob, currentPreview.fileName);
        const options = { suggestedName: currentPreview.fileName || 'documento' };
        if (ext && typedBlob.type && typedBlob.type !== 'application/octet-stream') {
          options.types = [{
            description: `File ${ext.toUpperCase()}`,
            accept: { [typedBlob.type.split(';')[0]]: [`.${ext}`] }
          }];
        }
        const handle = await window.showSaveFilePicker(options);
        const writable = await handle.createWritable();
        await writable.write(currentPreview.blob);
        await writable.close();
        showToast(`File salvato: "${handle.name}"`);
        return;
      } catch (err) {
        if (err?.name === 'AbortError') return;
      }
    }
    triggerFileDownload(currentPreview.blob, currentPreview.fileName);
    showToast(`Salvataggio di "${currentPreview.fileName}" avviato`);
  });
  els.previewOpenSystemBtn.addEventListener('click', async () => {
    if (!currentPreview.blob) return;
    const typedBlob = getTypedBlob(currentPreview.blob, currentPreview.fileName);
    const isMobileOrChromeOs = navigator.userAgentData?.mobile || /Android|iPhone|iPad|CrOS/i.test(navigator.userAgent);

    if (isMobileOrChromeOs) {
      try {
        const fileObj = new File([typedBlob], currentPreview.fileName, { type: typedBlob.type });
        if (navigator.canShare?.({ files: [fileObj] })) {
          await navigator.share({ files: [fileObj], title: currentPreview.fileName });
          return;
        }
      } catch (err) {
        if (err?.name === 'AbortError') return;
      }
    }

    const ext = getExtension(currentPreview.fileName);
    const browserInline = ['pdf', 'txt', 'html', 'htm', 'png', 'jpg', 'jpeg', 'gif', 'webp', 'svg', 'json', 'xml'].includes(ext);
    if (browserInline) {
      const url = URL.createObjectURL(typedBlob);
      window.open(url, '_blank', 'noopener');
      setTimeout(() => URL.revokeObjectURL(url), 60_000);
    } else {
      triggerFileDownload(typedBlob, currentPreview.fileName);
      showToast(`"${currentPreview.fileName}" scaricato: cliccalo nei Download (attiva "Apri sempre" per lanciarlo in automatico).`);
    }
  });

  // Quick Notes ("Note Pronte") Dropdown, Modal & Chips
  els.quickNotesToggleBtn.addEventListener('click', (e) => {
    e.stopPropagation();
    const isHidden = els.quickNotesDropdown.classList.toggle('hidden');
    els.quickNotesToggleBtn.setAttribute('aria-expanded', String(!isHidden));
  });
  els.manageQuickNotesBtn.addEventListener('click', () => {
    els.quickNotesDropdown.classList.add('hidden');
    els.quickNotesToggleBtn.setAttribute('aria-expanded', 'false');
    resetQuickNoteEditMode();
    renderQuickNotesTable();
    els.quickNotesModal.classList.remove('hidden');
    els.quickNoteInput.focus();
  });
  els.closeQuickNotesModalBtn.addEventListener('click', () => els.quickNotesModal.classList.add('hidden'));
  els.closeQuickNotesFooterBtn.addEventListener('click', () => els.quickNotesModal.classList.add('hidden'));
  els.cancelQuickNoteEditBtn.addEventListener('click', resetQuickNoteEditMode);
  els.saveQuickNoteBtn.addEventListener('click', async () => {
    const text = els.quickNoteInput.value.trim();
    if (!text) {
      showToast('Inserisci il testo della nota pronta.');
      els.quickNoteInput.focus();
      return;
    }
    if (state.editingQuickNoteId) {
      await storage.updateQuickNote(state.editingQuickNoteId, text);
      resetQuickNoteEditMode();
      showToast('Nota pronta aggiornata');
    } else {
      await storage.addQuickNote(text);
      els.quickNoteInput.value = '';
      showToast('Nota pronta aggiunta');
    }
    state.quickNotes = await storage.getQuickNotes();
    renderQuickNotesChips();
    renderQuickNotesTable();
  });

  // Drop zone
  els.browseFilesBtn.addEventListener('click', (e) => {
    e.stopPropagation();
    els.fileInput.click();
  });
  els.dropZone.addEventListener('click', () => els.fileInput.click());
  els.fileInput.addEventListener('change', async () => {
    await handleDroppedOrSelectedFiles(els.fileInput.files);
    els.fileInput.value = '';
  });

  els.dropZone.addEventListener('dragenter', (e) => {
    e.preventDefault();
    els.dropZone.classList.add('drag-over');
    els.dropZoneTitle.textContent = 'Rilascia per archiviare';
  });
  els.dropZone.addEventListener('dragover', (e) => {
    e.preventDefault();
    els.dropZone.classList.add('drag-over');
  });
  els.dropZone.addEventListener('dragleave', () => {
    els.dropZone.classList.remove('drag-over');
    els.dropZoneTitle.textContent = 'Trascina qui i documenti';
  });
  els.dropZone.addEventListener('drop', async (e) => {
    e.preventDefault();
    els.dropZone.classList.remove('drag-over');
    els.dropZoneTitle.textContent = 'Trascina qui i documenti';
    await handleDroppedOrSelectedFiles(e.dataTransfer?.files);
  });

  els.excludeDiaryAttachmentsChk.checked = state.excludeDiaryAttachments;
  els.excludeDiaryAttachmentsChk.addEventListener('change', () => {
    state.excludeDiaryAttachments = els.excludeDiaryAttachmentsChk.checked;
    localStorage.setItem('memobam_exclude_diary_att', String(state.excludeDiaryAttachments));
    renderArchive();
    if (!els.globalSearchPopup.classList.contains('hidden')) renderGlobalSearchPopup(false);
  });

  // Appunti composer & detail modal
  els.noteFmtBoldBtn.addEventListener('click', () => execEditorCmd(els.noteEditor, 'bold'));
  els.noteFmtItalicBtn.addEventListener('click', () => execEditorCmd(els.noteEditor, 'italic'));
  els.noteFmtBulletBtn.addEventListener('click', () => execEditorCmd(els.noteEditor, 'insertUnorderedList'));
  els.noteFmtCheckBtn.addEventListener('click', () => insertSecondaryCheckBox(els.noteEditor));
  els.noteDraftAttachBtn.addEventListener('click', () => els.noteDraftAttachInput.click());
  els.noteDraftAttachInput.addEventListener('change', () => {
    for (const file of Array.from(els.noteDraftAttachInput.files || [])) {
      if (!state.noteDraftAttachments.some((f) => f.name === file.name && f.size === file.size)) {
        state.noteDraftAttachments.push(file);
      }
    }
    els.noteDraftAttachInput.value = '';
    renderNoteDraftAttachments();
  });
  els.saveNoteBtn.addEventListener('click', async () => {
    let text = extractEditorPlainText(els.noteEditor);
    const title = els.noteTitleInput.value.trim();
    if (!title) {
      showToast('Il titolo del Post-IT è obbligatorio.');
      els.noteTitleInput.focus();
      return;
    }
    if (!text && state.noteDraftAttachments.length === 0) return;
    if (!text && state.noteDraftAttachments.length > 0) {
      text = `Allegati (${state.noteDraftAttachments.map((f) => f.name).join(', ')})`;
    }
    const richHtml = serializeEditorHtml(els.noteEditor);
    try {
      const note = await storage.addNote(title, text, richHtml || null);
      if (state.noteDraftAttachments.length > 0) {
        await storage.addNoteAttachments(note.Id, state.noteDraftAttachments);
        state.noteDraftAttachments = [];
        renderNoteDraftAttachments();
      }
      els.noteTitleInput.value = '';
      els.noteEditor.innerHTML = '';
      await reloadData();
      showStatus(`Post-IT salvato (${formatDateIt(note.CreatedAt)} ${formatTimeIt(note.CreatedAt)})`);
    } catch (err) {
      showStatus(err?.message || 'Errore salvataggio Post-IT', true);
    }
  });

  els.closeNoteDetailModalBtn.addEventListener('click', () => els.noteDetailModal.classList.add('hidden'));
  els.cancelNoteDetailBtn.addEventListener('click', () => els.noteDetailModal.classList.add('hidden'));
  els.noteDetailBoldBtn.addEventListener('click', () => execEditorCmd(els.noteDetailEditor, 'bold'));
  els.noteDetailItalicBtn.addEventListener('click', () => execEditorCmd(els.noteDetailEditor, 'italic'));
  els.noteDetailBulletBtn.addEventListener('click', () => execEditorCmd(els.noteDetailEditor, 'insertUnorderedList'));
  els.noteDetailCheckBtn.addEventListener('click', () => insertSecondaryCheckBox(els.noteDetailEditor));
  els.noteDetailAttachBtn.addEventListener('click', () => els.noteDetailAttachInput.click());
  els.noteDetailAttachInput.addEventListener('change', async () => {
    const files = Array.from(els.noteDetailAttachInput.files || []);
    els.noteDetailAttachInput.value = '';
    if (!files.length || !state.detailNoteId) return;
    await storage.addNoteAttachments(state.detailNoteId, files);
    await reloadData();
    const updated = state.notes.find((n) => n.Id === state.detailNoteId);
    if (updated) renderNoteDetailAttachments(updated);
    showToast(`Collegati ${files.length} allegati`);
  });
  els.saveNoteDetailBtn.addEventListener('click', async () => {
    const text = extractEditorPlainText(els.noteDetailEditor);
    const title = els.noteDetailTitleInput.value.trim();
    if (!title) {
      showToast('Il titolo del Post-IT è obbligatorio.');
      els.noteDetailTitleInput.focus();
      return;
    }
    if (!text) {
      showToast('Il testo del Post-IT non può essere vuoto.');
      els.noteDetailEditor.focus();
      return;
    }
    const existing = state.notes.find((n) => n.Id === state.detailNoteId);
    if (!existing) return;
    await storage.updateNote({
      ...existing,
      Title: title,
      Text: text,
      RichContentHtml: serializeEditorHtml(els.noteDetailEditor),
      IsDone: els.noteDetailDoneChk.checked
    });
    els.noteDetailModal.classList.add('hidden');
    await reloadData();
    showToast('Post-IT aggiornato');
  });

  els.noteEntryAttachInput.addEventListener('change', async () => {
    const files = Array.from(els.noteEntryAttachInput.files || []);
    els.noteEntryAttachInput.value = '';
    if (!files.length || !state.pendingAttachNoteId) return;
    try {
      await storage.addNoteAttachments(state.pendingAttachNoteId, files);
      await reloadData();
      showStatus(files.length === 1 ? `Allegato "${files[0].name}" collegato.` : `Collegati ${files.length} allegati.`);
    } catch (err) {
      showStatus(err?.message || 'Errore allegato appunto', true);
    }
    state.pendingAttachNoteId = null;
  });

  els.notesList.addEventListener('dragover', (e) => {
    const row = e.target.closest('.js-note-row');
    if (!row) return;
    e.preventDefault();
    row.classList.add('drag-over');
  });
  els.notesList.addEventListener('dragleave', (e) => {
    const row = e.target.closest('.js-note-row');
    if (row) row.classList.remove('drag-over');
  });
  els.notesList.addEventListener('drop', async (e) => {
    const row = e.target.closest('.js-note-row');
    if (!row) return;
    e.preventDefault();
    row.classList.remove('drag-over');
    const files = Array.from(e.dataTransfer?.files || []);
    if (!files.length) return;
    try {
      await storage.addNoteAttachments(row.dataset.id, files);
      await reloadData();
      showStatus(files.length === 1 ? `Allegato "${files[0].name}" collegato.` : `Collegati ${files.length} allegati.`);
    } catch (err) {
      showStatus(err?.message || 'Errore allegato appunto', true);
    }
  });

  // Diary date controls
  els.diaryDateInput.value = state.diaryDateFilter;
  els.diaryDateInput.addEventListener('change', () => {
    state.diaryDateFilter = els.diaryDateInput.value;
    renderDiary();
  });
  els.diaryTodayBtn.addEventListener('click', () => {
    state.diaryDateFilter = getTodayDateStr();
    els.diaryDateInput.value = state.diaryDateFilter;
    renderDiary();
  });
  els.diaryAllDaysBtn.addEventListener('click', () => {
    state.diaryDateFilter = '';
    els.diaryDateInput.value = '';
    renderDiary();
  });

  els.fmtBoldBtn.addEventListener('click', () => execEditorCmd(els.diaryEditor, 'bold'));
  els.fmtItalicBtn.addEventListener('click', () => execEditorCmd(els.diaryEditor, 'italic'));
  els.fmtBulletBtn.addEventListener('click', () => execEditorCmd(els.diaryEditor, 'insertUnorderedList'));
  els.fmtCheckBtn.addEventListener('click', () => insertSecondaryCheckBox(els.diaryEditor));

  els.draftAttachBtn.addEventListener('click', () => els.draftAttachInput.click());
  els.draftAttachInput.addEventListener('change', () => {
    for (const file of Array.from(els.draftAttachInput.files || [])) {
      if (!state.draftAttachments.some((f) => f.name === file.name && f.size === file.size)) {
        state.draftAttachments.push(file);
      }
    }
    els.draftAttachInput.value = '';
    renderDraftAttachments();
  });

  els.saveDiaryBtn.addEventListener('click', async () => {
    let text = extractEditorPlainText(els.diaryEditor);
    if (!text && state.draftAttachments.length === 0) return;

    if (!text && state.draftAttachments.length > 0) {
      text = `Allegati (${state.draftAttachments.map((f) => f.name).join(', ')})`;
    }

    const richHtml = serializeEditorHtml(els.diaryEditor);
    try {
      const entry = await storage.addDiaryEntry(
        text,
        state.diaryDateFilter || getTodayDateStr(),
        richHtml || null
      );
      if (state.draftAttachments.length > 0) {
        await storage.addDiaryAttachments(entry.Id, state.draftAttachments);
        state.draftAttachments = [];
        renderDraftAttachments();
      }
      els.diaryEditor.innerHTML = '';
      await reloadData();
      showStatus(`Diario registrato (${formatDateIt(entry.Timestamp)} ${formatTimeIt(entry.Timestamp)})`);
    } catch (err) {
      showStatus(err?.message || 'Errore salvataggio diario', true);
    }
  });

  // Pending alerts toggle
  els.pendingToggleBtn.addEventListener('click', () => {
    state.pendingExpanded = !state.pendingExpanded;
    renderPending();
  });

  // Folder & Settings modals
  const connectFolderAction = async () => {
    try {
      if (!storage.isFolderConnected() && storage.getPendingReconnectFolderName()) {
        const ok = await storage.reconnectSavedFolder();
        if (ok) {
          await reloadData();
          await renderSettingsPanels();
          showToast(`Cartella riconnessa: ${storage.folderName}`);
          return;
        }
      }
      const res = await storage.connectFolder();
      await reloadData();
      await renderSettingsPanels();
      showToast(`Cartella collegata (${res.writable ? 'Lettura/Scrittura' : 'Sola lettura'}): ${res.name}`);
    } catch (err) {
      if (err?.name !== 'AbortError') showToast(err?.message || 'Errore connessione cartella');
    }
  };
  els.connectFolderBtn.addEventListener('click', connectFolderAction);
  els.changePathBtn.addEventListener('click', async () => {
    await renderSettingsPanels();
    els.settingsModal.classList.remove('hidden');
  });
  els.settingsOpenBtn.addEventListener('click', async () => {
    await renderSettingsPanels();
    els.settingsModal.classList.remove('hidden');
  });
  els.closeSettingsModalBtn.addEventListener('click', () => els.settingsModal.classList.add('hidden'));

  // Edit Archive Item Modal
  els.closeEditItemModalBtn.addEventListener('click', () => els.editItemModal.classList.add('hidden'));
  els.cancelEditItemBtn.addEventListener('click', () => els.editItemModal.classList.add('hidden'));
  els.saveEditItemBtn.addEventListener('click', async () => {
    const title = els.editItemTitleInput.value.trim();
    if (!title) {
      showToast('Il titolo non può essere vuoto.');
      els.editItemTitleInput.focus();
      return;
    }
    const existing = state.archive.find((i) => i.Id === state.editingArchiveId);
    if (!existing) return;
    await storage.updateArchiveItem({
      ...existing,
      Title: title,
      Notes: els.editItemNotesInput.value.trim()
    });
    els.editItemModal.classList.add('hidden');
    await reloadData();
    showToast('Scheda aggiornata');
  });

  // Diary Detail Modal
  els.closeDiaryDetailModalBtn.addEventListener('click', () => els.diaryDetailModal.classList.add('hidden'));
  els.cancelDiaryDetailBtn.addEventListener('click', () => els.diaryDetailModal.classList.add('hidden'));
  els.detailBoldBtn.addEventListener('click', () => execEditorCmd(els.detailEditor, 'bold'));
  els.detailItalicBtn.addEventListener('click', () => execEditorCmd(els.detailEditor, 'italic'));
  els.detailBulletBtn.addEventListener('click', () => execEditorCmd(els.detailEditor, 'insertUnorderedList'));
  els.detailCheckBtn.addEventListener('click', () => insertSecondaryCheckBox(els.detailEditor));
  els.detailAttachBtn.addEventListener('click', () => els.detailAttachInput.click());
  els.detailAttachInput.addEventListener('change', async () => {
    const files = Array.from(els.detailAttachInput.files || []);
    els.detailAttachInput.value = '';
    if (!files.length || !state.detailDiaryId) return;
    await storage.addDiaryAttachments(state.detailDiaryId, files);
    await reloadData();
    const updated = state.diary.find((e) => e.Id === state.detailDiaryId);
    if (updated) renderDetailAttachments(updated);
    showToast(`Collegati ${files.length} allegati`);
  });
  els.saveDiaryDetailBtn.addEventListener('click', async () => {
    const text = extractEditorPlainText(els.detailEditor);
    if (!text) {
      showToast('Il testo della nota non può essere vuoto.');
      els.detailEditor.focus();
      return;
    }
    const existing = state.diary.find((e) => e.Id === state.detailDiaryId);
    if (!existing) return;
    await storage.updateDiaryEntry({
      ...existing,
      Text: text,
      RichContentHtml: serializeEditorHtml(els.detailEditor),
      IsDone: els.detailDoneChk.checked
    });
    els.diaryDetailModal.classList.add('hidden');
    await reloadData();
    showToast('Nota del diario aggiornata');
  });

  // Hidden file input for attaching to an existing diary row
  els.entryAttachInput.addEventListener('change', async () => {
    const files = Array.from(els.entryAttachInput.files || []);
    els.entryAttachInput.value = '';
    if (!files.length || !state.pendingAttachEntryId) return;
    try {
      await storage.addDiaryAttachments(state.pendingAttachEntryId, files);
      await reloadData();
      showStatus(files.length === 1 ? `Allegato "${files[0].name}" collegato.` : `Collegati ${files.length} allegati.`);
    } catch (err) {
      showStatus(err?.message || 'Errore allegato diario', true);
    }
    state.pendingAttachEntryId = null;
  });

  // Drag & Drop directly on diary rows
  els.diaryList.addEventListener('dragover', (e) => {
    const row = e.target.closest('.js-diary-row');
    if (!row) return;
    e.preventDefault();
    row.classList.add('drag-over');
  });
  els.diaryList.addEventListener('dragleave', (e) => {
    const row = e.target.closest('.js-diary-row');
    if (row) row.classList.remove('drag-over');
  });
  els.diaryList.addEventListener('drop', async (e) => {
    const row = e.target.closest('.js-diary-row');
    if (!row) return;
    e.preventDefault();
    row.classList.remove('drag-over');
    const files = Array.from(e.dataTransfer?.files || []);
    if (!files.length) return;
    try {
      await storage.addDiaryAttachments(row.dataset.id, files);
      await reloadData();
      showStatus(files.length === 1 ? `Allegato "${files[0].name}" collegato.` : `Collegati ${files.length} allegati.`);
    } catch (err) {
      showStatus(err?.message || 'Errore allegato diario', true);
    }
  });

  // Delegated click & change events across cards, pending banner, and settings
  document.body.addEventListener('click', async (e) => {
    const target = e.target;

    // Global search popup row clicks (open matching item without closing popup)
    if (target.closest('.js-popup-archive-row')) {
      const id = target.closest('.js-popup-archive-row').dataset.id;
      const item = getUnifiedArchive().find((i) => i.Id === id);
      if (item) {
        await openStoredFile(item.StoredRelativePath, item.OriginalFileName, item._isDiaryAttachment);
      }
      return;
    }
    if (target.closest('.js-popup-diary-row')) {
      const id = target.closest('.js-popup-diary-row').dataset.id;
      const entry = state.diary.find((d) => d.Id === id);
      if (entry) {
        openDiaryDetailModal(entry);
      }
      return;
    }
    if (target.closest('.js-popup-notes-row')) {
      const id = target.closest('.js-popup-notes-row').dataset.id;
      const note = state.notes.find((n) => n.Id === id);
      if (note) {
        openNoteDetailModal(note);
      }
      return;
    }

    // Pending actions
    if (target.closest('.js-pending-done')) {
      const btn = target.closest('.js-pending-done');
      const { type, id } = btn.dataset;
      if (type === 'archive') await storage.setArchiveDone(id, true);
      else if (type === 'diary') await storage.setDiaryDone(id, true);
      else if (type === 'notes') await storage.setNoteDone(id, true);
      await reloadData();
      return;
    }
    if (target.closest('.js-pending-open') || target.closest('.pending-row')) {
      const el = target.closest('.js-pending-open') || target.closest('.pending-row');
      await openPendingItem(el.dataset.type, el.dataset.id);
      return;
    }

    // Archive card actions
    if (target.closest('.js-archive-open')) {
      const id = target.closest('.js-archive-open').dataset.id;
      const item = getUnifiedArchive().find((i) => i.Id === id);
      if (item) await openStoredFile(item.StoredRelativePath, item.OriginalFileName, item._isDiaryAttachment);
      return;
    }
    if (target.closest('.js-archive-edit')) {
      const id = target.closest('.js-archive-edit').dataset.id;
      const item = getUnifiedArchive().find((i) => i.Id === id);
      if (!item) return;
      if (item._isDiaryAttachment && item._linkedEntry) {
        openDiaryDetailModal(item._linkedEntry);
      } else {
        openEditArchiveModal(item);
      }
      return;
    }
    if (target.closest('.js-archive-delete')) {
      const id = target.closest('.js-archive-delete').dataset.id;
      const item = getUnifiedArchive().find((i) => i.Id === id);
      if (!item) return;
      if (item._isDiaryAttachment && item._linkedEntry && item._linkedAttachment) {
        if (confirm(`Vuoi eliminare l'allegato "${item._linkedAttachment.FileName}" dalla riga del diario?`)) {
          await storage.removeDiaryAttachment(item._linkedEntry.Id, item._linkedAttachment.Id);
          await reloadData();
          showStatus(`Allegato "${item._linkedAttachment.FileName}" eliminato.`);
        }
      } else if (confirm(`Vuoi eliminare la scheda "${item.Title}"?`)) {
        await storage.deleteArchiveItem(item);
        await reloadData();
        showStatus(`Scheda "${item.Title}" eliminata.`);
      }
      return;
    }
    if (target.closest('#archiveList .item-card') && !target.closest('.done-checkbox, .card-actions')) {
      if (window.getSelection()?.toString()) return;
      const id = target.closest('.item-card').dataset.id;
      const item = getUnifiedArchive().find((i) => i.Id === id);
      if (item) await openStoredFile(item.StoredRelativePath, item.OriginalFileName, item._isDiaryAttachment);
      return;
    }

    // Draft attachment remove
    if (target.closest('.js-remove-draft-att')) {
      const idx = Number(target.closest('.js-remove-draft-att').dataset.idx);
      state.draftAttachments.splice(idx, 1);
      renderDraftAttachments();
      return;
    }

    // Quick Notes chips & table actions
    if (target.closest('.js-quick-note-chip')) {
      const id = target.closest('.js-quick-note-chip').dataset.id;
      const note = state.quickNotes.find((n) => n.Id === id);
      els.quickNotesDropdown.classList.add('hidden');
      els.quickNotesToggleBtn.setAttribute('aria-expanded', 'false');
      if (note) insertTextIntoDiaryEditor(note.Text);
      return;
    }
    if (target.closest('.js-qn-use')) {
      const id = target.closest('.js-qn-use').dataset.id;
      const note = state.quickNotes.find((n) => n.Id === id);
      if (note) {
        insertTextIntoDiaryEditor(note.Text);
        els.quickNotesModal.classList.add('hidden');
      }
      return;
    }
    if (target.closest('.js-qn-edit')) {
      const id = target.closest('.js-qn-edit').dataset.id;
      const note = state.quickNotes.find((n) => n.Id === id);
      if (note) {
        state.editingQuickNoteId = note.Id;
        els.quickNoteInput.value = note.Text;
        els.saveQuickNoteBtn.textContent = '💾 Salva';
        els.cancelQuickNoteEditBtn.classList.remove('hidden');
        els.quickNoteInput.focus();
      }
      return;
    }
    if (target.closest('.js-qn-delete')) {
      const id = target.closest('.js-qn-delete').dataset.id;
      const note = state.quickNotes.find((n) => n.Id === id);
      if (note && confirm(`Vuoi eliminare la nota pronta "${note.Text}"?`)) {
        if (state.editingQuickNoteId === id) resetQuickNoteEditMode();
        await storage.deleteQuickNote(id);
        state.quickNotes = await storage.getQuickNotes();
        renderQuickNotesChips();
        renderQuickNotesTable();
      }
      return;
    }

    // Appunti actions
    if (target.closest('.js-remove-note-draft-att')) {
      const idx = Number(target.closest('.js-remove-note-draft-att').dataset.idx);
      state.noteDraftAttachments.splice(idx, 1);
      renderNoteDraftAttachments();
      return;
    }
    if (target.closest('.js-note-open')) {
      const id = target.closest('.js-note-open').dataset.id;
      const note = state.notes.find((n) => n.Id === id);
      if (note) openNoteDetailModal(note);
      return;
    }
    if (target.closest('.js-note-attach')) {
      state.pendingAttachNoteId = target.closest('.js-note-attach').dataset.id;
      els.noteEntryAttachInput.click();
      return;
    }
    if (target.closest('.js-note-delete')) {
      const id = target.closest('.js-note-delete').dataset.id;
      const note = state.notes.find((n) => n.Id === id);
      if (!note) return;
      const extra = (note.Attachments || []).length > 0
        ? `\n\nVerranno eliminati anche i ${note.Attachments.length} allegati collegati.`
        : '';
      if (confirm(`Vuoi eliminare questo Post-IT?\n\n"${note.Title || note.Text}"${extra}`)) {
        await storage.deleteNote(id);
        await reloadData();
      }
      return;
    }
    if (target.closest('.js-note-att-open')) {
      const btn = target.closest('.js-note-att-open');
      const note = state.notes.find((n) => n.Id === btn.dataset.noteId);
      const att = note?.Attachments?.find((a) => a.Id === btn.dataset.attId);
      if (att) await openStoredFile(att.StoredRelativePath, att.FileName, 'notes');
      return;
    }
    if (target.closest('.js-note-att-remove')) {
      const btn = target.closest('.js-note-att-remove');
      const note = state.notes.find((n) => n.Id === btn.dataset.noteId);
      const att = note?.Attachments?.find((a) => a.Id === btn.dataset.attId);
      if (att && confirm(`Vuoi eliminare l'allegato "${att.FileName}" da questo Post-IT?`)) {
        await storage.removeNoteAttachment(note.Id, att.Id);
        await reloadData();
      }
      return;
    }
    if (target.closest('.js-note-detail-att-remove')) {
      const btn = target.closest('.js-note-detail-att-remove');
      const note = state.notes.find((n) => n.Id === btn.dataset.noteId);
      const att = note?.Attachments?.find((a) => a.Id === btn.dataset.attId);
      if (att && confirm(`Vuoi eliminare l'allegato "${att.FileName}"?`)) {
        await storage.removeNoteAttachment(note.Id, att.Id);
        await reloadData();
        const updated = state.notes.find((n) => n.Id === note.Id);
        if (updated) renderNoteDetailAttachments(updated);
      }
      return;
    }

    // Diary card actions
    if (target.closest('.js-diary-open')) {
      const id = target.closest('.js-diary-open').dataset.id;
      const entry = state.diary.find((d) => d.Id === id);
      if (entry) openDiaryDetailModal(entry);
      return;
    }
    if (target.closest('.js-diary-attach')) {
      state.pendingAttachEntryId = target.closest('.js-diary-attach').dataset.id;
      els.entryAttachInput.click();
      return;
    }
    if (target.closest('.js-diary-delete')) {
      const id = target.closest('.js-diary-delete').dataset.id;
      const entry = state.diary.find((d) => d.Id === id);
      if (!entry) return;
      const extra = (entry.Attachments || []).length > 0
        ? `\n\nVerranno eliminati anche i ${entry.Attachments.length} allegati collegati.`
        : '';
      if (confirm(`Vuoi eliminare questa registrazione dal diario?\n\n"${entry.Text}"${extra}`)) {
        await storage.deleteDiaryEntry(id);
        await reloadData();
      }
      return;
    }
    if (target.closest('.js-diary-att-open')) {
      const btn = target.closest('.js-diary-att-open');
      const entry = state.diary.find((d) => d.Id === btn.dataset.entryId);
      const att = entry?.Attachments?.find((a) => a.Id === btn.dataset.attId);
      if (att) await openStoredFile(att.StoredRelativePath, att.FileName, true);
      return;
    }
    if (target.closest('.js-diary-att-remove')) {
      const btn = target.closest('.js-diary-att-remove');
      const entry = state.diary.find((d) => d.Id === btn.dataset.entryId);
      const att = entry?.Attachments?.find((a) => a.Id === btn.dataset.attId);
      if (att && confirm(`Vuoi eliminare l'allegato "${att.FileName}" da questa riga del diario?`)) {
        await storage.removeDiaryAttachment(entry.Id, att.Id);
        await reloadData();
      }
      return;
    }
    if (target.closest('.js-diary-row') && !target.closest('.done-checkbox, .card-actions, .attach-chip')) {
      if (window.getSelection()?.toString()) return;
      const id = target.closest('.js-diary-row').dataset.id;
      const entry = state.diary.find((d) => d.Id === id);
      if (entry) openDiaryDetailModal(entry);
      return;
    }
    if (target.closest('.js-detail-att-remove')) {
      const btn = target.closest('.js-detail-att-remove');
      const entry = state.diary.find((d) => d.Id === btn.dataset.entryId);
      const att = entry?.Attachments?.find((a) => a.Id === btn.dataset.attId);
      if (att && confirm(`Vuoi eliminare l'allegato "${att.FileName}"?`)) {
        await storage.removeDiaryAttachment(entry.Id, att.Id);
        await reloadData();
        const updated = state.diary.find((d) => d.Id === entry.Id);
        if (updated) renderDetailAttachments(updated);
      }
      return;
    }

    // Settings actions
    if (target.closest('.js-connect-folder')) {
      await connectFolderAction();
    } else if (target.closest('.js-reconnect-folder')) {
      if (await storage.reconnectSavedFolder()) {
        await reloadData();
        await renderSettingsPanels();
        showToast('Cartella riconnessa!');
      }
    } else if (target.closest('.js-disconnect-folder')) {
      await storage.disconnectFolder();
      await reloadData();
      await renderSettingsPanels();
      showToast('Cartella scollegata');
    } else if (target.closest('.js-export-archive')) {
      downloadJsonFile('archivio_memobam.json', state.archive);
    } else if (target.closest('.js-export-diary')) {
      downloadJsonFile('diario_index.json', state.diary);
    } else if (target.closest('.js-export-all')) {
      downloadJsonFile(`memobam_backup_${getTodayDateStr()}.json`, {
        archive: state.archive,
        diary: state.diary
      });
    } else if (target.closest('.js-clear-cache')) {
      if (confirm('Svuotare i dati e i file salvati in cache su questo dispositivo?')) {
        await storage.clearLocalCache();
        await reloadData();
        showToast('Cache locale svuotata');
      }
    }
  });

  // Checkbox changes (Done & Import JSON)
  document.body.addEventListener('change', async (e) => {
    const target = e.target;
    if (target.classList.contains('js-archive-done')) {
      const id = target.dataset.id;
      const item = getUnifiedArchive().find((i) => i.Id === id);
      if (!item) return;
      if (item._isDiaryAttachment && item._linkedEntry) {
        await storage.setDiaryDone(item._linkedEntry.Id, target.checked);
      } else {
        await storage.setArchiveDone(id, target.checked);
      }
      await reloadData();
    } else if (target.classList.contains('js-diary-done')) {
      await storage.setDiaryDone(target.dataset.id, target.checked);
      await reloadData();
    } else if (target.classList.contains('js-note-done')) {
      await storage.setNoteDone(target.dataset.id, target.checked);
      await reloadData();
    } else if (target.classList.contains('diary-inline-check')) {
      const card = target.closest('.item-card');
      const bodyEl = card?.querySelector('.diary-text');
      const ownerType = target.dataset.ownerType || (card?.classList.contains('js-note-row') ? 'notes' : 'diary');
      const ownerId = target.dataset.ownerId || card?.dataset.id;
      if (!bodyEl || !ownerId) return;

      const updatedHtml = serializeEditorHtml(bodyEl);
      const updatedText = extractEditorPlainText(bodyEl);
      if (ownerType === 'notes') {
        const existing = state.notes.find((n) => n.Id === ownerId);
        if (existing) {
          await storage.updateNote({ ...existing, Text: updatedText, RichContentHtml: updatedHtml });
          await reloadData();
        }
      } else {
        const existing = state.diary.find((d) => d.Id === ownerId);
        if (existing) {
          await storage.updateDiaryEntry({ ...existing, Text: updatedText, RichContentHtml: updatedHtml });
          await reloadData();
        }
      }
    } else if (target.classList.contains('js-import-json')) {
      const files = Array.from(target.files || []);
      if (!files.length) return;
      let totalArc = 0;
      let totalDia = 0;
      try {
        for (const file of files) {
          const res = await storage.importJsonText(await file.text(), true);
          totalArc += res.importedArchive;
          totalDia += res.importedDiary;
        }
        await reloadData();
        els.settingsModal.classList.add('hidden');
        showToast(`Importati ${totalArc} documenti e ${totalDia} voci diario`);
      } catch {
        showToast('Errore: file JSON non valido');
      }
      target.value = '';
    }
  });

  // PWA Install prompt
  window.addEventListener('beforeinstallprompt', (e) => {
    e.preventDefault();
    state.deferredInstallPrompt = e;
    els.installBtn.classList.remove('hidden');
  });
  els.installBtn.addEventListener('click', async () => {
    if (!state.deferredInstallPrompt) return;
    state.deferredInstallPrompt.prompt();
    await state.deferredInstallPrompt.userChoice;
    state.deferredInstallPrompt = null;
    els.installBtn.classList.add('hidden');
  });

  // Docking & Auto-Hide controls
  document.querySelectorAll('.dock-btn').forEach((btn) => {
    btn.addEventListener('click', () => {
      els.topbarMenuDropdown.classList.add('hidden');
      state.dockMode = btn.dataset.dock;
      localStorage.setItem('memobam_dock_mode', state.dockMode);
      updateDockButtonsUI();
      if (state.dockMode !== 'normal' && !isStandalonePwaWindow()) {
        showToast('Per ancorare ai bordi Sx/Dx/Basso, apri MemoBam come App PWA installata.');
      }
      applyDockGeometry(false);
    });
  });

  els.autoHideToggleBtn.addEventListener('click', () => {
    state.autoHide = !state.autoHide;
    localStorage.setItem('memobam_auto_hide', String(state.autoHide));
    updateDockButtonsUI();
    if (state.autoHide && state.dockMode === 'normal') {
      showToast('Seleziona ⬅ Sx, ➡ Dx o ⬇ Basso per usare la scomparsa automatica.');
    } else if (!state.autoHide) {
      expandFromAutoHide();
    }
  });

  document.addEventListener('mouseleave', () => scheduleAutoHideCollapse());
  window.addEventListener('blur', () => scheduleAutoHideCollapse());
  document.addEventListener('mouseenter', () => expandFromAutoHide());
  document.addEventListener('dragenter', () => expandFromAutoHide());
  els.collapsedEdgeStrip.addEventListener('mouseenter', () => expandFromAutoHide());
  els.collapsedEdgeStrip.addEventListener('click', () => expandFromAutoHide());
  els.collapsedEdgeStrip.addEventListener('dragenter', (e) => {
    e.preventDefault();
    expandFromAutoHide();
  });
}

// ===== DOCKING & AUTO-HIDE (Standalone PWA Window) =====

const DOCK_SIDE_WIDTH = 500;
const DOCK_BOTTOM_HEIGHT = 380;
const EDGE_PEEK_PX = 28;
let autoHideTimer = null;

function isStandalonePwaWindow() {
  return window.matchMedia?.('(display-mode: standalone)').matches || window.navigator.standalone === true;
}

function applyDockGeometry(collapsed = false) {
  const mode = state.dockMode;
  const s = window.screen;
  const availLeft = s.availLeft ?? 0;
  const availTop = s.availTop ?? 0;
  const availW = s.availWidth || window.innerWidth;
  const availH = s.availHeight || window.innerHeight;

  state.isCollapsedEdge = collapsed && mode !== 'normal' && state.autoHide;
  els.collapsedEdgeStrip.classList.toggle('hidden', !state.isCollapsedEdge);
  els.appShell.classList.toggle('hidden', state.isCollapsedEdge);

  els.collapsedEdgeStrip.classList.remove('edge-left', 'edge-right', 'edge-bottom', 'vertical');
  if (state.isCollapsedEdge) {
    els.collapsedEdgeStrip.classList.add(`edge-${mode}`);
    if (mode === 'left' || mode === 'right') {
      els.collapsedEdgeStrip.classList.add('vertical');
    }
    els.collapsedEdgeLabel.textContent = mode === 'left'
      ? 'MemoBam ▶'
      : (mode === 'right' ? '◀ MemoBam' : '▲ MemoBam');
  }

  if (!isStandalonePwaWindow()) return;

  try {
    if (mode === 'normal') {
      const w = Math.min(1180, availW);
      const h = Math.min(760, availH);
      window.resizeTo(w, h);
      window.moveTo(availLeft + Math.round((availW - w) / 2), availTop + Math.round((availH - h) / 2));
      return;
    }

    if (mode === 'left') {
      const w = Math.min(DOCK_SIDE_WIDTH, availW);
      if (!state.isCollapsedEdge) window.resizeTo(w, availH);
      const actualW = window.outerWidth || w;
      const targetX = state.isCollapsedEdge ? (availLeft - actualW + EDGE_PEEK_PX) : availLeft;
      window.moveTo(targetX, availTop);
    } else if (mode === 'right') {
      const w = Math.min(DOCK_SIDE_WIDTH, availW);
      if (!state.isCollapsedEdge) window.resizeTo(w, availH);
      const actualW = window.outerWidth || w;
      const targetX = state.isCollapsedEdge ? (availLeft + availW - EDGE_PEEK_PX) : (availLeft + availW - actualW);
      window.moveTo(targetX, availTop);
    } else if (mode === 'bottom') {
      const h = Math.min(DOCK_BOTTOM_HEIGHT, availH);
      if (!state.isCollapsedEdge) window.resizeTo(availW, h);
      const actualH = window.outerHeight || h;
      const targetY = state.isCollapsedEdge ? (availTop + availH - EDGE_PEEK_PX) : (availTop + availH - actualH);
      window.moveTo(availLeft, targetY);
    }
  } catch {
    // Ignore if browser restricts moveTo/resizeTo
  }
}

function scheduleAutoHideCollapse() {
  clearTimeout(autoHideTimer);
  if (!state.autoHide || state.dockMode === 'normal' || state.isCollapsedEdge) return;
  autoHideTimer = setTimeout(() => {
    applyDockGeometry(true);
  }, 600);
}

function expandFromAutoHide() {
  clearTimeout(autoHideTimer);
  if (state.isCollapsedEdge) {
    applyDockGeometry(false);
  }
}

function updateDockButtonsUI() {
  document.querySelectorAll('.dock-btn').forEach((b) => {
    b.classList.toggle('active', b.dataset.dock === state.dockMode);
  });
  els.autoHideToggleBtn.classList.toggle('active', state.autoHide);
}

// ===== BOOTSTRAP =====

async function bootstrap() {
  initTheme();
  await storage.init();
  bindEvents();
  updateDockButtonsUI();
  if (state.dockMode !== 'normal' && isStandalonePwaWindow()) {
    applyDockGeometry(false);
  }

  const params = new URLSearchParams(window.location.search);
  const tab = params.get('tab');
  switchSection(tab === 'diary' ? 'diary' : 'archive');

  await reloadData();
  await renderSettingsPanels();

  if ('serviceWorker' in navigator) {
    navigator.serviceWorker.register('./sw.js').catch(() => { });
  }
}

bootstrap();
