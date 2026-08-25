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
