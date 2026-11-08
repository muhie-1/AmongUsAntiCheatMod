using System;
using System.Diagnostics;
using AntiCheat.Core;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;

namespace AntiCheat.Plugin;

[BepInPlugin("local.amongus.anticheat", "Among Us Anti Cheat", "0.3.0")]
public sealed partial class Plugin : BasePlugin
{
    internal static Plugin Instance = null!;
    private readonly Harmony harmony = new("local.amongus.anticheat");
    private readonly MovementDetector detector = new();
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private ConfigEntry<bool> enabled = null!;
    private ConfigEntry<bool> kick = null!;
    private ConfigEntry<float> speed = null!;
    private ConfigEntry<bool> monitorAsGuest = null!;
    private ConfigEntry<bool> showOverlay = null!;
    private string lastAlert = "";
    private double alertUntil;
    private AntiCheatOverlay? overlay;
    private bool failed;

    public override void Load()
    {
        Instance = this;
        enabled = Config.Bind("General", "Enabled", true, "Monitor movement during matches.");
        monitorAsGuest = Config.Bind("General", "MonitorAsGuest", true, "Show local movement warnings when joining another host. Guests cannot kick players.");
        showOverlay = Config.Bind("General", "ShowOverlay", true, "Show monitoring status and recent warnings on your screen only.");
        kick = Config.Bind("General", "KickOnDetection", false, "Kick after repeated movement violations. Test in private lobbies first. Never bans.");
        speed = Config.Bind("Movement", "MaximumWorldUnitsPerSecond", 12f,
            new ConfigDescription("Allowed speed including all lobby/role speed modifiers. Raise for custom roles or fast lobbies.", new AcceptableValueRange<float>(1f, 100f)));
        ConfigureProtection();
        harmony.PatchAll();
        overlay = AddComponent<AntiCheatOverlay>();
        Log.LogInfo("Anti Cheat loaded. Movement monitoring and meeting sabotage protection enabled by default.");
    }

    internal void Observe(PlayerControl player)
    {
        if (failed || !enabled.Value) return;
        try
        {
            var client = AmongUsClient.Instance;
            if (client == null || (!client.AmHost && !monitorAsGuest.Value) || !client.IsGameStarted || IsMeetingPhase)
            {
                detector.Clear();
                return;
            }
            if (player.Data == null || player.Data.Disconnected || player.Data.IsDead)
            {
                detector.Remove(player.PlayerId);
                return;
            }
            var position = player.GetTruePosition();
            // moveable excludes many scripted transitions; continuously refresh grace during them.
            bool exempt = player.inVent || player.onLadder || player.inMovingPlat || !player.moveable;
            var alert = detector.Observe(player.PlayerId, position.x, position.y,
                clock.Elapsed.TotalSeconds, speed.Value, exempt);
            if (alert == null) return;
            lastAlert = $"Player ID {player.PlayerId}: suspicious movement (unconfirmed)";
            alertUntil = clock.Elapsed.TotalSeconds + 12;
            Log.LogWarning($"Player ID {player.PlayerId}, owner {player.OwnerId}: {alert}");
            if (kick.Value && !player.AmOwner && client.AmHost)
            {
                client.KickPlayer(player.OwnerId, false);
                detector.Remove(player.PlayerId);
            }
        }
        catch (Exception error)
        {
            failed = true;
            detector.Clear();
            Log.LogError($"Monitoring disabled after an integration error. Restart after checking game compatibility: {error}");
        }
    }

    internal void DrawOverlay()
    {
        if (!showOverlay.Value || PlayerControl.LocalPlayer == null) return;
        var client = AmongUsClient.Instance;
        if (client == null) return;
        string status;
        if (failed) status = "Monitoring stopped: integration error. Check BepInEx log.";
        else if (!enabled.Value) status = "Monitoring disabled";
        else if (!client.AmHost && !monitorAsGuest.Value) status = "Guest monitoring disabled";
        else if (!client.IsGameStarted) status = "Waiting for match to start";
        else if (IsMeetingPhase) status = "Meeting | movement paused | " +
            (protectionFailed ? "sabotage checks stopped" : !monitorMeetingSabotage.Value ? "sabotage checks off" :
            client.AmHost && blockMeetingSabotage.Value ? "sabotage guard active" : "watching sabotage");
        else status = client.AmHost ? "Monitoring | Host" : "Monitoring | Guest (local warnings only)";
        if (enabled.Value && !failed && clock.Elapsed.TotalSeconds < alertUntil && lastAlert.Length > 0)
            status += "\n" + lastAlert;
        if (enabled.Value && clock.Elapsed.TotalSeconds < protectionNoticeUntil)
            status += "\n" + protectionNotice;
        GUI.Box(new Rect(12, 12, Math.Min(580, Screen.width - 24), 108), "Anti Cheat\n" + status);
    }

    public override bool Unload()
    {
        harmony.UnpatchSelf();
        if (overlay != null) UnityEngine.Object.Destroy(overlay);
        detector.Clear();
        ResetProtection();
        return true;
    }
}

public sealed class AntiCheatOverlay : MonoBehaviour
{
    public AntiCheatOverlay(IntPtr pointer) : base(pointer) { }
    public void Update() => Plugin.Instance.TickProtection();
    public void OnGUI() => Plugin.Instance.DrawOverlay();
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
internal static class MovementPatch
{
    private static void Postfix(PlayerControl __instance) => Plugin.Instance.Observe(__instance);
}
