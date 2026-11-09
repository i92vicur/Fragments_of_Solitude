/*
PlayerMovement.cs

Purpose:
- Controls the player movement flow for the roguelite combat loop, enemy behaviors, progression, and procedural map generation.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField, Tooltip("Movement speed in units per second.")]
    private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 moveInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Ensure the Rigidbody2D does not rotate during physics
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        EnsureGameplayInput();
    }

    void Update()
    {
        // Movement from GameInput: keyboard (WASD and arrows) and the gamepad left stick
        moveInput = GameInput.Move;

        // Normalize diagonal movement so speed is consistent in all directions
        if (moveInput.sqrMagnitude > 1f)
            moveInput = moveInput.normalized;
    }

    // Initializes GameInput once with the project's actions asset and enables the gameplay map.
    private static void EnsureGameplayInput()
    {
        if (!GameInput.IsInitialized)
        {
            GameInput.Initialize(InputSystem.actions);
        }

        GameInput.SetContext(GameInputContext.Gameplay);
    }

    void FixedUpdate()
    {
        // Apply movement through Rigidbody2D physics (velocity)
        rb.linearVelocity = moveInput * moveSpeed;
    }

    /// <summary>
    /// Increase the player's movement speed by amount (can be positive). Clamped to a reasonable maximum.
    /// </summary>
    public void IncreaseMoveSpeed(float amount)
    {
        if (amount <= 0f) return;
        const float maxSpeed = 20f;
        moveSpeed = Mathf.Min(moveSpeed + amount, maxSpeed);
    }
}
