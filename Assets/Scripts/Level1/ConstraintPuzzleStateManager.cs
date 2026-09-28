/*
ConstraintPuzzleStateManager.cs

Purpose:
- Controls the constraint puzzle state manager flow for the constraint and path-based puzzle systems and their drag-and-drop interactions.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ConstraintPuzzleStateManager : MonoBehaviour
{
    /// <summary>
    /// Maps Slot index (1-6) to Letter character (A-F).
    /// Empty slot = ' ' (space character).
    /// Example: slotState[1] = 'A' means Slot_1 contains Letter_A
    /// </summary>
    private Dictionary<int, char> slotState = new Dictionary<int, char>();

    private void Awake()
    {
        // Initialize all slots as empty
        for (int i = 1; i <= 6; i++)
        {
            slotState[i] = ' '; // Empty
        }
    }

    /// <summary>
    /// Gets the letter currently in a slot (or ' ' if empty)
    /// </summary>
    public char GetLetterInSlot(int slotNumber)
    {
        if (!slotState.ContainsKey(slotNumber))
        {
            Debug.LogWarning($"Invalid slot number: {slotNumber}");
            return ' ';
        }
        return slotState[slotNumber];
    }

    /// <summary>
    /// Gets the slot number containing a letter (or -1 if not found)
    /// </summary>
    public int GetSlotContainingLetter(char letter)
    {
        foreach (var kvp in slotState)
        {
            if (kvp.Value == letter)
                return kvp.Key;
        }
        return -1; // Not found
    }

    /// <summary>
    /// Places a letter in a slot and returns the letter that was there (or ' ' if empty)
    /// </summary>
    public char PlaceLetterInSlot(int slotNumber, char letter)
    {
        if (!slotState.ContainsKey(slotNumber))
        {
            Debug.LogError($"Invalid slot number: {slotNumber}");
            return ' ';
        }

        // Get the letter that's currently there
        char previousLetter = slotState[slotNumber];

        // Place the new letter
        slotState[slotNumber] = letter;

        // Log state change
        Debug.Log($"[State] Slot_{slotNumber}: {(previousLetter == ' ' ? "empty" : previousLetter.ToString())} → {letter}");

        return previousLetter;
    }

    /// <summary>
    /// Performs a swap between two slots and returns success
    /// </summary>
    public bool SwapSlots(int slotA, int slotB)
    {
        if (!slotState.ContainsKey(slotA) || !slotState.ContainsKey(slotB))
        {
            Debug.LogError($"Invalid slot numbers for swap: {slotA}, {slotB}");
            return false;
        }

        char letterA = slotState[slotA];
        char letterB = slotState[slotB];

        slotState[slotA] = letterB;
        slotState[slotB] = letterA;

        Debug.Log($"[State] Swap: Slot_{slotA} ({letterA}) ↔ Slot_{slotB} ({letterB})");

        return true;
    }

    /// <summary>
    /// Clears a slot (sets it to empty)
    /// </summary>
    public void ClearSlot(int slotNumber)
    {
        if (slotState.ContainsKey(slotNumber))
        {
            char previousLetter = slotState[slotNumber];
            slotState[slotNumber] = ' ';
            Debug.Log($"[State] Slot_{slotNumber}: {previousLetter} → empty");
        }
    }

    /// <summary>
    /// Returns the current state as a readable string
    /// </summary>
    public string GetStateAsString()
    {
        string state = "[State] ";
        for (int i = 1; i <= 6; i++)
        {
            char letter = slotState[i];
            state += $"Slot_{i}={( letter == ' ' ? "-" : letter.ToString())} ";
        }
        return state;
    }

    /// <summary>
    /// Prints the current state to console
    /// </summary>
    public void DebugPrintState()
    {
        Debug.Log(GetStateAsString());
    }
}
