/*
PlayerMovement.cs

Purpose: Handle player character locomotion in a top-down arena using keyboard input and Rigidbody2D velocity.

Design decisions:
- Input system: Uses Unity's new Input System (KeyCode-based) to read WASD or arrow keys in Update.
- Physics: Applies velocity directly to a Rigidbody2D in FixedUpdate to keep movement deterministic and framerate-independent.
- Movement style: Continuous 8-directional movement (cardinal + diagonals) at a fixed speed with no acceleration/deceleration.
- Rotation: Freezes Rigidbody2D rotation to prevent camera spin and physics-induced tumbling in a top-down game.

Functionality:
- Reads keyboard input (WASD or arrow keys) and normalizes movement input.
- Applies moveSpeed * normalized direction as Rigidbody2D.velocity each FixedUpdate.
- No animation or visual feedback beyond sprite movement; other systems handle that.

Configuration:
- moveSpeed: Units per second. Typical range 4–7 for an arena game.

Notes:
- Requires Rigidbody2D on the same GameObject (added at Awake if missing).
- Movement is independent of frame rate due to FixedUpdate and velocity application.
- No dashing, sprinting, or momentum—just direct speed control.
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
