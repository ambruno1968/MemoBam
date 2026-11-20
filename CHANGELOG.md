# Changelog

Tutte le modifiche rilevanti a **MemoBam** sono documentate in questo file.
Il formato segue [Keep a Changelog](https://keepachangelog.com/it/1.1.0/) e il progetto adotta il [Semantic Versioning](https://semver.org/lang/it/).

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
