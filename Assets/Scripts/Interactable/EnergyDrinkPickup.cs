using UnityEngine;
using System.Collections;

public class EnergyDrinkPickup : InteractableObject
{
    [Header("Energy Drink Settings")]
    [SerializeField] private float speedMultiplier = 1.3f; // +30%
    [SerializeField] private float duration = 10f;

    protected override void OnPickup(GameObject player)
    {
        PlayerController controller = player.GetComponent<PlayerController>();

        if (controller != null)
        {
            DebugHelper.Log($"Drank energy drink! +{(speedMultiplier - 1f) * 100}% speed for {duration} sec");

            // Start coroutine on player
            MonoBehaviour playerMono = player.GetComponent<MonoBehaviour>();
            if (playerMono != null)
            {
                playerMono.StartCoroutine(ApplySpeedBoost(controller, duration));
            }

            // Notification
            EventNotificationUI notification = FindObjectOfType<EventNotificationUI>();
            if (notification != null)
            {
                //notification.ShowNotification($"Speed +{(speedMultiplier - 1f) * 100:F0}%!", duration);
            }
        }
    }

    private IEnumerator ApplySpeedBoost(PlayerController controller, float duration)
    {
        // Save original speed
        float originalSpeed = controller.GetCurrentMoveSpeed();

        // Apply boost
        controller.moveSpeed *= speedMultiplier;

        DebugHelper.Log($"Speed increased: {originalSpeed} to {controller.moveSpeed}");

        // Wait
        yield return new WaitForSeconds(duration);

        // Restore speed (considering it might have changed from perks)
        controller.moveSpeed = originalSpeed;

        DebugHelper.Log($"Speed restored: {controller.moveSpeed}");
    }
}