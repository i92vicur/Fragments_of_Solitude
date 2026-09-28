/*
PuzzleTile.cs

Purpose:
- Controls the puzzle tile flow for the constraint and path-based puzzle systems and their drag-and-drop interactions.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PuzzleTile : MonoBehaviour
{
    [SerializeField] private int tileNumber;
    [SerializeField] private Text numberLabel;
    [SerializeField] private RectTransform rectTransform;

    public int TileNumber => tileNumber;

    private void Awake()
    {
        CacheReferences();
        RefreshVisual();
    }

    private void OnValidate()
    {
        CacheReferences();
        RefreshVisual();
    }

    public void SetNumber(int number)
    {
        tileNumber = number;
        RefreshVisual();
    }

    public void SetAnchoredPosition(Vector2 anchoredPosition)
    {
        CacheReferences();
        if (rectTransform != null)
            rectTransform.anchoredPosition = anchoredPosition;
    }

    private void CacheReferences()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (numberLabel == null)
            numberLabel = GetComponentInChildren<Text>();
    }

    private void RefreshVisual()
    {
        if (numberLabel != null)
            numberLabel.text = tileNumber.ToString();
    }
}
