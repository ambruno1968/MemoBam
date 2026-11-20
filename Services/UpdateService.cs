using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace MemoBam.Services;

public sealed record UpdateInfo(
    Version LatestVersion,
    string TagName,
    string ReleaseUrl,
    string? AssetDownloadUrl,
    string? AssetFileName,
    string? ReleaseNotes);

/// <summary>
/// Checks GitHub Releases for newer versions of MemoBam and applies ZIP/EXE updates cleanly.
/// </summary>
public sealed class UpdateService
{
    private const string LatestReleaseApiUrl = "https://api.github.com/repos/ambruno1968/MemoBam/releases/latest";

    private static readonly HttpClient Http = CreateHttpClient();

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(8)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("MemoBam-Updater/1.0");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    public static Version CurrentVersion
    {
        get
        {
            var ver = Assembly.GetExecutingAssembly().GetName().Version;
            return ver is null ? new Version(1, 0, 0) : new Version(ver.Major, ver.Minor, Math.Max(0, ver.Build));
        }
    }

    public async Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken ct = default)
    {
        using var response = await Http.GetAsync(LatestReleaseApiUrl, ct);
        if (!response.IsSuccessStatusCode) return null;

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;

        if (!root.TryGetProperty("tag_name", out var tagProp)) return null;
        string tagName = tagProp.GetString() ?? string.Empty;
        if (!TryParseVersion(tagName, out var latestVersion)) return null;

        if (latestVersion <= CurrentVersion) return null;

        string releaseUrl = root.TryGetProperty("html_url", out var htmlProp)
            ? htmlProp.GetString() ?? "https://github.com/ambruno1968/MemoBam/releases/latest"
            : "https://github.com/ambruno1968/MemoBam/releases/latest";

        string? notes = root.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() : null;

        string? assetUrl = null;
        string? assetName = null;

        if (root.TryGetProperty("assets", out var assetsProp) && assetsProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var asset in assetsProp.EnumerateArray())
            {
                string? name = asset.TryGetProperty("name", out var n) ? n.GetString() : null;
                string? url = asset.TryGetProperty("browser_download_url", out var u) ? u.GetString() : null;
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url)) continue;

                if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    assetName = name;
                    assetUrl = url;
                    break;
                }

                if (assetUrl is null && name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    assetName = name;
                    assetUrl = url;
                }
            }
        }

        return new UpdateInfo(latestVersion, tagName, releaseUrl, assetUrl, assetName, notes);
    }

    public async Task<bool> DownloadAndApplyAsync(UpdateInfo update, IProgress<string>? status = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(update.AssetDownloadUrl) || string.IsNullOrWhiteSpace(update.AssetFileName))
        {
            Process.Start(new ProcessStartInfo(update.ReleaseUrl) { UseShellExecute = true });
            return false;
        }

        string tempRoot = Path.Combine(Path.GetTempPath(), $"MemoBamUpdate_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        string downloadPath = Path.Combine(tempRoot, update.AssetFileName);
        status?.Report($"Scaricamento {update.TagName} in corso...");

        using (var downloadClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) })
        {
            downloadClient.DefaultRequestHeaders.UserAgent.ParseAdd("MemoBam-Updater/1.0");
            using var resp = await downloadClient.GetAsync(update.AssetDownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            resp.EnsureSuccessStatusCode();
            await using var netStream = await resp.Content.ReadAsStreamAsync(ct);
            await using var fileStream = File.Create(downloadPath);
            await netStream.CopyToAsync(fileStream, ct);
        }

        string appDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string exePath = Environment.ProcessPath ?? Path.Combine(appDir, "MemoBam.exe");
        int currentPid = Environment.ProcessId;

        if (downloadPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            status?.Report("Estrazione aggiornamento...");
            string extractDir = Path.Combine(tempRoot, "extracted");
            ZipFile.ExtractToDirectory(downloadPath, extractDir, overwriteFiles: true);

            // If zip contains a single nested folder, use it as source root
            string sourceDir = extractDir;
            var subDirs = Directory.GetDirectories(extractDir);
            var rootFiles = Directory.GetFiles(extractDir);
            if (rootFiles.Length == 0 && subDirs.Length == 1)
            {
                sourceDir = subDirs[0];
            }

            string scriptPath = Path.Combine(tempRoot, "apply_update.ps1");
            string script = $$"""
                $ErrorActionPreference = 'SilentlyContinue'
                try { Wait-Process -Id {{currentPid}} -Timeout 15 } catch {}
                Start-Sleep -Milliseconds 400
                Copy-Item -Path "{{sourceDir}}\*" -Destination "{{appDir}}" -Recurse -Force
                Start-Process -FilePath "{{exePath}}"
                Remove-Item -Path "{{tempRoot}}" -Recurse -Force
                """;
            await File.WriteAllTextAsync(scriptPath, script, ct);

            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{scriptPath}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            });
            return true;
        }

        // Direct .exe installer or executable
        Process.Start(new ProcessStartInfo(downloadPath) { UseShellExecute = true });
        return true;
    }

    private static bool TryParseVersion(string tag, out Version version)
    {
        string cleaned = tag.Trim().TrimStart('v', 'V');
        int dashIdx = cleaned.IndexOf('-');
        if (dashIdx >= 0) cleaned = cleaned[..dashIdx];

        if (Version.TryParse(cleaned, out var parsed))
        {
            version = new Version(parsed.Major, parsed.Minor, Math.Max(0, parsed.Build));
            return true;
        }

        version = new Version(0, 0, 0);
        return false;
    }
}
