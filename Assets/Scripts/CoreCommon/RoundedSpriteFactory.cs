using UnityEngine;

public static class RoundedSpriteFactory
{
    private const int TextureSize = 32;
    private const float CornerRadius = 10f;

    private static Sprite cachedSprite;

    // Sliced sprite so the corners keep their radius at any size.
    public static Sprite Get()
    {
        if (cachedSprite != null)
            return cachedSprite;

        Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Color[] pixels = new Color[TextureSize * TextureSize];
        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                Vector2 nearest = new Vector2(
                    Mathf.Clamp(p.x, CornerRadius, TextureSize - CornerRadius),
                    Mathf.Clamp(p.y, CornerRadius, TextureSize - CornerRadius));
                float coverage = Mathf.Clamp01(CornerRadius - Vector2.Distance(p, nearest) + 0.5f);
                pixels[y * TextureSize + x] = new Color(1f, 1f, 1f, coverage);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        cachedSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, TextureSize, TextureSize),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(CornerRadius, CornerRadius, CornerRadius, CornerRadius));

        return cachedSprite;
    }
}
