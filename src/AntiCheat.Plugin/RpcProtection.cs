using System;
using AntiCheat.Core;
using BepInEx.Configuration;
using HarmonyLib;

namespace AntiCheat.Plugin;

public sealed partial class Plugin
{
    private const byte EnterVentRpc = 19;
    private ConfigEntry<bool> blockForgedRpcs = null!;
    private ConfigEntry<bool> blockIllegalVents = null!;
    private ConfigEntry<int> rpcFloodLimit = null!;
    private readonly RpcRateTracker rpcRates = new();
    private bool wasHost;
    private double hostSince;
    private bool rpcFailed;

    private void ConfigureRpcProtection()
    {
        blockForgedRpcs = Config.Bind("Rpc", "BlockHostOnlyRpcsAsHost", true,
            "When host, reject RPCs that only a host may send (kills, roles, meetings, names, colors, shapeshifts). " +
            "Disable for custom game modes whose clients send these.");
        blockIllegalVents = Config.Bind("Rpc", "BlockIllegalVentsAsHost", true,
            "When host, reject vent entry by living players whose role cannot vent.");
        rpcFloodLimit = Config.Bind("Rpc", "FloodWarningPerSecond", 20,
            new ConfigDescription("Warn when one player's object receives more player RPCs than this in a second. 0 disables.",
                new AcceptableValueRange<int>(0, 500)));
    }

    // Host migration can deliver RPCs the previous host sent; skip a short window.
    private bool HostGuardReady(AmongUsClient client)
    {
        double now = clock.Elapsed.TotalSeconds;
        if (!client.AmHost) { wasHost = false; return false; }
        if (!wasHost) { wasHost = true; hostSince = now; }
        return now - hostSince >= 3;
    }

    private bool CanGuardRpcs(out AmongUsClient client)
    {
        client = AmongUsClient.Instance;
        return enabled.Value && !rpcFailed && client != null && HostGuardReady(client);
    }

    internal string Describe(PlayerControl? player)
    {
        if (player == null) return "unknown player";
        try
        {
            string? name = player.Data?.PlayerName;
            return string.IsNullOrWhiteSpace(name) ? $"Player ID {player.PlayerId}" : $"{name} (ID {player.PlayerId})";
        }
        catch { return $"Player ID {player.PlayerId}"; }
    }

    internal bool AllowPlayerRpc(PlayerControl target, byte callId)
    {
        try
        {
            if (!CanGuardRpcs(out var client)) return true;
            int count = rpcRates.Observe(target.PlayerId, clock.Elapsed.TotalSeconds, rpcFloodLimit.Value);
            if (count > 0)
                NoticeRpc($"RPC flood: {count}/s on {Describe(target)} (sender unverified)");
            if (!blockForgedRpcs.Value || !RpcGuardPolicy.IsHostOnly(callId)) return true;
            NoticeRpc($"Blocked forged {RpcGuardPolicy.Describe(callId)} on {Describe(target)} (sender unverified)");
            return false;
        }
        catch (Exception error) { return FailRpc(error); }
    }

    internal bool AllowPhysicsRpc(PlayerPhysics physics, byte callId)
    {
        try
        {
            if (callId != EnterVentRpc || !blockIllegalVents.Value || !CanGuardRpcs(out var client)
                || !client.IsGameStarted) return true;
            var player = physics.myPlayer;
            var data = player?.Data;
            if (data == null || data.IsDead || data.Disconnected || data.Role == null || data.Role.CanVent)
                return true;
            NoticeRpc($"Blocked vent entry by {Describe(player)}: role cannot vent");
            return false;
        }
        catch (Exception error) { return FailRpc(error); }
    }

    private double lastRpcNotice = double.NegativeInfinity;

    private void NoticeRpc(string notice)
    {
        // Always log; limit only the on-screen notice.
        Log.LogWarning(notice);
        double now = clock.Elapsed.TotalSeconds;
        if (now - lastRpcNotice < 2) return;
        lastRpcNotice = now;
        protectionNotice = notice;
        protectionNoticeUntil = now + 15;
    }

    private bool FailRpc(Exception error)
    {
        rpcFailed = true;
        Log.LogError($"RPC checks disabled after an integration error: {error}");
        SetProtectionNotice("RPC checks stopped: check BepInEx log");
        return true;
    }

    internal void ResetRpcProtection() => rpcRates.Clear();
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
internal static class PlayerRpcPatch
{
    private static bool Prefix(PlayerControl __instance, byte __0) => Plugin.Instance.AllowPlayerRpc(__instance, __0);
}

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleRpc))]
internal static class PhysicsRpcPatch
{
    private static bool Prefix(PlayerPhysics __instance, byte __0) => Plugin.Instance.AllowPhysicsRpc(__instance, __0);
}
