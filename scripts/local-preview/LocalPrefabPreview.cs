using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using GojoBracken;
using UnityEngine;

// Standalone test application only; never installed into Lethal Company.
public sealed class LocalPrefabPreview : MonoBehaviour
{
    public GameObject ModelPrefab;
    public GameObject AuraPrefab;
    public Camera ViewCamera;
    private GameObject anchor;
    private GameObject model;
    private BrackenCosmeticController controller;
    private AudioClip tone;
    private float anger, strength = 1f, yaw = 30f, distance = 6.2f;
    private int phase;
    private bool remote, killing, dead, bodyVisible = true, testTone, autoTesting;
    private string failure;
    private readonly List<string> passed = new List<string>();
    private readonly string runId = Guid.NewGuid().ToString("N");
    private string ReportPath => Path.Combine(Application.persistentDataPath, "preview-report.json");

    [Serializable] private sealed class Report
    {
        public int schemaVersion = 1;
        public string kind = "standalone-prefab-preview", runId, generatedUtc, unityVersion, gpu, graphicsApi;
        public string buildInfo, error, phase, screenshot;
        public bool automated, smokePassed, bodyVisible, remoteApproximation, originalVoiceIncluded = false;
        public int modelSkins, voiceRequests;
        public float anger, strength, auraLevel;
        public string colorSpace, skinWeights;
        public string[] checks;
    }

    private void Start()
    {
        autoTesting = Array.IndexOf(Environment.GetCommandLineArgs(), "--smoke-test") >= 0;
        try
        {
            tone = AudioClip.Create("Optional synthetic test tone - NOT Gojo voice", 24000, 1, 48000, false);
            var samples = new float[24000];
            for (int i = 0; i < samples.Length; i++)
                samples[i] = Mathf.Sin(2f * Mathf.PI * 440f * i / 48000f) * .08f * Mathf.Sin(Mathf.PI * i / samples.Length);
            tone.SetData(samples, 0);
            ResetPreview();
            WriteReport(false);
            if (autoTesting) StartCoroutine(SmokeTest());
        }
        catch (Exception exception) { Fail(exception); }
    }

    private void Update()
    {
        if (failure != null) return;
        try
        {
            ViewCamera.transform.position = new Vector3(Mathf.Sin(yaw * Mathf.Deg2Rad) * distance, 2.4f,
                -Mathf.Cos(yaw * Mathf.Deg2Rad) * distance);
            ViewCamera.transform.LookAt(new Vector3(0f, 1.1f, 0f));
            AudioListener.volume = testTone ? 1f : 0f;
            if (!autoTesting) Step(Time.unscaledDeltaTime);
        }
        catch (Exception exception) { Fail(exception); }
    }

    private void Step(float delta)
    {
        controller.SetVisibility(new BrackenVisibility(bodyVisible, 0));
        controller.Apply(new BrackenObservation(phase == 3 ? 1 : phase, anger * 12f, phase == 2,
            phase == 3, killing, dead), !remote, delta);
        foreach (var renderer in model.GetComponentsInChildren<Renderer>(true)) renderer.enabled = bodyVisible && !dead;
    }

    private void ResetPreview()
    {
        if (anchor != null) { anchor.SetActive(false); Destroy(anchor); }
        anchor = new GameObject("Simulated enemy anchor");
        model = Instantiate(ModelPrefab, anchor.transform, false);
        if (!BrackenCosmeticController.TryCreate(anchor.transform, BrackenV81Presentation.SupportedMvid,
            AuraPrefab, tone, 12f, strength, out controller, out var error)) throw new InvalidOperationException(error);
        anger = 0f; phase = 0; remote = killing = dead = false; bodyVisible = true;
        Step(0f);
    }

    private IEnumerator SmokeTest()
    {
        yield return null;
        try
        {
            Require(SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null, "graphics device active");
            var skins = model.GetComponentsInChildren<SkinnedMeshRenderer>();
            Require(skins.Length == 4, "four native Gojo skins loaded");
            var animator = model.GetComponent<Animator>();
            Require(animator != null && animator.runtimeAnimatorController != null && !animator.applyRootMotion
                && animator.runtimeAnimatorController.animationClips.Length == 1
                && animator.runtimeAnimatorController.animationClips[0].name == "StandingIdle", "native idle, no dance/root motion");
            foreach (var renderer in anchor.GetComponentsInChildren<Renderer>(true))
                foreach (var material in renderer.sharedMaterials)
                    Require(material != null && material.shader != null && material.shader.isSupported
                        && material.shader.name != "Hidden/InternalErrorShader", "supported material: " + renderer.name);
            Step(10f);
            Require(Mathf.Abs(controller.LastFrame.AuraLevel - .15f) < .0001f, "faint hunting baseline");
            anger = 1f; Step(10f);
            Require(Mathf.Abs(controller.LastFrame.AuraLevel - 1f) < .0001f, "full aura");
            phase = 2; Step(.1f); Step(.1f);
            Require(controller.VoiceRequests == 1, "one aggro voice request (synthetic tone only)");
            phase = 1; Step(.1f); phase = 2; Step(.1f);
            Require(controller.VoiceRequests == 2, "aggro reentry");
            phase = 3; Step(.1f);
            Require(controller.VoiceRequests == 2, "corpse overlay adds no voice");
            remote = true; phase = 0; Step(10f);
            Require(Mathf.Abs(controller.LastFrame.AuraLevel - .15f) < .0001f, "remote approximation");
            bodyVisible = false; Step(.1f);
            var effects = anchor.transform.Find("GojoBracken cosmetics");
            foreach (var renderer in effects.GetComponentsInChildren<Renderer>(true))
                Require(!renderer.enabled, "simulated visibility mask");
            bodyVisible = true; remote = false; phase = 2; dead = true; Step(.1f);
            Require(!effects.gameObject.activeSelf, "death stops effects");
            ResetPreview();
            Require(controller.VoiceRequests == 0, "reset clears voice counter");
            anger = 1f; Step(10f);
        }
        catch (Exception exception) { Fail(exception); yield break; }
        yield return null;
        try
        {
            foreach (var system in anchor.GetComponentsInChildren<ParticleSystem>(true))
                system.Simulate(1.4f, false, true, false);
            Capture();
            WriteReport(true);
            Debug.Log("LOCAL PREFAB PLAYER PASS: " + ReportPath);
            Application.Quit(0);
        }
        catch (Exception exception) { Fail(exception); }
    }

    private void Require(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException(label);
        passed.Add(label);
    }

    private void Capture()
    {
        Directory.CreateDirectory(Application.persistentDataPath);
        var target = new RenderTexture(960, 640, 24);
        var pixels = new Texture2D(960, 640, TextureFormat.RGB24, false);
        var oldTarget = ViewCamera.targetTexture;
        var oldRect = ViewCamera.rect;
        var oldActive = RenderTexture.active;
        try
        {
            ViewCamera.rect = new Rect(0f, 0f, 1f, 1f);
            ViewCamera.targetTexture = target;
            ViewCamera.Render();
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 960, 640), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(Path.Combine(Application.persistentDataPath, "preview-render.png"), pixels.EncodeToPNG());
        }
        finally
        {
            ViewCamera.targetTexture = oldTarget; ViewCamera.rect = oldRect; RenderTexture.active = oldActive;
            target.Release(); Destroy(target); Destroy(pixels);
        }
    }

    private void WriteReport(bool smokePassed)
    {
        Directory.CreateDirectory(Application.persistentDataPath);
        var info = Resources.Load<TextAsset>("build-info");
        var report = new Report {
            runId = runId, generatedUtc = DateTime.UtcNow.ToString("o"), unityVersion = Application.unityVersion,
            gpu = SystemInfo.graphicsDeviceName, graphicsApi = SystemInfo.graphicsDeviceType.ToString(),
            buildInfo = info != null ? info.text : "unavailable", error = failure, automated = autoTesting,
            smokePassed = smokePassed, phase = controller != null ? controller.LastFrame.Phase.ToString() : "unavailable",
            bodyVisible = bodyVisible, remoteApproximation = remote, modelSkins = model != null ? model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length : 0,
            voiceRequests = controller != null ? controller.VoiceRequests : 0, anger = anger, strength = strength,
            auraLevel = controller != null ? controller.LastFrame.AuraLevel : 0f, checks = passed.ToArray(),
            colorSpace = QualitySettings.activeColorSpace.ToString(), skinWeights = QualitySettings.skinWeights.ToString(),
            screenshot = "preview-render.png"
        };
        File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
    }

    private void Fail(Exception exception)
    {
        failure = exception.ToString();
        Debug.LogException(exception);
        WriteReport(false);
        if (autoTesting) Application.Quit(1);
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(12f, 12f, 340f, Screen.height - 24f), GUI.skin.box);
        GUILayout.Label("GojoBracken - LOCAL PREFAB PREVIEW");
        GUILayout.Label("Actual native prefabs; simulated enemy inputs.");
        GUILayout.Label("No game integration or Japanese voice included.");
        if (failure != null) GUILayout.Label("FAILED: " + failure);
        else if (controller != null)
        {
            GUI.enabled = !autoTesting && !dead;
            if (GUILayout.Button("Hunting")) phase = 0;
            if (GUILayout.Button("Retreat")) phase = 1;
            if (GUILayout.Button("Aggro")) phase = 2;
            if (GUILayout.Button("Corpse dragging")) phase = 3;
            GUILayout.Label("Anger: " + Mathf.RoundToInt(anger * 100f) + "%");
            anger = GUILayout.HorizontalSlider(anger, 0f, 1f);
            GUILayout.Label("Strength: " + strength.ToString("0.0") + " (change then Reset)");
            strength = GUILayout.HorizontalSlider(strength, 0f, 3f);
            remote = GUILayout.Toggle(remote, "Remote phase approximation");
            killing = GUILayout.Toggle(killing, "Kill animation overlay");
            bodyVisible = GUILayout.Toggle(bodyVisible, "Simulated body visibility");
            testTone = GUILayout.Toggle(testTone, "Enable synthetic test tone - NOT Gojo voice");
            GUILayout.Label("Voice requests: " + controller.VoiceRequests);
            if (GUILayout.Button("Simulate death")) { dead = true; Step(0f); }
            GUI.enabled = !autoTesting;
            if (GUILayout.Button("Reset")) ResetPreview();
            GUILayout.Label("View angle"); yaw = GUILayout.HorizontalSlider(yaw, -180f, 180f);
            GUILayout.Label("View distance"); distance = GUILayout.HorizontalSlider(distance, 4f, 9f);
            if (GUILayout.Button("Save report + render")) { Capture(); WriteReport(false); }
        }
        GUI.enabled = true;
        GUILayout.Label("Report folder:\n" + Application.persistentDataPath);
        if (GUILayout.Button("Quit")) Application.Quit();
        GUILayout.EndArea();
    }
}
