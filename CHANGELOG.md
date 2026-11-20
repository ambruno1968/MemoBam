# Changelog

Tutte le modifiche rilevanti a **MemoBam** sono documentate in questo file.
Il formato segue [Keep a Changelog](https://keepachangelog.com/it/1.1.0/) e il progetto adotta il [Semantic Versioning](https://semver.org/lang/it/).

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
