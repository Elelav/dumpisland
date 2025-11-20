using UnityEngine;

[CreateAssetMenu(fileName = "New Perk", menuName = "Perks/Perk Data")]
public class PerkData : ScriptableObject
{
    [Header("Basic Information")]
    public string perkName = "Perk";
    [TextArea(3, 5)]
    public string description = "Perk description";
    public Sprite icon;

    [Header("Levels")]
    public int maxLevel = 1; // Maximum level (1 = single level)
    public bool isStackable = true; // Can be taken repeatedly

    [Header("Perk Type")]
    public PerkType perkType;

    [Header("Effects (values per 1 level)")]
    public PerkEffects effects;

    [Header("Requirements")]
    public bool requiresRangedWeapon = false; // Needs ranged weapon
    public bool requiresMeleeWeapon = false; // Needs melee weapon
    public bool requiresBothWeaponTypes = false; // Needs both types

    [Header("Visual")]
    public Color perkColor = Color.white;
    public PerkRarity rarity = PerkRarity.Common;

    // Get description with level
    public string GetDescription(int currentLevel)
    {
        string desc = description;

        // Replace placeholders with actual values
        desc = desc.Replace("{level}", currentLevel.ToString());
        desc = desc.Replace("{maxLevel}", maxLevel.ToString());

        return desc;
    }

    // Check requirements
    public bool CheckRequirements(PlayerInventory inventory)
    {
        if (!requiresRangedWeapon && !requiresMeleeWeapon && !requiresBothWeaponTypes)
            return true;

        bool hasRanged = false;
        bool hasMelee = false;

        foreach (Weapon weapon in inventory.GetWeapons())
        {
            if (weapon != null && weapon.data != null)
            {
                if (weapon.data.projectilePrefab != null)
                    hasRanged = true;
                else
                    hasMelee = true;
            }
        }

        if (requiresBothWeaponTypes)
            return hasRanged && hasMelee;

        if (requiresRangedWeapon)
            return hasRanged;

        if (requiresMeleeWeapon)
            return hasMelee;

        return true;
    }
}

// Perk types
public enum PerkType
{
    Damage,        // Damage
    Speed,         // Speed
    Defense,       // Defense/Survival
    Utility,       // Utility
    Economy,       // Economy
    Special        // Special
}

// Perk rarity
public enum PerkRarity
{
    Common,        // Common
    Rare,          // Rare
    Epic,          // Epic
    Legendary      // Legendary
}

// Perk effects (values per 1 level!)
[System.Serializable]
public class PerkEffects
{
    [Header("Damage")]
    public float damageMultiplier = 0f; // +10% = 0.1
    public float meleeDamageMultiplier = 0f;
    public float rangedDamageMultiplier = 0f;
    public float critChanceBonus = 0f; // +20% = 0.2
    public float critMultiplierBonus = 0f; // +150% = 1.5

    [Header("Speed")]
    public float moveSpeedMultiplier = 0f; // +10% = 0.1
    public float attackSpeedMultiplier = 0f; // +10% = 0.1

    [Header("Defense")]
    public float maxHealthBonus = 0f; // +50 = 50
    public float maxHealthMultiplier = 0f; // +150% = 1.5
    public float healthRegenPerSecond = 0f;
    public float damageTakenMultiplier = 0f; // +50% = 0.5, -30% = -0.3
    public float dodgeChance = 0f; // 5% = 0.05
    public float armorBonus = 0f;

    [Header("Sizes")]
    public float attackRangeMultiplier = 0f; // +10% = 0.1
    public float projectileCountBonus = 0f; // +1 = 1

    [Header("Economy")]
    public float garbageValueMultiplier = 0f; // +20% = 0.2
    public float shopDiscountMultiplier = 0f; // -10% = 0.1
    public float expMultiplier = 0f; // +10% = 0.1
    public float bagCapacityBonus = 0f; // +5 = 5
    public float bagCapacityMultiplier = 0f; // +10% = 0.1

    [Header("Special Effects")]
    public bool canPierce = false; // Piercing
    public int pierceCount = 0; // Number of enemies to pierce (0 = infinite at max level)
    public bool hasChainLightning = false; // Chain lightning
    public bool hasKnockback = false; // Knockback
    public bool hasMagnet = false; // Magnet
    public float magnetRadius = 0f; // Magnet radius
    public bool hasGuardianAngel = false; // Guardian angel
    public bool canPhaseThrough = false; // Phase through enemies
    public bool grantsExtraRewardSlots = false; // Grants extra choice slots
    public int extraRewardSlots = 1; // How many slots it adds

    [Header("Universal - all stats")]
    public float allStatsMultiplier = 0f; // +5% = 0.05
}