using System;
using System.IO;
using System.Linq;
using UnityEngine;

public static class GojoBundleChecks
{
    public static string BundlePath => Path.Combine(GojoModelChecks.Output, "mod-bundle", "gojobracken");
    public static void Verify()
    {
        GojoModelChecks.Require(File.Exists(BundlePath), "Complete GojoBracken bundle is missing");
        var bundle = AssetBundle.LoadFromFile(BundlePath);
        GojoModelChecks.Require(bundle != null, "Complete GojoBracken bundle cannot reopen");
        try
        {
            GojoModelChecks.Check(bundle.LoadAsset<GameObject>("gojo"), GojoModelChecks.ReadData());
            GojoModelChecks.Require(bundle.LoadAsset<GameObject>("aura") != null, "Aura is missing");
            foreach (string name in new[] { "domain_expansion", "aggro_music" })
            {
                var clip = bundle.LoadAsset<AudioClip>(name);
                GojoModelChecks.Require(clip != null && clip.samples > 0, "Audio is missing: " + name);
            }
            GojoModelChecks.Require(bundle.LoadAsset<TextAsset>("asset_credits") != null, "Asset credits are missing");
            var expected = JsonUtility.FromJson<BundleSnapshot>(File.ReadAllText(Path.Combine(GojoModelChecks.Output, "expected-bundle.json")));
            var actual = BundleSnapshot.Capture(bundle);
            File.WriteAllText(Path.Combine(GojoModelChecks.Output, "actual-bundle.json"), JsonUtility.ToJson(actual, true));
            GojoModelChecks.Require(expected.aliases.SequenceEqual(actual.aliases), "Released asset aliases changed");
            foreach (var item in expected.audio)
            {
                var copy = actual.audio.Single(value => value.name == item.name);
                GojoModelChecks.Require(JsonUtility.ToJson(item) == JsonUtility.ToJson(copy), "Released decoded audio changed: " + item.name);
            }
            foreach (var item in expected.text)
                GojoModelChecks.Require(actual.text.Single(value => value.name == item.name).hash == item.hash, "Credits or voice metadata changed: " + item.name);
            GojoModelChecks.Require(expected.aura.Length == actual.aura.Length, "Aura component count changed");
            foreach (var item in expected.aura)
                GojoModelChecks.Require(actual.aura.Single(value => value.name == item.name).hash == item.hash, "Aura content changed: " + item.name);
        }
        finally { bundle.Unload(true); }
        GojoModelChecks.Require(GojoBracken.GojoAssetBundle.TryLoad(Path.GetDirectoryName(BundlePath), out var runtime, out string error), error);
        using (runtime)
        {
            GojoModelChecks.Require(runtime.BoostVoice && runtime.Music != null, "Runtime voice boost or music was lost");
            GojoBracken.BrackenModelAttachment.Validate(runtime.Model);
        }
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(BundlePath), "verification.json"),
            "{\"modelPoseVerified\":true,\"auraMatchesRelease\":true,\"decodedAudioMatchesRelease\":true,\"assetAliasesPreserved\":true,\"runtimeAssetLoaderPassed\":true}");
        Debug.Log("GOJO COMPLETE BUNDLE CHECKS PASS");
    }
}
