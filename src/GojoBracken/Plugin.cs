using BepInEx;

namespace GojoBracken
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "FreakyFriday.GojoBracken";
        public const string PluginName = "Gojo Bracken";
        public const string PluginVersion = "0.1.0";

        private void Awake()
        {
            Logger.LogInfo($"{PluginName} {PluginVersion} loaded. Plugin foundation only; cosmetics are not implemented yet.");
        }
    }
}

