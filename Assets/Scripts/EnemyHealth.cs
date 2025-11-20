using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] public float maxHealth = 30f;
    private float currentHealth;

    [Header("Loot")]
    [SerializeField] private GameObject lootPrefab; // Can stay as fallback
    [SerializeField] private int lootCount = 1; // How many loot items to drop
    [SerializeField] private int lootSize = 1; // Size of each loot (for bag)
    [SerializeField] private float lootSpreadRadius = 0.5f; // Radius for spread

    [Header("Enemy Settings")]
    [SerializeField] private EnemyType enemyType = EnemyType.Small;

    [Header("Rewards")]
    [SerializeField] private float expReward = 10f;

    [Header("Scaling Settings")]
    [SerializeField] private bool useAutoScale = false;

    public enum EnemyType
    {
        Small,   // Example: 1 loot of size 1
        Medium,  // Example: 2 loot items of size 1
        Large    // Example: 3 loot items of size 2
    }

    private SpriteRenderer spriteRenderer;

    void Start()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Automatically set loot based on type
        switch (enemyType)
        {
            case EnemyType.Small:
                lootCount = 1;
                lootSize = 1;
                if (useAutoScale)
                {
                    transform.localScale = Vector3.one * 0.8f;
                }
                break;
            case EnemyType.Medium:
                lootCount = 2;
                lootSize = 1;
                if (useAutoScale)
                {
                    transform.localScale = Vector3.one * 1.2f;
                }
                break;
            case EnemyType.Large:
                lootCount = 3;
                lootSize = 2;
                if (useAutoScale)
                {
                    transform.localScale = Vector3.one * 1.5f;
                }
                break;
        }
    }

    public void TakeDamage(float damage, bool isCrit = false)
    {
        currentHealth -= damage;

        if (DamageNumberManager.Instance != null)
        {
            DamageNumberManager.Instance.ShowDamage(damage, transform.position, isCrit);
        }

        if (HitParticleManager.Instance != null)
        {
            // if (isCrit) { HitParticleManager.Instance.CreateCritEffect(transform.position); }
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEnemyHit();
        }

        StartCoroutine(DamageFlash());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private System.Collections.IEnumerator DamageFlash()
    {
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originalColor;
    }

    private void Die()
    {
        CreateDeathParticles();

        if (AudioManager.Instance != null)
        {
            // AudioManager.Instance.PlayEnemyDeath();
        }

        GameStats stats = GameObject.FindObjectOfType<GameStats>();
        if (stats != null)
        {
            stats.AddEnemyKilled();
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            PlayerExperience playerExp = playerObj.GetComponent<PlayerExperience>();
            if (playerExp != null)
            {
                playerExp.AddExperience(expReward);
            }
        }

        // Updated: spawn multiple loot with spread
        SpawnLoot();

        Destroy(gameObject);
    }

    private void SpawnLoot()
    {
        for (int i = 0; i < lootCount; i++)
        {
            // Random position within spread radius
            Vector2 randomOffset = Random.insideUnitCircle * lootSpreadRadius;
            Vector3 spawnPosition = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0);

            if (LootPool.Instance != null)
            {
                LootPool.Instance.GetLoot(spawnPosition, lootSize);
            }
            else
            {
                // Fallback if pool not found
                if (lootPrefab != null)
                {
                    GameObject loot = Instantiate(lootPrefab, spawnPosition, Quaternion.identity);
                    Loot lootComponent = loot.GetComponent<Loot>();
                    if (lootComponent != null)
                    {
                        lootComponent.SetSize(lootSize);
                    }
                }
            }
        }
    }

    private void CreateDeathParticles()
    {
        int particleCount = Mathf.RoundToInt(transform.localScale.x * 8f);
        particleCount = Mathf.Clamp(particleCount, 3, 30);

        for (int i = 0; i < particleCount; i++)
        {
            GameObject particle = new GameObject("DeathParticle");
            particle.transform.position = transform.position;

            DeathParticle particleScript = particle.AddComponent<DeathParticle>();

            Vector2 direction = Random.insideUnitCircle.normalized;
            float speed = Random.Range(2f, 5f);

            particleScript.Initialize(
                direction * speed,
                spriteRenderer.color,
                spriteRenderer.sprite
            );

            particle.transform.localScale = Vector3.one * Random.Range(0.3f, 0.8f);
        }
    }

    public void SetHealth(float health)
    {
        maxHealth = health;
        currentHealth = health;
    }
}