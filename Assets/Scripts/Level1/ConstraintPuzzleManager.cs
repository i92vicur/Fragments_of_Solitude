/*
ConstraintPuzzleManager.cs

Purpose:
- Controls the constraint puzzle manager flow for the constraint and path-based puzzle systems and their drag-and-drop interactions.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ConstraintPuzzleManager : MonoBehaviour
{
    public const int PositionCount = 6;
    public static readonly string[] ItemIds = { "A", "B", "C", "D", "E", "F" };

    private readonly Dictionary<string, int> itemToPosition = new Dictionary<string, int>();
    private readonly Dictionary<int, string> positionToItem = new Dictionary<int, string>();
    private bool hasSolved;

    public event Action LayoutChanged;
    public event Action PuzzleSolved;

    public bool IsSolved => hasSolved;

    private void Awake()
    {
        ResetBoard();
        int solutionCount = CountSolutions();
        if (solutionCount != 1)
        {
            Debug.LogError($"[ConstraintPuzzle] Expected exactly 1 valid arrangement, found {solutionCount}. Check the rules before continuing.");
        }
        else
        {
            Debug.Log("[ConstraintPuzzle] Verified exactly 1 valid arrangement for the active constraints.");
        }
    }

    public void ResetBoard()
    {
        hasSolved = false;
        itemToPosition.Clear();
        positionToItem.Clear();
        LayoutChanged?.Invoke();
    }

    public bool TryCompleteIfValid()
    {
        if (hasSolved)
            return false;

        if (!IsValidArrangement(out string message))
            return false;

        hasSolved = true;
        PuzzleSolved?.Invoke();
        return true;
    }

    public void PlaceItemAtPosition(string itemId, int positionIndex)
    {
        if (hasSolved)
            return;

        if (string.IsNullOrEmpty(itemId))
            return;

        if (positionIndex < 1 || positionIndex > PositionCount)
            return;

        if (!ItemIds.Contains(itemId))
            return;

        if (itemToPosition.TryGetValue(itemId, out int existingPosition) && existingPosition == positionIndex)
            return;

        if (itemToPosition.ContainsKey(itemId))
        {
            positionToItem.Remove(itemToPosition[itemId]);
            itemToPosition.Remove(itemId);
        }

        if (positionToItem.TryGetValue(positionIndex, out string occupiedItemId))
        {
            itemToPosition.Remove(occupiedItemId);
            positionToItem.Remove(positionIndex);
        }

        itemToPosition[itemId] = positionIndex;
        positionToItem[positionIndex] = itemId;
        LayoutChanged?.Invoke();
    }

    public void ClearItem(string itemId)
    {
        if (hasSolved)
            return;

        if (!itemToPosition.TryGetValue(itemId, out int position))
            return;

        positionToItem.Remove(position);
        itemToPosition.Remove(itemId);
        LayoutChanged?.Invoke();
    }

    public int? GetPositionFor(string itemId)
    {
        return itemToPosition.TryGetValue(itemId, out int position) ? position : (int?)null;
    }

    public string GetItemAtPosition(int positionIndex)
    {
        return positionToItem.TryGetValue(positionIndex, out string itemId) ? itemId : null;
    }

    public bool HasAllItemsPlaced()
    {
        return itemToPosition.Count == PositionCount && positionToItem.Count == PositionCount;
    }

    public bool IsValidArrangement(out string message)
    {
        message = string.Empty;

        if (!HasAllItemsPlaced())
        {
            message = "The configuration is incomplete.";
            return false;
        }

        int? aPos = GetPositionFor("A");
        int? bPos = GetPositionFor("B");
        int? dPos = GetPositionFor("D");
        int? ePos = GetPositionFor("E");
        int? fPos = GetPositionFor("F");

        if (aPos == null || bPos == null || dPos == null || ePos == null || fPos == null)
        {
            message = "The configuration is incomplete.";
            return false;
        }

        if (aPos.Value != 3 && aPos.Value != 4)
        {
            message = "The configuration does not satisfy the clues.";
            return false;
        }

        if (bPos.Value != 1 && bPos.Value != 6)
        {
            message = "The configuration does not satisfy the clues.";
            return false;
        }

        if (!(dPos.Value < ePos.Value))
        {
            message = "The configuration does not satisfy the clues.";
            return false;
        }

        if (!(ePos.Value < fPos.Value))
        {
            message = "The configuration does not satisfy the clues.";
            return false;
        }

        if (Mathf.Abs(bPos.Value - dPos.Value) != 3)
        {
            message = "The configuration does not satisfy the clues.";
            return false;
        }

        message = "Puzzle Complete!";
        return true;
    }

    public int CountSolutions()
    {
        int count = 0;
        foreach (string[] permutation in GetAllPermutations(ItemIds))
        {
            Dictionary<string, int> placement = new Dictionary<string, int>();
            for (int i = 0; i < permutation.Length; i++)
            {
                placement[permutation[i]] = i + 1;
            }

            if (SatisfiesConstraints(placement))
                count++;
        }

        return count;
    }

    public string[] GetExpectedSolution()
    {
        return new[] { "B", "C", "A", "D", "E", "F" };
    }

    private static bool SatisfiesConstraints(Dictionary<string, int> placement)
    {
        int? aPos = GetPosition(placement, "A");
        int? bPos = GetPosition(placement, "B");
        int? dPos = GetPosition(placement, "D");
        int? ePos = GetPosition(placement, "E");
        int? fPos = GetPosition(placement, "F");

        if (aPos == null || bPos == null || dPos == null || ePos == null || fPos == null)
            return false;

        if (aPos.Value != 3 && aPos.Value != 4)
            return false;

        if (bPos.Value != 1 && bPos.Value != 6)
            return false;

        if (!(dPos.Value < ePos.Value))
            return false;

        if (!(ePos.Value < fPos.Value))
            return false;

        if (Mathf.Abs(bPos.Value - dPos.Value) != 3)
            return false;

        return true;
    }

    private static int? GetPosition(Dictionary<string, int> placement, string itemId)
    {
        return placement.TryGetValue(itemId, out int position) ? position : (int?)null;
    }

    private static IEnumerable<string[]> GetAllPermutations(string[] values)
    {
        List<string[]> results = new List<string[]>();
        Permute(values, 0, values.Length - 1, results);
        return results;
    }

    private static void Permute(string[] values, int startIndex, int endIndex, IList<string[]> results)
    {
        if (startIndex == endIndex)
        {
            results.Add((string[])values.Clone());
            return;
        }

        for (int i = startIndex; i <= endIndex; i++)
        {
            Swap(ref values[startIndex], ref values[i]);
            Permute(values, startIndex + 1, endIndex, results);
            Swap(ref values[startIndex], ref values[i]);
        }
    }

    private static void Swap(ref string left, ref string right)
    {
        string temp = left;
        left = right;
        right = temp;
    }
}

