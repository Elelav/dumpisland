using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TsunamiWave : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float waveSpeed = 8f;

    [Header("Wave Size")]
    [SerializeField] private float waveWidth = 1000f;
    [SerializeField] private float waveHeight = 3f;
    [SerializeField] private float tileWidth = 2f;
    [SerializeField] private float tileOverlap = 0.3f;

    [Header("Map Boundaries")]
    [SerializeField] private float startY = -15f;
    [SerializeField] private float endY = 15f;

    [Header("Tile Animation")]
    [SerializeField] private float wobbleSpeed = 3f;
    [SerializeField] private float wobbleHeight = 0.3f;
    [SerializeField] private float wobbleRandomness = 2f;

    [Header("Visual Effects")]
    [SerializeField] private Color waveColor = new Color(0.2f, 0.5f, 1f, 0.7f);

    [Header("Particles")]
    [SerializeField] private bool enableParticles = true;
    [SerializeField] private float particleSpawnRate = 0.05f;
    [SerializeField] private int particlesPerSpawn = 3;
    [SerializeField] private Color foamColor = new Color(0.9f, 0.95f, 1f, 0.9f);

    [Header("Optimization")]
    [SerializeField] private int maxSplashParticlesPerLoot = 5;
    [SerializeField] private int maxParticlesPerFrame = 50;

    [Header("Audio")]
    [SerializeField] private bool enableSound = true;

    private BoxCollider2D waveCollider;
    private int destroyedLootCount = 0;
    private List<WaveTile> tiles = new List<WaveTile>();
    private float lastParticleSpawnTime = 0f;
    private int particlesThisFrame = 0;

    // Cached wave sprite
    private static Sprite cachedWaveSprite;

    private class WaveTile
    {
        public GameObject gameObject;
        public SpriteRenderer spriteRenderer;
        public float wobbleOffset;
        public Vector3 startPosition;
    }

    void Awake()
    {
        waveCollider = gameObject.AddComponent<BoxCollider2D>();
        waveCollider.isTrigger = true;
        waveCollider.size = new Vector2(waveWidth, waveHeight);

        CreateWaveTiles();
    }

    private void CreateWaveTiles()
    {
        // Create sprite once
        if (cachedWaveSprite == null)
        {
            cachedWaveSprite = CreateWaveTileSprite();
        }

        float effectiveTileWidth = tileWidth - tileOverlap;
        int tileCount = Mathf.CeilToInt(waveWidth / effectiveTileWidth) + 2;

        float startX = -(waveWidth / 2f);

        for (int i = 0; i < tileCount; i++)
        {
            GameObject tileObj = new GameObject($"WaveTile_{i}");
            tileObj.transform.SetParent(transform);

            float xPos = startX + (i * effectiveTileWidth);
            tileObj.transform.localPosition = new Vector3(xPos, 0, 0);

            SpriteRenderer sr = tileObj.AddComponent<SpriteRenderer>();
            sr.sprite = cachedWaveSprite;
            sr.color = waveColor;
            sr.sortingLayerName = "Enemies";
            sr.sortingOrder = 15;

            float spriteHeightInUnits = 2f;
            float scaleY = waveHeight / spriteHeightInUnits;
            float scaleX = tileWidth / 2.5f;

            tileObj.transform.localScale = new Vector3(scaleX, scaleY, 1f);

            WaveTile tile = new WaveTile
            {
                gameObject = tileObj,
                spriteRenderer = sr,
                wobbleOffset = Random.Range(0f, wobbleRandomness * Mathf.PI),
                startPosition = tileObj.transform.localPosition
            };

            tiles.Add(tile);
        }
    }

    public void StartTsunami(Vector2 centerPosition)
    {
        transform.position = new Vector3(centerPosition.x, startY, 0);
        destroyedLootCount = 0;

        DebugHelper.Log("TSUNAMI STARTED!");

        if (TsunamiWarningUI.Instance != null)
        {
            TsunamiWarningUI.Instance.ShowWarning(3f);
        }

        StartCoroutine(TsunamiAnimation());
    }

    private IEnumerator TsunamiAnimation()
    {
        float currentY = startY;
        float time = 0f;

        while (currentY < endY)
        {
            time += Time.deltaTime;
            currentY += waveSpeed * Time.deltaTime;
            transform.position = new Vector3(transform.position.x, currentY, 0);

            AnimateTiles(time);

            // Reset particle counter
            particlesThisFrame = 0;

            // Spawn foam
            if (enableParticles && Time.time >= lastParticleSpawnTime + particleSpawnRate)
            {
                SpawnFoamParticles();
                lastParticleSpawnTime = Time.time;
            }

            // Destroy loot
            CheckAndDestroyLoot();

            yield return null;
        }

        DebugHelper.Log($"Tsunami completed! Destroyed: {destroyedLootCount} loot items");

        Destroy(gameObject);
    }

    private void AnimateTiles(float time)
    {
        foreach (WaveTile tile in tiles)
        {
            if (tile.gameObject == null) continue;

            float wobbleY = Mathf.Sin((time * wobbleSpeed) + tile.wobbleOffset) * wobbleHeight;
            float wobbleX = Mathf.Cos((time * wobbleSpeed * 0.5f) + tile.wobbleOffset) * 0.1f;

            tile.gameObject.transform.localPosition = new Vector3(
                tile.startPosition.x + wobbleX,
                tile.startPosition.y + wobbleY,
                tile.startPosition.z
            );
        }
    }

    private void SpawnFoamParticles()
    {
        int baseParticles = Mathf.RoundToInt(waveWidth / 50f);
        int totalParticles = Mathf.Max(baseParticles, particlesPerSpawn);

        // Respect limit
        totalParticles = Mathf.Min(totalParticles, maxParticlesPerFrame - particlesThisFrame);

        for (int i = 0; i < totalParticles; i++)
        {
            float randomX = Random.Range(-waveWidth / 2f, waveWidth / 2f);
            float randomYOffset = Random.Range(waveHeight * 0.3f, waveHeight * 0.5f);
            Vector3 spawnPos = transform.position + new Vector3(randomX, randomYOffset, 0);

            // Get particle from pool
            GameObject particle = WaterParticlePool.Instance != null
                ? WaterParticlePool.Instance.GetParticle()
                : null;

            if (particle == null) continue;

            particle.transform.position = spawnPos;

            WaterSplashParticle splash = particle.GetComponent<WaterSplashParticle>();
            if (splash != null)
            {
                Vector2 direction = new Vector2(
                    Random.Range(-0.5f, 0.5f),
                    Random.Range(0.5f, 1.2f)
                ).normalized;

                float speed = Random.Range(1.5f, 5f);

                Color color = foamColor;
                color.r += Random.Range(-0.05f, 0.1f);
                color.g += Random.Range(-0.05f, 0.05f);
                color.b += Random.Range(-0.05f, 0.05f);
                color.a = Random.Range(0.5f, 0.95f);

                float size = Random.Range(0.08f, 0.25f);

                splash.Initialize(direction * speed, color, size);

                particlesThisFrame++;
            }
        }
    }

    private void CheckAndDestroyLoot()
    {
        Loot[] allLoot = FindObjectsOfType<Loot>();

        foreach (Loot loot in allLoot)
        {
            if (loot == null) continue;

            Vector3 lootPos = loot.transform.position;
            float distanceY = Mathf.Abs(lootPos.y - transform.position.y);
            float distanceX = Mathf.Abs(lootPos.x - transform.position.x);

            if (distanceY < waveHeight / 2f && distanceX < waveWidth / 2f)
            {
                DestroyLoot(loot.gameObject);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        Loot loot = collision.GetComponent<Loot>();

        if (loot != null)
        {
            DestroyLoot(collision.gameObject);
        }
    }

    private void DestroyLoot(GameObject lootObj)
    {
        if (lootObj == null) return;

        destroyedLootCount++;

        if (enableParticles && particlesThisFrame < maxParticlesPerFrame)
        {
            CreateSimpleSplash(lootObj.transform.position);
        }

        // Return to pool instead of Destroy
        if (LootPool.Instance != null)
        {
            LootPool.Instance.ReturnLoot(lootObj);
        }
        else
        {
            Destroy(lootObj); // Fallback
        }
    }

    private void CreateSimpleSplash(Vector3 position)
    {
        int particlesAvailable = maxParticlesPerFrame - particlesThisFrame;
        int particlesToSpawn = Mathf.Min(maxSplashParticlesPerLoot, particlesAvailable);

        for (int i = 0; i < particlesToSpawn; i++)
        {
            // Get particle from pool
            GameObject particle = WaterParticlePool.Instance != null
                ? WaterParticlePool.Instance.GetParticle()
                : null;

            if (particle == null) break;

            particle.transform.position = position;

            WaterSplashParticle splash = particle.GetComponent<WaterSplashParticle>();
            if (splash != null)
            {
                Vector2 direction = new Vector2(
                    Random.Range(-1f, 1f),
                    Random.Range(0.5f, 1f)
                ).normalized;

                float speed = Random.Range(2f, 4f);

                Color color = new Color(0.7f, 0.9f, 1f, 0.8f);
                color.r += Random.Range(-0.1f, 0.1f);
                color.g += Random.Range(-0.1f, 0.1f);

                float size = Random.Range(0.1f, 0.2f);

                splash.Initialize(direction * speed, color, size);

                particlesThisFrame++;
            }
        }
    }

    private Sprite CreateWaveTileSprite()
    {
        // Verification check
        Debug.LogError("TsunamiWave.CreateWaveTileSprite() CALLED!");

        int width = 80;
        int height = 64;

        Texture2D texture = new Texture2D(width, height);

        // Register texture creation
        if (TextureDebugger.Instance != null)
        {
            TextureDebugger.Instance.RegisterTextureCreation("TsunamiWave", width, height);
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float gradientY = (float)y / height;
                float wave = Mathf.Sin((float)x / width * Mathf.PI * 3f) * 0.2f;
                float adjustedGradientY = gradientY + wave;

                float gradientX = 1f;
                if (x < 5)
                {
                    gradientX = (float)x / 5f;
                }
                else if (x > width - 5)
                {
                    gradientX = (float)(width - x) / 5f;
                }

                float foam = 0f;
                if (y > height - 10 && Random.value > 0.7f)
                {
                    foam = Random.Range(0.3f, 0.8f);
                }

                float alpha = (0.7f - adjustedGradientY * 0.4f) * gradientX;
                alpha = Mathf.Clamp01(alpha + foam);

                texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
        }

        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0, 0, width, height),
            new Vector2(0.5f, 0.5f),
            32
        );
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 size = new Vector3(waveWidth, waveHeight, 0);
        Gizmos.DrawWireCube(transform.position, size);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(
            new Vector3(transform.position.x, startY, 0),
            new Vector3(transform.position.x, endY, 0)
        );
    }
}