using System;
using System.IO;
using System.Linq;
using GojoBracken;
using GojoBracken.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

public static class BakedAuraChecks
{
    public static void Run()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BakeNativeAura.PrefabPath);
        Check(prefab);
        string guid = AssetDatabase.AssetPathToGUID(BakeNativeAura.PrefabPath);
        bool refused = false;
        try { BakeNativeAura.Create(); }
        catch (BuildFailedException exception) { refused = exception.Message.Contains("already exist"); }
        Require(refused && guid == AssetDatabase.AssetPathToGUID(BakeNativeAura.PrefabPath), "bake refuses to overwrite existing assets");
        foreach (string path in AssetDatabase.GetDependencies(BakeNativeAura.PrefabPath, true))
            Require(!path.StartsWith("Assets/GojoBrackenPreview/") && !path.StartsWith("Assets/LocalModelStudy/"),
                "native prefab has no study/model dependencies: " + path);
        string output = Path.GetFullPath("BakedAuraBundleCheck");
        Directory.CreateDirectory(output);
        var definition = new AssetBundleBuild {
            assetBundleName = "native-aura-check",
            assetNames = new[] { BakeNativeAura.PrefabPath },
            addressableNames = new[] { GojoAssetContract.Aura }
        };
        Require(BuildPipeline.BuildAssetBundles(output, new[] { definition },
            BuildAssetBundleOptions.StrictMode | BuildAssetBundleOptions.ChunkBasedCompression,
            BuildTarget.StandaloneWindows64) != null, "Windows bundle export");
        var bundle = AssetBundle.LoadFromFile(Path.Combine(output, "native-aura-check"));
        Require(bundle != null, "bundle reopens");
        try { Check(bundle.LoadAsset<GameObject>(GojoAssetContract.Aura)); }
        finally { bundle.Unload(true); }
        Debug.Log("BAKED AURA PASS: native components, persisted mesh/material/texture/shader, full-intensity settings, no study dependencies, overwrite guard, Windows bundle round-trip.");
    }

    private static void Check(GameObject prefab)
    {
        Require(prefab != null, "native aura exists");
        foreach (var node in prefab.GetComponentsInChildren<Transform>(true))
            foreach (var component in node.GetComponents<Component>())
                Require(component is Transform || component is ParticleSystem || component is ParticleSystemRenderer
                    || component is MeshFilter || component is MeshRenderer, "only supported native components");
        var particles = prefab.GetComponent<ParticleSystem>();
        Require(particles != null && !particles.main.playOnAwake && particles.main.loop && !particles.main.useUnscaledTime,
            "particles wait for controller and use game time");
        Require(particles.emission.rateOverTime.constant == 220f && particles.main.maxParticles == 384,
            "approved full-intensity emission");
        Require(Mathf.Abs(particles.main.startSize.constantMax - .26f) < .0001f, "approved maximum wisp size");
        Require(particles.GetComponent<ParticleSystemRenderer>().sharedMaterial.mainTexture != null, "wisp texture persists");
        var shell = prefab.GetComponentInChildren<MeshFilter>(true);
        Require(shell != null && shell.sharedMesh != null && shell.sharedMesh.vertexCount == 2665
            && Mathf.Abs(shell.sharedMesh.vertices.Max(vertex => vertex.y) - 3f) < .0001f, "approved 3-unit shell mesh persists");
        var material = shell.GetComponent<Renderer>().sharedMaterial;
        Require(material != null && material.shader != null && material.shader.name == "GojoBracken/FlameEnvelope"
            && !ShaderUtil.ShaderHasError(material.shader) && material.GetFloat("_Strength") == 1f
            && material.GetFloat("_Intensity") == 1f, "shell shader and defaults persist");
    }

    private static void Require(bool condition, string reason)
    {
        if (!condition) throw new InvalidOperationException("Baked aura check failed: " + reason);
    }
}
