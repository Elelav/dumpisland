using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class HealthBarUI : MonoBehaviour
{
    [Header("HP Bar")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Armor")] // New!
    [SerializeField] private TextMeshProUGUI armorText; // Optional
    [SerializeField] private GameObject armorIcon; // Optional - armor icon

    void Start()
    {
        if (playerHealth != null)
        {
            // Subscribe to events
            playerHealth.OnHealthChanged.AddListener(UpdateHealthBar);
            playerHealth.OnArmorChanged.AddListener(UpdateArmorDisplay); // New!
        }

        // Hide armor if none
        UpdateArmorDisplay(0);
    }

    private void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;
        if (currentHealth <= 0)
        {
            //healthSlider.GetComponentInChildren<Image>().enabled = false;
            healthSlider.enabled = false;
        }
    }

    // New method: Update armor display
    private void UpdateArmorDisplay(float armor)
    {
        // Update text if available
        if (armorText != null)
        {
            if (armor > 0)
            {
                armorText.text = $"{armor:F0}";
                armorText.gameObject.SetActive(true);
            }
            else
            {
                armorText.gameObject.SetActive(false);
            }
        }

        // If there’s an icon – show/hide it
        if (armorIcon != null)
        {
            armorIcon.SetActive(armor > 0);
        }
    }
}