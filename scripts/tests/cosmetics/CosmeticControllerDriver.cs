using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using UnityEngine;

[InitializeOnLoad]
public static class CosmeticControllerDriver
{
    static CosmeticControllerDriver() { EditorApplication.update += Watch; }
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        var probe = new GameObject("Cosmetic checks").AddComponent<CosmeticControllerProbe>();
        probe.BakedAura = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GojoBracken/Prefabs/Aura.prefab");
        probe.ModelFixture = CreateModelFixture();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/CosmeticChecks.unity");
        SessionState.SetBool("GojoCosmeticChecks", true);
        SessionState.SetFloat("GojoCosmeticDeadline", (float)EditorApplication.timeSinceStartup + 60f);
        EditorApplication.EnterPlaymode();
    }
    private static GameObject CreateModelFixture()
    {
        var mesh = new Mesh { name = "Synthetic attachment test triangle" };
        mesh.vertices = new[] { Vector3.zero, Vector3.right, Vector3.up };
        mesh.triangles = new[] { 0, 1, 2 };
        mesh.bindposes = new[] { Matrix4x4.identity };
        mesh.boneWeights = new[] {
            new BoneWeight { boneIndex0 = 0, weight0 = 1f },
            new BoneWeight { boneIndex0 = 0, weight0 = 1f },
            new BoneWeight { boneIndex0 = 0, weight0 = 1f } };
        mesh.RecalculateNormals();
        AssetDatabase.CreateAsset(mesh, "Assets/FixtureMesh.asset");
        var material = new Material(Shader.Find("Standard"));
        AssetDatabase.CreateAsset(material, "Assets/FixtureMaterial.mat");
        var clip = new AnimationClip();
        clip.SetCurve("Bone", typeof(Transform), "m_LocalPosition.y", AnimationCurve.Constant(0f, 1f, 0f));
        AssetDatabase.CreateAsset(clip, "Assets/FixtureIdle.anim");
        var controller = AnimatorController.CreateAnimatorControllerAtPath("Assets/FixtureIdle.controller");
        controller.layers[0].stateMachine.AddState("Idle").motion = clip;
        var root = new GameObject("Synthetic model, not Gojo");
        var bone = new GameObject("Bone").transform;
        bone.SetParent(root.transform, false);
        var skin = root.AddComponent<SkinnedMeshRenderer>();
        skin.sharedMesh = mesh; skin.sharedMaterial = material;
        skin.bones = new[] { bone }; skin.rootBone = bone;
        root.AddComponent<Animator>().runtimeAnimatorController = controller;
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, "Assets/ModelFixture.prefab");
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        return prefab;
    }
    private static void Watch()
    {
        if (SessionState.GetBool("GojoCosmeticChecks", false)
            && EditorApplication.timeSinceStartup > SessionState.GetFloat("GojoCosmeticDeadline", float.MaxValue))
        {
            Debug.LogError("COSMETIC CONTROLLER FAIL: Play-mode watchdog expired.");
            EditorApplication.Exit(1);
        }
    }
}
