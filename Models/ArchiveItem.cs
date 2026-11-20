using System.IO;
using System.Text.Json.Serialization;

namespace MemoBam.Models;

/// <summary>
/// Represents a single archived document card ("scheda") in MemoBam.
/// </summary>
public class ArchiveItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredRelativePath { get; set; } = string.Empty;
    public DateTime ArchivedAt { get; set; } = DateTime.Now;
    public string Notes { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public bool IsDirectory { get; set; }
    public bool IsDone { get; set; } = true;

    [JsonIgnore]
    public string DateFormatted => ArchivedAt.ToString("dd/MM/yyyy");

    [JsonIgnore]
    public string TimeFormatted => ArchivedAt.ToString("HH:mm:ss");

    [JsonIgnore]
    public string ShortTimeFormatted => ArchivedAt.ToString("HH:mm");

    [JsonIgnore]
    public string FileTypeBadge
    {
        get
        {
            if (IsDirectory) return "DIR";
            var ext = Path.GetExtension(OriginalFileName).TrimStart('.').ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(ext)) return "FILE";
            return ext.Length > 5 ? ext[..5] : ext;
        }
    }

    [JsonIgnore]
    public string FileSizeFormatted
    {
        get
        {
            if (IsDirectory) return "Cartella";
            string[] units = ["B", "KB", "MB", "GB"];
            double size = FileSizeBytes;
            int unitIndex = 0;
            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }
            return unitIndex == 0 ? $"{size:0} {units[unitIndex]}" : $"{size:0.#} {units[unitIndex]}";
        }
    }

    [JsonIgnore]
    public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);

    [JsonIgnore]
    public bool IsDiaryAttachment { get; set; }

    [JsonIgnore]
    public string DiarySourceLabel { get; set; } = string.Empty;

    [JsonIgnore]
    public string? DirectFullPath { get; set; }

    [JsonIgnore]
    public DiaryEntry? LinkedDiaryEntry { get; set; }

    [JsonIgnore]
    public DiaryAttachment? LinkedDiaryAttachment { get; set; }
}
