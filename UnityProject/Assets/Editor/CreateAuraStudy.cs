using System;
using System.IO;
using GojoBracken.Authoring;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GojoBracken.Editor
{
    public static class CreateAuraStudy
    {
        private const string Folder = "Assets/GojoBrackenPreview";
        private const string ScenePath = Folder + "/AuraStudy.unity";
        private const string PrefabPath = Folder + "/AuraStudy.prefab";
        private const string MaterialPath = Folder + "/AuraStudy.mat";
        private const string TexturePath = Folder + "/AuraDot.png";
        private const string BodyMaterialPath = Folder + "/StandIn.mat";
        private const string FloorMaterialPath = Folder + "/Floor.mat";

        [MenuItem("GojoBracken/Create native aura study")]
        public static void Create()
        {
            if (Application.unityVersion != BuildGojoAssets.RequiredUnityVersion)
                throw new BuildFailedException("Use Unity " + BuildGojoAssets.RequiredUnityVersion + ".");
            foreach (string path in new[] { ScenePath, PrefabPath, MaterialPath, TexturePath, BodyMaterialPath, FloorMaterialPath })
                if (File.Exists(path))
                    throw new BuildFailedException("Study output already exists: " + path + ". Open the existing study; creation will not overwrite it.");

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets", "GojoBrackenPreview");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var particleShader = Shader.Find("Legacy Shaders/Particles/Additive");
            var bodyShader = Shader.Find("Standard");
            if (particleShader == null || bodyShader == null)
                throw new BuildFailedException("Built-in authoring shaders were not found. This study does not select an HDRP package.");

            CreateDotTexture();
            var particleMaterial = new Material(particleShader) { name = "AuraStudy" };
            particleMaterial.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            AssetDatabase.CreateAsset(particleMaterial, MaterialPath);
            var bodyMaterial = new Material(bodyShader) { color = new Color(0.12f, 0.20f, 0.29f), name = "StandIn" };
            var floorMaterial = new Material(bodyShader) { color = new Color(0.55f, 0.60f, 0.65f), name = "Floor" };
            AssetDatabase.CreateAsset(bodyMaterial, BodyMaterialPath);
            AssetDatabase.CreateAsset(floorMaterial, FloorMaterialPath);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Neutral stand-in - NOT Gojo";
            body.transform.position = new Vector3(0f, 1.05f, 0f);
            body.GetComponent<Renderer>().sharedMaterial = bodyMaterial;
            UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Study floor";
            floor.transform.localScale = Vector3.one * 0.6f;
            floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;
            UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>());

            var auraObject = new GameObject("Aura study - authoring only");
            var aura = auraObject.AddComponent<ParticleSystem>();
            AuraStudyController.ConfigureFlames(aura);
            aura.GetComponent<ParticleSystemRenderer>().sharedMaterial = particleMaterial;
            PrefabUtility.SaveAsPrefabAsset(auraObject, PrefabPath);

            var controlObject = new GameObject("Preview controls - editor study only");
            var controller = controlObject.AddComponent<AuraStudyController>();
            controller.Aura = aura;
            controller.Phase = AuraStudyController.PreviewPhase.Hunting;
            controller.VoiceSource = controlObject.AddComponent<AudioSource>();
            controller.VoiceSource.playOnAwake = false;
            controller.VoiceSource.loop = false;
            controller.VoiceSource.spatialBlend = 0f;
            controller.VoiceSource.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/GojoBracken/Audio/DomainExpansion.wav");

            var cameraObject = new GameObject("Study camera");
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<AudioListener>();
            camera.transform.position = new Vector3(3.3f, 2.4f, -5.3f);
            camera.transform.LookAt(new Vector3(0f, 1.1f, 0f));
            camera.backgroundColor = new Color(0.14f, 0.18f, 0.23f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.fieldOfView = 35f;
            var light = new GameObject("Study light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.transform.rotation = Quaternion.Euler(40f, -35f, 0f);
            RenderSettings.ambientLight = new Color(0.65f, 0.70f, 0.78f);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new BuildFailedException("Could not save aura study scene.");
            AssetDatabase.SaveAssets();
            Debug.Log("Native aura study created: " + ScenePath + ". This is not a release prefab or game compatibility result.");
        }

        [MenuItem("GojoBracken/Refresh study flame aura")]
        public static void RefreshFlames()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                AuraStudyController.ConfigureFlames(root.GetComponent<ParticleSystem>());
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var controller = UnityEngine.Object.FindObjectOfType<AuraStudyController>();
            AuraStudyController.ConfigureFlames(controller.Aura);
            controller.ApplyAura();
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Study flame aura refreshed.");
        }

        private static void CreateDotTexture()
        {
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    float radius = new Vector2((x - 15.5f) / 15.5f, (y - 15.5f) / 15.5f).magnitude;
                    float alpha = Mathf.Pow(Mathf.Clamp01(1f - radius), 2f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            texture.Apply();
            File.WriteAllBytes(TexturePath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        // Run in batch mode with graphics enabled; writes only an ignored authoring screenshot.
        public static void Capture()
        {
            if (!Application.isBatchMode)
                throw new BuildFailedException("Capture is a batch-only verification helper.");
            EditorSceneManager.OpenScene(ScenePath);
            var controller = UnityEngine.Object.FindObjectOfType<AuraStudyController>();
            var camera = Camera.main;
            if (controller == null || camera == null)
                throw new BuildFailedException("Study controller or camera is missing.");
            AuraStudyController.ConfigureFlames(controller.Aura);
            controller.Anger = 1f;
            controller.ApplyAura();
            controller.Aura.Simulate(1.4f, true, true, false);
            var target = new RenderTexture(960, 640, 24);
            var image = new Texture2D(960, 640, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 960, 640), 0, 0);
                image.Apply();
                string repository = Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;
                string output = Path.Combine(repository, "artifacts", "assets", "native-aura-study.png");
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                File.WriteAllBytes(output, image.EncodeToPNG());
                Debug.Log("Native aura study captured: " + output);
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(image);
            }
        }
    }
}

