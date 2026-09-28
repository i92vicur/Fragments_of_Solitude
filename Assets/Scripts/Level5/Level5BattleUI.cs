/*
Level5BattleUI.cs

Purpose:
- Controls the level5 battle u i flow for the turn-based battle loop, combatants, battle UI, and encounter rules.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class Level5BattleUI : MonoBehaviour
{
    [SerializeField] private Level5BattleController battleController;

    [Header("Action buttons")]
    [SerializeField] private Button attackButton;
    [SerializeField] private Button defendButton;
    [SerializeField] private Button itemButton;
    [SerializeField, Tooltip("Mage-only special attack. Only interactable during the Mage's own turn.")]
    private Button magicButton;

    [Header("Text areas")]
    [SerializeField, Tooltip("Combat message banner.")]
    private Text combatMessageText;

    [SerializeField, Tooltip("Enemy info placeholder text.")]
    private Text enemyInfoText;

    [Header("Character slots (one per party member, in party order)")]
    [SerializeField] private Combatant[] partyMembers = new Combatant[0];
    [SerializeField] private Text[] characterSlotTexts = new Text[0];
    [SerializeField] private Image[] characterSlotBackgrounds = new Image[0];

    [SerializeField, Tooltip("Enemy combatant, used to fill the enemy info placeholder.")]
    private Combatant enemyCombatant;

    [SerializeField, Tooltip("The Mage combatant, used to gate the MAGIC button to only their own turn.")]
    private Combatant mageCombatant;

    [SerializeField, Tooltip("The Support combatant, used to give USE ITEM a larger heal for them only.")]
    private Combatant supportCombatant;

    [SerializeField, Tooltip("Background colour of the slot belonging to the acting character.")]
    private Color activeSlotColor = new Color(0.30f, 0.45f, 0.75f, 0.95f);

    [SerializeField, Tooltip("Background colour of the slots that are not acting.")]
    private Color inactiveSlotColor = new Color(0.15f, 0.15f, 0.20f, 0.85f);

    private string lastActionLine = string.Empty;
    private string battleEndedMessage = null;

    private const string AttackAction = "Attack";
    private const string DefendAction = "Defend";
    private const string UseItemAction = "Use Item";
    private const string MagicAction = "Magic";
    private const int TemporaryDefenseBonus = 10;
    private const int HealAmount = 30;
    private const int SupportHealAmount = 45;

    private void Awake()
    {
        if (battleController == null)
            battleController = FindAnyObjectByType<Level5BattleController>();

        if (attackButton != null) attackButton.onClick.AddListener(() => ChooseAction(AttackAction));
        if (defendButton != null) defendButton.onClick.AddListener(() => ChooseAction(DefendAction));
        if (itemButton != null) itemButton.onClick.AddListener(() => ChooseAction(UseItemAction));
        if (magicButton != null) magicButton.onClick.AddListener(() => ChooseAction(MagicAction));
    }

    private void OnEnable()
    {
        if (battleController != null)
        {
            battleController.OnTurnChanged += HandleTurnChanged;
            battleController.OnBattleEnded += HandleBattleEnded;
            battleController.OnEnemyAttack += HandleEnemyAttack;
        }

        PauseManager.Instance.OnPauseStateChanged += HandlePauseStateChanged;
    }

    private void OnDisable()
    {
        if (battleController != null)
        {
            battleController.OnTurnChanged -= HandleTurnChanged;
            battleController.OnBattleEnded -= HandleBattleEnded;
            battleController.OnEnemyAttack -= HandleEnemyAttack;
        }

        PauseManager.Instance.OnPauseStateChanged -= HandlePauseStateChanged;
    }

    private void Start()
    {
        Refresh();
    }

    private void ChooseAction(string actionName)
    {
        if (battleController == null || !battleController.IsPlayerTurn || battleEndedMessage != null || PauseManager.Instance.IsPaused)
            return;

        Combatant actor = battleController.CurrentCombatant;

        if (actionName == AttackAction)
        {
            if (battleController.IsLevel0BossTurnModeActive)
            {
                lastActionLine = "The attack has no effect.";
                Debug.Log($"[Level5Battle] {lastActionLine}", actor);
                battleController.EndCurrentTurn();
                return;
            }

            Combatant target = battleController.EnemyCombatant;
            if (target == null)
            {
                Debug.LogWarning("[Level5Battle] No enemy combatant to attack.", this);
                return;
            }

            int damageDealt = battleController.PerformAttack(actor, target, out bool missed, out bool critical);
            lastActionLine = missed
                ? $"{actor.CombatantName} attacks {target.CombatantName} but misses!"
                : critical
                    ? $"{actor.CombatantName} lands a CRITICAL HIT for {damageDealt} damage!"
                    : $"{actor.CombatantName} attacks {target.CombatantName} for {damageDealt} damage!";

            // Refresh before ending the turn so the number reflects the hit even if turn advance is delayed later.
            RefreshEnemyInfo();
        }
        else if (actionName == DefendAction)
        {
            if (battleController.IsLevel0BossTurnModeActive)
            {
                lastActionLine = "The action has no effect.";
                Debug.Log($"[Level5Battle] {lastActionLine}", actor);
                battleController.EndCurrentTurn();
                return;
            }

            actor.ApplyDefendBonus(TemporaryDefenseBonus);
            lastActionLine = $"{actor.CombatantName} is defending!";
        }
        else if (actionName == UseItemAction)
        {
            if (battleController.IsLevel0BossTurnModeActive)
            {
                lastActionLine = "The action has no effect.";
                Debug.Log($"[Level5Battle] {lastActionLine}", actor);
                battleController.EndCurrentTurn();
                return;
            }

            int healAmount = actor == supportCombatant ? SupportHealAmount : HealAmount;
            int healed = actor.Heal(healAmount);
            lastActionLine = healed > 0
                ? $"{actor.CombatantName} uses an item and restores {healed} HP!"
                : $"{actor.CombatantName} is already at full HP.";
        }
        else if (actionName == MagicAction)
        {
            if (battleController.IsLevel0BossTurnModeActive)
            {
                lastActionLine = "The magic has no effect.";
                Debug.Log($"[Level5Battle] {lastActionLine}", actor);
                battleController.EndCurrentTurn();
                return;
            }

            if (actor != mageCombatant)
                return;

            Combatant target = battleController.EnemyCombatant;
            if (target == null)
            {
                Debug.LogWarning("[Level5Battle] No enemy combatant for MAGIC to hit.", this);
                return;
            }

            int damageDealt = battleController.PerformMagicAttack(actor, target, out bool magicMissed, out bool magicCritical);
            lastActionLine = magicMissed
                ? $"{actor.CombatantName} casts MAGIC but misses!"
                : magicCritical
                    ? $"{actor.CombatantName} lands a CRITICAL MAGIC HIT for {damageDealt} damage!"
                    : $"{actor.CombatantName} casts MAGIC for {damageDealt} damage!";

            RefreshEnemyInfo();
        }
        else
        {
            lastActionLine = $"{actor.CombatantName} chooses {actionName}";
        }

        Debug.Log($"[Level5Battle] {lastActionLine}", actor);

        battleController.EndCurrentTurn();
    }

    private void HandleTurnChanged(Combatant combatant)
    {
        Refresh();
    }

    private void HandleBattleEnded(string message)
    {
        battleEndedMessage = message;
        Refresh();
    }

    private void HandleEnemyAttack(string message)
    {
        lastActionLine = message;
        Refresh();
    }

    private void HandlePauseStateChanged(System.Collections.Generic.HashSet<PauseManager.PauseReason> activeReasons)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (battleController == null)
            return;

        Combatant current = battleController.CurrentCombatant;
        bool playerTurn = battleController.IsPlayerTurn && battleEndedMessage == null && !PauseManager.Instance.IsPaused;

        SetButtonsInteractable(playerTurn);
        if (magicButton != null) magicButton.interactable = playerTurn && current == mageCombatant;

        if (combatMessageText != null)
        {
            if (battleEndedMessage != null)
            {
                combatMessageText.text = battleEndedMessage;
            }
            else if (current != null)
            {
                string turnLine = $"{current.CombatantName}'s turn";
                combatMessageText.text = string.IsNullOrEmpty(lastActionLine)
                    ? turnLine
                    : $"{lastActionLine}\n{turnLine}";
            }
        }

        HighlightActiveSlot(current);
        RefreshEnemyInfo();
        RefreshCharacterSlots();
    }

    private void RefreshEnemyInfo()
    {
        if (enemyInfoText == null || enemyCombatant == null)
            return;

        enemyInfoText.text = $"{enemyCombatant.CombatantName}\nHP: {enemyCombatant.CurrentHP} / {enemyCombatant.MaxHP}";
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (attackButton != null) attackButton.interactable = interactable;
        if (defendButton != null) defendButton.interactable = interactable;
        if (itemButton != null) itemButton.interactable = interactable;
    }

    private void HighlightActiveSlot(Combatant current)
    {
        for (int i = 0; i < characterSlotBackgrounds.Length; i++)
        {
            if (characterSlotBackgrounds[i] == null)
                continue;

            bool isActive = current != null && i < partyMembers.Length && partyMembers[i] == current;
            characterSlotBackgrounds[i].color = isActive ? activeSlotColor : inactiveSlotColor;
        }
    }

    private void RefreshCharacterSlots()
    {
        for (int i = 0; i < characterSlotTexts.Length; i++)
        {
            if (characterSlotTexts[i] == null || i >= partyMembers.Length || partyMembers[i] == null)
                continue;

            Combatant member = partyMembers[i];
            characterSlotTexts[i].text = $"{member.CombatantName}\nHP {member.CurrentHP} / {member.MaxHP}";
        }
    }
}
