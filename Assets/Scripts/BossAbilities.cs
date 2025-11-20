using UnityEngine;
using System.Collections;

public class BossAbilities : MonoBehaviour
{
    [Header("Abilities")]
    [SerializeField] private float shockwaveCooldown = 8f;
    [SerializeField] private float chargeCooldown = 12f;
    [SerializeField] private float summonCooldown = 15f;

    [Header("Shockwave")]
    [SerializeField] private float shockwaveRange = 8f;
    [SerializeField] private float shockwaveDamage = 25f;

    [Header("Charge")]
    [SerializeField] private float chargeSpeed = 10f;
    [SerializeField] private float chargeDuration = 1f;
    [SerializeField] private float chargeDamage = 40f;

    [Header("Summon")]
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private int summonCount = 3;

    private Transform player;
    private EnemyAI enemyAI;
    private Rigidbody2D rb;

    private float lastShockwave = 0f;
    private float lastCharge = 0f;
    private float lastSummon = 0f;

    private bool isCharging = false;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        enemyAI = GetComponent<EnemyAI>();
        rb = GetComponent<Rigidbody2D>();

        lastShockwave = Time.time;
        lastCharge = Time.time + 3f;
        lastSummon = Time.time + 5f;
    }

    void Update()
    {
        if (player == null || isCharging) return;

        float distance = Vector2.Distance(transform.position, player.position);

        // 1. Shockwave
        if (Time.time >= lastShockwave + shockwaveCooldown && distance <= shockwaveRange * 1.2f)
        {
            StartCoroutine(Shockwave());
            lastShockwave = Time.time;
        }

        // 2. Charge
        if (Time.time >= lastCharge + chargeCooldown && distance > 5f && distance < 15f)
        {
            StartCoroutine(Charge());
            lastCharge = Time.time;
        }

        // 3. Summon enemies
        if (Time.time >= lastSummon + summonCooldown)
        {
            SummonEnemies();
            lastSummon = Time.time;
        }
    }

    // Ability 1: Shockwave
    private IEnumerator Shockwave()
    {
        DebugHelper.Log("BOSS: SHOCKWAVE!");

        Vector3 originalScale = transform.localScale;

        // Contract
        for (float t = 0; t < 0.3f; t += Time.deltaTime)
        {
            transform.localScale = Vector3.Lerp(originalScale, originalScale * 0.8f, t / 0.3f);
            yield return null;
        }

        // Expand
        for (float t = 0; t < 0.2f; t += Time.deltaTime)
        {
            transform.localScale = Vector3.Lerp(originalScale * 0.8f, originalScale * 1.2f, t / 0.2f);
            yield return null;
        }

        transform.localScale = originalScale;

        // Create wave
        StartCoroutine(ShockwaveVisual());

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBossAbility();
        }

        // Damage player
        float distance = Vector2.Distance(transform.position, player.position);
        if (distance <= shockwaveRange)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(shockwaveDamage);
            }
        }
    }

    private IEnumerator ShockwaveVisual()
    {
        GameObject wave = new GameObject("Shockwave");
        wave.transform.position = transform.position;

        SpriteRenderer sr = wave.AddComponent<SpriteRenderer>();

        // Fixed: Using SpriteCache!
        if (SpriteCache.Instance != null)
        {
            sr.sprite = SpriteCache.Instance.GetCircleSprite(128); // Large sprite for wave
        }
        else
        {
            // Fallback: try to load from Resources
            sr.sprite = Resources.Load<Sprite>("UI/Skin/Knob");

            // If failed to load - create programmatically
            if (sr.sprite == null)
            {
                Debug.LogWarning("BossAbilities: Failed to load wave sprite! Creating programmatically.");
                sr.sprite = CreateCircleSprite(128);
            }
        }

        sr.color = new Color(1f, 0.3f, 0f, 0.8f);
        sr.sortingLayerName = "Enemies";
        sr.sortingOrder = 5;

        float elapsed = 0f;
        float duration = 0.8f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float scale = Mathf.Lerp(0, shockwaveRange * 2.5f, t);
            wave.transform.localScale = Vector3.one * scale;

            Color color = sr.color;
            color.a = (1f - t) * 0.8f; // Slightly more transparent
            sr.color = color;

            yield return null;
        }

        Destroy(wave);
    }

    // Ability 2: Charge
    private IEnumerator Charge()
    {
        DebugHelper.Log("BOSS: CHARGE!");

        isCharging = true;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBossAbility();
        }

        if (enemyAI != null) enemyAI.enabled = false;

        Vector2 direction = (player.position - transform.position).normalized;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Color originalColor = sr.color;
        sr.color = Color.red;

        float elapsed = 0f;
        while (elapsed < chargeDuration)
        {
            rb.linearVelocity = direction * chargeSpeed;
            elapsed += Time.deltaTime;

            if (Random.value > 0.7f)
            {
                CreateChargeTrail();
            }

            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        sr.color = originalColor;

        if (enemyAI != null) enemyAI.enabled = true;

        isCharging = false;
    }

    private void CreateChargeTrail()
    {
        GameObject trail = new GameObject("ChargeTrail");
        trail.transform.position = transform.position;
        trail.transform.localScale = transform.localScale * 0.8f;

        SpriteRenderer sr = trail.AddComponent<SpriteRenderer>();
        sr.sprite = GetComponent<SpriteRenderer>().sprite;
        sr.color = new Color(1f, 0f, 0f, 0.5f);
        sr.sortingLayerName = "Enemies";

        Destroy(trail, 0.3f);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isCharging && collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(chargeDamage);
                DebugHelper.Log($"Boss rammed player! Damage: {chargeDamage}");
            }
        }
    }

    // Ability 3: Summon enemies
    private void SummonEnemies()
    {
        if (enemyPrefabs.Length == 0) return;

        DebugHelper.Log("BOSS: SUMMONING ENEMIES!");

        for (int i = 0; i < summonCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * 3f;
            Vector3 spawnPos = transform.position + new Vector3(offset.x, offset.y, 0);

            GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

            StartCoroutine(SummonEffect(spawnPos));
        }
    }

    private IEnumerator SummonEffect(Vector3 position)
    {
        GameObject effect = new GameObject("SummonEffect");
        effect.transform.position = position;

        SpriteRenderer sr = effect.AddComponent<SpriteRenderer>();

        // Fixed: Using SpriteCache!
        if (SpriteCache.Instance != null)
        {
            sr.sprite = SpriteCache.Instance.GetCircleSprite(64);
        }
        else
        {
            // Fallback: try to load from Resources
            sr.sprite = Resources.Load<Sprite>("UI/Skin/Knob");

            // If failed to load - create programmatically
            if (sr.sprite == null)
            {
                Debug.LogWarning("BossAbilities: Failed to load summon sprite! Creating programmatically.");
                sr.sprite = CreateCircleSprite(64);
            }
        }

        sr.color = new Color(0.5f, 0f, 0.5f, 0.8f);
        sr.sortingLayerName = "Enemies";

        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.5f;

            effect.transform.localScale = Vector3.one * (3f - t * 3f);

            Color color = sr.color;
            color.a = (1f - t) * 0.8f;
            sr.color = color;

            yield return null;
        }

        Destroy(effect);
    }

    // New: Create circle sprite programmatically (fallback)
    private Sprite CreateCircleSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = distance < radius ? 1f - (distance / radius) : 0f;
                texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
        }
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, shockwaveRange);
    }
}