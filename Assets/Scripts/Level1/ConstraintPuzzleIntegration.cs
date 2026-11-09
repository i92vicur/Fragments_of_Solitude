/*
ConstraintPuzzleIntegration.cs

Purpose:
- Controls the constraint puzzle integration flow for the constraint and path-based puzzle systems and their drag-and-drop interactions.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using UnityEngine;

[DisallowMultipleComponent]
public sealed class ConstraintPuzzleIntegration : MonoBehaviour
{
    [SerializeField] private ConstraintPuzzleManager manager;
    [SerializeField] private ConstraintPuzzleStateManager stateManager;

    private bool hasInitialized;

    private void Awake()
    {
        InitializeReferences();
    }

    private void InitializeReferences()
    {
        if (hasInitialized)
            return;

        if (manager == null)
            manager = GetComponent<ConstraintPuzzleManager>();
        if (manager == null)
            manager = FindFirstObjectByType<ConstraintPuzzleManager>();
        
        // Create manager if it doesn't exist
        if (manager == null)
        {
            GameObject managerGO = new GameObject("ConstraintPuzzleManager");
            manager = managerGO.AddComponent<ConstraintPuzzleManager>();
            Debug.Log("[ConstraintPuzzleIntegration] Created ConstraintPuzzleManager (auto-created)");
        }

        if (stateManager == null)
            stateManager = GetComponent<ConstraintPuzzleStateManager>();
        if (stateManager == null)
            stateManager = FindFirstObjectByType<ConstraintPuzzleStateManager>();

        // Create state manager if it doesn't exist
        if (stateManager == null)
        {
            GameObject stateGO = new GameObject("ConstraintPuzzleStateManager");
            stateManager = stateGO.AddComponent<ConstraintPuzzleStateManager>();
            Debug.Log("[ConstraintPuzzleIntegration] Created ConstraintPuzzleStateManager (auto-created)");
        }

        if (manager == null)
        {
            Debug.LogError("[ConstraintPuzzleIntegration] ConstraintPuzzleManager failed to initialize!");
            enabled = false;
            return;
        }

        if (stateManager == null)
        {
            Debug.LogError("[ConstraintPuzzleIntegration] ConstraintPuzzleStateManager failed to initialize!");
            enabled = false;
            return;
        }

        hasInitialized = true;
        Debug.Log("[ConstraintPuzzleIntegration] Initialized successfully");
    }

    /// <summary>
    /// Single entry point for placing a letter in a slot, used by mouse drops (and future gamepad input).
    /// sourceSlot is the slot the letter came from, or 0 if it came from LettersArea.
    /// If the target slot is occupied, the occupant goes to sourceSlot (swap) or back to LettersArea (displaced = occupant).
    /// Updates ConstraintPuzzleStateManager and re-synchronizes ConstraintPuzzleManager from it.
    /// </summary>
    public bool TryAssignLetter(char letter, int targetSlot, int sourceSlot, out char displaced)
    {
        displaced = ' ';
        InitializeReferences();

        if (!hasInitialized || manager.IsSolved)
            return false;

        if (targetSlot < 1 || targetSlot > ConstraintPuzzleManager.PositionCount)
            return false;

        if (System.Array.IndexOf(ConstraintPuzzleManager.ItemIds, letter.ToString()) < 0)
            return false;

        if (sourceSlot == targetSlot)
            return true;

        displaced = stateManager.GetLetterInSlot(targetSlot);
        if (displaced == letter)
            displaced = ' ';

        if (sourceSlot > 0)
            stateManager.ClearSlot(sourceSlot);

        int staleSlot = stateManager.GetSlotContainingLetter(letter);
        if (staleSlot > 0)
            stateManager.ClearSlot(staleSlot);

        if (displaced != ' ' && sourceSlot > 0)
            stateManager.PlaceLetterInSlot(sourceSlot, displaced);

        stateManager.PlaceLetterInSlot(targetSlot, letter);
        SyncManagerFromState();
        return true;
    }

    /// <summary>
    /// Frees the slot that holds the letter (used when a letter is returned to LettersArea).
    /// </summary>
    public void ReleaseLetter(char letter)
    {
        InitializeReferences();

        if (!hasInitialized || manager.IsSolved)
            return;

        int slot = stateManager.GetSlotContainingLetter(letter);
        if (slot <= 0)
            return;

        stateManager.ClearSlot(slot);
        SyncManagerFromState();
    }

    private void SyncManagerFromState()
    {
        manager.ResetBoard();
        for (int slot = 1; slot <= ConstraintPuzzleManager.PositionCount; slot++)
        {
            char letter = stateManager.GetLetterInSlot(slot);
            if (letter != ' ')
                manager.PlaceItemAtPosition(letter.ToString(), slot);
        }
    }

    /// <summary>
    /// Called by ConstraintPuzzleCheckHandler when user clicks Check.
    /// Validates the current arrangement using ConstraintPuzzleManager logic.
    /// </summary>
    public bool ValidateAndComplete()
    {
        InitializeReferences();
        
        if (!hasInitialized || manager == null)
        {
            Debug.LogWarning("[ConstraintPuzzleIntegration] Cannot validate: manager not initialized");
            return false;
        }

        if (manager.IsSolved)
        {
            Debug.Log("[ConstraintPuzzleIntegration] Puzzle already solved");
            return true;
        }

        bool isValid = manager.TryCompleteIfValid();
        
        if (isValid)
        {
            Debug.Log("[ConstraintPuzzleIntegration] ✓ Puzzle solved!");
        }
        else
        {
            if (manager.IsValidArrangement(out string message))
            {
                Debug.Log($"[ConstraintPuzzleIntegration] ✓ Arrangement is valid: {message}");
            }
            else
            {
                Debug.Log($"[ConstraintPuzzleIntegration] ✗ Arrangement is invalid: {message}");
            }
        }

        return isValid;
    }

    /// <summary>
    /// Returns whether the puzzle has been solved
    /// </summary>
    public bool IsSolved
    {
        get
        {
            InitializeReferences();
            return hasInitialized && manager != null && manager.IsSolved;
        }
    }

    /// <summary>
    /// Resets the puzzle to initial state
    /// </summary>
    public void ResetPuzzle()
    {
        InitializeReferences();

        if (!hasInitialized)
        {
            Debug.LogWarning("[ConstraintPuzzleIntegration] Cannot reset: manager not initialized");
            return;
        }

        for (int slot = 1; slot <= ConstraintPuzzleManager.PositionCount; slot++)
        {
            stateManager.ClearSlot(slot);
        }

        manager.ResetBoard();

        foreach (DraggableLetter letter in FindObjectsByType<DraggableLetter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            letter.ReturnToHome();
        }

        Debug.Log("[ConstraintPuzzleIntegration] Puzzle reset");
    }
}
