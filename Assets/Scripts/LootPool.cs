using UnityEngine;
using System.Collections.Generic;

public class LootPool : MonoBehaviour
{
    public static LootPool Instance;

    [SerializeField] private GameObject lootPrefab;
    [SerializeField] private int initialPoolSize = 50;
    [SerializeField] private int maxPoolSize = 200;

    private Queue<GameObject> pool = new Queue<GameObject>();
    private HashSet<GameObject> activeLoots = new HashSet<GameObject>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Pre-create pool
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewLootObject();
        }

        Debug.Log($"LootPool created: {pool.Count} objects");
    }

    private GameObject CreateNewLootObject()
    {
        GameObject loot = Instantiate(lootPrefab);
        loot.SetActive(false);
        loot.transform.SetParent(transform);
        pool.Enqueue(loot);
        return loot;
    }

    public GameObject GetLoot(Vector3 position, int size)
    {
        GameObject loot;

        if (pool.Count > 0)
        {
            loot = pool.Dequeue();
        }
        else
        {
            if (activeLoots.Count >= maxPoolSize)
            {
                Debug.LogWarning("LootPool is full! Not creating new loot.");
                return null;
            }

            loot = CreateNewLootObject();
            pool.Dequeue(); // Remove from queue (as just added)
        }

        loot.transform.position = position;
        loot.transform.SetParent(null);
        loot.SetActive(true);

        Loot lootComponent = loot.GetComponent<Loot>();
        if (lootComponent != null)
        {
            lootComponent.SetSize(size);
        }

        activeLoots.Add(loot);

        return loot;
    }

    public void ReturnLoot(GameObject loot)
    {
        if (loot == null) return;

        activeLoots.Remove(loot);

        loot.SetActive(false);
        loot.transform.SetParent(transform);

        pool.Enqueue(loot);
    }

    public int GetActiveCount() => activeLoots.Count;
    public int GetPoolCount() => pool.Count;
}