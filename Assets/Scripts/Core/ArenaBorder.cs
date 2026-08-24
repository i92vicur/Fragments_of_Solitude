using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ArenaBorder
/// ----------------
/// Small utility that draws a visible border around the playable arena so map limits are obvious in both
/// Scene view and Game view. The component is intentionally simple and focused for the prototype:
/// - Requires a reference to the Arena_Bounds root Transform (no scene-wide searches).
/// - Computes a bounding rectangle from child Collider2D bounds (common setup for arena walls) and
///   draws a closed rectangle using a LineRenderer for Game view, and Gizmos for Scene view.
/// - Exposes color and thickness tuning values in the Inspector. No gameplay or physics changes.
///
/// Design decisions:
/// - Keeps behaviour deterministic and allocation-free during play: border is rebuilt only on Start/OnValidate.
/// - Avoids adding colliders or touching physics layers. The border is purely visual.
/// - Requires manual assignment of the Arena_Bounds reference to respect the project's "avoid scene-wide searches" rule.
///
/// Usage:
/// - Add this component to any GameObject (for example a new child of the existing Arena_Bounds object).
/// - Assign the Arena Bounds Root (Transform containing the arena wall colliders) in the Inspector.
/// - Tweak Color and Thickness to taste. Toggle DrawInGame to hide border during play if undesired.
///
/// Limitations:
/// - If Arena_Bounds has no Collider2D children, the optional FallbackCenter/FallbackSize are used.
/// - The border is axis-aligned (rectangle). Irregular arena shapes are approximated by the bounding box.
/// - The component does not update automatically if arena walls move at runtime. Call RebuildBorder() after changes.
/// </summary>
[ExecuteAlways]
public class ArenaBorder : MonoBehaviour
{
    [Header("References (required)")]
    [Tooltip("Transform that contains the arena wall colliders as children (e.g. Arena_Bounds).")]
    [SerializeField] private Transform arenaBoundsRoot = null;

    [Header("Visual")]
    [SerializeField] private Color borderColor = new Color(1f, 0.2f, 0.2f, 0.9f);
    [SerializeField] private float thickness = 0.08f;
    [SerializeField] private bool drawInGame = true;

    [Header("Fallback (used only if no Collider2D children found)")]
    [SerializeField] private Vector2 fallbackCenter = Vector2.zero;
    [SerializeField] private Vector2 fallbackSize = new Vector2(20f, 12f);

    private LineRenderer lineRenderer;
    // Shared material to avoid runtime allocations/leaks when creating many materials in editor/play mode
    private static Material s_lineMaterial = null;
    private readonly Vector3[] corners = new Vector3[4];

    private void Awake()
    {
        if (!Application.isPlaying)
            return;

        EnsureLineRenderer();
        RebuildBorder();
    }

    private void OnEnable()
    {
        // In editor, keep visuals up-to-date when enabled
        EnsureLineRenderer();
        RebuildBorder();
    }

    private void OnValidate()
    {
        // Keep editor changes visible immediately without allocating per-frame
        EnsureLineRenderer();
        RebuildBorder();
    }

    private void EnsureLineRenderer()
    {
        if (lineRenderer != null)
            return;

        // Try to find an existing child LineRenderer
        var existing = GetComponentInChildren<LineRenderer>();
        if (existing != null && existing.gameObject != gameObject)
        {
            lineRenderer = existing;
            return;
        }

        // Create a dedicated child to hold the LineRenderer
        var go = new GameObject("ArenaBorderRenderer");
        go.transform.SetParent(transform, false);

        lineRenderer = go.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = true;
        lineRenderer.positionCount = 4;
        lineRenderer.startWidth = thickness;
        lineRenderer.endWidth = thickness;
        lineRenderer.numCapVertices = 2;
        lineRenderer.numCornerVertices = 2;

        // Use a shared static material to avoid creating/destroying materials at runtime (prevents native leaks)
        // and ensure consistent rendering. Create it only once.
        if (s_lineMaterial == null)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader != null)
                s_lineMaterial = new Material(shader);
            else
                s_lineMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            // Keep the material from being accidentally unloaded during domain reloads in editor
            s_lineMaterial.hideFlags = HideFlags.DontUnloadUnusedAsset;
        }

        lineRenderer.material = s_lineMaterial;

        // Prefer to render on top of floor by using a large sorting order
        #if UNITY_2019_2_OR_NEWER
        lineRenderer.sortingOrder = 1000;
        #endif

        lineRenderer.enabled = drawInGame;
        lineRenderer.startColor = borderColor;
        lineRenderer.endColor = borderColor;
    }

    /// <summary>
    /// Recompute the bounding rectangle and update the LineRenderer positions.
    /// Call this manually if the arena changes at runtime.
    /// </summary>
    public void RebuildBorder()
    {
        Bounds bounds;
        if (arenaBoundsRoot != null)
        {
            var colliders = arenaBoundsRoot.GetComponentsInChildren<Collider2D>();
            if (colliders != null && colliders.Length > 0)
            {
                bounds = colliders[0].bounds;
                for (int i = 1; i < colliders.Length; i++)
                    bounds.Encapsulate(colliders[i].bounds);
            }
            else
            {
                bounds = new Bounds((Vector3)fallbackCenter, (Vector3)fallbackSize);
            }
        }
        else
        {
            bounds = new Bounds((Vector3)fallbackCenter, (Vector3)fallbackSize);
        }

        float minX = GetLeftArenaBorderX();
        float maxX = GetRightArenaBorderX();
        float minY = GetBottomArenaBorderY();
        float maxY = GetTopArenaBorderY();

        corners[0] = new Vector3(minX, minY, 0f);
        corners[1] = new Vector3(minX, maxY, 0f);
        corners[2] = new Vector3(maxX, maxY, 0f);
        corners[3] = new Vector3(maxX, minY, 0f);

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 4;
            lineRenderer.startWidth = thickness;
            lineRenderer.endWidth = thickness;
            lineRenderer.startColor = borderColor;
            lineRenderer.endColor = borderColor;

            for (int i = 0; i < 4; i++)
                lineRenderer.SetPosition(i, corners[i]);

            lineRenderer.enabled = drawInGame;
        }
    }

    private void OnDrawGizmos()
    {
        // Scene-view gizmo: draw the same rectangle so it's visible while editing.
        float minX = GetLeftArenaBorderX();
        float maxX = GetRightArenaBorderX();
        float minY = GetBottomArenaBorderY();
        float maxY = GetTopArenaBorderY();

        Gizmos.color = borderColor;
        var bl = new Vector3(minX, minY, 0f);
        var tl = new Vector3(minX, maxY, 0f);
        var tr = new Vector3(maxX, maxY, 0f);
        var br = new Vector3(maxX, minY, 0f);

        Gizmos.DrawLine(bl, tl);
        Gizmos.DrawLine(tl, tr);
        Gizmos.DrawLine(tr, br);
        Gizmos.DrawLine(br, bl);
    }

    public float GetTopArenaBorderY()
    {
        return GetBoundaryY(true);
    }

    public float GetBottomArenaBorderY()
    {
        return GetBoundaryY(false);
    }

    public float GetLeftArenaBorderX()
    {
        return GetBoundaryX(true);
    }

    public float GetRightArenaBorderX()
    {
        return GetBoundaryX(false);
    }

    private float GetBoundaryY(bool top)
    {
        if (arenaBoundsRoot == null)
            return fallbackCenter.y + (top ? fallbackSize.y * 0.5f : -fallbackSize.y * 0.5f);

        var colliders = arenaBoundsRoot.GetComponentsInChildren<Collider2D>();
        if (colliders == null || colliders.Length == 0)
            return fallbackCenter.y + (top ? fallbackSize.y * 0.5f : -fallbackSize.y * 0.5f);

        float best = top ? float.MinValue : float.MaxValue;
        for (int i = 0; i < colliders.Length; i++)
        {
            var col = colliders[i];
            if (col == null)
                continue;

            float candidate = top ? col.bounds.min.y : col.bounds.max.y;
            if (top)
            {
                if (candidate > best)
                    best = candidate;
            }
            else
            {
                if (candidate < best)
                    best = candidate;
            }
        }

        if (top && best != float.MinValue)
            return best;
        if (!top && best != float.MaxValue)
            return best;

        return fallbackCenter.y + (top ? fallbackSize.y * 0.5f : -fallbackSize.y * 0.5f);
    }

    private float GetBoundaryX(bool left)
    {
        if (arenaBoundsRoot == null)
            return fallbackCenter.x + (left ? -fallbackSize.x * 0.5f : fallbackSize.x * 0.5f);

        var colliders = arenaBoundsRoot.GetComponentsInChildren<Collider2D>();
        if (colliders == null || colliders.Length == 0)
            return fallbackCenter.x + (left ? -fallbackSize.x * 0.5f : fallbackSize.x * 0.5f);

        float best = left ? float.MaxValue : float.MinValue;
        for (int i = 0; i < colliders.Length; i++)
        {
            var col = colliders[i];
            if (col == null)
                continue;

            float candidate = left ? col.bounds.max.x : col.bounds.min.x;
            if (left)
            {
                if (candidate < best)
                    best = candidate;
            }
            else
            {
                if (candidate > best)
                    best = candidate;
            }
        }

        if (left && best != float.MaxValue)
            return best;
        if (!left && best != float.MinValue)
            return best;

        return fallbackCenter.x + (left ? -fallbackSize.x * 0.5f : fallbackSize.x * 0.5f);
    }

    // Public helper to set the arena root and rebuild on demand
    public void SetArenaRoot(Transform root)
    {
        arenaBoundsRoot = root;
        RebuildBorder();
    }
}
