/*
SetupLevel5ActionUI.cs  (Editor tool)

Purpose:
- Turn the Level 5 placeholder buttons into the four real battle actions (Attack, Defend, Use Item,
  Magic), then attach Level5BattleUI to BattleCanvas and assign every reference it needs.

Run from: Prototype -> Level5 -> Setup Level5 Action UI

Notes:
- Run AFTER "Setup Level5 Combatants".
- Idempotent: an existing Level5BattleUI is reused instead of duplicated. The MAGIC placeholder is laid
  out and wired like the other three buttons instead of being removed, since it is now the Mage's
  character-specific action.
- No onClick UnityEvents are configured here. Level5BattleUI adds its listeners once in Awake, so the
  Inspector onClick lists stay empty and cannot accumulate duplicates.
*/

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SetupLevel5ActionUI
{
    private const string ScenePath = "Assets/Scenes/Level5/Level5.unity";

    private static readonly string[] PartyObjectNames =
    {
        "Player_1_Warrior",
        "Player_2_Tank",
        "Player_3_Mage",
        "Player_4_Support"
    };

    [MenuItem("Prototype/Level5/Setup Level5 Action UI")]
    public static void Setup()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Transform canvas = FindInScene("BattleCanvas");
        Transform actionPanel = FindInScene("BattleCanvas/ActionButtons_Panel");
        if (canvas == null || actionPanel == null)
        {
            Debug.LogError("[SetupLevel5ActionUI] BattleCanvas/ActionButtons_Panel not found. Aborting.");
            return;
        }

        Transform magic = actionPanel.Find("MAGIC_Button");
        Button attackButton = LayoutActionButton(actionPanel, "ATTACK_Button", "ATTACK", 30f);
        Button defendButton = LayoutActionButton(actionPanel, "DEFEND_Button", "DEFEND", 315f);
        Button itemButton   = LayoutActionButton(actionPanel, "ITEM_Button",   "USE ITEM", 600f);
        Button magicButton  = magic != null ? LayoutActionButton(actionPanel, "MAGIC_Button", "MAGIC", 885f) : null;

        if (attackButton == null || defendButton == null || itemButton == null)
        {
            Debug.LogError("[SetupLevel5ActionUI] One or more action buttons are missing. Aborting.");
            return;
        }

        var controller = Object.FindAnyObjectByType<Level5BattleController>();
        if (controller == null)
        {
            Debug.LogError("[SetupLevel5ActionUI] Level5BattleController not found. Run 'Setup Level5 Combatants' first.");
            return;
        }

        var battleUI = canvas.GetComponent<Level5BattleUI>();
        if (battleUI == null)
            battleUI = canvas.gameObject.AddComponent<Level5BattleUI>();

        var so = new SerializedObject(battleUI);
        so.FindProperty("battleController").objectReferenceValue = controller;
        so.FindProperty("attackButton").objectReferenceValue     = attackButton;
        so.FindProperty("defendButton").objectReferenceValue     = defendButton;
        so.FindProperty("itemButton").objectReferenceValue       = itemButton;
        so.FindProperty("magicButton").objectReferenceValue      = magicButton;
        so.FindProperty("combatMessageText").objectReferenceValue =
            GetComponentAt<Text>("BattleCanvas/CombatMessage_Panel/CombatMessage_Placeholder");
        so.FindProperty("enemyInfoText").objectReferenceValue =
            GetComponentAt<Text>("BattleCanvas/EnemyInfo_Panel/EnemyInfo_Placeholder");

        SerializedProperty party      = so.FindProperty("partyMembers");
        SerializedProperty slotTexts  = so.FindProperty("characterSlotTexts");
        SerializedProperty slotImages = so.FindProperty("characterSlotBackgrounds");
        party.arraySize      = PartyObjectNames.Length;
        slotTexts.arraySize  = PartyObjectNames.Length;
        slotImages.arraySize = PartyObjectNames.Length;

        for (int i = 0; i < PartyObjectNames.Length; i++)
        {
            party.GetArrayElementAtIndex(i).objectReferenceValue =
                GetComponentAt<Combatant>($"BattleArena/Party/{PartyObjectNames[i]}");

            Image slotImage = GetComponentAt<Image>($"BattleCanvas/CharacterInfo_Panel/CharacterSlot_{i + 1}");
            if (slotImage != null)
                slotImage.preserveAspect = true;

            slotTexts.GetArrayElementAtIndex(i).objectReferenceValue =
                GetComponentAt<Text>($"BattleCanvas/CharacterInfo_Panel/CharacterSlot_{i + 1}/CharacterSlot_{i + 1}_Placeholder");
            slotImages.GetArrayElementAtIndex(i).objectReferenceValue = slotImage;
        }

        so.FindProperty("enemyCombatant").objectReferenceValue =
            GetComponentAt<Combatant>("BattleArena/Enemy_FinalEnemy");
        so.FindProperty("mageCombatant").objectReferenceValue =
            GetComponentAt<Combatant>("BattleArena/Party/Player_3_Mage");
        so.FindProperty("supportCombatant").objectReferenceValue =
            GetComponentAt<Combatant>("BattleArena/Party/Player_4_Support");

        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SetupLevel5ActionUI] Action UI wired: ATTACK / DEFEND / USE ITEM / MAGIC.");
    }

    private static Button LayoutActionButton(Transform panel, string objectName, string label, float x)
    {
        Transform buttonTransform = panel.Find(objectName);
        if (buttonTransform == null)
        {
            Debug.LogError($"[SetupLevel5ActionUI] Button not found: {objectName}");
            return null;
        }

        var rect = buttonTransform.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(x, -30f);

        Transform labelTransform = buttonTransform.Find("Label");
        if (labelTransform != null)
        {
            var labelText = labelTransform.GetComponent<Text>();
            if (labelText != null)
                labelText.text = label;
        }

        return buttonTransform.GetComponent<Button>();
    }

    private static T GetComponentAt<T>(string path) where T : Component
    {
        Transform target = FindInScene(path);
        return target != null ? target.GetComponent<T>() : null;
    }

    private static Transform FindInScene(string path)
    {
        string[] parts = path.Split('/');

        GameObject root = null;
        foreach (var candidate in SceneManager.GetActiveScene().GetRootGameObjects())
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
