using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace AmongUsAntiCheat
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("Among Us.exe")]
    public class Plugin : BasePlugin
    {
        public const string PluginGuid = "com.muhie-1.amongusanticheat";
        public const string PluginName = "Among Us Anti-Cheat";
        public const string PluginVersion = "1.0.0";

        internal static ManualLogSource Logger;
        private Harmony _harmony;

        public override void Load()
        {
            Logger = Log;
            Logger.LogInfo($"{PluginName} v{PluginVersion} has been loaded successfully!");

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll();
        }
    }
}