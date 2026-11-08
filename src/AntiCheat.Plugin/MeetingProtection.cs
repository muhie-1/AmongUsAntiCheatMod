using System;
using AntiCheat.Core;
using BepInEx.Configuration;
using HarmonyLib;
using Hazel;

namespace AntiCheat.Plugin;

public sealed partial class Plugin
{
    private ConfigEntry<bool> monitorMeetingSabotage = null!;
    private ConfigEntry<bool> blockMeetingSabotage = null!;
    private readonly MeetingSabotageTracker sabotageStates = new();
    private bool meetingLatched;
    private double meetingBeganAt;
    private double nextProtectionPoll;
    private double lastSabotageNotice = double.NegativeInfinity;
    private bool protectionFailed;
    private string protectionNotice = "";
    private double protectionNoticeUntil;

    private static readonly SystemTypes[] WatchedSystems =
    {
        SystemTypes.Reactor, SystemTypes.LifeSupp, SystemTypes.Electrical,
        SystemTypes.Comms, SystemTypes.Laboratory, SystemTypes.HeliSabotage,
        SystemTypes.MushroomMixupSabotage
    };

    private void ConfigureProtection()
    {
        monitorMeetingSabotage = Config.Bind("Sabotage", "MonitorDuringMeetings", true,
            "Warn about new sabotage activity during meetings. Guest observations cannot identify the sender.");
        blockMeetingSabotage = Config.Bind("Sabotage", "BlockDuringMeetingsAsHost", true,
            "When host, reject known sabotage initiation requests during meetings/exile. Repairs pass. Disable for custom rules allowing meeting sabotage.");
    }

    internal bool IsMeetingPhase => meetingLatched || MeetingHud.Instance != null || ExileController.Instance != null;

    private bool CanObserveProtection
    {
        get
        {
            var client = AmongUsClient.Instance;
            return enabled.Value && !protectionFailed && client != null
                && (client.AmHost || monitorAsGuest.Value);
        }
    }

    internal void BeginMeeting()
    {
        if (!meetingLatched) meetingBeganAt = clock.Elapsed.TotalSeconds;
        meetingLatched = true;
    }

    internal void EndMeeting() => meetingLatched = false;

    internal void ResetProtection()
    {
        meetingLatched = false;
        sabotageStates.Clear();
        lastSabotageNotice = double.NegativeInfinity;
        protectionNotice = "";
        protectionNoticeUntil = 0;
        detector.Clear();
        lastAlert = "";
    }

    // Blocking uses only game phase and the operation. The PlayerControl in an
    // UpdateSystem payload is not authenticated sender identity: never kick it.
    internal bool AllowSystemUpdate(SystemTypes systemType, byte amount)
    {
        if (!CanObserveProtection || !monitorMeetingSabotage.Value) return true;
        var client = AmongUsClient.Instance;
        if (!client.IsGameStarted || !IsMeetingPhase
            || !SabotageRequestPolicy.IsSabotageStart((byte)systemType, amount)) return true;
        bool block = client.AmHost && blockMeetingSabotage.Value;
        NoticeSabotage(block
            ? "Blocked sabotage request during meeting (sender unverified)"
            : "Sabotage request during meeting (source unverified)");
        return !block;
    }

    private void SetProtectionNotice(string notice)
    {
        protectionNotice = notice;
        protectionNoticeUntil = clock.Elapsed.TotalSeconds + 15;
        Log.LogWarning(notice);
    }

    private void NoticeSabotage(string notice)
    {
        double now = clock.Elapsed.TotalSeconds;
        if (now - lastSabotageNotice < 5) return;
        lastSabotageNotice = now;
        SetProtectionNotice(notice);
    }

    internal void TickProtection()
    {
        if (protectionFailed || clock.Elapsed.TotalSeconds < nextProtectionPoll) return;
        nextProtectionPoll = clock.Elapsed.TotalSeconds + 0.25;
        try
        {
            var client = AmongUsClient.Instance;
            if (client == null || !client.IsGameStarted)
            {
                meetingLatched = false;
                sabotageStates.Clear();
                return;
            }
            bool hasMeetingUi = MeetingHud.Instance != null || ExileController.Instance != null;
            if (hasMeetingUi) BeginMeeting();
            // Fallback if a map/version skips the explicit resume callback.
            if (meetingLatched && !hasMeetingUi && clock.Elapsed.TotalSeconds - meetingBeganAt > 2
                && PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.moveable)
                EndMeeting();
            if (!CanObserveProtection || !monitorMeetingSabotage.Value)
            {
                sabotageStates.Clear();
                return;
            }
            var ship = ShipStatus.Instance;
            if (ship == null) return;
            ulong active = 0;
            for (int i = 0; i < WatchedSystems.Length; i++)
            {
                if (ship.Systems.TryGetValue(WatchedSystems[i], out var system)
                    && system != null && system.TryCast<IActivatable>()?.IsActive == true)
                    active |= 1UL << i;
            }
            // Record baselines during grace so delayed pre-meeting state isn't
            // treated as evidence. This can miss an attack in the grace window.
            bool settledMeeting = IsMeetingPhase && clock.Elapsed.TotalSeconds - meetingBeganAt >= 1.5;
            ulong appeared = sabotageStates.Observe(active, true, settledMeeting);
            for (int i = 0; i < WatchedSystems.Length; i++)
                if ((appeared & (1UL << i)) != 0)
                {
                    NoticeSabotage($"{WatchedSystems[i]} became active during meeting (source unknown)");
                    break;
                }
        }
        catch (Exception error)
        {
            protectionFailed = true;
            Log.LogError($"Meeting sabotage monitoring disabled after integration error: {error}");
            SetProtectionNotice("Meeting sabotage checks stopped: check BepInEx log");
        }
    }
}

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.UpdateSystem),
    new[] { typeof(SystemTypes), typeof(PlayerControl), typeof(byte) })]
internal static class SabotageBytePatch
{
    private static bool Prefix(SystemTypes __0, byte __2) => Plugin.Instance.AllowSystemUpdate(__0, __2);
}

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.UpdateSystem),
    new[] { typeof(SystemTypes), typeof(PlayerControl), typeof(MessageReader) })]
internal static class SabotageReaderPatch
{
    private static bool Prefix(SystemTypes __0, MessageReader __2)
    {
        if (__2 == null || __2.BytesRemaining < 1) return true;
        int position = __2.Position;
        byte amount;
        try { amount = __2.ReadByte(); }
        finally { __2.Position = position; }
        return Plugin.Instance.AllowSystemUpdate(__0, amount);
    }
}

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.StartMeeting))]
internal static class MeetingStartPatch
{
    private static void Prefix() => Plugin.Instance.BeginMeeting();
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Awake))]
internal static class MeetingHudStartPatch
{
    private static void Prefix() => Plugin.Instance.BeginMeeting();
}

[HarmonyPatch(typeof(ExileController), nameof(ExileController.ReEnableGameplay))]
internal static class MeetingResumePatch
{
    private static void Postfix() => Plugin.Instance.EndMeeting();
}

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Awake))]
internal static class RoundStartPatch
{
    private static void Prefix() => Plugin.Instance.ResetProtection();
}
