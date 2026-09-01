using UnityEngine;
using UnityEngine.UI;
/*
PlatformerDashDebugUI.cs

Purpose: Show temporary text indicating whether the player's single air dash is ready or already used.

Design decisions:
- The overlay is created at runtime and listens to PlatformerJumpController.DashAvailabilityChanged, so it
    does not poll gameplay state for its main updates.
- This is diagnostic UI for the prototype, not the final platformer HUD; it should be replaceable without
    changing the jump controller contract.
*/

using TMPro;

[DisallowMultipleComponent]
public class PlatformerDashDebugUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField, Tooltip("Jump controller to read dash state from. If empty, the first platformer jump controller in the scene is used.")]
    private PlatformerJumpController jumpController;

    [SerializeField, Tooltip("Font asset used for the dash debug label.")]
    private TMP_FontAsset labelFont;

    [Header("Appearance")]
    [SerializeField, Tooltip("Temporary debug text shown when dash is available.")]
    private string dashReadyText = "DASH READY";

    [SerializeField, Tooltip("Temporary debug text shown after dash has been consumed.")]
    private string dashUsedText = "DASH USED";

    [SerializeField, Tooltip("Font size for the temporary debug label.")]
    private int fontSize = 24;

    [SerializeField, Tooltip("Anchored position of the label box relative to the top-center of the screen, in pixels.")]
    private Vector2 anchoredOffset = new Vector2(0f, -52f);

    [SerializeField, Tooltip("Background color for the temporary debug label.")]
    private Color backgroundColor = new Color(0f, 0f, 0f, 0.72f);

    [SerializeField, Tooltip("Text color for the temporary debug label.")]
    private Color textColor = new Color(1f, 1f, 1f, 1f);

    private Canvas canvas;
    private GameObject rootObject;
    private TextMeshProUGUI label;
    private bool lastDashAvailable;
    private bool hasDashState;

    private void Awake()
    {
        if (jumpController == null)
            jumpController = FindAnyObjectByType<PlatformerJumpController>();

        BuildUi();
        SetDashLabel(true);
    }

    private void OnEnable()
    {
        HookJumpController();
        RefreshFromController(true);
    }

    private void Start()
    {
        RefreshFromController(true);
    }

    private void OnDisable()
    {
        UnhookJumpController();
    }

    private void OnDestroy()
    {
        UnhookJumpController();
    }

    private void HandleDashAvailabilityChanged(bool dashAvailable)
    {
        hasDashState = true;
        lastDashAvailable = dashAvailable;
        SetDashLabel(dashAvailable);
    }

    private void BuildUi()
    {
        canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("PlatformerDashDebugCanvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 101;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        rootObject = new GameObject("TempDashDebugLabel");
        rootObject.transform.SetParent(canvas.transform, false);

        RectTransform root = rootObject.AddComponent<RectTransform>();
        root.anchorMin = new Vector2(0.5f, 1f);
        root.anchorMax = new Vector2(0.5f, 1f);
        root.pivot = new Vector2(0.5f, 1f);
        root.sizeDelta = new Vector2(360f, 44f);
        root.anchoredPosition = anchoredOffset;

        GameObject backgroundGO = new GameObject("Background");
        backgroundGO.transform.SetParent(root, false);
        RectTransform backgroundRect = backgroundGO.AddComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        Image backgroundImage = backgroundGO.AddComponent<Image>();
        backgroundImage.sprite = GetWhiteSprite();
        backgroundImage.color = backgroundColor;

        GameObject textGO = new GameObject("LabelText");
        textGO.transform.SetParent(root, false);
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        label = textGO.AddComponent<TextMeshProUGUI>();
        label.font = labelFont;
        label.fontSize = Mathf.Max(fontSize, 28);
        label.alignment = TextAlignmentOptions.Center;
        label.color = textColor;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        label.text = "TEMP DASH DEBUG";
    }

    private void HookJumpController()
    {
        if (jumpController == null)
            return;

        jumpController.DashAvailabilityChanged -= HandleDashAvailabilityChanged;
        jumpController.DashAvailabilityChanged += HandleDashAvailabilityChanged;
    }

    private void UnhookJumpController()
    {
        if (jumpController == null)
            return;

        jumpController.DashAvailabilityChanged -= HandleDashAvailabilityChanged;
    }

    private void RefreshFromController(bool forceUpdate)
    {
        if (jumpController == null)
        {
            jumpController = FindAnyObjectByType<PlatformerJumpController>();
            if (jumpController == null)
            {
                SetDashLabel(true);
                return;
            }

            HookJumpController();
        }

        bool dashAvailable = jumpController.IsDashAvailableThisJump;
        if (!hasDashState || forceUpdate || dashAvailable != lastDashAvailable)
        {
            hasDashState = true;
            lastDashAvailable = dashAvailable;
            SetDashLabel(dashAvailable);
        }
    }

    private void SetDashLabel(bool dashAvailable)
    {
        if (label == null)
            return;

        if (rootObject != null && !rootObject.activeSelf)
            rootObject.SetActive(true);

        label.text = dashAvailable ? dashReadyText : dashUsedText;
    }

    private static Sprite GetWhiteSprite()
    {
        Texture2D texture = Texture2D.whiteTexture;
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }
}
