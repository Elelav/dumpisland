using UnityEngine;
using System.Collections.Generic;

public class WaterParticlePool : MonoBehaviour
{
    public static WaterParticlePool Instance;

    [Header("Настройки пула")]
    [SerializeField] private int initialPoolSize = 50;
    [SerializeField] private int maxPoolSize = 200;

    private Queue<GameObject> availableParticles = new Queue<GameObject>();
    private List<GameObject> allParticles = new List<GameObject>();

    private static Sprite cachedSprite;

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

        if (cachedSprite == null)
        {
            cachedSprite = Resources.Load<Sprite>("UI/Skin/Knob");
            if (cachedSprite == null)
            {
                cachedSprite = CreateCircleSprite();
            }
        }

        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewParticle();
        }

        DebugHelper.Log($"💧 Water Particle Pool создан: {initialPoolSize} партиклов");
    }

    private GameObject CreateNewParticle()
    {
        GameObject particle = new GameObject("PooledWaterParticle");
        particle.transform.SetParent(transform);
        particle.SetActive(false);

        SpriteRenderer sr = particle.AddComponent<SpriteRenderer>();
        sr.sprite = cachedSprite;
        sr.sortingLayerName = "Effects";
        sr.sortingOrder = 20;

        WaterSplashParticle splash = particle.AddComponent<WaterSplashParticle>();
        splash.SetPool(this);

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

    private static Sprite CreateCircleSprite()
    {
        Debug.LogError("🔴 HitParticlePool.CreateCircleSprite() Called!");

        int size = 32;
        Texture2D texture = new Texture2D(size, size);

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

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    void OnDestroy()
    {
        if (cachedSprite != null && cachedSprite.texture != null)
        {
            Destroy(cachedSprite.texture);
            cachedSprite = null;
        }
    }
}