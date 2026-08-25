/*
CreateSpawnPoints.cs

Editor utility: create or replace the PlayerStart and BossSpawnPoint objects in Main and wire the boss point
into EnemySpawner. It edits and saves the scene selected by the tool's setup flow.
*/

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor utility: create PlayerStart and BossSpawnPoint in the Main scene and wire EnemySpawner.bossSpawnPoint
public static class CreateSpawnPoints
{
    private const string scenePath = "Assets/Scenes/Main.unity";

    [MenuItem("Prototype/Place PlayerStart & BossSpawnPoint in Main Scene")]
    public static void PlaceSpawnPointsInMain()
    {
        // Attempt to open Main scene if it exists
        Scene scene = SceneManager.GetActiveScene();
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null)
        {
            if (scene.path != scenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
        }

        // Work on the active scene
        if (!scene.isLoaded)
        {
            Debug.LogError("Main scene is not loaded. Please create and save a scene at Assets/Scenes/Main.unity first.");
            return;
        }

        // Find existing Player object (by tag or PlayerMovement)
        GameObject playerGO = GameObject.FindWithTag("Player");
        if (playerGO == null)
        {
            // Try to find by component name "PlayerMovement"
            var all = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var m in all)
            {
                if (m == null) continue;
                if (m.GetType().Name == "PlayerMovement") { playerGO = m.gameObject; break; }
            }
        }

        Vector3 playerStartPos = new Vector3(0f, -4f, 0f);
        if (playerGO != null)
        {
            playerStartPos = playerGO.transform.position;
        }

        // Create PlayerStart if missing
        GameObject playerStart = GameObject.Find("PlayerStart");
        if (playerStart == null)
        {
            playerStart = new GameObject("PlayerStart");
            Undo.RegisterCreatedObjectUndo(playerStart, "Create PlayerStart");
            playerStart.transform.position = playerStartPos;
        }
        else
        {
            Debug.Log("PlayerStart already exists in the scene. Leaving its transform as-is.");
        }

        // Move player to PlayerStart if player exists
        if (playerGO != null)
        {
            Undo.RecordObject(playerGO.transform, "Move Player to PlayerStart");
            playerGO.transform.position = playerStart.transform.position;
            Debug.Log("Moved Player GameObject to PlayerStart position.");
        }
        else
        {
            Debug.LogWarning("Player GameObject not found. PlayerStart created but you must place or assign the Player manually.");
        }

        // Determine boss spawn candidate position
        Vector2 bossPos = Vector2.zero;
        bool bossPosFound = false;

        // Try to compute arena top inner area from Arena_Bounds if present
        var arenaGO = GameObject.Find("Arena_Bounds");
        if (arenaGO != null)
        {
            var top = arenaGO.transform.Find("Wall_Top");
            if (top != null)
            {
                var topCol = top.GetComponent<BoxCollider2D>();
                if (topCol != null)
                {
                    float topInner = top.transform.position.y - (topCol.size.y * top.lossyScale.y) * 0.5f;
                    bossPos = new Vector2(0f, topInner - 3f);
                    bossPosFound = true;
                }
            }
        }

        if (!bossPosFound)
        {
            bossPos = new Vector2(0f, 8f);
        }

        // Create BossSpawnPoint if missing
        GameObject bossPoint = GameObject.Find("BossSpawnPoint");
        if (bossPoint == null)
        {
            bossPoint = new GameObject("BossSpawnPoint");
            Undo.RegisterCreatedObjectUndo(bossPoint, "Create BossSpawnPoint");
            bossPoint.transform.position = bossPos;
        }
        else
        {
            Debug.Log("BossSpawnPoint already exists in the scene. Leaving its transform as-is.");
        }

        // Attempt to validate against Environment colliders if EnemySpawner with bossPrefab exists
        EnemySpawner enemySpawner = null;
        var allMb = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var m in allMb)
        {
            if (m == null) continue;
            if (m.GetType().Name == "EnemySpawner") { enemySpawner = m as EnemySpawner; break; }
        }

        float bossRadius = 2.5f;
        int envLayer = LayerMask.NameToLayer("Environment");
        int envMask = (envLayer != -1) ? (1 << envLayer) : 0;

        if (enemySpawner != null)
        {
            // Use SerializedObject to read bossPrefab property
            SerializedObject so = new SerializedObject(enemySpawner);
            var prop = so.FindProperty("bossPrefab");
            if (prop != null && prop.objectReferenceValue != null)
            {
                GameObject bossPrefab = prop.objectReferenceValue as GameObject;
                // Try to compute radius via prefab contents
                string prefabPath = AssetDatabase.GetAssetPath(bossPrefab);
                if (!string.IsNullOrEmpty(prefabPath))
                {
                    GameObject loaded = PrefabUtility.LoadPrefabContents(prefabPath);
                    if (loaded != null)
                    {
                        var col = loaded.GetComponent<Collider2D>() ?? loaded.GetComponentInChildren<Collider2D>();
                        if (col != null)
                        {
                            if (col is BoxCollider2D box)
                            {
                                Vector3 scale = loaded.transform.localScale;
                                bossRadius = Mathf.Max(box.size.x * scale.x, box.size.y * scale.y) * 0.5f;
                            }
                            else if (col is CircleCollider2D circ)
                            {
                                bossRadius = circ.radius * Mathf.Max(loaded.transform.localScale.x, loaded.transform.localScale.y);
                            }
                            else
                            {
                                bossRadius = Mathf.Max(col.bounds.extents.x, col.bounds.extents.y);
                            }
                        }
                        PrefabUtility.UnloadPrefabContents(loaded);
                    }
                }
            }

            // Check overlap and try to nudge horizontally if overlap
            if (envMask != 0)
            {
                Collider2D hit = Physics2D.OverlapCircle(bossPoint.transform.position, bossRadius, envMask);
                if (hit != null)
                {
                    bool placed = false;
                    for (int i = 1; i <= 5; i++)
                    {
                        float offset = i * 2.0f;
                        Vector2 tryPosA = bossPos + new Vector2(offset, 0f);
                        Vector2 tryPosB = bossPos - new Vector2(offset, 0f);
                        if (Physics2D.OverlapCircle(tryPosA, bossRadius, envMask) == null)
                        {
                            bossPoint.transform.position = tryPosA;
                            placed = true; break;
                        }
                        if (Physics2D.OverlapCircle(tryPosB, bossRadius, envMask) == null)
                        {
                            bossPoint.transform.position = tryPosB;
                            placed = true; break;
                        }
                    }

                    if (!placed)
                    {
                        Debug.LogError("BossSpawnPoint overlaps Environment colliders and a nearby clear spot could not be found. Please adjust BossSpawnPoint manually.");
                    }
                    else
                    {
                        Debug.Log("BossSpawnPoint nudged to a nearby clear spot.");
                    }
                }
            }
        }

        // Assign bossSpawnPoint to EnemySpawner if present
        if (enemySpawner != null)
        {
            SerializedObject so = new SerializedObject(enemySpawner);
            var prop = so.FindProperty("bossSpawnPoint");
            if (prop != null)
            {
                prop.objectReferenceValue = bossPoint.transform;
                so.ApplyModifiedProperties();
                Debug.Log("Assigned BossSpawnPoint to EnemySpawner.bossSpawnPoint");
            }
        }

        // Mark scene dirty and save
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("PlayerStart and BossSpawnPoint created/validated in Main scene.");
    }
}
