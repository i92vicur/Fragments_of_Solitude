/*
FixLevel5MagicButtonScene.cs  (Editor tool, one-off scene migration)

Purpose:
- Clone the existing ITEM_Button into a new "MAGIC_Button" GameObject under
  BattleCanvas/ActionButtons_Panel, save the scene, then run the existing
  SetupLevel5ActionUI tool so it lays out/labels the new button and wires
  Level5BattleUI.magicButton / mageCombatant / supportCombatant.
- Does not touch Combatant.cs, Level5BattleController.cs or Level5BattleUI.cs.

Run from: Prototype -> Level5 -> Fix Level5 Magic Button Scene
*/

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class FixLevel5MagicButtonScene
{
    private const string ScenePath = "Assets/Scenes/Level5/Level5.unity";

    [MenuItem("Prototype/Level5/Fix Level5 Magic Button Scene")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Transform actionPanel = FindInScene("BattleCanvas/ActionButtons_Panel");
        if (actionPanel == null)
        {
            Debug.LogError("[FixLevel5MagicButtonScene] BattleCanvas/ActionButtons_Panel not found. Aborting.");
            return;
        }

        if (actionPanel.Find("MAGIC_Button") == null)
        {
            Transform template = actionPanel.Find("ITEM_Button");
            if (template == null)
            {
                Debug.LogError("[FixLevel5MagicButtonScene] ITEM_Button template not found. Aborting.");
                return;
            }

            GameObject clone = Object.Instantiate(template.gameObject, actionPanel);
            clone.name = "MAGIC_Button";

            // Clear any onClick entries copied from the template; Level5BattleUI wires listeners in Awake.
            var button = clone.GetComponent<Button>();
            if (button != null)
            {
                button.onClick = new Button.ButtonClickedEvent();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[FixLevel5MagicButtonScene] MAGIC_Button created from ITEM_Button template.");
        }
        else
        {
            Debug.Log("[FixLevel5MagicButtonScene] MAGIC_Button already exists, skipping creation.");
        }

        // Re-run the existing setup tool so it lays out/labels MAGIC_Button and wires
        // Level5BattleUI.magicButton / mageCombatant / supportCombatant.
        SetupLevel5ActionUI.Setup();
    }

    private static Transform FindInScene(string path)
    {
        string[] parts = path.Split('/');

        GameObject root = null;
        foreach (var candidate in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (candidate.name == parts[0]) { root = candidate; break; }
        }

        if (root == null) return null;

        Transform current = root.transform;
        for (int i = 1; i < parts.Length; i++)
        {
            current = current.Find(parts[i]);
            if (current == null) return null;
        }

        return current;
    }
}
