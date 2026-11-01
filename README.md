# Among Us Anti Cheat

A movement-monitoring mod for hosts and guests on Windows Among Us, including the Microsoft Store edition when a compatible BepInEx IL2CPP loader is available. This is an initial source implementation, not a game-version-verified release.

## What it does

- Samples locally observed player positions and flags repeated movement above a configurable speed budget.
- Shows a local status panel in the lobby and match, with recent suspicious-movement warnings for 12 seconds. Checks begin when the match starts.
- Works when you join another host; guest mode only displays/logs warnings and cannot kick anyone.
- Requires four consecutive excessive samples, with allowance for network position jitter.
- Resets monitoring outside gameplay, during meetings/exiles, and for dead/disconnected players.
- Allows grace periods after spawning, vents, ladders, moving platforms, and movement-disabled transitions.
- Logs evidence to the BepInEx log. Optional kicks are disabled by default; it never bans players.
- Stops monitoring and logs an error if the game integration throws an exception.

This is a movement heuristic. It does not prevent all cheats, validate kill/task RPCs, detect installed cheat software, or protect a lobby hosted by somebody else. Network jitter and legitimate custom-role abilities can resemble cheating. Single teleports are deliberately not enough to trigger an alert. Keep automatic kicks off until private-lobby testing is complete.

## Installation for players

Use the downloadable mod ZIP from [Releases](https://github.com/muhie-1/AmongUsAntiCheatMod/releases). You do not need Git, Visual Studio, the .NET SDKs, or `Game.props` to install the compiled release.

The current prerelease was compiled against the Microsoft Store Windows x64 installation with BepInEx **6.0.0-be.791**. Loader startup and compilation were verified locally; in-game detection and the overlay still need verification. Other game versions and storefronts are unverified. The mod ZIP includes both plugin DLLs, instructions, and the license; install BepInEx separately.

### 1. Find your game folder

Close Among Us. In the Xbox app, use the game's **Manage / Files / Browse** option if available. Open the folder containing `Among Us.exe`, for example:

```text
C:\XboxGames\Among Us\Content
```

Your installation path may differ. If the installation cannot be modified through supported app options, stop here; do not change WindowsApps ownership or permissions.

### 2. Install BepInEx IL2CPP

If a compatible BepInEx 6 Unity IL2CPP loader already works in this installation, skip to step 3.

1. Open the [official BepInEx builds page](https://builds.bepinex.dev/projects/bepinex_be).
2. For the Microsoft Store **x64** setup used by this project, locate build **#791** and download `BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.791+b782da4.zip`. The architecture must match the game executable; a 32-bit game needs the `win-x86` IL2CPP loader instead. BepInEx 5 and Unity Mono packages do not provide this mod's required loader.
3. Extract the entire loader ZIP into the folder containing `Among Us.exe`. The loader's `BepInEx` folder should be beside the executable, rather than inside an extra ZIP-named folder. If replacing a different loader, back it up outside the game folder first and avoid mixing its files with the new loader.
4. Launch Among Us normally. The first launch may take longer while BepInEx generates assemblies. Wait for the main menu, then close the game.
5. Confirm these files exist:

   ```text
   BepInEx/core/BepInEx.Unity.IL2CPP.dll
   BepInEx/interop/Assembly-CSharp.dll
   ```

See the [official BepInEx IL2CPP installation guide](https://docs.bepinex.dev/master/articles/user_guide/installation/unity_il2cpp.html) for loader setup details.

### 3. Install the mod

1. On this repository's [Releases page](https://github.com/muhie-1/AmongUsAntiCheatMod/releases), expand **Assets** and download `AmongUsAntiCheatMod-v0.2.0.zip`. The automatic **Source code** downloads are for developers and do not contain the compiled mod.
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

### 4. Check the installation

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

## Configuration

After the first launch, close the game and edit `BepInEx/config/local.amongus.anticheat.cfg`:

```ini
[General]
Enabled = true
MonitorAsGuest = true
ShowOverlay = true
KickOnDetection = false

[Movement]
MaximumWorldUnitsPerSecond = 12
```

The limit is in Unity world units per second, **not** the lobby's player-speed multiplier. The conservative default tolerates ordinary movement; it can miss slower speed cheats. Adjust upward for higher speeds/custom roles. Configuration is loaded on startup. Alerts use player IDs and owner IDs rather than player names.

Guest warnings use the movement your client receives over the network, so delayed updates and abilities can produce misleading alerts. Treat them as unconfirmed observations. The overlay is visible only to you; it sends no chat or warning messages to other players. Even with `KickOnDetection = true`, the kick call is guarded by a host check.

Test normal walking, venting, ladders, platforms, meetings, round transitions, and high-latency players on each map you use before considering kicks. Host enforcement uses the game's normal non-ban kick call. Remove both plugin DLLs to uninstall.

## Verify the detection engine

```powershell
dotnet run --project tests/AntiCheat.Checks/AntiCheat.Checks.csproj -c Release
```

This dependency-free check executable covers normal movement, sustained excessive movement, grace periods, transition exemptions, delayed samples, invalid coordinates, and state reset. It does not require Among Us.

The integration follows the BepInEx IL2CPP `BasePlugin` and Harmony pattern used by [Reactor](https://github.com/NuclearPowered/Reactor/blob/master/Reactor/ReactorPlugin.cs). Reactor itself is not a dependency.

## License

This project's source code is licensed under the [MIT License](LICENSE). Among Us and third-party dependencies remain covered by their respective licenses.
