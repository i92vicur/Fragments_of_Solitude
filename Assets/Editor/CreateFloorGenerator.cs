using UnityEditor;
using UnityEngine;

public static class CreateFloorGenerator
{
    [MenuItem("Tools/Prototype/Create Floor Generator")]
    public static void Create()
    {
        GameObject arenaBounds = GameObject.Find("Arena_Bounds");
        if (arenaBounds == null)
        {
            EditorUtility.DisplayDialog(
                "Arena_Bounds not found",
                "I couldn’t find an Arena_Bounds object in the current scene. Create the arena boundaries first, then run this tool again.",
                "OK");
            return;
        }

        GameObject floorRoot = GameObject.Find("FloorGenerator");
        if (floorRoot == null)
        {
            floorRoot = new GameObject("FloorGenerator");
            Undo.RegisterCreatedObjectUndo(floorRoot, "Create Floor Generator");
        }

        GeneratedFloor generatedFloor = floorRoot.GetComponent<GeneratedFloor>();
        if (generatedFloor == null)
        {
            generatedFloor = Undo.AddComponent<GeneratedFloor>(floorRoot);
        }

        SerializedObject so = new SerializedObject(generatedFloor);
        SerializedProperty mapBounds = so.FindProperty("mapBounds");
        mapBounds.objectReferenceValue = arenaBounds.GetComponent<Collider2D>();
        if (mapBounds.objectReferenceValue == null)
        {
            var colliders = arenaBounds.GetComponentsInChildren<Collider2D>();
            if (colliders != null && colliders.Length > 0)
                mapBounds.objectReferenceValue = colliders[0];
        }
        so.ApplyModifiedProperties();

        Selection.activeGameObject = floorRoot;
        EditorUtility.DisplayDialog(
            "Floor Generator Ready",
            "The FloorGenerator object was created and linked to Arena_Bounds. Now just assign the floor sprites in the Inspector and click Generate Floor.",
            "OK");
    }
}
