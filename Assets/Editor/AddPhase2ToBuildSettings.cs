/*
AddPhase2ToBuildSettings.cs (Editor utility)

Purpose: Add Level3_Phase2 to Build Settings after scene file creation.
*/

using UnityEditor;
using UnityEditor.SceneManagement;

public static class AddPhase2ToBuildSettings
{
    private const string ScenePath = "Assets/Scenes/Level3/Level3_Phase2.unity";

    [MenuItem("Prototype/Roguelite/Add Level3 Phase 2 to Build Settings")]
    public static void AddToBuildSettings()
    {
        // Load current build scenes
        EditorBuildSettingsScene[] currentScenes = EditorBuildSettings.scenes;

        // Check if scene already exists in build settings
        foreach (var scene in currentScenes)
        {
            if (scene.path == ScenePath)
            {
                UnityEngine.Debug.Log("[AddPhase2ToBuildSettings] Scene already in Build Settings: " + ScenePath);
                return;
            }
        }

        // Create new array with existing scenes + new scene
        System.Collections.Generic.List<EditorBuildSettingsScene> sceneList = 
            new System.Collections.Generic.List<EditorBuildSettingsScene>(currentScenes);
        
        sceneList.Add(new EditorBuildSettingsScene(ScenePath, true));

        // Apply
        EditorBuildSettings.scenes = sceneList.ToArray();
        UnityEngine.Debug.Log("[AddPhase2ToBuildSettings] Added Level3_Phase2 to Build Settings at index " + (sceneList.Count - 1));
        AssetDatabase.SaveAssets();
    }
}
