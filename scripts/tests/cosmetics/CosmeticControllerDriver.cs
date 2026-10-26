using UnityEditor;
using UnityEditor.SceneManagement;
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
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/CosmeticChecks.unity");
        SessionState.SetBool("GojoCosmeticChecks", true);
        SessionState.SetFloat("GojoCosmeticDeadline", (float)EditorApplication.timeSinceStartup + 60f);
        EditorApplication.EnterPlaymode();
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
