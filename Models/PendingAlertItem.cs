namespace MemoBam.Models;

/// <summary>
/// Unified view model for unchecked ("da terminare") items displayed in the top alert panel.
/// </summary>
public class PendingAlertItem
{
    public string SourceBadge { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string DateTimeLabel { get; init; } = string.Empty;
    public ArchiveItem? ArchiveItem { get; init; }
    public DiaryEntry? DiaryEntry { get; init; }
}
