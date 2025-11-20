using UnityEngine;
using System.Collections.Generic;

public class HitParticlePool : MonoBehaviour
{
    public static HitParticlePool Instance;

    [Header("Pool Settings")]
    [SerializeField] private int initialPoolSize = 100;
    [SerializeField] private int maxPoolSize = 300;

    private Queue<GameObject> availableParticles = new Queue<GameObject>();
    private List<GameObject> allParticles = new List<GameObject>();

    // Cached sprite (created once)
    private static Sprite cachedCircleSprite;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Create sprite once for all particles
        if (cachedCircleSprite == null)
        {
            cachedCircleSprite = Resources.Load<Sprite>("UI/Skin/Knob");
            if (cachedCircleSprite == null)
            {
                cachedCircleSprite = CreateCircleSprite();
                DebugHelper.Log("Created cached sprite for hit particles");
            }
        }

        // Pre-warm the pool
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewParticle();
        }

        DebugHelper.Log($"Hit Particle Pool created: {initialPoolSize} particles");
    }

    private GameObject CreateNewParticle()
    {
        GameObject particle = new GameObject("PooledHitParticle");
        particle.transform.SetParent(transform);
        particle.SetActive(false);

        HitParticle hitScript = particle.AddComponent<HitParticle>();
        hitScript.SetPool(this);
        hitScript.SetSprite(cachedCircleSprite); // Use cached sprite

        allParticles.Add(particle);
        availableParticles.Enqueue(particle);

        return particle;
    }

    public GameObject GetParticle()
    {
        GameObject particle;

        if (availableParticles.Count > 0)
        {
            particle = availableParticles.Dequeue();
        }
        else if (allParticles.Count < maxPoolSize)
        {
            particle = CreateNewParticle();
        }
        else
        {
            return null;
        }

        particle.SetActive(true);
        return particle;
    }

    public void ReturnParticle(GameObject particle)
    {
        if (particle == null) return;

        particle.SetActive(false);
        particle.transform.SetParent(transform);

        if (!availableParticles.Contains(particle))
        {
            availableParticles.Enqueue(particle);
        }
    }

    public int GetActiveCount()
    {
        return allParticles.Count - availableParticles.Count;
    }

    // Create texture once
    private static Sprite CreateCircleSprite()
    {
        DebugHelper.LogError("HitParticlePool.CreateCircleSprite() CALLED!");

        int size = 32;
        Texture2D texture = new Texture2D(size, size);

        // Register creation
        if (TextureDebugger.Instance != null)
        {
            TextureDebugger.Instance.RegisterTextureCreation("HitParticlePool", size, size);
        }

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
                    float alpha = 1f - (distance / radius) * 0.3f;
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

        return Sprite.Create(texture, new Rect(0, 0, size, size),
                             new Vector2(0.5f, 0.5f), size);
    }

    // Cleanup on destroy
    void OnDestroy()
    {
        if (cachedCircleSprite != null && cachedCircleSprite.texture != null)
        {
            Destroy(cachedCircleSprite.texture);
            cachedCircleSprite = null;
        }
    }
}