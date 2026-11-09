/*
LevelMapData.cs

Purpose:
- Controls the level map data flow for the roguelite combat loop, enemy behaviors, progression, and procedural map generation.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using UnityEngine;

[CreateAssetMenu(fileName = "LevelMapData", menuName = "Roguelite/Level Map Data")]
public class LevelMapData : ScriptableObject
{
    public const char Empty = ' ';
    public const char Floor = '.';
    public const char Wall = '#';
    public const char Obstacle = 'O';
    public const char Spawn = 'S';

    [SerializeField, TextArea(8, 30)]
    private string[] rows = new string[0];

    public string[] Rows => rows;

    public void InitializeDefaultLayout()
    {
        const int width = 72;
        const int height = 52;
        rows = new string[height];

        for (int y = 0; y < height; y++)
        {
            char[] line = new char[width];
            for (int x = 0; x < width; x++)
            {
                line[x] = '.';
            }

            for (int x = 0; x < width; x++)
            {
                bool insideMainBounds = IsInsideMainSilhouette(x, y, width, height);
                bool isWall = !insideMainBounds;

                if (isWall)
                {
                    line[x] = '#';
                    continue;
                }

                if (IsObstacleCell(x, y, width, height))
                {
                    line[x] = 'O';
                    continue;
                }

                if (IsInteriorWallCell(x, y, width, height))
                {
                    line[x] = '#';
                }
            }

            rows[y] = new string(line);
        }
    }

    private static bool IsInsideMainSilhouette(int x, int y, int width, int height)
    {
        float cx = width * 0.5f;
        float cy = height * 0.5f;
        float dx = (x - cx) / (width * 0.5f - 1.5f);
        float dy = (y - cy) / (height * 0.5f - 1.5f);

        bool mainShape = (dx * dx * 1.25f) + (dy * dy * 1.35f) <= 1.0f;

        if (!mainShape)
            return false;

        bool leftIndent = x < 12 && y > 8 && y < height - 9;
        bool rightIndent = x > width - 13 && y > 9 && y < height - 10;
        bool topIndent = y < 9 && x > 18 && x < width - 18;
        bool bottomIndent = y > height - 10 && x > 15 && x < width - 15;
        bool midLeftCut = x < 20 && y > 18 && y < 34;
        bool midRightCut = x > width - 21 && y > 18 && y < 34;

        return !(leftIndent || rightIndent || topIndent || bottomIndent || midLeftCut || midRightCut);
    }

    private static bool IsInteriorWallCell(int x, int y, int width, int height)
    {
        bool leftBlock = x >= 18 && x <= 24 && y >= 10 && y <= 28;
        bool rightBlock = x >= width - 25 && x <= width - 19 && y >= 12 && y <= 28;
        bool topBridge = x >= 26 && x <= 46 && y >= 10 && y <= 15;
        bool midBridge = x >= 28 && x <= 44 && y >= 23 && y <= 29;
        bool bottomBridge = x >= 22 && x <= 50 && y >= height - 18 && y <= height - 13;

        return leftBlock || rightBlock || topBridge || midBridge || bottomBridge;
    }

    private static bool IsObstacleCell(int x, int y, int width, int height)
    {
        bool leftCluster = x >= 12 && x <= 17 && y >= 18 && y <= 24;
        bool rightCluster = x >= width - 18 && x <= width - 13 && y >= 18 && y <= 24;
        bool centerCluster = x >= 31 && x <= 39 && y >= 18 && y <= 24;
        bool lowerCluster = x >= 24 && x <= 48 && y >= height - 20 && y <= height - 15;

        return leftCluster || rightCluster || centerCluster || lowerCluster;
    }

    public int Width => rows == null || rows.Length == 0 ? 0 : rows[0].Length;

    public int Height => rows == null ? 0 : rows.Length;

    public char GetCell(int x, int y)
    {
        if (rows == null || rows.Length == 0)
            return Empty;

        if (y < 0 || y >= rows.Length)
            return Empty;

        if (x < 0 || x >= rows[y].Length)
            return Empty;

        return rows[y][x];
    }

    public bool IsWalkable(int x, int y)
    {
        char cell = GetCell(x, y);
        return cell == Floor || cell == Spawn;
    }

    public bool IsSolid(int x, int y)
    {
        char cell = GetCell(x, y);
        return cell == Wall || cell == Empty || cell == Obstacle;
    }

    public bool IsObstacle(int x, int y)
    {
        return GetCell(x, y) == Obstacle;
    }

    public void ValidateRows()
    {
        if (rows == null || rows.Length == 0)
        {
            rows = new[] { "#" };
            return;
        }

        int maxWidth = 0;
        for (int i = 0; i < rows.Length; i++)
        {
            if (rows[i] == null)
                rows[i] = string.Empty;

            maxWidth = Mathf.Max(maxWidth, rows[i].Length);
        }

        for (int i = 0; i < rows.Length; i++)
        {
            if (rows[i].Length < maxWidth)
                rows[i] = rows[i].PadRight(maxWidth, Empty);
        }
    }
}
