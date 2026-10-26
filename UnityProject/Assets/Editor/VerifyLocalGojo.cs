using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GojoBracken.Editor
{
    public static class VerifyLocalGojo
    {
        [MenuItem("GojoBracken/Verify local native Gojo prefab")]
        public static void Run()
        {
            if (Application.isPlaying || Application.unityVersion != BuildGojoAssets.RequiredUnityVersion)
                throw new InvalidOperationException("Use the pinned Editor outside Play mode.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BakeLocalGojo.PrefabPath);
            Check(prefab);
            string guid = AssetDatabase.AssetPathToGUID(BakeLocalGojo.PrefabPath);
            bool refused = false;
            try { BakeLocalGojo.Create(); }
            catch (InvalidOperationException exception) { refused = exception.Message.Contains("already exist"); }
            Require(refused && guid == AssetDatabase.AssetPathToGUID(BakeLocalGojo.PrefabPath), "existing outputs are not overwritten");
            foreach (string dependency in AssetDatabase.GetDependencies(BakeLocalGojo.PrefabPath, true))
                if (dependency.StartsWith("Assets/LocalModelStudy/") && !dependency.StartsWith(BakeLocalGojo.Folder + "/"))
                    throw new InvalidOperationException("Native prefab still depends on the imported study: " + dependency);
            string repository = Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;
            string output = Path.Combine(repository, "artifacts", "local-model-check");
            Directory.CreateDirectory(output);
            var definition = new AssetBundleBuild {
                assetBundleName = "local-native-gojo-check",
                assetNames = new[] { BakeLocalGojo.PrefabPath },
                addressableNames = new[] { GojoAssetContract.Model }
            };
            Require(BuildPipeline.BuildAssetBundles(output, new[] { definition },
                BuildAssetBundleOptions.StrictMode | BuildAssetBundleOptions.ChunkBasedCompression,
                BuildTarget.StandaloneWindows64) != null, "local Windows bundle export");
            var bundle = AssetBundle.LoadFromFile(Path.Combine(output, "local-native-gojo-check"));
            Require(bundle != null, "local bundle reopens");
            try { Check(bundle.LoadAsset<GameObject>(GojoAssetContract.Model)); }
            finally { bundle.Unload(true); }
            Debug.Log("LOCAL NATIVE GOJO PASS: four native skins, persisted references, no imported-study dependencies, constant idle without dance/root motion, anchor following, stable height/grounding, Windows bundle round-trip. Local evaluation only.");
        }

        private static void Check(GameObject prefab)
        {
            Require(prefab != null, "local native prefab exists");
            Require(prefab.GetComponentsInChildren<MonoBehaviour>(true).Length == 0, "no custom scripts");
            var skins = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Require(skins.Length == 4 && skins.Sum(skin => skin.sharedMesh.vertexCount) == 37716, "all four skins retained");
            foreach (var skin in skins)
            {
                Require(skin.bones.Length == 31 && skin.sharedMesh.bindposes.Length == 31 && skin.bones.All(bone => bone != null), "complete skin bones/bind poses");
                foreach (var material in skin.sharedMaterials)
                {
                    Require(material != null && material.shader != null && !ShaderUtil.ShaderHasError(material.shader), "valid saved material/shader");
                    Require(material.GetTexturePropertyNames().Any(property => material.GetTexture(property) != null), "saved material texture");
                }
            }
            var animator = prefab.GetComponent<Animator>();
            Require(animator != null && animator.enabled && !animator.applyRootMotion && animator.runtimeAnimatorController != null,
                "enabled native Animator, no root motion");
            var clips = animator.runtimeAnimatorController.animationClips;
            Require(clips.Length == 1 && clips[0].name == "StandingIdle" && clips[0].isLooping
                && AnimationUtility.GetAnimationEvents(clips[0]).Length == 0, "only a looped standing idle, no dance/events");
            var anchor = new GameObject("Local verification anchor");
            try
            {
                var instance = UnityEngine.Object.Instantiate(prefab, anchor.transform, false);
                var nodes = instance.GetComponentsInChildren<Transform>(true);
                var positions = nodes.Select(node => node.localPosition).ToArray();
                var rotations = nodes.Select(node => node.localRotation).ToArray();
                var scales = nodes.Select(node => node.localScale).ToArray();
                var player = instance.GetComponent<Animator>();
                player.Rebind();
                for (int i = 0; i < 15; i++) player.Update(.2f);
                for (int i = 0; i < nodes.Length; i++)
                    Require(Vector3.Distance(nodes[i].localPosition, positions[i]) < .0001f
                        && Quaternion.Angle(nodes[i].localRotation, rotations[i]) < .03f
                        && Vector3.Distance(nodes[i].localScale, scales[i]) < .0001f, "idle preserves approved pose: " + nodes[i].name);
                var before = GojoIdlePose.VisibleBounds(instance);
                Require(Mathf.Abs(before.size.y - 2.1f) < .01f && Mathf.Abs(before.min.y) < .005f, "approved height and floor contact");
                anchor.transform.position = new Vector3(3f, 0f, 2f);
                player.Update(.2f);
                var after = GojoIdlePose.VisibleBounds(instance);
                Require(Vector3.Distance(after.center - before.center, anchor.transform.position) < .001f,
                    "visual follows its anchor rather than clip root motion");
            }
            finally { UnityEngine.Object.DestroyImmediate(anchor); }
        }

        private static void Require(bool condition, string reason)
        {
            if (!condition) throw new InvalidOperationException("Local native Gojo check failed: " + reason);
        }
    }
}
