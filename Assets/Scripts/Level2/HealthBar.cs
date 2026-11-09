/*
HealthBar.cs

Purpose:
- Controls the health bar flow for the roguelite combat loop, enemy behaviors, progression, and procedural map generation.

Design decisions:
- Keeps each component focused on one responsibility so the scene remains easier to understand and maintain.
- Favors explicit references, events, and serialized fields over hidden scene-wide lookups or global state.
- Uses the Unity lifecycle and lightweight state checks to keep behavior readable and easy to debug.
*/

using UnityEngine;

public class HealthBar : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Health component to observe. If empty, the script will attempt to find the Player by tag and use its Health.")]
    public Health targetHealth;

    [Header("Layout (pixels)")]
    public Vector2 position = new Vector2(10, 10);
    public Vector2 size = new Vector2(220, 22);

    [Header("Colors")]
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.6f);
    public Color fillColor = new Color(0.2f, 0.8f, 0.2f, 0.9f);
    public Color textColor = Color.white;

    [Header("Options")]
    public bool showText = true;

    // internal state updated via event
    private int current = 0;
    private int max = 1;

    void Awake()
    {
        // If no explicit target, try to find the Player by tag (non-expensive one-time attempt)
        if (targetHealth == null)
        {
            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null)
            {
                targetHealth = playerGO.GetComponent<Health>();
            }
        }
    }

    void OnEnable()
    {
        if (targetHealth != null)
        {
            targetHealth.OnHealthChanged += OnHealthChanged;
            // ensure initial values are read
            current = targetHealth.CurrentHealth;
            max = targetHealth.MaxHealth;
        }
    }

    void OnDisable()
    {
        if (targetHealth != null)
            targetHealth.OnHealthChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(int newCurrent, int newMax)
    {
        current = newCurrent;
        max = Mathf.Max(1, newMax);
    }

    void OnGUI()
    {
        // Draw only if target was found
        if (targetHealth == null) return;

        // Background
        var bgRect = new Rect(position.x, position.y, size.x, size.y);
        Color prev = GUI.color;
        GUI.color = backgroundColor;
        GUI.Box(bgRect, GUIContent.none);

        // Fill (clamped)
        float fillPct = (max > 0) ? (current / (float)max) : 0f;
        fillPct = Mathf.Clamp01(fillPct);
        var fillRect = new Rect(position.x + 2, position.y + 2, (size.x - 4) * fillPct, size.y - 4);
        GUI.color = fillColor;
        GUI.Box(fillRect, GUIContent.none);

        // Text overlay
        if (showText)
        {
            GUI.color = textColor;
            string label = $"HP: {current}/{max}";
            var style = GUI.skin.label;
            style.alignment = TextAnchor.MiddleCenter;
            GUI.Label(bgRect, label, style);
        }

        GUI.color = prev;
    }
}
