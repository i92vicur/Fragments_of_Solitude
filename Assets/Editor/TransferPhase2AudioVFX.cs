using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TransferPhase2AudioVFX
{
    private const string Level2Path = "Assets/Scenes/Level2/Level2.unity";
    private const string Phase2Path = "Assets/Scenes/Level2/Level2_Phase2.unity";

    [MenuItem("Prototype/Roguelite/Transfer AudioHub & VFXHub to Phase 2")]
    public static void Transfer()
    {
        Debug.Log("TransferPhase2AudioVFX: starting transfer");

        if (EditorApplication.isPlaying)
        {
            Debug.LogError("TransferPhase2AudioVFX: Exit Play Mode before running this.");
            EditorApplication.Exit(0);
            return;
        }

        Scene phase2Scene = EditorSceneManager.GetSceneByPath(Phase2Path);
        if (!phase2Scene.IsValid() || !phase2Scene.isLoaded)
        {
            phase2Scene = EditorSceneManager.OpenScene(Phase2Path, OpenSceneMode.Single);
        }

        Scene level2Scene = EditorSceneManager.GetSceneByPath(Level2Path);
        bool openedLevel2 = false;
        if (!level2Scene.IsValid() || !level2Scene.isLoaded)
        {
            level2Scene = EditorSceneManager.OpenScene(Level2Path, OpenSceneMode.Additive);
            openedLevel2 = true;
        }

        GameObject sourceAudio = FindRoot(level2Scene, "AudioHub");
        GameObject sourceVfx = FindRoot(level2Scene, "VFXHub");

        if (sourceAudio == null || sourceVfx == null)
        {
            Debug.LogError("TransferPhase2AudioVFX: AudioHub or VFXHub is missing from Level2.unity.");
            if (openedLevel2) EditorSceneManager.CloseScene(level2Scene, true);
            EditorApplication.Exit(0);
            return;
        }

        DuplicateHubIntoScene(sourceAudio, phase2Scene, "AudioHub");
        DuplicateHubIntoScene(sourceVfx, phase2Scene, "VFXHub");

        EditorSceneManager.MarkSceneDirty(phase2Scene);
        bool saved = EditorSceneManager.SaveScene(phase2Scene);
        Debug.Log("TransferPhase2AudioVFX: scene save result = " + saved);

        if (openedLevel2)
        {
            EditorSceneManager.CloseScene(level2Scene, true);
        }

        Debug.Log("TransferPhase2AudioVFX: Copied the real Level 2 AudioHub and VFXHub into Level2_Phase2.unity.");
        EditorApplication.Exit(0);
    }

    private static void DuplicateHubIntoScene(GameObject source, Scene targetScene, string objectName)
    {
        GameObject existing = FindRoot(targetScene, objectName);
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        GameObject copy = Object.Instantiate(source);
        copy.name = objectName;
        copy.transform.SetParent(null, false);
        SceneManager.MoveGameObjectToScene(copy, targetScene);

        // Keep the exact serialized values from the source object, including project asset references.
        // This prevents scene-local references from being carried over.
        if (source.GetComponent<AudioHub>() != null)
        {
            AudioHub sourceHub = source.GetComponent<AudioHub>();
            AudioHub targetHub = copy.GetComponent<AudioHub>();
            if (targetHub == null)
            {
                targetHub = copy.AddComponent<AudioHub>();
            }
            EditorUtility.CopySerialized(sourceHub, targetHub);
        }
        else if (source.GetComponent<VFXHub>() != null)
        {
            VFXHub sourceHub = source.GetComponent<VFXHub>();
            VFXHub targetHub = copy.GetComponent<VFXHub>();
            if (targetHub == null)
            {
                targetHub = copy.AddComponent<VFXHub>();
            }
            EditorUtility.CopySerialized(sourceHub, targetHub);
        }
    }

    private static GameObject FindRoot(Scene scene, string objectName)
    {
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            if (go.name == objectName)
            {
                return go;
            }
        }

        return null;
    }
}
