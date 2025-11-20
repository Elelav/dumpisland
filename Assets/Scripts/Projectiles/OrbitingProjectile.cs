using UnityEngine;
using System.Collections.Generic;

public class OrbitingProjectile : MonoBehaviour
{
    [Header("Parameters")]
    private float damage = 10f;
    private bool isCrit = false;
    private string weaponName = "";

    [Header("Orbit")]
    [SerializeField] private float orbitRadius = 2f;
    [SerializeField] private float orbitSpeed = 180f; // degrees per second

    // New: Lifetime and spawn animation
    private float lifetime = 5f;
    public float aliveTime = 0f;
    private bool isSpawning = true; // Spawn animation
    private float spawnDuration = 0.3f;
    private float spawnElapsed = 0f;

    private int remainingPierceCount = 0;
    private List<GameObject> hitEnemies = new List<GameObject>();
    private PlayerPerks playerPerks;

    private Transform player;
    private float currentAngle = 0f;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    void Update()
    {
        if (player == null)
        {
            Destroy(gameObject);
            return;
        }

        // New: Spawn animation (flying out from character)
        if (isSpawning)
        {
            spawnElapsed += Time.deltaTime;
            float t = spawnElapsed / spawnDuration;

            if (t >= 1f)
            {
                isSpawning = false;
                t = 1f;
            }

            // Smooth fly out from center to orbit
            float currentRadius = Mathf.Lerp(0f, orbitRadius, EaseOutCubic(t));

            // Also smooth appearance (transparency and size)
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = Mathf.Lerp(0f, 1f, t);
                spriteRenderer.color = color;
            }

            transform.localScale = Vector3.one * Mathf.Lerp(4f, 4f, 4f);

            // Position at current radius
            float radians = currentAngle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(
                Mathf.Cos(radians) * currentRadius,
                Mathf.Sin(radians) * currentRadius,
                0f
            );

            transform.position = player.position + offset;

            // Slow rotation during spawn
            transform.Rotate(0f, 0f, orbitSpeed * Time.deltaTime * 0.3f);

            return; // Don't process the rest during spawn
        }

        // Lifetime countdown
        if (lifetime > 0)
        {
            aliveTime += Time.deltaTime;

            // New: Fade before disappearing
            if (aliveTime >= lifetime - 0.5f && spriteRenderer != null)
            {
                float fadeTime = 0.5f;
                float timeLeft = lifetime - aliveTime;
                float alpha = timeLeft / fadeTime;

                Color color = spriteRenderer.color;
                color.a = Mathf.Clamp01(alpha);
                spriteRenderer.color = color;
            }

            if (aliveTime >= lifetime)
            {
                DebugHelper.Log($"Star disappeared (lived {aliveTime:F1}s)");
                Destroy(gameObject);
                return;
            }
        }

        // Normal orbit rotation
        currentAngle += orbitSpeed * Time.deltaTime;
        if (currentAngle >= 360f)
            currentAngle -= 360f;

        float rad = currentAngle * Mathf.Deg2Rad;
        Vector3 orbitOffset = new Vector3(
            Mathf.Cos(rad) * orbitRadius,
            Mathf.Sin(rad) * orbitRadius,
            0f
        );

        transform.position = player.position + orbitOffset;
        transform.Rotate(0f, 0f, orbitSpeed * Time.deltaTime * 0.5f);
    }

    public void Initialize(float dmg, bool crit, Color color, PlayerPerks perks, float startAngle = 0f, string weapon = "", float lifeTime = 0f)
    {
        damage = dmg;
        isCrit = crit;
        playerPerks = perks;
        currentAngle = startAngle;
        weaponName = weapon;
        lifetime = lifeTime;

        if (playerPerks != null)
        {
            remainingPierceCount = playerPerks.GetMaxPierceCount();
        }

        // New: Start with transparency
        if (spriteRenderer != null)
        {
            Color col = spriteRenderer.color;
            col.a = 0f;
            spriteRenderer.color = col;
        }

        if (isCrit && spriteRenderer != null)
        {
            CreateCritGlow();
        }

        DebugHelper.Log($"Orbital star: damage={dmg}, angle={startAngle}°, pierces={remainingPierceCount}, lifetime={lifetime:F1}s");
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // Don't deal damage during spawn
        if (isSpawning) return;

        if (collision.CompareTag("Enemy"))
        {
            if (hitEnemies.Contains(collision.gameObject))
            {
                return;
            }

            EnemyHealth enemyHealth = collision.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage, isCrit);
                hitEnemies.Add(collision.gameObject);
                CreateHitEffect(collision.transform.position);

                GameStats stats = GameObject.FindObjectOfType<GameStats>();
                if (stats != null)
                {
                    stats.AddDamageDealt(damage);

                    if (!string.IsNullOrEmpty(weaponName))
                    {
                        stats.AddWeaponDamage(weaponName, damage);
                    }
                }

                DebugHelper.Log($"Star hit {collision.name} ({damage} damage), pierces remaining: {remainingPierceCount}");

                if (remainingPierceCount > 0)
                {
                    remainingPierceCount--;
                    return;
                }

                Destroy(gameObject);
            }
        }
    }

    // Ease function for smooth animation
    private float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    private void CreateCritGlow()
    {
        GameObject glow = new GameObject("CritGlow");
        glow.transform.SetParent(transform);
        glow.transform.localPosition = Vector3.zero;
        glow.transform.localRotation = Quaternion.identity;
        glow.transform.localScale = Vector3.one * 1.3f;

        SpriteRenderer glowRenderer = glow.AddComponent<SpriteRenderer>();

        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            glowRenderer.sprite = spriteRenderer.sprite;
        }

        glowRenderer.color = new Color(1f, 1f, 0f, 0.5f);
        glowRenderer.sortingLayerName = spriteRenderer.sortingLayerName;
        glowRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;

        StartCoroutine(PulseGlow(glow));
    }

    private System.Collections.IEnumerator PulseGlow(GameObject glow)
    {
        float elapsed = 0f;
        Vector3 baseScale = Vector3.one * 1.3f;

        while (glow != null)
        {
            elapsed += Time.deltaTime;
            float pulse = Mathf.Sin(elapsed * 10f) * 0.2f + 1f;
            glow.transform.localScale = baseScale * pulse;
            yield return null;
        }
    }

    private void CreateHitEffect(Vector3 position)
    {
        GameObject hitEffect = new GameObject("StarHit");
        hitEffect.transform.position = position;

        SpriteRenderer sr = hitEffect.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("UI/Skin/Knob");

        if (spriteRenderer != null)
        {
            sr.color = isCrit ? Color.yellow : spriteRenderer.color;
        }
        else
        {
            sr.color = isCrit ? Color.yellow : Color.magenta;
        }

        sr.sortingLayerName = "Enemies";
        sr.sortingOrder = 15;

        Destroy(hitEffect, 0.2f);

        StartCoroutine(HitEffectAnimation(hitEffect));
    }

    private System.Collections.IEnumerator HitEffectAnimation(GameObject effect)
    {
        SpriteRenderer sr = effect.GetComponent<SpriteRenderer>();
        float elapsed = 0f;
        float duration = 0.2f;

        while (elapsed < duration && effect != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            effect.transform.localScale = Vector3.one * (1f - t) * 1.2f;

            Color color = sr.color;
            color.a = 1f - t;
            sr.color = color;

            yield return null;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            Gizmos.color = new Color(1f, 0f, 1f, 0.3f);
            Gizmos.DrawWireSphere(player.position, orbitRadius);
        }
    }
}