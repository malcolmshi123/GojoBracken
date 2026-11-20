using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class BuildGojoBundle
{
    public const string Folder = "Assets/GojoBundle";
    public static readonly string[] Aliases = { "aura", "domain_expansion", "aggro_music", "asset_credits", "voice_source", "uniform_voice_boost" };
    [Serializable] private sealed class Resource { public string source, file, sha256; }
    [Serializable] private sealed class Resources { public Resource[] items; }
    private static Resource[] resources;
    [Serializable] public sealed class TextureResource { public string name, file, sha256; }
    [Serializable] public sealed class TextureResources { public TextureResource[] items; }
    public static TextureResource[] ReadTextureResources() => JsonUtility.FromJson<TextureResources>(File.ReadAllText(Path.Combine(GojoModelChecks.Output, "source/resources/textures.json"))).items;
    public static void Prepare()
    {
        GojoModelChecks.Require(Application.unityVersion == "2022.3.62f2", "Use Unity 2022.3.62f2");
        GojoModelChecks.Require(!AssetDatabase.IsValidFolder(Folder), "Released asset copies already exist; refusing to overwrite");
        string source = Path.Combine(GojoModelChecks.Output, "source");
        resources = JsonUtility.FromJson<Resources>("{\"items\":" + File.ReadAllText(Path.Combine(source, "resources/resources.json")) + "}").items;
        AssetDatabase.CreateFolder("Assets", "GojoBundle");
        foreach (string name in new[] { "Resources", "Audio", "Textures", "Materials", "Meshes", "Shaders" }) AssetDatabase.CreateFolder(Folder, name);
        foreach (var resource in resources)
        {
            byte[] bytes = File.ReadAllBytes(Path.Combine(source, "resources", resource.file));
            GojoModelChecks.Require(BundleSnapshot.Hash(bytes) == resource.sha256, "Source resource hash mismatch");
            File.WriteAllBytes(Folder + "/Resources/" + resource.file, bytes);
        }
        foreach (string name in new[] { "Flame", "Wisps" })
            File.WriteAllText(Folder + "/Shaders/" + name + ".shader", File.ReadAllText(Path.GetFullPath(Path.Combine(GojoModelChecks.Output, "../../scripts/hdrp", name + ".shader"))).Replace("\r\n", "\n"), new UTF8Encoding(false));
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var bundle = AssetBundle.LoadFromFile(Path.Combine(source, "package/BepInEx/plugins/GojoBracken/gojobracken"));
        GojoModelChecks.Require(bundle != null, "Released GojoBracken bundle is required");
        try
        {
            var expected = BundleSnapshot.Capture(bundle, true);
            CopyAura(bundle.LoadAsset<GameObject>("aura"));
            foreach (string name in new[] { "domain_expansion", "aggro_music" })
                SaveNative(UnityEngine.Object.Instantiate(bundle.LoadAsset<AudioClip>(name)), Folder + "/Audio/" + name + ".asset");
            foreach (string name in new[] { "voice_source", "uniform_voice_boost" })
                File.WriteAllBytes(Folder + "/" + name + ".txt", bundle.LoadAsset<TextAsset>(name).bytes);
            byte[] credits = File.ReadAllBytes(Path.Combine(source, "package/ASSET-CREDITS.txt")).Concat(Encoding.UTF8.GetBytes(
                "\nAdditional model edits: symmetric standing posture and hand/jacket geometry edited in Blender; imported as a native Unity model.\n")).ToArray();
            File.WriteAllBytes(Folder + "/asset_credits.txt", credits);
            expected.text.Single(item => item.name == "asset_credits").hash = BundleSnapshot.Hash(credits);
            File.WriteAllText(Path.Combine(GojoModelChecks.Output, "expected-bundle.json"), JsonUtility.ToJson(expected, true));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.SaveAssets();
            Debug.Log("GOJO RELEASE ASSETS PREPARED: native aura and audio, original encoded resources, current credits");
        }
        finally { bundle.Unload(true); }
    }
    private static void CopyAura(GameObject source)
    {
        var clone = UnityEngine.Object.Instantiate(source); clone.name = source.name;
        var meshes = new Dictionary<Mesh, Mesh>();
        var textures = new Dictionary<Texture, Texture>();
        var materials = new Dictionary<Material, Material>();
        try
        {
            foreach (var filter in clone.GetComponentsInChildren<MeshFilter>(true)) filter.sharedMesh = CopyMesh(filter.sharedMesh, meshes);
            foreach (var particles in clone.GetComponentsInChildren<ParticleSystemRenderer>(true))
                if (particles.mesh != null) particles.mesh = CopyMesh(particles.mesh, meshes);
            foreach (var renderer in clone.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterials = renderer.sharedMaterials.Select(sourceMaterial =>
                {
                    if (materials.TryGetValue(sourceMaterial, out var found)) return found;
                    var material = new Material(sourceMaterial) { name = sourceMaterial.name };
                    string shader = sourceMaterial.shader.name;
                    string file = shader == "GojoBracken/HDRP/Flame" ? "Flame" : shader == "GojoBracken/HDRP/Wisps" ? "Wisps" : null;
                    GojoModelChecks.Require(file != null, "Unexpected aura shader: " + shader);
                    material.shader = AssetDatabase.LoadAssetAtPath<Shader>(Folder + "/Shaders/" + file + ".shader");
                    foreach (string property in material.GetTexturePropertyNames())
                    {
                        var texture = material.GetTexture(property); if (texture == null) continue;
                        if (!textures.TryGetValue(texture, out var copy))
                        {
                            GojoModelChecks.Require(texture is Texture2D, "Unexpected aura texture type");
                            var original = (Texture2D)texture;
                            var resource = ReadTextureResources().Single(item => item.name == texture.name);
                            byte[] payload = File.ReadAllBytes(Path.Combine(GojoModelChecks.Output, "source/resources", resource.file));
                            GojoModelChecks.Require(BundleSnapshot.Hash(payload) == resource.sha256, "Aura texture data hash mismatch");
                            var native = new Texture2D(original.width, original.height, original.format, original.mipmapCount, !original.isDataSRGB)
                            {
                                name = original.name,
                                filterMode = original.filterMode,
                                wrapModeU = original.wrapModeU,
                                wrapModeV = original.wrapModeV,
                                wrapModeW = original.wrapModeW,
                                anisoLevel = original.anisoLevel,
                                mipMapBias = original.mipMapBias
                            };
                            native.LoadRawTextureData(payload); native.Apply(false, false); copy = native;
                            AssetDatabase.CreateAsset(copy, Folder + "/Textures/Texture" + textures.Count + ".asset"); textures.Add(texture, copy);
                        }
                        material.SetTexture(property, copy);
                    }
                    AssetDatabase.CreateAsset(material, Folder + "/Materials/Material" + materials.Count + ".mat");
                    materials.Add(sourceMaterial, material); return material;
                }).ToArray();
            GojoModelChecks.Require(PrefabUtility.SaveAsPrefabAsset(clone, Folder + "/Aura.prefab") != null, "Aura prefab save failed");
        }
        finally { UnityEngine.Object.DestroyImmediate(clone); }
    }
    private static Mesh CopyMesh(Mesh source, Dictionary<Mesh, Mesh> copies)
    {
        if (copies.TryGetValue(source, out var mesh)) return mesh;
        mesh = UnityEngine.Object.Instantiate(source); mesh.name = source.name;
        SaveNative(mesh, Folder + "/Meshes/Mesh" + copies.Count + ".asset"); copies.Add(source, mesh); return mesh;
    }
    private static void SaveNative(UnityEngine.Object value, string path)
    {
        AssetDatabase.CreateAsset(value, path); AssetDatabase.SaveAssets();
        string serialized = File.ReadAllText(path);
        foreach (var resource in resources)
            serialized = Regex.Replace(serialized, "archive:/[^\\r\\n\" ]*" + Regex.Escape(resource.source), Folder + "/Resources/" + resource.file);
        File.WriteAllText(path, serialized, new UTF8Encoding(false));
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
    }
    public static string PathFor(string alias) => alias == "aura" ? Folder + "/Aura.prefab" :
        alias == "domain_expansion" || alias == "aggro_music" ? Folder + "/Audio/" + alias + ".asset" : Folder + "/" + alias + ".txt";
    public static void Build()
    {
        GojoModelChecks.Verify();
        string output = Path.GetDirectoryName(GojoBundleChecks.BundlePath); Directory.CreateDirectory(output);
        var aliases = new[] { "gojo" }.Concat(Aliases).ToArray();
        var paths = new[] { GojoModelChecks.Prefab }.Concat(Aliases.Select(PathFor)).ToArray();
        var manifest = BuildPipeline.BuildAssetBundles(output, new[]{new AssetBundleBuild {
            assetBundleName="gojobracken",assetNames=paths,addressableNames=aliases
        }}, BuildAssetBundleOptions.StrictMode | BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64);
        GojoModelChecks.Require(manifest != null, "Complete Windows bundle build failed");
        File.Copy(Folder + "/asset_credits.txt", Path.Combine(output, "ASSET-CREDITS.txt"), true);
        GojoBundleChecks.Verify();
        Debug.Log("GOJO COMPLETE BUNDLE BUILD PASS: " + GojoBundleChecks.BundlePath);
    }
}
