using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class Phase1Level6Staircase : MonoBehaviour
{
    [Header("Scene transition")]
    [SerializeField] private SceneLoader sceneLoader;
    [SerializeField] private string phase2SceneName = "Level2_Phase2";
    [SerializeField] private SpriteRenderer visual;

    private Collider2D triggerCollider;
    private PlayerExperience playerExperience;
    private bool activated;
    private bool transitionTriggered;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
            triggerCollider.enabled = false;
        }

        if (visual == null)
        {
            visual = GetComponentInChildren<SpriteRenderer>();
        }

        if (visual == null)
        {
            GameObject visualGO = new GameObject("StairVisual");
            visualGO.transform.SetParent(transform, false);
            visual = visualGO.AddComponent<SpriteRenderer>();
            visual.sprite = CreatePlaceholderSprite();
            visual.drawMode = SpriteDrawMode.Sliced;
            visual.size = new Vector2(2.5f, 2.5f);
        }

        visual.color = new Color(0.35f, 0.45f, 0.9f, 0.45f);
        visual.enabled = false;
    }

    private void OnEnable()
    {
        if (sceneLoader == null)
        {
            sceneLoader = FindAnyObjectByType<SceneLoader>();
        }

        if (playerExperience == null)
        {
            playerExperience = FindAnyObjectByType<PlayerExperience>();
        }

        if (playerExperience != null)
        {
            playerExperience.OnLevelUp -= HandleLevelUp;
            playerExperience.OnLevelUp += HandleLevelUp;
        }
    }

    private void OnDisable()
    {
        if (playerExperience != null)
        {
            playerExperience.OnLevelUp -= HandleLevelUp;
        }
    }

    private void HandleLevelUp(int newLevel)
    {
        if (newLevel < 6 || activated)
        {
            return;
        }

        Activate();
    }

    public void Activate()
    {
        if (activated)
        {
            return;
        }

        activated = true;

        if (triggerCollider != null)
        {
            triggerCollider.enabled = true;
        }

        if (visual != null)
        {
            visual.enabled = true;
            visual.color = new Color(0.4f, 0.9f, 0.7f, 1f);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!activated || transitionTriggered)
        {
            return;
        }

        if (other == null)
        {
            return;
        }

        bool isPlayer = other.CompareTag("Player")
            || (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player"));

        if (!isPlayer)
        {
            return;
        }

        if (sceneLoader == null)
        {
            sceneLoader = FindAnyObjectByType<SceneLoader>();
        }

        if (sceneLoader == null)
        {
            Debug.LogError("[Phase1Level6Staircase] SceneLoader not found. Assign it in the Inspector or keep one in the scene.", this);
            return;
        }

        transitionTriggered = true;

        if (!string.IsNullOrEmpty(phase2SceneName))
        {
            sceneLoader.SetSceneNameForPhase2(phase2SceneName);
        }

        sceneLoader.LoadPhase2();
    }

    private static Sprite CreatePlaceholderSprite()
    {
        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color(0.25f, 0.45f, 0.95f, 1f);
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 32f);
    }
}
