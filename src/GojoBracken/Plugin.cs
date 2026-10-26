using System;
using System.Collections;
using System.Diagnostics;
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
        public const string PluginVersion = "0.1.3";

        private RuntimeDiagnostics diagnostics;
        private BrackenRigReport rigReport;
        private Coroutine observations;
        private readonly Stopwatch clock = new Stopwatch();

        private void Awake()
        {
            bool enableDiagnostics = Config.Bind("Diagnostics", "Enabled", false,
                "Log runtime render settings and Bracken state changes for compatibility testing. Restart after changing.").Value;
            bool exportRig = Config.Bind("Diagnostics", "ExportBrackenRig", false,
                "Export a read-only report from an already-loaded Bracken prefab. Never spawns enemies. Restart after changing.").Value;
            if (enableDiagnostics) diagnostics = new RuntimeDiagnostics(Logger);
            if (exportRig)
                rigReport = new BrackenRigReport(AccessTools.TypeByName("FlowermanAI"),
                    Path.Combine(Paths.ConfigPath, "GojoBracken"), PluginVersion,
                    message => Logger.LogInfo(message), message => Logger.LogWarning(message));

            Logger.LogInfo($"{PluginName} {PluginVersion} loaded. Plugin foundation only; cosmetics are not implemented yet.");
            if (!enableDiagnostics && !exportRig) return;

            Logger.LogInfo($"[Observer] Starting; diagnostics={enableDiagnostics}; rigReport={exportRig}; enabled={enabled}; active={gameObject.activeInHierarchy}");
            clock.Start();
            try
            {
                // StartCoroutine runs the first sample immediately, rather than waiting for Update.
                observations = StartCoroutine(Observe());
            }
            catch (Exception exception)
            {
                Logger.LogWarning($"[Observer] Could not start: {exception.GetType().Name}: {exception.Message}");
                rigReport?.Cancel("observer could not start");
            }
        }

        private IEnumerator Observe()
        {
            Logger.LogInfo("[Observer] First sample entered.");
            while (true)
            {
                float now = (float)clock.Elapsed.TotalSeconds;
                if (diagnostics != null)
                {
                    try { diagnostics.Update(now); }
                    catch (Exception exception)
                    {
                        diagnostics = null;
                        Logger.LogWarning($"[Observer] Diagnostics stopped: {exception.GetType().Name}: {exception.Message}");
                    }
                }
                if (rigReport != null)
                {
                    try { rigReport.Update(now); }
                    catch (Exception exception)
                    {
                        Logger.LogWarning($"[Observer] Rig reporter stopped: {exception.GetType().Name}: {exception.Message}");
                        rigReport.Cancel("report callback failed");
                    }
                }
                yield return new WaitForSecondsRealtime(0.5f);
            }
        }

        private void StopObservations(string reason)
        {
            if (observations == null) return;
            StopCoroutine(observations);
            observations = null;
            clock.Stop();
            rigReport?.Cancel(reason);
            Logger.LogInfo("[Observer] Stopped: " + reason);
        }

        private void OnDisable() { StopObservations("plugin disabled"); }
        private void OnDestroy() { StopObservations("plugin destroyed"); }
        private void OnApplicationQuit() { StopObservations("game closing"); }
    }
}
