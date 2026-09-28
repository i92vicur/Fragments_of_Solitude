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
    }

    void Update()
    {
        // Read input via the new Input System (Keyboard)
        moveInput = Vector2.zero;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) moveInput.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) moveInput.y -= 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) moveInput.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) moveInput.x += 1f;
        }

        // Normalize diagonal movement so speed is consistent in all directions
        if (moveInput.sqrMagnitude > 1f)
            moveInput = moveInput.normalized;
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
