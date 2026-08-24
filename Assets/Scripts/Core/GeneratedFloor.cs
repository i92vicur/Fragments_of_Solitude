using System.Collections.Generic;
using UnityEngine;

public class GeneratedFloor : MonoBehaviour
{
    [Header("Visual tiles")]
    [SerializeField] private Sprite[] floorTiles;

    [Header("Back wall / buildings")]
    [SerializeField] private Sprite[] upperWallTiles;
    [SerializeField] private Sprite[] roofTiles;
    [SerializeField] private bool generateUpperWall = true;
    [SerializeField] private bool generateRoof = true;
    [SerializeField] private int upperWallSortingOrder = -50;
    [SerializeField] private string upperWallSortingLayerName = "Default";

    [Header("Manual tuning")]
    [SerializeField] private float floorVerticalOffset = 0f;
    [SerializeField] private float topEdgeOffset = 0.05f;
    [SerializeField] private float roofGap = 0.05f;

    [Header("Map area")]
    [SerializeField] private Collider2D mapBounds;
    [SerializeField] private ArenaBorder arenaBorder;

    [Header("Generation")]
    [SerializeField] private int randomSeed = 0;
    [SerializeField] private int sortingOrder = -100;
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private bool autoGenerateOnStart = true;

    private GameObject generatedFloorRoot;
    private GameObject generatedUpperWallRoot;
    private GameObject generatedRoofRoot;

    private void Start()
    {
        if (autoGenerateOnStart)
            GenerateFloor();
    }

    [ContextMenu("Generate Floor")]
    public void GenerateFloor()
    {
        ClearFloor();

        if (mapBounds == null)
        {
            Debug.LogWarning($"{nameof(GeneratedFloor)}: mapBounds is not assigned.");
            return;
        }

        List<Sprite> validTiles = new List<Sprite>();
        if (floorTiles != null)
        {
            for (int i = 0; i < floorTiles.Length; i++)
            {
                if (floorTiles[i] != null)
                    validTiles.Add(floorTiles[i]);
            }
        }

        if (validTiles.Count == 0)
        {
            Debug.LogWarning($"{nameof(GeneratedFloor)}: No valid floor tile sprites were assigned.");
            return;
        }

        Sprite sampleTile = validTiles[0];
        Vector2 tileSize = sampleTile.bounds.size;
        float tileWidth = tileSize.x;
        float tileHeight = tileSize.y;

        if (tileWidth <= 0f || tileHeight <= 0f)
        {
            Debug.LogWarning($"{nameof(GeneratedFloor)}: Assigned tile dimensions are invalid.");
            return;
        }

        Bounds bounds = GetMapBounds();
        int startX = Mathf.FloorToInt(bounds.min.x / tileWidth);
        int endX = Mathf.CeilToInt(bounds.max.x / tileWidth);
        int startY = Mathf.FloorToInt(bounds.min.y / tileHeight);
        int endY = Mathf.CeilToInt(bounds.max.y / tileHeight);

        generatedFloorRoot = new GameObject("GeneratedFloor");
        generatedFloorRoot.transform.SetParent(transform, false);
        generatedFloorRoot.transform.localPosition = Vector3.zero;
        generatedFloorRoot.transform.localRotation = Quaternion.identity;
        generatedFloorRoot.transform.localScale = Vector3.one;

        System.Random random = new System.Random(randomSeed);

        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                GameObject tileObject = new GameObject("FloorTile");
                tileObject.transform.SetParent(generatedFloorRoot.transform, false);
                tileObject.transform.localPosition = new Vector3((x + 0.5f) * tileWidth, (y + 0.5f) * tileHeight + floorVerticalOffset, 0f);
                tileObject.transform.localRotation = Quaternion.identity;
                tileObject.transform.localScale = Vector3.one;

                SpriteRenderer spriteRenderer = tileObject.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = validTiles[random.Next(validTiles.Count)];
                spriteRenderer.sortingLayerName = sortingLayerName;
                spriteRenderer.sortingOrder = sortingOrder;
                spriteRenderer.drawMode = SpriteDrawMode.Simple;
                spriteRenderer.maskInteraction = SpriteMaskInteraction.None;
            }
        }

        if (generateUpperWall)
            GenerateUpperWall();
    }

    private void GenerateUpperWall()
    {
        if (mapBounds == null)
            return;

        Bounds bounds = GetMapBounds();

        List<Sprite> validWallTiles = new List<Sprite>();
        if (upperWallTiles != null)
        {
            for (int i = 0; i < upperWallTiles.Length; i++)
            {
                if (upperWallTiles[i] != null)
                    validWallTiles.Add(upperWallTiles[i]);
            }
        }

        if (validWallTiles.Count == 0)
            return;

        Sprite sampleWallTile = validWallTiles[0];
        Vector2 wallTileSize = sampleWallTile.bounds.size;
        float wallTileWidth = wallTileSize.x;
        float wallTileHeight = wallTileSize.y;

        if (wallTileWidth <= 0f || wallTileHeight <= 0f)
            return;

        generatedUpperWallRoot = new GameObject("GeneratedUpperWall");
        generatedUpperWallRoot.transform.SetParent(transform, false);
        generatedUpperWallRoot.transform.localPosition = Vector3.zero;
        generatedUpperWallRoot.transform.localRotation = Quaternion.identity;
        generatedUpperWallRoot.transform.localScale = Vector3.one;

        System.Random random = new System.Random(randomSeed + 5000);

        float arenaMinX = GetArenaLeftBoundaryX();
        float arenaMaxX = GetArenaRightBoundaryX();
        int startX = Mathf.FloorToInt(arenaMinX / wallTileWidth);
        int endX = Mathf.CeilToInt(arenaMaxX / wallTileWidth);

        // Border is the authoritative reference for the arena top. The wall lives just above it.
        float arenaTopY = GetArenaTopBoundaryY();
        float wallPadding = Mathf.Max(topEdgeOffset, 0.05f);
        float wallBottomY = arenaTopY + wallPadding;
        float wallCenterY = wallBottomY + wallTileHeight * 0.5f;

        for (int x = startX; x <= endX; x++)
        {
            GameObject tileObject = new GameObject("BackWallTile");
            tileObject.transform.SetParent(generatedUpperWallRoot.transform, false);
            tileObject.transform.localPosition = new Vector3((x + 0.5f) * wallTileWidth, wallCenterY, 0f);
            tileObject.transform.localRotation = Quaternion.identity;
            tileObject.transform.localScale = Vector3.one;

            SpriteRenderer spriteRenderer = tileObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = validWallTiles[random.Next(validWallTiles.Count)];
            spriteRenderer.sortingLayerName = upperWallSortingLayerName;
            spriteRenderer.sortingOrder = upperWallSortingOrder;
            spriteRenderer.drawMode = SpriteDrawMode.Simple;
            spriteRenderer.maskInteraction = SpriteMaskInteraction.None;
        }

        if (generateRoof)
            GenerateRoof(wallTileWidth, wallTileHeight, random);
    }

    private void GenerateRoof(float wallTileWidth, float wallTileHeight, System.Random random)
    {
        List<Sprite> validRoofTiles = new List<Sprite>();
        if (roofTiles != null)
        {
            for (int i = 0; i < roofTiles.Length; i++)
            {
                if (roofTiles[i] != null)
                    validRoofTiles.Add(roofTiles[i]);
            }
        }

        if (validRoofTiles.Count == 0)
            return;

        Sprite sampleRoofTile = validRoofTiles[0];
        Vector2 roofTileSize = sampleRoofTile.bounds.size;
        float roofTileWidth = roofTileSize.x;
        float roofTileHeight = roofTileSize.y;

        if (roofTileWidth <= 0f || roofTileHeight <= 0f)
            return;

        generatedRoofRoot = new GameObject("GeneratedRoof");
        generatedRoofRoot.transform.SetParent(transform, false);
        generatedRoofRoot.transform.localPosition = Vector3.zero;
        generatedRoofRoot.transform.localRotation = Quaternion.identity;
        generatedRoofRoot.transform.localScale = Vector3.one;

        float arenaMinX = GetArenaLeftBoundaryX();
        float arenaMaxX = GetArenaRightBoundaryX();
        int startX = Mathf.FloorToInt(arenaMinX / wallTileWidth);
        int endX = Mathf.CeilToInt(arenaMaxX / wallTileWidth);

        float arenaTopY = GetArenaTopBoundaryY();
        float wallPadding = Mathf.Max(topEdgeOffset, 0.05f);
        float roofBottomY = arenaTopY + wallPadding + wallTileHeight + Mathf.Max(roofGap, 0.05f);
        float roofCenterY = roofBottomY + roofTileHeight * 0.5f;

        for (int x = startX; x <= endX; x++)
        {
            float wallX = (x + 0.5f) * wallTileWidth;

            GameObject tileObject = new GameObject("RoofTile");
            tileObject.transform.SetParent(generatedRoofRoot.transform, false);
            tileObject.transform.localPosition = new Vector3(wallX, roofCenterY, 0f);
            tileObject.transform.localRotation = Quaternion.identity;
            tileObject.transform.localScale = Vector3.one;

            SpriteRenderer spriteRenderer = tileObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = validRoofTiles[random.Next(validRoofTiles.Count)];
            spriteRenderer.sortingLayerName = upperWallSortingLayerName;
            spriteRenderer.sortingOrder = upperWallSortingOrder + 1;
            spriteRenderer.drawMode = SpriteDrawMode.Simple;
            spriteRenderer.maskInteraction = SpriteMaskInteraction.None;
        }
    }

    private Bounds GetMapBounds()
    {
        if (mapBounds == null)
            return default;

        Collider2D[] candidateColliders = null;

        if (mapBounds.transform.parent != null)
        {
            candidateColliders = mapBounds.transform.parent.GetComponentsInChildren<Collider2D>();
            if (candidateColliders != null && candidateColliders.Length > 1)
            {
                Bounds combined = candidateColliders[0].bounds;
                for (int i = 1; i < candidateColliders.Length; i++)
                    combined.Encapsulate(candidateColliders[i].bounds);
                return combined;
            }
        }

        candidateColliders = mapBounds.GetComponentsInChildren<Collider2D>();
        if (candidateColliders != null && candidateColliders.Length > 0)
        {
            Bounds combined = candidateColliders[0].bounds;
            for (int i = 1; i < candidateColliders.Length; i++)
                combined.Encapsulate(candidateColliders[i].bounds);
            return combined;
        }

        return mapBounds.bounds;
    }

    private float GetArenaTopBoundaryY()
    {
        if (arenaBorder != null)
            return arenaBorder.GetTopArenaBorderY();

        if (mapBounds == null)
            return 0f;

        return GetMapBounds().max.y;
    }

    private float GetArenaLeftBoundaryX()
    {
        if (arenaBorder != null)
            return arenaBorder.GetLeftArenaBorderX();

        if (mapBounds == null)
            return 0f;

        return GetMapBounds().min.x;
    }

    private float GetArenaRightBoundaryX()
    {
        if (arenaBorder != null)
            return arenaBorder.GetRightArenaBorderX();

        if (mapBounds == null)
            return 0f;

        return GetMapBounds().max.x;
    }

    [ContextMenu("Clear Floor")]
    public void ClearFloor()
    {
        if (generatedFloorRoot != null)
        {
            if (Application.isPlaying)
                Destroy(generatedFloorRoot);
            else
                DestroyImmediate(generatedFloorRoot);

            generatedFloorRoot = null;
        }

        if (generatedUpperWallRoot != null)
        {
            if (Application.isPlaying)
                Destroy(generatedUpperWallRoot);
            else
                DestroyImmediate(generatedUpperWallRoot);

            generatedUpperWallRoot = null;
        }

        if (generatedRoofRoot != null)
        {
            if (Application.isPlaying)
                Destroy(generatedRoofRoot);
            else
                DestroyImmediate(generatedRoofRoot);

            generatedRoofRoot = null;
        }
    }
}
