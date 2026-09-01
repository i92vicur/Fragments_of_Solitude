/*
ConstraintPuzzleIntegration.cs

Bridge between visual drag & drop system (SlotDropZone + DraggableLetter)
and the logical constraint validation system (ConstraintPuzzleManager).

This system:
1. Keeps ConstraintPuzzleManager in sync with slot placements
2. Enables the Check button to validate the current arrangement
3. Prevents modification after solve
4. Does NOT modify the existing drag & drop mechanics

NOT a Singleton. References are set per-scene.
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
    /// Called by SlotDropZone after a successful placement or swap.
    /// Synchronizes the internal manager state.
    /// </summary>
    public void OnLetterPlacedInSlot(char letterChar, int slotNumber)
    {
        InitializeReferences();
        
        if (!hasInitialized || manager == null)
        {
            Debug.LogWarning("[ConstraintPuzzleIntegration] Cannot update: manager not initialized");
            return;
        }

        if (manager.IsSolved)
        {
            Debug.Log("[ConstraintPuzzleIntegration] Puzzle already solved, ignoring placement");
            return;
        }

        // Convert char to string itemId (A → "A")
        string itemId = letterChar.ToString();
        
        // Position is the slot number
        int positionIndex = slotNumber;

        Debug.Log($"[ConstraintPuzzleIntegration] Placing {itemId} at position {positionIndex}");
        manager.PlaceItemAtPosition(itemId, positionIndex);
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
        
        if (!hasInitialized || manager == null)
        {
            Debug.LogWarning("[ConstraintPuzzleIntegration] Cannot reset: manager not initialized");
            return;
        }

        manager.ResetBoard();
        Debug.Log("[ConstraintPuzzleIntegration] Puzzle reset");
    }
}
