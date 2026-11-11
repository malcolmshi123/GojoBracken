using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildLocalPreview
{
    public static void Run()
    {
        if (Application.unityVersion != "2022.3.62f2") throw new InvalidOperationException("Pinned Unity Editor required.");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        var preview = new GameObject("Local prefab preview").AddComponent<LocalPrefabPreview>();
        preview.ModelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LocalModelStudy/NativeGojo/Gojo.prefab");
        preview.AuraPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GojoBracken/Prefabs/Aura.prefab");
        if (preview.ModelPrefab == null || preview.AuraPrefab == null) throw new InvalidOperationException("Native prefabs missing.");
        var camera = new GameObject("Preview camera").AddComponent<Camera>();
        camera.gameObject.AddComponent<AudioListener>();
        camera.rect = new Rect(.29f, 0f, .71f, 1f);
        camera.backgroundColor = new Color(.14f, .18f, .23f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.fieldOfView = 35f;
        preview.ViewCamera = camera;
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>());
        var material = new Material(Shader.Find("Standard")) { color = new Color(.55f, .6f, .65f) };
        var grid = new Texture2D(32, 32, TextureFormat.RGB24, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++) grid.SetPixel(x, y, x == 0 || y == 0 ? new Color(.72f, .72f, .72f) : Color.white);
        grid.Apply();
        AssetDatabase.CreateAsset(grid, "Assets/FloorGrid.asset");
        material.mainTexture = grid; material.mainTextureScale = Vector2.one * 10f;
        AssetDatabase.CreateAsset(material, "Assets/Floor.mat");
        floor.GetComponent<Renderer>().sharedMaterial = material;
        var light = new GameObject("Preview light").AddComponent<Light>();
        light.type = LightType.Directional; light.intensity = 1.2f;
        light.transform.rotation = Quaternion.Euler(40f, -35f, 0f);
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.18f, .18f, .18f);
        RenderSettings.reflectionIntensity = 0f;
        PlayerSettings.colorSpace = ColorSpace.Gamma;
        EditorSceneManager.SaveScene(scene, "Assets/LocalPreview.unity");
        PlayerSettings.companyName = "GojoBrackenLocalTests";
        PlayerSettings.productName = "GojoBracken Local Preview";
        PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 800;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.runInBackground = true;
        string output = Path.GetFullPath("../Player");
        Directory.CreateDirectory(output);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/LocalPreview.unity" }, locationPathName = Path.Combine(output, "GojoBrackenPreview.exe"),
            target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Player build failed: " + report.summary.result);
        Debug.Log("LOCAL PREVIEW BUILD PASS: " + output);
    }
}
