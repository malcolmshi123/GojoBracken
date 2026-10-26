using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace GojoBracken.Editor
{
    public static class BuildGojoAssets
    {
        public const string RequiredUnityVersion = "2022.3.62f2";
        public const string BundleName = GojoAssetContract.BundleName;
        private const string Root = "Assets/GojoBracken/";
        private const string ModelPath = Root + "Prefabs/Gojo.prefab";
        private const string AuraPath = Root + "Prefabs/Aura.prefab";
        private const string VoicePath = Root + "Audio/DomainExpansion.wav";
        private const string CreditsPath = Root + "CREDITS.txt";

        [MenuItem("GojoBracken/Create asset folders")]
        public static void CreateAssetFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/GojoBracken"))
                AssetDatabase.CreateFolder("Assets", "GojoBracken");
            foreach (string name in new[] { "Prefabs", "Models", "Materials", "Audio" })
            {
                if (!AssetDatabase.IsValidFolder(Root + name))
                    AssetDatabase.CreateFolder("Assets/GojoBracken", name);
            }
            Debug.Log("Asset folders created. Import actual assets and retain their .meta files.");
        }

        [MenuItem("GojoBracken/Validate assets")]
        public static void ValidateAssets()
        {
            if (Application.unityVersion != RequiredUnityVersion)
                throw new BuildFailedException("Use Unity " + RequiredUnityVersion + "; found " + Application.unityVersion + ".");

            var model = RequireAsset<GameObject>(ModelPath);
            var aura = RequireAsset<GameObject>(AuraPath);
            var voice = RequireAsset<AudioClip>(VoicePath);
            var credits = RequireAsset<TextAsset>(CreditsPath);

            var meshes = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (meshes.Length == 0)
                throw new BuildFailedException("Gojo.prefab needs a skinned mesh; a stand-in is not a release model.");
            foreach (var mesh in meshes)
            {
                if (mesh.sharedMesh == null || mesh.sharedMesh.vertexCount == 0 || mesh.bones.Length == 0
                    || mesh.sharedMesh.bindposes.Length != mesh.bones.Length || Array.Exists(mesh.bones, bone => bone == null))
                    throw new BuildFailedException("Gojo.prefab has an empty or unrigged skinned mesh: " + mesh.name);
            }
            if (model.GetComponentInChildren<Animator>(true) == null)
                throw new BuildFailedException("Gojo.prefab needs an Animator. Rig/animation adaptation still requires manual validation.");
            foreach (var animator in model.GetComponentsInChildren<Animator>(true))
                if (animator.runtimeAnimatorController == null)
                    throw new BuildFailedException("Gojo.prefab Animator needs an animation controller: " + animator.name);
            if (aura.GetComponentsInChildren<ParticleSystem>(true).Length == 0)
                throw new BuildFailedException("Aura.prefab needs at least one ParticleSystem.");
            if (voice.samples <= 0 || voice.length <= 0)
                throw new BuildFailedException("DomainExpansion.wav did not import as a non-empty AudioClip.");
            if (string.IsNullOrWhiteSpace(credits.text))
                throw new BuildFailedException("CREDITS.txt must record sources, authors, licenses/permissions, and modifications.");

            ValidatePrefab(model, ModelPath);
            ValidatePrefab(aura, AuraPath);
            Debug.Log("Asset structure validated. This does not certify material compatibility, animation quality, or usage rights.");
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new BuildFailedException("Missing or invalid " + typeof(T).Name + ": " + path);
            return asset;
        }

        private static void ValidatePrefab(GameObject prefab, string path)
        {
            // Bundle prefabs use native Unity components. The plugin will attach its runtime code.
            foreach (var transform in prefab.GetComponentsInChildren<Transform>(true))
            {
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) != 0)
                    throw new BuildFailedException("Missing script in " + path + ": " + transform.name);
                if (transform.GetComponents<MonoBehaviour>().Length != 0)
                    throw new BuildFailedException("Remove custom scripts from bundle prefab " + path + ": " + transform.name);
            }

            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.sharedMaterials.Length == 0)
                    throw new BuildFailedException("Renderer has no materials in " + path + ": " + renderer.name);
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null || material.shader == null || material.shader.name == "Hidden/InternalErrorShader")
                        throw new BuildFailedException("Missing material or shader in " + path + ": " + renderer.name);
                }
            }
        }

        [MenuItem("GojoBracken/Build Windows asset bundle")]
        public static void BuildWindowsBundle()
        {
            ValidateAssets();
            string projectDirectory = Directory.GetParent(Application.dataPath).FullName;
            string repositoryDirectory = Directory.GetParent(projectDirectory).FullName;
            string output = Path.Combine(repositoryDirectory, "artifacts", "assets");
            Directory.CreateDirectory(output);

            var definition = new AssetBundleBuild
            {
                assetBundleName = BundleName,
                assetNames = new[] { ModelPath, AuraPath, VoicePath, CreditsPath },
                addressableNames = new[] { GojoAssetContract.Model, GojoAssetContract.Aura,
                    GojoAssetContract.Voice, GojoAssetContract.Credits }
            };
            var manifest = BuildPipeline.BuildAssetBundles(output, new[] { definition },
                BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode,
                BuildTarget.StandaloneWindows64);
            if (manifest == null)
                throw new BuildFailedException("Unity did not return a bundle manifest.");

            string bundlePath = Path.Combine(output, BundleName);
            var bundle = AssetBundle.LoadFromFile(bundlePath);
            if (bundle == null)
                throw new BuildFailedException("Built bundle could not be reopened.");
            try
            {
                if (bundle.LoadAsset<GameObject>(GojoAssetContract.Model) == null ||
                    bundle.LoadAsset<GameObject>(GojoAssetContract.Aura) == null ||
                    bundle.LoadAsset<AudioClip>(GojoAssetContract.Voice) == null ||
                    bundle.LoadAsset<TextAsset>(GojoAssetContract.Credits) == null)
                    throw new BuildFailedException("Built bundle is missing an expected asset name or type.");
            }
            finally
            {
                bundle.Unload(true);
            }

            File.Copy(Path.Combine(projectDirectory, CreditsPath),
                Path.Combine(output, "ASSET-CREDITS.txt"), true);
            Debug.Log("Built and reopened Windows x64 bundle: " + bundlePath +
                "; hash=" + manifest.GetAssetBundleHash(BundleName));
        }
    }
}
