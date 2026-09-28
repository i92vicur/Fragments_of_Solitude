/*
MovingPlatform.cs

Purpose:
- Controls the moving platform flow for the platforming flow, movement, hazards, camera behavior, and phase transitions.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class MovingPlatform : MonoBehaviour
{
    [Header("Path (Local Space)")]
    [SerializeField, Tooltip("Local-space offset from the initial position for the path start.")]
    private Vector2 startLocalOffset = Vector2.zero;

    [SerializeField, Tooltip("Local-space offset from the initial position for the path end.")]
    private Vector2 endLocalOffset = new Vector2(3f, 0f);

    [Header("Motion")]
    [SerializeField, Tooltip("Horizontal movement speed between endpoints (units per second).")]
    private float movementSpeed = 2f;

    [SerializeField, Tooltip("Optional pause duration at each endpoint. Set to 0 for no pause.")]
    private float endpointPauseDuration = 0f;

    private readonly HashSet<Rigidbody2D> riders = new HashSet<Rigidbody2D>();

    private Rigidbody2D rb;
    private Collider2D platformCollider;
    private Vector3 anchorLocalPosition;
    private bool movingTowardEnd = true;
    private float pauseTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        platformCollider = GetComponent<Collider2D>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;
        rb.simulated = true;
        rb.interpolation = RigidbodyInterpolation2D.None;

        platformCollider.isTrigger = false;

        anchorLocalPosition = transform.localPosition;

        startLocalOffset.y = 0f;
        endLocalOffset.y = 0f;

        Vector2 startPosition = GetWorldPoint(startLocalOffset);
        rb.position = startPosition;
        transform.position = startPosition;
        movingTowardEnd = true;
    }

    private void OnValidate()
    {
        movementSpeed = Mathf.Max(0f, movementSpeed);
        endpointPauseDuration = Mathf.Max(0f, endpointPauseDuration);

        startLocalOffset.y = 0f;
        endLocalOffset.y = 0f;
    }

    private void FixedUpdate()
    {
        Vector2 before = rb.position;

        if (pauseTimer > 0f)
        {
            pauseTimer = Mathf.Max(0f, pauseTimer - Time.fixedDeltaTime);
        }
        else
        {
            MoveAlongPath();
        }

        Vector2 delta = rb.position - before;
        if (delta.sqrMagnitude > 0f)
            CarryRiders(delta);
    }

    private void MoveAlongPath()
    {
        Vector2 target = movingTowardEnd ? GetWorldPoint(endLocalOffset) : GetWorldPoint(startLocalOffset);
        Vector2 next = Vector2.MoveTowards(rb.position, target, movementSpeed * Time.fixedDeltaTime);
        rb.MovePosition(next);

        if ((next - target).sqrMagnitude <= 0.0001f)
        {
            movingTowardEnd = !movingTowardEnd;
            if (endpointPauseDuration > 0f)
                pauseTimer = endpointPauseDuration;
        }
    }

    private Vector2 GetWorldPoint(Vector2 localOffset)
    {
        Vector3 localPoint = anchorLocalPosition + new Vector3(localOffset.x, 0f, 0f);
        if (transform.parent != null)
            return transform.parent.TransformPoint(localPoint);

        return localPoint;
    }

    private void CarryRiders(Vector2 delta)
    {
        if (riders.Count == 0)
            return;

        List<Rigidbody2D> toRemove = null;

        foreach (Rigidbody2D rider in riders)
        {
            if (rider == null || !IsRiderStillStanding(rider))
            {
                toRemove ??= new List<Rigidbody2D>();
                toRemove.Add(rider);
                continue;
            }

            rider.MovePosition(rider.position + delta);
        }

        if (toRemove == null)
            return;

        for (int i = 0; i < toRemove.Count; i++)
            riders.Remove(toRemove[i]);
    }

    private bool IsRiderStillStanding(Rigidbody2D rider)
    {
        if (platformCollider == null)
            return false;

        float platformTop = platformCollider.bounds.max.y;
        if (rider.position.y < platformTop - 0.2f)
            return false;

        if (rider.linearVelocity.y > 0.05f)
            return false;

        return true;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        Rigidbody2D otherBody = collision.rigidbody;
        if (otherBody == null)
            return;

        if (IsStandingContact(collision))
            riders.Add(otherBody);
        else
            riders.Remove(otherBody);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.rigidbody != null)
            riders.Remove(collision.rigidbody);
    }

    private bool IsStandingContact(Collision2D collision)
    {
        if (platformCollider == null)
            return false;

        float platformTop = platformCollider.bounds.max.y - 0.02f;
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D contact = collision.GetContact(i);
            if (contact.point.y >= platformTop)
                return true;
        }

        return false;
    }
}
