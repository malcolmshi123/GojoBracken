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
        private const string ModelPath = "Assets/LocalModelStudy/source/gojo_menu.gltf";
        private const string ScenePath = "Assets/LocalModelStudy/GojoModelStudy.unity";

        [MenuItem("GojoBracken/Create local Gojo model study")]
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
            Bounds bounds = meshes[0].bounds;
            foreach (var renderer in meshes) bounds.Encapsulate(renderer.bounds);
            if (bounds.size.y <= 0.001f) throw new InvalidOperationException("Model bounds are empty.");
            model.transform.localScale *= 2.1f / bounds.size.y;
            bounds = meshes[0].bounds;
            foreach (var renderer in meshes) bounds.Encapsulate(renderer.bounds);
            model.transform.position += new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
            var standIn = GameObject.Find("Neutral stand-in - NOT Gojo");
            if (standIn != null) standIn.SetActive(false);
            var clips = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>().ToArray();
            if (clips.Length != 1 || clips[0].length <= 0f)
                throw new InvalidOperationException("Expected one non-empty imported clip.");
            var animator = model.GetComponentInChildren<Animator>(true);
            if (animator == null) throw new InvalidOperationException("Imported Animator is missing.");
            const string controllerPath = "Assets/LocalModelStudy/ModelStudy.controller";
            if (File.Exists(controllerPath))
                throw new InvalidOperationException("A study controller already exists; existing work will not be overwritten.");
            var animationController = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPathWithClip(controllerPath, clips[0]);
            animator.runtimeAnimatorController = animationController;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            // Verify a real imported bone curve changes before calling this an animated study.
            var joint = animator.GetComponentsInChildren<Transform>(true).FirstOrDefault(item => item.name == "rightUpperArm");
            if (joint == null) throw new InvalidOperationException("Expected arm joint is missing.");
            clips[0].SampleAnimation(animator.gameObject, 0f);
            var rotation = joint.localRotation;
            clips[0].SampleAnimation(animator.gameObject, 0.8f);
            if (Quaternion.Angle(rotation, joint.localRotation) < 0.01f)
                throw new InvalidOperationException("Sampled clip did not move the expected arm joint.");
            clips[0].SampleAnimation(animator.gameObject, 0f);
            Debug.Log("MODEL MOTION: imported clip moves arm joint between 0 and 0.8 seconds.");
            foreach (var clip in clips)
                Debug.Log("MODEL CLIP: " + clip.name + "; seconds=" + clip.length + "; legacy=" + clip.legacy);
            Debug.Log("MODEL ANIMATION COMPONENTS: Animator=" + model.GetComponentsInChildren<Animator>(true).Length
                + "; Animation=" + model.GetComponentsInChildren<Animation>(true).Length);
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

        private static void Capture(Camera camera)
        {
            string repository = Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;
            string output = Path.Combine(repository, "artifacts", "assets", "gojo-model-study.png");
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
