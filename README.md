# MemoBam

Applicazione desktop Windows (WPF / .NET 10) per archiviare fisicamente documenti e lavori eseguiti tramite Drag & Drop e tenere un diario giornaliero delle attività svolte.

## Funzionalità principali

- **Drag & Drop immediato**: trascina file singoli, multipli o cartelle dalla Scrivania o da Esplora File nella zona dedicata (in modalità *Copia* sicura di default o *Sposta*).
- **Schede con data e ora**: ogni documento archiviato genera una scheda con titolo modificabile, note, dimensione, data (`dd/MM/yyyy`) e ora (`HH:mm:ss`).
- **Ricerca veloce**: cerca istantaneamente per titolo, nome file, note, data o ora, oppure filtra per giorno tramite calendario.
- **Diario Giornaliero**: registra rapidamente le attività fatte durante la giornata nella cartella dedicata `Diario\`, con data e ora automatiche e sincronizzazione in file Markdown (`Diario_YYYY-MM-DD.md`).
- **Ancoraggio e modalità desktop**:
  - `Finestra`, `⬇ Basso`, `⬅ Sx`, `➡ Dx` (con modalità `👁️ Scomparsa` automatica sui bordi).
  - `📌 Sfondo`: lascia visibile e semitrasparente il solo riquadro Drag & Drop ancorato sullo sfondo e apre la finestra completa al passaggio del mouse o al trascinamento di un file.
- **Percorso personalizzabile**: cartella principale configurabile tramite il pulsante `Cambia...` (salvata in `%LocalAppData%\MemoBam\settings.json`).

## Requisiti e compilazione

- Windows 10 / 11
- [.NET 10 SDK](https://dotnet.microsoft.com/)

```powershell
dotnet build -c Release
```

L'eseguibile viene generato in `bin/Release/net10.0-windows/MemoBam.exe`.
