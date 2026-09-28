/*
MainMenuHoverPreview.cs

Purpose:
- Controls the main menu hover preview flow for shared UI, scene flow, settings, and cross-scene runtime systems.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MainMenuHoverPreview : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image previewImage;
    [SerializeField] private RectTransform previewRect;
    [SerializeField] private Sprite[] frames = Array.Empty<Sprite>();
    [SerializeField] private float frameRate = 12f;
    [SerializeField] private bool isHovering;
    [SerializeField] private int frameIndex;
    [SerializeField] private float frameTimer;

    private void Awake()
    {
        EnsurePreviewObject();
        LoadFramesForButton();
    }

    private void EnsurePreviewObject()
    {
        if (previewRect == null)
        {
            Transform existingPreview = transform.Find("Preview");
            if (existingPreview != null)
            {
                previewRect = existingPreview as RectTransform;
                previewImage = previewRect != null ? previewRect.GetComponent<Image>() : null;
            }
        }

        if (previewRect == null)
        {
            GameObject previewGO = new GameObject("Preview", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            previewGO.transform.SetParent(transform, false);
            previewRect = previewGO.GetComponent<RectTransform>();
            previewImage = previewGO.GetComponent<Image>();
        }

        if (previewRect != null)
        {
            previewRect.anchorMin = new Vector2(0.5f, 0f);
            previewRect.anchorMax = new Vector2(0.5f, 0f);
            previewRect.pivot = new Vector2(0.5f, 0f);
            previewRect.sizeDelta = new Vector2(90f, 90f);
            previewRect.anchoredPosition = new Vector2(0f, -118f);
        }

        if (previewImage != null)
        {
            previewImage.raycastTarget = false;
            previewImage.preserveAspect = true;
            previewImage.color = Color.white;
        }

        if (previewRect != null)
            previewRect.gameObject.SetActive(false);
    }

    private void LoadFramesForButton()
    {
        bool hasValidFrames = frames != null && frames.Length > 0 && frames.All(sprite => sprite != null);

        if (hasValidFrames)
        {
            if (previewImage != null)
                previewImage.sprite = frames[0];

            if (previewRect != null)
                previewRect.gameObject.SetActive(false);
            return;
        }

        string assetPath = GetSpriteAssetPathForButton();
        if (string.IsNullOrEmpty(assetPath))
            return;

#if UNITY_EDITOR
        frames = LoadFramesFromAssetPath(assetPath);
        if (frames.Length > 0 && previewImage != null)
            previewImage.sprite = frames[0];
#endif

        if (previewRect != null)
            previewRect.gameObject.SetActive(false);
    }

    private string GetSpriteAssetPathForButton()
    {
        if (gameObject.name.Contains("Level1", StringComparison.OrdinalIgnoreCase) || 
            gameObject.name.Contains("PuzzleMenu", StringComparison.OrdinalIgnoreCase))
            return "Assets/Art/Shared/Menus/Menu_animation.png";

        if (gameObject.name.Contains("Level2", StringComparison.OrdinalIgnoreCase) ||
            gameObject.name.Contains("Level3", StringComparison.OrdinalIgnoreCase))
            return "Assets/Art/Characters/Level2/Player/Animations/Run/Run_while_shooting_Left.png";

        if (gameObject.name.Contains("Level4", StringComparison.OrdinalIgnoreCase))
            return "Assets/Art/Characters/Level4/Player/Animations/Dash/Dash_right_Down.png";

        if (gameObject.name.Contains("Level5", StringComparison.OrdinalIgnoreCase))
            return "Assets/Art/Shared/Menus/main_menu_chest.png";

        return string.Empty;
    }

    private Sprite[] LoadFramesFromAssetPath(string assetPath)
    {
#if UNITY_EDITOR
        Sprite[] sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath)
            .OfType<Sprite>()
            .ToArray();

        return sprites ?? Array.Empty<Sprite>();
#else
        return Array.Empty<Sprite>();
#endif
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (frames == null || frames.Length == 0)
            return;

        isHovering = true;
        frameIndex = 0;
        frameTimer = 0f;
        if (previewRect != null)
            previewRect.gameObject.SetActive(true);
        SetCurrentFrame();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        if (previewRect != null)
            previewRect.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!isHovering || frames == null || frames.Length <= 1)
            return;

        frameTimer += Time.unscaledDeltaTime;
        float frameDuration = 1f / frameRate;

        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frameIndex = (frameIndex + 1) % frames.Length;
        }

        SetCurrentFrame();
    }

    private void SetCurrentFrame()
    {
        if (previewImage == null || frames == null || frames.Length == 0)
            return;

        previewImage.sprite = frames[frameIndex];
    }
}
