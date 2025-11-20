using UnityEngine;
using System.Collections.Generic;

public class Projectile : MonoBehaviour
{
    [Header("Parameters")]
    private float damage;
    private float speed = 10f;
    private float lifetime = 5f;
    private bool isCrit = false;
    private string weaponName = "";

    [Header("Visual")]
    private Color projectileColor = Color.white;
    private Vector2 direction;

    [Header("Effects")]
    [SerializeField] private bool createTrail = true;
    [SerializeField] private float trailInterval = 0.05f;

    // Perks
    private PlayerPerks playerPerks;
    private int remainingPierceCount = 0;

    // Two separate lists
    protected List<GameObject> hitEnemies = new List<GameObject>(); // Direct hits
    private List<GameObject> chainedEnemies = new List<GameObject>(); // Hit by lightning

    private float trailTimer = 0f;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        Destroy(gameObject, lifetime);

        // For crits add visual glow effect
        if (isCrit && spriteRenderer != null)
        {
            CreateCritGlow();
        }

        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }
    }

    void Update()
    {
        if (createTrail)
        {
            trailTimer += Time.deltaTime;
            if (trailTimer >= trailInterval)
            {
                CreateTrail();
                trailTimer = 0f;
            }
        }
    }

    public void Initialize(Vector2 dir, float dmg, bool crit, Color color, float projectileSpeed = 10f, PlayerPerks perks = null, string weapon = "")
    {
        direction = dir.normalized;
        damage = dmg;
        isCrit = crit;
        projectileColor = color;
        speed = projectileSpeed;
        playerPerks = perks;
        weaponName = weapon;

        if (playerPerks != null)
        {
            remainingPierceCount = playerPerks.GetMaxPierceCount();
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            // Check if already hit this enemy
            if (hitEnemies.Contains(collision.gameObject))
            {
                return;
            }

            EnemyHealth enemyHealth = collision.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                // New: Call virtual method (can be overridden in derived classes)
                OnEnemyHit(collision, enemyHealth);
            }
        }

        if (collision.gameObject.layer == LayerMask.NameToLayer("Walls"))
        {
            Destroy(gameObject);
        }
    }

    // Chain lightning with separate list
    private void TryChainLightning(Vector3 fromPosition)
    {
        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(fromPosition, 5f);

        int chainCount = 0;
        int maxChains = 3;

        foreach (Collider2D enemy in nearbyEnemies)
        {
            if (!enemy.CompareTag("Enemy")) continue;

            // Check BOTH lists
            // Don't hit those who already received direct hit OR already hit by lightning
            if (hitEnemies.Contains(enemy.gameObject) || chainedEnemies.Contains(enemy.gameObject))
            {
                continue;
            }

            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                float chainDamage = damage * 0.1f;
                enemyHealth.TakeDamage(chainDamage, false);

                // Chain lightning damage
                GameStats stats = GameObject.FindObjectOfType<GameStats>();
                if (stats != null && !string.IsNullOrEmpty(weaponName))
                {
                    stats.AddWeaponDamage(weaponName + " (Chain lightning)", chainDamage);
                }

                CreateLightningEffect(fromPosition, enemy.transform.position);

                // Add to lightning list (NOT to hitEnemies!)
                chainedEnemies.Add(enemy.gameObject);
                chainCount++;

                DebugHelper.Log($"Lightning hit {enemy.name} ({chainDamage:F0} damage)");

                if (chainCount >= maxChains) break;
            }
        }

        if (chainCount > 0)
        {
            DebugHelper.Log($"Chain lightning: {chainCount} bounces!");
        }
    }

    // Handle enemy hit (can be overridden)
    protected virtual void OnEnemyHit(Collider2D enemyCollider, EnemyHealth enemyHealth)
    {
        // Deal damage
        enemyHealth.TakeDamage(damage, isCrit);

        if (isCrit)
        {
            DebugHelper.Log($"Projectile: CRITICAL damage {damage:F0}!");
        }

        // Remember enemy
        hitEnemies.Add(enemyCollider.gameObject);

        // Update damage statistics
        GameStats stats = GameObject.FindObjectOfType<GameStats>();
        if (stats != null)
        {
            stats.AddDamageDealt(damage);
            // Track damage from specific weapon
            if (!string.IsNullOrEmpty(weaponName))
            {
                stats.AddWeaponDamage(weaponName, damage);
            }
        }

        CreateHitEffect(enemyCollider.transform.position);

        // Chain lightning
        if (playerPerks != null && playerPerks.HasPerk("chainLightning"))
        {
            TryChainLightning(enemyCollider.transform.position);
        }

        // Pierce
        if (remainingPierceCount > 0)
        {
            remainingPierceCount--;
            DebugHelper.Log($"Pierce! Remaining: {remainingPierceCount}");
            return; // Don't destroy, keep flying
        }

        Destroy(gameObject);
    }

    private void CreateLightningEffect(Vector3 from, Vector3 to)
    {
        GameObject lightning = new GameObject("Lightning");
        LineRenderer lr = lightning.AddComponent<LineRenderer>();

        lr.startWidth = 0.1f;
        lr.endWidth = 0.1f;
        lr.positionCount = 2;
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);

        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = Color.cyan;
        lr.endColor = Color.white;
        lr.sortingLayerName = "Enemies";
        lr.sortingOrder = 10;

        Destroy(lightning, 0.1f);
    }

    private void CreateHitEffect(Vector3 position)
    {
        GameObject hitEffect = new GameObject("ProjectileHit");
        hitEffect.transform.position = position;

        SpriteRenderer sr = hitEffect.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("UI/Skin/Knob");

        // Use projectile color or yellow for crit
        if (spriteRenderer != null)
        {
            sr.color = isCrit ? Color.yellow : spriteRenderer.color; // Use current sprite color
        }
        else
        {
            sr.color = isCrit ? Color.yellow : Color.white;
        }

        sr.sortingLayerName = "Enemies";
        sr.sortingOrder = 15;

        MonoBehaviour mono = FindObjectOfType<MonoBehaviour>();
        if (mono != null)
        {
            mono.StartCoroutine(HitEffectAnimation(hitEffect));
        }
    }

    private System.Collections.IEnumerator HitEffectAnimation(GameObject effect)
    {
        SpriteRenderer sr = effect.GetComponent<SpriteRenderer>();
        float elapsed = 0f;
        float duration = 0.2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            effect.transform.localScale = Vector3.one * (1f - t) * 1.5f;

            Color color = sr.color;
            color.a = 1f - t;
            sr.color = color;

            yield return null;
        }

        Destroy(effect);
    }

    private void CreateTrail()
    {
        GameObject trail = new GameObject("ProjectileTrail");
        trail.transform.position = transform.position;
        trail.transform.localScale = transform.localScale * 0.7f;
        trail.transform.rotation = transform.rotation;

        SpriteRenderer sr = trail.AddComponent<SpriteRenderer>();
        sr.sprite = spriteRenderer.sprite;
        sr.color = spriteRenderer.color;
        sr.sortingLayerName = "Enemies";
        sr.sortingOrder = 4;

        Destroy(trail, 0.2f);

        MonoBehaviour mono = FindObjectOfType<MonoBehaviour>();
        if (mono != null)
        {
            mono.StartCoroutine(FadeTrail(sr));
        }
    }

    private System.Collections.IEnumerator FadeTrail(SpriteRenderer sr)
    {
        float elapsed = 0f;
        float duration = 0.2f;
        Color startColor = sr.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Color color = startColor;
            color.a = 1f - (elapsed / duration);
            if (sr != null)
            {
                sr.color = color;
            }
            yield return null;
        }
    }

    // Glow effect for critical projectile
    private void CreateCritGlow()
    {
        // Create child glow object
        GameObject glow = new GameObject("CritGlow");
        glow.transform.SetParent(transform);
        glow.transform.localPosition = Vector3.zero;
        glow.transform.localRotation = Quaternion.identity;
        glow.transform.localScale = Vector3.one * 1.5f; // Slightly larger than original

        SpriteRenderer glowRenderer = glow.AddComponent<SpriteRenderer>();

        // Copy original sprite
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            glowRenderer.sprite = spriteRenderer.sprite;
        }

        // Yellow glow
        glowRenderer.color = new Color(1f, 1f, 0f, 0.5f); // Yellow semi-transparent
        glowRenderer.sortingLayerName = spriteRenderer.sortingLayerName;
        glowRenderer.sortingOrder = spriteRenderer.sortingOrder - 1; // Behind original

        // Pulsing
        StartCoroutine(PulseGlow(glow));
    }

    // Glow pulsing animation
    private System.Collections.IEnumerator PulseGlow(GameObject glow)
    {
        float elapsed = 0f;
        Vector3 baseScale = Vector3.one * 1.5f;

        while (glow != null)
        {
            elapsed += Time.deltaTime;
            float pulse = Mathf.Sin(elapsed * 10f) * 0.2f + 1f;
            glow.transform.localScale = baseScale * pulse;
            yield return null;
        }
    }
}