/*
Level5BattleUI.cs

Purpose:
- Drive the Level 5 battle Canvas: show whose turn it is, and let the player end a player turn by picking
  one of the three actions (Attack, Defend, Use Item).

Design decisions:
- Listeners are wired once in Awake, never per turn, so repeated turn changes cannot stack duplicates.
- The UI only reads Level5BattleController and Combatant; it never mutates combat state beyond calling
  EndCurrentTurn().
- Actions have effect for ATTACK, DEFEND, USE ITEM and MAGIC. ATTACK asks the controller to resolve
  damage; DEFEND applies Combatant's temporary Defend bonus; USE ITEM heals the acting character via
  Combatant.Heal (Support heals for more than the other three); MAGIC deals the controller's fixed
  MagicDamage straight to the enemy. All four then end the turn the same way. MAGIC and Support's larger
  heal are the only character-specific behaviours so far; Warrior and Tank have none yet.
- MAGIC is only interactable while mageCombatant is the current combatant; ChooseAction also re-checks
  this so the button cannot be triggered for another actor by a stale click.
- Buttons are enabled strictly from Level5BattleController.IsPlayerTurn, so they are non-interactable on
  the Final Enemy's turn (and would be on a dead combatant's turn, which the controller already skips).
- Once Level5BattleController reaches BattleState.Victory or BattleState.Defeat, OnBattleEnded fires
  exactly once: the message banner shows "VICTORY!" or "DEFEAT" and the three action buttons are disabled
  for good. No scene transition, reward, experience, restart, game-over screen or animation is handled here.
- The Final Enemy's attack is resolved entirely by Level5BattleController (via its temporary enemy-turn
  button); this UI only listens to OnEnemyAttack to show the resulting message, the same way it shows the
  player's own attack line.
- Each character slot shows "{Name}\nHP {current} / {max}", read straight from its Combatant every time
  Refresh() runs (after any turn change, attack or battle end), so it can never go stale after damage. No
  HP bar, animation, floating numbers or death UI: the slot text is the only display.

Usage:
- Placed on BattleCanvas in Level5.unity with all references assigned by the SetupLevel5ActionUI tool.
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
    }

    private void OnDisable()
    {
        if (battleController != null)
        {
            battleController.OnTurnChanged -= HandleTurnChanged;
            battleController.OnBattleEnded -= HandleBattleEnded;
            battleController.OnEnemyAttack -= HandleEnemyAttack;
        }
    }

    private void Start()
    {
        Refresh();
    }

    private void ChooseAction(string actionName)
    {
        if (battleController == null || !battleController.IsPlayerTurn || battleEndedMessage != null)
            return;

        Combatant actor = battleController.CurrentCombatant;

        if (actionName == AttackAction)
        {
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
            actor.ApplyDefendBonus(TemporaryDefenseBonus);
            lastActionLine = $"{actor.CombatantName} is defending!";
        }
        else if (actionName == UseItemAction)
        {
            int healAmount = actor == supportCombatant ? SupportHealAmount : HealAmount;
            int healed = actor.Heal(healAmount);
            lastActionLine = healed > 0
                ? $"{actor.CombatantName} uses an item and restores {healed} HP!"
                : $"{actor.CombatantName} is already at full HP.";
        }
        else if (actionName == MagicAction)
        {
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

    private void Refresh()
    {
        if (battleController == null)
            return;

        Combatant current = battleController.CurrentCombatant;
        bool playerTurn = battleController.IsPlayerTurn && battleEndedMessage == null;

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
