/*
AddSceneLoaderToMain.cs

Editor utility: open the Main scene, add or reuse SceneLoader and wire the restart controller to it, then
save the scene. It is an explicit scene-wiring tool and may modify existing serialized references.
*/

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor utility: Add SceneLoader to the Main gameplay scene and wire RestartController to it.
public static class AddSceneLoaderToMain
{
    private const string mainScenePath = "Assets/Scenes/Main.unity";

    [MenuItem("Prototype/Add SceneLoader to Main Scene")]
    public static void AddToMain()
    {
        // Save current scene path so we can return
        string previousScenePath = SceneManager.GetActiveScene().path;

        var mainScene = EditorSceneManager.OpenScene(mainScenePath, OpenSceneMode.Single);

        // Check if SceneLoader already exists
        SceneLoader existing = Object.FindAnyObjectByType<SceneLoader>();
        if (existing != null)
        {
            Debug.Log("[AddSceneLoaderToMain] SceneLoader already present in Main scene.");
        }
        else
        {
            GameObject go = new GameObject("SceneLoader");
            Undo.RegisterCreatedObjectUndo(go, "Add SceneLoader");
            go.AddComponent<SceneLoader>();
            Debug.Log("[AddSceneLoaderToMain] SceneLoader added to Main scene.");
        }

        // Auto-wire RestartController if present and SceneLoader reference is missing
        RestartController restartController = Object.FindAnyObjectByType<RestartController>();
        SceneLoader sceneLoader = Object.FindAnyObjectByType<SceneLoader>();

        if (restartController != null && sceneLoader != null)
        {
            SerializedObject so = new SerializedObject(restartController);
            SerializedProperty prop = so.FindProperty("sceneLoader");
            if (prop != null && prop.objectReferenceValue == null)
            {
                prop.objectReferenceValue = sceneLoader;
                so.ApplyModifiedProperties();
                Debug.Log("[AddSceneLoaderToMain] RestartController wired to SceneLoader.");
            }
        }
        else if (restartController == null)
        {
            Debug.LogWarning("[AddSceneLoaderToMain] RestartController not found in Main scene. Wire SceneLoader manually if needed.");
        }

        EditorSceneManager.MarkSceneDirty(mainScene);
        EditorSceneManager.SaveScene(mainScene);
        Debug.Log("[AddSceneLoaderToMain] Main scene saved.");
    }
}
