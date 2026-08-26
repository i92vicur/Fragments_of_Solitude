using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Rebuilds Level3_Phase2.unity by duplicating the real, working GameObjects
// (Player, HUD, SceneLoader, RestartController, Main Camera, AudioHub, VFXHub) from Level3.unity.
// This avoids hand-authored/guessed GUIDs, fileIDs, and serialized values:
// every component and field is copied by Unity itself via GameObject.Instantiate.
public static class BuildPhase2FromLevel3
{
    private const string Level3Path = "Assets/Scenes/Level3/Level3.unity";
    private const string Phase2Path = "Assets/Scenes/Level3/Level3_Phase2.unity";
    private static readonly string[] RootNamesToCopy = { "Player", "HUD", "SceneLoader", "RestartController", "Main Camera", "AudioHub", "VFXHub" };

    [MenuItem("Prototype/Roguelite/Rebuild Level3 Phase2 From Level3")]
    public static void Rebuild()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("BuildPhase2FromLevel3: Exit Play Mode before running this.");
            return;
        }

        Scene level3 = EditorSceneManager.GetSceneByPath(Level3Path);
        bool weOpenedLevel3 = false;
        if (!level3.IsValid() || !level3.isLoaded)
        {
            level3 = EditorSceneManager.OpenScene(Level3Path, OpenSceneMode.Additive);
            weOpenedLevel3 = true;
        }

        GameObject[] sources = new GameObject[RootNamesToCopy.Length];
        for (int i = 0; i < RootNamesToCopy.Length; i++)
        {
            sources[i] = FindRoot(level3, RootNamesToCopy[i]);
            if (sources[i] == null)
                Debug.LogWarning($"BuildPhase2FromLevel3: Could not find root GameObject '{RootNamesToCopy[i]}' in Level3.");
        }

        if (sources[0] == null || sources[1] == null || sources[2] == null)
        {
            Debug.LogError("BuildPhase2FromLevel3: Required objects (Player/HUD/SceneLoader) missing in Level3. Aborting.");
            if (weOpenedLevel3) EditorSceneManager.CloseScene(level3, true);
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

        if (weOpenedLevel3) EditorSceneManager.CloseScene(level3, true);
        EditorSceneManager.CloseScene(phase2, true);

        Debug.Log("BuildPhase2FromLevel3: Level3_Phase2.unity rebuilt from Level3's actual GameObjects.");
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
