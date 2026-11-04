using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using GojoBracken;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class ObserverLifecycleProbe : MonoBehaviour
{
    public GameObject LoadedPrefab;
    private readonly CaptureLogs capture = new CaptureLogs();
    private Plugin plugin;
    private Camera soloPreviewCamera;

    private void Start()
    {
        try
        {
            string project = Directory.GetParent(Application.dataPath).FullName;
            Directory.SetCurrentDirectory(project);
            typeof(Paths).GetMethod("SetExecutablePath", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { Path.Combine(project, "fixture.exe"), Path.Combine(project, "BepInEx"), null, new string[0] });
            File.WriteAllText(Path.Combine(project, Plugin.PluginGuid + ".cfg"),
                "[Diagnostics]" + Environment.NewLine + "Enabled = true" + Environment.NewLine + "ExportBrackenRig = true");
            BepInEx.Logging.Logger.Listeners.Add(capture);
            Time.timeScale = 0f;
            var host = new GameObject("BepInEx_Manager");
            host.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(host);
            plugin = host.AddComponent<Plugin>();
            StartCoroutine(Check());
        }
        catch (Exception exception) { Fail(exception); }
    }

    private IEnumerator Check()
    {
        yield return new WaitForSecondsRealtime(1f);
        try
        {
            Require(capture.Lines.Any(line => line.Contains("[Observer] First sample entered.")), "Awake starts real coroutine");
            Require(capture.Lines.Any(line => line.Contains("[Diagnostics] Render pipeline=")), "pipeline sampled");
            Require(capture.Lines.Any(line => line.Contains("[RigReport] Exported loaded prefab report:")), "prefab exported");
            Require(UnityEngine.Object.FindObjectsOfType<FlowermanAI>().Length == 0, "no enemy spawned");
            Debug.Log("OBSERVER LIFECYCLE PASS: real Plugin Awake, paused-time coroutine, pipeline, report, no spawn");
            string previewDirectory = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "PreviewFixture");
            Directory.CreateDirectory(previewDirectory);
            var runner = typeof(Plugin).Assembly.GetType("GojoBracken.InGameRenderPreviewRunner", true);
            runner.GetMethod("StartPreview", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null,
                new object[] { previewDirectory, (Func<Camera>)(() => soloPreviewCamera),
                    (Action<string>)(message => capture.Lines.Add(message)),
                    (Action<string>)(message => capture.Lines.Add(message)) });
            plugin.enabled = false;
            Require(capture.Lines.Any(line => line.Contains("[Observer] Stopped: plugin disabled")), "disable accounted for");
            Debug.Log("OBSERVER LIFECYCLE PASS: disable logged");
            File.WriteAllText(Path.Combine(Directory.GetParent(Application.dataPath).FullName, Plugin.PluginGuid + ".cfg"),
                "[Diagnostics]" + Environment.NewLine + "Enabled = false" + Environment.NewLine + "ExportBrackenRig = false");
            int startCount = capture.Lines.Count(line => line.Contains("[Observer] Starting;"));
            new GameObject("DisabledOptionsHost").AddComponent<Plugin>();
            Require(capture.Lines.Count(line => line.Contains("[Observer] Starting;")) == startCount, "no observer when options disabled");
            Debug.Log("OBSERVER LIFECYCLE PASS: disabled options do not start observer");
            File.WriteAllText(Path.Combine(Directory.GetParent(Application.dataPath).FullName, Plugin.PluginGuid + ".cfg"),
                "[Diagnostics]\nEnabled = false\nExportBrackenRig = false\n[DevelopmentEffects]\nEnabled = true");
            new GameObject("VersionRejectedEffectsHost").AddComponent<Plugin>();
            Require(capture.Lines.Any(line => line.Contains("[Effects] Disabled: FlowermanAI does not match")),
                "actual Release DLL rejects synthetic game module before loading assets");
            Require(capture.Lines.Count(line => line.Contains("[Observer] Starting;")) == startCount,
                "rejected effects start no scan when diagnostics disabled");
            Debug.Log("OBSERVER LIFECYCLE PASS: effects opt-in rejects wrong module without starting scans");
            StartCoroutine(CheckPreviewRunner(previewDirectory));
        }
        catch (Exception exception) { Fail(exception); }
    }

    private IEnumerator CheckPreviewRunner(string directory)
    {
        string request = Path.Combine(directory, "gojobracken-preview.request");
        File.WriteAllText(request, "clear");
        var runnerType = typeof(Plugin).Assembly.GetType("GojoBracken.InGameRenderPreviewRunner", true);
        var onCameraRendered = (Action<ScriptableRenderContext, Camera>)runnerType
            .GetField("cameraRendered", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        var camera = new GameObject("Late gameplay camera").AddComponent<Camera>();
        onCameraRendered(default, camera);
        yield return new WaitForSecondsRealtime(.6f);
        try
        {
            Require(GameObject.Find("GojoBracken render preview runner") == null && File.Exists(request),
                "startup camera cannot create the runner or consume its request");
            soloPreviewCamera = camera;
            onCameraRendered(default, camera);
        }
        catch (Exception exception) { Fail(exception); yield break; }

        yield return new WaitForSecondsRealtime(.6f);
        try
        {
            Require(capture.Lines.Any(line => line.EndsWith("watching: " + request, StringComparison.Ordinal)),
                "gameplay camera starts a runner on the exact request path");
            Require(capture.Lines.Any(line => line.Contains(" poll 1, active scene="))
                && !File.Exists(request), "runner polls after the gameplay camera becomes active");
            var runner = GameObject.Find("GojoBracken render preview runner");
            Require(runner != null, "preview runner exists after its first poll");
            Destroy(runner);
        }
        catch (Exception exception) { Fail(exception); yield break; }

        yield return new WaitForSecondsRealtime(.6f);
        try
        {
            Require(capture.Lines.Any(line => line.Contains(" disabled after "))
                && capture.Lines.Any(line => line.Contains(" destroyed after ")), "runner teardown is logged");
            File.WriteAllText(request, "clear");
            onCameraRendered(default, camera);
        }
        catch (Exception exception) { Fail(exception); yield break; }

        yield return new WaitForSecondsRealtime(.6f);
        try
        {
            Require(!File.Exists(request) && capture.Lines.Count(line => line.Contains(" poll 1, active scene=")) == 2,
                "a later gameplay render recreates the destroyed runner and polls again");
        }
        catch (Exception exception) { Fail(exception); yield break; }

        Debug.Log("OBSERVER LIFECYCLE PASS: late solo camera starts and recreates a polling preview runner.");
        Time.timeScale = 1f;
        UnityEditor.EditorApplication.Exit(0);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Fail(Exception exception)
    {
        Debug.LogError("OBSERVER LIFECYCLE FAIL: " + exception);
        UnityEditor.EditorApplication.Exit(1);
    }

    private sealed class CaptureLogs : ILogListener
    {
        public readonly List<string> Lines = new List<string>();
        public void LogEvent(object sender, LogEventArgs args)
        {
            string line = Convert.ToString(args.Data);
            Lines.Add(line);
            Debug.Log("PLUGIN LOG: " + line);
        }
        public void Dispose() { }
    }
}
