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
        new GameObject("Cosmetic checks").AddComponent<CosmeticControllerProbe>();
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
