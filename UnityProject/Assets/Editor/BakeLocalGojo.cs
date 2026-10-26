using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GojoBracken.Editor
{
    public static class BakeLocalGojo
    {
        public const string Folder = "Assets/LocalModelStudy/NativeGojo";
        public const string PrefabPath = Folder + "/Gojo.prefab";
        private const string Study = "Assets/LocalModelStudy/GojoIdleStudy.unity";

        [MenuItem("GojoBracken/Bake local native Gojo prefab")]
        public static void Create()
        {
            if (Application.isPlaying || Application.unityVersion != BuildGojoAssets.RequiredUnityVersion)
                throw new InvalidOperationException("Use Unity " + BuildGojoAssets.RequiredUnityVersion + " outside Play mode.");
            string project = Directory.GetParent(Application.dataPath).FullName;
            if (Directory.Exists(Path.Combine(project, Folder)))
                throw new InvalidOperationException("Local native outputs already exist; baking will not overwrite them.");
            if (!File.Exists(Path.Combine(project, Study))) throw new InvalidOperationException("The approved local idle scene is required.");
            var scene = SceneManager.GetSceneByPath(Study);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(Study, OpenSceneMode.Additive);
            GameObject root = null;
            try
            {
                if (scene.isDirty) throw new InvalidOperationException("Save the local idle scene before baking its pose.");
                var source = scene.GetRootGameObjects().Single(item => item.name == "Gojo candidate - local evaluation");
                AssetDatabase.CreateFolder("Assets/LocalModelStudy", "NativeGojo");
                root = new GameObject("Gojo - local native evaluation");
                var clone = UnityEngine.Object.Instantiate(source, root.transform, true);
                clone.name = "Approved standing rig";
                if (PrefabUtility.IsPartOfPrefabInstance(clone))
                    PrefabUtility.UnpackPrefabInstance(clone, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                foreach (var component in clone.GetComponentsInChildren<MonoBehaviour>(true))
                    UnityEngine.Object.DestroyImmediate(component);
                foreach (var animator in clone.GetComponentsInChildren<Animator>(true))
                    UnityEngine.Object.DestroyImmediate(animator);
                foreach (var node in clone.GetComponentsInChildren<Transform>(true))
                    foreach (var component in node.GetComponents<Component>())
                        if (!(component is Transform || component is SkinnedMeshRenderer || component is MeshFilter || component is MeshRenderer))
                            throw new InvalidOperationException("Unexpected or missing native model component: " + node.name);

                var meshes = new Dictionary<Mesh, Mesh>();
                var materials = new Dictionary<Material, Material>();
                var textures = new Dictionary<Texture, Texture>();
                foreach (var skin in clone.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (skin.sharedMesh == null || skin.bones.Any(bone => bone == null))
                        throw new InvalidOperationException("Invalid source skin: " + skin.name);
                    skin.sharedMesh = CopyMesh(skin.sharedMesh, meshes);
                }
                foreach (var filter in clone.GetComponentsInChildren<MeshFilter>(true))
                    filter.sharedMesh = CopyMesh(filter.sharedMesh, meshes);
                foreach (var renderer in clone.GetComponentsInChildren<Renderer>(true))
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(material =>
                        CopyMaterial(material, materials, textures)).ToArray();

                var clip = new AnimationClip { name = "StandingIdle", frameRate = 30f };
                var paths = new HashSet<string>();
                foreach (var node in root.GetComponentsInChildren<Transform>(true))
                {
                    string path = AnimationUtility.CalculateTransformPath(node, root.transform);
                    if (!paths.Add(path)) throw new InvalidOperationException("Ambiguous animation path: " + path);
                    Vector3 p = node.localPosition, s = node.localScale;
                    Quaternion q = node.localRotation;
                    Curve(clip, path, "m_LocalPosition.x", p.x); Curve(clip, path, "m_LocalPosition.y", p.y); Curve(clip, path, "m_LocalPosition.z", p.z);
                    Curve(clip, path, "m_LocalRotation.x", q.x); Curve(clip, path, "m_LocalRotation.y", q.y); Curve(clip, path, "m_LocalRotation.z", q.z); Curve(clip, path, "m_LocalRotation.w", q.w);
                    Curve(clip, path, "m_LocalScale.x", s.x); Curve(clip, path, "m_LocalScale.y", s.y); Curve(clip, path, "m_LocalScale.z", s.z);
                }
                clip.EnsureQuaternionContinuity();
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                AssetDatabase.CreateAsset(clip, Folder + "/StandingIdle.anim");
                var controller = AnimatorController.CreateAnimatorControllerAtPath(Folder + "/StandingIdle.controller");
                var state = controller.layers[0].stateMachine.AddState("Standing idle");
                state.motion = clip;
                controller.layers[0].stateMachine.defaultState = state;
                var outputAnimator = root.AddComponent<Animator>();
                outputAnimator.runtimeAnimatorController = controller;
                outputAnimator.applyRootMotion = false;
                outputAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                if (PrefabUtility.SaveAsPrefabAsset(root, PrefabPath) == null)
                    throw new InvalidOperationException("Native local prefab save failed.");
                AssetDatabase.SaveAssets();
                Debug.Log("LOCAL GOJO BAKE: " + PrefabPath + "; native standing idle only; local evaluation, provenance unresolved.");
            }
            finally
            {
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void Curve(AnimationClip clip, string path, string property, float value)
        {
            clip.SetCurve(path, typeof(Transform), property, AnimationCurve.Constant(0f, 1f, value));
        }

        private static Mesh CopyMesh(Mesh source, Dictionary<Mesh, Mesh> copies)
        {
            if (source == null) throw new InvalidOperationException("Missing model mesh.");
            if (!copies.TryGetValue(source, out var result))
            {
                result = UnityEngine.Object.Instantiate(source);
                AssetDatabase.CreateAsset(result, Folder + "/Mesh" + copies.Count + ".asset");
                copies.Add(source, result);
            }
            return result;
        }

        private static Material CopyMaterial(Material source, Dictionary<Material, Material> copies, Dictionary<Texture, Texture> textures)
        {
            if (source == null) throw new InvalidOperationException("Missing model material.");
            if (copies.TryGetValue(source, out var existing)) return existing;
            var result = new Material(source);
            foreach (string property in source.GetTexturePropertyNames())
            {
                var texture = source.GetTexture(property);
                if (texture == null) continue;
                if (!textures.TryGetValue(texture, out var copy))
                {
                    if (!(texture is Texture2D)) throw new InvalidOperationException("Unsupported local model texture type.");
                    copy = UnityEngine.Object.Instantiate(texture);
                    AssetDatabase.CreateAsset(copy, Folder + "/Texture" + textures.Count + ".asset");
                    textures.Add(texture, copy);
                }
                result.SetTexture(property, copy);
            }
            AssetDatabase.CreateAsset(result, Folder + "/Material" + copies.Count + ".mat");
            copies.Add(source, result);
            return result;
        }
    }
}
