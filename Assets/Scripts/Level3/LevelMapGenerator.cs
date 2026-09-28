/*
LevelMapGenerator.cs

Purpose:
- Controls the level map generator flow for the roguelite combat loop, enemy behaviors, progression, and procedural map generation.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class LevelMapGenerator : MonoBehaviour
{
    [Header("Map data")]
    [SerializeField] private LevelMapData mapData;
    [SerializeField] private int seed = 1337;
    [SerializeField] private float cellSize = 1f;

    [Header("Visuals")]
    [SerializeField] private Sprite[] floorTiles;
    [SerializeField] private Sprite[] wallTiles;
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private bool autoGenerateOnStart = true;
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 0;

    [Header("Environment")]
    [SerializeField] private bool generateWallsWithColliders = true;
    [SerializeField] private bool generateFloorVisuals = true;

    private Transform generatedMapRoot;
    private System.Random deterministicRandom;
    private readonly List<GameObject> generatedObjects = new List<GameObject>();

    public LevelMapData MapData => mapData;

    private void Reset()
    {
        AutoAssignDefaultAssets();
    }

    private void Start()
    {
        if (autoGenerateOnStart)
            GenerateMap();
    }

    [ContextMenu("Generate Map")]
    public void GenerateMap()
    {
        if (mapData == null)
        {
#if UNITY_EDITOR
            LevelMapData asset = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelMapData>("Assets/ScriptableObjects/Level3/Level3_DefaultMap.asset");
            if (asset != null)
            {
                mapData = asset;
            }
            else
            {
                mapData = ScriptableObject.CreateInstance<LevelMapData>();
                mapData.InitializeDefaultLayout();
            }
#else
            mapData = ScriptableObject.CreateInstance<LevelMapData>();
            mapData.InitializeDefaultLayout();
#endif
        }

        mapData.ValidateRows();
        ClearMap();

        GameObject generatedRoot = new GameObject("GeneratedLevelMap");
        generatedRoot.transform.SetParent(transform, false);
        generatedRoot.transform.localPosition = Vector3.zero;
        generatedMapRoot = generatedRoot.transform;

        deterministicRandom = new System.Random(seed);

        int width = mapData.Width;
        int height = mapData.Height;
        float originX = -(width * cellSize) * 0.5f + (cellSize * 0.5f);
        float originY = -(height * cellSize) * 0.5f + (cellSize * 0.5f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                char cell = mapData.GetCell(x, y);
                Vector3 worldPosition = new Vector3(originX + x * cellSize, originY + y * cellSize, 0f);

                switch (cell)
                {
                    case LevelMapData.Floor:
                        if (generateFloorVisuals)
                            CreateFloorTile(worldPosition);
                        break;

                    case LevelMapData.Wall:
                        CreateWallTile(worldPosition);
                        break;

                    case LevelMapData.Obstacle:
                        CreateObstacle(worldPosition, x, y);
                        break;

                    case LevelMapData.Spawn:
                        if (generateFloorVisuals)
                            CreateFloorTile(worldPosition);
                        break;
                }
            }
        }
    }

    public bool IsWalkableAt(int x, int y)
    {
        if (mapData == null)
            return false;

        return mapData.IsWalkable(x, y);
    }

    public bool IsSolidAt(int x, int y)
    {
        if (mapData == null)
            return true;

        return mapData.IsSolid(x, y);
    }

    public Vector2Int WorldToCell(Vector2 worldPosition)
    {
        if (mapData == null)
            return Vector2Int.zero;

        int width = mapData.Width;
        int height = mapData.Height;
        float halfWidth = width * cellSize * 0.5f;
        float halfHeight = height * cellSize * 0.5f;

        int x = Mathf.FloorToInt((worldPosition.x + halfWidth) / cellSize);
        int y = Mathf.FloorToInt((worldPosition.y + halfHeight) / cellSize);

        x = Mathf.Clamp(x, 0, Mathf.Max(0, width - 1));
        y = Mathf.Clamp(y, 0, Mathf.Max(0, height - 1));
        return new Vector2Int(x, y);
    }

    public void ClearMap()
    {
        if (generatedMapRoot != null)
        {
            if (Application.isPlaying)
                Destroy(generatedMapRoot);
            else
                DestroyImmediate(generatedMapRoot);

            generatedMapRoot = null;
        }

        generatedObjects.Clear();
    }

    [ContextMenu("Assign Default Level3 Assets")]
    public void AutoAssignDefaultAssets()
    {
#if UNITY_EDITOR
        floorTiles = new[]
        {
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Arena/Level3/floor_tiles.png")
        };

        wallTiles = new[]
        {
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Arena/Level3/wall_tiles.png")
        };

        obstaclePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Level3/Environment/BasicObstacle_Medium.prefab");
        if (obstaclePrefab == null)
        {
            obstaclePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Level3/Environment/BasicObstacle.prefab");
        }

        if (mapData == null)
        {
            mapData = AssetDatabase.LoadAssetAtPath<LevelMapData>("Assets/ScriptableObjects/Level3/Level3_DefaultMap.asset");
            if (mapData == null)
            {
                mapData = ScriptableObject.CreateInstance<LevelMapData>();
                mapData.InitializeDefaultLayout();
            }
        }
#endif
    }

    private void CreateFloorTile(Vector3 worldPosition)
    {
        GameObject floorCell = new GameObject("FloorCell");
        floorCell.transform.SetParent(generatedMapRoot, false);
        floorCell.transform.position = worldPosition;

        SpriteRenderer spriteRenderer = floorCell.AddComponent<SpriteRenderer>();
        if (floorTiles != null && floorTiles.Length > 0)
        {
            spriteRenderer.sprite = floorTiles[deterministicRandom.Next(floorTiles.Length)];
        }
        spriteRenderer.sortingLayerName = sortingLayerName;
        spriteRenderer.sortingOrder = sortingOrder;
        spriteRenderer.color = new Color(0.8f, 0.8f, 0.8f, 1f);

        generatedObjects.Add(floorCell);
    }

    private void CreateWallTile(Vector3 worldPosition)
    {
        GameObject wallCell = new GameObject("WallCell");
        wallCell.transform.SetParent(generatedMapRoot, false);
        wallCell.transform.position = worldPosition;

        SpriteRenderer spriteRenderer = wallCell.AddComponent<SpriteRenderer>();
        if (wallTiles != null && wallTiles.Length > 0)
        {
            spriteRenderer.sprite = wallTiles[deterministicRandom.Next(wallTiles.Length)];
        }
        spriteRenderer.sortingLayerName = sortingLayerName;
        spriteRenderer.sortingOrder = sortingOrder + 1;

        if (generateWallsWithColliders)
        {
            BoxCollider2D collider = wallCell.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one * 0.96f;
            int environmentLayer = LayerMask.NameToLayer("Environment");
            if (environmentLayer >= 0)
                wallCell.layer = environmentLayer;
        }

        generatedObjects.Add(wallCell);
    }

    private void CreateObstacle(Vector3 worldPosition, int x, int y)
    {
        if (obstaclePrefab != null)
        {
            GameObject obstacle = Instantiate(obstaclePrefab, worldPosition, Quaternion.identity, generatedMapRoot);
            obstacle.name = $"Obstacle_{x}_{y}";
            obstacle.transform.localScale = Vector3.one;
            generatedObjects.Add(obstacle);
            return;
        }

        GameObject fallback = new GameObject($"Obstacle_{x}_{y}");
        fallback.transform.SetParent(generatedMapRoot, false);
        fallback.transform.position = worldPosition;

        SpriteRenderer spriteRenderer = fallback.AddComponent<SpriteRenderer>();
        spriteRenderer.color = new Color(0.7f, 0.35f, 0.25f, 1f);
        spriteRenderer.sortingLayerName = sortingLayerName;
        spriteRenderer.sortingOrder = sortingOrder + 2;

        BoxCollider2D collider = fallback.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one * 0.96f;
        int environmentLayer = LayerMask.NameToLayer("Environment");
        if (environmentLayer >= 0)
            fallback.layer = environmentLayer;

        generatedObjects.Add(fallback);
    }
}
