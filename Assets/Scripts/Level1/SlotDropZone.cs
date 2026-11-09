/*
SlotDropZone.cs

Purpose:
- Controls the slot drop zone flow for the constraint and path-based puzzle systems and their drag-and-drop interactions.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
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
    private ConstraintPuzzleIntegration integration;
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

        // Find integration, the single entry point for slot assignment and state updates
        integration = FindFirstObjectByType<ConstraintPuzzleIntegration>();
        if (integration == null)
        {
            Debug.LogWarning($"SlotDropZone ({gameObject.name}): ConstraintPuzzleIntegration not found in scene");
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
            return;

        DraggableLetter draggableLetter = eventData.pointerDrag.GetComponent<DraggableLetter>();
        if (draggableLetter == null)
            return;

        // Dropping a letter on the slot it already occupies only re-centers it
        if (draggableLetter.transform.parent == transform)
        {
            draggableLetter.PlaceInside(transform);
            draggableLetter.MarkDropHandled();
            return;
        }

        if (TryAssignLetter(draggableLetter))
            draggableLetter.MarkDropHandled();
    }

    /// <summary>
    /// Assigns the letter to this slot through ConstraintPuzzleIntegration and moves the physical objects to match.
    /// If this slot was occupied, the occupant goes to the letter's source slot, or back to LettersArea.
    /// </summary>
    public bool TryAssignLetter(DraggableLetter letter)
    {
        if (integration == null || slotNumber <= 0)
            return false;

        SlotDropZone sourceZone = letter.transform.parent != null
            ? letter.transform.parent.GetComponent<SlotDropZone>()
            : null;
        int sourceSlot = sourceZone != null ? sourceZone.slotNumber : 0;

        if (!integration.TryAssignLetter(letter.Letter, slotNumber, sourceSlot, out char displacedLetter))
            return false;

        if (displacedLetter != ' ')
        {
            DraggableLetter occupant = FindOccupant(letter);
            if (occupant != null)
            {
                if (sourceZone != null)
                    occupant.PlaceInside(sourceZone.transform);
                else
                    occupant.ReturnToHome();
            }
        }

        letter.PlaceInside(transform);
        return true;
    }

    private DraggableLetter FindOccupant(DraggableLetter excluded)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            DraggableLetter child = transform.GetChild(i).GetComponent<DraggableLetter>();
            if (child != null && child != excluded)
                return child;
        }
        return null;
    }
}
