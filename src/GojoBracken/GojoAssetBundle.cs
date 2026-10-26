using System;
using System.IO;
using UnityEngine;

namespace GojoBracken
{
    // Main-thread only. Owns assets, never instantiates objects or changes enemy renderers.
    // The future attachment owner must destroy all cosmetic instances before disposing this bundle.
    internal sealed class GojoAssetBundle : IDisposable
    {
        private AssetBundle bundle;
        internal GameObject Model { get; private set; }
        internal GameObject Aura { get; private set; }
        internal AudioClip Voice { get; private set; }
        internal string Credits { get; private set; }

        private GojoAssetBundle(AssetBundle bundle, GameObject model, GameObject aura, AudioClip voice, string credits)
        {
            this.bundle = bundle;
            Model = model;
            Aura = aura;
            Voice = voice;
            Credits = credits;
        }

        internal static bool TryLoad(string directory, out GojoAssetBundle assets, out string error)
        {
            assets = null;
            error = null;
            AssetBundle candidate = null;
            try
            {
                if (string.IsNullOrWhiteSpace(directory))
                    throw new InvalidDataException("A plugin asset directory is required.");
                string path = Path.Combine(directory, GojoAssetContract.BundleName);
                if (!File.Exists(path))
                    throw new FileNotFoundException("Missing gojobracken bundle beside the plugin DLL.");
                candidate = AssetBundle.LoadFromFile(path);
                if (candidate == null)
                    throw new InvalidDataException("Unity could not open gojobracken. Check the bundle platform, Unity version, or corruption.");
                var model = candidate.LoadAsset<GameObject>(GojoAssetContract.Model);
                var aura = candidate.LoadAsset<GameObject>(GojoAssetContract.Aura);
                var voice = candidate.LoadAsset<AudioClip>(GojoAssetContract.Voice);
                var credits = candidate.LoadAsset<TextAsset>(GojoAssetContract.Credits);
                Validate(model, aura, voice, credits);
                assets = new GojoAssetBundle(candidate, model, aura, voice, credits.text);
                candidate = null; // Ownership transfers only after every check passes.
                return true;
            }
            catch (Exception exception)
            {
                error = "Gojo assets unavailable; keep vanilla Bracken visible. "
                    + exception.GetType().Name + ": " + exception.Message;
                return false;
            }
            finally
            {
                if (candidate != null) candidate.Unload(true);
            }
        }

        internal static void Validate(GameObject model, GameObject aura, AudioClip voice, TextAsset credits)
        {
            if (model == null) throw new InvalidDataException("Missing GameObject asset 'gojo'.");
            if (aura == null) throw new InvalidDataException("Missing GameObject asset 'aura'.");
            if (voice == null || voice.samples <= 0 || voice.length <= 0f)
                throw new InvalidDataException("Missing or empty AudioClip 'domain_expansion'.");
            if (credits == null || string.IsNullOrWhiteSpace(credits.text))
                throw new InvalidDataException("Missing or empty TextAsset 'asset_credits'; rebuild with the current exporter.");
            var skins = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (skins.Length == 0) throw new InvalidDataException("Gojo requires a skinned mesh.");
            foreach (var skin in skins)
            {
                if (skin.sharedMesh == null || skin.sharedMesh.vertexCount == 0 || skin.bones.Length == 0
                    || skin.sharedMesh.bindposes.Length != skin.bones.Length)
                    throw new InvalidDataException("Gojo has an empty mesh or inconsistent skin bones/bind poses.");
                foreach (var bone in skin.bones)
                    if (bone == null) throw new InvalidDataException("Gojo has a missing skin bone.");
            }
            var animators = model.GetComponentsInChildren<Animator>(true);
            if (animators.Length == 0) throw new InvalidDataException("Gojo requires an Animator with an animation controller.");
            foreach (var animator in animators)
                if (animator.runtimeAnimatorController == null)
                    throw new InvalidDataException("Gojo Animator is missing its animation controller.");
            if (aura.GetComponentsInChildren<ParticleSystem>(true).Length == 0)
                throw new InvalidDataException("Aura requires a ParticleSystem.");
            ValidatePrefab(model, "gojo");
            ValidatePrefab(aura, "aura");
        }

        private static void ValidatePrefab(GameObject prefab, string name)
        {
            foreach (var node in prefab.GetComponentsInChildren<Transform>(true))
                foreach (var component in node.GetComponents<Component>())
                    if (component == null || component is MonoBehaviour)
                        throw new InvalidDataException(name + " contains a missing or custom script: " + node.name);
            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.sharedMaterials.Length == 0)
                    throw new InvalidDataException(name + " has a renderer without materials: " + renderer.name);
                foreach (var material in renderer.sharedMaterials)
                    if (material == null || material.shader == null || !material.shader.isSupported
                        || material.shader.name == "Hidden/InternalErrorShader")
                        throw new InvalidDataException(name + " has a missing or unsupported material/shader: " + renderer.name);
            }
        }

        public void Dispose()
        {
            if (bundle == null) return;
            bundle.Unload(true);
            bundle = null;
            Model = Aura = null;
            Voice = null;
            Credits = null;
        }
    }
}
