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
        string[] args = Environment.GetCommandLineArgs();
        int voiceArg = Array.IndexOf(args, "-gojoVoiceId");
        bool withVoice = voiceArg >= 0;
        if (withVoice && (voiceArg + 1 >= args.Length || !Guid.TryParseExact(args[voiceArg + 1], "N", out _)))
            throw new InvalidOperationException("Invalid staged voice identifier.");
        string voiceStem = withVoice ? "Assets/GojoVoice-" + args[voiceArg + 1] : null;
        string voice = withVoice ? voiceStem + ".wav" : "Assets/SoloLiveTestSilence.wav";
        string voiceCredits = withVoice ? voiceStem + ".txt" : null;
        const string credits = "Assets/SoloLiveTestCredits.txt";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(model) == null
            || AssetDatabase.LoadAssetAtPath<GameObject>(aura) == null)
            throw new InvalidOperationException("Build the HDRP candidate prefabs first.");

        if (!withVoice) WriteSilence(voice);
        string audioCredits = withVoice ? File.ReadAllText(voiceCredits) : "Audio is generated silence.";
        if (string.IsNullOrWhiteSpace(audioCredits))
            throw new InvalidOperationException("Voice credits are empty.");
        File.WriteAllText(credits,
            "PRIVATE SOLO ATTACHMENT TEST. Gojo Satoru Dance asset by Ayanokoji031 on Sketchfab, used for evaluation. "
            + "Aura by GojoBracken contributors. Asset provenance and release clearance are pending."
            + Environment.NewLine + audioCredits);

        AssetDatabase.ImportAsset(voice, ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(credits, ImportAssetOptions.ForceSynchronousImport);
        if (withVoice) AssetDatabase.ImportAsset(voiceCredits, ImportAssetOptions.ForceSynchronousImport);
        var imported = AssetDatabase.LoadAssetAtPath<AudioClip>(voice);
        if (imported == null || imported.samples <= 0 || (withVoice && (imported.channels != 1 || imported.length > 10f)))
            throw new InvalidOperationException("Import a nonempty mono WAV trimmed to ten seconds or less.");

        string folder = withVoice ? "live-test-voice-bundle" : "live-test-bundle";
        string output = Path.Combine(Directory.GetParent(Application.dataPath).Parent.FullName, folder);
        Directory.CreateDirectory(output);
        var manifest = BuildPipeline.BuildAssetBundles(output, new[] { new AssetBundleBuild {
            assetBundleName = "gojobracken",
            assetNames = withVoice ? new[] { model, aura, voice, credits, voiceCredits } : new[] { model, aura, voice, credits },
            addressableNames = withVoice ? new[] { "gojo", "aura", "domain_expansion", "asset_credits", "voice_source" }
                : new[] { "gojo", "aura", "domain_expansion", "asset_credits" }
        } }, BuildAssetBundleOptions.StrictMode | BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64);
        if (manifest == null) throw new InvalidOperationException("Live test bundle build failed.");

        var bundle = AssetBundle.LoadFromFile(Path.Combine(output, "gojobracken"));
        if (bundle == null) throw new InvalidOperationException("Could not reopen live test bundle.");
        try
        {
            var clip = bundle.LoadAsset<AudioClip>("domain_expansion");
            var source = bundle.LoadAsset<TextAsset>("voice_source");
            if (bundle.LoadAsset<GameObject>("gojo") == null || bundle.LoadAsset<GameObject>("aura") == null
                || clip == null || clip.samples != imported.samples || bundle.LoadAsset<TextAsset>("asset_credits") == null
                || (withVoice ? source == null || source.text.Trim() != audioCredits.Trim() : source != null))
                throw new InvalidOperationException("Live test bundle round trip failed.");
        }
        finally { bundle.Unload(true); }
        Debug.Log($"SOLO LIVE BUNDLE PASS: {output}, audio={(withVoice ? "local WAV" : "generated silence")}, samples={imported.samples}.");
    }

    private static void WriteSilence(string voice)
    {
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
    }
}
