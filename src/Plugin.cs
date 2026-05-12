using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace NoMoreRolling
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log = null!;
        private readonly Harmony _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

        private void Awake()
        {
            Log = Logger;
            _harmony.PatchAll();
            Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} loaded.");
        }

        private void OnDestroy()
        {
            _harmony.UnpatchSelf();
        }
    }

    internal static class MyPluginInfo
    {
        public const string PLUGIN_GUID    = "nomorolling";
        public const string PLUGIN_NAME    = "NoMoreRolling";
        public const string PLUGIN_VERSION = "1.0.0";
    }
}
