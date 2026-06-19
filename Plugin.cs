using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace ScavSetLib
{
    public static class PluginInfo
    {
        public const string GUID = "com.kanisuko.scavsetlib";
        public const string Name = "ScavSetLib";
        public const string Version = "1.0.0";
    }

    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource Log { get; private set; } = null!;
        private Harmony? _harmony;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"{PluginInfo.Name} version {PluginInfo.Version} loaded successfully.");

            _harmony = new Harmony(PluginInfo.GUID);
            _harmony.PatchAll(typeof(SettingsInjectionPatch));
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
