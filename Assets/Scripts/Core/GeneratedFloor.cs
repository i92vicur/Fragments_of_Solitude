/*
GeneratedFloor.cs

Purpose:
- Build the visual floor, upper wall and roof layers for the main arena at runtime.
- Recreate the collision geometry that defines the playable map when complete-map mode is enabled.

Design decisions:
- Tile selection is deterministic for a given randomSeed, so visual variation does not change the map shape.
- Complete-map mode uses the hard-coded polygon and interior regions below instead of a rectangle. Those
    coordinates are level art data and must stay in sync with the scene.
- Generation is destructive: GenerateFloor clears previously generated roots before rebuilding them. It can
    run on Start or manually through the ContextMenu.

Configuration:
- floorTiles are required; wall and roof arrays are optional according to their generation toggles.
- Disable complete-map mode to use mapBounds as the generation rectangle instead.
*/

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
    [SerializeField] private bool useCompleteMapGeometry = true;
    [SerializeField] private Transform mapStructuresRoot;

    private static readonly Vector2[] CompleteMapBoundary =
    {
        new Vector2(-31f, 26f), new Vector2(25f, 26f), new Vector2(25f, 24f),
        new Vector2(31f, 24f), new Vector2(31f, 22f), new Vector2(34f, 22f),
        new Vector2(34f, 12f), new Vector2(36f, 12f), new Vector2(36f, 1f),
        new Vector2(34f, 1f), new Vector2(34f, -10f), new Vector2(30f, -10f),
        new Vector2(30f, -16f), new Vector2(22f, -16f), new Vector2(22f, -22f),
        new Vector2(8f, -22f), new Vector2(8f, -25f), new Vector2(-27f, -25f),
        new Vector2(-27f, -23f), new Vector2(-30f, -23f), new Vector2(-30f, -18f),
        new Vector2(-32f, -18f), new Vector2(-32f, 12f), new Vector2(-34f, 12f),
        new Vector2(-34f, 22f), new Vector2(-31f, 22f)
    };

    [Header("Generation")]
    [SerializeField] private int randomSeed = 0;
    [SerializeField] private int sortingOrder = -100;
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private bool autoGenerateOnStart = true;

    private GameObject generatedFloorRoot;
    private GameObject generatedUpperWallRoot;
    private GameObject generatedRoofRoot;
    private EdgeCollider2D completeMapBoundary;
    private GameObject generatedExteriorWallRoot;
    private static Sprite solidBlockSprite;

    private void Start()
    {
        if (autoGenerateOnStart)
            GenerateFloor();
    }

    [ContextMenu("Generate Floor")]
    public void GenerateFloor()
    {
        ClearFloor();

        if (mapBounds == null && !useCompleteMapGeometry)
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

        if (useCompleteMapGeometry)
            EnsureCompleteMapGeometry();

        if (mapStructuresRoot == null)
        {
            Debug.LogError($"{nameof(GeneratedFloor)}: mapStructuresRoot is required in complete-map mode.");
            return;
        }
        GenerateExteriorBlockWall();

        Bounds bounds = useCompleteMapGeometry ? completeMapBoundary.bounds : GetMapBounds();
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
                Vector3 tilePosition = new Vector3((x + 0.5f) * tileWidth, (y + 0.5f) * tileHeight + floorVerticalOffset, 0f);
                if (useCompleteMapGeometry && (!IsInsideCompleteMap(tilePosition) || IsInsideMapStructure(tilePosition)))
                    continue;

                GameObject tileObject = new GameObject("FloorTile");
                tileObject.transform.SetParent(generatedFloorRoot.transform, false);
                tileObject.transform.localPosition = tilePosition;
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

    private void EnsureCompleteMapGeometry()
    {
        if (completeMapBoundary == null)
            completeMapBoundary = GetComponent<EdgeCollider2D>();

        if (completeMapBoundary == null)
            completeMapBoundary = gameObject.AddComponent<EdgeCollider2D>();

        Vector2[] points = new Vector2[CompleteMapBoundary.Length + 1];
        for (int i = 0; i < CompleteMapBoundary.Length; i++)
            points[i] = CompleteMapBoundary[i];
        points[points.Length - 1] = CompleteMapBoundary[0];
        completeMapBoundary.points = points;
        completeMapBoundary.isTrigger = false;

        int environmentLayer = LayerMask.NameToLayer("Environment");
        if (environmentLayer >= 0)
            gameObject.layer = environmentLayer;

        if (arenaBorder != null)
            arenaBorder.SetBoundarySource(completeMapBoundary);
    }

    private bool IsInsideCompleteMap(Vector2 point)
    {
        bool inside = false;
        for (int i = 0, j = CompleteMapBoundary.Length - 1; i < CompleteMapBoundary.Length; j = i++)
        {
            Vector2 current = CompleteMapBoundary[i];
            Vector2 previous = CompleteMapBoundary[j];
            bool crosses = (current.y > point.y) != (previous.y > point.y);
            if (crosses && point.x < (previous.x - current.x) * (point.y - current.y) / (previous.y - current.y) + current.x)
                inside = !inside;
        }

        return inside;
    }

    private bool IsInsideMapStructure(Vector2 point)
    {
        Collider2D[] colliders = mapStructuresRoot.GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null && colliders[i].bounds.Contains(point))
                return true;
        }

        return false;
    }

    private void GenerateExteriorBlockWall()
    {
        if (!useCompleteMapGeometry)
            return;

        generatedExteriorWallRoot = new GameObject("GeneratedExteriorBlockWall");
        generatedExteriorWallRoot.transform.SetParent(transform, false);

        const float blockSpacing = 0.9f;
        for (int i = 0; i < CompleteMapBoundary.Length; i++)
        {
            Vector2 start = CompleteMapBoundary[i];
            Vector2 end = CompleteMapBoundary[(i + 1) % CompleteMapBoundary.Length];
            Vector2 segment = end - start;

            if (segment.sqrMagnitude <= 0.001f || !IsRenderedPerimeterSegment(i))
                continue;

            float length = segment.magnitude;
            Vector2 direction = segment / length;
            Vector2 outward = new Vector2(-direction.y, direction.x);
            float wallOffset = 0.5f;
            int tileCount = Mathf.Max(1, Mathf.CeilToInt(length / blockSpacing));
            float blockSize = length / tileCount * 1.04f;

            for (int tileIndex = 0; tileIndex < tileCount; tileIndex++)
            {
                float distance = Mathf.Min((tileIndex + 0.5f) * length / tileCount, length);
                Vector2 position = start + direction * distance + outward * wallOffset;

                CreateRaisedExteriorBlock(
                    position,
                    Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg,
                    blockSize);
            }
        }

        for (int i = 0; i < CompleteMapBoundary.Length; i++)
        {
            int previousSegment = (i - 1 + CompleteMapBoundary.Length) % CompleteMapBoundary.Length;
            int nextSegment = i;
            if (!IsRenderedPerimeterSegment(previousSegment) && !IsRenderedPerimeterSegment(nextSegment))
                continue;

            Vector2 outward = Vector2.zero;
            Vector2 cornerDirection = Vector2.right;
            if (IsRenderedPerimeterSegment(previousSegment))
            {
                Vector2 previousDirection = (CompleteMapBoundary[i] - CompleteMapBoundary[previousSegment]).normalized;
                outward += new Vector2(-previousDirection.y, previousDirection.x);
                cornerDirection = previousDirection;
            }

            if (IsRenderedPerimeterSegment(nextSegment))
            {
                Vector2 nextDirection = (CompleteMapBoundary[(i + 1) % CompleteMapBoundary.Length] - CompleteMapBoundary[i]).normalized;
                outward += new Vector2(-nextDirection.y, nextDirection.x);
                cornerDirection = nextDirection;
            }

            if (outward.sqrMagnitude <= 0.001f)
                outward = new Vector2(-cornerDirection.y, cornerDirection.x);
            else
                outward.Normalize();

            Vector2 cornerPosition = CompleteMapBoundary[i] + outward * 0.5f;
            CreateRaisedExteriorBlock(
                cornerPosition,
                Mathf.Atan2(cornerDirection.y, cornerDirection.x) * Mathf.Rad2Deg,
                blockSpacing);
        }
    }

    private bool IsRenderedPerimeterSegment(int segmentIndex)
    {
        Vector2 start = CompleteMapBoundary[segmentIndex];
        Vector2 end = CompleteMapBoundary[(segmentIndex + 1) % CompleteMapBoundary.Length];
        return segmentIndex >= 0
            && segmentIndex < CompleteMapBoundary.Length
            && (end - start).sqrMagnitude > 0.001f
            && !IsTopOpeningSegment(start, end);
    }

    private void CreateRaisedExteriorBlock(Vector2 position, float rotation, float blockSize)
    {
        if (solidBlockSprite == null)
        {
            solidBlockSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
            solidBlockSprite.name = "ProceduralGrayBlockSprite";
        }

        GameObject block = new GameObject("ExteriorRaisedBlock");
        block.transform.SetParent(generatedExteriorWallRoot.transform, false);
        block.transform.localPosition = new Vector3(position.x, position.y, -0.05f);
        block.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);

        CreateBlockFace(block.transform, "BlockBody", Vector3.zero,
            new Vector2(blockSize, blockSize), new Color(0.42f, 0.45f, 0.48f, 1f), 1100);
        CreateBlockFace(block.transform, "BlockTop", new Vector3(0f, blockSize * 0.32f, -0.01f),
            new Vector2(blockSize * 0.86f, blockSize * 0.22f), new Color(0.63f, 0.66f, 0.68f, 1f), 1102);
        CreateBlockFace(block.transform, "BlockFront", new Vector3(0f, -blockSize * 0.32f, -0.01f),
            new Vector2(blockSize * 0.86f, blockSize * 0.22f), new Color(0.25f, 0.27f, 0.29f, 1f), 1101);
    }

    private void CreateBlockFace(Transform parent, string name, Vector3 localPosition,
        Vector2 size, Color color, int sortingOrder)
    {
        GameObject face = new GameObject(name);
        face.transform.SetParent(parent, false);
        face.transform.localPosition = localPosition;
        face.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer renderer = face.AddComponent<SpriteRenderer>();
        renderer.sprite = solidBlockSprite;
        renderer.color = color;
        renderer.sortingLayerName = upperWallSortingLayerName;
        renderer.sortingOrder = sortingOrder;
    }

    private bool IsTopOpeningSegment(Vector2 start, Vector2 end)
    {
        return start.y >= 22f && end.y >= 22f && Mathf.Abs(start.y - end.y) < 0.01f;
    }

    private void DestroyGeneratedObject(GameObject generatedObject)
    {
        if (generatedObject == null)
            return;

        if (Application.isPlaying)
            Destroy(generatedObject);
        else
            DestroyImmediate(generatedObject);
    }

    private void GenerateUpperWall()
    {
        if (mapBounds == null)
            return;

        Bounds bounds = useCompleteMapGeometry ? completeMapBoundary.bounds : GetMapBounds();

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

        float arenaMinX = useCompleteMapGeometry ? completeMapBoundary.bounds.min.x : GetArenaLeftBoundaryX();
        float arenaMaxX = useCompleteMapGeometry ? completeMapBoundary.bounds.max.x : GetArenaRightBoundaryX();
        int startX = Mathf.FloorToInt(arenaMinX / wallTileWidth);
        int endX = Mathf.CeilToInt(arenaMaxX / wallTileWidth);

        // Border is the authoritative reference for the arena top. The wall lives just above it.
        float arenaTopY = useCompleteMapGeometry ? completeMapBoundary.bounds.max.y : GetArenaTopBoundaryY();
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

        float arenaMinX = useCompleteMapGeometry ? completeMapBoundary.bounds.min.x : GetArenaLeftBoundaryX();
        float arenaMaxX = useCompleteMapGeometry ? completeMapBoundary.bounds.max.x : GetArenaRightBoundaryX();
        int startX = Mathf.FloorToInt(arenaMinX / wallTileWidth);
        int endX = Mathf.CeilToInt(arenaMaxX / wallTileWidth);

        float arenaTopY = useCompleteMapGeometry ? completeMapBoundary.bounds.max.y : GetArenaTopBoundaryY();
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
            DestroyGeneratedObject(generatedRoofRoot);

            generatedRoofRoot = null;
        }

        if (generatedExteriorWallRoot != null)
        {
            DestroyGeneratedObject(generatedExteriorWallRoot);
            generatedExteriorWallRoot = null;
        }
    }
}
