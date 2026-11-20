using System.IO;
using System.Text;
using System.Text.Json;
using MemoBam.Models;

namespace MemoBam.Services;

/// <summary>
/// Manages the daily work diary in a dedicated "Diario" subfolder under MemoBam,
/// automatically stamping date/time and syncing human-readable daily Markdown files.
/// </summary>
public class DiaryService
{
    private const string DiaryFolderName = "Diario";
    private const string IndexFileName = "diario_index.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly List<DiaryEntry> _entries = [];

    public string DiaryDirectory { get; }
    private string IndexFilePath => Path.Combine(DiaryDirectory, IndexFileName);

    public DiaryService(string? baseDirectory = null)
    {
        string root = string.IsNullOrWhiteSpace(baseDirectory)
            ? ArchiveService.FixedArchivePath
            : baseDirectory;

        DiaryDirectory = Path.Combine(root, DiaryFolderName);
        Directory.CreateDirectory(DiaryDirectory);
        LoadIndex();
    }

    public DiaryEntry AddEntry(string text, DateTime? targetDate = null)
    {
        var now = DateTime.Now;
        var stamp = targetDate.HasValue && targetDate.Value.Date != now.Date
            ? targetDate.Value.Date.Add(now.TimeOfDay)
            : now;

        var entry = new DiaryEntry
        {
            Timestamp = stamp,
            Text = text.Trim()
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

    public List<DiaryEntry> GetPendingEntries() =>
        _entries.Where(e => !e.IsDone).OrderByDescending(e => e.Timestamp).ToList();

    public void DeleteEntry(DiaryEntry entry)
    {
        var existing = _entries.FirstOrDefault(e => e.Id == entry.Id);
        if (existing is null) return;

        var date = existing.Timestamp.Date;
        _entries.Remove(existing);
        SaveIndex();
        SyncDailyMarkdownFile(date);
    }

    public int GetTodayCount() =>
        _entries.Count(e => e.Timestamp.Date == DateTime.Today);

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
                    || e.TimeFormatted.Contains(term, StringComparison.OrdinalIgnoreCase));
            }
        }

        return results.OrderByDescending(e => e.Timestamp).ToList();
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
}
