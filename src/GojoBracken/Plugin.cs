using System.IO;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace GojoBracken
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "FreakyFriday.GojoBracken";
        public const string PluginName = "Gojo Bracken";
        public const string PluginVersion = "0.1.2";

        private RuntimeDiagnostics diagnostics;
        private BrackenRigReport rigReport;

        private void Awake()
        {
            bool enableDiagnostics = Config.Bind("Diagnostics", "Enabled", false,
                "Log runtime render settings and Bracken state changes for compatibility testing. Restart the game after changing this setting.").Value;
            if (enableDiagnostics)
            {
                diagnostics = new RuntimeDiagnostics(Logger);
            }

            bool exportRig = Config.Bind("Diagnostics", "ExportBrackenRig", false,
                "Export a read-only report from an already-loaded Bracken prefab. Never spawns enemies. Restart after changing.").Value;
            if (exportRig)
            {
                rigReport = new BrackenRigReport(AccessTools.TypeByName("FlowermanAI"),
                    Path.Combine(Paths.ConfigPath, "GojoBracken"), PluginVersion,
                    message => Logger.LogInfo(message), message => Logger.LogWarning(message));
            }
            Logger.LogInfo($"{PluginName} {PluginVersion} loaded. Plugin foundation only; cosmetics are not implemented yet.");
        }

        private void Update()
        {
            diagnostics?.Update();
            rigReport?.Update(Time.unscaledTime);
        }
    }
}
