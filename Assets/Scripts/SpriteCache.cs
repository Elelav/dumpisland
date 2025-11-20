using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Cache for programmatically created sprites to prevent memory leaks
/// </summary>
public class SpriteCache : MonoBehaviour
{
    private static SpriteCache _instance;
    public static SpriteCache Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("SpriteCache");
                _instance = go.AddComponent<SpriteCache>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // Sprite cache by key
    private Dictionary<string, Sprite> cachedSprites = new Dictionary<string, Sprite>();

    /// <summary>
    /// Get or create a circular sprite
    /// </summary>
    public Sprite GetCircleSprite(int size = 32)
    {
        string key = $"Circle_{size}";

        if (cachedSprites.ContainsKey(key))
        {
            return cachedSprites[key];
        }

        // Create once
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);

                if (distance <= radius)
                {
                    float alpha = 1f - (distance / radius);
                    alpha = Mathf.Pow(alpha, 1.2f);
                    pixels[y * size + x] = new Color(1, 1, 1, alpha);
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        cachedSprites[key] = sprite;

        DebugHelper.Log($"SpriteCache: Created circle sprite {size}x{size}");

        return sprite;
    }

    /// <summary>
    /// Get or create an elongated sprite (for slash effects)
    /// </summary>
    public Sprite GetSlashSprite(int width = 32, int height = 96)
    {
        string key = $"Slash_{width}x{height}";

        if (cachedSprites.ContainsKey(key))
        {
            return cachedSprites[key];
        }

        Texture2D texture = new Texture2D(width, height);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float distFromCenterX = Mathf.Abs(x - width / 2f) / (width / 2f);
                float distFromCenterY = Mathf.Abs(y - height / 2f) / (height / 2f);

                float alpha = (1f - distFromCenterX * 0.5f) * (1f - distFromCenterY * 0.3f);
                alpha = Mathf.Clamp01(alpha);

                texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
        }
        texture.Apply();

        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 32);
        cachedSprites[key] = sprite;

        DebugHelper.Log($"SpriteCache: Created slash sprite {width}x{height}");

        return sprite;
    }

    /// <summary>
    /// Clear cache (called on game exit)
    /// </summary>
    public void ClearCache()
    {
        foreach (var sprite in cachedSprites.Values)
        {
            if (sprite != null && sprite.texture != null)
            {
                Destroy(sprite.texture);
            }
            if (sprite != null)
            {
                Destroy(sprite);
            }
        }
        cachedSprites.Clear();
        DebugHelper.Log("SpriteCache cleared");
    }

    void OnDestroy()
    {
        ClearCache();
    }
}