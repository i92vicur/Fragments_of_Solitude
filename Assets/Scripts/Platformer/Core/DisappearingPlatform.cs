using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class DisappearingPlatform : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField, Tooltip("Time in seconds after trigger before the platform disappears.")]
    private float timeBeforeDisappearing = 0.8f;

    [SerializeField, Tooltip("Time in seconds before the platform reappears after disappearing.")]
    private float timeBeforeReappearing = 1.5f;

    [SerializeField, Tooltip("Optional warning window at the end of the countdown. Set to 0 to disable.")]
    private float warningDuration = 0.2f;

    [SerializeField, Tooltip("If true, trigger only when the player lands from above. If false, any contact can trigger.")]
    private bool triggerOnLanding = true;

    [Header("Debug Visual")]
    [SerializeField, Tooltip("Tint used during the warning phase before disappearing.")]
    private Color warningColor = new Color(1f, 0.75f, 0.35f, 1f);

    [SerializeField, Tooltip("Tint used while the platform is disappeared/non-solid.")]
    private Color hiddenColor = new Color(1f, 1f, 1f, 0.2f);

    private enum State
    {
        SolidIdle,
        Countdown,
        Hidden
    }

    private Collider2D platformCollider;
    private SpriteRenderer spriteRenderer;
    private Color defaultColor;
    private State currentState;
    private float stateTimer;
    private PlatformerPlayerController playerController;
    private Collider2D playerCollider;
    private Rigidbody2D playerBody;

    private void Awake()
    {
        platformCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        defaultColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        ResolvePlayerReferences();

        timeBeforeDisappearing = Mathf.Max(0f, timeBeforeDisappearing);
        timeBeforeReappearing = Mathf.Max(0f, timeBeforeReappearing);
        warningDuration = Mathf.Max(0f, warningDuration);

        SetSolidState();
    }

    private void OnEnable()
    {
        SetSolidState();
    }

    private void OnValidate()
    {
        timeBeforeDisappearing = Mathf.Max(0f, timeBeforeDisappearing);
        timeBeforeReappearing = Mathf.Max(0f, timeBeforeReappearing);
        warningDuration = Mathf.Max(0f, warningDuration);
    }

    private void Update()
    {
        switch (currentState)
        {
            case State.Countdown:
                stateTimer -= Time.deltaTime;
                UpdateWarningVisual();
                if (stateTimer <= 0f)
                    EnterHiddenState();
                break;
            case State.Hidden:
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                    SetSolidState();
                break;
            case State.SolidIdle:
                if (ShouldTriggerFromTouching())
                {
                    currentState = State.Countdown;
                    stateTimer = timeBeforeDisappearing;
                    UpdateWarningVisual();
                }
                break;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryTrigger(collision, true);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!triggerOnLanding)
            TryTrigger(collision, false);
    }

    private void TryTrigger(Collision2D collision, bool isEnter)
    {
        if (currentState != State.SolidIdle)
            return;

        if (!IsPlayerCollision(collision))
            return;

        if (triggerOnLanding)
        {
            if (!isEnter)
                return;

            if (!HasTopContact(collision))
                return;
        }

        currentState = State.Countdown;
        stateTimer = timeBeforeDisappearing;
        UpdateWarningVisual();
    }

    private bool IsPlayerCollision(Collision2D collision)
    {
        if (collision.gameObject != null && collision.gameObject.CompareTag("Player"))
            return true;

        if (collision.transform != null && collision.transform.GetComponent<PlatformerPlayerController>() != null)
            return true;

        return IsPlayerCollider(collision.collider)
            || IsPlayerCollider(collision.otherCollider)
            || (collision.rigidbody != null && collision.rigidbody.GetComponent<PlatformerPlayerController>() != null);
    }

    private bool HasTopContact(Collision2D collision)
    {
        if (platformCollider == null)
            return false;

        float platformTop = platformCollider.bounds.max.y - 0.06f;
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D contact = collision.GetContact(i);
            if (contact.point.y >= platformTop)
                return true;
        }

        Rigidbody2D otherBody = collision.rigidbody;
        if (otherBody != null && otherBody.worldCenterOfMass.y >= platformCollider.bounds.center.y)
            return true;

        return false;
    }

    private static bool IsPlayerCollider(Collider2D other)
    {
        if (other == null)
            return false;

        if (other.CompareTag("Player"))
            return true;

        return other.GetComponent<PlatformerPlayerController>() != null;
    }

    private bool ShouldTriggerFromTouching()
    {
        if (platformCollider == null)
            return false;

        ResolvePlayerReferences();
        if (playerCollider == null || !playerCollider.isActiveAndEnabled)
            return false;

        if (!IsPlayerOnPlatformTop())
            return false;

        if (!triggerOnLanding)
            return true;

        return playerBody == null || playerBody.linearVelocity.y <= 0.05f;
    }

    private void ResolvePlayerReferences()
    {
        if (playerController == null)
            playerController = FindAnyObjectByType<PlatformerPlayerController>();

        if (playerController == null)
        {
            playerCollider = null;
            playerBody = null;
            return;
        }

        if (playerCollider == null)
            playerCollider = playerController.GetComponent<Collider2D>();

        if (playerBody == null)
            playerBody = playerController.GetComponent<Rigidbody2D>();
    }

    private bool IsPlayerOnPlatformTop()
    {
        Bounds platformBounds = platformCollider.bounds;
        Bounds playerBounds = playerCollider.bounds;

        float platformMinX = platformBounds.min.x;
        float platformMaxX = platformBounds.max.x;
        float playerCenterX = playerBounds.center.x;

        if (playerCenterX < platformMinX || playerCenterX > platformMaxX)
            return false;

        float platformTop = platformBounds.max.y;
        float playerFeet = playerBounds.min.y;
        float verticalDistance = Mathf.Abs(playerFeet - platformTop);

        return verticalDistance <= 0.18f;
    }

    private void EnterHiddenState()
    {
        currentState = State.Hidden;
        stateTimer = timeBeforeReappearing;

        if (platformCollider != null)
            platformCollider.enabled = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = hiddenColor;
            spriteRenderer.enabled = false;
        }
    }

    private void SetSolidState()
    {
        currentState = State.SolidIdle;
        stateTimer = 0f;

        if (platformCollider != null)
            platformCollider.enabled = true;

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = defaultColor;
        }
    }

    private void UpdateWarningVisual()
    {
        if (spriteRenderer == null)
            return;

        if (warningDuration <= 0f || stateTimer > warningDuration)
        {
            spriteRenderer.color = defaultColor;
            return;
        }

        float t = warningDuration <= 0f ? 1f : Mathf.Clamp01(1f - (stateTimer / warningDuration));
        spriteRenderer.color = Color.Lerp(defaultColor, warningColor, t);
    }
}
