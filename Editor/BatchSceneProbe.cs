using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BatchSceneProbe
{
    public static void OpenSceneByEnv()
    {
        string scenePath = Environment.GetEnvironmentVariable("SCENE_TO_OPEN");
        if (string.IsNullOrWhiteSpace(scenePath))
        {
            scenePath = "Assets/Scenes/Level_01_TrenchRift.unity";
        }

        Debug.Log($"[BatchSceneProbe] SCENE_TO_OPEN={scenePath}");

        try
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Debug.Log($"[BatchSceneProbe] OPEN_OK path={scene.path}");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BatchSceneProbe] OPEN_FAIL {ex}");
            EditorApplication.Exit(2);
        }
    }
}
