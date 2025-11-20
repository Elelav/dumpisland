using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LootManager : MonoBehaviour
{
    public static LootManager Instance;

    [Header("Settings")]
    [SerializeField] private int maxLootOnMap = 100;
    [SerializeField] private float magnetCheckInterval = 0.1f;

    [Header("Loot Merging")]
    [SerializeField] private bool enableMerging = true; // Enable/disable merging
    [SerializeField] private float mergeRadius = 4f; // Merge search radius
    [SerializeField] private float mergeInterval = 2f; // Check interval in seconds
    [SerializeField] private int minLootToMerge = 3; // Minimum loot pieces to merge
    [SerializeField] private int maxMergedSize = 50; // Maximum merged loot size

    [Header("Debug")]
    [SerializeField] private int currentLootCount = 0;
    [SerializeField] private int mergeCount = 0; // Total merge count

    private List<Loot> allLoot = new List<Loot>();
    private Transform player;
    private PlayerPerks playerPerks;
    private float lastMagnetCheck = 0f;
    private float lastMergeCheck = 0f;
    private float lastDebugUpdate = 0f;
    private float cachedMagnetRadius = 0f;

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
    }

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerPerks = playerObj.GetComponent<PlayerPerks>();
        }
    }

    void Update()
    {
        float time = Time.time;
        float deltaTime = Time.deltaTime;

        // Update magnet radius once per interval
        if (time - lastMagnetCheck >= magnetCheckInterval)
        {
            lastMagnetCheck = time;

            if (playerPerks != null)
            {
                cachedMagnetRadius = playerPerks.GetMagnetRadius();
            }
        }

        // Periodically merge loot
        if (enableMerging && time - lastMergeCheck >= mergeInterval)
        {
            lastMergeCheck = time;
            MergeLoot();
        }

        // Main loot processing loop
        for (int i = allLoot.Count - 1; i >= 0; i--)
        {
            if (allLoot[i] == null)
            {
                allLoot.RemoveAt(i);
                continue;
            }

            Loot loot = allLoot[i];

            bool isPulling = false;

            if (cachedMagnetRadius > 0 && player != null)
            {
                float distance = Vector2.Distance(loot.transform.position, player.position);

                if (distance <= cachedMagnetRadius)
                {
                    loot.PullToPlayer(player.position, deltaTime);
                    isPulling = true;
                }
            }

            if (!isPulling)
            {
                loot.StopPulling();
            }

            loot.UpdateBobbing(time, deltaTime);
        }

        // Update counter once per second
        if (time - lastDebugUpdate >= 1f)
        {
            lastDebugUpdate = time;
            currentLootCount = allLoot.Count;
        }
    }

    // Merge nearby loot
    private void MergeLoot()
    {
        if (allLoot.Count < minLootToMerge) return;

        // Track already processed loot (avoid double merging)
        HashSet<Loot> processed = new HashSet<Loot>();
        List<Loot> toRemove = new List<Loot>();

        for (int i = 0; i < allLoot.Count; i++)
        {
            Loot centerLoot = allLoot[i];

            if (centerLoot == null || processed.Contains(centerLoot))
                continue;

            // Find all loot within radius
            List<Loot> nearby = new List<Loot>();
            nearby.Add(centerLoot);

            for (int j = i + 1; j < allLoot.Count; j++)
            {
                Loot otherLoot = allLoot[j];

                if (otherLoot == null || processed.Contains(otherLoot))
                    continue;

                float distance = Vector2.Distance(
                    centerLoot.transform.position,
                    otherLoot.transform.position
                );

                if (distance <= mergeRadius)
                {
                    nearby.Add(otherLoot);
                }
            }

            // If found enough loot - merge them
            if (nearby.Count >= minLootToMerge)
            {
                // Calculate center of mass
                Vector3 centerPosition = Vector3.zero;
                int totalSize = 0;

                foreach (Loot loot in nearby)
                {
                    centerPosition += loot.transform.position;
                    totalSize += loot.GetSize();
                    processed.Add(loot);
                    toRemove.Add(loot);
                }

                centerPosition /= nearby.Count;

                // Limit size
                totalSize = Mathf.Min(totalSize, maxMergedSize);

                // Create new merged loot
                if (LootPool.Instance != null)
                {
                    GameObject mergedLoot = LootPool.Instance.GetLoot(centerPosition, totalSize);

                    if (mergedLoot != null)
                    {
                        // Visual merge effect (optional)
                        CreateMergeEffect(centerPosition);

                        mergeCount++;
                        DebugHelper.Log($"Merged {nearby.Count} loot pieces (size {totalSize}) at {centerPosition}");
                    }
                }
            }
        }

        // Remove old loot
        foreach (Loot loot in toRemove)
        {
            if (loot != null && LootPool.Instance != null)
            {
                LootPool.Instance.ReturnLoot(loot.gameObject);
            }
        }
    }

    // Visual merge effect
    private void CreateMergeEffect(Vector3 position)
    {
        GameObject effect = new GameObject("MergeEffect");
        effect.transform.position = position;

        SpriteRenderer sr = effect.AddComponent<SpriteRenderer>();

        if (SpriteCache.Instance != null)
        {
            sr.sprite = SpriteCache.Instance.GetCircleSprite(64);
        }

        sr.color = new Color(1f, 1f, 0.5f, 0.8f); // Golden color
        sr.sortingLayerName = "Items";
        sr.sortingOrder = 5;

        StartCoroutine(MergeEffectAnimation(effect));
    }

    private System.Collections.IEnumerator MergeEffectAnimation(GameObject effect)
    {
        SpriteRenderer sr = effect.GetComponent<SpriteRenderer>();
        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration && effect != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Shrink to center
            effect.transform.localScale = Vector3.one * Mathf.Lerp(2f, 0.5f, t);

            // Fade out
            Color color = sr.color;
            color.a = (1f - t) * 0.8f;
            sr.color = color;

            yield return null;
        }

        Destroy(effect);
    }

    public void RegisterLoot(Loot loot)
    {
        allLoot.Add(loot);

        if (allLoot.Count > maxLootOnMap)
        {
            Loot oldestLoot = allLoot[0];
            allLoot.RemoveAt(0);

            if (oldestLoot != null)
            {
                if (LootPool.Instance != null)
                {
                    LootPool.Instance.ReturnLoot(oldestLoot.gameObject);
                }
                else
                {
                    Destroy(oldestLoot.gameObject);
                }
            }
        }
    }

    public void UnregisterLoot(Loot loot)
    {
        allLoot.Remove(loot);
    }

    public int GetLootCount() => allLoot.Count;

    [ContextMenu("Clear 50% of Loot")]
    public void ClearHalfLoot()
    {
        int countToRemove = allLoot.Count / 2;

        for (int i = 0; i < countToRemove; i++)
        {
            if (allLoot.Count > 0)
            {
                Loot loot = allLoot[0];
                allLoot.RemoveAt(0);

                if (loot != null)
                {
                    Destroy(loot.gameObject);
                }
            }
        }

        DebugHelper.Log($"Cleared {countToRemove} loot. Remaining: {allLoot.Count}");
    }

    [ContextMenu("Count All Loot")]
    void DebugCountAllLoot()
    {
        Loot[] allLootInScene = FindObjectsOfType<Loot>();

        Debug.LogError($"LOOT STATISTICS:");
        Debug.LogError($"In LootManager list: {allLoot.Count}");
        Debug.LogError($"Actually in scene: {allLootInScene.Length}");
        Debug.LogError($"DIFFERENCE: {allLootInScene.Length - allLoot.Count}");
        Debug.LogError($"Merge count: {mergeCount}");
    }

    [ContextMenu("Force Merge Loot Now")]
    public void ForceMerge()
    {
        MergeLoot();
        Debug.Log("Force merge completed!");
    }
}