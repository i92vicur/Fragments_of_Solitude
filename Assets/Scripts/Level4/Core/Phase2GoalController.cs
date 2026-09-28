/*
Phase2GoalController.cs

Purpose:
- Controls the phase2 goal controller flow for the platforming flow, movement, hazards, camera behavior, and phase transitions.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public class Phase2GoalController : MonoBehaviour
{
    [SerializeField, Tooltip("Player transform to monitor. If empty, auto-finds Phase2MovementController.")]
    private Transform player;

    [SerializeField, Tooltip("Collider of the existing goal platform (e.g. Platform_Meta).")]
    private Collider2D goalCollider;

    [SerializeField, Tooltip("Camera scroller to stop once the goal is reached. Auto-found if left empty.")]
    private Phase2CameraScroller cameraScroller;

    [SerializeField, Tooltip("Victory menu shown once the goal is reached. Auto-found if left empty.")]
    private Phase2VictoryMenu victoryMenu;

    [SerializeField, Tooltip("Death controller to disable once the goal is reached. Auto-found if left empty.")]
    private Phase2DeathController deathController;

    [SerializeField, Tooltip("Vertical distance above the goal platform's top at which arrival is already detected, so the goal wins any race against the death check.")]
    private float arrivalTolerance = 2f;

    private Collider2D playerCollider;
    private bool goalReached;

    private void Awake()
    {
        if (player == null)
        {
            Phase2MovementController phase2Controller = FindAnyObjectByType<Phase2MovementController>();
            if (phase2Controller != null)
                player = phase2Controller.transform;
        }

        if (player != null)
            playerCollider = player.GetComponent<Collider2D>();

        if (cameraScroller == null)
            cameraScroller = FindAnyObjectByType<Phase2CameraScroller>();

        if (victoryMenu == null)
            victoryMenu = FindAnyObjectByType<Phase2VictoryMenu>();

        if (deathController == null)
            deathController = FindAnyObjectByType<Phase2DeathController>();
    }

    private void Update()
    {
        if (goalReached || playerCollider == null || goalCollider == null)
            return;

        if (IsPlayerAtGoal())
            HandleGoalReached();
    }

    private bool IsPlayerAtGoal()
    {
        Bounds goalBounds = goalCollider.bounds;
        Bounds playerBounds = playerCollider.bounds;

        if (playerBounds.center.x < goalBounds.min.x || playerBounds.center.x > goalBounds.max.x)
            return false;

        return playerBounds.min.y <= goalBounds.max.y + arrivalTolerance;
    }

    private void HandleGoalReached()
    {
        goalReached = true;

        cameraScroller?.StopDescent();
        if (deathController != null)
            deathController.enabled = false;

        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.MarkLevel4Completed();
        }

        victoryMenu?.Show();
    }
}
