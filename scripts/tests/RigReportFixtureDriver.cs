using System;
using System.Collections.Generic;
using System.IO;
using GojoBracken;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
public static class RigReportFixtureDriver
{
    public static void Run()
    {
        string stamp = Guid.NewGuid().ToString("N").Substring(0, 8);
        string assetFolder = "Assets/LocalRigFixture" + stamp;
        AssetDatabase.CreateFolder("Assets", "LocalRigFixture" + stamp);
        string output = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "..", "artifacts", "rig-checks-" + stamp);
        var messages = new List<string>();
        var warnings = new List<string>();
        var root = new GameObject("FixtureEnemy");
        root.AddComponent<RigReportFixtureAI>();
        var boneRoot = new GameObject("Armature").transform;
        boneRoot.SetParent(root.transform, false);
        var spine = new GameObject("Spine").transform;
        spine.SetParent(boneRoot, false);
        spine.localPosition = Vector3.up;
        var skin = root.AddComponent<SkinnedMeshRenderer>();
        var mesh = new Mesh { name = "FixtureMesh" };
        mesh.vertices = new[] { Vector3.zero, Vector3.right, Vector3.up };
        mesh.triangles = new[] { 0, 1, 2 };
        mesh.bindposes = new[] { Matrix4x4.identity, spine.worldToLocalMatrix * root.transform.localToWorldMatrix };
        mesh.boneWeights = new[] {
            new BoneWeight { boneIndex0=0, weight0=1f },
            new BoneWeight { boneIndex0=0, weight0=1f },
            new BoneWeight { boneIndex0=1, weight0=1f } };
        AssetDatabase.CreateAsset(mesh, assetFolder + "/FixtureMesh.asset");
        skin.sharedMesh = mesh;
        skin.bones = new[] { boneRoot, spine };
        skin.rootBone = boneRoot;
        var material = new Material(Shader.Find("Standard"));
        AssetDatabase.CreateAsset(material, assetFolder + "/FixtureMaterial.mat");
        skin.sharedMaterial = material;
        var clip = new AnimationClip { name = "FixtureIdle" };
        clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1, 0));
        AssetDatabase.CreateAsset(clip, assetFolder + "/FixtureIdle.anim");
        var controller = AnimatorController.CreateAnimatorControllerAtPathWithClip(assetFolder + "/Fixture.controller", clip);
        controller.AddParameter("FixtureSpeed", AnimatorControllerParameterType.Float);
        controller.AddParameter("FixtureAttack", AnimatorControllerParameterType.Trigger);
        root.AddComponent<Animator>().runtimeAnimatorController = controller;
        AssetDatabase.SaveAssets();

        var sceneOnly = new BrackenRigReport(typeof(RigReportFixtureAI), output + "-none", "test", messages.Add, warnings.Add);
        for (int i = 0; i < 24; i++) sceneOnly.Update(i * 5f);
        Require(!Directory.Exists(output + "-none"), "scene instance must not be exported");
        Require(warnings.Count == 1 && warnings[0].Contains("No loaded Bracken prefab"), "bounded timeout");
        Debug.Log("RIG CHECK PASS: scene exclusion and bounded timeout");

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, assetFolder + "/Fixture.prefab");
        UnityEngine.Object.DestroyImmediate(root);
        var ai = prefab.GetComponent<RigReportFixtureAI>();
        string before = EditorJsonUtility.ToJson(prefab.transform);
        var report = new BrackenRigReport(typeof(RigReportFixtureAI), output, "test", messages.Add, warnings.Add);
        report.Update(0f);
        report.Update(10f);
        var files = Directory.GetFiles(output, "bracken-rig-*.txt");
        Require(files.Length == 1, "one unique report per run");
        string text = File.ReadAllText(files[0]);
        foreach(string expected in new[] { "FixtureHunt", "FixtureAggro", "./Armature[0]/Spine[0]",
            "bone[1]", "FixtureIdle", "parameters=<unavailable: prefab Animator not initialized", "schema=1" })
            Require(text.Contains(expected), "report contains " + expected);
        Require(ai.angerMeter == 0.25f && before == EditorJsonUtility.ToJson(prefab.transform), "prefab unchanged");
        Debug.Log("RIG CHECK PASS: loaded prefab, bones, states, clips, explicit unavailable parameters, single export, no mutation");

        int warningCount = warnings.Count;
        var unavailable = new BrackenRigReport(null, output + "-missing", "test", messages.Add, warnings.Add);
        unavailable.Update(0f);
        Require(warnings.Count == warningCount + 1, "missing component handled");
        string blocked = output + "-blocked";
        File.WriteAllText(blocked, "fixture");
        var failure = new BrackenRigReport(typeof(RigReportFixtureAI), blocked, "test", messages.Add, warnings.Add);
        failure.Update(0f);
        failure.Update(10f);
        Require(warnings.Count == warningCount + 2 && warnings[warnings.Count - 1].Contains("Export stopped"), "I/O failure stops safely");
        Debug.Log("RIG CHECK PASS: unavailable type and I/O failure");
        int beforeCancel = warnings.Count;
        var cancelled = new BrackenRigReport(typeof(RigReportEmptyAI), output + "-cancelled", "test", messages.Add, warnings.Add);
        cancelled.Update(0f);
        cancelled.Cancel("fixture disabled");
        cancelled.Cancel("repeat");
        cancelled.Update(120f);
        Require(warnings.Count == beforeCancel + 1 && warnings[warnings.Count - 1].Contains("Search cancelled after 1/24"), "single cancellation message");
        Require(messages.Exists(line => line.Contains("Scan 1/24 complete")), "first scan progress");
        Require(!Directory.Exists(output + "-cancelled"), "cancelled search does not export");
        Debug.Log("RIG CHECK PASS: first scan progress and explicit cancellation");
        Debug.Log("RIG CHECK REPORT: " + files[0]);
    }
    private static void Require(bool success, string message)
    {
        if (!success) throw new InvalidOperationException("Rig report check failed: " + message);
    }
}
