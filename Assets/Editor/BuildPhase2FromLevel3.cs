using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Rebuilds Level2_Phase2.unity by duplicating the real, working GameObjects
// (Player, HUD, SceneLoader, RestartController, Main Camera, AudioHub, VFXHub) from Level2.unity.
// This avoids hand-authored/guessed GUIDs, fileIDs, and serialized values:
// every component and field is copied by Unity itself via GameObject.Instantiate.
public static class BuildPhase2FromLevel2
{
    private const string Level2Path = "Assets/Scenes/Level2/Level2.unity";
    private const string Phase2Path = "Assets/Scenes/Level2/Level2_Phase2.unity";
    private static readonly string[] RootNamesToCopy = { "Player", "HUD", "SceneLoader", "RestartController", "Main Camera", "AudioHub", "VFXHub" };

    [MenuItem("Prototype/Roguelite/Rebuild Level2 Phase2 From Level2")]
    public static void Rebuild()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("BuildPhase2FromLevel2: Exit Play Mode before running this.");
            return;
        }

        Scene level2 = EditorSceneManager.GetSceneByPath(Level2Path);
        bool weOpenedLevel2 = false;
        if (!level2.IsValid() || !level2.isLoaded)
        {
            level2 = EditorSceneManager.OpenScene(Level2Path, OpenSceneMode.Additive);
            weOpenedLevel2 = true;
        }

        GameObject[] sources = new GameObject[RootNamesToCopy.Length];
        for (int i = 0; i < RootNamesToCopy.Length; i++)
        {
            sources[i] = FindRoot(level2, RootNamesToCopy[i]);
            if (sources[i] == null)
                Debug.LogWarning($"BuildPhase2FromLevel2: Could not find root GameObject '{RootNamesToCopy[i]}' in Level2.");
        }

        if (sources[0] == null || sources[1] == null || sources[2] == null)
        {
            Debug.LogError("BuildPhase2FromLevel2: Required objects (Player/HUD/SceneLoader) missing in Level2. Aborting.");
            if (weOpenedLevel2) EditorSceneManager.CloseScene(level2, true);
            return;
        }

        Scene phase2 = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

        GameObject sceneLoaderCopy = null;
        GameObject restartControllerCopy = null;

        foreach (GameObject source in sources)
        {
            if (source == null) continue;
            GameObject copy = Object.Instantiate(source);
            copy.name = source.name;
            SceneManager.MoveGameObjectToScene(copy, phase2);

            if (source.name == "SceneLoader") sceneLoaderCopy = copy;
            if (source.name == "RestartController") restartControllerCopy = copy;
        }

        // Re-wire RestartController -> SceneLoader reference to point at the copies in Phase2
        // (Instantiate duplicates cross-references within the duplicated set automatically only if
        // objects are instantiated together; since we instantiate one at a time, fix explicitly.)
        if (restartControllerCopy != null && sceneLoaderCopy != null)
        {
            var restartController = restartControllerCopy.GetComponent<RestartController>();
            if (restartController != null)
            {
                var so = new SerializedObject(restartController);
                so.FindProperty("sceneLoader").objectReferenceValue = sceneLoaderCopy.GetComponent<SceneLoader>();
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        EditorSceneManager.MarkSceneDirty(phase2);
        EditorSceneManager.SaveScene(phase2, Phase2Path);

        if (weOpenedLevel2) EditorSceneManager.CloseScene(level2, true);
        EditorSceneManager.CloseScene(phase2, true);

        Debug.Log("BuildPhase2FromLevel2: Level2_Phase2.unity rebuilt from Level2's actual GameObjects.");
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
