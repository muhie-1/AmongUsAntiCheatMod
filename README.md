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

## Microsoft Store setup

1. Find the actual game installation through the Xbox app's **Manage / Files / Browse** option if available. Do not change WindowsApps ownership or permissions. If the installation cannot be modified through supported app options, this mod cannot be installed there directly.
2. Install a BepInEx 6 **Unity IL2CPP** build compatible with the exact game release and executable architecture. The Microsoft Store executable can differ from Steam; do not assume a Steam loader package works. Follow the [BepInEx IL2CPP installation documentation](https://docs.bepinex.dev/master/articles/user_guide/installation/unity_il2cpp.html).
3. Launch once, close the game, and verify `BepInEx/interop/Assembly-CSharp.dll` and `BepInEx/core/BepInEx.Unity.IL2CPP.dll` exist.
4. Install the .NET 6 and .NET 8 SDKs. Copy `Game.props.example` to `Game.props` and change `GameDir` to that installation directory.
5. Build from this repository:

   ```powershell
   dotnet build src/AntiCheat.Plugin/AntiCheat.Plugin.csproj -c Release
   ```

6. Copy **both** `AmongUs.AntiCheat.dll` and `AntiCheat.Core.dll` from `src/AntiCheat.Plugin/bin/Release/net6.0/` into `BepInEx/plugins/AntiCheat/`.
7. Start the game and host a private lobby. Confirm `Anti Cheat loaded` in `BepInEx/LogOutput.log`.

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
