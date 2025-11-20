using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject[] enemyPrefabs; // Array of enemy prefabs
    [SerializeField] private Transform player;
    [SerializeField] private float spawnRadius = 10f; // Distance around the player to spawn
    [SerializeField] private int initialEnemyCount = 5; // How many enemies to spawn at start

    [Header("Test Settings")]
    [SerializeField] private bool spawnOnStart = true;

    void Start()
    {
        if (spawnOnStart)
        {
            SpawnEnemies(initialEnemyCount);
        }
    }

    public void SpawnEnemies(int count)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnEnemy();
        }
    }

    private void SpawnEnemy()
    {
        // Random position around player
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        Vector2 spawnPosition = (Vector2)player.position + randomDirection * spawnRadius;

        if (enemyPrefabs.Length > 0)
        {
            GameObject randomEnemy = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            Instantiate(randomEnemy, spawnPosition, Quaternion.identity);
        }
    }

    // Test: press E to spawn an enemy
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            SpawnEnemy();
            DebugHelper.Log("Enemy spawned!");
        }
    }
}