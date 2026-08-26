using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Builds the Cycle 2 rectangular arena for Level3_Phase2.unity:
// Arena_Bounds (4 real BoxCollider2D walls), MapStructures (empty root required by
// GeneratedFloor), FloorGenerator (rectangular mode, sprite arrays copied from the
// real Level3 FloorGenerator), and ArenaBorder. Also repositions the existing Player
// and re-wires the camera follow target. No invented GUIDs/fileIDs: all sprite and
// script references are copied live from Level3's actual FloorGenerator component.
public static class BuildPhase2Arena
{
    private const string Level3Path = "Assets/Scenes/Level3/Level3.unity";
    private const string Phase2Path = "Assets/Scenes/Level3/Level3_Phase2.unity";

    // Half-extents of the new rectangular arena (world units).
    private const float HalfWidth = 35f;
    private const float HalfHeight = 25f;
    private const float WallThickness = 1f;

    [MenuItem("Prototype/Roguelite/Build Phase2 Rectangular Arena")]
    public static void Build()
    {
        Scene active = EditorSceneManager.GetActiveScene();
        if (active.path != Phase2Path)
        {
            Debug.LogError("BuildPhase2Arena: Please open Level3_Phase2.unity as the active scene before running this.");
            return;
        }

        // --- Pull real sprite/config references from Level3's FloorGenerator ---
        Scene level3 = EditorSceneManager.GetSceneByPath(Level3Path);
        bool weOpenedLevel3 = false;
        if (!level3.IsValid() || !level3.isLoaded)
        {
            level3 = EditorSceneManager.OpenScene(Level3Path, OpenSceneMode.Additive);
            weOpenedLevel3 = true;
        }

        GameObject sourceFloorGeneratorGO = FindRoot(level3, "FloorGenerator");
        if (sourceFloorGeneratorGO == null)
        {
            Debug.LogError("BuildPhase2Arena: Could not find 'FloorGenerator' in Level3.");
            if (weOpenedLevel3) EditorSceneManager.CloseScene(level3, true);
            return;
        }

        GeneratedFloor sourceFloor = sourceFloorGeneratorGO.GetComponent<GeneratedFloor>();
        SerializedObject sourceFloorSO = new SerializedObject(sourceFloor);

        SerializedProperty srcFloorTiles = sourceFloorSO.FindProperty("floorTiles").Copy();
        SerializedProperty srcUpperWallTiles = sourceFloorSO.FindProperty("upperWallTiles").Copy();
        SerializedProperty srcRoofTiles = sourceFloorSO.FindProperty("roofTiles").Copy();
        bool generateUpperWall = sourceFloorSO.FindProperty("generateUpperWall").boolValue;
        bool generateRoof = sourceFloorSO.FindProperty("generateRoof").boolValue;
        int upperWallSortingOrder = sourceFloorSO.FindProperty("upperWallSortingOrder").intValue;
        string upperWallSortingLayerName = sourceFloorSO.FindProperty("upperWallSortingLayerName").stringValue;
        float topEdgeOffset = sourceFloorSO.FindProperty("topEdgeOffset").floatValue;
        float roofGap = sourceFloorSO.FindProperty("roofGap").floatValue;
        float floorVerticalOffset = sourceFloorSO.FindProperty("floorVerticalOffset").floatValue;
        int sortingOrder = sourceFloorSO.FindProperty("sortingOrder").intValue;
        string sortingLayerName = sourceFloorSO.FindProperty("sortingLayerName").stringValue;

        if (weOpenedLevel3) EditorSceneManager.CloseScene(level3, true);

        // --- Arena_Bounds with 4 real wall colliders (enabled, since rectangle mode has no polygon collider) ---
        GameObject arenaBounds = GameObject.Find("Arena_Bounds");
        if (arenaBounds == null)
            arenaBounds = new GameObject("Arena_Bounds");

        int environmentLayer = LayerMask.NameToLayer("Environment");

        BoxCollider2D topCollider = CreateWall(arenaBounds.transform, "Wall_Top",
            new Vector2(0f, HalfHeight + WallThickness * 0.5f), new Vector2(HalfWidth * 2f + WallThickness * 2f, WallThickness), environmentLayer);
        CreateWall(arenaBounds.transform, "Wall_Bottom",
            new Vector2(0f, -HalfHeight - WallThickness * 0.5f), new Vector2(HalfWidth * 2f + WallThickness * 2f, WallThickness), environmentLayer);
        CreateWall(arenaBounds.transform, "Wall_Left",
            new Vector2(-HalfWidth - WallThickness * 0.5f, 0f), new Vector2(WallThickness, HalfHeight * 2f + WallThickness * 2f), environmentLayer);
        CreateWall(arenaBounds.transform, "Wall_Right",
            new Vector2(HalfWidth + WallThickness * 0.5f, 0f), new Vector2(WallThickness, HalfHeight * 2f + WallThickness * 2f), environmentLayer);

        // --- MapStructures (empty root; GeneratedFloor requires this reference even in rectangle mode) ---
        GameObject mapStructures = GameObject.Find("MapStructures");
        if (mapStructures == null)
            mapStructures = new GameObject("MapStructures");

        // --- ArenaBorder (visual only, references Arena_Bounds) ---
        GameObject arenaBorderGO = GameObject.Find("ArenaBorder");
        if (arenaBorderGO == null)
            arenaBorderGO = new GameObject("ArenaBorder");
        ArenaBorder arenaBorder = arenaBorderGO.GetComponent<ArenaBorder>();
        if (arenaBorder == null)
            arenaBorder = arenaBorderGO.AddComponent<ArenaBorder>();

        SerializedObject borderSO = new SerializedObject(arenaBorder);
        borderSO.FindProperty("arenaBoundsRoot").objectReferenceValue = arenaBounds.transform;
        borderSO.FindProperty("borderColor").colorValue = new Color(1f, 0.2f, 0.2f, 0.9f);
        borderSO.FindProperty("thickness").floatValue = 0.08f;
        borderSO.FindProperty("drawInGame").boolValue = true;
        borderSO.FindProperty("fallbackCenter").vector2Value = Vector2.zero;
        borderSO.FindProperty("fallbackSize").vector2Value = new Vector2(HalfWidth * 2f, HalfHeight * 2f);
        borderSO.ApplyModifiedPropertiesWithoutUndo();

        // --- FloorGenerator (rectangular mode) ---
        GameObject floorGenGO = GameObject.Find("FloorGenerator");
        if (floorGenGO == null)
            floorGenGO = new GameObject("FloorGenerator");
        GeneratedFloor floorGen = floorGenGO.GetComponent<GeneratedFloor>();
        if (floorGen == null)
            floorGen = floorGenGO.AddComponent<GeneratedFloor>();

        SerializedObject floorSO = new SerializedObject(floorGen);
        CopyArray(srcFloorTiles, floorSO.FindProperty("floorTiles"));
        CopyArray(srcUpperWallTiles, floorSO.FindProperty("upperWallTiles"));
        CopyArray(srcRoofTiles, floorSO.FindProperty("roofTiles"));
        floorSO.FindProperty("generateUpperWall").boolValue = generateUpperWall;
        floorSO.FindProperty("generateRoof").boolValue = generateRoof;
        floorSO.FindProperty("upperWallSortingOrder").intValue = upperWallSortingOrder;
        floorSO.FindProperty("upperWallSortingLayerName").stringValue = upperWallSortingLayerName;
        floorSO.FindProperty("floorVerticalOffset").floatValue = floorVerticalOffset;
        floorSO.FindProperty("topEdgeOffset").floatValue = topEdgeOffset;
        floorSO.FindProperty("roofGap").floatValue = roofGap;
        floorSO.FindProperty("mapBounds").objectReferenceValue = topCollider;
        floorSO.FindProperty("arenaBorder").objectReferenceValue = arenaBorder;
        floorSO.FindProperty("useCompleteMapGeometry").boolValue = false;
        floorSO.FindProperty("mapStructuresRoot").objectReferenceValue = mapStructures.transform;
        floorSO.FindProperty("randomSeed").intValue = 0;
        floorSO.FindProperty("sortingOrder").intValue = sortingOrder;
        floorSO.FindProperty("sortingLayerName").stringValue = sortingLayerName;
        floorSO.FindProperty("autoGenerateOnStart").boolValue = true;
        floorSO.ApplyModifiedPropertiesWithoutUndo();

        // --- Reposition the existing Player inside the new arena ---
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            player.transform.position = new Vector3(0f, 0f, player.transform.position.z);
        else
            Debug.LogWarning("BuildPhase2Arena: No GameObject tagged 'Player' found in the active scene.");

        // --- Re-wire camera follow target (if a follow component is present) ---
        Camera mainCam = Camera.main;
        if (mainCam != null && player != null)
        {
            foreach (var behaviour in mainCam.GetComponents<MonoBehaviour>())
            {
                if (behaviour == null) continue;
                SerializedObject camSO = new SerializedObject(behaviour);
                SerializedProperty targetProp = camSO.FindProperty("target");
                if (targetProp != null && targetProp.propertyType == SerializedPropertyType.ObjectReference)
                {
                    targetProp.objectReferenceValue = player.transform;
                    camSO.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        // Generate the floor immediately so it is visible without entering Play Mode.
        floorGen.GenerateFloor();

        EditorSceneManager.MarkSceneDirty(active);
        EditorSceneManager.SaveScene(active);

        Debug.Log("BuildPhase2Arena: Rectangular arena built and scene saved.");
    }

    private static BoxCollider2D CreateWall(Transform parent, string name, Vector2 localPosition, Vector2 size, int layer)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
        if (layer >= 0) go.layer = layer;

        BoxCollider2D collider = go.GetComponent<BoxCollider2D>();
        if (collider == null)
            collider = go.AddComponent<BoxCollider2D>();
        collider.enabled = true;
        collider.size = size;
        collider.offset = Vector2.zero;
        return collider;
    }

    private static void CopyArray(SerializedProperty source, SerializedProperty destination)
    {
        destination.arraySize = source.arraySize;
        for (int i = 0; i < source.arraySize; i++)
            destination.GetArrayElementAtIndex(i).objectReferenceValue = source.GetArrayElementAtIndex(i).objectReferenceValue;
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
