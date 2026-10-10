using HarmonyLib;
using UnityEngine;

namespace AmongUsAntiCheat.Patches
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    public static class SpeedDetectorPatch
    {
        // Maximum allowed speed threshold
        private const float MaxAllowedSpeed = 3.5f;

        public static void Postfix(PlayerControl __instance)
        {
            if (__instance == null || __instance.MyPhysics == null) return;

            // Calculate current player velocity
            Vector2 velocity = __instance.MyPhysics.velocity;
            float currentSpeed = velocity.magnitude;

            // Check if speed exceeds threshold
            if (currentSpeed > MaxAllowedSpeed)
            {
                string playerName = __instance.Data != null ? __instance.Data.PlayerName : "Unknown";

                Plugin.Logger.LogWarning(
                    $"[CHEAT DETECTED] Player '{playerName}' exceeded speed limit! Current speed: {currentSpeed:F2}"
                );
            }
        }
    }
}