using BepInEx;

namespace GojoBracken
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "FreakyFriday.GojoBracken";
        public const string PluginName = "Gojo Bracken";
        public const string PluginVersion = "0.1.1";

        private RuntimeDiagnostics diagnostics;

        private void Awake()
        {
            bool enableDiagnostics = Config.Bind("Diagnostics", "Enabled", false,
                "Log runtime render settings and Bracken state changes for compatibility testing. Restart the game after changing this setting.").Value;
            if (enableDiagnostics)
            {
                diagnostics = new RuntimeDiagnostics(Logger);
            }

            Logger.LogInfo($"{PluginName} {PluginVersion} loaded. Plugin foundation only; cosmetics are not implemented yet.");
        }

        private void Update()
        {
            diagnostics?.Update();
        }
    }
}
