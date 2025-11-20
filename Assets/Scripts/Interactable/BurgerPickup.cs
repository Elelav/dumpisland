using UnityEngine;

public class BurgerPickup : InteractableObject
{
    [Header("Burger Settings")]
    [SerializeField] private float healAmount = 50f;

    protected override void OnPickup(GameObject player)
    {
        PlayerHealth health = player.GetComponent<PlayerHealth>();

        if (health != null)
        {
            health.Heal(healAmount);
            DebugHelper.Log($"Ate burger! +{healAmount} HP");

            // Visual effect
            if (DamageNumberManager.Instance != null)
            {
                DamageNumberManager.Instance.ShowDamage(healAmount, transform.position, false);
            }
        }
    }
}