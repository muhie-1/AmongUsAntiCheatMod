using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using AntiCheat.Core;

namespace AntiCheat.UpdateNotifier;

internal static class ReleaseChecker
{
    internal const string RepositoryUrl = "https://github.com/muhie-1/AmongUsAntiCheatMod";
    private const string ApiUrl = "https://api.github.com/repos/muhie-1/AmongUsAntiCheatMod/releases?per_page=100";

    internal static async Task<ReleaseUpdateCandidate?> CheckAsync(NotifierSettings settings)
    {
        if (!InstalledPluginVersion.TryRead(settings.PluginPath, out var installedText)
            || !ReleaseVersion.TryParse(installedText, out var installed) || installed == null)
            throw new InvalidOperationException("The installed Anti Cheat version could not be read. Select its DLL again in setup.");
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("AmongUsAntiCheatMod-UpdateNotifier/1.0");
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        using var response = await http.GetAsync(ApiUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        const int maxBytes = 2 * 1024 * 1024;
        if (response.Content.Headers.ContentLength > maxBytes) throw new InvalidDataException("Release list is too large.");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var input = await response.Content.ReadAsStreamAsync(timeout.Token);
        using var buffer = new MemoryStream();
        byte[] chunk = new byte[8192];
        int read;
        while ((read = await input.ReadAsync(chunk, timeout.Token)) > 0)
        {
            if (buffer.Length + read > maxBytes) throw new InvalidDataException("Release list is too large.");
            buffer.Write(chunk, 0, read);
        }
        return SelectFromJson(settings, installed, buffer.ToArray());
    }

    internal static ReleaseUpdateCandidate? SelectFromJson(NotifierSettings settings, ReleaseVersion installed, byte[] json)
    {
        var releases = JsonSerializer.Deserialize<List<GitHubRelease>>(json) ?? new();
        var candidates = releases.Select(r => new ReleaseUpdateCandidate
        {
            TagName = r.TagName ?? "",
            Draft = r.Draft,
            Prerelease = r.Prerelease,
            HasDownload = r.Assets?.Any(a => IsEditionAsset(a.Name, r.TagName, settings.Edition)) == true
        });
        return ReleaseUpdateSelector.PickNewest(installed, candidates, settings.IncludePrereleases);
    }

    internal static string ReleaseUrl(string tag) => RepositoryUrl + "/releases/tag/" + Uri.EscapeDataString(tag);

    internal static bool IsEditionAsset(string? name, string? tag, string edition)
    {
        if (name == null || !ReleaseVersion.TryParse(tag, out _)) return false;
        string version = tag!.StartsWith('v') ? tag[1..] : tag;
        string prefix = "AmongUsAntiCheatMod-v" + version;
        return name.Equals(prefix + "-" + edition + ".zip", StringComparison.OrdinalIgnoreCase)
            || (edition == "MicrosoftStore" && name.Equals(prefix + ".zip", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")] public string? TagName { get; set; }
        [JsonPropertyName("draft")] public bool Draft { get; set; }
        [JsonPropertyName("prerelease")] public bool Prerelease { get; set; }
        [JsonPropertyName("assets")] public List<GitHubAsset>? Assets { get; set; }
    }

    private sealed class GitHubAsset
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
    }
}
