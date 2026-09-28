/*
PlatformerPhase2Transition.cs

Purpose:
- Controls the platformer phase2 transition flow for the platforming flow, movement, hazards, camera behavior, and phase transitions.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class PlatformerPhase2Transition : MonoBehaviour
{
    [SerializeField] private string phase2SceneName = "Level4Phase2";

    private SpriteRenderer backgroundRenderer;
    private Collider2D playerCollider;
    private SceneLoader sceneLoader;
    private bool transitionTriggered;

    private void Awake()
    {
        backgroundRenderer = GetComponent<SpriteRenderer>();

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            playerCollider = player.GetComponent<Collider2D>();

        sceneLoader = FindAnyObjectByType<SceneLoader>();
    }

    private void Update()
    {
        if (transitionTriggered || backgroundRenderer == null || playerCollider == null || sceneLoader == null)
            return;

        if (playerCollider.bounds.max.y < backgroundRenderer.bounds.max.y)
            return;

        transitionTriggered = true;
        sceneLoader.SetPhase2SceneName(phase2SceneName);
        sceneLoader.LoadPhase2();
    }
}