/*
PuzzleBoard.cs

Purpose:
- Controls the puzzle board flow for the constraint and path-based puzzle systems and their drag-and-drop interactions.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PuzzleBoard : MonoBehaviour
{
    [Header("Board Setup")]
    [SerializeField, Min(2)] private int boardWidth = 4;
    [SerializeField, Min(2)] private int boardHeight = 4;
    [SerializeField] private GameObject tilePrefab;

    [Header("Board Layout")]
    [SerializeField] private RectTransform boardVisualRoot;
    [SerializeField] private Vector2 tileSize = new Vector2(140f, 140f);
    [SerializeField] private Vector2 tileSpacing = new Vector2(12f, 12f);
    [SerializeField] private Vector2 boardOffset = new Vector2(0f, -40f);
    [SerializeField, Range(0.05f, 0.5f)] private float slideDurationSeconds = 0.15f;

    [Header("Shuffle")]
    [SerializeField, Min(1)] private int shuffleMoveCount = 120;

    [Header("Completion")]

    private int[] tilesByIndex;
    private Vector2Int emptyCell;
    private int moveCount;
    private bool isMoveInProgress;
    private bool isPuzzleCompleted;
    private readonly Dictionary<int, PuzzleTile> tilesByNumber = new Dictionary<int, PuzzleTile>();

    public event Action PuzzleCompleted;

    public int BoardWidth => boardWidth;
    public int BoardHeight => boardHeight;
    public Vector2Int EmptyCell => emptyCell;
    public int MoveCount => moveCount;
    public bool IsPuzzleCompleted => isPuzzleCompleted;

    private void Start()
    {
        StartNewPuzzle();
    }

    private void Update()
    {
        if (isMoveInProgress || isPuzzleCompleted)
            return;

        Vector2Int requestedTileCell;
        if (!TryGetKeyboardRequestedTileCell(out requestedTileCell))
            return;

        int tileNumber = GetTileNumberAt(requestedTileCell);
        if (tileNumber <= 0)
            return;

        // Reuse exactly the same internal movement path as mouse clicks.
        TryMoveTile(tileNumber);
    }

    public int GetTileNumberAt(Vector2Int cell)
    {
        if (!IsInsideBoard(cell))
            return -1;

        return tilesByIndex[ToIndex(cell)];
    }

    public Vector2Int GetCellOfTile(int tileNumber)
    {
        for (int i = 0; i < tilesByIndex.Length; i++)
        {
            if (tilesByIndex[i] == tileNumber)
                return ToCell(i);
        }

        return new Vector2Int(-1, -1);
    }

    public Vector2 GetAnchoredPositionForCell(Vector2Int cell)
    {
        float stepX = tileSize.x + tileSpacing.x;
        float stepY = tileSize.y + tileSpacing.y;

        float originX = -((boardWidth - 1) * stepX) * 0.5f;
        float originY = ((boardHeight - 1) * stepY) * 0.5f;

        return new Vector2(
            originX + cell.x * stepX,
            originY - cell.y * stepY);
    }

    private void InitializeSolvedBoard()
    {
        int totalCells = boardWidth * boardHeight;
        tilesByIndex = new int[totalCells];
        moveCount = 0;
        isMoveInProgress = false;
        isPuzzleCompleted = false;

        for (int i = 0; i < totalCells; i++)
            tilesByIndex[i] = i + 1;

        tilesByIndex[totalCells - 1] = 0;
        emptyCell = ToCell(totalCells - 1);
    }

    private void EnsureVisualRoot()
    {
        if (boardVisualRoot != null)
            return;

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[PuzzleBoard] No Canvas found in scene for tile visuals.", this);
            return;
        }

        GameObject visualRoot = new GameObject("BoardVisualRoot", typeof(RectTransform));
        boardVisualRoot = visualRoot.GetComponent<RectTransform>();
        boardVisualRoot.SetParent(canvas.transform, false);
        boardVisualRoot.anchorMin = new Vector2(0.5f, 0.5f);
        boardVisualRoot.anchorMax = new Vector2(0.5f, 0.5f);
        boardVisualRoot.pivot = new Vector2(0.5f, 0.5f);
        boardVisualRoot.anchoredPosition = boardOffset;
    }

    private GameObject GetTileTemplate()
    {
        if (tilePrefab != null)
            return tilePrefab;

        GameObject fallbackTile = new GameObject("PuzzleTileFallback", typeof(RectTransform), typeof(Image), typeof(Button), typeof(PuzzleTile));
        RectTransform tileRect = fallbackTile.GetComponent<RectTransform>();
        tileRect.sizeDelta = tileSize;

        Image tileImage = fallbackTile.GetComponent<Image>();
        tileImage.color = new Color(0.22f, 0.46f, 0.86f, 1f);

        Button tileButton = fallbackTile.GetComponent<Button>();
        tileButton.targetGraphic = tileImage;

        GameObject labelGO = new GameObject("NumberLabel", typeof(RectTransform), typeof(Text));
        labelGO.transform.SetParent(fallbackTile.transform, false);

        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text label = labelGO.GetComponent<Text>();
        label.font = Resources.Load<Font>("Fonts/PressStart2P-Regular");
        label.fontSize = 38;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.text = "1";

        tilePrefab = fallbackTile;
        return tilePrefab;
    }

    private void BuildTilesFromLogicalState()
    {
        GameObject tileTemplate = GetTileTemplate();

        if (boardVisualRoot == null)
        {
            Debug.LogError("[PuzzleBoard] Board visual root is missing.", this);
            return;
        }

        tilesByNumber.Clear();
        ClearExistingVisualTiles();

        int totalCells = boardWidth * boardHeight;
        for (int i = 0; i < totalCells; i++)
        {
            int tileNumber = tilesByIndex[i];
            if (tileNumber == 0)
                continue;

            GameObject tileObject = Instantiate(tileTemplate, boardVisualRoot);
            tileObject.name = $"Tile_{tileNumber}";

            PuzzleTile tile = tileObject.GetComponent<PuzzleTile>();
            if (tile == null)
            {
                Debug.LogError("[PuzzleBoard] Tile prefab is missing PuzzleTile component.", tileObject);
                Destroy(tileObject);
                continue;
            }

            Vector2Int cell = ToCell(i);
            int clickedTileNumber = tileNumber;
            tile.SetNumber(tileNumber);
            tile.SetAnchoredPosition(GetAnchoredPositionForCell(cell));

            Button button = tileObject.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => TryMoveTile(clickedTileNumber));
            }

            tilesByNumber[tileNumber] = tile;
        }

        ResizeVisualRoot();
    }

    private void ClearExistingVisualTiles()
    {
        for (int i = boardVisualRoot.childCount - 1; i >= 0; i--)
            Destroy(boardVisualRoot.GetChild(i).gameObject);
    }

    private void ResizeVisualRoot()
    {
        float width = boardWidth * tileSize.x + (boardWidth - 1) * tileSpacing.x;
        float height = boardHeight * tileSize.y + (boardHeight - 1) * tileSpacing.y;
        boardVisualRoot.sizeDelta = new Vector2(width, height);
        boardVisualRoot.anchoredPosition = boardOffset;
    }

    private void TryMoveTile(int tileNumber)
    {
        TryMoveTileInternal(tileNumber, countAsPlayerMove: true, animateMove: true);
    }

    private void ShuffleNewPuzzle()
    {
        if (shuffleMoveCount < 1)
            shuffleMoveCount = 1;

        const int maxShuffleAttempts = 10;
        int attempt = 0;

        do
        {
            InitializeSolvedBoard();
            ResetTilesToSolvedStateInstant();

            Vector2Int previousEmptyCell = emptyCell;
            for (int i = 0; i < shuffleMoveCount; i++)
            {
                if (!TryGetRandomShuffleTileCell(previousEmptyCell, out Vector2Int tileCell))
                    continue;

                int tileNumber = GetTileNumberAt(tileCell);
                if (tileNumber <= 0)
                    continue;

                if (TryMoveTileInternal(tileNumber, countAsPlayerMove: false, animateMove: false))
                    previousEmptyCell = tileCell;
            }

            moveCount = 0;
            attempt++;
        }
        while (IsSolvedBoard() && attempt < maxShuffleAttempts);
    }

    public void RestartPuzzle()
    {
        StopAllCoroutines();
        StartNewPuzzle();
    }

    private void StartNewPuzzle()
    {
        InitializeSolvedBoard();
        EnsureVisualRoot();
        BuildTilesFromLogicalState();
        ShuffleNewPuzzle();
    }

    private bool TryMoveTileInternal(int tileNumber, bool countAsPlayerMove, bool animateMove)
    {
        if (isMoveInProgress || isPuzzleCompleted)
            return false;

        if (!tilesByNumber.TryGetValue(tileNumber, out PuzzleTile tile))
            return false;

        Vector2Int tileCell = GetCellOfTile(tileNumber);
        if (!IsInsideBoard(tileCell))
            return false;

        if (!IsAdjacent(tileCell, emptyCell))
            return false;

        isMoveInProgress = true;

        Vector2Int previousEmptyCell = emptyCell;
        int tileIndex = ToIndex(tileCell);
        int emptyIndex = ToIndex(previousEmptyCell);

        // Logical swap: tile goes into empty cell, clicked tile cell becomes empty.
        tilesByIndex[emptyIndex] = tileNumber;
        tilesByIndex[tileIndex] = 0;
        emptyCell = tileCell;

        if (countAsPlayerMove)
            moveCount += 1;

        bool solvedAfterMove = countAsPlayerMove && IsSolvedBoard();
        if (solvedAfterMove)
            CompletePuzzle();

        Vector2 targetPosition = GetAnchoredPositionForCell(previousEmptyCell);
        if (animateMove)
        {
            StartCoroutine(AnimateTileSlide(tile, targetPosition));
        }
        else
        {
            tile.SetAnchoredPosition(targetPosition);
            isMoveInProgress = false;
        }

        return true;
    }

    private void CompletePuzzle()
    {
        if (isPuzzleCompleted)
            return;

        isPuzzleCompleted = true;
        PuzzleCompleted?.Invoke();
    }

    private void ResetTilesToSolvedStateInstant()
    {
        int totalCells = boardWidth * boardHeight;
        tilesByIndex = new int[totalCells];

        for (int i = 0; i < totalCells; i++)
            tilesByIndex[i] = i + 1;

        tilesByIndex[totalCells - 1] = 0;
        emptyCell = ToCell(totalCells - 1);

        foreach (KeyValuePair<int, PuzzleTile> entry in tilesByNumber)
        {
            Vector2Int solvedCell = ToCell(entry.Key - 1);
            entry.Value.SetAnchoredPosition(GetAnchoredPositionForCell(solvedCell));
        }
    }

    private IEnumerator AnimateTileSlide(PuzzleTile tile, Vector2 targetAnchoredPosition)
    {
        RectTransform tileRect = tile.GetComponent<RectTransform>();
        if (tileRect == null)
        {
            tile.SetAnchoredPosition(targetAnchoredPosition);
            isMoveInProgress = false;
            yield break;
        }

        float duration = Mathf.Max(0.0001f, slideDurationSeconds);
        Vector2 startPosition = tileRect.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            tile.SetAnchoredPosition(Vector2.Lerp(startPosition, targetAnchoredPosition, t));
            yield return null;
        }

        tile.SetAnchoredPosition(targetAnchoredPosition);
        isMoveInProgress = false;
    }

    private bool IsAdjacent(Vector2Int a, Vector2Int b)
    {
        int distance = Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        return distance == 1;
    }

    private bool TryGetRandomShuffleTileCell(Vector2Int previousEmptyCell, out Vector2Int tileCell)
    {
        List<Vector2Int> candidates = new List<Vector2Int>(4);

        Vector2Int up = new Vector2Int(emptyCell.x, emptyCell.y - 1);
        Vector2Int down = new Vector2Int(emptyCell.x, emptyCell.y + 1);
        Vector2Int left = new Vector2Int(emptyCell.x - 1, emptyCell.y);
        Vector2Int right = new Vector2Int(emptyCell.x + 1, emptyCell.y);

        if (IsInsideBoard(up) && up != previousEmptyCell) candidates.Add(up);
        if (IsInsideBoard(down) && down != previousEmptyCell) candidates.Add(down);
        if (IsInsideBoard(left) && left != previousEmptyCell) candidates.Add(left);
        if (IsInsideBoard(right) && right != previousEmptyCell) candidates.Add(right);

        if (candidates.Count == 0)
        {
            if (IsInsideBoard(up)) candidates.Add(up);
            if (IsInsideBoard(down)) candidates.Add(down);
            if (IsInsideBoard(left)) candidates.Add(left);
            if (IsInsideBoard(right)) candidates.Add(right);
        }

        if (candidates.Count == 0)
        {
            tileCell = emptyCell;
            return false;
        }

        tileCell = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        return true;
    }

    private bool IsSolvedBoard()
    {
        int totalCells = boardWidth * boardHeight;

        for (int i = 0; i < totalCells - 1; i++)
        {
            if (tilesByIndex[i] != i + 1)
                return false;
        }

        return tilesByIndex[totalCells - 1] == 0;
    }

    private bool TryGetKeyboardRequestedTileCell(out Vector2Int tileCell)
    {
        tileCell = emptyCell;

        // Controls are relative to the empty position.
        // Up/W means: tile above empty moves down into empty.
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            tileCell = new Vector2Int(emptyCell.x, emptyCell.y - 1);
            return true;
        }

        // Down/S means: tile below empty moves up into empty.
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            tileCell = new Vector2Int(emptyCell.x, emptyCell.y + 1);
            return true;
        }

        // Left/A means: tile left of empty moves right into empty.
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            tileCell = new Vector2Int(emptyCell.x - 1, emptyCell.y);
            return true;
        }

        // Right/D means: tile right of empty moves left into empty.
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            tileCell = new Vector2Int(emptyCell.x + 1, emptyCell.y);
            return true;
        }

        return false;
    }

    private int ToIndex(Vector2Int cell)
    {
        return cell.y * boardWidth + cell.x;
    }

    private Vector2Int ToCell(int index)
    {
        int x = index % boardWidth;
        int y = index / boardWidth;
        return new Vector2Int(x, y);
    }

    private bool IsInsideBoard(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < boardWidth && cell.y >= 0 && cell.y < boardHeight;
    }
}
