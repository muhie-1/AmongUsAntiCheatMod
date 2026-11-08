using System.Text.Json;
using Microsoft.Win32;

namespace AntiCheat.UpdateNotifier;

internal sealed class NotifierSettings
{
    internal const string StartupName = "AmongUsAntiCheatUpdateNotifier";
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    internal static string InstallDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AmongUsAntiCheatMod", "UpdateNotifier");
    private static string SettingsPath => Path.Combine(InstallDirectory, "settings.json");

    public string PluginPath { get; set; } = "";
    public string Edition { get; set; } = "MicrosoftStore";
    public bool IncludePrereleases { get; set; } = true;

    internal static NotifierSettings? Load()
    {
        try
        {
            var settings = JsonSerializer.Deserialize<NotifierSettings>(File.ReadAllText(SettingsPath));
            return settings != null && ValidEdition(settings.Edition) && Path.IsPathFullyQualified(settings.PluginPath)
                ? settings : null;
        }
        catch { return null; }
    }

    internal static bool ValidEdition(string edition) => edition is "MicrosoftStore" or "Steam" or "EpicGames";

    internal void Install()
    {
        PluginPath = Path.GetFullPath(PluginPath);
        if (!ValidEdition(Edition) || !InstalledPluginVersion.TryRead(PluginPath, out _))
            throw new InvalidOperationException("Select the installed AmongUs.AntiCheat.dll and a supported edition.");
        string sourceExe = Environment.ProcessPath ?? "";
        if (!Path.GetFileName(sourceExe).Equals("AmongUs.AntiCheat.UpdateNotifier.exe", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Install using the published UpdateNotifier executable, not dotnet or a source DLL.");
        if (File.Exists(Path.ChangeExtension(sourceExe, ".dll")))
            throw new InvalidOperationException("Use the self-contained notifier ZIP for installation. This development build needs additional files.");
        string destinationExe = Path.Combine(InstallDirectory, "AmongUs.AntiCheat.UpdateNotifier.exe");
        string startupCommand = "\"" + destinationExe + "\" --startup";
        if (startupCommand.Length > 260) throw new InvalidOperationException("The startup command path is too long.");
        Directory.CreateDirectory(InstallDirectory);
        if (!Path.GetFullPath(sourceExe).Equals(destinationExe, StringComparison.OrdinalIgnoreCase))
            File.Copy(sourceExe, destinationExe, true);
        string temporarySettings = SettingsPath + ".tmp";
        File.WriteAllText(temporarySettings, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporarySettings, SettingsPath, true);
        using var startup = Registry.CurrentUser.CreateSubKey(RunKey, true);
        startup.SetValue(StartupName, startupCommand, RegistryValueKind.String);
    }

    internal static bool IsEnabled()
    {
        using var startup = Registry.CurrentUser.OpenSubKey(RunKey);
        return startup?.GetValue(StartupName) is string;
    }

    internal static void Disable()
    {
        using var startup = Registry.CurrentUser.OpenSubKey(RunKey, true);
        startup?.DeleteValue(StartupName, false);
    }
}
