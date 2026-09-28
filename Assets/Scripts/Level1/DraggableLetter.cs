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
    private Vector3 originalPosition;
    private Transform originalParent;
    private bool isDragging = false;

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

        // Store original state
        originalPosition = rectTransform.localPosition;
        originalParent = rectTransform.parent;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        
        // Make slightly transparent to show it's being dragged
        canvasGroup.alpha = 0.7f;
        
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
        isDragging = false;
        
        // Restore opacity
        canvasGroup.alpha = 1f;
        
        Debug.Log($"Stopped dragging {gameObject.name}");
    }
}
