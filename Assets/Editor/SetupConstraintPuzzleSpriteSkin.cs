using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SetupConstraintPuzzleSpriteSkin
{
    private const string ScenePath = "Assets/Scenes/Level1/ConstraintPuzzle_New.unity";

    [MenuItem("Prototype/Constraint Puzzle/Install Sprite Skin")]
    public static void InstallSpriteSkin()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.isLoaded)
        {
            Debug.LogError("SetupConstraintPuzzleSpriteSkin: Failed to open ConstraintPuzzle_New scene.");
            return;
        }

        GameObject puzzleRoot = GameObject.Find("Canvas/ConstraintPuzzleCanvas/PuzzleRoot");
        if (puzzleRoot == null)
        {
            Debug.LogError("SetupConstraintPuzzleSpriteSkin: PuzzleRoot not found.");
            return;
        }

        ConstraintPuzzleSpriteSkin skin = puzzleRoot.GetComponent<ConstraintPuzzleSpriteSkin>();
        if (skin == null)
            skin = puzzleRoot.AddComponent<ConstraintPuzzleSpriteSkin>();

        skin.RefreshVisuals();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("SetupConstraintPuzzleSpriteSkin: Sprite skin installed on PuzzleRoot. Assign the 6 slot sprites and 6 letter sprites in the inspector.");
    }
}