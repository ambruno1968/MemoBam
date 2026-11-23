namespace MemoBam.Models;

/// <summary>
/// Represents a reusable "nota pronta" (quick note template) stored in a separate table for the daily diary.
/// </summary>
public class QuickNote
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
