using System;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public static class ImportGojo
{
    private const string Folder = "Assets/Gojo";
    public static void Run()
    {
        GojoModelChecks.Require(Application.unityVersion == "2022.3.62f2", "Use Unity 2022.3.62f2");
        GojoModelChecks.Require(!AssetDatabase.IsValidFolder(Folder), "Gojo outputs already exist; refusing to overwrite");
        var data = GojoModelChecks.ReadData();
        GojoModelChecks.Require(data.formatVersion == 1 && data.bones.Length == 31 && data.meshes.Length == 7, "Unexpected Blender export");
        PlayerSettings.colorSpace = ColorSpace.Linear;
        QualitySettings.skinWeights = SkinWeights.FourBones;
        var pipeline = ScriptableObject.CreateInstance<HDRenderPipelineAsset>();
        AssetDatabase.CreateAsset(pipeline, "Assets/GojoPipeline.asset");
        GraphicsSettings.defaultRenderPipeline = pipeline;
        AssetDatabase.CreateFolder("Assets", "Gojo");
        foreach (string name in new[] { "Meshes", "Materials", "Textures" }) AssetDatabase.CreateFolder(Folder, name);
        var materials = new System.Collections.Generic.Dictionary<string, Material>();
        foreach (string name in new[] { "Face", "Hair", "Jacket", "Pants" })
        {
            string path = Folder + "/Textures/" + name + ".png";
            File.Copy(Path.Combine(GojoModelChecks.Output, "..", "blender", "source", "textures", name + ".png"), path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.SaveAndReimport();
            var material = new Material(Shader.Find("HDRP/Lit")) { name = "Gojo " + name };
            material.SetTexture("_BaseColorMap", AssetDatabase.LoadAssetAtPath<Texture2D>(path));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_MaterialID", 4);
            material.SetColor("_SpecularColor", new Color(.003f, .003f, .003f));
            material.SetFloat("_Smoothness", .15f);
            material.SetFloat("_DoubleSidedEnable", 1);
            HDMaterial.ValidateMaterial(material);
            AssetDatabase.CreateAsset(material, Folder + "/Materials/" + name + ".mat");
            materials.Add(name + ".png", material);
        }
        var root = new GameObject("Gojo");
        try
        {
            var rig = new GameObject("Gojo Rig").transform;
            rig.SetParent(root.transform, false);
            var bones = data.bones.Select(b => new GameObject(b.name).transform).ToArray();
            for (int i = 0; i < bones.Length; i++)
            {
                int parent = data.bones[i].parent;
                bones[i].SetParent(parent < 0 ? rig : bones[parent], false);
                var pose = GojoModelChecks.Matrix(data.bones[i].pose);
                var local = parent < 0 ? pose : GojoModelChecks.Matrix(data.bones[parent].pose).inverse * pose;
                bones[i].localPosition = local.GetColumn(3);
                bones[i].localRotation = local.rotation;
                bones[i].localScale = local.lossyScale;
            }
            foreach (var source in data.meshes)
            {
                int count = source.posed.Length / 3;
                var mesh = new Mesh { name = source.name, indexFormat = count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.vertices = Enumerable.Range(0, count).Select(i => GojoModelChecks.V3(source.posed, i)).ToArray();
                mesh.normals = Enumerable.Range(0, count).Select(i => GojoModelChecks.V3(source.normals, i)).ToArray();
                mesh.uv = Enumerable.Range(0, count).Select(i => new Vector2(source.uv[i * 2], source.uv[i * 2 + 1])).ToArray();
                mesh.triangles = source.triangles;
                mesh.bindposes = data.bones.Select(b => GojoModelChecks.Matrix(b.pose).inverse).ToArray();
                var weights = Enumerable.Range(0, source.weights.Length).Select(i => new BoneWeight1 { boneIndex = source.boneIndices[i], weight = source.weights[i] }).ToArray();
                using (var counts = new NativeArray<byte>(source.bonesPerVertex.Select(n => checked((byte)n)).ToArray(), Allocator.Temp))
                using (var values = new NativeArray<BoneWeight1>(weights, Allocator.Temp)) mesh.SetBoneWeights(counts, values);
                mesh.RecalculateBounds();
                mesh.RecalculateTangents();
                AssetDatabase.CreateAsset(mesh, Folder + "/Meshes/" + source.name + ".asset");
                var node = new GameObject(source.name);
                node.transform.SetParent(root.transform, false);
                var skin = node.AddComponent<SkinnedMeshRenderer>();
                skin.sharedMesh = mesh;
                skin.sharedMaterial = materials[source.texture];
                skin.bones = bones;
                skin.rootBone = bones[Array.FindIndex(data.bones, b => b.parent < 0)];
                skin.localBounds = mesh.bounds;
                skin.updateWhenOffscreen = true;
            }
            var clip = new AnimationClip { name = "StandingIdle", frameRate = 30 };
            foreach (var node in root.GetComponentsInChildren<Transform>())
            {
                string path = AnimationUtility.CalculateTransformPath(node, root.transform);
                Vector3 p = node.localPosition, s = node.localScale;
                Quaternion q = node.localRotation;
                Curve(clip, path, "m_LocalPosition", new[] { p.x, p.y, p.z });
                Curve(clip, path, "m_LocalRotation", new[] { q.x, q.y, q.z, q.w });
                Curve(clip, path, "m_LocalScale", new[] { s.x, s.y, s.z });
            }
            clip.EnsureQuaternionContinuity();
            var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, Folder + "/StandingIdle.anim");
            var controller = AnimatorController.CreateAnimatorControllerAtPath(Folder + "/StandingIdle.controller");
            var state = controller.layers[0].stateMachine.AddState("Standing"); state.motion = clip;
            controller.layers[0].stateMachine.defaultState = state;
            root.AddComponent<Animator>().runtimeAnimatorController = controller;
            GojoModelChecks.Require(PrefabUtility.SaveAsPrefabAsset(root, GojoModelChecks.Prefab) != null, "Could not save Gojo prefab");
            File.WriteAllText(Folder + "/Source.txt", "Blender SHA-256: " + data.sourceSha256 + "\nStanding bind pose, serialized bones, constant idle.\n");
            AssetDatabase.SaveAssets();
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
        GojoModelChecks.Verify();
        string output = Path.Combine(GojoModelChecks.Output, "bundle"); Directory.CreateDirectory(output);
        var result = BuildPipeline.BuildAssetBundles(output, new[] { new AssetBundleBuild {
            assetBundleName = "gojo-model", assetNames = new[] { GojoModelChecks.Prefab }, addressableNames = new[] { "gojo" }
        } }, BuildAssetBundleOptions.StrictMode | BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64);
        GojoModelChecks.Require(result != null, "Windows model bundle build failed");
        var bundle = AssetBundle.LoadFromFile(Path.Combine(output, "gojo-model"));
        GojoModelChecks.Require(bundle != null, "Windows model bundle could not reopen");
        try { GojoModelChecks.Check(bundle.LoadAsset<GameObject>("gojo"), data); }
        finally { bundle.Unload(true); }
        Debug.Log("GOJO UNITY IMPORT PASS: saved prefab and reopened Windows model bundle preserve the approved standing model");
    }
    private static void Curve(AnimationClip clip, string path, string property, float[] values)
    {
        string[] axes = { ".x", ".y", ".z", ".w" };
        for (int i = 0; i < values.Length; i++) clip.SetCurve(path, typeof(Transform), property + axes[i], AnimationCurve.Constant(0, 1, values[i]));
    }
}
