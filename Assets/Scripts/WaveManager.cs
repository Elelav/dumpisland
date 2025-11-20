using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class WaveManager : MonoBehaviour
{
    [Header("Wave Settings")]
    [SerializeField] private int totalWaves = 10;
    [SerializeField] private float waveDuration = 180f;
    [SerializeField] private float timeBetweenWaves = 15f;
    [SerializeField] private RewardChoiceManager rewardManager;

    private int currentWave = 0;
    private float waveTimer = 0f;
    private bool waveActive = false;

    [Header("Enemy Spawning")]
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private GameObject eliteEnemyPrefab;
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private Transform islandCenter;
    [SerializeField] private float spawnRadius = 12f;
    [SerializeField] private float minSpawnDistance = 8f;

    [Header("Difficulty Progression")]
    [SerializeField] private int baseEnemiesPerWave = 10;
    [SerializeField] private int enemiesIncreasePerWave = 3;

    // Updated: Increased base values
    [SerializeField] private float baseEnemyHealth = 50f; // Was 30
    [SerializeField] private float healthIncreasePerWave = 18f; // Was 10

    // New: Damage and speed modifiers
    [SerializeField] private float baseDamageMultiplier = 1.3f; // +30% damage
    [SerializeField] private float damageIncreasePerWave = 0.05f; // +5% per wave
    [SerializeField] private float baseSpeedMultiplier = 1.15f; // +15% speed
    [SerializeField] private float speedIncreasePerWave = 0.02f; // +2% per wave

    private int maxEnemiesThisWave = 0;
    private int enemiesSpawnedTotal = 0;
    private float respawnCheckInterval = 1f;
    private float lastRespawnCheck = 0f;

    [Header("Events")]
    public UnityEvent<int> OnWaveStart;
    public UnityEvent<int> OnWaveComplete;
    public UnityEvent<float> OnWaveTimerUpdate;
    public UnityEvent OnAllWavesComplete;
    public UnityEvent OnBossSpawn;

    [Header("Visual Effects")]
    [SerializeField] private WaterWaveEffect waterWaveEffect;

    [Header("Tsunami")]
    [SerializeField] private GameObject tsunamiPrefab;
    [SerializeField] private float tsunamiWarningTime = 3f;

    void Start()
    {
        if (islandCenter == null)
        {
            DebugHelper.LogWarning("Island Center not assigned! Creating temporary at (0,0,0)");
            GameObject tempCenter = new GameObject("IslandCenter_TEMP");
            tempCenter.transform.position = Vector3.zero;
            islandCenter = tempCenter.transform;
        }

        Invoke(nameof(StartNextWave), 3f);
    }

    void Update()
    {
        if (!waveActive) return;

        waveTimer -= Time.deltaTime;
        OnWaveTimerUpdate?.Invoke(waveTimer);

        if (Time.time >= lastRespawnCheck + respawnCheckInterval)
        {
            lastRespawnCheck = Time.time;
            CheckAndRespawnEnemies();
        }

        if (waveTimer <= 0)
        {
            EndWave();
        }
    }

    public void StartNextWave()
    {
        currentWave++;

        if (currentWave > totalWaves)
        {
            OnAllWavesComplete?.Invoke();
            DebugHelper.Log("All waves completed! VICTORY!");

            EndlessMode endlessMode = GetComponent<EndlessMode>();
            if (endlessMode != null)
            {
                Invoke(nameof(StartEndless), 5f);
            }

            return;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayWaveStart();
        }

        waveActive = true;
        waveTimer = waveDuration;

        maxEnemiesThisWave = baseEnemiesPerWave + (enemiesIncreasePerWave * (currentWave - 1));
        enemiesSpawnedTotal = 0;

        DebugHelper.Log($"=== WAVE {currentWave}/{totalWaves} STARTED ===");
        DebugHelper.Log($"Max enemies: {maxEnemiesThisWave}");

        OnWaveStart?.Invoke(currentWave);

        if (currentWave > 1)
        {
            StartCoroutine(TsunamiSequence());
        }

        if (currentWave == 10)
        {
            SpawnBoss();
        }
        else if (currentWave % 3 == 0)
        {
            SpawnEliteEnemy();
        }
        if (currentWave == 1)
        {
            if (rewardManager != null)
            {
                rewardManager.ShowRewardChoice(RewardFilterType.OnlyWeapons);
            }
        }
        SpawnWaveInstantly();
    }

    private void SpawnWaveInstantly()
    {
        int enemiesToSpawn = maxEnemiesThisWave;

        if (currentWave % 3 == 0 || currentWave == 10)
        {
            enemiesToSpawn -= 1;
        }

        DebugHelper.Log($"Spawning {enemiesToSpawn} enemies immediately!");

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            SpawnEnemy();
            enemiesSpawnedTotal++;
        }
    }

    private void CheckAndRespawnEnemies()
    {
        GameObject[] aliveEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        int currentEnemyCount = aliveEnemies.Length;

        if (currentEnemyCount < maxEnemiesThisWave)
        {
            int enemiesToRespawn = maxEnemiesThisWave - currentEnemyCount;

            DebugHelper.Log($"Respawn: {currentEnemyCount}/{maxEnemiesThisWave} enemies, adding {enemiesToRespawn}");

            for (int i = 0; i < enemiesToRespawn; i++)
            {
                SpawnEnemy();
                enemiesSpawnedTotal++;
            }
        }
    }

    private void SpawnEnemy()
    {
        if (enemyPrefabs.Length == 0) return;

        Vector2 spawnPosition = GetRandomSpawnPosition();
        GameObject enemyPrefab = GetRandomEnemyPrefab();
        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

        // Apply modifiers
        ApplyEnemyModifiers(enemy, currentWave);
    }

    private void SpawnEliteEnemy()
    {
        if (eliteEnemyPrefab == null) return;

        Vector2 spawnPosition = GetRandomSpawnPosition();
        GameObject elite = Instantiate(eliteEnemyPrefab, spawnPosition, Quaternion.identity);

        DebugHelper.Log("ELITE ENEMY APPEARED!");

        // Apply modifiers with x3 multiplier
        ApplyEnemyModifiers(elite, currentWave, 3f);

        elite.transform.localScale = Vector3.one * 1.8f;
        enemiesSpawnedTotal++;
    }

    private void SpawnBoss()
    {
        if (bossPrefab == null)
        {
            DebugHelper.LogWarning("Boss not assigned!");
            SpawnEliteEnemy();
            return;
        }

        Vector2 spawnPosition = GetRandomSpawnPosition();
        GameObject boss = Instantiate(bossPrefab, spawnPosition, Quaternion.identity);

        DebugHelper.Log("FINAL BOSS!");
        OnBossSpawn?.Invoke();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBossMusic();
        }

        // Apply modifiers with x10 multiplier
        ApplyEnemyModifiers(boss, currentWave, 10f);

        boss.transform.localScale = Vector3.one * 2.5f;
        enemiesSpawnedTotal++;
    }

    // New method: Apply modifiers to enemy
    private void ApplyEnemyModifiers(GameObject enemy, int wave, float healthMultiplier = 1f)
    {
        // Health
        EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            float health = (enemyHealth.maxHealth + (healthIncreasePerWave * (wave - 1))) * healthMultiplier;
            enemyHealth.SetHealth(health);
        }

        // Damage and speed
        EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            // Damage: base multiplier + progression
            float damageMultiplier = baseDamageMultiplier + (damageIncreasePerWave * (wave - 1));

            // Speed: base multiplier + progression
            float speedMultiplier = baseSpeedMultiplier + (speedIncreasePerWave * (wave - 1));

            enemyAI.ApplyWaveModifiers(damageMultiplier, speedMultiplier);

            DebugHelper.Log($"Enemy enhanced: damage x{damageMultiplier:F2}, speed x{speedMultiplier:F2}");
        }
    }

    private GameObject GetRandomEnemyPrefab()
    {
        float roll = Random.value;
        float smallChance = 0.6f;// + (currentWave * 0.03f);
        float mediumChance = 0.3f;// + (currentWave * 0.02f);

        if (roll < smallChance && enemyPrefabs.Length > 2)
        {
            return enemyPrefabs[3];
        }
        else if (roll < smallChance + mediumChance && enemyPrefabs.Length > 1)
        {
            float roll2 = Random.value;
            if (roll2 < 0.5f)
            {
                return enemyPrefabs[0];
            }
            else
            {
                return enemyPrefabs[1];
            }
        }
        else
        {
            return enemyPrefabs[2];
        }
    }

    private Vector2 GetRandomSpawnPosition()
    {
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

    private void EndWave()
    {
        waveActive = false;

        DebugHelper.Log($"=== WAVE {currentWave} COMPLETED ===");
        DebugHelper.Log($"Total enemies spawned: {enemiesSpawnedTotal}");

        OnWaveComplete?.Invoke(currentWave);

        Invoke(nameof(StartNextWave), timeBetweenWaves);

        DebugHelper.Log($"Next wave in {timeBetweenWaves} seconds");
    }

    private IEnumerator WaterWaveEffect()
    {
        if (waterWaveEffect != null)
        {
            waterWaveEffect.PlayWaveAnimation();
        }

        yield return new WaitForSeconds(1f);
    }

    private void StartEndless()
    {
        EndlessMode endlessMode = GetComponent<EndlessMode>();
        if (endlessMode != null)
        {
            endlessMode.StartEndlessMode();
        }
    }

    private IEnumerator TsunamiSequence()
    {
        DebugHelper.Log("TSUNAMI APPROACHING!");

        EventNotificationUI notification = FindObjectOfType<EventNotificationUI>();
        if (notification != null)
        {
            //notification.ShowNotification("TSUNAMI APPROACHING!", tsunamiWarningTime);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayWaveStart();
        }

        yield return new WaitForSeconds(tsunamiWarningTime);

        if (tsunamiPrefab != null)
        {
            GameObject tsunami = Instantiate(tsunamiPrefab);
            TsunamiWave wave = tsunami.GetComponent<TsunamiWave>();

            if (wave != null)
            {
                wave.StartTsunami(islandCenter.position);
            }
        }
        else
        {
            DebugHelper.LogWarning("Tsunami Prefab not assigned!");
        }
    }

    public int GetCurrentWave() => currentWave;
    public int GetTotalWaves() => totalWaves;
    public float GetWaveTimer() => waveTimer;
    public bool IsWaveActive() => waveActive;
}