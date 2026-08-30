/*
SlotDropZone.cs

Drop handler for ConstraintPuzzle_New slots with support for:
- Basic drop (slot empty)
- Swap/Exchange (slot occupied)
- State manager integration
*/

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public class SlotDropZone : MonoBehaviour, IDropHandler
{
    private Image slotImage;
    private Color originalColor;
    private RectTransform rectTransform;
    private ConstraintPuzzleStateManager stateManager;
    private ConstraintPuzzleIntegration integration;
    private Transform lettersArea;
    private int slotNumber = -1;

    private void Awake()
    {
        slotImage = GetComponent<Image>();
        originalColor = slotImage.color;
        rectTransform = GetComponent<RectTransform>();
        
        // Ensure this slot can receive raycast events
        slotImage.raycastTarget = true;

        // Extract slot number from name (e.g., "Slot_1" → 1)
        string slotName = gameObject.name;
        if (slotName.StartsWith("Slot_") && int.TryParse(slotName.Substring(5), out int number))
        {
            slotNumber = number;
        }

        // Find state manager in scene (must be in same scene)
        stateManager = FindFirstObjectByType<ConstraintPuzzleStateManager>();
        if (stateManager == null)
        {
            Debug.LogWarning($"SlotDropZone ({gameObject.name}): ConstraintPuzzleStateManager not found in scene");
        }

        // Find integration for constraint validation
        integration = FindFirstObjectByType<ConstraintPuzzleIntegration>();
        if (integration == null)
        {
            Debug.LogWarning($"SlotDropZone ({gameObject.name}): ConstraintPuzzleIntegration not found in scene");
        }

        // Find LettersArea for potential swap/reposition
        lettersArea = transform.root.Find("ConstraintPuzzleCanvas/PuzzleRoot/LettersArea");
        if (lettersArea == null)
        {
            Debug.LogWarning($"SlotDropZone ({gameObject.name}): LettersArea not found");
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        // Try to find a DraggableLetter in the dragged object
        GameObject draggedObject = eventData.pointerDrag;
        
        if (draggedObject == null)
        {
            Debug.LogWarning($"SlotDropZone ({gameObject.name}): Dropped object is null");
            return;
        }

        DraggableLetter draggableLetter = draggedObject.GetComponent<DraggableLetter>();
        
        if (draggableLetter == null)
        {
            Debug.LogWarning($"SlotDropZone ({gameObject.name}): Dropped object has no DraggableLetter component");
            return;
        }

        // Extract letter from name (e.g., "Letter_A" → 'A')
        char droppedLetter = ExtractLetterFromName(draggedObject.name);
        if (droppedLetter == ' ')
        {
            Debug.LogWarning($"SlotDropZone ({gameObject.name}): Could not extract letter from {draggedObject.name}");
            return;
        }

        Debug.Log($"SlotDropZone ({gameObject.name}): Drop received for {draggedObject.name}");

        // Check if this slot is already occupied by a LETTER (not the Number text child)
        bool isSlotOccupiedByLetter = false;
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).name.StartsWith("Letter_"))
            {
                isSlotOccupiedByLetter = true;
                break;
            }
        }

        if (isSlotOccupiedByLetter)
        {
            // Slot is occupied - perform SWAP
            PerformSwap(draggedObject, droppedLetter);
        }
        else
        {
            // Slot is empty - simple placement
            PerformPlacement(draggedObject, droppedLetter);
        }

        // Update state manager
        if (stateManager != null)
        {
            stateManager.DebugPrintState();
        }

        // Update constraint puzzle manager via integration
        if (integration != null && slotNumber > 0)
        {
            integration.OnLetterPlacedInSlot(droppedLetter, slotNumber);
        }
    }

    /// <summary>
    /// Places a letter in an empty slot
    /// </summary>
    private void PerformPlacement(GameObject letterObject, char letterChar)
    {
        // Move the letter to this slot (as a child, positioned inside)
        letterObject.transform.SetParent(transform);
        
        // Reset local position so it appears centered inside the slot
        RectTransform letterRect = letterObject.GetComponent<RectTransform>();
        if (letterRect != null)
        {
            letterRect.localPosition = Vector3.zero;
            letterRect.anchorMin = Vector2.zero;
            letterRect.anchorMax = Vector2.one;
            letterRect.offsetMin = Vector2.zero;
            letterRect.offsetMax = Vector2.zero;
        }

        // Update state
        if (stateManager != null && slotNumber > 0)
        {
            stateManager.PlaceLetterInSlot(slotNumber, letterChar);
        }

        Debug.Log($"[Drop] {letterObject.name} placed in {gameObject.name}");
    }

    /// <summary>
    /// Swaps the letter being dropped with the one already in this slot
    /// </summary>
    private void PerformSwap(GameObject incomingLetter, char incomingLetterChar)
    {
        // Find the letter child that is currently occupying this slot
        // (exclude non-letter children like number labels)
        GameObject occupantObject = null;
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.name.StartsWith("Letter_"))
            {
                occupantObject = child.gameObject;
                break;
            }
        }

        if (occupantObject == null)
        {
            Debug.LogError($"SlotDropZone ({gameObject.name}): Could not find occupant letter in children");
            return;
        }

        char occupantLetterChar = ExtractLetterFromName(occupantObject.name);
        if (occupantLetterChar == ' ')
        {
            Debug.LogError($"SlotDropZone ({gameObject.name}): Could not extract letter from occupant {occupantObject.name}");
            return;
        }

        Debug.Log($"[Swap] Starting: {incomingLetter.name} → {gameObject.name}, {occupantObject.name} needs new home");

        // Find which slot the incoming letter came from
        int incomingLetterCurrentSlot = -1;
        if (stateManager != null)
        {
            incomingLetterCurrentSlot = stateManager.GetSlotContainingLetter(incomingLetterChar);
        }

        if (incomingLetterCurrentSlot <= 0)
        {
            Debug.LogWarning($"SlotDropZone ({gameObject.name}): Could not find incoming letter's previous slot");
            // If we can't find it, just place it and abandon the swap
            PerformPlacement(incomingLetter, incomingLetterChar);
            return;
        }

        // Step 1: Temporarily detach incoming letter to avoid parent conflicts
        Transform incomingLetterOriginalParent = incomingLetter.transform.parent;
        incomingLetter.transform.SetParent(null, false);

        // Step 2: Move incoming letter to this slot
        incomingLetter.transform.SetParent(transform, false);
        RectTransform incomingRect = incomingLetter.GetComponent<RectTransform>();
        if (incomingRect != null)
        {
            incomingRect.localPosition = Vector3.zero;
            incomingRect.anchorMin = Vector2.zero;
            incomingRect.anchorMax = Vector2.one;
            incomingRect.offsetMin = Vector2.zero;
            incomingRect.offsetMax = Vector2.zero;
        }
        Debug.Log($"[Swap] {incomingLetter.name} → {gameObject.name}");

        // Step 3: Find the slot where the occupant should go
        SlotDropZone targetSlotDropZone = null;
        
        // Try to find by name in parent hierarchy
        Transform slotsAreaTransform = transform.parent; // Assuming Slot is direct child of SlotsArea
        if (slotsAreaTransform != null)
        {
            Transform targetSlotTransform = slotsAreaTransform.Find($"Slot_{incomingLetterCurrentSlot}");
            if (targetSlotTransform != null)
            {
                targetSlotDropZone = targetSlotTransform.GetComponent<SlotDropZone>();
            }
        }

        // Fallback: search entire scene
        if (targetSlotDropZone == null)
        {
            SlotDropZone[] allSlots = FindObjectsByType<SlotDropZone>(FindObjectsSortMode.None);
            foreach (SlotDropZone slot in allSlots)
            {
                if (slot.slotNumber == incomingLetterCurrentSlot)
                {
                    targetSlotDropZone = slot;
                    break;
                }
            }
        }

        if (targetSlotDropZone != null)
        {
            // Step 4: Move occupant to the target slot (the one where incoming came from)
            occupantObject.transform.SetParent(targetSlotDropZone.transform, false);
            RectTransform occupantRect = occupantObject.GetComponent<RectTransform>();
            if (occupantRect != null)
            {
                occupantRect.localPosition = Vector3.zero;
                occupantRect.anchorMin = Vector2.zero;
                occupantRect.anchorMax = Vector2.one;
                occupantRect.offsetMin = Vector2.zero;
                occupantRect.offsetMax = Vector2.zero;
            }
            Debug.Log($"[Swap] {occupantObject.name} → Slot_{incomingLetterCurrentSlot}");

            // Step 5: Update state manager
            if (stateManager != null)
            {
                stateManager.PlaceLetterInSlot(slotNumber, incomingLetterChar);
                stateManager.PlaceLetterInSlot(incomingLetterCurrentSlot, occupantLetterChar);
            }
        }
        else
        {
            Debug.LogError($"SlotDropZone ({gameObject.name}): Could not find target slot for occupant. Swap aborted.");
        }

        Debug.Log($"[Swap] Complete: {incomingLetter.name} in {gameObject.name}, {occupantObject.name} in Slot_{incomingLetterCurrentSlot}");
    }

    /// <summary>
    /// Extracts letter character from GameObject name (e.g., "Letter_A" → 'A')
    /// </summary>
    private char ExtractLetterFromName(string objectName)
    {
        if (objectName.StartsWith("Letter_") && objectName.Length > 7)
        {
            return objectName[7];
        }
        return ' '; // Invalid
    }
}
