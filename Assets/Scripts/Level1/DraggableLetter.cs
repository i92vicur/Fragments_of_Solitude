/*
DraggableLetter.cs

Purpose:
- Controls the draggable letter flow for the constraint and path-based puzzle systems and their drag-and-drop interactions.

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
public class DraggableLetter : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private struct LayoutSnapshot
    {
        public Transform parent;
        public Vector3 localPosition;
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 offsetMin;
        public Vector2 offsetMax;
    }

    private LayoutSnapshot homeLayout;
    private LayoutSnapshot dragStartLayout;
    private ConstraintPuzzleIntegration integration;
    private bool isDragging = false;
    private bool dropHandled = false;

    public char Letter => name.Length > 7 && name.StartsWith("Letter_") ? name[7] : ' ';

    private void Awake()
    {
        // Ensure we have a Canvas to work with
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("DraggableLetter: No Canvas found in parent hierarchy!");
            enabled = false;
            return;
        }

        rectTransform = GetComponent<RectTransform>();
        
        // Add CanvasGroup if it doesn't exist (for alpha control during drag)
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        integration = FindFirstObjectByType<ConstraintPuzzleIntegration>();
        homeLayout = CaptureLayout();
    }

    /// <summary>Moves the letter inside a container (slot) and centers it.</summary>
    public void PlaceInside(Transform container)
    {
        rectTransform.SetParent(container, false);
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localPosition = Vector3.zero;
    }

    /// <summary>Restores the letter to its initial place in LettersArea.</summary>
    public void ReturnToHome()
    {
        ApplyLayout(homeLayout);
    }

    /// <summary>Called by a drop target that accepted the letter, so OnEndDrag does not undo the move.</summary>
    public void MarkDropHandled()
    {
        dropHandled = true;
    }

    private LayoutSnapshot CaptureLayout()
    {
        return new LayoutSnapshot
        {
            parent = rectTransform.parent,
            localPosition = rectTransform.localPosition,
            anchorMin = rectTransform.anchorMin,
            anchorMax = rectTransform.anchorMax,
            offsetMin = rectTransform.offsetMin,
            offsetMax = rectTransform.offsetMax
        };
    }

    private void ApplyLayout(LayoutSnapshot layout)
    {
        rectTransform.SetParent(layout.parent, false);
        rectTransform.anchorMin = layout.anchorMin;
        rectTransform.anchorMax = layout.anchorMax;
        rectTransform.offsetMin = layout.offsetMin;
        rectTransform.offsetMax = layout.offsetMax;
        rectTransform.localPosition = layout.localPosition;
    }

    private bool IsOverLettersArea(PointerEventData eventData)
    {
        RectTransform lettersArea = homeLayout.parent as RectTransform;
        return lettersArea != null
            && RectTransformUtility.RectangleContainsScreenPoint(lettersArea, eventData.position, eventData.pressEventCamera);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (integration != null && integration.IsSolved)
            return;

        isDragging = true;
        dropHandled = false;
        dragStartLayout = CaptureLayout();
        
        // Make slightly transparent to show it's being dragged
        canvasGroup.alpha = 0.7f;
        canvasGroup.blocksRaycasts = false;
        
        // Move to end of hierarchy to appear on top
        rectTransform.SetAsLastSibling();
        
        Debug.Log($"Started dragging {gameObject.name}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        
        // Follow the mouse/pointer position
        rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        isDragging = false;
        
        // Restore opacity
        canvasGroup.alpha = 1f;
        
        canvasGroup.blocksRaycasts = true;
        Debug.Log($"Stopped dragging {gameObject.name}");

        if (dropHandled)
            return;

        if (IsOverLettersArea(eventData))
        {
            if (integration != null)
                integration.ReleaseLetter(Letter);
            ReturnToHome();
        }
        else
        {
            ApplyLayout(dragStartLayout);
        }
    }
}
