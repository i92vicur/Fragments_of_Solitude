/*
MapStructure.cs

Purpose:
- Controls the map structure flow for the roguelite combat loop, enemy behaviors, progression, and procedural map generation.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using UnityEngine;

[DisallowMultipleComponent]
public sealed class MapStructure : MonoBehaviour
{
    [SerializeField] private SpriteRenderer structureRenderer;
    [SerializeField] private BoxCollider2D structureCollider;

    private void Reset()
    {
        structureRenderer = GetComponent<SpriteRenderer>();
        structureCollider = GetComponent<BoxCollider2D>();
    }

    private void OnValidate()
    {
        if (structureRenderer == null)
            structureRenderer = GetComponent<SpriteRenderer>();
        if (structureCollider == null)
            structureCollider = GetComponent<BoxCollider2D>();
    }
}
