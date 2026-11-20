using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[Serializable] public sealed class GojoModelData
{
    public int formatVersion;
    public string sourceSha256;
    public GojoBoneData[] bones;
    public GojoMeshData[] meshes;
}
[Serializable] public sealed class GojoBoneData
{
    public string name;
    public int parent;
    public float[] rest, pose;
}
[Serializable] public sealed class GojoMeshData
{
    public string name, texture;
    public float[] positions, normals, uv, weights, posed;
    public int[] triangles, boneIndices, bonesPerVertex;
}

public static class GojoModelChecks
{
    public const string Prefab = "Assets/Gojo/Gojo.prefab";
    public static string Output => Directory.GetParent(Application.dataPath).Parent.FullName;
    public static GojoModelData ReadData() => JsonUtility.FromJson<GojoModelData>(File.ReadAllText(Path.Combine(Output, "Gojo.json")));
    public static void Verify()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
        var data = ReadData();
        Check(prefab, data);
        var normals = data.meshes[0].normals;
        for (int i = 0; i < 3; i++) normals[i] = -normals[i];
        bool rejected = false;
        try { Check(prefab, data); }
        catch (InvalidOperationException error) { rejected = error.Message.StartsWith("Blender/Unity normals differ"); }
        finally { for (int i = 0; i < 3; i++) normals[i] = -normals[i]; }
        Require(rejected, "Normal regression was not rejected");
        Debug.Log("GOJO PREFAB CHECKS PASS");
    }
    public static void VerifyBundle()
    {
        var data = ReadData();
        Verify();
        var bundle = AssetBundle.LoadFromFile(Path.Combine(Output, "bundle", "gojo-model"));
        Require(bundle != null, "Windows model bundle is missing");
        float maximum;
        try { maximum = Check(bundle.LoadAsset<GameObject>("gojo"), data); }
        finally { bundle.Unload(true); }
        File.WriteAllText(Path.Combine(Output, "verification.json"), JsonUtility.ToJson(new Verification {
            sourceSha256 = data.sourceSha256, meshes = data.meshes.Length, bones = data.bones.Length,
            vertices = data.meshes.Sum(m => m.posed.Length / 3), maximumPosedVertexErrorMeters = maximum,
            animatorDisabledPosePreserved = true, runtimeAttachmentValidated = true, windowsBundleReopened = true
        }, true));
        Debug.Log("GOJO BUNDLE CHECKS PASS");
    }
    [Serializable] private sealed class Verification
    {
        public string sourceSha256;
        public int meshes, bones, vertices;
        public float maximumPosedVertexErrorMeters;
        public bool animatorDisabledPosePreserved, runtimeAttachmentValidated, windowsBundleReopened;
    }
    public static float Check(GameObject prefab, GojoModelData data)
    {
        Require(prefab != null, "Blender Gojo prefab is missing");
        Require(data.formatVersion == 1 && data.bones.Length == 31 && data.meshes.Length == 7, "Unexpected export schema or rig");
        Require(prefab.GetComponentsInChildren<MonoBehaviour>(true).Length == 0, "Custom model scripts are not allowed");
        GojoBracken.BrackenModelAttachment.Validate(prefab);
        var instance = UnityEngine.Object.Instantiate(prefab);
        try
        {
            var animator = instance.GetComponent<Animator>();
            Require(animator != null && animator.runtimeAnimatorController != null && !animator.applyRootMotion, "Standing controller is missing");
            var nodes = instance.GetComponentsInChildren<Transform>(true);
            var positions = nodes.Select(n => n.localPosition).ToArray();
            var rotations = nodes.Select(n => n.localRotation).ToArray();
            animator.Rebind();
            for (int i = 0; i < 6; i++) animator.Update(.25f);
            for (int i = 0; i < nodes.Length; i++)
                Require(Vector3.Distance(positions[i], nodes[i].localPosition) < .00001f && Quaternion.Angle(rotations[i], nodes[i].localRotation) < .03f, "Controller changes the approved pose: " + nodes[i].name);
            animator.enabled = false;
            var skins = instance.GetComponentsInChildren<SkinnedMeshRenderer>();
            Require(skins.Length == 7, "Expected seven skins");
            float maximum = 0;
            foreach (var source in data.meshes)
            {
                var skin = skins.Single(s => s.name == source.name);
                Require(skin.bones.Length == 31 && skin.sharedMesh.bindposes.Length == 31 && skin.bones.All(b => b != null), "Skin has missing bones");
                Require(skin.sharedMaterials.Length == 1 && skin.sharedMaterial.shader.name == "HDRP/Lit" && skin.sharedMaterial.GetTexture("_BaseColorMap") != null, "HDRP material or texture is missing");
                var baked = new Mesh();
                try
                {
                    skin.BakeMesh(baked);
                    var vertices = baked.vertices;
                    var normals = baked.normals;
                    Require(vertices.Length * 3 == source.posed.Length, "Vertex count changed");
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        maximum = Mathf.Max(maximum, Vector3.Distance(skin.transform.TransformPoint(vertices[i]), V3(source.posed, i)));
                        var actualNormal = skin.transform.TransformDirection(normals[i]).normalized;
                        Require(Vector3.Dot(actualNormal, V3(source.normals, i).normalized) > .9999f,
                            "Blender/Unity normals differ: " + source.name + " vertex " + i);
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(baked); }
            }
            Require(maximum < .00001f, "Blender/Unity deformation differs: " + maximum + " metres");
            Debug.Log("GOJO GEOMETRY PASS: maximum posed vertex error=" + maximum + "m; vertices=" + skins.Sum(s => s.sharedMesh.vertexCount));
            GojoBracken.BrackenModelAttachment.UseStandingPose(instance.transform);
            Require(!animator.enabled, "Runtime must disable the Animator");
            Vector3 facing = Vector3.zero;
            foreach (string side in new[] { "left", "right" })
                facing += nodes.Single(n => n.name == side + "FootToe").position - nodes.Single(n => n.name == side + "Foot").position;
            Require(Vector3.Angle(Vector3.ProjectOnPlane(facing, Vector3.up), Vector3.forward) < .03f, "Runtime facing correction failed");
            instance.transform.position = new Vector3(3, 0, 2);
            foreach (var source in data.meshes)
            {
                var skin = skins.Single(s => s.name == source.name);
                var baked = new Mesh();
                try
                {
                    skin.BakeMesh(baked);
                    var vertices = baked.vertices;
                    for (int i = 0; i < vertices.Length; i++)
                        maximum = Mathf.Max(maximum, Vector3.Distance(skin.transform.TransformPoint(vertices[i]), instance.transform.TransformPoint(V3(source.posed, i))));
                }
                finally { UnityEngine.Object.DestroyImmediate(baked); }
            }
            Require(maximum < .00001f, "Runtime facing or anchor movement deforms the model: " + maximum);
            Debug.Log("GOJO RUNTIME ATTACHMENT PASS: validation, Animator disabling, facing correction and anchor movement; maximum error=" + maximum + "m");
            return maximum;
        }
        finally { UnityEngine.Object.DestroyImmediate(instance); }
    }
    public static Vector3 V3(float[] a, int i) => new Vector3(a[i * 3], a[i * 3 + 1], a[i * 3 + 2]);
    public static Matrix4x4 Matrix(float[] a)
    {
        var result = new Matrix4x4();
        for (int r = 0; r < 4; r++) for (int c = 0; c < 4; c++) result[r, c] = a[r * 4 + c];
        return result;
    }
    public static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
