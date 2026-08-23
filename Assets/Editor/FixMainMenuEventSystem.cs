using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// One-click fix: ensures the MainMenu scene's EventSystem has a StandaloneInputModule.
/// Run this once from Prototype -> Fix MainMenu EventSystem.
/// Without an input module, Button onClick events are never processed and clicks do nothing.
/// </summary>
public static class FixMainMenuEventSystem
{
    private const string mainMenuScenePath = "Assets/Scenes/MainMenu.unity";

    [MenuItem("Prototype/Fix MainMenu EventSystem")]
    public static void Fix()
    {
        var mainMenuScene = EditorSceneManager.OpenScene(mainMenuScenePath, OpenSceneMode.Single);

        var eventSystem = Object.FindAnyObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogError("[FixMainMenuEventSystem] No EventSystem found in MainMenu scene. Run 'Create MainMenu Scene' first.");
            return;
        }

        // Add StandaloneInputModule if no input module is present
        var existing = eventSystem.GetComponent<StandaloneInputModule>();
        if (existing != null)
        {
            Debug.Log("[FixMainMenuEventSystem] StandaloneInputModule already present — nothing to do.");
        }
        else
        {
            Undo.AddComponent<StandaloneInputModule>(eventSystem.gameObject);
            Debug.Log("[FixMainMenuEventSystem] StandaloneInputModule added to EventSystem.");
        }

        EditorSceneManager.MarkSceneDirty(mainMenuScene);
        EditorSceneManager.SaveScene(mainMenuScene);
        Debug.Log("[FixMainMenuEventSystem] MainMenu scene saved.");
    }
}
