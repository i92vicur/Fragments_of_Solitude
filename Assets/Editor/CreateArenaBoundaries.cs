using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CreateArenaBoundaries
{
    [MenuItem("Tools/Prototype/Create Arena Boundaries")]
    public static void CreateBounds()
    {
        if (!EditorUtility.DisplayDialog("Create Arena Boundaries",
            "This will create four invisible BoxCollider2D GameObjects around the prototype arena and parent them under 'Arena_Bounds'. Existing Arena_Bounds will be replaced. Proceed?",
            "Create", "Cancel"))
            return;

        string mainScenePath = "Assets/Scenes/Main.unity";
        if (File.Exists(mainScenePath))
        {
            EditorSceneManager.OpenScene(mainScenePath, OpenSceneMode.Single);
            Debug.Log("Opened scene: " + mainScenePath);
        }
        else
        {
            Debug.Log("Main.unity not found; using currently active scene.");
        }

        GameObject existing = GameObject.Find("Arena_Bounds");
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("Replace existing Arena_Bounds?","An 'Arena_Bounds' GameObject already exists. Replace it?","Replace","Cancel"))
                return;
            Undo.DestroyObjectImmediate(existing);
        }

        // Arena extents (match obstacle placement script): X [-20,20], Y [-14,14]
        float xMin = -20f, xMax = 20f, yMin = -14f, yMax = 14f;
        float thickness = 1f; // collider thickness in world units

        GameObject parent = new GameObject("Arena_Bounds");
        Undo.RegisterCreatedObjectUndo(parent, "Create Arena_Bounds");

        int envLayer = LayerMask.NameToLayer("Environment");

        // Create helper to add collider-only object
        System.Action<string, Vector2, Vector2> createWall = (name, center, size) =>
        {
            GameObject wall = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(wall, "Create Arena Wall");
            wall.transform.position = new Vector3(center.x, center.y, 0f);
            var col = wall.AddComponent<BoxCollider2D>();
            col.size = size;
            col.offset = Vector2.zero;
            col.isTrigger = false;
            if (envLayer != -1) wall.layer = envLayer;
            wall.transform.SetParent(parent.transform, true);
        };

        // Top wall: spans X from xMin..xMax, positioned at yMax + thickness/2
        Vector2 topCenter = new Vector2((xMin + xMax) * 0.5f, yMax + thickness * 0.5f);
        Vector2 topSize = new Vector2((xMax - xMin) + thickness * 2f, thickness);
        createWall("Wall_Top", topCenter, topSize);

        // Bottom wall
        Vector2 bottomCenter = new Vector2((xMin + xMax) * 0.5f, yMin - thickness * 0.5f);
        Vector2 bottomSize = new Vector2((xMax - xMin) + thickness * 2f, thickness);
        createWall("Wall_Bottom", bottomCenter, bottomSize);

        // Left wall
        Vector2 leftCenter = new Vector2(xMin - thickness * 0.5f, (yMin + yMax) * 0.5f);
        Vector2 leftSize = new Vector2(thickness, (yMax - yMin) + thickness * 2f);
        createWall("Wall_Left", leftCenter, leftSize);

        // Right wall
        Vector2 rightCenter = new Vector2(xMax + thickness * 0.5f, (yMin + yMax) * 0.5f);
        Vector2 rightSize = new Vector2(thickness, (yMax - yMin) + thickness * 2f);
        createWall("Wall_Right", rightCenter, rightSize);

        // Mark scene dirty and save
        Selection.activeGameObject = parent;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        EditorUtility.DisplayDialog("Arena Boundaries Created", "Created Arena_Bounds with four invisible colliders. Verify in the Scene view.", "OK");
    }
}