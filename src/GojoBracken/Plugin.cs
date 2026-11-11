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
        public const string PluginVersion = "0.1.37";

        private RuntimeDiagnostics diagnostics;
        private BrackenRigReport rigReport;
        private Coroutine observations;
        private DevelopmentEffects effects;
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

            bool enableEffects = Config.Bind("DevelopmentEffects", "Enabled", false,
                "Enable development cosmetics. Requires inspected v81 and a validated bundle. Model preview is a separate opt-in. Restart after changing.").Value;
            bool standingModel = Config.Bind("DevelopmentEffects", "StandingModelPreview", false,
                "Experimental Gojo model replacement with a fixed standing pose and vanilla restoration. Requires Enabled=true. Restart after changing.").Value;
            float saturation = Config.Bind("DevelopmentEffects", "AngerSaturation", 12f,
                "Positive cosmetic saturation threshold, not a game anger limit. Restart after changing.").Value;
            float strength = Config.Bind("DevelopmentEffects", "AuraStrength", 1f,
                "Aura intensity from 0 to 3; zero hides the aura but keeps voice events. Restart after changing.").Value;
            bool soloLiveTest = Config.Bind("DevelopmentEffects", "SoloLiveTest", false,
                "Start the private live attachment test after the solo gameplay camera renders.").Value;
            bool preview = Config.Bind("RenderPreview", "Enabled", false,
                "Enable local HDRP render checks via request files. Use a solo test profile.").Value;
            if (enableEffects || soloLiveTest || preview)
                Logger.LogInfo($"[Startup] Effective config: DevelopmentEffects.Enabled={enableEffects}, " +
                    $"StandingModelPreview={standingModel}, SoloLiveTest={soloLiveTest}, RenderPreview.Enabled={preview}. File={Config.ConfigFilePath}");
            if (enableEffects && soloLiveTest)
                ArmLiveTest(saturation, strength, standingModel);
            else if (enableEffects)
            {
                try
                {
                    effects = DevelopmentEffects.Start(AccessTools.TypeByName("FlowermanAI"),
                        Path.GetDirectoryName(Info.Location), saturation, strength,
                        message => Logger.LogInfo(message), message => Logger.LogWarning(message), standingModel);
                }
                catch (Exception exception) { Logger.LogWarning("[Effects] Disabled: " + exception.Message); }
            }
            if (preview)
            {
                try
                {
                    var enemyType = AccessTools.TypeByName("FlowermanAI");
                    if (enableEffects || Application.unityVersion != "2022.3.62f2"
                        || enemyType?.Module.ModuleVersionId != BrackenV81Presentation.SupportedMvid)
                        throw new InvalidOperationException("Requires inspected v81, Unity 2022.3.62f2 and DevelopmentEffects disabled.");
                    InGameRenderPreviewRunner.StartPreview(Path.GetDirectoryName(Info.Location),
                        InGameRenderPreview.BindSoloCamera(enemyType.Assembly),
                        message => Logger.LogInfo(message), message => Logger.LogWarning(message));
                }
                catch (Exception exception) { Logger.LogWarning("[RenderPreview] Disabled: " + exception.Message); }
            }

            Logger.LogInfo($"{PluginName} {PluginVersion} loaded. Development cosmetics are opt-in.");
            if (!enableDiagnostics && !exportRig && effects == null) return;

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
                effects?.Dispose();
                effects = null;
            }
        }

        private void ArmLiveTest(float saturation, float strength, bool standingModel)
        {
            var log = Logger;
            try
            {
                var enemyType = AccessTools.TypeByName("FlowermanAI");
                if (!standingModel || Application.unityVersion != "2022.3.62f2"
                    || enemyType?.Module.ModuleVersionId != BrackenV81Presentation.SupportedMvid)
                    throw new InvalidOperationException("Requires inspected v81, Unity 2022.3.62f2, and StandingModelPreview enabled.");
                var directory = Path.GetDirectoryName(Info.Location);
                InGameRenderPreviewRunner.StartLiveTest(InGameRenderPreview.BindSoloCamera(enemyType.Assembly),
                    () =>
                    {
                        try
                        {
                            return DevelopmentEffects.Start(enemyType, directory, saturation, strength,
                                message => log.LogInfo(message), message => log.LogWarning(message), true, true);
                        }
                        catch (Exception error)
                        {
                            log.LogWarning("[LiveTest] Attachment test could not start: " + error.Message);
                            return null;
                        }
                    }, message => log.LogInfo(message), message => log.LogWarning(message));
            }
            catch (Exception error) { log.LogWarning("[LiveTest] Disabled: " + error.Message); }
        }

        private IEnumerator Observe()
        {
            Logger.LogInfo("[Observer] First sample entered.");
            while (true)
            {
                float now = (float)clock.Elapsed.TotalSeconds;
                if (effects != null)
                {
                    try { effects.Scan(); }
                    catch (Exception exception)
                    {
                        effects.Dispose();
                        effects = null;
                        Logger.LogWarning("[Effects] Scanning stopped: " + exception.Message);
                    }
                }
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
            effects?.Dispose();
            effects = null;
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
