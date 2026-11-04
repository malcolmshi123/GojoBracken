using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public static class BuildHdrpMaterials
{
    private const string Output = "Assets/HdrpCandidates";

    public static void Run()
    {
        if (Application.unityVersion != "2022.3.62f2")
            throw new InvalidOperationException("Use Unity 2022.3.62f2.");

        PlayerSettings.colorSpace = ColorSpace.Linear;
        var pipeline = ScriptableObject.CreateInstance<HDRenderPipelineAsset>();
        AssetDatabase.CreateAsset(pipeline, "Assets/ValidationPipeline.asset");
        GraphicsSettings.defaultRenderPipeline = pipeline;

        AssetDatabase.CreateFolder("Assets", "HdrpCandidates");
        var model = Copy("Assets/LocalModelStudy/NativeGojo/Gojo.prefab", "Gojo", ConvertBody);
        var aura = Copy("Assets/GojoBracken/Prefabs/Aura.prefab", "Aura", ConvertAura);
        AssetDatabase.SaveAssets();

        string bundleFolder = Path.GetFullPath("../bundles");
        Directory.CreateDirectory(bundleFolder);
        var manifest = BuildPipeline.BuildAssetBundles(bundleFolder, new[] { new AssetBundleBuild {
            assetBundleName = "gojobracken-hdrp-material-check",
            assetNames = new[] { model, aura },
            addressableNames = new[] { "gojo", "aura" }
        } }, BuildAssetBundleOptions.StrictMode | BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64);
        Require(manifest != null, "Windows asset bundle build failed");

        var bundle = AssetBundle.LoadFromFile(Path.Combine(bundleFolder, "gojobracken-hdrp-material-check"));
        Require(bundle != null, "Bundle could not be reopened");
        try
        {
            var gojo = bundle.LoadAsset<GameObject>("gojo");
            var effect = bundle.LoadAsset<GameObject>("aura");
            Require(gojo != null && effect != null, "Expected both prefab addresses");
            var skins = gojo.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Require(skins.Length == 4 && skins.All(skin => skin.bones.Length == 31
                && skin.sharedMaterials.All(material => material.shader.name == "HDRP/Lit"
                    && material.GetTexture("_BaseColorMap") != null)), "Body rig or texture mapping did not survive export");
            Require(gojo.GetComponent<Animator>().runtimeAnimatorController != null, "Idle controller was lost");
            var particles = effect.GetComponentInChildren<ParticleSystemRenderer>(true);
            Require(particles != null && particles.sharedMaterial.shader.name == "GojoBracken/HDRP/Wisps"
                && particles.sharedMaterial.GetTexture("_MainTex") != null, "Wisp texture or shader was lost");
            var flame = effect.GetComponentsInChildren<MeshRenderer>(true).Single().sharedMaterial;
            Require(flame.shader.name == "GojoBracken/HDRP/Flame" && flame.HasProperty("_Strength")
                && flame.HasProperty("_Intensity"), "Flame controller properties were lost");
        }
        finally { bundle.Unload(true); }

        Debug.Log("HDRP MATERIALS PASS: converted four skins and both aura materials, Windows bundle round trip.");
    }

    private static string Copy(string source, string name, Func<Material, Material> convert)
    {
        var root = PrefabUtility.LoadPrefabContents(source);
        try
        {
            var copies = new Dictionary<Material, Material>();
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(original =>
                {
                    if (copies.TryGetValue(original, out var material)) return material;
                    material = convert(original);
                    AssetDatabase.CreateAsset(material, $"{Output}/{name}-{copies.Count}.mat");
                    copies.Add(original, material);
                    return material;
                }).ToArray();
            }
            string path = $"{Output}/{name}.prefab";
            Require(PrefabUtility.SaveAsPrefabAsset(root, path) != null, "Could not save " + path);
            return path;
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static Material ConvertBody(Material source)
    {
        Require(source.shader.name == "glTF/PbrMetallicRoughness" && source.renderQueue == 2000
            && source.GetTexture("baseColorTexture") != null, "Unexpected Gojo material: " + source.name);
        foreach (string property in new[] { "metallicRoughnessTexture", "normalTexture", "occlusionTexture", "emissiveTexture" })
            Require(source.GetTexture(property) == null, "Unmapped source texture: " + property);

        var material = NewMaterial("HDRP/Lit");
        material.SetTexture("_BaseColorMap", source.GetTexture("baseColorTexture"));
        material.SetTextureScale("_BaseColorMap", source.GetTextureScale("baseColorTexture"));
        material.SetTextureOffset("_BaseColorMap", source.GetTextureOffset("baseColorTexture"));
        material.SetColor("_BaseColor", source.GetColor("baseColorFactor"));
        material.SetFloat("_Metallic", source.GetFloat("metallicFactor"));
        material.SetFloat("_Smoothness", 1f - source.GetFloat("roughnessFactor"));
        material.SetFloat("_DoubleSidedEnable", source.doubleSidedGI ? 1f : 0f);
        Require(HDMaterial.ValidateMaterial(material), "Could not validate HDRP Lit");
        return material;
    }

    private static Material ConvertAura(Material source)
    {
        if (source.shader.name == "Legacy Shaders/Particles/Additive")
        {
            var wisps = NewMaterial("GojoBracken/HDRP/Wisps");
            wisps.SetTexture("_MainTex", source.mainTexture);
            return wisps;
        }

        Require(source.shader.name == "GojoBracken/FlameEnvelope", "Unexpected aura material: " + source.name);
        var flame = NewMaterial("GojoBracken/HDRP/Flame");
        flame.SetFloat("_Strength", source.GetFloat("_Strength"));
        flame.SetFloat("_Intensity", source.GetFloat("_Intensity"));
        return flame;
    }

    private static Material NewMaterial(string name)
    {
        var shader = Shader.Find(name);
        Require(shader != null && !ShaderUtil.ShaderHasError(shader), "Missing or invalid shader: " + name);
        return new Material(shader);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
