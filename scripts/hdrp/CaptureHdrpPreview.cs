#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace GojoBracken
{
    public static class CaptureHdrpPreview
    {
        public static void Run()
        {
            if (!(GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset)
                || QualitySettings.activeColorSpace != ColorSpace.Linear
                || SystemInfo.graphicsDeviceType != GraphicsDeviceType.Direct3D11)
                throw new InvalidOperationException("Use the HDRP candidate project in Linear color space with Direct3D 11.");

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            var model = Load("Gojo");
            var aura = Load("Aura");
            var anchor = new GameObject("Preview anchor");
            UnityEngine.Object.Instantiate(model, anchor.transform);
            var camera = new GameObject("Capture camera").AddComponent<Camera>();
            camera.transform.position = new Vector3(3.1f, 2.2f, -5.4f);
            camera.transform.LookAt(new Vector3(0f, 1.35f, 0f));
            camera.fieldOfView = 33f;
            var cameraData = camera.gameObject.AddComponent<HDAdditionalCameraData>();
            cameraData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
            cameraData.backgroundColorHDR = new Color(.035f, .05f, .075f);

            var volume = new GameObject("Fixed exposure").AddComponent<Volume>();
            volume.isGlobal = true;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.sharedProfile = profile;
            var exposure = profile.Add<Exposure>(true);
            exposure.mode.value = ExposureMode.Fixed;
            exposure.fixedExposure.value = 6f;
            profile.Add<Bloom>(true).intensity.value = 0f;
            Light("Key", new Vector3(40f, 20f, 0f), 70f);
            Light("Fill", new Vector3(22f, -35f, 0f), 30f);
            Light("Rim", new Vector3(38f, 145f, 0f), 15f);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("HDRP/Lit"));
            floor.transform.localScale = Vector3.one * .65f;
            string output = Path.Combine(Directory.GetParent(Application.dataPath).Parent.FullName, "renders");
            Directory.CreateDirectory(output);
            var tone = AudioClip.Create("Silent preview fixture", 480, 1, 48000, false);
            if (!BrackenCosmeticController.TryCreate(anchor.transform, BrackenV81Presentation.SupportedMvid,
                aura, tone, 12f, 1f, out var controller, out var error))
                throw new InvalidOperationException(error);

            var target = new RenderTexture(960, 720, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            camera.Render();
            for (int i = 0; i < 2; i++)
            {
                controller.Apply(new BrackenObservation(0, i * 12f, false, false, false, false), true, 10f);
                foreach (var system in anchor.GetComponentsInChildren<ParticleSystem>())
                {
                    system.useAutoRandomSeed = false;
                    system.randomSeed = 42;
                    system.Simulate(1.4f, false, true, false);
                }
                camera.Render();
                Save(target, Path.Combine(output, i == 0 ? "hdrp-calm.png" : "hdrp-full.png"));
            }
            Debug.Log("HDRP CAPTURE PASS: " + output);
        }

        private static GameObject Load(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/HdrpCandidates/" + name + ".prefab");
            if (prefab == null) throw new InvalidOperationException("Missing HDRP candidate prefab: " + name);
            return prefab;
        }

        private static void Light(string name, Vector3 angles, float lux)
        {
            var light = new GameObject(name).AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(angles);
            var data = light.gameObject.AddComponent<HDAdditionalLightData>();
            data.SetIntensity(lux, LightUnit.Lux);
        }

        private static void Save(RenderTexture target, string path)
        {
            var previous = RenderTexture.active;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(image);
            }
        }
    }
}
#endif
