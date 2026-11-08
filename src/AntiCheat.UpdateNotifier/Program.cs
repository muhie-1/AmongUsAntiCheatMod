using System.Diagnostics;
using System.Text.Json;

namespace AntiCheat.UpdateNotifier;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        using var instance = new Mutex(true, @"Local\AmongUsAntiCheatMod.UpdateNotifier", out bool isFirst);
        if (!isFirst) return 0;
        bool startup = args.Contains("--startup");
        bool diagnose = args.Contains("--diagnose");
        try
        {
            ApplicationConfiguration.Initialize();
            if (args.Contains("--disable"))
            {
                NotifierSettings.Disable();
                return 0;
            }
            if (args.Contains("--enable"))
            {
                var settings = new NotifierSettings
                {
                    PluginPath = Argument(args, "--plugin") ?? "",
                    Edition = Argument(args, "--edition") ?? "MicrosoftStore",
                    IncludePrereleases = !args.Contains("--stable-only")
                };
                settings.Install();
                return 0;
            }
            var saved = NotifierSettings.Load();
            if (args.Contains("--status"))
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    startupEnabled = NotifierSettings.IsEnabled(),
                    configured = saved != null,
                    installedVersion = saved == null ? null : InstalledPluginVersion.Read(saved.PluginPath),
                    edition = saved?.Edition
                }));
                return 0;
            }
            if (diagnose)
            {
                var settings = new NotifierSettings
                {
                    PluginPath = Argument(args, "--plugin") ?? saved?.PluginPath ?? "",
                    Edition = Argument(args, "--edition") ?? saved?.Edition ?? "MicrosoftStore",
                    IncludePrereleases = !args.Contains("--stable-only")
                };
                var candidate = ReleaseChecker.CheckAsync(settings).GetAwaiter().GetResult();
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    installedVersion = InstalledPluginVersion.Read(settings.PluginPath),
                    edition = settings.Edition,
                    availableUpdate = candidate?.TagName,
                    startupEnabled = NotifierSettings.IsEnabled()
                }));
                return 0;
            }
            if (!startup && !args.Contains("--check-now"))
            {
                Application.Run(new SetupForm(saved));
                return 0;
            }
            if (saved == null) return 0;
            if (startup) Task.Delay(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();
            var update = ReleaseChecker.CheckAsync(saved).GetAwaiter().GetResult();
            if (update == null)
            {
                if (!startup) MessageBox.Show("Your installed mod is up to date for this release channel.", "Among Us Anti Cheat");
                return 0;
            }
            string channel = update.Prerelease ? " (experimental prerelease)" : "";
            string message = $"A new version of Among Us Anti Cheat is available: {update.TagName}{channel}.\n\n"
                + "Would you like to open its GitHub download page?\n\nSelect your edition's ZIP and install it with Among Us closed.";
            if (MessageBox.Show(message, "Among Us Anti Cheat update", MessageBoxButtons.YesNo,
                MessageBoxIcon.Information, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                Process.Start(new ProcessStartInfo(ReleaseChecker.ReleaseUrl(update.TagName)) { UseShellExecute = true });
            return 0;
        }
        catch (Exception error)
        {
            if (diagnose || args.Contains("--status") || args.Contains("--enable") || args.Contains("--disable")) Console.Error.WriteLine(error.Message);
            else if (!startup) MessageBox.Show("Unable to check or configure updates. " + error.Message, "Among Us Anti Cheat");
            // Network failures and missing installations stay silent at sign-in.
            return 1;
        }
    }

    private static string? Argument(string[] args, string name)
    {
        int index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}
