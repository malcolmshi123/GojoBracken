using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ObserverLifecycleDriver
{
    static ObserverLifecycleDriver() { EditorApplication.update += Watch; }

    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        var enemy = new GameObject("LifecyclePrefab");
        enemy.AddComponent<FlowermanAI>();
        var prefab = PrefabUtility.SaveAsPrefabAsset(enemy, "Assets/LifecyclePrefab.prefab");
        UnityEngine.Object.DestroyImmediate(enemy);
        var probe = new GameObject("ObserverLifecycleProbe").AddComponent<ObserverLifecycleProbe>();
        probe.LoadedPrefab = prefab;
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/Lifecycle.unity");
        SessionState.SetBool("GojoLifecycleRunning", true);
        SessionState.SetFloat("GojoLifecycleDeadline", (float)EditorApplication.timeSinceStartup + 45f);
        EditorApplication.EnterPlaymode();
    }

    private static void Watch()
    {
        if (SessionState.GetBool("GojoLifecycleRunning", false)
            && EditorApplication.timeSinceStartup > SessionState.GetFloat("GojoLifecycleDeadline", float.MaxValue))
        {
            Debug.LogError("OBSERVER LIFECYCLE FAIL: Play-mode callback watchdog expired.");
            EditorApplication.Exit(1);
        }
    }
}
