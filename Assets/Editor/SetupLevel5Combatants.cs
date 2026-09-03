/*
SetupLevel5Combatants.cs  (Editor tool)

Purpose:
- Rename the five Level 5 placeholders to readable combatant names, attach a Combatant component to each
  and write the prototype stat profiles into it.
- Add a small world-space TextMesh label above each placeholder showing the combatant name.
- Create the BattleController object and assign the five combatants to it in fixed turn order.

Run from: Prototype -> Level5 -> Setup Level5 Combatants

Notes:
- Idempotent: objects are looked up by their current name or their original placeholder name, and an
  existing Combatant / NameLabel is reused instead of duplicated.
- The profile table below is the authoring source. After running, the values live in the scene and can be
  tweaked directly in the Inspector.
*/

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SetupLevel5Combatants
{
    private const string ScenePath = "Assets/Scenes/Level5/Level5.unity";

    private struct Profile
    {
        public string ParentPath;
        public string OriginalName;
        public string ObjectName;
        public string CombatantName;
        public bool IsEnemy;
        public int MaxHP;
        public int Attack;
        public int Defense;
    }

    private static readonly Profile[] Profiles =
    {
        new Profile { ParentPath = "BattleArena",       OriginalName = "Enemy_Placeholder",       ObjectName = "Enemy_FinalEnemy", CombatantName = "FINAL ENEMY", IsEnemy = true,  MaxHP = 500, Attack = 30, Defense = 12 },
        new Profile { ParentPath = "BattleArena/Party", OriginalName = "Character_1_Placeholder", ObjectName = "Player_1_Warrior", CombatantName = "WARRIOR",     IsEnemy = false, MaxHP = 120, Attack = 25, Defense = 15 },
        new Profile { ParentPath = "BattleArena/Party", OriginalName = "Character_2_Placeholder", ObjectName = "Player_2_Tank",    CombatantName = "TANK",        IsEnemy = false, MaxHP = 160, Attack = 15, Defense = 25 },
        new Profile { ParentPath = "BattleArena/Party", OriginalName = "Character_3_Placeholder", ObjectName = "Player_3_Mage",    CombatantName = "MAGE",        IsEnemy = false, MaxHP = 90,  Attack = 35, Defense = 8  },
        new Profile { ParentPath = "BattleArena/Party", OriginalName = "Character_4_Placeholder", ObjectName = "Player_4_Support", CombatantName = "SUPPORT",     IsEnemy = false, MaxHP = 110, Attack = 18, Defense = 15 }
    };

    [MenuItem("Prototype/Level5/Setup Level5 Combatants")]
    public static void Setup()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var orderedCombatants = new Combatant[Profiles.Length];

        for (int i = 0; i < Profiles.Length; i++)
        {
            Profile profile = Profiles[i];
            Transform parent = FindInScene(profile.ParentPath);
            if (parent == null)
            {
                Debug.LogError($"[SetupLevel5Combatants] Parent not found: {profile.ParentPath}. Aborting.");
                return;
            }

            Transform target = parent.Find(profile.ObjectName) ?? parent.Find(profile.OriginalName);
            if (target == null)
            {
                Debug.LogError($"[SetupLevel5Combatants] Object not found: {profile.ParentPath}/{profile.OriginalName}. Aborting.");
                return;
            }

            target.gameObject.name = profile.ObjectName;

            var combatant = target.GetComponent<Combatant>();
            if (combatant == null)
                combatant = target.gameObject.AddComponent<Combatant>();

            var so = new SerializedObject(combatant);
            so.FindProperty("combatantName").stringValue = profile.CombatantName;
            so.FindProperty("isEnemy").boolValue  = profile.IsEnemy;
            so.FindProperty("maxHP").intValue     = profile.MaxHP;
            so.FindProperty("attack").intValue    = profile.Attack;
            so.FindProperty("defense").intValue   = profile.Defense;
            so.FindProperty("currentHP").intValue = profile.MaxHP;
            so.ApplyModifiedProperties();

            CreateOrUpdateNameLabel(target, profile.CombatantName);
            orderedCombatants[i] = combatant;

            Debug.Log($"[SetupLevel5Combatants] {profile.CombatantName} -> {profile.ParentPath}/{profile.ObjectName}");
        }

        SetupBattleController(orderedCombatants);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SetupLevel5Combatants] Done.");
    }

    private static void SetupBattleController(Combatant[] orderedCombatants)
    {
        Transform existing = FindInScene("BattleController");
        GameObject controllerGO = existing != null ? existing.gameObject : new GameObject("BattleController");

        var controller = controllerGO.GetComponent<Level5BattleController>();
        if (controller == null)
            controller = controllerGO.AddComponent<Level5BattleController>();

        var so = new SerializedObject(controller);
        SerializedProperty order = so.FindProperty("turnOrder");
        order.arraySize = orderedCombatants.Length;

        // Profiles[0] is the enemy, which acts last.
        for (int i = 0; i < orderedCombatants.Length; i++)
        {
            int slot = (i == 0) ? orderedCombatants.Length - 1 : i - 1;
            order.GetArrayElementAtIndex(slot).objectReferenceValue = orderedCombatants[i];
        }

        so.ApplyModifiedProperties();
        Debug.Log("[SetupLevel5Combatants] BattleController turn order assigned.");
    }

    private static void CreateOrUpdateNameLabel(Transform owner, string text)
    {
        Transform labelTransform = owner.Find("NameLabel");
        GameObject labelGO = labelTransform != null ? labelTransform.gameObject : new GameObject("NameLabel");
        labelGO.transform.SetParent(owner, false);

        // Neutralise the placeholder's scale so every label renders at the same world size.
        float parentScaleX = Mathf.Approximately(owner.localScale.x, 0f) ? 1f : owner.localScale.x;
        float parentScaleY = Mathf.Approximately(owner.localScale.y, 0f) ? 1f : owner.localScale.y;

        labelGO.transform.localScale    = new Vector3(1f / parentScaleX, 1f / parentScaleY, 1f);
        labelGO.transform.localPosition = new Vector3(0f, 0.5f + (0.4f / parentScaleY), -0.1f);

        var textMesh = labelGO.GetComponent<TextMesh>();
        if (textMesh == null)
            textMesh = labelGO.AddComponent<TextMesh>();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textMesh.text          = text;
        textMesh.font          = font;
        textMesh.fontSize      = 60;
        textMesh.characterSize = 0.08f;
        textMesh.anchor        = TextAnchor.MiddleCenter;
        textMesh.alignment     = TextAlignment.Center;
        textMesh.color         = Color.white;

        var renderer = labelGO.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = font.material;
        renderer.sortingOrder   = 10;
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
