using UnityEngine;
using System.Collections.Generic;

public class InteractableSpawner : MonoBehaviour
{
    [Header("Object Prefabs")]
    [SerializeField] private GameObject burgerPrefab;
    [SerializeField] private GameObject energyDrinkPrefab;
    [SerializeField] private GameObject chestPrefab;
    [SerializeField] private GameObject starPrefab;
    [SerializeField] private GameObject megaMagnetPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private Transform islandCenter; // Map center
    [SerializeField] private float spawnRadius = 10f; // Spawn radius from center
    [SerializeField] private float spawnInterval = 30f; // Spawn interval (every 30 sec)
    [SerializeField] private int maxActiveObjects = 5; // Maximum objects simultaneously

    [Header("Spawn Probabilities")]
    [SerializeField] private float burgerChance = 0.3f; // 30%
    [SerializeField] private float energyDrinkChance = 0.25f; // 25%
    [SerializeField] private float chestChance = 0.2f; // 20%
    [SerializeField] private float starChance = 0.15f; // 15%
    [SerializeField] private float megaMagnetChance = 0.1f; // 10%

    private float nextSpawnTime = 0f;
    private List<GameObject> activeObjects = new List<GameObject>();

    void Start()
    {
        if (islandCenter == null)
        {
            GameObject centerObj = GameObject.Find("IslandCenter");
            if (centerObj != null)
            {
                islandCenter = centerObj.transform;
            }
            else
            {
                DebugHelper.LogWarning("IslandCenter not found for InteractableSpawner!");
                islandCenter = transform; // Use spawner position
            }
        }

        nextSpawnTime = Time.time + spawnInterval;
    }

    void Update()
    {
        // Clear list from destroyed objects
        activeObjects.RemoveAll(obj => obj == null);

        // Check if need to spawn
        if (Time.time >= nextSpawnTime && activeObjects.Count < maxActiveObjects)
        {
            SpawnRandomObject();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    private void SpawnRandomObject()
    {
        // Select random object based on probabilities
        GameObject prefabToSpawn = GetRandomPrefab();

        if (prefabToSpawn == null)
        {
            DebugHelper.LogWarning("Failed to select prefab for spawn!");
            return;
        }

        // Find random position
        Vector2 spawnPos = GetRandomSpawnPosition();

        // Spawn object
        GameObject spawnedObj = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        activeObjects.Add(spawnedObj);

        DebugHelper.Log($"Spawned {spawnedObj.name} at position {spawnPos}");
    }

    private GameObject GetRandomPrefab()
    {
        float roll = Random.value;
        float cumulative = 0f;

        cumulative += burgerChance;
        if (roll < cumulative && burgerPrefab != null) return burgerPrefab;

        cumulative += energyDrinkChance;
        if (roll < cumulative && energyDrinkPrefab != null) return energyDrinkPrefab;

        cumulative += chestChance;
        if (roll < cumulative && chestPrefab != null) return chestPrefab;

        cumulative += starChance;
        if (roll < cumulative && starPrefab != null) return starPrefab;

        cumulative += megaMagnetChance;
        if (roll < cumulative && megaMagnetPrefab != null) return megaMagnetPrefab;

        // If nothing selected - give burger by default
        return burgerPrefab;
    }

    private Vector2 GetRandomSpawnPosition()
    {
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        float distance = Random.Range(0f, spawnRadius);

        return (Vector2)islandCenter.position + randomDirection * distance;
    }
}