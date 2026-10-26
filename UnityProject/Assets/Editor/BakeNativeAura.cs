using System.IO;
using GojoBracken.Authoring;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace GojoBracken.Editor
{
    public static class BakeNativeAura
    {
        public const string PrefabPath = "Assets/GojoBracken/Prefabs/Aura.prefab";
        public const string Folder = "Assets/GojoBracken/Effects";
        private const string Study = "Assets/GojoBrackenPreview/";

        [MenuItem("GojoBracken/Bake native aura prefab")]
        public static void Create()
        {
            if (Application.isPlaying || Application.unityVersion != BuildGojoAssets.RequiredUnityVersion)
                throw new BuildFailedException("Bake outside Play mode in Unity " + BuildGojoAssets.RequiredUnityVersion + ".");
            // Generated source assets are reviewable. Never overwrite user edits or reset GUIDs.
            if (File.Exists(PrefabPath) || Directory.Exists(Folder))
                throw new BuildFailedException("Baked aura outputs already exist. Open the existing prefab; baking will not overwrite it.");
            var source = AssetDatabase.LoadAssetAtPath<Material>(Study + "AuraStudy.mat");
            if (source == null || source.mainTexture == null)
                throw new BuildFailedException("Approved study material/texture is missing.");
            string shaderSource = File.ReadAllText(Study + "FlameEnvelope.shader");
            BuildGojoAssets.CreateAssetFolders();
            AssetDatabase.CreateFolder("Assets/GojoBracken", "Effects");
            File.WriteAllText(Folder + "/FlameEnvelope.shader",
                shaderSource.Replace("GojoBracken/PreviewFlameEnvelope", "GojoBracken/FlameEnvelope"));
            AssetDatabase.ImportAsset(Folder + "/FlameEnvelope.shader", ImportAssetOptions.ForceSynchronousImport);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(Folder + "/FlameEnvelope.shader");
            if (shader == null || ShaderUtil.ShaderHasError(shader))
                throw new BuildFailedException("Baked envelope shader did not compile.");
            if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source.mainTexture), Folder + "/Wisp.png"))
                throw new BuildFailedException("Could not copy the approved wisp texture/import settings.");
            var wisps = new Material(source) { name = "GojoWisps" };
            wisps.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/Wisp.png");
            AssetDatabase.CreateAsset(wisps, Folder + "/Wisps.mat");
            var flame = new Material(shader) { name = "GojoFlame" };
            flame.SetFloat("_Strength", 1f);
            flame.SetFloat("_Intensity", 1f);
            AssetDatabase.CreateAsset(flame, Folder + "/Flame.mat");
            var mesh = FlameEnvelope.BuildMesh();
            mesh.name = "GojoFlameEnvelope";
            AssetDatabase.CreateAsset(mesh, Folder + "/FlameMesh.asset");
            var root = new GameObject("Gojo aura - native components");
            try
            {
                root.SetActive(false);
                var particles = root.AddComponent<ParticleSystem>();
                AuraStudyController.ConfigureFlames(particles);
                AuraStudyController.ConfigureWisps(particles, 1f, 1f);
                var main = particles.main;
                main.playOnAwake = false;
                // Game effects follow game time; the authoring study deliberately uses unscaled time.
                main.useUnscaledTime = false;
                root.GetComponent<ParticleSystemRenderer>().sharedMaterial = wisps;
                var shell = new GameObject("Continuous flame envelope");
                shell.transform.SetParent(root.transform, false);
                shell.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = shell.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = flame;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                root.SetActive(true);
                if (PrefabUtility.SaveAsPrefabAsset(root, PrefabPath) == null)
                    throw new BuildFailedException("Could not save the native aura prefab.");
                AssetDatabase.SaveAssets();
                Debug.Log("Baked native aura: " + PrefabPath + ". Full-intensity authoring look; game render-pipeline compatibility remains pending.");
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
