using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AssignMainMenuPreviewFrames
{
    [MenuItem("Tools/Main Menu/Assign Preview Frames")]
    public static void Assign()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);

        var assignments = new Dictionary<string, string>
        {
            ["Level1Button"] = "Assets/Art/Characters/Level1/Menu_animation.png",
            ["Level3Button"] = "Assets/Art/Characters/Level2/Player/Animations/Run/Run_while_shooting_Left.png",
            ["Level4Button"] = "Assets/Art/Characters/Level4/Player/Animations/Dash/Dash_right_Down.png",
            ["Level5Button"] = "Assets/Art/Level5/main_menu_chest.png"
        };

        foreach (var kvp in assignments)
        {
            var gameObject = GameObject.Find(kvp.Key);
            if (gameObject == null)
            {
                Debug.LogError($"Could not find {kvp.Key} in MainMenu scene.");
                continue;
            }

            var component = gameObject.GetComponent<MainMenuHoverPreview>();
            if (component == null)
            {
                Debug.LogError($"{kvp.Key} does not have MainMenuHoverPreview.");
                continue;
            }

            var sprites = AssetDatabase.LoadAllAssetsAtPath(kvp.Value).OfType<Sprite>().ToArray();
            if (sprites.Length == 0)
            {
                Debug.LogError($"No sprites found for {kvp.Key} at {kvp.Value}");
                continue;
            }

            var so = new SerializedObject(component);
            var prop = so.FindProperty("frames");
            prop.arraySize = sprites.Length;

            for (int i = 0; i < sprites.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(component);
            Debug.Log($"Assigned {sprites.Length} sprites to {kvp.Key} from {kvp.Value}");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Main menu preview frames assigned.");
    }
}
