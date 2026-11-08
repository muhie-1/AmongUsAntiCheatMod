# Among Us Anti Cheat

A Windows Among Us mod with movement monitoring and meeting sabotage protection. Includes installation and build instructions for Steam, Epic Games, and Microsoft Store. Requires a compatible BepInEx IL2CPP loader. This is an experimental project; see compatibility below.

Installation: [Steam (Windows)](#steam-installation-windows) | [Epic Games (Windows)](#epic-games-installation-windows) | [Microsoft Store / Xbox (Windows)](#microsoft-store-installation-windows) | [Update notifications](#optional-windows-update-notifier)

## Downloads

All three downloads are assets of the [v0.3.0 experimental prerelease](https://github.com/muhie-1/AmongUsAntiCheatMod/releases/tag/v0.3.0), which adds meeting sabotage protection. Choose the ZIP for your edition:

| Windows edition | Download asset |
| --- | --- |
| Microsoft Store / Xbox | [AmongUsAntiCheatMod-v0.3.0-MicrosoftStore.zip](https://github.com/muhie-1/AmongUsAntiCheatMod/releases/download/v0.3.0/AmongUsAntiCheatMod-v0.3.0-MicrosoftStore.zip) |
| Steam | [AmongUsAntiCheatMod-v0.3.0-Steam.zip](https://github.com/muhie-1/AmongUsAntiCheatMod/releases/download/v0.3.0/AmongUsAntiCheatMod-v0.3.0-Steam.zip) |
| Epic Games (experimental compatibility) | [AmongUsAntiCheatMod-v0.3.0-EpicGames.zip](https://github.com/muhie-1/AmongUsAntiCheatMod/releases/download/v0.3.0/AmongUsAntiCheatMod-v0.3.0-EpicGames.zip) |

Each ZIP includes both mod DLLs, installation instructions, the README, and license. Install a compatible **BepInEx 6 Unity IL2CPP** loader separately. The automatic **Source code** archives are for developers and do not contain the compiled mod.

Steam targets game version **2026.9.29**. The Epic download uses Steam API references and has not been verified against Epic's assemblies or in-game. Check the compatibility table below before installing; keep automatic kicking off during testing.

### Optional Windows update notifier

For update notifications when you start your computer and sign in, look for **`AmongUsAntiCheatMod-UpdateNotifier-win-x64.zip`** under **Assets** on the [v0.3.0 release page](https://github.com/muhie-1/AmongUsAntiCheatMod/releases/tag/v0.3.0). This separate Windows x64 tool works with Microsoft Store, Steam, and Epic Games installations. Installing a game-mod ZIP alone does not enable notifications.

1. Extract the notifier ZIP and run **`AmongUs.AntiCheat.UpdateNotifier.exe`**.
2. Click **Browse** and select your installed `BepInEx/plugins/AntiCheat/AmongUs.AntiCheat.dll`.
3. Select your edition: **MicrosoftStore**, **Steam**, or **EpicGames**.
4. Keep **Include experimental prereleases** checked to receive this project's prerelease updates, or uncheck it for stable releases only.
5. Click **Enable at Windows sign-in**. This enables checks for your Windows account; administrator rights and the .NET SDK are not required.

At each sign-in, the notifier waits 30 seconds, checks GitHub once, and exits. If a newer version has a ZIP for your edition, it asks whether to open the release's download page. Choose **Yes** to open GitHub, then download and install your edition's mod ZIP with Among Us closed. Downloads and installation stay manual. It stays silent when you are up to date or the network check fails.

To stop notifications, run the notifier again and click **Disable startup checks**, or disable **AmongUsAntiCheatUpdateNotifier** in Windows Startup apps. Moving the game or switching editions requires selecting the new installed DLL in setup.

See [Windows update notifications](docs/UPDATE-NOTIFICATIONS.md) for details, building the notifier, and troubleshooting. Simply attaching the ZIP to a release does not enable it on anybody's computer; each user must enable it once.

## What it does

The **v0.3.0** downloads above include movement monitoring and meeting sabotage protection. See the [release notes](https://github.com/muhie-1/AmongUsAntiCheatMod/releases/tag/v0.3.0) for changes and testing limitations.

- Samples locally observed player positions and flags repeated movement above a configurable speed budget.
- Shows a local status panel in the lobby and match, with recent suspicious-movement warnings for 12 seconds. Checks begin when the match starts.
- Works when you join another host; guest mode only displays/logs warnings and cannot kick anyone.
- Requires four consecutive excessive samples, with allowance for network position jitter.
- Resets monitoring outside gameplay, during meetings/exiles, and for dead/disconnected players.
- Allows grace periods after spawning, vents, ladders, moving platforms, and movement-disabled transitions.
- Logs evidence to the BepInEx log. Optional kicks are disabled by default; it never bans players.
- Stops monitoring and logs an error if the game integration throws an exception.
- In v0.3.0, checks known reactor, oxygen, lights, communications, seismic, helicopter, and mushroom mixup sabotage starts during meetings/exile. Hosts can reject these requests; guests can only observe warnings.

This is a movement heuristic. It does not prevent all cheats, validate kill/task RPCs, detect installed cheat software, or protect a lobby hosted by somebody else. Network jitter and legitimate custom-role abilities can resemble cheating. Single teleports are deliberately not enough to trigger an alert. Keep automatic kicks off until private-lobby testing is complete.

### Meeting sabotage protection (v0.3.0)

When you host, the default guard rejects recognized sabotage initiation requests during the meeting/exile phase. It covers both system-update overloads, including direct damage operations, and leaves recognized repairs and console operations alone. Doors and unknown/custom operations are outside this rule. It does not automatically kick the player referenced in an update payload because that reference does not prove who sent it.

As a guest, the mod cannot enforce the host's game state. It watches sabotage systems becoming active during meetings and shows a local warning with **source unknown**. Systems that were already active are not treated as new attacks. The observer records a baseline during a 1.5-second meeting grace period to reduce delayed-state false alarms; attacks in that window can be missed. Scripted/custom game modes and delayed packets may still create misleading warnings. Disable this guard for rules that intentionally allow meeting sabotage.

Warnings stay on screen for 15 seconds and are logged locally. Repeated sabotage warnings are limited to one per five seconds; the host still rejects every recognized blocked request. In-game verification remains pending, and this does not cover every malicious RPC or state-synchronization path.

### Role knowledge limitation

This mod cannot determine whether another player secretly read impostor roles without sending observable cheating actions. Correct deductions, accusations, or leaving the game do not prove role hacking. It does not reveal hidden roles or label somebody a hacker based on a guess.

## Installation for players

Use the downloadable mod ZIP from [Releases](https://github.com/muhie-1/AmongUsAntiCheatMod/releases). You do not need Git, Visual Studio, the .NET SDKs, or `Game.props` to install the compiled release.

The mod ZIP includes both plugin DLLs, instructions, and the license; install BepInEx separately. The plugin targets AnyCPU; the loader's architecture must match the game executable.

| Windows edition | Build and verification status |
| --- | --- |
| Microsoft Store | Compiled against the local x64 game's assemblies with BepInEx **6.0.0-be.791**; loader startup verified. In-game detection and overlay verification remain pending. |
| Steam | Dedicated target compiled against `AmongUs.GameLibs.Steam` **2026.9.29**, using generated Steam interop references. Windows x64 runtime testing remains pending. |
| Epic Games | Experimental compatibility package using the same shared Windows API build and Steam **2026.9.29** references. Compilation against Epic-specific assemblies and Epic runtime testing are unverified. |

Use `AmongUsAntiCheatMod-v0.3.0-MicrosoftStore.zip` for Microsoft Store or `AmongUsAntiCheatMod-v0.3.0-Steam.zip` for Steam **2026.9.29**. These compile the same protection and overlay code against each edition's references; the Epic compatibility package uses Steam references. All three downloads are listed in [Downloads](#downloads) under the same `v0.3.0` release. Compilation does not establish in-game detection accuracy.

### Steam installation (Windows)

Close Among Us before installing.

In Steam, open **Library**, right-click **Among Us**, and choose **Manage → Browse local files** (or **Properties → Installed Files → Browse**). A common path is:

```text
C:\Program Files (x86)\Steam\steamapps\common\Among Us
```

Your Steam library may be on another drive. Keep Steam open and launch the game through Steam after installing the loader and mod. These instructions target native Windows; Linux/Steam Deck/Proton are unverified.

1. Install the Windows **BepInEx 6 Unity IL2CPP** loader matching your game executable's architecture. See [loader setup](#install-bepinex-il2cpp-all-editions) below for download links and the first launch.
2. Download the [Steam ZIP](https://github.com/muhie-1/AmongUsAntiCheatMod/releases/download/v0.3.0/AmongUsAntiCheatMod-v0.3.0-Steam.zip). It targets Steam game version **2026.9.29** on Windows x64. To compile it yourself, use the [pinned Steam build](#build-a-steam-release-zip). Other game versions require a matching build.
3. Extract the mod ZIP into this Steam game folder. Check that **both** DLLs are in `BepInEx/plugins/AntiCheat/` as shown in [Install the mod](#install-the-mod-all-editions).
4. Launch through Steam and join a private lobby. Check for the **Anti Cheat** panel and `Anti Cheat loaded` in the BepInEx log. Steam runtime verification is still pending.

### Epic Games installation (Windows)

The Epic Games ZIP is an **experimental compatibility candidate** for Windows game version **2026.9.29**. It uses Steam reference assemblies for the game APIs accessed by the mod; Epic-specific assembly compatibility has not been established. It is not an Epic-tested release.

1. Close Among Us. In Epic Games Launcher, open **Library**, click the **three dots** beside Among Us, choose **Manage**, and click the folder icon beside **Installation**. This follows [Epic's instructions for finding an installed game's folder](https://www.epicgames.com/help/c-32735058/c-36403860/a20183292). Use the folder containing `Among Us.exe`; an example is `C:\Program Files\Epic Games\AmongUs`, but your path may differ.
2. Install **BepInEx 6 Unity IL2CPP** matching the installed game executable's architecture: `win-x64` for 64-bit or `win-x86` for 32-bit. Follow the shared loader setup below. Launch once through Epic, wait for the main menu, then close the game and check that `BepInEx/interop/Assembly-CSharp.dll` exists.
3. Download the [experimental Epic Games ZIP](https://github.com/muhie-1/AmongUsAntiCheatMod/releases/download/v0.3.0/AmongUsAntiCheatMod-v0.3.0-EpicGames.zip). Extract it into the game folder, merging the included `BepInEx` directory. Both DLLs belong in `BepInEx/plugins/AntiCheat/`.
4. Launch through Epic and test in a private lobby with automatic kicking off. Look for the panel and `Anti Cheat loaded` in the log. If the log reports missing game types/methods or patching errors, close the game and remove the mod, then [build against your Epic installation's own assemblies](#build-an-epic-games-release-zip).

### Microsoft Store installation (Windows)

In the Xbox app, use the game's **Manage / Files / Browse** option if available. An example path is:

```text
C:\XboxGames\Among Us\Content
```

Your installation path may differ. If the installation cannot be modified through supported app options, stop here; do not change WindowsApps ownership or permissions.

1. Follow [loader setup](#install-bepinex-il2cpp-all-editions) using the Microsoft Store x64 package described below.
2. Download the [Microsoft Store ZIP](https://github.com/muhie-1/AmongUsAntiCheatMod/releases/download/v0.3.0/AmongUsAntiCheatMod-v0.3.0-MicrosoftStore.zip) and follow [Install the mod](#install-the-mod-all-editions).
3. Launch normally through Xbox / Microsoft Store and check the installation in a private lobby.

### Install BepInEx IL2CPP (all editions)

If a compatible BepInEx 6 Unity IL2CPP loader already works in this installation, skip to [Install the mod](#install-the-mod-all-editions).

1. Open the [official BepInEx builds page](https://builds.bepinex.dev/projects/bepinex_be).
2. For the Microsoft Store **x64** setup used by this project, scroll down and locate build **#791** and download `BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.791+b782da4.zip`. The architecture must match the game executable; a 32-bit game needs the `win-x86` IL2CPP loader instead. BepInEx 5 and Unity Mono packages do not provide this mod's required loader.
3. Extract the entire loader ZIP into the folder containing `Among Us.exe`. The loader's `BepInEx` folder should be beside the executable, rather than inside an extra ZIP-named folder. If replacing a different loader, back it up outside the game folder first and avoid mixing its files with the new loader.
4. Launch Among Us normally. The first launch may take longer while BepInEx generates assemblies. Wait for the main menu, then close the game.
5. Confirm these files exist:

   ```text
   BepInEx/core/BepInEx.Unity.IL2CPP.dll
   BepInEx/interop/Assembly-CSharp.dll
   ```

See the [official BepInEx IL2CPP installation guide](https://docs.bepinex.dev/master/articles/user_guide/installation/unity_il2cpp.html) for loader setup details.

For the **Steam 2026.9.29** target, use the Windows **x64 Unity IL2CPP** loader (build #791 is the loader used for this project's local Microsoft Store setup). Older Steam builds may have a different architecture and are not covered by this release. Do not infer the game's architecture from `Program Files (x86)` in the installation path. Follow the [BepInEx architecture check](https://docs.bepinex.dev/master/articles/user_guide/troubleshooting.html#windows-check-the-bitness-of-the-game) if unsure. The generated log also reports `Process bitness`. The Steam target compiles, but its loader/game combination still needs runtime validation.

### Install the mod (all editions)

1. On the [v0.3.0 release page](https://github.com/muhie-1/AmongUsAntiCheatMod/releases/tag/v0.3.0), expand **Assets** and choose your edition's mod ZIP: `-MicrosoftStore.zip`, `-Steam.zip`, or the experimental `-EpicGames.zip`. The automatic **Source code** downloads are for developers and do not contain the compiled mod.
2. With the game closed, extract the mod ZIP into the folder containing `Among Us.exe`. Merge the included `BepInEx` folder with the existing one.
3. Check that both DLLs are in this exact layout:

   ```text
   Among Us.exe
   BepInEx/
     plugins/
       AntiCheat/
         AmongUs.AntiCheat.dll
         AntiCheat.Core.dll
   ```

### Check the installation

Launch the game and join or host a private lobby. Look for the **Anti Cheat** panel near the top-left. In the waiting lobby it should say **Waiting for match to start**; movement checks begin during the match. Guest mode shows local warnings only. Automatic kicking is off by default.

After closing the game, check `BepInEx/LogOutput.log` for `Anti Cheat loaded`. Loading successfully does not establish detection accuracy; test normal movement, meetings, and role abilities in private matches first.

### Troubleshooting

- **No BepInEx log or generated assemblies:** check that the entire Unity IL2CPP loader archive was extracted beside `Among Us.exe` and that its architecture matches the game. Check `BepInEx/LogOutput.log` (or `LogOutput.txt` on some builds) if present.
- **BepInEx loads but no Anti Cheat panel:** check that both mod DLLs are in `BepInEx/plugins/AntiCheat/`, then search the log for `Anti Cheat` and errors. If a configuration file already exists, confirm `Enabled = true` and `ShowOverlay = true`, then restart the game.
- **Warnings during legitimate movement:** leave `KickOnDetection = false`. Network delays and legitimate abilities can resemble cheating; see configuration below.
- **Still stuck:** [open an issue](https://github.com/muhie-1/AmongUsAntiCheatMod/issues) with your game version, storefront, BepInEx version, and relevant error lines. Remove personal paths or other private information before sharing logs.

### Updating or uninstalling

To update, close the game and replace both DLLs in `BepInEx/plugins/AntiCheat/` with the new release's copies. Keep only one copy of the mod in the plugins folder. Existing configuration is retained; check the new release's compatibility notes before updating.

To uninstall this mod, close the game and remove the `BepInEx/plugins/AntiCheat/` folder. This leaves BepInEx and any other mods installed.

## Build from source (developers)

Complete the BepInEx setup above first so the generated game assemblies exist.

1. Install the .NET 6 and .NET 8 SDKs. Copy `Game.props.example` to `Game.props` and change `GameDir` to the folder containing `Among Us.exe`.
2. Build from this repository:

   ```powershell
   dotnet build src/AntiCheat.Plugin/AntiCheat.Plugin.csproj -c Release
   ```

3. Copy **both** `AmongUs.AntiCheat.dll` and `AntiCheat.Core.dll` from `src/AntiCheat.Plugin/bin/Release/net6.0/` into `BepInEx/plugins/AntiCheat/`.
4. Start the game and join or host a private lobby. Confirm `Anti Cheat loaded` in `BepInEx/LogOutput.log`.

Game API changes may require adjusting `Plugin.cs`; a successful core check does not establish game compatibility. No game assemblies are bundled. The plugin has been compiled against the local Microsoft Store game's generated assemblies with BepInEx 6.0.0-be.791. In-game detection and enforcement have not yet been verified.

### Build a Steam release ZIP

The dedicated `src/AntiCheat.Steam/AntiCheat.Steam.csproj` target shares `Plugin.cs` and the detection engine with the Microsoft Store build. It restores the pinned `AmongUs.GameLibs.Steam` **2026.9.29** package and generates interop references using `BepInEx.IL2CPP.MSBuild`. It does not use `Game.props` or Microsoft Store assemblies and does not require a local Steam installation to compile.

Install the .NET 6 and .NET 8 SDKs. From this repository's PowerShell terminal, run:

```powershell
.\scripts\Build-Release.ps1 -Store Steam
```

The script creates `dist/Steam/AmongUsAntiCheatMod-v0.3.0-Steam.zip` with both DLLs, the license, and installation instructions. Package restore uses the BepInEx and NuGet.org feeds in `config/NuGet.Steam.Config`; the first build requires internet access and can take longer to generate references. The script writes the release into this repository's ignored `dist/` folder, and the build tools use normal .NET/NuGet caches. It does not modify a game installation or upload anything. Compilation failure stops packaging.

If PowerShell reports that scripts are disabled, this command allows local scripts for this process only, without changing your saved execution policy:

```powershell
powershell -NoProfile -ExecutionPolicy RemoteSigned -File .\scripts\Build-Release.ps1 -Store Steam
```

To compile the Steam DLLs without packaging, run:

```powershell
dotnet build src/AntiCheat.Steam/AntiCheat.Steam.csproj -c Release
```

For a different game version, or to use the exact references generated by your own Steam loader, install and launch the game with BepInEx first, then provide its path:

```powershell
.\scripts\Build-Release.ps1 -Store Steam -GameDir "C:\Program Files (x86)\Steam\steamapps\common\Among Us"
```

Passing `GameDir` selects the local-assembly build instead of the pinned reference-package build. For manual local builds, copy `config/Game.Steam.props.example` to the repository root as `Game.props`, edit the path, and run `dotnet build src/AntiCheat.Plugin/AntiCheat.Plugin.csproj -c Release`. `config/Game.MicrosoftStore.props.example` provides the equivalent Xbox path. The release script passes `GameDir` explicitly, so your existing `Game.props` does not need to change.

To build a Microsoft Store package with the same script:

```powershell
.\scripts\Build-Release.ps1 -Store MicrosoftStore -GameDir "C:\XboxGames\Among Us\Content"
```

### Build an Epic Games release ZIP

To package the shared Windows API build as an explicitly unverified Epic compatibility download:

```powershell
.\scripts\Build-Release.ps1 -Store EpicGames
```

This creates `dist/EpicGames/AmongUsAntiCheatMod-v0.3.0-EpicGames.zip`. It uses the existing Steam package target and includes the reference source and Epic verification limits in `INSTALL.txt`. No Epic-specific package or local Epic installation was available here. This command does not turn Steam references into Epic references.

For a build compiled against Epic's actual generated assemblies, install the matching BepInEx loader in the Epic game folder, launch once, close the game, and run:

```powershell
.\scripts\Build-Release.ps1 -Store EpicGames -GameDir "C:\Program Files\Epic Games\AmongUs"
```

Use the actual path from Epic's **Manage / Installation** folder button. Passing `GameDir` selects the local-assembly target and replaces the Epic ZIP with that build. Alternatively, copy `config/Game.EpicGames.props.example` to the repository root as `Game.props`, edit the path, and build `src/AntiCheat.Plugin/AntiCheat.Plugin.csproj` manually. If scripts are disabled, use the process-only `RemoteSigned` command shown in the Steam section, replacing `-Store Steam` with `-Store EpicGames`.

### Publishing builds for all editions

Attach the generated `-Steam.zip`, `-EpicGames.zip`, and `-MicrosoftStore.zip` assets to a GitHub prerelease. Keep `dist/` out of Git. Include the game version, reference source, BepInEx version, and verification status for each asset in the release notes. The Steam package target (also used for the default Epic compatibility download) currently uses BepInEx **6.0.0-be.735** and Il2CppInterop **1.4.6-ci.426** as compile-time references to match the Cecil-based MSBuild generator; these dependencies are not bundled. Runtime compatibility with the selected loader must still be checked. Do not describe a shared-reference compatibility package as compiled or tested against Epic's own assemblies.

For each edition, verify plugin loading, the panel in a lobby, guest and host monitoring in a private match, meetings, role abilities, and map movement transitions. Keep automatic kicking off. The current tests verify the detection engine, not Steam runtime integration.

## Configuration

### Optional update notifications at Windows sign-in

Follow [Optional Windows update notifier](#optional-windows-update-notifier) above to enable or disable sign-in notifications. The tool is downloaded separately and opens the GitHub release page only after you choose **Yes**; updates are installed manually.

### Mod settings

After the first launch, close the game and edit `BepInEx/config/local.amongus.anticheat.cfg`:

```ini
[General]
Enabled = true
MonitorAsGuest = true
ShowOverlay = true
KickOnDetection = false

[Movement]
MaximumWorldUnitsPerSecond = 12

[Sabotage]
MonitorDuringMeetings = true
BlockDuringMeetingsAsHost = true
```

The limit is in Unity world units per second, **not** the lobby's player-speed multiplier. The conservative default tolerates ordinary movement; it can miss slower speed cheats. Adjust upward for higher speeds/custom roles. Configuration is loaded on startup. Alerts use player IDs and owner IDs rather than player names.

Guest warnings use the movement your client receives over the network, so delayed updates and abilities can produce misleading alerts. Treat them as unconfirmed observations. The overlay is visible only to you; it sends no chat or warning messages to other players. Even with `KickOnDetection = true`, the kick call is guarded by a host check.

Test normal walking, venting, ladders, platforms, meetings, round transitions, and high-latency players on each map you use before considering kicks. Host enforcement uses the game's normal non-ban kick call. Remove both plugin DLLs to uninstall.

## Verify the detection engine

```powershell
dotnet run --project tests/AntiCheat.Checks/AntiCheat.Checks.csproj -c Release
```

This dependency-free check executable covers normal movement, sustained excessive movement, grace periods, transition exemptions, delayed samples, invalid coordinates, sabotage start/repair classification, meeting-state transitions, state resets, and update-version selection. It does not require Among Us. The 624 checks exercise the core logic; they do not replace testing the Unity hooks in-game.

The integration follows the BepInEx IL2CPP `BasePlugin` and Harmony pattern used by [Reactor](https://github.com/NuclearPowered/Reactor/blob/master/Reactor/ReactorPlugin.cs). Reactor itself is not a dependency.

## License

This project's source code is licensed under the [MIT License](LICENSE). Among Us and third-party dependencies remain covered by their respective licenses.
