using System;
using System.IO;
using System.Linq;
using GojoBracken;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AssetBundleChecks
{
    public static void Run()
    {
        Directory.CreateDirectory("Assets/Fixtures");
        AssetDatabase.Refresh();
        var shader = Shader.Find("Standard");
        Require(shader != null, "fixture shader available");
        var material = new Material(shader);
        AssetDatabase.CreateAsset(material, "Assets/Fixtures/material.mat");
        var mesh = new Mesh { name = "Synthetic triangle; not Gojo" };
        mesh.vertices = new[] { Vector3.zero, Vector3.right, Vector3.up };
        mesh.triangles = new[] { 0, 1, 2 };
        mesh.bindposes = new[] { Matrix4x4.identity };
        mesh.boneWeights = Enumerable.Repeat(new BoneWeight { boneIndex0 = 0, weight0 = 1f }, 3).ToArray();
        AssetDatabase.CreateAsset(mesh, "Assets/Fixtures/mesh.asset");
        var controller = AnimatorController.CreateAnimatorControllerAtPath("Assets/Fixtures/controller.controller");
        controller.layers[0].stateMachine.AddState("Synthetic idle");
        var model = new GameObject("Synthetic model - not a release asset");
        var bone = new GameObject("Bone").transform;
        bone.SetParent(model.transform, false);
        var skin = model.AddComponent<SkinnedMeshRenderer>();
        skin.sharedMesh = mesh;
        skin.sharedMaterial = material;
        skin.rootBone = bone;
        skin.bones = new[] { bone };
        model.AddComponent<Animator>().runtimeAnimatorController = controller;
        PrefabUtility.SaveAsPrefabAsset(model, "Assets/Fixtures/model.prefab");
        UnityEngine.Object.DestroyImmediate(model);
        var aura = new GameObject("Synthetic aura - not the approved effect");
        aura.AddComponent<ParticleSystem>();
        aura.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
        PrefabUtility.SaveAsPrefabAsset(aura, "Assets/Fixtures/aura.prefab");
        UnityEngine.Object.DestroyImmediate(aura);
        File.WriteAllText("Assets/Fixtures/credits.txt", "Synthetic test fixture only. Contains no Gojo model or voice line.");
        WriteSilentWav("Assets/Fixtures/voice.wav");
        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();

        string output = Path.GetFullPath("FixtureBundles");
        Directory.CreateDirectory(output);
        var good = new AssetBundleBuild {
            assetBundleName = "fixture-good",
            assetNames = new[] { "Assets/Fixtures/model.prefab", "Assets/Fixtures/aura.prefab", "Assets/Fixtures/voice.wav", "Assets/Fixtures/credits.txt" },
            addressableNames = new[] { GojoAssetContract.Model, GojoAssetContract.Aura, GojoAssetContract.Voice, GojoAssetContract.Credits }
        };
        var incomplete = new AssetBundleBuild {
            assetBundleName = "fixture-incomplete",
            assetNames = new[] { "Assets/Fixtures/model.prefab", "Assets/Fixtures/aura.prefab", "Assets/Fixtures/credits.txt" },
            addressableNames = new[] { GojoAssetContract.Model, GojoAssetContract.Aura, GojoAssetContract.Credits }
        };
        Require(BuildPipeline.BuildAssetBundles(output, new[] { good },
            BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode,
            BuildTarget.StandaloneWindows64) != null, "build real Windows bundles");
        string incompleteOutput = Path.Combine(output, "incomplete");
        Directory.CreateDirectory(incompleteOutput);
        Require(BuildPipeline.BuildAssetBundles(incompleteOutput, new[] { incomplete },
            BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode,
            BuildTarget.StandaloneWindows64) != null, "build incomplete bundle fixture");
        string installed = Path.GetFullPath("FixtureInstallation");
        Directory.CreateDirectory(installed);
        string path = Path.Combine(installed, GojoAssetContract.BundleName);
        int roots = SceneManager.GetActiveScene().rootCount;
        int bundles = AssetBundle.GetAllLoadedAssetBundles().Count();
        Reject(null, "directory");
        Reject(installed, "Missing gojobracken");
        File.WriteAllText(path, "not an AssetBundle");
        Reject(installed, "could not open");
        File.Copy(Path.Combine(incompleteOutput, "fixture-incomplete"), path, true);
        Reject(installed, "domain_expansion");
        Require(AssetBundle.GetAllLoadedAssetBundles().Count() == bundles, "failed loads release bundles");
        File.Copy(Path.Combine(output, "fixture-good"), path, true);
        Require(GojoAssetBundle.TryLoad(installed, out var loaded, out var error), error);
        Require(loaded.Model != null && loaded.Aura != null && loaded.Voice.samples > 0
            && loaded.Credits.Contains("Synthetic test fixture"), "named assets loaded");
        Require(SceneManager.GetActiveScene().rootCount == roots, "loader never instantiates scene objects");
        var loadedSkin = loaded.Model.GetComponent<SkinnedMeshRenderer>();
        var originalMaterial = loadedSkin.sharedMaterial;
        loadedSkin.sharedMaterial = null;
        RejectValidation(loaded.Model, loaded.Aura, loaded.Voice, new TextAsset(loaded.Credits), "material");
        loadedSkin.sharedMaterial = originalMaterial;
        var animator = loaded.Model.GetComponent<Animator>();
        var originalController = animator.runtimeAnimatorController;
        animator.runtimeAnimatorController = null;
        RejectValidation(loaded.Model, loaded.Aura, loaded.Voice, new TextAsset(loaded.Credits), "controller");
        animator.runtimeAnimatorController = originalController;
        var originalBones = loadedSkin.bones;
        loadedSkin.bones = new Transform[] { null };
        RejectValidation(loaded.Model, loaded.Aura, loaded.Voice, new TextAsset(loaded.Credits), "bone");
        loadedSkin.bones = originalBones;
        RejectValidation(loaded.Model, loaded.Aura, loaded.Voice, new TextAsset(" "), "asset_credits");
        RejectValidation(loaded.Model, loaded.Aura, null, new TextAsset(loaded.Credits), "domain_expansion");
        var empty = new GameObject("Invalid empty prefab");
        RejectValidation(empty, loaded.Aura, loaded.Voice, new TextAsset(loaded.Credits), "skinned mesh");
        RejectValidation(loaded.Model, empty, loaded.Voice, new TextAsset(loaded.Credits), "ParticleSystem");
        UnityEngine.Object.DestroyImmediate(empty);
        var ownedModel = loaded.Model;
        loaded.Dispose();
        loaded.Dispose();
        Require(loaded.Model == null && loaded.Voice == null && loaded.Credits == null && ownedModel == null,
            "idempotent disposal unloads owned assets");
        Require(AssetBundle.GetAllLoadedAssetBundles().Count() == bundles, "successful load releases bundle");
        Require(GojoAssetBundle.TryLoad(installed, out loaded, out error), "reload after dispose: " + error);
        loaded.Dispose();
        // Exercise the actual exporter too, entirely inside this ignored fixture project.
        Directory.CreateDirectory("Assets/GojoBracken/Prefabs");
        Directory.CreateDirectory("Assets/GojoBracken/Audio");
        AssetDatabase.Refresh();
        Require(AssetDatabase.CopyAsset("Assets/Fixtures/model.prefab", "Assets/GojoBracken/Prefabs/Gojo.prefab"), "copy model fixture");
        Require(AssetDatabase.CopyAsset("Assets/Fixtures/aura.prefab", "Assets/GojoBracken/Prefabs/Aura.prefab"), "copy aura fixture");
        Require(AssetDatabase.CopyAsset("Assets/Fixtures/voice.wav", "Assets/GojoBracken/Audio/DomainExpansion.wav"), "copy voice fixture");
        Require(AssetDatabase.CopyAsset("Assets/Fixtures/credits.txt", "Assets/GojoBracken/CREDITS.txt"), "copy credits fixture");
        GojoBracken.Editor.BuildGojoAssets.BuildWindowsBundle();
        string exported = Path.Combine(Directory.GetParent(Application.dataPath).Parent.FullName, "artifacts", "assets");
        Require(GojoAssetBundle.TryLoad(exported, out loaded, out error), "production exporter round-trip: " + error);
        Require(File.ReadAllText(Path.Combine(exported, "ASSET-CREDITS.txt")) == loaded.Credits, "embedded and sidecar credits agree");
        loaded.Dispose();
        Debug.Log("ASSET BUNDLE PASS: real Windows bundle, missing/corrupt/incomplete rollback, named assets, structural rejection, no scene instances, dispose and reload.");
        Debug.Log("ASSET EXPORTER PASS: production exporter round-trip and embedded/sidecar credits.");
    }

    private static void Reject(string directory, string fragment)
    {
        Require(!GojoAssetBundle.TryLoad(directory, out var loaded, out var error)
            && loaded == null && error != null && error.Contains(fragment) && error.Contains("keep vanilla"),
            "failed load: " + fragment + "; " + error);
    }

    private static void RejectValidation(GameObject model, GameObject aura, AudioClip voice, TextAsset credits, string fragment)
    {
        try { GojoAssetBundle.Validate(model, aura, voice, credits); }
        catch (InvalidDataException exception)
        {
            Require(exception.Message.Contains(fragment), "validation reason: " + exception.Message);
            UnityEngine.Object.DestroyImmediate(credits);
            return;
        }
        throw new InvalidOperationException("Accepted invalid assets: " + fragment);
    }

    private static void WriteSilentWav(string path)
    {
        using (var writer = new BinaryWriter(File.Create(path)))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + 1600);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
            writer.Write((short)1); writer.Write((short)1); writer.Write(8000); writer.Write(16000);
            writer.Write((short)2); writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(1600);
            writer.Write(new byte[1600]);
        }
    }

    private static void Require(bool condition, string reason)
    {
        if (!condition) throw new InvalidOperationException("Asset bundle check failed: " + reason);
    }
}
