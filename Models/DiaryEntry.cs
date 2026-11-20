using System.Text.Json.Serialization;

namespace MemoBam.Models;

/// <summary>
/// Represents a single timestamped activity recorded in the daily diary.
/// </summary>
public class DiaryEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Text { get; set; } = string.Empty;

    [JsonIgnore]
    public string DateFormatted => Timestamp.ToString("dd/MM/yyyy");

    [JsonIgnore]
    public string TimeFormatted => Timestamp.ToString("HH:mm:ss");
}
