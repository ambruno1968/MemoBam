# Changelog

Tutte le modifiche rilevanti a **MemoBam** sono documentate in questo file.
Il formato segue [Keep a Changelog](https://keepachangelog.com/it/1.1.0/) e il progetto adotta il [Semantic Versioning](https://semver.org/lang/it/).

## [1.10.1] - 2026-11-23

### Risolto
- **Pulsante `✏️ Modifica / Categoria` in anteprima file**: preservati `ownerType` e `ownerId` in `currentPreview` dopo `renderFilePreview()` in `js/app.js` così il clic apre correttamente la modale di modifica.

## [1.10.0] - 2026-11-23

### Aggiunto
- **Gestione manuale delle Categorie**: nuova modale `🏷️ Categorie` per creare, modificare ed eliminare categorie personalizzate con nome, colore e assegnazione selettiva alle funzioni (`📁 Archivio`, `📓 Diario`, `📝 Post-IT`).
- **Categorizzazione e filtri**: selettori di categoria in creazione e modifica per Archivio, Diario e Post-IT, filtri dedicati per sezione, badge colorati sulle schede e ricerca globale estesa al nome categoria (`categorie.json` / IndexedDB `categories`).
- **Pulsante `✏️ Modifica / Categoria` in anteprima file**: possibilità di passare direttamente dall'anteprima documento (`#filePreviewModal`) alla modifica di titolo, note e categoria.

### Risolto
- **Apertura Post-IT al clic sulla scheda**: cliccando su una riga `Post-IT` (`.js-note-row`) ora si apre la finestra di dettaglio, in modo coerente con il Diario.

## [1.9.2] - 2026-11-23

### Risolto
- **Errore `Cannot read properties of null (reading 'transaction')` su Collega Cartella**: reso sicuro `idbTx()` quando `this.db` è `null` e garantito il collegamento della cartella in `connectFolder()` anche se IndexedDB non è disponibile al momento.

## [1.9.1] - 2026-11-23

### Risolto
- **Blocco avvio su aggiornamento IndexedDB**: eseguito `bindEvents()` prima di `storage.init()` in `js/app.js` con gestione `try/catch`, e aggiunti `onversionchange`, `onblocked` e timeout di sicurezza in `openDb()` (`js/storage.js`) così menù, tema, cartella e impostazioni risultano sempre reattivi anche con più schede aperte.

## [1.9.0] - 2026-11-23

### Aggiunto
- **Nuova sezione `📝 Post-IT`**: tab dedicata con titolo obbligatorio, editor ricco, allegati (drag & drop e modale dettaglio), spunta `Fatto`, banner `Da terminare` e terza colonna dedicata nel popup di ricerca parallela (`Appunti/appunti_index.json` e IndexedDB `notes`).

### Migliorato
- **Note pronte compatte**: spostate nel menu a tendina **`⚡ Note pronte ▾`** nella toolbar del Diario (nascoste di default).
- **Caselle di controllo reali (`☑ Spunta`)**: sostituiti i segnaposto `[ ]` / `[x]` con vere `<input type="checkbox">` interattive negli editor e nelle schede salvate (con salvataggio immediato al clic).
- **Scomparsa laterale al bordo**: in finestra PWA installata, la modalità `👁️ Scomparsa` fa scorrere la finestra lateralmente lasciando esposta una striscia di `28 px` sul bordo.

## [1.8.0] - 2026-11-23

### Aggiunto
- **Menu laterale sinistro (`☰`) e ancoraggio finestra PWA**: aggiunti `🪟 Finestra`, `⬅ Ancora a Sx`, `➡ Ancora a Dx`, `⬇ Ancora in Basso` e `👁️ Scomparsa al bordo` (con riapertura automatica al passaggio del mouse o drag & drop in finestra PWA installata).
- **Popup Credits & QR Code PWA** (`#creditsModal`): spostati i credits (`by Bruno Astemio • 📱 PWA`) nel menu sinistro con QR Code offline ([qrcode-memobam.png](icons/qrcode-memobam.png)) e pulsante `📋 Copia link`.

### Migliorato
- **Palette temi**: tema scuro con base **nero** (`#000000`) e tema chiaro con base **blu chiarissimo** (`#EBF4FF`).
- **Badge cartella**: rimossa l'etichetta superflua `(clicca 📁 per riconnettere)` mantenendo la riconnessione in 1 clic.

## [1.7.0] - 2026-11-23

### Aggiunto
- **Anteprima integrata per Archivio e Allegati Diario** (`js/preview.js`): finestra dedicata per visualizzare inline **PDF**, **Word** (`.docx`), **Excel / CSV** (`.xlsx`, `.xlsm`, `.csv` con schede multi-foglio), **TXT/JSON/MD/XML**, **HTML** e **Immagini** senza dipendenze esterne.
- **Azioni dalla finestra di anteprima**: pulsanti **`🚀 Apri con app`** e **`💾 Salva con nome`** (`showSaveFilePicker` con scelta manuale di percorso e nome, più fallback download).
- **Persistenza ultima cartella collegata**: memorizzazione automatica dell'ultimo nome/handle cartella (`savedFolderName`) con riconnessione in 1 clic dalla barra superiore.

### Migliorato
- **Popup Ricerca Parallela persistente**: il popup rimane aperto sopra l'app (`z-index: 50`) e sotto dettaglio Diario (`z-index: 60`) / anteprima Documento (`z-index: 70`), chiudendosi solo su comando esplicito dell'utente.

## [1.6.0] - 2026-11-23

### Aggiunto
- **PWA completa (lettura e scrittura)**: migrato il progetto principale su PWA con supporto File System Access API (`readwrite`) e IndexedDB offline, eliminando il progetto desktop Windows.
- **Ricerca globale superiore con popup parallelo**: barra di ricerca unificata in alto che apre un popup con i risultati affiancati di **Archivio Documenti** e **Diario / Agenda** (clic per aprire direttamente la corrispondenza, pulsante `Chiudi` dedicato).
- **Note pronte per il Diario**: gestione su tabella separata (`Diario/note_pronte.json` e IndexedDB `quickNotes`) con inserimento rapido tramite chip (es. `"Listino cavi aggiornato"`).
- **Service Worker network-first (`memobam-pwa-v1.6.0`)**: aggiornamento automatico immediato quando online con fallback offline.

## [1.5.0] - 2026-11-20

### Aggiunto
- **QR Code PWA & Credit in barra superiore**: aggiunto il pulsante `by Bruno Astemio • 📱 PWA` con anteprima QR cliccabile che apre la finestra ingrandita per accedere rapidamente alla PWA di sola lettura (`https://ambruno1968.github.io/MemoBamPWA/`).
- **Controllo aggiornamenti automatico all'avvio**: `UpdateService` verifica le release su GitHub all'avvio dell'applicazione e, se disponibile una nuova versione, propone il download, la sostituzione dei file e il riavvio automatico.
- **Distribuzione organizzata**: aggiunti `publish-dist.ps1` (genera pacchetto `dist/MemoBam-vX.Y.Z-win-x64.zip`) e il workflow GitHub Actions `.github/workflows/release.yml` per il rilascio automatico su tag `v*`.

## [1.4.0] - 2026-11-20

### Aggiunto
- **Filtro allegati Diario in Archivio Documenti**: aggiunta la spunta **`Escludi allegati provenienti dal Diario`** (attiva di default) nella barra di ricerca di **Archivio Documenti**, per scegliere rapidamente se mostrare o nascondere i documenti collegati alle righe del Diario.

## [1.3.0] - 2026-11-20

### Aggiunto
- **Allegati del Diario visibili nell'Archivio Documenti**: gli allegati collegati alle righe del Diario compaiono ora anche in **Archivio Documenti** (e nella ricerca) con il badge **`📓 Diario`** e il riferimento alla riga di appartenenza.
- **Aggancio allegati alla nota in stesura**: trascinando file nella zona Drag & Drop mentre stai scrivendo una nuova nota del Diario, gli allegati vengono agganciati alla bozza corrente senza creare righe extra e salvati insieme alla nota al clic su **`💾 Salva`**.

## [1.2.0] - 2026-11-20

### Aggiunto
- **Note ricche e formattazione nel Diario**: campo note multi-riga con tasto `💾 Salva`, finestra dedicata di lettura/modifica (`DiaryDetailWindow`), barra di formattazione (`B` grassetto, `I` corsivo, `• Elenco`, `☑ Spunta` secondaria non ripetibile sulla stessa riga).
- **Allegati collegati alle righe del Diario**: drag & drop diretto sulle righe o pulsante `📎 Allega` con salvataggio in `Diario\Allegati\`, ricerca per allegato ed eliminazione sincronizzata.
- **Colori pastello alternati** sulle righe del Diario (con palette dedicata per tema chiaro e tema scuro).
- **Tema Chiaro / Scuro** (`🌙 Scuro` / `☀️ Chiaro`) salvato nelle impostazioni, con nuova palette Indigo/Slate moderna e sfondo filigrana leggero e trasparente.
- **Icona ufficiale MemoBam** multi-risoluzione (`Assets\memobam.ico`).

### Risolto
- Supporto multi-monitor completo per la modalità **Sfondo** (senza riposizionamento forzato sullo schermo principale) e spostamento libero del mini-riquadro Drag & Drop.

## [1.1.0] - 2026-11-20

### Aggiunto
- **Spunta "Fatto" su ogni riga**: aggiunta una casella di spunta (attiva di default) sia nelle schede dell'**Archivio Documenti** sia nelle righe del **Diario Giornaliero** (`- [x]` / `- [ ]` nei file Markdown). Togliendo la spunta, la riga viene evidenziata in ambra per ricordare di completarla.
- **Alert in alto per elementi non spuntati**: barra di avviso dedicata che mostra il conteggio di documenti e attività da terminare, con elenco espandibile e pulsanti rapidi **Apri** (apre il documento o porta al giorno del diario) e **✓ Fatto**.

## [1.0.0] - 2026-11-20

### Aggiunto
- **Archivio Documenti Drag & Drop**: trascinamento di file singoli, multipli o cartelle dalla Scrivania o da Esplora File con creazione automatica della scheda con data (`dd/MM/yyyy`) e ora (`HH:mm:ss`).
- **Organizzazione fisica su disco**: salvataggio ordinato in sottocartelle mensili (`YYYY-MM`) con indice `archivio_memobam.json` e gestione automatica dei nomi duplicati.
- **Ricerca veloce**: filtro istantaneo su titolo, nome file originale, note, data e ora, più filtro calendario (`DatePicker`).
- **Gestione schede**: apertura diretta con doppio clic o pulsante `Apri`, selezione in Esplora File (`Cartella`), modifica di titolo e note (`EditItemWindow`) ed eliminazione con conferma.
- **Diario Giornaliero**: sezione dedicata (`Diario\`) con selezione automatica della data odierna, registrazione rapida con orario automatico (`HH:mm:ss`), ricerca multi-giorno ed esportazione automatica in file Markdown giornalieri (`Diario_YYYY-MM-DD.md`).
- **Modalità di ancoraggio e finestra**:
  - `Finestra`: finestra ridimensionabile con barra superiore personalizzata (`WindowChrome`).
  - `⬇ Basso`, `⬅ Sx`, `➡ Dx`: ancoraggio ai bordi dello schermo allineato a `SystemParameters.WorkArea`.
  - `👁️ Scomparsa`: barra laterale/inferiore a scomparsa automatica (attiva di default su `➡ Dx`), espandibile al passaggio del mouse o durante il drag & drop.
  - `📌 Sfondo`: mantiene ancorato e semitrasparente il solo riquadro Drag & Drop sullo sfondo, aprendo la finestra completa al suo utilizzo.
- **Percorso personalizzabile**: pulsante `Cambia...` per scegliere la cartella principale (salvata in `%LocalAppData%\MemoBam\settings.json`), con percorso predefinito su OneDrive.
