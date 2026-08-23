using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Quick test setup: creates an NPC in the open scene wired for EasyDS, with a
// trigger collider (required for DetectPlayer.OnTriggerEnter2D to fire) and a
// sample Dialogue asset with correctly tagged lines ("/line", "/end").
public static class CreateDialogueTestNPC
{
    [MenuItem("Tools/Dialogue/Create Test NPC")]
    public static void CreateTestNpc()
    {
        // 1) Sample Dialogue asset (only created once)
        string soFolder = "Assets/DialogueSystem/Scriptable Object";
        string soPath = Path.Combine(soFolder, "TestNPC_Dialogue.asset");
        Dialogue dialogue = AssetDatabase.LoadAssetAtPath<Dialogue>(soPath);
        if (dialogue == null)
        {
            dialogue = ScriptableObject.CreateInstance<Dialogue>();
            dialogue.dialogue = new List<string>
            {
                "Hola! Esto es una prueba del sistema de dialogo. /line",
                "Si puedes leer esto, la deteccion del jugador funciona. /end"
            };
            AssetDatabase.CreateAsset(dialogue, soPath);
            AssetDatabase.SaveAssets();
        }

        // 2) NPC GameObject
        var npc = new GameObject("TestNPC");
        Undo.RegisterCreatedObjectUndo(npc, "Create Test NPC");

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/basic_obstacle.png");
        var sr = npc.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(0.3f, 0.8f, 0.9f);

        var col = npc.AddComponent<BoxCollider2D>();
        col.isTrigger = true; // required for DetectPlayer's OnTriggerEnter2D to fire
        col.size = new Vector2(2.5f, 2.5f); // generous interaction range for testing

        npc.AddComponent<DetectPlayer>();
        var easyDs = npc.AddComponent<EasyDS>();
        easyDs.Name = "Test NPC";
        easyDs.dialogue = new List<Dialogue> { dialogue };

        // Place near the scene view pivot (or player, if found) for convenience
        var playerGO = GameObject.FindWithTag("Player");
        Vector3 spawnPos = playerGO != null
            ? playerGO.transform.position + new Vector3(2f, 0f, 0f)
            : Vector3.zero;
        var sv = SceneView.lastActiveSceneView;
        if (playerGO == null && sv != null)
        {
            spawnPos = sv.pivot;
            spawnPos.z = 0f;
        }
        npc.transform.position = spawnPos;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = npc;

        EditorUtility.DisplayDialog(
            "Test NPC Created",
            "Se creo 'TestNPC' en la escena con un BoxCollider2D en modo Trigger, DetectPlayer y EasyDS ya configurados con un dialogo de prueba.\n\n" +
            "Recuerda: DialogueManager.interactKey debe tener una tecla asignada, y el Player debe tener tag 'Player' y un Collider2D/Rigidbody2D (ya deberia cumplirlo).",
            "OK");
    }
}
