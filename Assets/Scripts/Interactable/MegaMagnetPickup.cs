using UnityEngine;

public class MegaMagnetPickup : InteractableObject
{
    [Header("Mega Magnet Settings")]
    [SerializeField] private float magnetRange = 5000f; // Huge radius
    [SerializeField] private float pullSpeed = 30f;

    protected override void OnPickup(GameObject player)
    {
        DebugHelper.Log("Mega magnet! Pulling all loot!");

        // Find ALL loot objects on the map
        Loot[] allLoot = FindObjectsOfType<Loot>();

        int count = 0;
        MonoBehaviour playerMono = player.GetComponent<MonoBehaviour>();

        if (playerMono == null)
        {
            DebugHelper.LogError("Player doesn't have MonoBehaviour for coroutines!");
            return;
        }

        foreach (Loot loot in allLoot)
        {
            if (loot == null) continue;

            float distance = Vector2.Distance(loot.transform.position, player.transform.position);

            if (distance <= magnetRange)
            {
                // FIXED: Starting coroutine ON PLAYER, not on magnet!
                playerMono.StartCoroutine(PullLootToPlayer(loot, player.transform));
                count++;
            }
        }

        DebugHelper.Log($"Pulling {count} loot objects!");

        // Notification
        EventNotificationUI notification = FindObjectOfType<EventNotificationUI>();
        if (notification != null)
        {
            //notification.ShowNotification($"{count} loot being pulled!", 2f);
        }
    }

    private static System.Collections.IEnumerator PullLootToPlayer(Loot loot, Transform player)
    {
        if (loot == null || player == null) yield break;

        float pullSpeed = 30f; // Pull speed
        float arrivalDistance = 0.3f; // Arrival distance to player

        // Wait until loot reaches player or is destroyed
        while (loot != null && player != null)
        {
            // Calculate direction to player
            Vector3 targetPos = player.position;
            float distance = Vector2.Distance(loot.transform.position, targetPos);

            // If already close - stop
            if (distance < arrivalDistance)
            {
                DebugHelper.Log($"Loot reached player: {loot.name}");

                // Update position to stop bobbing
                loot.UpdateStartPosition(loot.transform.position);
                yield break;
            }

            // Direction to player
            Vector3 direction = (targetPos - loot.transform.position).normalized;

            // Move loot
            float step = pullSpeed * Time.deltaTime;
            loot.transform.position += direction * step;

            // Update startPosition for bobbing
            loot.UpdateStartPosition(loot.transform.position);

            yield return null;
        }

        // If loot was destroyed (picked up)
        if (loot == null)
        {
            DebugHelper.Log("Loot was picked up during pull");
        }
    }
}