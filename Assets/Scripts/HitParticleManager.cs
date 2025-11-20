using UnityEngine;

public class HitParticleManager : MonoBehaviour
{
    public static HitParticleManager Instance { get; private set; }

    [Header("Limits")]
    [SerializeField] private int maxParticlesPerHit = 8;

    // New: Cached sprite for crit flash
    private static Sprite cachedCritFlashSprite;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // Create sprite once
        if (cachedCritFlashSprite == null)
        {
            cachedCritFlashSprite = Resources.Load<Sprite>("UI/Skin/Knob");

            if (cachedCritFlashSprite == null)
            {
                cachedCritFlashSprite = CreateSimpleCircleSprite();
                Debug.Log("Created cached sprite for crit flash");
            }
        }
    }

    public void CreateHitEffect(Vector3 position, Color enemyColor, bool isCrit = false, Vector2 hitDirection = default)
    {
        int particleCount = isCrit ? Random.Range(6, maxParticlesPerHit) : Random.Range(3, 6);

        for (int i = 0; i < particleCount; i++)
        {
            GameObject particle = HitParticlePool.Instance != null
                ? HitParticlePool.Instance.GetParticle()
                : new GameObject("HitParticle");

            if (particle == null) break;

            particle.transform.position = position;

            HitParticle particleScript = particle.GetComponent<HitParticle>();
            if (particleScript == null)
            {
                particleScript = particle.AddComponent<HitParticle>();
            }

            Vector2 direction;
            if (hitDirection != Vector2.zero)
            {
                float angle = Random.Range(-45f, 45f);
                direction = Quaternion.Euler(0, 0, angle) * hitDirection;
            }
            else
            {
                direction = Random.insideUnitCircle.normalized;
            }

            float speed = isCrit ? Random.Range(3f, 6f) : Random.Range(1.5f, 3f);

            Color particleColor;
            if (isCrit)
            {
                particleColor = Color.Lerp(Color.yellow, Color.white, Random.Range(0.3f, 0.7f));
            }
            else
            {
                particleColor = Color.Lerp(enemyColor, Color.white, 0.4f);
            }

            particleScript.Initialize(direction * speed, particleColor, isCrit);
        }
    }

    public void CreateCritEffect(Vector3 position)
    {
        GameObject flash = new GameObject("CritFlash");
        flash.transform.position = position;

        SpriteRenderer sr = flash.AddComponent<SpriteRenderer>();

        // Use cached sprite
        sr.sprite = cachedCritFlashSprite;

        sr.color = new Color(1f, 1f, 0.3f, 0.9f);
        sr.sortingLayerName = "Effects";
        sr.sortingOrder = 15;

        flash.transform.localScale = Vector3.one * 0.5f;

        StartCoroutine(CritFlashAnimation(flash));
    }

    private System.Collections.IEnumerator CritFlashAnimation(GameObject flash)
    {
        SpriteRenderer sr = flash.GetComponent<SpriteRenderer>();
        float elapsed = 0f;
        float duration = 0.3f;

        Vector3 startScale = Vector3.one * 0.3f;
        Vector3 endScale = Vector3.one * 0.8f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            flash.transform.localScale = Vector3.Lerp(startScale, endScale, t);

            Color color = sr.color;
            color.a = 1f - t;
            sr.color = color;

            yield return null;
        }

        Destroy(flash);
    }

    // Create once
    private static Sprite CreateSimpleCircleSprite()
    {
        Debug.LogError("HitParticleManager.CreateSimpleCircleSprite() CALLED!");

        int size = 64;
        Texture2D texture = new Texture2D(size, size);

        if (TextureDebugger.Instance != null)
        {
            TextureDebugger.Instance.RegisterTextureCreation("HitParticleManager.CritFlash", size, size);
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
                    float alpha = 1f - (distance / radius);
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
        // Clear cached texture
        if (cachedCritFlashSprite != null && cachedCritFlashSprite.texture != null)
        {
            Destroy(cachedCritFlashSprite.texture);
            cachedCritFlashSprite = null;
        }
    }
}