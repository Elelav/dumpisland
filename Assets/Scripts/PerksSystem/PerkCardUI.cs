using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PerkCardUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image cardIcon;
    [SerializeField] private TextMeshProUGUI cardName;
    [SerializeField] private TextMeshProUGUI cardLevel;
    [SerializeField] private TextMeshProUGUI cardDescription;
    [SerializeField] private TextMeshProUGUI cardRarity;
    [SerializeField] private Button cardButton;
    [SerializeField] private Image cardBackground;

    [Header("Reward Type Colors")]
    [SerializeField] private Color weaponColor = new Color(0.8f, 0.4f, 0.1f); // Orange
    [SerializeField] private Color commonColor = new Color(0.5f, 0.5f, 0.5f);
    [SerializeField] private Color rareColor = new Color(0.3f, 0.6f, 1f);
    [SerializeField] private Color epicColor = new Color(0.8f, 0.3f, 1f);
    [SerializeField] private Color legendaryColor = new Color(1f, 0.7f, 0f);

    private System.Action onSelectedCallback;

    // Setup for PERK
    public void SetupPerk(PerkData perkData, int currentLevel, System.Action onSelected)
    {
        if (perkData == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        onSelectedCallback = onSelected;

        // Name
        if (cardName != null)
        {
            cardName.text = perkData.perkName;
        }

        // Icon
        if (cardIcon != null && perkData.icon != null)
        {
            cardIcon.sprite = perkData.icon;
            cardIcon.enabled = true;
        }
        else if (cardIcon != null)
        {
            cardIcon.enabled = false;
        }

        // Level
        if (cardLevel != null)
        {
            if (currentLevel == 0)
            {
                cardLevel.text = $"NEW PERK!";
                cardLevel.color = Color.green;
            }
            else if (currentLevel >= perkData.maxLevel)
            {
                cardLevel.text = "MAX";
                cardLevel.color = Color.red;
            }
            else
            {
                cardLevel.text = $"Lv. {currentLevel} to {currentLevel + 1}";
                cardLevel.color = Color.yellow;
            }
        }

        // Description
        if (cardDescription != null)
        {
            cardDescription.text = perkData.GetDescription(currentLevel + 1);
        }

        // Rarity/Type
        if (cardRarity != null)
        {
            Color rarityColor = GetRarityColor(perkData.rarity);
            cardRarity.text = "PERK: " + GetRarityText(perkData.rarity);
            cardRarity.color = rarityColor;

            if (cardName != null)
            {
                cardName.color = rarityColor;
            }
        }

        // Button
        if (cardButton != null)
        {
            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(() => onSelectedCallback?.Invoke());
        }
    }

    // Setup for WEAPON
    public void SetupWeapon(WeaponData weaponData, int currentLevel, System.Action onSelected)
    {
        if (weaponData == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        onSelectedCallback = onSelected;

        // Name
        if (cardName != null)
        {
            cardName.text = weaponData.weaponName;
            cardName.color = weaponColor;
        }

        // Icon
        if (cardIcon != null && weaponData.icon != null)
        {
            cardIcon.sprite = weaponData.icon;
            cardIcon.enabled = true;
        }
        else if (cardIcon != null)
        {
            cardIcon.enabled = false;
        }

        // Level
        if (cardLevel != null)
        {
            if (currentLevel == 0)
            {
                cardLevel.text = $"NEW WEAPON!";
                cardLevel.color = Color.green;
            }
            else if (currentLevel >= 10)
            {
                cardLevel.text = "MAX (10)";
                cardLevel.color = Color.red;
            }
            else
            {
                cardLevel.text = $"Lv. {currentLevel} to {currentLevel + 1}";
                cardLevel.color = Color.yellow;
            }
        }

        // Description (weapon stats)
        if (cardDescription != null)
        {
            int nextLevel = currentLevel + 1;
            string desc = $"Damage {weaponData.GetDamage(nextLevel):F0}\n" +
                         $"Attack Speed {weaponData.GetAttackSpeed(nextLevel):F1}\n" +
                         $"Range {weaponData.GetRange(nextLevel):F1}\n" +
                         $"Crit Chance {weaponData.GetCritChance(nextLevel) * 100:F0}%";

            cardDescription.text = desc;
        }

        // Type
        if (cardRarity != null)
        {
            cardRarity.text = "Weapon";
            cardRarity.color = weaponColor;
        }

        // Button
        if (cardButton != null)
        {
            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(() => onSelectedCallback?.Invoke());
        }
    }

    private Color GetRarityColor(PerkRarity rarity)
    {
        switch (rarity)
        {
            case PerkRarity.Common: return commonColor;
            case PerkRarity.Rare: return rareColor;
            case PerkRarity.Epic: return epicColor;
            case PerkRarity.Legendary: return legendaryColor;
            default: return Color.white;
        }
    }

    private string GetRarityText(PerkRarity rarity)
    {
        switch (rarity)
        {
            case PerkRarity.Common: return "Common";
            case PerkRarity.Rare: return "Rare";
            case PerkRarity.Epic: return "Epic";
            case PerkRarity.Legendary: return "Legendary";
            default: return "";
        }
    }
}