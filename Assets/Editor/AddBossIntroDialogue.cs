using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Adds an EasyDS pre-fight dialogue to the boss prefab without touching its
// existing combat BoxCollider2D: a second, larger CircleCollider2D (isTrigger)
// is added so DetectPlayer can sense the player from further away.
public static class AddBossIntroDialogue
{
    private const string BossPrefabPath = "Assets/Prefabs/Enemy_Boss.prefab";
    private const string DialogueAssetPath = "Assets/DialogueSystem/Scriptable Object/Boss_Intro_Dialogue.asset";

    [MenuItem("Tools/Dialogue/Add Boss Intro Dialogue")]
    public static void AddDialogue()
    {
        var bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
        if (bossPrefab == null)
        {
            Debug.LogError($"Boss prefab not found at {BossPrefabPath}.");
            return;
        }

        // 1) Sample intro dialogue asset (only created once)
        Dialogue dialogue = AssetDatabase.LoadAssetAtPath<Dialogue>(DialogueAssetPath);
        if (dialogue == null)
        {
            dialogue = ScriptableObject.CreateInstance<Dialogue>();
            dialogue.dialogue = new List<string>
            {
                "Has llegado muy lejos... /line",
                "Pero aqui termina tu camino. /end"
            };
            AssetDatabase.CreateAsset(dialogue, DialogueAssetPath);
            AssetDatabase.SaveAssets();
        }

        // 2) Edit the prefab contents
        GameObject root = PrefabUtility.LoadPrefabContents(BossPrefabPath);
        try
        {
            if (root.GetComponent<DetectPlayer>() == null)
                root.AddComponent<DetectPlayer>();

            // Dedicated trigger collider for dialogue range; keeps the existing
            // BoxCollider2D (solid, used for physics + combat) untouched.
            var dialogueRangeCollider = root.GetComponents<CircleCollider2D>();
            CircleCollider2D rangeCol = null;
            foreach (var c in dialogueRangeCollider)
            {
                if (c.isTrigger) { rangeCol = c; break; }
            }
            if (rangeCol == null)
            {
                rangeCol = root.AddComponent<CircleCollider2D>();
                rangeCol.isTrigger = true;
                rangeCol.radius = 3f;
            }

            var easyDs = root.GetComponent<EasyDS>();
            if (easyDs == null)
                easyDs = root.AddComponent<EasyDS>();
            easyDs.Name = "Boss";
            easyDs.dialogue = new List<Dialogue> { dialogue };

            PrefabUtility.SaveAsPrefabAsset(root, BossPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        EditorUtility.DisplayDialog(
            "Boss Intro Dialogue Added",
            "Se agrego DetectPlayer + EasyDS al prefab del boss, con un CircleCollider2D adicional (Is Trigger) para detectar al jugador sin afectar su collider de combate.\n\n" +
            "Recuerda: EnemyMovement y EnemyContactDamage ahora se pausan automaticamente mientras haya un dialogo activo (DialogueGate).",
            "OK");
    }
}
