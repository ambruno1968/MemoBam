using System.IO;
using System.Text.Json;

namespace MemoBam.Services;

public class AppSettings
{
    public string ArchiveRootPath { get; set; } = ArchiveService.FixedArchivePath;
    public bool IsDarkTheme { get; set; }
}

/// <summary>
/// Persists user preferences (such as the customizable root archive folder) in LocalAppData.
/// </summary>
public static class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static string SettingsFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MemoBam",
        "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                string json = File.ReadAllText(SettingsFilePath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (loaded is not null && !string.IsNullOrWhiteSpace(loaded.ArchiveRootPath))
                {
                    return loaded;
                }
            }
        }
        catch
        {
            // Fall back to default settings if unreadable.
        }

        return new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        string? dir = Path.GetDirectoryName(SettingsFilePath);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(SettingsFilePath, json);
    }
}
