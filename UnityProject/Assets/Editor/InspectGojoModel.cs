using System;
using System.IO;
using System.Linq;
using GojoBracken.Authoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GojoBracken.Editor
{
    public static class InspectGojoModel
    {
        private const float TargetPreviewHeight = 2.1f;
        private const string ModelPath = "Assets/LocalModelStudy/source/gojo_menu.gltf";
        private const string ScenePath = "Assets/LocalModelStudy/GojoIdleStudy.unity";

        [MenuItem("GojoBracken/Create local Gojo idle study")]
        public static void CreateStudy()
        {
            if (Application.unityVersion != BuildGojoAssets.RequiredUnityVersion)
                throw new InvalidOperationException("Use Unity " + BuildGojoAssets.RequiredUnityVersion + ".");
            if (Application.isPlaying)
                throw new InvalidOperationException("Stop Play mode before creating a model study.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            if (File.Exists(ScenePath))
                throw new InvalidOperationException("The local study already exists. Open " + ScenePath + "; creation will not overwrite it.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (asset == null) throw new InvalidOperationException("glTF did not import as a GameObject: " + ModelPath);
            var scene = EditorSceneManager.OpenScene("Assets/GojoBrackenPreview/AuraStudy.unity");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            model.name = "Gojo candidate - local evaluation";
            var meshes = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (meshes.Length != 4) throw new InvalidOperationException("Expected four skinned meshes; found " + meshes.Length);
            foreach (var renderer in meshes)
            {
                if (renderer.sharedMesh == null || renderer.bones.Length == 0 || renderer.bones.Any(bone => bone == null))
                    throw new InvalidOperationException("Invalid skin: " + renderer.name);
                foreach (var material in renderer.sharedMaterials)
                    if (material == null || material.shader == null || material.shader.name.Contains("InternalError"))
                        throw new InvalidOperationException("Missing material/shader: " + renderer.name);
                Debug.Log("MODEL SKIN: " + renderer.name + "; vertices=" + renderer.sharedMesh.vertexCount
                    + "; bones=" + renderer.bones.Length + "; bindposes=" + renderer.sharedMesh.bindposes.Length);
            }
            FitStandingModel(model);
            var standIn = GameObject.Find("Neutral stand-in - NOT Gojo");
            if (standIn != null) standIn.SetActive(false);
            Debug.Log("MODEL IDLE: upright spine, legs straightened, arms relaxed, soles aligned and grounded, dance disabled.");
            // Save the study without baking the generated flame shell into the scene.
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Could not save local model study.");
            var controller = UnityEngine.Object.FindObjectOfType<AuraStudyController>();
            AuraStudyController.ConfigureFlames(controller.Aura);
            controller.Anger = 0.7f;
            controller.ApplyAura();
            controller.Aura.Simulate(1.4f, true, true, false);
            Capture(Camera.main);
            Debug.Log("MODEL STUDY COMPLETE: " + ScenePath);
        }

        [MenuItem("GojoBracken/Refresh local Gojo idle pose")]
        public static void RefreshIdlePose()
        {
            if (Application.unityVersion != BuildGojoAssets.RequiredUnityVersion || Application.isPlaying)
                throw new InvalidOperationException("Use the pinned Editor outside Play mode.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var model = GameObject.Find("Gojo candidate - local evaluation");
            if (model == null) throw new InvalidOperationException("Gojo candidate is missing from the idle study.");
            FitStandingModel(model);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Could not save the corrected idle pose.");
            Debug.Log("IDLE REFRESH COMPLETE: upright spine, aligned soles, both feet grounded.");
        }

        private static void FitStandingModel(GameObject model)
        {
            GojoIdlePose.Apply(model);
            Bounds bounds = GojoIdlePose.VisibleBounds(model);
            if (bounds.size.y <= 0.001f) throw new InvalidOperationException("Model bounds are empty.");
            model.transform.localScale *= TargetPreviewHeight / bounds.size.y;
            bounds = GojoIdlePose.VisibleBounds(model);
            model.transform.position += new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
            GojoIdlePose.GroundFeet(model);
            bounds = GojoIdlePose.VisibleBounds(model);
            if (Mathf.Abs(bounds.size.y - TargetPreviewHeight) > 0.002f)
                throw new InvalidOperationException("Model did not fit the target preview height.");
        }

        // Run in an isolated project with -executeMethod for regression verification.
        public static void VerifyIdleStudy()
        {
            if (!Application.isBatchMode)
                throw new InvalidOperationException("Run this verification in batch mode on a project copy.");
            Vector3 previousScale = Vector3.zero;
            for (int pass = 0; pass < 3; pass++)
            {
                RefreshIdlePose();
                var model = GameObject.Find("Gojo candidate - local evaluation");
                Bounds bounds = GojoIdlePose.VisibleBounds(model);
                if (Mathf.Abs(bounds.size.y - TargetPreviewHeight) > 0.002f || Mathf.Abs(bounds.min.y) > 0.001f)
                    throw new InvalidOperationException("Idle height or floor contact changed after refresh.");
                if (pass > 0 && (model.transform.localScale - previousScale).sqrMagnitude > 0.000001f)
                    throw new InvalidOperationException("Repeated refresh changed model scale.");
                previousScale = model.transform.localScale;
                Debug.Log("IDLE SCALE PASS " + (pass + 1) + ": height=" + bounds.size.y);
            }
            var controller = UnityEngine.Object.FindObjectOfType<AuraStudyController>();
            AuraStudyController.ConfigureFlames(controller.Aura);
            controller.Anger = 1f;
            controller.ApplyAura();
            controller.Aura.Simulate(1.4f, true, true, false);
            var envelope = controller.Aura.GetComponentInChildren<FlameEnvelope>();
            if (envelope == null) throw new InvalidOperationException("Flame envelope is missing.");
            float tip = envelope.GetComponent<MeshFilter>().sharedMesh.vertices
                .Max(vertex => envelope.transform.TransformPoint(vertex).y);
            var standing = GojoIdlePose.VisibleBounds(GameObject.Find("Gojo candidate - local evaluation"));
            if (tip < standing.max.y + 0.2f)
                throw new InvalidOperationException("Maximum aura does not extend above the model.");
            Debug.Log("IDLE AURA SCALE PASS: modelTop=" + standing.max.y + "; flameTip=" + tip);
            Capture(Camera.main);
        }

        private static void Capture(Camera camera)
        {
            string repository = Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;
            string output = Path.Combine(repository, "artifacts", "assets", "gojo-idle-study.png");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
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
                File.WriteAllBytes(output, image.EncodeToPNG());
                Debug.Log("MODEL CAPTURE: " + output);
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
