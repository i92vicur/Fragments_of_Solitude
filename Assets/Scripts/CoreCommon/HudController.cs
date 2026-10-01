/*
HudController.cs

Purpose:
- Controls the hud controller flow for shared UI, scene flow, settings, and cross-scene runtime systems.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class HudController : MonoBehaviour
{
    [Header("Runtime creation options")]
    [Tooltip("If true, the HUD will create a Canvas and UI elements automatically when the scene has none.")]
    public bool createIfMissing = true;

    [Header("Temporary input")]
    [Tooltip("If true, pressing the Pause Toggle key will pause/unpause the game using PauseReason.Debug. Temporary for testing.")]
    public bool enablePauseToggle = true;
    [Tooltip("Key used to toggle the Debug pause reason (temporary).")]
    public KeyCode pauseToggleKey = KeyCode.P;

    [Header("Reward HUD")]
    [SerializeField, Tooltip("How long the temporary healing text remains visible (seconds, unscaled time).")]
    private float healIndicatorDuration = 1.2f;

    [Header("Interaction Hint")]
    [SerializeField, Tooltip("HUD text group shown when the current NPC detection is active. Assign the existing InteractionHintText object here.")]
    private CanvasGroup interactionHintCanvasGroup;
    [SerializeField, Tooltip("Smooth fade speed for the interaction hint.")]
    private float interactionHintFadeSpeed = 8f;
    [SerializeField, Tooltip("Existing dialogue trigger used as the proximity source. Reuses DetectPlayer.inRange instead of adding another detection system.")]
    private DetectPlayer interactionTarget;

    [Header("Health Bar")]
    [SerializeField] private Sprite[] healthBarSprites = new Sprite[5];

    [SerializeField] private Sprite menuFrame;
    [SerializeField] private Sprite upgradeMenuFrame;

    [Header("Font")]
    [SerializeField, Tooltip("Font used by the HUD. Assign Assets/Fonts/PressStart2P-Regular.ttf here for the custom look.")]
    private Font hudFont;

    // References to the player components we observe
    private Health playerHealth;
    private PlayerExperience playerExperience;

    // Root UI objects
    private Canvas hudCanvas;
    private RectTransform rootPanel;

    // UI elements
    private Image healthFillImage;
    private Text healthText;
    private Text levelExpText;
    private Text pauseStateText;
    private Text bossPiecesText;
    private Text megaAttackReadyText;
    private Text healGainText;

    // Boss UI
    private GameObject bossPanel;
    private Image bossFillImage;
    private Text bossText;

    // Level-up UI
    private GameObject levelUpPanel;

    // Pause menu UI
    private const string PersistentPauseButtonName = "PauseButton";
    private Button pauseButton;
    private GameObject pausePanel;
    [SerializeField] private GameObject controlHintPanel;
    private Button levelBtnMove;
    private Button levelBtnAttackSpeed;
    private Button levelBtnProjectile;

    // End-state UI (Victory / Defeat)
    private GameObject endStatePanel;
    private Text endStateTitleText;
    private Button endStateRestartButton;

    // Internal cached values
    private int lastHealth = -1;
    private int lastMaxHealth = -1;
    private float healIndicatorHideAtUnscaled = -1f;

    // LevelUp controller reference
    private LevelUpController levelUpController;
    private PlayerBossAttackPieces playerBossAttackPieces;

    private void OnValidate()
    {
        if (healIndicatorDuration <= 0f)
            healIndicatorDuration = 0.1f;
    }

    private Sprite ResolveMenuFrame()
    {
        if (menuFrame != null)
            return menuFrame;

        #if UNITY_EDITOR
        menuFrame = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Shared/Menus/menu_frame.png");
        #endif

        if (menuFrame == null)
        {
            menuFrame = Resources.Load<Sprite>("menu_frame");
            if (menuFrame == null)
                menuFrame = Resources.Load<Sprite>("Art/menu_frame");
        }

        return menuFrame;
    }

    private Sprite ResolveUpgradeMenuFrame()
    {
        if (upgradeMenuFrame != null)
            return upgradeMenuFrame;

        #if UNITY_EDITOR
        upgradeMenuFrame = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Shared/Menus/upgrade_menu_frame.png");
        #endif

        if (upgradeMenuFrame == null)
        {
            upgradeMenuFrame = Resources.Load<Sprite>("upgrade_menu_frame");
            if (upgradeMenuFrame == null)
                upgradeMenuFrame = Resources.Load<Sprite>("Art/upgrade_menu_frame");
        }

        return upgradeMenuFrame;
    }

    private Font GetHudFont()
    {
        if (hudFont != null)
            return hudFont;

        hudFont = Resources.Load<Font>("Fonts/PressStart2P-Regular");
        if (hudFont != null)
            return hudFont;

        try { hudFont = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch (Exception) { hudFont = Font.CreateDynamicFontFromOSFont("Arial", 14); }
        return hudFont;
    }

    void Awake()
    {
        // Ensure exactly one EventSystem exists so runtime-created buttons are clickable.
        // Use FindObjectsByType to count all instances — Unity 6 may place one in the scene
        // by default, so only create one if none exist at all.
        var allEventSystems = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        if (allEventSystems.Length == 0)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
        }
        // Try to find an existing Canvas in scene
        var existing = UnityEngine.Object.FindAnyObjectByType<Canvas>();
        if (existing == null && createIfMissing)
        {
            try
            {
                CreateCanvasAndElements();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("HudController: failed to create runtime HUD elements: " + ex.Message, this);
            }
        }
        else if (existing != null)
        {
            // Use or attach to the first Canvas found
            hudCanvas = existing;
            // Attempt to find elements if an existing HUD exists (not required)
            TryFindExistingElements();
        }

        if (pauseButton == null)
            pauseButton = TryFindPauseButtonInScene();

        SetControlHintVisible(false);

        if (pauseButton != null)
            WirePauseButton();

        // If an IMGUI HealthBar exists in scene, disable it so HUD is primary
        var imguiHB = UnityEngine.Object.FindAnyObjectByType<HealthBar>();
        if (imguiHB != null)
        {
            imguiHB.enabled = false;
        }

        // Try to auto-setup for Player by tag
        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null)
        {
            SetupForPlayer(playerGO);
        }

        ResolveInteractionTarget();
    }

    void OnEnable()
    {
        if (PauseManager.Instance != null)
            PauseManager.Instance.OnPauseStateChanged += OnPauseStateChanged;
    }

    void OnDisable()
    {
        if (PauseManager.Instance != null)
            PauseManager.Instance.OnPauseStateChanged -= OnPauseStateChanged;

        if (playerHealth != null)
            playerHealth.OnHealthChanged -= OnPlayerHealthChanged;

        if (playerExperience != null)
        {
            playerExperience.OnExperienceChanged -= OnExperienceChanged;
            playerExperience.OnLevelUp -= OnLevelUp;
        }

        if (levelUpController != null)
            levelUpController.OnPendingLevelUpsChanged -= OnPendingLevelUpsChanged;

        if (playerBossAttackPieces != null)
            playerBossAttackPieces.OnPiecesChanged -= OnBossPiecesChanged;
    }

    void Update()
    {
        // Temporary pause toggle using PauseReason.Debug bound to a key (P by default)
        if (enablePauseToggle && Input.GetKeyDown(pauseToggleKey))
            ToggleDebugPause();

        if (healGainText != null && healGainText.gameObject.activeSelf && Time.unscaledTime >= healIndicatorHideAtUnscaled)
        {
            healGainText.gameObject.SetActive(false);
        }

        UpdateInteractionHint();
    }

    private void ResolveInteractionTarget()
    {
        if (interactionTarget != null)
            return;

        var testNpc = GameObject.Find("TestNPC");
        if (testNpc != null)
            interactionTarget = testNpc.GetComponent<DetectPlayer>();
    }

    private void UpdateInteractionHint()
    {
        if (interactionHintCanvasGroup == null)
            return;

        ResolveInteractionTarget();

        bool shouldShow = interactionTarget != null && interactionTarget.inRange;
        float targetAlpha = shouldShow ? 1f : 0f;
        interactionHintCanvasGroup.alpha = Mathf.Lerp(interactionHintCanvasGroup.alpha, targetAlpha, interactionHintFadeSpeed * Time.unscaledDeltaTime);
        interactionHintCanvasGroup.interactable = shouldShow;
        interactionHintCanvasGroup.blocksRaycasts = shouldShow;
    }

    private void ToggleDebugPause()
    {
        if (PauseManager.Instance.IsPausedFor(PauseManager.PauseReason.Debug))
            PauseManager.Instance.Unpause(PauseManager.PauseReason.Debug);
        else if (!PauseManager.Instance.IsPaused)
            PauseManager.Instance.Pause(PauseManager.PauseReason.Debug);
    }

    private void WirePauseButton()
    {
        if (pauseButton == null)
            return;

        pauseButton.onClick.RemoveAllListeners();
        pauseButton.onClick.AddListener(ToggleDebugPause);
        pauseButton.gameObject.SetActive(true);
    }

    /// <summary>
    /// Public API: call this if the Player GameObject wasn't present at Awake or you want to assign a different target.
    /// </summary>
    public void SetupForPlayer(GameObject player)
    {
        if (player == null) return;
        var h = player.GetComponent<Health>();
        var xp = player.GetComponent<PlayerExperience>();
        var pieces = player.GetComponent<PlayerBossAttackPieces>() ?? player.GetComponentInParent<PlayerBossAttackPieces>();

        if (h != null)
        {
            if (playerHealth != null) playerHealth.OnHealthChanged -= OnPlayerHealthChanged;
            playerHealth = h;
            playerHealth.OnHealthChanged += OnPlayerHealthChanged;
            // initialize display
            OnPlayerHealthChanged(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }

        if (xp != null)
        {
            if (playerExperience != null)
            {
                playerExperience.OnExperienceChanged -= OnExperienceChanged;
                playerExperience.OnLevelUp -= OnLevelUp;
            }
            playerExperience = xp;
            playerExperience.OnExperienceChanged += OnExperienceChanged;
            playerExperience.OnLevelUp += OnLevelUp;
            // initialize
            OnExperienceChanged(playerExperience.CurrentExperience, playerExperience.ExperienceToNext);
            OnLevelUp(playerExperience.Level);
        }

        if (pieces != null)
        {
            if (playerBossAttackPieces != null)
                playerBossAttackPieces.OnPiecesChanged -= OnBossPiecesChanged;

            playerBossAttackPieces = pieces;
            playerBossAttackPieces.OnPiecesChanged += OnBossPiecesChanged;
            OnBossPiecesChanged(playerBossAttackPieces.PiecesCollected);
        }

        // Try to hook LevelUpController on the same GameObject for UI-driven selection
        var luc = player.GetComponent<LevelUpController>();
        if (luc != null)
        {
            if (levelUpController != null)
            {
                levelUpController.OnPendingLevelUpsChanged -= OnPendingLevelUpsChanged;
            }
            levelUpController = luc;
            levelUpController.OnPendingLevelUpsChanged += OnPendingLevelUpsChanged;
            // create panel now (hidden) so reference is ready
            EnsureLevelUpPanelExists();
        }
    }

    private void OnPlayerHealthChanged(int current, int max)
    {
        int previousHealth = lastHealth;
        lastHealth = current;
        lastMaxHealth = max;
        UpdateHealthUI(current, max);

        if (previousHealth >= 0 && current > previousHealth)
        {
            ShowHealIndicator(current - previousHealth);
        }
    }

    private void OnExperienceChanged(int current, int required)
    {
        UpdateLevelExpUI(current, required);
    }

    private void OnLevelUp(int newLevel)
    {
        UpdateLevelExpUI(playerExperience != null ? playerExperience.CurrentExperience : 0,
                         playerExperience != null ? playerExperience.ExperienceToNext : 0);
    }

    private void OnPendingLevelUpsChanged(int pending)
    {
        // Show or hide the level-up panel when there are pending level-ups
        if (levelUpPanel == null) EnsureLevelUpPanelExists();

        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(pending > 0);

            // If showing, ensure buttons are wired to the controller
            if (pending > 0 && levelUpController != null)
            {
                WireLevelUpButtons();
            }
        }
    }

    private void OnBossPiecesChanged(int currentPieces)
    {
        int required = PlayerBossAttackPieces.RequiredPiecesForMegaAttack;
        int shownPieces = Mathf.Clamp(currentPieces, 0, required);

        if (bossPiecesText != null)
            bossPiecesText.text = $"Boss Attack Pieces: {shownPieces}/{required}";

        bool isReady = playerBossAttackPieces != null && playerBossAttackPieces.HasMegaAttackAvailable;
        if (megaAttackReadyText != null)
            megaAttackReadyText.gameObject.SetActive(isReady);
    }

    private void ShowHealIndicator(int restoredAmount)
    {
        if (healGainText == null || restoredAmount <= 0) return;

        healGainText.text = $"+{restoredAmount} HP";
        healGainText.gameObject.SetActive(true);
        healIndicatorHideAtUnscaled = Time.unscaledTime + Mathf.Max(0.1f, healIndicatorDuration);
    }

    private void EnsureLevelUpPanelExists()
    {
        if (levelUpPanel != null) return;
        // Creation happens in CreateCanvasAndElements during Awake; if that hasn't run, try to run it
        var existing = GameObject.Find("HUD_Canvas");
        if (existing == null)
        {
            try { CreateCanvasAndElements(); } catch { }
        }

        // After creation, levelUpPanel should be assigned; if still null, bail
        if (levelUpPanel == null) return;

        // Wire buttons if controller already present
        if (levelUpController != null)
            WireLevelUpButtons();
    }

    private void WireLevelUpButtons()
    {
        if (levelUpController == null) return;
        if (levelBtnMove != null)
        {
            levelBtnMove.onClick.RemoveAllListeners();
            levelBtnMove.onClick.AddListener(() => levelUpController.SelectMovementSpeedUpgrade());
        }
        if (levelBtnAttackSpeed != null)
        {
            levelBtnAttackSpeed.onClick.RemoveAllListeners();
            levelBtnAttackSpeed.onClick.AddListener(() => levelUpController.SelectAttackSpeedUpgrade());
        }
        if (levelBtnProjectile != null)
        {
            levelBtnProjectile.onClick.RemoveAllListeners();
            levelBtnProjectile.onClick.AddListener(() => levelUpController.SelectProjectileDamageUpgrade());
        }
    }

    private void OnPauseStateChanged(System.Collections.Generic.HashSet<PauseManager.PauseReason> reasons)
    {
        // Always hide the pause panel first; re-show only if appropriate below
        if (pausePanel != null) pausePanel.SetActive(false);
        if (pauseStateText != null) pauseStateText.gameObject.SetActive(false);
        SetControlHintVisible(false);

        if (reasons == null || reasons.Count == 0)
        {
            if (endStatePanel != null) endStatePanel.SetActive(false);
            return;
        }

        // Victory or Death → end-state panel takes priority
        if (reasons.Contains(PauseManager.PauseReason.Victory) || reasons.Contains(PauseManager.PauseReason.Death))
        {
            if (levelUpPanel != null) levelUpPanel.SetActive(false);

            if (endStatePanel != null)
            {
                bool isVictory = reasons.Contains(PauseManager.PauseReason.Victory);
                endStateTitleText.text = isVictory ? "VICTORY! Run complete" : "OOPS! You Died";
                endStateTitleText.color = isVictory ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.6f, 0.6f);
                endStatePanel.SetActive(true);

                if (endStateRestartButton != null)
                {
                    endStateRestartButton.onClick.RemoveAllListeners();
                    endStateRestartButton.onClick.AddListener(() => {
                        var rc = UnityEngine.Object.FindAnyObjectByType<RestartController>();
                        if (rc != null) rc.RestartNow();
                        else Debug.LogWarning("RestartController not found to restart.");
                    });
                }
            }
            return;
        }

        // Debug (P key) pause → show the pause menu panel
        if (reasons.Contains(PauseManager.PauseReason.Debug))
        {
            if (pausePanel != null) pausePanel.SetActive(true);
            SetControlHintVisible(true);
            return;
        }

        // LevelUp or any other reason → LevelUpController manages its own UI; nothing extra here
    }

    private void SetControlHintVisible(bool visible)
    {
        if (controlHintPanel != null)
            controlHintPanel.SetActive(visible);
    }

    private void UpdateHealthUI(int current, int max)
    {
        if (healthFillImage != null && healthBarSprites != null && healthBarSprites.Length >= 5)
        {
            int damage = Mathf.Max(0, max - current);
            int spriteIndex = Mathf.Clamp(Mathf.CeilToInt(damage / 2f), 0, 4);
            healthFillImage.sprite = healthBarSprites[spriteIndex];
        }

        if (healthText != null)
        {
            healthText.text = $"HP: {current}/{max}";
        }
    }

    private void UpdateLevelExpUI(int currentExp, int expToNext)
    {
        if (levelExpText != null && playerExperience != null)
        {
            levelExpText.text = $"Lv {playerExperience.Level}  EXP: {currentExp}/{expToNext}";
        }
    }

    // --- Boss API ---
    public void ShowBossHealth(int current, int max)
    {
        if (bossPanel == null) return;
        bossPanel.SetActive(true);
        if (bossFillImage != null)
            bossFillImage.fillAmount = (max > 0) ? Mathf.Clamp01(current / (float)max) : 0f;
        if (bossText != null)
            bossText.text = $"BOSS: {current}/{max}";
    }

    public void HideBossHealth()
    {
        if (bossPanel == null) return;
        bossPanel.SetActive(false);
    }

    // --- UI creation helpers ---
    private void CreateCanvasAndElements()
    {
        var go = new GameObject("HUD_Canvas");
        hudCanvas = go.AddComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();

        // Root layer that fills the screen and groups the persistent HUD pieces
        var persistentGO = new GameObject("HUD_Persistent");
        persistentGO.transform.SetParent(go.transform, false);
        rootPanel = persistentGO.AddComponent<RectTransform>();
        rootPanel.anchorMin = new Vector2(0, 0);
        rootPanel.anchorMax = new Vector2(1, 1);
        rootPanel.offsetMin = Vector2.zero;
        rootPanel.offsetMax = Vector2.zero;

        // Separate overlay layer for modal panels that should appear above the HUD
        var overlayGO = new GameObject("HUD_Overlay");
        overlayGO.transform.SetParent(go.transform, false);
        var overlayRT = overlayGO.AddComponent<RectTransform>();
        overlayRT.anchorMin = new Vector2(0, 0);
        overlayRT.anchorMax = new Vector2(1, 1);
        overlayRT.offsetMin = Vector2.zero;
        overlayRT.offsetMax = Vector2.zero;

        // Health bar
        var healthFillGO = CreateUIElement("Health_Fill", persistentGO.transform, new Vector2(10, -10), new Vector2(180, 36), TextAnchor.UpperLeft);
        var fillImage = healthFillGO.AddComponent<Image>();
        fillImage.preserveAspect = true;
        if (healthBarSprites != null && healthBarSprites.Length > 0)
            fillImage.sprite = healthBarSprites[0];
        healthFillImage = fillImage;

        // Health text beside the sprite bar
        var healthTextGO = CreateUIElement("Health_Text", persistentGO.transform, new Vector2(198, -21), new Vector2(100, 24), TextAnchor.UpperLeft);
        var ht = healthTextGO.AddComponent<Text>();
        ht.alignment = TextAnchor.UpperLeft;
        ht.color = Color.white;
        ht.font = GetHudFont();
        ht.fontSize = 10;
        healthText = ht;

        // Level/Exp text (top-left, under health)
        var levelGO = CreateUIElement("LevelExp_Text", persistentGO.transform, new Vector2(25, -50), new Vector2(300, 24), TextAnchor.UpperLeft);
        var lt = levelGO.AddComponent<Text>();
        lt.alignment = TextAnchor.UpperLeft;
        lt.color = Color.white;
        lt.font = GetHudFont();
        lt.fontSize = 10;
        levelExpText = lt;

        // Temporary healing indicator (top-left near health, hidden by default)
        var healGO = CreateUIElement("Heal_Gain_Text", persistentGO.transform, new Vector2(240, -10), new Vector2(140, 24), TextAnchor.UpperLeft);
        var healTxt = healGO.AddComponent<Text>();
        healTxt.alignment = TextAnchor.UpperLeft;
        healTxt.color = new Color(0.6f, 1f, 0.6f, 1f);
        healTxt.font = GetHudFont();
        healTxt.fontSize = 10;
        healTxt.fontStyle = FontStyle.Bold;
        healTxt.gameObject.SetActive(false);
        healGainText = healTxt;

        // Boss attack piece counter (persistent)
        var piecesGO = CreateUIElement("BossPieces_Text", persistentGO.transform, new Vector2(25, -73), new Vector2(320, 24), TextAnchor.UpperLeft);
        var piecesTxt = piecesGO.AddComponent<Text>();
        piecesTxt.alignment = TextAnchor.UpperLeft;
        piecesTxt.color = Color.white;
        piecesTxt.font = GetHudFont();
        piecesTxt.fontSize = 10;
        piecesTxt.text = $"Boss Attack Pieces: 0/{PlayerBossAttackPieces.RequiredPiecesForMegaAttack}";
        bossPiecesText = piecesTxt;

        var readyGO = CreateUIElement("MegaReady_Text", persistentGO.transform, new Vector2(25, -92), new Vector2(320, 24), TextAnchor.UpperLeft);
        var readyTxt = readyGO.AddComponent<Text>();
        readyTxt.alignment = TextAnchor.UpperLeft;
        readyTxt.color = new Color(1f, 0.85f, 0.2f, 1f);
        readyTxt.font = GetHudFont();
        readyTxt.fontSize = 10;
        readyTxt.fontStyle = FontStyle.Bold;
        readyTxt.text = "MEGA ATTACK READY";
        readyTxt.gameObject.SetActive(false);
        megaAttackReadyText = readyTxt;

        // Pause state text (center-top)
        var pauseGO = CreateUIElement("Pause_State", overlayGO.transform, new Vector2(0, -10), new Vector2(600, 24), TextAnchor.UpperCenter);
        var pt = pauseGO.AddComponent<Text>();
        pt.alignment = TextAnchor.UpperCenter;
        pt.color = Color.yellow;
        pt.font = GetHudFont();
        pt.fontSize = 10;
        pt.gameObject.SetActive(false);
        pauseStateText = pt;

        pauseButton = TryFindPauseButtonInScene();
        if (pauseButton == null)
        {
            pauseButton = CreateButton(PersistentPauseButtonName, overlayGO.transform, Vector2.zero, new Vector2(110, 38), "PAUSE");
            var pauseButtonRect = pauseButton.GetComponent<RectTransform>();
            pauseButtonRect.anchorMin = new Vector2(1, 1);
            pauseButtonRect.anchorMax = new Vector2(1, 1);
            pauseButtonRect.pivot = new Vector2(1, 1);
            pauseButtonRect.anchoredPosition = new Vector2(-18, -18);
        }
        else
        {
            var pauseButtonRect = pauseButton.GetComponent<RectTransform>();
            if (pauseButtonRect != null)
            {
                pauseButtonRect.SetParent(overlayGO.transform, false);
                pauseButtonRect.anchorMin = new Vector2(1, 1);
                pauseButtonRect.anchorMax = new Vector2(1, 1);
                pauseButtonRect.pivot = new Vector2(1, 1);
                pauseButtonRect.anchoredPosition = new Vector2(-18, -18);
                pauseButtonRect.sizeDelta = new Vector2(110, 38);
            }
        }
        WirePauseButton();

        // Boss panel (top-center, hidden by default)
        bossPanel = new GameObject("Boss_Panel");
        bossPanel.transform.SetParent(persistentGO.transform, false);
        var bossRT = bossPanel.AddComponent<RectTransform>();
        bossRT.anchorMin = new Vector2(0.5f, 1f);
        bossRT.anchorMax = new Vector2(0.5f, 1f);
        bossRT.pivot = new Vector2(0.5f, 1f);
        bossRT.anchoredPosition = new Vector2(0, -40);
        bossRT.sizeDelta = new Vector2(300, 28);

        var bossBG = new GameObject("Boss_BG");
        bossBG.transform.SetParent(bossPanel.transform, false);
        var bossBGImage = bossBG.AddComponent<Image>();
        bossBGImage.color = new Color(0f, 0f, 0f, 0.6f);
        var bossBGRT = bossBG.GetComponent<RectTransform>();
        bossBGRT.anchorMin = new Vector2(0, 0);
        bossBGRT.anchorMax = new Vector2(1, 1);
        bossBGRT.offsetMin = Vector2.zero;
        bossBGRT.offsetMax = Vector2.zero;

        var bossFillGO2 = new GameObject("Boss_Fill");
        bossFillGO2.transform.SetParent(bossPanel.transform, false);
        bossFillImage = bossFillGO2.AddComponent<Image>();
        bossFillImage.color = new Color(0.8f, 0.2f, 0.2f, 0.9f);
        bossFillImage.type = Image.Type.Filled;
        bossFillImage.fillMethod = Image.FillMethod.Horizontal;
        bossFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        var bossFillRT = bossFillGO2.GetComponent<RectTransform>();
        bossFillRT.anchorMin = new Vector2(0, 0);
        bossFillRT.anchorMax = new Vector2(1, 1);
        bossFillRT.offsetMin = new Vector2(2, 2);
        bossFillRT.offsetMax = new Vector2(-2, -2);

        var bossTextGO = new GameObject("Boss_Text");
        bossTextGO.transform.SetParent(bossPanel.transform, false);
        var btxt = bossTextGO.AddComponent<Text>();
        btxt.alignment = TextAnchor.MiddleCenter;
        btxt.color = Color.white;
        btxt.font = GetHudFont();
        btxt.fontSize = 10;
        bossText = btxt;

        bossPanel.SetActive(false);

        // Level-up panel (center screen, hidden)
        levelUpPanel = new GameObject("LevelUp_Panel");
        levelUpPanel.transform.SetParent(overlayGO.transform, false);

        // End-state panel (center screen, hidden) - Victory/Defeat
        endStatePanel = new GameObject("EndState_Panel");
        endStatePanel.transform.SetParent(overlayGO.transform, false);
        var endRT = endStatePanel.AddComponent<RectTransform>();
        endRT.anchorMin = new Vector2(0.5f, 0.5f);
        endRT.anchorMax = new Vector2(0.5f, 0.5f);
        endRT.pivot = new Vector2(0.5f, 0.5f);
        endRT.anchoredPosition = new Vector2(0, 0);
        endRT.sizeDelta = new Vector2(480, 340);

        var endBG = new GameObject("End_BG");
        endBG.transform.SetParent(endStatePanel.transform, false);
        var endBGImage = endBG.AddComponent<Image>();
        endBGImage.color = new Color(0f, 0f, 0f, 0.85f);
        var endBGRT = endBG.GetComponent<RectTransform>();
        endBGRT.anchorMin = new Vector2(0, 0);
        endBGRT.anchorMax = new Vector2(1, 1);
        endBGRT.offsetMin = Vector2.zero;
        endBGRT.offsetMax = Vector2.zero;

        var endTitleGO = new GameObject("End_Title");
        endTitleGO.transform.SetParent(endStatePanel.transform, false);
        endStateTitleText = endTitleGO.AddComponent<Text>();
        var endTitleRT = endTitleGO.GetComponent<RectTransform>();
        endTitleRT.anchorMin = new Vector2(0.5f, 1f);
        endTitleRT.anchorMax = new Vector2(0.5f, 1f);
        endTitleRT.pivot = new Vector2(0.5f, 1f);
        endTitleRT.anchoredPosition = new Vector2(0, -30);
        endTitleRT.sizeDelta = new Vector2(420, 40);
        endStateTitleText.alignment = TextAnchor.MiddleCenter;
        endStateTitleText.color = Color.white;
        endStateTitleText.font = GetHudFont();
        endStateTitleText.fontSize = 10;

        // Restart button
        endStateRestartButton = CreateButton("End_Restart", endStatePanel.transform, new Vector2(0, -40), new Vector2(220, 48), "Restart");
        endStateRestartButton.onClick.RemoveAllListeners();
        endStateRestartButton.onClick.AddListener(() => {
            var rc = UnityEngine.Object.FindAnyObjectByType<RestartController>();
            if (rc != null) rc.RestartNow();
            else Debug.LogWarning("RestartController not found to restart.");
        });

        // Return to Main Menu button — SceneLoader resets timeScale before loading
        var endMenuButton = CreateButton("End_MainMenu", endStatePanel.transform, new Vector2(0, -110), new Vector2(220, 48), "Return to Main Menu");
        endMenuButton.onClick.AddListener(() => {
            var loader = UnityEngine.Object.FindAnyObjectByType<SceneLoader>();
            if (loader != null)
                loader.LoadMainMenu();
            else
            {
                Time.timeScale = 1f;
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu",
                    UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        });

        endStatePanel.SetActive(false);

        // --- Pause menu panel (center screen, hidden by default) ---
        pausePanel = new GameObject("Pause_Panel");
        pausePanel.transform.SetParent(overlayGO.transform, false);
        var pauseRT = pausePanel.AddComponent<RectTransform>();
        pauseRT.anchorMin = new Vector2(0.5f, 0.5f);
        pauseRT.anchorMax = new Vector2(0.5f, 0.5f);
        pauseRT.pivot = new Vector2(0.5f, 0.5f);
        pauseRT.anchoredPosition = Vector2.zero;
        pauseRT.sizeDelta = new Vector2(420, 330);

        var pauseBG = new GameObject("Pause_BG");
        pauseBG.transform.SetParent(pausePanel.transform, false);
        var pauseBGImage = pauseBG.AddComponent<Image>();
        var resolvedMenuFrame = ResolveMenuFrame();
        pauseBGImage.sprite = resolvedMenuFrame != null ? resolvedMenuFrame : menuFrame;
        pauseBGImage.type = Image.Type.Simple;
        pauseBGImage.color = Color.white;
        var pauseBGRT = pauseBG.GetComponent<RectTransform>();
        pauseBGRT.anchorMin = Vector2.zero;
        pauseBGRT.anchorMax = Vector2.one;
        pauseBGRT.offsetMin = Vector2.zero;
        pauseBGRT.offsetMax = Vector2.zero;

        var pauseTitleGO = new GameObject("Pause_Title");
        pauseTitleGO.transform.SetParent(pausePanel.transform, false);
        var pauseTitleRT = pauseTitleGO.AddComponent<RectTransform>();
        pauseTitleRT.anchorMin = new Vector2(0.5f, 1f);
        pauseTitleRT.anchorMax = new Vector2(0.5f, 1f);
        pauseTitleRT.pivot = new Vector2(0.5f, 1f);
        pauseTitleRT.anchoredPosition = new Vector2(0, -100);
        pauseTitleRT.sizeDelta = new Vector2(280, 36);
        var pauseTitleTxt = pauseTitleGO.AddComponent<Text>();
        pauseTitleTxt.text = "PAUSED";
        pauseTitleTxt.alignment = TextAnchor.MiddleCenter;
        pauseTitleTxt.color = Color.white;
        pauseTitleTxt.fontStyle = FontStyle.Bold;
        pauseTitleTxt.font = GetHudFont();
        pauseTitleTxt.fontSize = 10;

        // Resume button — same as pressing P again
        var resumeBtn = CreateButton("Pause_Resume", pausePanel.transform, new Vector2(0, 0), new Vector2(240, 50), "Resume (P)");
        resumeBtn.onClick.AddListener(() => {
            if (PauseManager.Instance != null)
                PauseManager.Instance.Unpause(PauseManager.PauseReason.Debug);
        });

        // Return to Main Menu button — SceneLoader resets timeScale before loading
        var menuBtn = CreateButton("Pause_MainMenu", pausePanel.transform, new Vector2(0, -70), new Vector2(240, 50), "Return to Main Menu");
        menuBtn.onClick.AddListener(() => {
            var loader = UnityEngine.Object.FindAnyObjectByType<SceneLoader>();
            if (loader != null)
                loader.LoadMainMenu();
            else
            {
                // Fallback: reset timescale and load by name
                Time.timeScale = 1f;
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu",
                    UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        });

        pausePanel.SetActive(false);

        levelUpPanel.SetActive(false);

        // Wire buttons when LevelUpController is present (SetupForPlayer will attach callbacks)
        var lupRT = levelUpPanel.AddComponent<RectTransform>();
        lupRT.anchorMin = new Vector2(0.5f, 0.5f);
        lupRT.anchorMax = new Vector2(0.5f, 0.5f);
        lupRT.pivot = new Vector2(0.5f, 0.5f);
        lupRT.anchoredPosition = new Vector2(0, 0);
        lupRT.sizeDelta = new Vector2(550, 200);

        var lupBG = new GameObject("LevelUp_BG");
        lupBG.transform.SetParent(levelUpPanel.transform, false);
        var lupBGImage = lupBG.AddComponent<Image>();
        var resolvedUpgradeFrame = ResolveUpgradeMenuFrame();
        lupBGImage.sprite = resolvedUpgradeFrame != null ? resolvedUpgradeFrame : menuFrame;
        lupBGImage.type = Image.Type.Simple;
        lupBGImage.color = Color.white;
        var lupBGRT = lupBG.GetComponent<RectTransform>();
        lupBGRT.anchorMin = new Vector2(0, 0);
        lupBGRT.anchorMax = new Vector2(1, 1);
        lupBGRT.offsetMin = Vector2.zero;
        lupBGRT.offsetMax = Vector2.zero;

        // Title
        var titleGO = new GameObject("LevelUp_Title");
        titleGO.transform.SetParent(levelUpPanel.transform, false);
        var titleRT = titleGO.AddComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 1f);
        titleRT.anchorMax = new Vector2(0.5f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0, -50);
        titleRT.sizeDelta = new Vector2(380, 24);
        var titleText = titleGO.AddComponent<Text>();
        titleText.alignment = TextAnchor.UpperCenter;
        titleText.color = Color.white;
        titleText.font = GetHudFont();
        titleText.fontSize = 10;
        titleText.text = "New level Reached!! Choose an Upgrade";

        // Buttons
        levelBtnMove = CreateButton("MoveSpeed", levelUpPanel.transform, new Vector2(-150, -30), new Vector2(140, 40), "Increase Speed");
        levelBtnAttackSpeed = CreateButton("AttackSpeed", levelUpPanel.transform, new Vector2(0, -30), new Vector2(140, 40), "Faster Attack");
        levelBtnProjectile = CreateButton("ProjectileDamage", levelUpPanel.transform, new Vector2(150, -30), new Vector2(140, 40), "Stronger Projectiles");

        levelUpPanel.SetActive(false);

        // Wire buttons when LevelUpController is present (SetupForPlayer will attach callbacks)
    }

    private GameObject CreateUIElement(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, TextAnchor anchor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;
        return go;
    }

    private Button CreateButton(string name, Transform parent, Vector2 anchoredPos, Vector2 size, string label)
    {
        var btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent, false);
        var rt = btnGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var img = btnGO.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);
        var btn = btnGO.AddComponent<Button>();

        // Text child
        var txtGO = new GameObject(name + "_Label");
        txtGO.transform.SetParent(btnGO.transform, false);
        var txtRT = txtGO.AddComponent<RectTransform>();
        txtRT.anchorMin = new Vector2(0, 0);
        txtRT.anchorMax = new Vector2(1, 1);
        txtRT.offsetMin = Vector2.zero;
        txtRT.offsetMax = Vector2.zero;
        var txt = txtGO.AddComponent<Text>();
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.font = GetHudFont();
        txt.fontSize = 10;
        txt.text = label;

        return btn;
    }

    private Button TryFindPauseButtonInScene()
    {
        var pauseButtonGO = GameObject.Find(PersistentPauseButtonName);
        if (pauseButtonGO == null)
            return null;

        var button = pauseButtonGO.GetComponent<Button>();
        if (button != null)
            return button;

        button = pauseButtonGO.GetComponentInChildren<Button>();
        return button;
    }

    private void TryFindExistingElements()
    {
        // Try to find existing named elements under the Canvas for compatibility with editor-setup HUDs.
        try
        {
            var pauseBtn = GameObject.Find(PersistentPauseButtonName);
            if (pauseBtn != null)
                pauseButton = pauseBtn.GetComponent<Button>() ?? pauseBtn.GetComponentInChildren<Button>();

            var healthFill = GameObject.Find("Health_Fill");
            if (healthFill != null)
                healthFillImage = healthFill.GetComponent<Image>();

            var healthTxt = GameObject.Find("Health_Text");
            if (healthTxt != null)
                healthText = healthTxt.GetComponent<Text>();

            var levelTxt = GameObject.Find("LevelExp_Text");
            if (levelTxt != null)
                levelExpText = levelTxt.GetComponent<Text>();

            var pauseTxt = GameObject.Find("Pause_State");
            if (pauseTxt != null)
                pauseStateText = pauseTxt.GetComponent<Text>();

            var piecesTxt = GameObject.Find("BossPieces_Text");
            if (piecesTxt != null)
                bossPiecesText = piecesTxt.GetComponent<Text>();

            var readyTxt = GameObject.Find("MegaReady_Text");
            if (readyTxt != null)
                megaAttackReadyText = readyTxt.GetComponent<Text>();

            var healTxt = GameObject.Find("Heal_Gain_Text");
            if (healTxt != null)
                healGainText = healTxt.GetComponent<Text>();

            var bossP = GameObject.Find("Boss_Panel");
            if (bossP != null)
            {
                bossPanel = bossP;
                var bf = GameObject.Find("Boss_Fill");
                if (bf != null) bossFillImage = bf.GetComponent<Image>();
                var bt = GameObject.Find("Boss_Text");
                if (bt != null) bossText = bt.GetComponent<Text>();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("HudController: error while probing existing UI elements: " + ex.Message);
        }
    }
}
