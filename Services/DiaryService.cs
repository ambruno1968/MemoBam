using System.IO;
using System.Text;
using System.Text.Json;
using MemoBam.Models;

namespace MemoBam.Services;

/// <summary>
/// Manages the daily work diary in a dedicated "Diario" subfolder under MemoBam,
/// automatically stamping date/time, linking attachments, and syncing daily Markdown files.
/// </summary>
public class DiaryService
{
    private const string DiaryFolderName = "Diario";
    private const string AttachmentsFolderName = "Allegati";
    private const string IndexFileName = "diario_index.json";
    private const string QuickNotesFileName = "note_pronte.json";

    private static readonly (string Bg, string Border)[] PastelPalette =
    [
        ("#EEF2FF", "#C7D2FE"), // Pastel Indigo
        ("#ECFDF5", "#A7F3D0"), // Pastel Mint
        ("#F5F3FF", "#DDD6FE"), // Pastel Lavender
        ("#FFF1F2", "#FECDD3"), // Pastel Rose
        ("#F0F9FF", "#BAE6FD")  // Pastel Sky
    ];

    private static readonly (string Bg, string Border)[] DarkPastelPalette =
    [
        ("#1E1B4B", "#3730A3"), // Deep Pastel Indigo
        ("#064E3B", "#047857"), // Deep Pastel Mint
        ("#2E1065", "#6D28D9"), // Deep Pastel Lavender
        ("#4C0519", "#BE123C"), // Deep Pastel Rose
        ("#0C4A6E", "#0369A1")  // Deep Pastel Sky
    ];

    public bool IsDarkTheme { get; set; }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly List<DiaryEntry> _entries = [];
    private readonly List<QuickNote> _quickNotes = [];

    public string DiaryDirectory { get; }
    private string AttachmentsRootDirectory => Path.Combine(DiaryDirectory, AttachmentsFolderName);
    private string IndexFilePath => Path.Combine(DiaryDirectory, IndexFileName);
    private string QuickNotesFilePath => Path.Combine(DiaryDirectory, QuickNotesFileName);

    public DiaryService(string? baseDirectory = null)
    {
        string root = string.IsNullOrWhiteSpace(baseDirectory)
            ? ArchiveService.FixedArchivePath
            : baseDirectory;

        DiaryDirectory = Path.Combine(root, DiaryFolderName);
        Directory.CreateDirectory(DiaryDirectory);
        LoadIndex();
        LoadQuickNotes();
    }

    public DiaryEntry AddEntry(string text, DateTime? targetDate = null, string? richContentXaml = null)
    {
        var now = DateTime.Now;
        var stamp = targetDate.HasValue && targetDate.Value.Date != now.Date
            ? targetDate.Value.Date.Add(now.TimeOfDay)
            : now;

        var entry = new DiaryEntry
        {
            Timestamp = stamp,
            Text = text.Trim(),
            RichContentXaml = richContentXaml
        };

        _entries.Add(entry);
        SaveIndex();
        SyncDailyMarkdownFile(stamp.Date);
        return entry;
    }

    public void UpdateEntry(DiaryEntry updated)
    {
        var existing = _entries.FirstOrDefault(e => e.Id == updated.Id);
        if (existing is null) return;

        existing.Text = updated.Text.Trim();
        existing.RichContentXaml = updated.RichContentXaml;
        existing.IsDone = updated.IsDone;
        SaveIndex();
        SyncDailyMarkdownFile(existing.Timestamp.Date);
    }

    public void SetDone(DiaryEntry entry, bool isDone)
    {
        var existing = _entries.FirstOrDefault(e => e.Id == entry.Id);
        if (existing is null) return;

        existing.IsDone = isDone;
        SaveIndex();
        SyncDailyMarkdownFile(existing.Timestamp.Date);
    }

    public List<DiaryAttachment> AddAttachments(DiaryEntry entry, IEnumerable<string> sourcePaths, bool moveFiles = false)
    {
        var existing = _entries.FirstOrDefault(e => e.Id == entry.Id);
        if (existing is null) return [];

        var added = new List<DiaryAttachment>();
        var validPaths = sourcePaths.Where(p => File.Exists(p) || Directory.Exists(p)).ToList();
        if (validPaths.Count == 0) return added;

        var dayFolder = Path.Combine(AttachmentsRootDirectory, existing.Timestamp.ToString("yyyy-MM-dd"));
        Directory.CreateDirectory(dayFolder);

        foreach (var sourcePath in validPaths)
        {
            var now = DateTime.Now;
            bool isDir = Directory.Exists(sourcePath);
            string originalName = Path.GetFileName(sourcePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            string targetPath = GetUniqueTargetPath(dayFolder, originalName, isDir, now);

            if (isDir)
            {
                if (moveFiles) MoveDirectory(sourcePath, targetPath);
                else CopyDirectory(sourcePath, targetPath);
            }
            else
            {
                if (moveFiles) File.Move(sourcePath, targetPath);
                else File.Copy(sourcePath, targetPath);
            }

            var attachment = new DiaryAttachment
            {
                FileName = originalName,
                StoredRelativePath = Path.GetRelativePath(DiaryDirectory, targetPath),
                AddedAt = now,
                IsDirectory = isDir
            };

            existing.Attachments.Add(attachment);
            added.Add(attachment);
        }

        if (added.Count > 0)
        {
            SaveIndex();
            SyncDailyMarkdownFile(existing.Timestamp.Date);
        }

        return added;
    }

    public void RemoveAttachment(DiaryEntry entry, DiaryAttachment attachment)
    {
        var existing = _entries.FirstOrDefault(e => e.Id == entry.Id);
        if (existing is null) return;

        var target = existing.Attachments.FirstOrDefault(a => a.Id == attachment.Id);
        if (target is null) return;

        DeletePhysicalAttachment(target);
        existing.Attachments.Remove(target);
        SaveIndex();
        SyncDailyMarkdownFile(existing.Timestamp.Date);
    }

    public string GetAttachmentFullPath(DiaryAttachment attachment) =>
        Path.Combine(DiaryDirectory, attachment.StoredRelativePath);

    public List<DiaryEntry> GetPendingEntries() =>
        _entries.Where(e => !e.IsDone).OrderByDescending(e => e.Timestamp).ToList();

    public void DeleteEntry(DiaryEntry entry)
    {
        var existing = _entries.FirstOrDefault(e => e.Id == entry.Id);
        if (existing is null) return;

        foreach (var attachment in existing.Attachments)
        {
            DeletePhysicalAttachment(attachment);
        }

        var date = existing.Timestamp.Date;
        _entries.Remove(existing);
        SaveIndex();
        SyncDailyMarkdownFile(date);
    }

    public int GetTodayCount() =>
        _entries.Count(e => e.Timestamp.Date == DateTime.Today);

    public List<(DiaryEntry Entry, DiaryAttachment Attachment)> GetMatchingAttachments(string? query, DateTime? dateFilter)
    {
        var matches = new List<(DiaryEntry Entry, DiaryAttachment Attachment)>();
        var terms = string.IsNullOrWhiteSpace(query)
            ? []
            : query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var entry in _entries)
        {
            if (entry.Attachments.Count == 0) continue;

            foreach (var att in entry.Attachments)
            {
                if (dateFilter.HasValue && att.AddedAt.Date != dateFilter.Value.Date && entry.Timestamp.Date != dateFilter.Value.Date)
                {
                    continue;
                }

                bool termMatch = true;
                foreach (var term in terms)
                {
                    if (!att.FileName.Contains(term, StringComparison.OrdinalIgnoreCase)
                        && !entry.Text.Contains(term, StringComparison.OrdinalIgnoreCase)
                        && !att.AddedAt.ToString("dd/MM/yyyy").Contains(term, StringComparison.OrdinalIgnoreCase)
                        && !att.AddedAt.ToString("yyyy-MM-dd").Contains(term, StringComparison.OrdinalIgnoreCase)
                        && !att.AddedAt.ToString("HH:mm:ss").Contains(term, StringComparison.OrdinalIgnoreCase))
                    {
                        termMatch = false;
                        break;
                    }
                }

                if (termMatch)
                {
                    matches.Add((entry, att));
                }
            }
        }

        return matches;
    }

    public List<DiaryEntry> GetEntries(DateTime? dateFilter, string? query = null)
    {
        IEnumerable<DiaryEntry> results = _entries;

        if (dateFilter.HasValue)
        {
            var day = dateFilter.Value.Date;
            results = results.Where(e => e.Timestamp.Date == day);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var term in terms)
            {
                results = results.Where(e =>
                    e.Text.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || e.DateFormatted.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || e.TimeFormatted.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || e.Attachments.Any(a => a.FileName.Contains(term, StringComparison.OrdinalIgnoreCase)));
            }
        }

        var palette = IsDarkTheme ? DarkPastelPalette : PastelPalette;
        var ordered = results.OrderByDescending(e => e.Timestamp).ToList();
        for (int i = 0; i < ordered.Count; i++)
        {
            var (bg, border) = palette[i % palette.Length];
            ordered[i].RowBackgroundHex = bg;
            ordered[i].RowBorderHex = border;
        }

        return ordered;
    }

    private void DeletePhysicalAttachment(DiaryAttachment attachment)
    {
        var fullPath = GetAttachmentFullPath(attachment);
        if (attachment.IsDirectory && Directory.Exists(fullPath))
        {
            Directory.Delete(fullPath, recursive: true);
        }
        else if (!attachment.IsDirectory && File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    private static string GetUniqueTargetPath(string targetFolder, string originalName, bool isDir, DateTime timestamp)
    {
        string candidate = Path.Combine(targetFolder, originalName);
        if (!File.Exists(candidate) && !Directory.Exists(candidate))
        {
            return candidate;
        }

        string baseName = isDir ? originalName : Path.GetFileNameWithoutExtension(originalName);
        string ext = isDir ? string.Empty : Path.GetExtension(originalName);
        string stamp = timestamp.ToString("yyyyMMdd_HHmmss");

        candidate = Path.Combine(targetFolder, $"{baseName}_{stamp}{ext}");
        int counter = 1;
        while (File.Exists(candidate) || Directory.Exists(candidate))
        {
            candidate = Path.Combine(targetFolder, $"{baseName}_{stamp}_{counter}{ext}");
            counter++;
        }

        return candidate;
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)));
        }
        foreach (var subDir in Directory.GetDirectories(sourceDir))
        {
            CopyDirectory(subDir, Path.Combine(destDir, Path.GetFileName(subDir)));
        }
    }

    private static void MoveDirectory(string sourceDir, string destDir)
    {
        var sourceRoot = Path.GetPathRoot(Path.GetFullPath(sourceDir));
        var destRoot = Path.GetPathRoot(Path.GetFullPath(destDir));

        if (string.Equals(sourceRoot, destRoot, StringComparison.OrdinalIgnoreCase))
        {
            Directory.Move(sourceDir, destDir);
        }
        else
        {
            CopyDirectory(sourceDir, destDir);
            Directory.Delete(sourceDir, recursive: true);
        }
    }

    private void SyncDailyMarkdownFile(DateTime date)
    {
        Directory.CreateDirectory(DiaryDirectory);
        string filePath = Path.Combine(DiaryDirectory, $"Diario_{date:yyyy-MM-dd}.md");

        var dayEntries = _entries
            .Where(e => e.Timestamp.Date == date.Date)
            .OrderBy(e => e.Timestamp)
            .ToList();

        if (dayEntries.Count == 0)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"# Diario del {date:dd/MM/yyyy}");
        sb.AppendLine();
        foreach (var entry in dayEntries)
        {
            string check = entry.IsDone ? "[x]" : "[ ]";
            sb.AppendLine($"- {check} **[{entry.TimeFormatted}]** {entry.Text}");
            foreach (var att in entry.Attachments)
            {
                sb.AppendLine($"  - Allegato: `{att.FileName}` ({att.StoredRelativePath})");
            }
        }

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }

    private void LoadIndex()
    {
        _entries.Clear();
        if (!File.Exists(IndexFilePath)) return;

        try
        {
            string json = File.ReadAllText(IndexFilePath);
            var loaded = JsonSerializer.Deserialize<List<DiaryEntry>>(json, JsonOptions);
            if (loaded is not null)
            {
                _entries.AddRange(loaded);
            }
        }
        catch
        {
            // Avoid crashing on startup if index is temporarily unreadable.
        }
    }

    private void SaveIndex()
    {
        Directory.CreateDirectory(DiaryDirectory);
        string tempPath = IndexFilePath + ".tmp";
        string json = JsonSerializer.Serialize(_entries, JsonOptions);
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, IndexFilePath, overwrite: true);
    }

    #region Quick Notes ("Note Pronte")

    public IReadOnlyList<QuickNote> GetQuickNotes() =>
        _quickNotes.OrderBy(n => n.CreatedAt).ToList();

    public QuickNote AddQuickNote(string text)
    {
        var note = new QuickNote
        {
            Text = text.Trim(),
            CreatedAt = DateTime.Now
        };
        _quickNotes.Add(note);
        SaveQuickNotes();
        return note;
    }

    public void UpdateQuickNote(QuickNote updated)
    {
        var existing = _quickNotes.FirstOrDefault(n => n.Id == updated.Id);
        if (existing is null) return;
        existing.Text = updated.Text.Trim();
        SaveQuickNotes();
    }

    public void DeleteQuickNote(QuickNote note)
    {
        var existing = _quickNotes.FirstOrDefault(n => n.Id == note.Id);
        if (existing is null) return;
        _quickNotes.Remove(existing);
        SaveQuickNotes();
    }

    private void LoadQuickNotes()
    {
        _quickNotes.Clear();
        if (!File.Exists(QuickNotesFilePath))
        {
            _quickNotes.Add(new QuickNote
            {
                Text = "Listino cavi aggiornato",
                CreatedAt = DateTime.Now
            });
            SaveQuickNotes();
            return;
        }

        try
        {
            string json = File.ReadAllText(QuickNotesFilePath);
            var loaded = JsonSerializer.Deserialize<List<QuickNote>>(json, JsonOptions);
            if (loaded is not null)
            {
                _quickNotes.AddRange(loaded);
            }
        }
        catch
        {
            // Keep empty list if quick notes file is unreadable.
        }
    }

    private void SaveQuickNotes()
    {
        Directory.CreateDirectory(DiaryDirectory);
        string tempPath = QuickNotesFilePath + ".tmp";
        string json = JsonSerializer.Serialize(_quickNotes, JsonOptions);
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, QuickNotesFilePath, overwrite: true);
    }

    #endregion
}
