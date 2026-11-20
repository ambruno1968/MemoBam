using System.Text.Json.Serialization;

namespace MemoBam.Models;

/// <summary>
/// Represents a file or folder attached to a specific diary entry.
/// </summary>
public class DiaryAttachment
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string FileName { get; set; } = string.Empty;
    public string StoredRelativePath { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; } = DateTime.Now;
    public bool IsDirectory { get; set; }

    [JsonIgnore]
    public string DisplayLabel => IsDirectory ? $"📁 {FileName}" : $"📎 {FileName}";
}
