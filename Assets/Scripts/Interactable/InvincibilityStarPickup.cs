using UnityEngine;
using System.Collections;

public class InvincibilityStarPickup : InteractableObject
{
    [Header("Star Settings")]
    [SerializeField] private float invincibilityDuration = 5f;

    protected override void OnPickup(GameObject player)
    {
        DebugHelper.Log($"Picked up star! Invincibility for {invincibilityDuration} sec");

        MonoBehaviour playerMono = player.GetComponent<MonoBehaviour>();
        if (playerMono != null)
        {
            playerMono.StartCoroutine(ApplyInvincibility(player, invincibilityDuration));
        }

        // Notification
        EventNotificationUI notification = FindObjectOfType<EventNotificationUI>();
        if (notification != null)
        {
            //notification.ShowNotification($"INVINCIBILITY!", invincibilityDuration);
        }
    }

    private IEnumerator ApplyInvincibility(GameObject player, float duration)
    {
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        SpriteRenderer sprite = player.GetComponent<SpriteRenderer>();

        if (health == null) yield break;

        // Save original color
        Color originalColor = sprite != null ? sprite.color : Color.white;
        Color invincibleColor = new Color(1f, 1f, 0.5f, 1f); // Golden

        // Make invincible (temporary hack - give huge HP)
        float originalHP = health.GetArmor();
        float tempArmor = 999999f;
        health.UpdateArmor(tempArmor);

        float elapsed = 0f;

        // Golden flashing effect
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            if (sprite != null)
            {
                sprite.color = Color.Lerp(originalColor, invincibleColor, Mathf.PingPong(Time.time * 5f, 1f));
            }

            yield return null;
        }

        // Restore armor
        health.UpdateArmor(originalHP);

        // Restore color
        if (sprite != null)
        {
            sprite.color = originalColor;
        }

        DebugHelper.Log("Invincibility ended");
    }
}