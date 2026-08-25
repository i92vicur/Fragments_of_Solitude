/*
SimpleEnemySpriteAnimator.cs

Purpose: Animate enemies by swapping SpriteRenderer frames without requiring an Animator Controller.

Design decisions:
- Separate run, attack and hit arrays are selected from Rigidbody2D velocity, EnemyContactDamage and Health.
- Hit has priority over attack, and each active frame sequence loops at framesPerSecond with immediate state
    changes rather than blended transitions.
- Empty frame arrays are tolerated, allowing the component to be used incrementally during prototyping.
*/

using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(EnemyContactDamage))]
public class SimpleEnemySpriteAnimator : MonoBehaviour
{
    [SerializeField] private Sprite[] runFrames;
    [SerializeField] private Sprite[] attackFrames;
    [SerializeField] private Sprite[] hitFrames;
    [SerializeField] private float framesPerSecond = 12f;
    [SerializeField] private float hitDuration = 0.25f;

    private Rigidbody2D body;
    private SpriteRenderer spriteRenderer;
    private Health health;
    private EnemyContactDamage contactDamage;
    private Sprite[] activeFrames;
    private int frameIndex;
    private float frameTimer;
    private float hitTimer;
    private int lastHealth;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        health = GetComponent<Health>();
        contactDamage = GetComponent<EnemyContactDamage>();
        framesPerSecond = Mathf.Max(1f, framesPerSecond);
        hitDuration = Mathf.Max(0f, hitDuration);
        lastHealth = health.CurrentHealth;
    }

    private void OnEnable()
    {
        health.OnHealthChanged += HandleHealthChanged;
    }

    private void OnDisable()
    {
        health.OnHealthChanged -= HandleHealthChanged;
    }

    private void Update()
    {
        if (Mathf.Abs(body.linearVelocity.x) > 0.01f)
            spriteRenderer.flipX = body.linearVelocity.x < 0f;

        hitTimer = Mathf.Max(0f, hitTimer - Time.deltaTime);
        Sprite[] desiredFrames = hitTimer > 0f ? hitFrames : contactDamage.IsAttacking ? attackFrames : runFrames;
        if (desiredFrames != activeFrames)
        {
            activeFrames = desiredFrames;
            frameIndex = 0;
            frameTimer = 0f;
        }

        if (activeFrames == null || activeFrames.Length == 0)
            return;

        frameTimer += Time.deltaTime;
        float frameDuration = 1f / framesPerSecond;
        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frameIndex = (frameIndex + 1) % activeFrames.Length;
        }

        spriteRenderer.sprite = activeFrames[frameIndex];
    }

    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        if (currentHealth < lastHealth)
            hitTimer = hitDuration;

        lastHealth = currentHealth;
    }
}
