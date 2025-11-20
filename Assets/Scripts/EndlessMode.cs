using UnityEngine;
using System.Collections;

public class EndlessMode : MonoBehaviour
{
    [Header("Endless Mode Settings")]
    [SerializeField] private bool isActive = false;
    [SerializeField] private float difficultyIncreaseInterval = 30f;

    [Header("Progression")]
    [SerializeField] private float healthMultiplier = 1.2f; // x1.2 every 30 sec
    [SerializeField] private float speedMultiplier = 1.05f; // +5% speed
    [SerializeField] private float damageMultiplier = 1.1f; // New: +10% damage every 30 sec
    [SerializeField] private int baseMaxEnemies = 15;
    [SerializeField] private int maxEnemiesIncrease = 5;

    [Header("Prefabs")]
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private GameObject elitePrefab;
    [SerializeField] private GameObject bossPrefab;

    [Header("Dependencies")]
    [SerializeField] private Transform islandCenter;
    [SerializeField] private float spawnRadius = 12f;
    [SerializeField] private float minSpawnDistance = 8f;

    [Header("Tsunami")]
    [SerializeField] private GameObject tsunamiPrefab;
    [SerializeField] private float tsunamiInterval = 60f;
    private float tsunamiTimer = 0f;

    private int maxEnemiesForDifficulty = 15;
    private float respawnCheckInterval = 1f;
    private float lastRespawnCheck = 0f;

    private float difficultyTimer = 0f;
    private int difficultyLevel = 1;

    // Updated: Added damage variable
    private float currentEnemyHealth = 50f;
    private float currentEnemySpeed = 3f;
    private float currentEnemyDamageMultiplier = 1.3f; // New: Starting damage multiplier (+30%)

    void Update()
    {
        if (!isActive) return;

        difficultyTimer += Time.deltaTime;

        if (difficultyTimer >= difficultyIncreaseInterval)
        {
            IncreaseDifficulty();
            difficultyTimer = 0f;
        }

        tsunamiTimer += Time.deltaTime;

        if (tsunamiTimer >= tsunamiInterval)
        {
            tsunamiTimer = 0f;
            StartCoroutine(TsunamiSequence());
        }

        if (Time.time >= lastRespawnCheck + respawnCheckInterval)
        {
            lastRespawnCheck = Time.time;
            CheckAndRespawnEnemies();
        }
    }

    public void StartEndlessMode()
    {
        DebugHelper.Log("=== ENDLESS MODE ACTIVATED! ===");

        if (islandCenter == null)
        {
            DebugHelper.LogWarning("Island Center not assigned for EndlessMode! Looking for existing...");

            GameObject centerObj = GameObject.Find("IslandCenter");
            if (centerObj != null)
            {
                islandCenter = centerObj.transform;
                DebugHelper.Log("Found IslandCenter!");
            }
            else
            {
                DebugHelper.LogError("IslandCenter not found! Create an object 'IslandCenter' at map center!");
                islandCenter = new GameObject("IslandCenter_TEMP").transform;
                islandCenter.position = Vector3.zero;
            }
        }

        isActive = true;
        difficultyLevel = 1;
        difficultyTimer = 0f;

        currentEnemyHealth = 50f;
        currentEnemySpeed = 3f;
        currentEnemyDamageMultiplier = 1.3f; // New: Reset damage
        maxEnemiesForDifficulty = baseMaxEnemies;

        SpawnWave();

        DebugHelper.Log($"Max enemies: {maxEnemiesForDifficulty}");
        DebugHelper.Log($"Damage multiplier: x{currentEnemyDamageMultiplier:F2}");
    }

    public void StopEndlessMode()
    {
        isActive = false;
        DebugHelper.Log("Endless mode stopped");
    }

    private void IncreaseDifficulty()
    {
        difficultyLevel++;

        currentEnemyHealth *= healthMultiplier;
        currentEnemySpeed *= speedMultiplier;
        currentEnemyDamageMultiplier *= damageMultiplier; // New: Increase damage
        maxEnemiesForDifficulty += maxEnemiesIncrease;

        DebugHelper.Log($"=== DIFFICULTY INCREASED! Level: {difficultyLevel} ===");
        DebugHelper.Log($"Enemy HP: {currentEnemyHealth:F0}");
        DebugHelper.Log($"Speed: {currentEnemySpeed:F1}");
        DebugHelper.Log($"Damage multiplier: x{currentEnemyDamageMultiplier:F2}"); // New: Logging
        DebugHelper.Log($"Max enemies: {maxEnemiesForDifficulty}");

        if (difficultyLevel % 3 == 0)
        {
            SpawnElite();
        }

        if (difficultyLevel % 5 == 0)
        {
            SpawnBoss();
        }
    }

    private void SpawnWave()
    {
        int enemiesToSpawn = maxEnemiesForDifficulty;

        DebugHelper.Log($"Endless: spawning {enemiesToSpawn} enemies IMMEDIATELY!");

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            SpawnEnemy();
        }
    }

    private void CheckAndRespawnEnemies()
    {
        GameObject[] aliveEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        int currentEnemyCount = aliveEnemies.Length;

        if (currentEnemyCount < maxEnemiesForDifficulty)
        {
            int enemiesToRespawn = maxEnemiesForDifficulty - currentEnemyCount;
            enemiesToRespawn = Mathf.Min(enemiesToRespawn, 5);

            DebugHelper.Log($"Endless respawn: {currentEnemyCount}/{maxEnemiesForDifficulty} enemies, adding {enemiesToRespawn}");

            for (int i = 0; i < enemiesToRespawn; i++)
            {
                SpawnEnemy();
            }
        }
    }

    private void SpawnEnemy()
    {
        if (enemyPrefabs.Length == 0) return;

        Vector2 spawnPosition = GetRandomSpawnPosition();
        GameObject enemyPrefab = GetRandomEnemyPrefab();
        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

        // Updated: Apply all modifiers
        ApplyEnemyModifiers(enemy, 1f);
    }

    private void SpawnElite()
    {
        if (elitePrefab == null) return;

        Vector2 spawnPosition = GetRandomSpawnPosition();
        GameObject elite = Instantiate(elitePrefab, spawnPosition, Quaternion.identity);

        DebugHelper.Log($"Elite enemy! (Difficulty level: {difficultyLevel})");

        // Updated: Multiplier x3
        ApplyEnemyModifiers(elite, 3f);

        elite.transform.localScale = Vector3.one * 1.8f;
    }

    private void SpawnBoss()
    {
        if (bossPrefab == null) return;

        Vector2 spawnPosition = GetRandomSpawnPosition();
        GameObject boss = Instantiate(bossPrefab, spawnPosition, Quaternion.identity);

        DebugHelper.Log($"BOSS! (Difficulty level: {difficultyLevel})");

        // Updated: Multiplier x10
        ApplyEnemyModifiers(boss, 10f);

        boss.transform.localScale = Vector3.one * 2.5f;
    }

    // New method: Apply all modifiers
    private void ApplyEnemyModifiers(GameObject enemy, float healthMultiplier)
    {
        // Health
        EnemyHealth health = enemy.GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.SetHealth(currentEnemyHealth * healthMultiplier);
        }

        // Damage and speed through EnemyAI
        EnemyAI ai = enemy.GetComponent<EnemyAI>();
        if (ai != null)
        {
            // Speed varies slightly
            float speedVariation = currentEnemySpeed * (healthMultiplier > 1f ? 1.2f : 1f);

            // Apply modifiers
            ai.ApplyWaveModifiers(currentEnemyDamageMultiplier, speedVariation / 3f); // Divide by base speed ~3

            DebugHelper.Log($"Endless enemy: HP={currentEnemyHealth * healthMultiplier:F0}, damage x{currentEnemyDamageMultiplier:F2}, speed={speedVariation:F1}");
        }
    }

    private GameObject GetRandomEnemyPrefab()
    {
        float roll = Random.value;
        float largeChance = Mathf.Min(0.1f + (difficultyLevel * 0.05f), 0.5f);
        float mediumChance = Mathf.Min(0.3f + (difficultyLevel * 0.03f), 0.4f);

        if (roll < largeChance && enemyPrefabs.Length > 2)
        {
            return enemyPrefabs[2];
        }
        else if (roll < largeChance + mediumChance && enemyPrefabs.Length > 1)
        {
            return enemyPrefabs[1];
        }
        else
        {
            return enemyPrefabs[0];
        }
    }

    private Vector2 GetRandomSpawnPosition()
    {
        if (islandCenter == null)
        {
            DebugHelper.LogError("IslandCenter is null!");
            return Vector2.zero;
        }

        Vector2 spawnPosition;
        int attempts = 0;

        do
        {
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            float distance = Random.Range(minSpawnDistance, spawnRadius);
            spawnPosition = (Vector2)islandCenter.position + randomDirection * distance;

            attempts++;
            if (attempts > 20) break;

        } while (Vector2.Distance(spawnPosition, islandCenter.position) < minSpawnDistance);

        return spawnPosition;
    }

    private IEnumerator TsunamiSequence()
    {
        DebugHelper.Log("TSUNAMI IN ENDLESS MODE!");

        EventNotificationUI notification = FindObjectOfType<EventNotificationUI>();
        if (notification != null)
        {
            // notification.ShowNotification("TSUNAMI!", 2f);
        }

        yield return new WaitForSeconds(2f);

        if (tsunamiPrefab != null)
        {
            GameObject tsunami = Instantiate(tsunamiPrefab);
            TsunamiWave wave = tsunami.GetComponent<TsunamiWave>();

            if (wave != null && islandCenter != null)
            {
                wave.StartTsunami(islandCenter.position);
            }
        }
    }

    public bool IsActive() => isActive;
    public int GetDifficultyLevel() => difficultyLevel;
    public int GetMaxEnemies() => maxEnemiesForDifficulty;
}