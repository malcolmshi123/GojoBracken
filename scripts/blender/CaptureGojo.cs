using System;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public static class CaptureGojo
{
    public static void Run()
    {
        GojoModelChecks.Require(GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset, "HDRP pipeline is not active");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        var bundle = AssetBundle.LoadFromFile(Path.Combine(GojoModelChecks.Output, "bundle", "gojo-model"));
        GojoModelChecks.Require(bundle != null, "Model bundle is missing");
        try
        {
            var root = UnityEngine.Object.Instantiate(bundle.LoadAsset<GameObject>("gojo"));
            root.GetComponent<Animator>().enabled = false;
            var camera = new GameObject("Camera").AddComponent<Camera>();
            camera.orthographic = true;
            camera.nearClipPlane = .01f;
            camera.farClipPlane = 30;
            var data = camera.gameObject.AddComponent<HDAdditionalCameraData>();
            data.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
            data.backgroundColorHDR = new Color(.09f, .09f, .09f);
            var volume = new GameObject("Exposure").AddComponent<Volume>(); volume.isGlobal = true;
            volume.sharedProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            var exposure = volume.sharedProfile.Add<Exposure>(true); exposure.mode.value = ExposureMode.Fixed; exposure.fixedExposure.value = 6;
            Light("Key", new Vector3(40, -30, 0), 300);
            Light("Fill", new Vector3(25, 40, 0), 100);
            Light("Rim", new Vector3(35, 150, 0), 150);
            var target = new RenderTexture(640, 800, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            string output = Path.Combine(GojoModelChecks.Output, "previews"); Directory.CreateDirectory(output);
            Vector3 center = new Vector3(-.016415f, 1.05f, 0);
            Render(camera, target, output, "front", center, Vector3.back, 2.52f);
            Render(camera, target, output, "side", center, Vector3.left, 2.52f);
            Render(camera, target, output, "back", center, Vector3.forward, 2.52f);
            Render(camera, target, output, "three-quarter", center, new Vector3(-1, 0, -1), 2.52f);
            Render(camera, target, output, "hand-contact", new Vector3(center.x, .96f, 0), Vector3.left, .43f);
            UnityEngine.Object.DestroyImmediate(target);
            Debug.Log("GOJO HDRP PREVIEWS PASS: " + output);
        }
        finally { bundle.Unload(true); }
    }
    private static void Light(string name, Vector3 angles, float lux)
    {
        var light = new GameObject(name).AddComponent<Light>(); light.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(angles);
        light.gameObject.AddComponent<HDAdditionalLightData>().SetIntensity(lux, LightUnit.Lux);
    }
    private static void Render(Camera camera, RenderTexture target, string output, string name, Vector3 center, Vector3 direction, float height)
    {
        camera.transform.position = center + direction.normalized * 4;
        camera.transform.LookAt(center);
        camera.orthographicSize = height / 2;
        camera.Render(); camera.Render();
        var previous = RenderTexture.active;
        var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        try
        {
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
            File.WriteAllBytes(Path.Combine(output, name + ".png"), image.EncodeToPNG());
        }
        finally { RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(image); }
    }
}
