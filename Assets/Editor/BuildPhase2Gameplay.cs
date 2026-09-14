using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Adds the existing EnemySpawner and ChestSpawner systems to Level2_Phase2, using the
// real Level2 configuration as a starting point (enemy prefabs, chest prefab, spawn
// tuning values copied - no invented references). All source values are extracted into
// plain C# locals BEFORE closing the temporary Level2 scene, since SerializedProperty
// references become invalid once their scene is closed. Boss configuration is copied
// from the real Level2 spawner so Phase2 uses the existing boss infrastructure. Arena bounds are auto-detected by the existing components
// themselves via the "Arena_Bounds" GameObject already present in Phase2.
public static class BuildPhase2Gameplay
{
    private const string Level2Path = "Assets/Scenes/Level2/Level2.unity";
    private const string Phase2Path = "Assets/Scenes/Level2/Level2_Phase2.unity";

    private struct EnemyEntryData
    {
        public string label;
        public Object prefab;
        public int weight;
    }

    [MenuItem("Prototype/Roguelite/Build Phase2 Gameplay (Enemies + Chests)")]
    public static void Build()
    {
        Scene active = EditorSceneManager.GetActiveScene();
        if (active.path != Phase2Path)
        {
            Debug.LogError("BuildPhase2Gameplay: Please open Level2_Phase2.unity as the active scene before running this.");
            return;
        }

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogError("BuildPhase2Gameplay: No GameObject tagged 'Player' found in the active scene.");
            return;
        }

        Scene level2 = EditorSceneManager.GetSceneByPath(Level2Path);
        bool weOpenedLevel2 = false;
        if (!level2.IsValid() || !level2.isLoaded)
        {
            level2 = EditorSceneManager.OpenScene(Level2Path, OpenSceneMode.Additive);
            weOpenedLevel2 = true;
        }

        GameObject sourceEnemySpawnerGO = FindRoot(level2, "EnemySpawner");
        GameObject sourceChestSpawnerGO = FindRoot(level2, "ChestSpawner");
        GameObject sourceAudioHubGO = FindRoot(level2, "AudioHub");
        GameObject sourceVFXHubGO = FindRoot(level2, "VFXHub");

        if (sourceEnemySpawnerGO == null || sourceChestSpawnerGO == null)
        {
            Debug.LogError("BuildPhase2Gameplay: Could not find 'EnemySpawner' and/or 'ChestSpawner' in Level2.");
            if (weOpenedLevel2) EditorSceneManager.CloseScene(level2, true);
            return;
        }

        SerializedObject sourceEnemySO = new SerializedObject(sourceEnemySpawnerGO.GetComponent<EnemySpawner>());
        SerializedObject sourceChestSO = new SerializedObject(sourceChestSpawnerGO.GetComponent<ChestSpawner>());

        // --- Extract EnemySpawner tuning into plain values (skip playerTransform, arena bounds, boss fields) ---
        SerializedProperty srcEnemyTypesProp = sourceEnemySO.FindProperty("enemyTypes");
        EnemyEntryData[] enemyEntries = new EnemyEntryData[srcEnemyTypesProp.arraySize];
        for (int i = 0; i < enemyEntries.Length; i++)
        {
            SerializedProperty entry = srcEnemyTypesProp.GetArrayElementAtIndex(i);
            enemyEntries[i] = new EnemyEntryData
            {
                label = entry.FindPropertyRelative("label").stringValue,
                prefab = entry.FindPropertyRelative("prefab").objectReferenceValue,
                weight = entry.FindPropertyRelative("weight").intValue
            };
        }

        float initialDelay = sourceEnemySO.FindProperty("initialDelay").floatValue;
        float spawnInterval = sourceEnemySO.FindProperty("spawnInterval").floatValue;
        float minSpawnInterval = sourceEnemySO.FindProperty("minSpawnInterval").floatValue;
        float minDistance = sourceEnemySO.FindProperty("minDistance").floatValue;
        float maxDistance = sourceEnemySO.FindProperty("maxDistance").floatValue;
        int maxLiving = sourceEnemySO.FindProperty("maxLiving").intValue;
        bool enableProgression = sourceEnemySO.FindProperty("enableProgression").boolValue;
        float progressionDuration = sourceEnemySO.FindProperty("progressionDuration").floatValue;
        int targetMaxLiving = sourceEnemySO.FindProperty("targetMaxLiving").intValue;
        bool drawGizmos = sourceEnemySO.FindProperty("drawGizmos").boolValue;
        int maxSpawnAttempts = sourceEnemySO.FindProperty("maxSpawnAttempts").intValue;
        int enemyObstacleLayerMask = sourceEnemySO.FindProperty("obstacleLayerMask.m_Bits").intValue;
        Object bossPrefab = sourceEnemySO.FindProperty("bossPrefab").objectReferenceValue;
        int bossLevelThreshold = sourceEnemySO.FindProperty("bossLevelThreshold").intValue;
        bool pauseSpawningDuringBoss = sourceEnemySO.FindProperty("pauseSpawningDuringBoss").boolValue;

        Vector3 bossSpawnPosition = Vector3.zero;
        Quaternion bossSpawnRotation = Quaternion.identity;
        GameObject sourceBossSpawnPoint = FindRoot(level2, "BossSpawnPoint");
        if (sourceBossSpawnPoint != null)
        {
            bossSpawnPosition = sourceBossSpawnPoint.transform.position;
            bossSpawnRotation = sourceBossSpawnPoint.transform.rotation;
        }

        // --- Extract ChestSpawner tuning into plain values (skip arena bounds and boss/player-start refs) ---
        Object chestPrefab = sourceChestSO.FindProperty("chestPrefab").objectReferenceValue;
        int chestCount = sourceChestSO.FindProperty("chestCount").intValue;
        int maxTotalAttempts = sourceChestSO.FindProperty("maxTotalAttempts").intValue;
        int chestObstacleLayerMask = sourceChestSO.FindProperty("obstacleLayerMask.m_Bits").intValue;
        float borderMargin = sourceChestSO.FindProperty("borderMargin").floatValue;
        float minDistanceFromPlayerStart = sourceChestSO.FindProperty("minDistanceFromPlayerStart").floatValue;
        float minDistanceFromBossSpawn = sourceChestSO.FindProperty("minDistanceFromBossSpawn").floatValue;
        float minDistanceBetweenChests = sourceChestSO.FindProperty("minDistanceBetweenChests").floatValue;

        if (weOpenedLevel2) EditorSceneManager.CloseScene(level2, true);

        // --- Create/find EnemySpawner in Phase2 ---
        GameObject enemySpawnerGO = GameObject.Find("EnemySpawner");
        if (enemySpawnerGO == null)
            enemySpawnerGO = new GameObject("EnemySpawner");
        EnemySpawner enemySpawner = enemySpawnerGO.GetComponent<EnemySpawner>();
        if (enemySpawner == null)
            enemySpawner = enemySpawnerGO.AddComponent<EnemySpawner>();

        SerializedObject enemySO = new SerializedObject(enemySpawner);
        SerializedProperty dstEnemyTypesProp = enemySO.FindProperty("enemyTypes");
        dstEnemyTypesProp.arraySize = enemyEntries.Length;
        for (int i = 0; i < enemyEntries.Length; i++)
        {
            SerializedProperty entry = dstEnemyTypesProp.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("label").stringValue = enemyEntries[i].label;
            entry.FindPropertyRelative("prefab").objectReferenceValue = enemyEntries[i].prefab;
            entry.FindPropertyRelative("weight").intValue = enemyEntries[i].weight;
        }
        enemySO.FindProperty("playerTransform").objectReferenceValue = player.transform;
        enemySO.FindProperty("initialDelay").floatValue = initialDelay;
        enemySO.FindProperty("spawnInterval").floatValue = spawnInterval;
        enemySO.FindProperty("minSpawnInterval").floatValue = minSpawnInterval;
        enemySO.FindProperty("minDistance").floatValue = minDistance;
        enemySO.FindProperty("maxDistance").floatValue = maxDistance;
        enemySO.FindProperty("maxLiving").intValue = maxLiving;
        enemySO.FindProperty("enableProgression").boolValue = enableProgression;
        enemySO.FindProperty("progressionDuration").floatValue = progressionDuration;
        enemySO.FindProperty("targetMaxLiving").intValue = targetMaxLiving;
        enemySO.FindProperty("drawGizmos").boolValue = drawGizmos;
        enemySO.FindProperty("maxSpawnAttempts").intValue = maxSpawnAttempts;
        enemySO.FindProperty("obstacleLayerMask.m_Bits").intValue = enemyObstacleLayerMask;
        enemySO.FindProperty("autoDetectArenaBounds").boolValue = true;

        // Reuse the existing boss prefab and threshold, while creating a Phase2-local
        // spawn point so the scene does not retain a reference to the additive source scene.
        GameObject bossSpawnPoint = GameObject.Find("BossSpawnPoint");
        if (bossSpawnPoint == null)
            bossSpawnPoint = new GameObject("BossSpawnPoint");
        bossSpawnPoint.transform.position = bossSpawnPosition;
        bossSpawnPoint.transform.rotation = bossSpawnRotation;
        enemySO.FindProperty("bossPrefab").objectReferenceValue = bossPrefab;
        enemySO.FindProperty("bossSpawnPoint").objectReferenceValue = bossSpawnPoint.transform;
        enemySO.FindProperty("bossLevelThreshold").intValue = bossLevelThreshold;
        enemySO.FindProperty("pauseSpawningDuringBoss").boolValue = pauseSpawningDuringBoss;
        enemySO.ApplyModifiedPropertiesWithoutUndo();

        // --- Create/find ChestSpawner in Phase2 ---
        GameObject chestSpawnerGO = GameObject.Find("ChestSpawner");
        if (chestSpawnerGO == null)
            chestSpawnerGO = new GameObject("ChestSpawner");
        ChestSpawner chestSpawner = chestSpawnerGO.GetComponent<ChestSpawner>();
        if (chestSpawner == null)
            chestSpawner = chestSpawnerGO.AddComponent<ChestSpawner>();

        SerializedObject chestSO = new SerializedObject(chestSpawner);
        chestSO.FindProperty("chestPrefab").objectReferenceValue = chestPrefab;
        chestSO.FindProperty("chestCount").intValue = chestCount;
        chestSO.FindProperty("maxTotalAttempts").intValue = maxTotalAttempts;
        chestSO.FindProperty("autoDetectArenaBounds").boolValue = true;
        chestSO.FindProperty("obstacleLayerMask.m_Bits").intValue = chestObstacleLayerMask;
        chestSO.FindProperty("borderMargin").floatValue = borderMargin;
        chestSO.FindProperty("minDistanceFromPlayerStart").floatValue = minDistanceFromPlayerStart;
        chestSO.FindProperty("minDistanceFromBossSpawn").floatValue = minDistanceFromBossSpawn;
        chestSO.FindProperty("minDistanceBetweenChests").floatValue = minDistanceBetweenChests;
        // ChestSpawner can now avoid the Phase2 boss spawn area via the local point.
        chestSO.ApplyModifiedPropertiesWithoutUndo();

        // --- Reuse the real Phase 1 feedback hubs in Phase2 ---
        if (sourceAudioHubGO != null)
        {
            GameObject audioHubGO = GameObject.Find("AudioHub");
            if (audioHubGO == null)
            {
                audioHubGO = Object.Instantiate(sourceAudioHubGO);
                audioHubGO.name = sourceAudioHubGO.name;
                SceneManager.MoveGameObjectToScene(audioHubGO, active);
            }
            else
            {
                var sourceAudioHub = sourceAudioHubGO.GetComponent<AudioHub>();
                var targetAudioHub = audioHubGO.GetComponent<AudioHub>() ?? audioHubGO.AddComponent<AudioHub>();
                EditorUtility.CopySerialized(sourceAudioHub, targetAudioHub);
            }
        }

        if (sourceVFXHubGO != null)
        {
            GameObject vfxHubGO = GameObject.Find("VFXHub");
            if (vfxHubGO == null)
            {
                vfxHubGO = Object.Instantiate(sourceVFXHubGO);
                vfxHubGO.name = sourceVFXHubGO.name;
                SceneManager.MoveGameObjectToScene(vfxHubGO, active);
            }
            else
            {
                var sourceVFXHub = sourceVFXHubGO.GetComponent<VFXHub>();
                var targetVFXHub = vfxHubGO.GetComponent<VFXHub>() ?? vfxHubGO.AddComponent<VFXHub>();
                EditorUtility.CopySerialized(sourceVFXHub, targetVFXHub);
            }
        }

        EditorSceneManager.MarkSceneDirty(active);
        EditorSceneManager.SaveScene(active);

        Debug.Log("BuildPhase2Gameplay: EnemySpawner and ChestSpawner configured in Level2_Phase2.");
    }

    private static GameObject FindRoot(Scene scene, string objectName)
    {
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            if (go.name == objectName) return go;
        }
        return null;
    }
}
