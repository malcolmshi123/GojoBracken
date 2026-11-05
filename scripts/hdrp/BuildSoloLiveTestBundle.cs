using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class BuildSoloLiveTestBundle
{
    public static void Run()
    {
        if (Application.unityVersion != "2022.3.62f2")
            throw new InvalidOperationException("Use Unity 2022.3.62f2.");
        const string model = "Assets/HdrpCandidates/Gojo.prefab";
        const string aura = "Assets/HdrpCandidates/Aura.prefab";
        const string voice = "Assets/SoloLiveTestSilence.wav";
        const string credits = "Assets/SoloLiveTestCredits.txt";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(model) == null
            || AssetDatabase.LoadAssetAtPath<GameObject>(aura) == null)
            throw new InvalidOperationException("Build the HDRP candidate prefabs first.");

        // One second of mono PCM silence for the private attachment test.
        using (var file = File.Create(voice))
        using (var writer = new BinaryWriter(file))
        {
            writer.Write(new[] { 'R', 'I', 'F', 'F' });
            writer.Write(96036);
            writer.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(48000);
            writer.Write(96000);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(new[] { 'd', 'a', 't', 'a' });
            writer.Write(96000);
            writer.Write(new byte[96000]);
        }
        File.WriteAllText(credits,
            "PRIVATE SOLO ATTACHMENT TEST. Gojo Satoru Dance asset by Ayanokoji031 on Sketchfab, used for evaluation. "
            + "Aura by GojoBracken contributors. Audio is generated silence. Asset provenance and release clearance are pending.");
        AssetDatabase.ImportAsset(voice, ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(credits, ImportAssetOptions.ForceSynchronousImport);
        string output = Path.Combine(Directory.GetParent(Application.dataPath).Parent.FullName, "live-test-bundle");
        Directory.CreateDirectory(output);
        var manifest = BuildPipeline.BuildAssetBundles(output, new[] { new AssetBundleBuild {
            assetBundleName = "gojobracken",
            assetNames = new[] { model, aura, voice, credits },
            addressableNames = new[] { "gojo", "aura", "domain_expansion", "asset_credits" }
        } }, BuildAssetBundleOptions.StrictMode | BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64);
        if (manifest == null) throw new InvalidOperationException("Live test bundle build failed.");

        var bundle = AssetBundle.LoadFromFile(Path.Combine(output, "gojobracken"));
        if (bundle == null) throw new InvalidOperationException("Could not reopen live test bundle.");
        try
        {
            var clip = bundle.LoadAsset<AudioClip>("domain_expansion");
            if (bundle.LoadAsset<GameObject>("gojo") == null || bundle.LoadAsset<GameObject>("aura") == null
                || clip == null || clip.samples != 48000 || bundle.LoadAsset<TextAsset>("asset_credits") == null)
                throw new InvalidOperationException("Live test bundle round trip failed.");
        }
        finally { bundle.Unload(true); }
        Debug.Log("SOLO LIVE BUNDLE PASS: " + output);
    }
}
