/*
ConstraintPuzzleSpriteSkin.cs

Purpose:
- Controls the constraint puzzle sprite skin flow for the constraint and path-based puzzle systems and their drag-and-drop interactions.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[ExecuteAlways]
public sealed class ConstraintPuzzleSpriteSkin : MonoBehaviour
{
    [SerializeField] private Sprite[] letterSprites = new Sprite[6];
    [SerializeField] private Sprite[] slotSprites = new Sprite[6];
    [SerializeField] private bool showLetterLabels = true;
    [SerializeField] private bool showSlotLabels = true;

    private void OnEnable()
    {
        RefreshVisuals();
    }

    private void OnValidate()
    {
        RefreshVisuals();
    }

    public void RefreshVisuals()
    {
        if (!TryResolveAreas(out Transform lettersArea, out Transform slotsArea))
            return;

        ApplySprites(lettersArea, "Letter_", letterSprites, showLetterLabels, "Text");
        ApplySprites(slotsArea, "Slot_", slotSprites, showSlotLabels, "Number");
    }

    private bool TryResolveAreas(out Transform lettersArea, out Transform slotsArea)
    {
        lettersArea = null;
        slotsArea = null;

        Transform puzzleRoot = transform;
        if (puzzleRoot.name != "PuzzleRoot")
        {
            Transform directPuzzleRoot = transform.Find("PuzzleRoot");
            if (directPuzzleRoot != null)
                puzzleRoot = directPuzzleRoot;
        }

        lettersArea = puzzleRoot.Find("LettersArea");
        slotsArea = puzzleRoot.Find("SlotsArea");

        if (lettersArea == null || slotsArea == null)
        {
            Debug.LogWarning("ConstraintPuzzleSpriteSkin: LettersArea or SlotsArea not found under PuzzleRoot.");
            return false;
        }

        return true;
    }

    private static void ApplySprites(Transform area, string prefix, Sprite[] sprites, bool showLabels, string labelName)
    {
        for (int i = 0; i < sprites.Length; i++)
        {
            Transform target = area.Find($"{prefix}{i + 1}");
            if (target == null)
                continue;

            Image overlay = EnsureOverlay(target);
            Sprite sprite = sprites[i];
            overlay.enabled = sprite != null;
            overlay.sprite = sprite;
            overlay.raycastTarget = false;

            Transform label = target.Find(labelName);
            if (label != null)
                label.gameObject.SetActive(showLabels);
        }
    }

    private static Image EnsureOverlay(Transform target)
    {
        Transform overlayTransform = target.Find("SpriteOverlay");
        if (overlayTransform == null)
        {
            GameObject overlayGO = new GameObject("SpriteOverlay", typeof(RectTransform), typeof(Image));
            overlayGO.transform.SetParent(target, false);
            overlayTransform = overlayGO.transform;
            overlayTransform.SetSiblingIndex(0);
        }

        RectTransform overlayRect = overlayTransform.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayImage = overlayTransform.GetComponent<Image>();
        overlayImage.color = Color.white;
        overlayImage.preserveAspect = true;
        overlayImage.raycastTarget = false;

        return overlayImage;
    }
}