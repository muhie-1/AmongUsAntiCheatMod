using System.Collections.Generic;

namespace AntiCheat.Core;

/// <summary>
/// RPC rules for a host. Among Us does not include the sender in game data, so
/// these rules judge the call itself, never who sent it.
/// </summary>
public static class RpcGuardPolicy
{
    // PlayerControl RPC ids (game 2026.9.29) that only the host sends in vanilla
    // games. Clients request these with Check* RPCs; the host answers with these.
    // A host never receives its own broadcasts, so receiving one as host means a
    // client forged it.
    private static readonly HashSet<byte> HostOnly = new()
    {
        2,  // SyncSettings
        3,  // SetInfected
        4,  // Exiled
        6,  // SetName
        8,  // SetColor
        12, // MurderPlayer
        14, // StartMeeting
        22, // CloseMeeting
        23, // VotingComplete
        44, // SetRole
        45, // ProtectPlayer
        46, // Shapeshift
        52, // UseZipline
        56, // RejectShapeshift
        63, // StartVanish
        65, // StartAppear
    };

    public static bool IsHostOnly(byte callId) => HostOnly.Contains(callId);

    public static string Describe(byte callId) => callId switch
    {
        2 => "SyncSettings",
        3 => "SetInfected",
        4 => "Exiled",
        6 => "SetName",
        8 => "SetColor",
        12 => "MurderPlayer",
        14 => "StartMeeting",
        19 => "EnterVent",
        22 => "CloseMeeting",
        23 => "VotingComplete",
        44 => "SetRole",
        45 => "ProtectPlayer",
        46 => "Shapeshift",
        52 => "UseZipline",
        56 => "RejectShapeshift",
        63 => "StartVanish",
        65 => "StartAppear",
        _ => "RPC " + callId,
    };
}

/// <summary>Counts calls per key in fixed one-second windows and reports floods once per window.</summary>
public sealed class RpcRateTracker
{
    private sealed class Window
    {
        public double Start;
        public int Count;
        public bool Reported;
    }

    private readonly Dictionary<int, Window> windows = new();

    public void Clear() => windows.Clear();

    /// <summary>Returns the window's count the first time it exceeds the limit, otherwise 0.</summary>
    public int Observe(int key, double now, int limitPerSecond)
    {
        if (limitPerSecond <= 0) return 0;
        if (!windows.TryGetValue(key, out var window) || now - window.Start >= 1 || now < window.Start)
        {
            window = new Window { Start = now };
            windows[key] = window;
        }
        window.Count++;
        if (window.Reported || window.Count <= limitPerSecond) return 0;
        window.Reported = true;
        return window.Count;
    }
}
