/*
BossMarker.cs

Purpose:
- Controls the boss marker flow for the roguelite combat loop, enemy behaviors, progression, and procedural map generation.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using UnityEngine;

// Marker component to identify boss instances at runtime.
[DisallowMultipleComponent]
public class BossMarker : MonoBehaviour { }
