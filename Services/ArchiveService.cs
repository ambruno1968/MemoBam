using System.IO;
using System.Text.Json;
using MemoBam.Models;

namespace MemoBam.Services;

/// <summary>
/// Manages physical archiving of documents into the fixed MemoBam folder
/// and persistence/search of archive cards ("schede").
/// </summary>
public class ArchiveService
{
    public const string FixedArchivePath = @"C:\Users\bruno.astemio\OneDrive - Comoli Ferrari & c. spa\MemoBam";
    private const string IndexFileName = "archivio_memobam.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly List<ArchiveItem> _items = [];

    public string BaseDirectory { get; }
    private string IndexFilePath => Path.Combine(BaseDirectory, IndexFileName);

    public ArchiveService(string? baseDirectory = null)
    {
        BaseDirectory = string.IsNullOrWhiteSpace(baseDirectory) ? FixedArchivePath : baseDirectory;
        Directory.CreateDirectory(BaseDirectory);
        LoadIndex();
    }

    public IReadOnlyList<ArchiveItem> GetAllItems() =>
        _items.OrderByDescending(i => i.ArchivedAt).ToList();

    public string GetFullPath(ArchiveItem item) =>
        Path.Combine(BaseDirectory, item.StoredRelativePath);

    /// <summary>
    /// Archives one or more files/directories into the fixed folder and creates cards with current date and time.
    /// </summary>
    public List<ArchiveItem> AddPaths(IEnumerable<string> sourcePaths, bool moveFiles, string? customTitle = null, string? notes = null)
    {
        Directory.CreateDirectory(BaseDirectory);
        var added = new List<ArchiveItem>();
        var validPaths = sourcePaths.Where(p => File.Exists(p) || Directory.Exists(p)).ToList();

        foreach (var sourcePath in validPaths)
        {
            var now = DateTime.Now;
            var monthFolderName = now.ToString("yyyy-MM");
            var targetFolder = Path.Combine(BaseDirectory, monthFolderName);
            Directory.CreateDirectory(targetFolder);

            bool isDir = Directory.Exists(sourcePath);
            string originalName = Path.GetFileName(sourcePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            string targetPath = GetUniqueTargetPath(targetFolder, originalName, isDir, now);

            long sizeBytes = 0;
            if (isDir)
            {
                if (moveFiles)
                {
                    MoveDirectory(sourcePath, targetPath);
                }
                else
                {
                    CopyDirectory(sourcePath, targetPath);
                }
            }
            else
            {
                sizeBytes = new FileInfo(sourcePath).Length;
                if (moveFiles)
                {
                    File.Move(sourcePath, targetPath);
                }
                else
                {
                    File.Copy(sourcePath, targetPath);
                }
            }

            string defaultTitle = isDir
                ? originalName
                : Path.GetFileNameWithoutExtension(originalName);

            string title = validPaths.Count == 1 && !string.IsNullOrWhiteSpace(customTitle)
                ? customTitle.Trim()
                : defaultTitle;

            var item = new ArchiveItem
            {
                Title = title,
                OriginalFileName = originalName,
                StoredRelativePath = Path.GetRelativePath(BaseDirectory, targetPath),
                ArchivedAt = now,
                Notes = notes?.Trim() ?? string.Empty,
                FileSizeBytes = sizeBytes,
                IsDirectory = isDir
            };

            _items.Add(item);
            added.Add(item);
        }

        if (added.Count > 0)
        {
            SaveIndex();
        }

        return added;
    }

    public void UpdateItem(ArchiveItem updated)
    {
        var existing = _items.FirstOrDefault(i => i.Id == updated.Id);
        if (existing is null) return;

        existing.Title = updated.Title.Trim();
        existing.Notes = updated.Notes.Trim();
        SaveIndex();
    }

    public void DeleteItem(ArchiveItem item, bool deletePhysicalFile)
    {
        var existing = _items.FirstOrDefault(i => i.Id == item.Id);
        if (existing is null) return;

        if (deletePhysicalFile)
        {
            var fullPath = GetFullPath(existing);
            if (existing.IsDirectory && Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, recursive: true);
            }
            else if (!existing.IsDirectory && File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }

        _items.Remove(existing);
        SaveIndex();
    }

    /// <summary>
    /// Fast search across title, original filename, notes, date (dd/MM/yyyy, yyyy-MM-dd), and time (HH:mm:ss).
    /// Supports space-separated terms (AND logic).
    /// </summary>
    public List<ArchiveItem> Search(string? query, DateTime? dateFilter = null)
    {
        IEnumerable<ArchiveItem> results = _items;

        if (dateFilter.HasValue)
        {
            var targetDate = dateFilter.Value.Date;
            results = results.Where(i => i.ArchivedAt.Date == targetDate);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var term in terms)
            {
                results = results.Where(i => MatchesTerm(i, term));
            }
        }

        return results.OrderByDescending(i => i.ArchivedAt).ToList();
    }

    private static bool MatchesTerm(ArchiveItem item, string term)
    {
        return item.Title.Contains(term, StringComparison.OrdinalIgnoreCase)
            || item.OriginalFileName.Contains(term, StringComparison.OrdinalIgnoreCase)
            || item.Notes.Contains(term, StringComparison.OrdinalIgnoreCase)
            || item.DateFormatted.Contains(term, StringComparison.OrdinalIgnoreCase)
            || item.ArchivedAt.ToString("yyyy-MM-dd").Contains(term, StringComparison.OrdinalIgnoreCase)
            || item.TimeFormatted.Contains(term, StringComparison.OrdinalIgnoreCase);
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

    private void LoadIndex()
    {
        _items.Clear();
        if (!File.Exists(IndexFilePath)) return;

        try
        {
            string json = File.ReadAllText(IndexFilePath);
            var loaded = JsonSerializer.Deserialize<List<ArchiveItem>>(json, JsonOptions);
            if (loaded is not null)
            {
                _items.AddRange(loaded);
            }
        }
        catch
        {
            // Keep empty list if index is unreadable; avoid crashing on startup.
        }
    }

    private void SaveIndex()
    {
        Directory.CreateDirectory(BaseDirectory);
        string tempPath = IndexFilePath + ".tmp";
        string json = JsonSerializer.Serialize(_items, JsonOptions);
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, IndexFilePath, overwrite: true);
    }
}
