/*
HealthBar.cs

Tiny on-screen health bar for the prototype (temporary).
- Purpose: Provide a minimal, easy-to-replace visual for the Player's Health using Unity's IMGUI (OnGUI).
- Reason: IMGUI avoids requiring Unity UI setup and is extremely small and safe for early prototyping.
- Behavior:
  - Subscribes to a Health component's OnHealthChanged event to receive updates (no polling).
  - If no Health is assigned in the Inspector, attempts to find the Player by tag and uses its Health.
  - Draws a simple background rectangle and a filled foreground representing (current/max).
  - Optional text shows numeric health.
- Temporary: This is explicitly a prototype helper. Replace with a proper Canvas + Image-based HUD when ready.
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
