using System.Text;
using System.Text.Json;
using AntiCheat.Core;
using AntiCheat.UpdateNotifier;

int checks = 0;
void Check(bool condition, string description)
{
    if (!condition) throw new InvalidOperationException("FAILED: " + description);
    checks++;
}

ReleaseVersion Version(string value)
{
    if (!ReleaseVersion.TryParse(value, out var version) || version == null)
        throw new InvalidOperationException("Invalid test version: " + value);
    return version;
}

string Asset(string version, string edition) => $"AmongUsAntiCheatMod-v{version}-{edition}.zip";
byte[] Json(params ReleaseFixture[] releases) => JsonSerializer.SerializeToUtf8Bytes(releases.Select(release => new
{
    tag_name = release.Tag,
    draft = release.Draft,
    prerelease = release.Prerelease,
    assets = release.Assets.Select(name => new { name, browser_download_url = "https://untrusted.invalid/payload.exe" }),
    html_url = "https://untrusted.invalid/payload.exe"
}));

ReleaseUpdateCandidate? Select(string edition, bool prereleases, params ReleaseFixture[] releases) =>
    ReleaseChecker.SelectFromJson(new NotifierSettings { Edition = edition, IncludePrereleases = prereleases },
        Version("0.3.0"), Json(releases));

foreach (string edition in new[] { "MicrosoftStore", "Steam", "EpicGames" })
{
    Check(Select(edition, true, new ReleaseFixture("v0.3.0", false, false, Asset("0.3.0", edition))) == null,
        edition + ": installed version does not prompt");
    Check(Select(edition, true, new ReleaseFixture("v0.2.0", false, false, Asset("0.2.0", edition))) == null,
        edition + ": older version does not prompt");
    Check(Select(edition, true, new ReleaseFixture("v0.4.0", false, false, Asset("0.4.0", edition)))?.TagName == "v0.4.0",
        edition + ": newer matching asset prompts");
    Check(Select(edition, true, new ReleaseFixture("v0.4.0", true, false, Asset("0.4.0", edition))) == null,
        edition + ": draft is ignored");
    Check(Select(edition, false, new ReleaseFixture("v0.4.0", false, true, Asset("0.4.0", edition))) == null,
        edition + ": GitHub prerelease excluded from stable channel");
    Check(Select(edition, true, new ReleaseFixture("v0.4.0", false, true, Asset("0.4.0", edition)))?.TagName == "v0.4.0",
        edition + ": GitHub prerelease included when opted in");
    Check(Select(edition, false, new ReleaseFixture("v0.4.0-rc.1", false, false, Asset("0.4.0-rc.1", edition))) == null,
        edition + ": SemVer prerelease excluded even without GitHub flag");
    Check(Select(edition, true, new ReleaseFixture("v0.4.0-rc.1", false, true, Asset("0.4.0-rc.1", edition)))?.TagName == "v0.4.0-rc.1",
        edition + ": SemVer prerelease included when opted in");
    Check(Select(edition, true, new ReleaseFixture("v0.4.0", false, false, Asset("0.3.0", edition))) == null,
        edition + ": asset must match release version");
    Check(Select(edition, true, new ReleaseFixture("v0.4.0", false, false)) == null,
        edition + ": release without assets ignored");

    foreach (string otherEdition in new[] { "MicrosoftStore", "Steam", "EpicGames" }.Where(value => value != edition))
        Check(Select(edition, true, new ReleaseFixture("v0.4.0", false, false, Asset("0.4.0", otherEdition))) == null,
            edition + ": ignores asset for " + otherEdition);
}

Check(Select("Steam", true,
    new ReleaseFixture("v0.3.1", false, false, Asset("0.3.1", "Steam")),
    new ReleaseFixture("v0.9.0", false, false, Asset("0.9.0", "EpicGames")),
    new ReleaseFixture("v0.8.0", true, false, Asset("0.8.0", "Steam")),
    new ReleaseFixture("v0.4.0", false, false, Asset("0.4.0", "Steam")),
    new ReleaseFixture("v0.3.9", false, false, Asset("0.3.9", "Steam")))?.TagName == "v0.4.0",
    "Unordered API results choose newest eligible version");
Check(Select("Steam", false,
    new ReleaseFixture("v0.5.0", false, true, Asset("0.5.0", "Steam")),
    new ReleaseFixture("v0.4.0", false, false, Asset("0.4.0", "Steam")))?.TagName == "v0.4.0",
    "Stable channel can fall back to older eligible stable release");
Check(ReleaseChecker.SelectFromJson(new NotifierSettings(), Version("0.3.0"), Encoding.UTF8.GetBytes("[]")) == null,
    "Empty API result produces no update");
Check(ReleaseChecker.SelectFromJson(new NotifierSettings(), Version("0.3.0"), Encoding.UTF8.GetBytes(
    "[{\"tag_name\":\"v0.4.0\",\"assets\":null}]")) == null,
    "Null asset list produces no update");
Check(ReleaseChecker.SelectFromJson(new NotifierSettings(), Version("0.3.0"), Encoding.UTF8.GetBytes(
    "[{\"assets\":[{\"name\":\"AmongUsAntiCheatMod-v0.4.0-MicrosoftStore.zip\"}]}]")) == null,
    "Missing release tag produces no update");

foreach (string tag in new[] { "https://untrusted.invalid/payload.exe", "../../payload", "v0.4.0/../../payload", "v0.4.0?run=payload", "v0.4.0\r\n.exe" })
{
    Check(Select("Steam", true, new ReleaseFixture(tag, false, false, Asset("0.4.0", "Steam"))) == null,
        "Reject malicious release tag: " + JsonSerializer.Serialize(tag));
    Uri url = new(ReleaseChecker.ReleaseUrl(tag));
    Check(url.Scheme == Uri.UriSchemeHttps && url.Host == "github.com" && url.Query == "" && url.Fragment == ""
        && url.AbsolutePath.StartsWith("/muhie-1/AmongUsAntiCheatMod/releases/tag/", StringComparison.Ordinal),
        "Untrusted tag cannot change download-page scheme, host, repository, query or fragment");
}

Check(ReleaseChecker.IsEditionAsset("AmongUsAntiCheatMod-v0.2.0.zip", "v0.2.0", "MicrosoftStore"),
    "Legacy Microsoft Store asset filename remains recognized");
Check(!ReleaseChecker.IsEditionAsset("AmongUsAntiCheatMod-v0.2.0.zip", "v0.2.0", "Steam"),
    "Legacy Microsoft Store asset cannot match Steam");
Check(ReleaseChecker.IsEditionAsset("AmongUsAntiCheatMod-v0.4.0-EpicGames.zip", "0.4.0", "EpicGames"),
    "Version tag without v prefix is accepted");
Check(!ReleaseChecker.IsEditionAsset(null, "v0.4.0", "Steam"), "Null asset name is ignored");
Check(!ReleaseChecker.IsEditionAsset("AmongUsAntiCheatMod-v0.4.0-Steam.zip.exe", "v0.4.0", "Steam"),
    "Asset extension must be exactly ZIP");
Check(ReleaseChecker.ReleaseUrl("v0.4.0") == "https://github.com/muhie-1/AmongUsAntiCheatMod/releases/tag/v0.4.0",
    "Release page uses hardcoded repository, ignoring API-supplied html_url and download URLs");

string pluginPath = args.Length > 0 ? Path.GetFullPath(args[0]) : Path.GetFullPath(Path.Combine(
    AppContext.BaseDirectory, "../../../../../src/AntiCheat.Plugin/bin/Release/net6.0/AmongUs.AntiCheat.dll"));
if (!File.Exists(pluginPath)) throw new FileNotFoundException("Build the mod first, or provide its DLL as the first argument.", pluginPath);
Check(InstalledPluginVersion.Read(pluginPath) == "0.3.0", "Actual plugin metadata declares installed version 0.3.0");
Check(InstalledPluginVersion.TryRead(pluginPath, out string? declaredVersion) && declaredVersion == "0.3.0",
    "TryRead reads actual installed plugin version");
Check(InstalledPluginVersion.Read(typeof(ReleaseVersion).Assembly.Location) == null, "Core DLL is not mistaken for a plugin");
Check(InstalledPluginVersion.Read(typeof(InstalledPluginVersion).Assembly.Location) == null, "Notifier DLL is not mistaken for a plugin");
Check(!InstalledPluginVersion.TryRead(Path.Combine(Path.GetDirectoryName(pluginPath)!, "missing-plugin.dll"), out _),
    "Missing plugin cannot report an installed version");
Check(InstalledPluginVersion.Read("") == null, "Empty plugin path is rejected");

Console.WriteLine($"All {checks} notifier integration checks passed (no UI, registry changes, or network access).");

sealed record ReleaseFixture(string Tag, bool Draft, bool Prerelease, params string[] Assets);

