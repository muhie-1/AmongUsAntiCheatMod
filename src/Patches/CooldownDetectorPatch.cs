using HarmonyLib;

namespace AmongUsAntiCheat.Patches
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
    public static class CooldownDetectorPatch
    {
        public static bool Prefix(PlayerControl __instance, PlayerControl target)
        {
            if (__instance == null || __instance.Data == null) return true;

            if (!__instance.Data.Role.IsImpostor)
            {
                Plugin.Logger.LogWarning(
                    $"[CHEAT DETECTED] Non-Impostor player '{__instance.Data.PlayerName}' attempted to kill '{target.Data.PlayerName}'!"
                );
                return false;
            }

            return true;
        }
    }
}