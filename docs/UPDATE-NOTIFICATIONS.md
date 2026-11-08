# Optional Windows update notifications

The notifier can check for a newer Among Us Anti Cheat release when you sign in to Windows after starting your computer. It is separate from the game mod and only runs if you enable it. It checks once, then exits; it is not a background service.

## Enable notifications

1. Extract `AmongUsAntiCheatMod-UpdateNotifier-win-x64.zip` somewhere you can open it.
2. Run `AmongUs.AntiCheat.UpdateNotifier.exe` to open setup.
3. Click **Browse** and choose your installed `BepInEx/plugins/AntiCheat/AmongUs.AntiCheat.dll`.
4. Select **MicrosoftStore**, **Steam**, or **EpicGames**.
5. Leave **Include experimental prereleases** checked to hear about this project's prereleases. Uncheck it for stable releases only.
6. Click **Enable at Windows sign-in**.

Setup copies the notifier into `%LOCALAPPDATA%/AmongUsAntiCheatMod/UpdateNotifier/` and adds a startup entry for your Windows account. Administrator rights are not required. The published executable includes its .NET runtime; installing the .NET SDK is not needed.

## What happens at sign-in

After a 30-second delay, the notifier reads the installed mod's version from its DLL metadata without loading or executing the DLL. It checks the repository's public GitHub Releases API for a newer version with a ZIP for your selected edition. Drafts, invalid version tags, releases without an edition download, and older/equal versions are ignored.

When an update is available, you see **“A new version of Among Us Anti Cheat is available. Would you like to open its GitHub download page?”** Clicking **Yes** opens that release on GitHub. Choose your edition's ZIP and update both mod DLLs with Among Us closed. The notifier never downloads or installs software automatically. Clicking **No** leaves everything unchanged and may remind you at your next sign-in.

If the mod is current, your installed DLL is unavailable, or the network/API fails, it exits without a startup popup. Windows may delay startup applications. This runs at user sign-in, not before anybody logs in.

GitHub receives a normal unauthenticated release-list request, including normal network information. No game code, chat, player information, DLL content, or local paths are uploaded. A local settings file stores the DLL path, edition, and prerelease preference.

## Disable notifications

Run the notifier again and click **Disable startup checks**. You can also disable **AmongUsAntiCheatUpdateNotifier** in Windows Startup apps.

For the command-line disable option:

```powershell
& "$env:LOCALAPPDATA\AmongUsAntiCheatMod\UpdateNotifier\AmongUs.AntiCheat.UpdateNotifier.exe" --disable
```

Disabling removes the startup registration; it does not uninstall the game mod. If you move the game or switch editions, run setup again and choose the new installed DLL.

## Build from source

```powershell
.\scripts\Build-UpdateNotifier.ps1
```

This creates `dist/UpdateNotifier/AmongUsAntiCheatMod-UpdateNotifier-win-x64.zip`. Publishing a self-contained Windows build may download official .NET runtime packs through NuGet. Attach this optional ZIP to a GitHub Release separately from the three game-mod downloads. Existing mod ZIPs do not enable startup notifications automatically.

If local scripts are disabled, use `powershell -NoProfile -ExecutionPolicy RemoteSigned -File .\scripts\Build-UpdateNotifier.ps1` to set the policy for that process only.

Publishers must increase the mod's `BepInPlugin` version together with the GitHub release version and upload an edition ZIP before a notification can appear. Re-uploading a file under the same version does not count as a new version.

Validation commands (no startup changes or dialogs):

```powershell
dotnet run --project tests/AntiCheat.Checks/AntiCheat.Checks.csproj -c Release
dotnet run --project tests/AntiCheat.NotifierChecks/AntiCheat.NotifierChecks.csproj -c Release
```

These cover 624 core checks and 63 notifier integration checks, including current-version silence, edition-specific assets, prerelease filtering, safe links, and reading installed plugin metadata. Real network failures are handled silently at sign-in; GitHub availability is outside the notifier's control.

Diagnostics can check the real release API without a dialog or startup changes:

```powershell
dotnet src/AntiCheat.UpdateNotifier/bin/Release/net8.0-windows/AmongUs.AntiCheat.UpdateNotifier.dll --diagnose --plugin "C:\XboxGames\Among Us\Content\BepInEx\plugins\AntiCheat\AmongUs.AntiCheat.dll" --edition MicrosoftStore
```

Technical references: [GitHub release-list API](https://docs.github.com/en/rest/releases/releases#list-releases), [Windows per-user Run keys](https://learn.microsoft.com/en-us/windows/win32/setupapi/run-and-runonce-registry-keys).
