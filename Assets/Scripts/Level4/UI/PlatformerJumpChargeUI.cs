/*
PlatformerJumpChargeUI.cs

Purpose: Display the current jump charge as a runtime-created uGUI fill bar.

Design decisions:
- The bar reads NormalizedJumpCharge from PlatformerJumpController and is visible only while a grounded
    charge is active, keeping presentation derived from the controller's state.
- Canvas and Image setup is self-contained for prototype scenes; this component does not decide jump force
    or consume input.
*/

using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class PlatformerJumpChargeUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField, Tooltip("Jump controller to read charge state from. If empty, the first platformer jump controller in the scene is used.")]
    private PlatformerJumpController jumpController;

    [Header("Appearance")]
    [SerializeField, Tooltip("Sprite shown when the jump charge is depleted.")]
    private Sprite emptyBarSprite;

    [SerializeField, Tooltip("Sprite progressively revealed as jump charge increases.")]
    private Sprite fullBarSprite;

    [SerializeField, Tooltip("Width of the charge bar in pixels.")]
    private float barWidth = 220f;

    [SerializeField, Tooltip("Distance from the top of the screen in pixels.")]
    private float topOffset = 24f;

    [Header("Label")]
    [SerializeField, Tooltip("Font asset used for the 'Jump Power' label.")]
    private TMP_FontAsset labelFont;

    [SerializeField, Tooltip("Text shown to the left of the charge bar.")]
    private string labelText = "Jump Power";

    [SerializeField, Tooltip("Font size for the label.")]
    private float labelFontSize = 16f;

    [SerializeField, Tooltip("Width reserved for the label, in pixels.")]
    private float labelWidth = 140f;

    [SerializeField, Tooltip("Horizontal gap between the label and the charge bar, in pixels.")]
    private float labelSpacing = 12f;

    private Canvas canvas;
    private RectTransform root;
    private Image fillImage;
    private GameObject rootObject;

    private void Awake()
    {
        if (jumpController == null)
            jumpController = FindAnyObjectByType<PlatformerJumpController>();

        BuildUi();
        SetVisible(true);
    }

    private void LateUpdate()
    {
        if (jumpController == null)
        {
            jumpController = FindAnyObjectByType<PlatformerJumpController>();
            if (jumpController == null)
            {
                SetVisible(true);
                if (fillImage != null)
                    fillImage.fillAmount = 0f;
                return;
            }
        }

        bool isCharging = jumpController.IsGrounded && jumpController.IsCharging;
        SetVisible(true);

        if (isCharging && fillImage != null)
            fillImage.fillAmount = jumpController.NormalizedJumpCharge;
        else if (fillImage != null)
            fillImage.fillAmount = 0f;
    }

    private void BuildUi()
    {
        canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("PlatformerChargeCanvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        rootObject = new GameObject("ChargeBar");
        rootObject.transform.SetParent(canvas.transform, false);

        root = rootObject.AddComponent<RectTransform>();
        root.anchorMin = new Vector2(0.5f, 1f);
        root.anchorMax = new Vector2(0.5f, 1f);
        root.pivot = new Vector2(0.5f, 1f);
        root.sizeDelta = new Vector2(barWidth, barWidth / GetBarAspectRatio());
        root.anchoredPosition = new Vector2(0f, -topOffset);

        GameObject emptyBarObject = new GameObject("EmptyBar");
        emptyBarObject.transform.SetParent(root, false);
        RectTransform emptyBarRect = emptyBarObject.AddComponent<RectTransform>();
        StretchToParent(emptyBarRect);

        Image emptyBarImage = emptyBarObject.AddComponent<Image>();
        emptyBarImage.sprite = emptyBarSprite;
        emptyBarImage.preserveAspect = true;
        emptyBarImage.raycastTarget = false;

        GameObject filledBarObject = new GameObject("FilledBar");
        filledBarObject.transform.SetParent(root, false);
        RectTransform filledBarRect = filledBarObject.AddComponent<RectTransform>();
        StretchToParent(filledBarRect);

        fillImage = filledBarObject.AddComponent<Image>();
        fillImage.sprite = fullBarSprite;
        fillImage.preserveAspect = true;
        fillImage.raycastTarget = false;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = 0f;

        GameObject labelGO = new GameObject("JumpPowerLabel");
        labelGO.transform.SetParent(root, false);
        RectTransform labelRect = labelGO.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(0f, 0.5f);
        labelRect.pivot = new Vector2(1f, 0.5f);
        labelRect.sizeDelta = new Vector2(labelWidth, root.sizeDelta.y);
        labelRect.anchoredPosition = new Vector2(-labelSpacing, 0f);

        TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
        label.font = labelFont;
        label.fontSize = labelFontSize;
        label.alignment = TextAlignmentOptions.MidlineRight;
        label.text = labelText;
        label.raycastTarget = false;
    }

    private float GetBarAspectRatio()
    {
        Sprite referenceSprite = fullBarSprite != null ? fullBarSprite : emptyBarSprite;
        if (referenceSprite == null || referenceSprite.rect.height <= 0f)
            return 4f;

        return referenceSprite.rect.width / referenceSprite.rect.height;
    }

    private static void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private void SetVisible(bool visible)
    {
        if (rootObject != null && rootObject.activeSelf != visible)
            rootObject.SetActive(visible);
    }

}