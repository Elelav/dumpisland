using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

public class PlayerPerks : MonoBehaviour
{
    [Header("Active Perks")]
    [SerializeField] private List<ActivePerk> activePerks = new List<ActivePerk>();

    [Header("Max Perks")]
    [SerializeField] public int maxPerks = 50; // Can be increased

    // Events
    public UnityEvent<PerkData, int> OnPerkAdded; // (perk, level)
    public UnityEvent<PerkData, int> OnPerkLevelUp; // (perk, new level)

    // References to other systems (cached)
    private PlayerHealth playerHealth;
    private PlayerController playerController;
    private PlayerInventory playerInventory;
    private GarbageBag garbageBag;

    void Start()
    {
        // Cache components
        playerHealth = GetComponent<PlayerHealth>();
        playerController = GetComponent<PlayerController>();
        playerInventory = GetComponent<PlayerInventory>();
        garbageBag = GetComponent<GarbageBag>();
    }

    // Add or upgrade perk
    public bool AddPerk(PerkData perkData)
    {
        if (perkData == null) return false;

        // Check requirements
        if (!perkData.CheckRequirements(playerInventory))
        {
            DebugHelper.Log($"Requirements not met for perk {perkData.perkName}");
            return false;
        }

        // Look for existing perk
        ActivePerk existingPerk = activePerks.Find(p => p.data == perkData);

        if (existingPerk != null)
        {
            // Perk already exists - try to level up
            if (existingPerk.level < perkData.maxLevel)
            {
                existingPerk.level++;
                DebugHelper.Log($"Perk {perkData.perkName} upgraded to level {existingPerk.level}");

                ApplyPerkEffects(perkData, 1); // Apply effects for 1 level
                OnPerkLevelUp?.Invoke(perkData, existingPerk.level);
                return true;
            }
            else
            {
                DebugHelper.Log($"Perk {perkData.perkName} is already max level!");
                return false;
            }
        }
        else
        {
            // New perk
            if (activePerks.Count >= maxPerks)
            {
                DebugHelper.Log("Maximum perks reached!");
                return false;
            }

            ActivePerk newPerk = new ActivePerk(perkData, 1);
            activePerks.Add(newPerk);

            DebugHelper.Log($"Obtained new perk: {perkData.perkName}");

            ApplyPerkEffects(perkData, 1);
            OnPerkAdded?.Invoke(perkData, 1);
            return true;
        }
    }

    // Apply perk effects (for specified number of levels)
    private void ApplyPerkEffects(PerkData perk, int levels)
    {
        PerkEffects effects = perk.effects;

        // Apply effects multiplied by number of levels
        // Effects will be applied through GetTotalMultiplier during calculations

        // Special one-time effects
        if (effects.hasGuardianAngel && levels > 0)
        {
            // Guardian angel - add component
            //if (GetComponent<GuardianAngel>() == null)
            //{
            //    gameObject.AddComponent<GuardianAngel>();
            //}
        }

        if (effects.grantsExtraRewardSlots && levels > 0)
        {
            RewardChoiceManager rewardManager = FindObjectOfType<RewardChoiceManager>();
            if (rewardManager != null)
            {
                rewardManager.AddBonusSlots(effects.extraRewardSlots * levels);
            }
        }

        // Update player base stats
        UpdatePlayerStats();
    }

    // Update player base stats (called after getting perk)
    private void UpdatePlayerStats()
    {
        // Update max HP
        if (playerHealth != null)
        {
            float hpBonus = GetTotalStatBonus("maxHealth");
            float hpMultiplier = GetTotalStatMultiplier("maxHealth");

            // Apply (will be implemented in PlayerHealth)
            playerHealth.UpdateMaxHealth(hpBonus, hpMultiplier);

            float armorBonus = GetTotalStatBonus("armor");
            playerHealth.UpdateArmor(armorBonus);

            float regenBonus = GetTotalBonus("healthRegen");
            DebugHelper.Log($"UpdatePlayerStats: regenBonus = {regenBonus}"); // Debug
            playerHealth.UpdateHealthRegen(regenBonus);
        }

        // Update move speed
        if (playerController != null)
        {
            float speedMultiplier = GetTotalStatMultiplier("moveSpeed");
            playerController.UpdateMoveSpeed(speedMultiplier);
        }

        // Update bag capacity
        if (garbageBag != null)
        {
            float capacityBonus = GetTotalStatBonus("bagCapacity");
            float capacityMultiplier = GetTotalStatMultiplier("bagCapacity");

            garbageBag.UpdateCapacity(capacityBonus, capacityMultiplier);
        }
    }

    // Get total stat bonus (additive)
    public float GetTotalStatBonus(string statName)
    {
        float total = 0f;

        foreach (ActivePerk perk in activePerks)
        {
            PerkEffects effects = perk.data.effects;
            int level = perk.level;

            switch (statName)
            {
                case "maxHealth":
                    total += effects.maxHealthBonus * level;
                    break;
                case "bagCapacity":
                    total += effects.bagCapacityBonus * level;
                    break;
                case "armor":
                    total += effects.armorBonus * level;
                    break;
            }
        }

        return total;
    }

    // Get total stat multiplier (multiplicative)
    public float GetTotalStatMultiplier(string statName)
    {
        float multiplier = 1f;

        foreach (ActivePerk perk in activePerks)
        {
            PerkEffects effects = perk.data.effects;
            int level = perk.level;

            // Check conditions for Universal
            bool universalActive = false;
            if (effects.allStatsMultiplier > 0 && perk.data.requiresBothWeaponTypes)
            {
                universalActive = perk.data.CheckRequirements(playerInventory);
            }

            switch (statName)
            {
                case "maxHealth":
                    multiplier *= (1f + effects.maxHealthMultiplier * level);
                    if (universalActive)
                        multiplier *= (1f + effects.allStatsMultiplier * level);
                    break;

                case "moveSpeed":
                    multiplier *= (1f + effects.moveSpeedMultiplier * level);
                    if (universalActive)
                        multiplier *= (1f + effects.allStatsMultiplier * level);
                    break;

                case "attackSpeed":
                    multiplier *= (1f + effects.attackSpeedMultiplier * level);
                    if (universalActive)
                        multiplier *= (1f + effects.allStatsMultiplier * level);
                    break;

                case "damage":
                    multiplier *= (1f + effects.damageMultiplier * level);
                    if (universalActive)
                        multiplier *= (1f + effects.allStatsMultiplier * level);
                    break;

                case "meleeDamage":
                    multiplier *= (1f + effects.meleeDamageMultiplier * level);
                    multiplier *= (1f + effects.damageMultiplier * level); // General damage also counted
                    if (universalActive)
                        multiplier *= (1f + effects.allStatsMultiplier * level);
                    break;

                case "rangedDamage":
                    multiplier *= (1f + effects.rangedDamageMultiplier * level);
                    multiplier *= (1f + effects.damageMultiplier * level);
                    if (universalActive)
                        multiplier *= (1f + effects.allStatsMultiplier * level);
                    break;

                case "damageTaken":
                    multiplier *= (1f + effects.damageTakenMultiplier * level);
                    break;

                case "bagCapacity":
                    multiplier *= (1f + effects.bagCapacityMultiplier * level);
                    break;

                case "garbageValue":
                    multiplier *= (1f + effects.garbageValueMultiplier * level);
                    break;

                case "shopDiscount":
                    multiplier *= (1f - effects.shopDiscountMultiplier * level);
                    break;

                case "exp":
                    multiplier *= (1f + effects.expMultiplier * level);
                    break;

                case "attackRange":
                    multiplier *= (1f + effects.attackRangeMultiplier * level);
                    if (universalActive)
                        multiplier *= (1f + effects.allStatsMultiplier * level);
                    break;
            }
        }

        return multiplier;
    }

    // Get stat bonus (used for non-multipliers)
    public float GetTotalBonus(string statName)
    {
        float total = 0f;

        foreach (ActivePerk perk in activePerks)
        {
            PerkEffects effects = perk.data.effects;
            int level = perk.level;

            switch (statName)
            {
                case "critChance":
                    total += effects.critChanceBonus * level;
                    break;
                case "critMultiplier":
                    total += effects.critMultiplierBonus * level;
                    break;
                case "dodgeChance":
                    total += effects.dodgeChance * level;
                    break;
                case "projectileCount":
                    total += effects.projectileCountBonus * level;
                    break;
                case "healthRegen":
                    total += effects.healthRegenPerSecond * level;
                    break;
            }
        }

        return total;
    }

    // Check special effects
    public bool HasPerk(string effectName)
    {
        foreach (ActivePerk perk in activePerks)
        {
            PerkEffects effects = perk.data.effects;

            switch (effectName)
            {
                case "chainLightning":
                    if (effects.hasChainLightning) return true;
                    break;
                case "knockback":
                    if (effects.hasKnockback) return true;
                    break;
                case "phaseThrough":
                    if (effects.canPhaseThrough) return true;
                    break;
                case "guardianAngel":
                    if (effects.hasGuardianAngel) return true;
                    break;
            }
        }

        return false;
    }

    // Get perk level
    public int GetPerkLevel(PerkData perkData)
    {
        ActivePerk perk = activePerks.Find(p => p.data == perkData);
        return perk != null ? perk.level : 0;
    }

    // Get list of all active perks
    public List<ActivePerk> GetActivePerks()
    {
        return activePerks;
    }

    // Get max pierce count
    public int GetMaxPierceCount()
    {
        int maxPierce = 0;
        bool hasUnlimitedPierce = false;

        foreach (ActivePerk perk in activePerks)
        {
            if (perk.data.effects.canPierce)
            {
                int pierceCount = perk.data.effects.pierceCount * perk.level;

                // Check max level of Piercing (5 levels = infinite pierce)
                if (perk.level >= perk.data.maxLevel && pierceCount == 0)
                {
                    hasUnlimitedPierce = true;
                }
                else
                {
                    maxPierce += pierceCount;
                }
            }
        }

        return hasUnlimitedPierce ? 999 : maxPierce;
    }

    // Get magnet radius
    public float GetMagnetRadius()
    {
        float radius = 0f;
        bool hasMagnetPerk = false;

        foreach (ActivePerk perk in activePerks)
        {
            if (perk.data.effects.hasMagnet)
            {
                hasMagnetPerk = true;

                // Base radius + increase per level
                // For example: level 1 = 2.0, level 2 = 2.2, level 3 = 2.4 etc.
                float baseRadius = perk.data.effects.magnetRadius;
                float radiusPerLevel = baseRadius * 0.15f; // +15% per level

                radius += baseRadius + (radiusPerLevel * (perk.level - 1));

                DebugHelper.Log($"Magnet: base radius={baseRadius}, level={perk.level}, total={radius}");
            }
        }

        if (!hasMagnetPerk && radius == 0)
        {
            // DebugHelper.Log("Magnet perk not active");
        }

        return radius;
    }

    // Remove perk (for one-time effects like Angel)
    public void RemovePerk(string effectName)
    {
        ActivePerk toRemove = null;

        foreach (ActivePerk perk in activePerks)
        {
            if (perk.data.effects.hasGuardianAngel && effectName == "guardianAngel")
            {
                toRemove = perk;
                break;
            }
        }

        if (toRemove != null)
        {
            activePerks.Remove(toRemove);
            DebugHelper.Log($"Perk {toRemove.data.perkName} removed (one-time effect triggered)");
        }
    }

    // Check for Guardian Angel presence
    public bool HasGuardianAngel()
    {
        foreach (ActivePerk perk in activePerks)
        {
            if (perk.data.effects.hasGuardianAngel)
            {
                return true;
            }
        }
        return false;
    }

    // Remove Guardian Angel (one-time effect)
    public void RemoveGuardianAngel()
    {
        ActivePerk toRemove = null;

        foreach (ActivePerk perk in activePerks)
        {
            if (perk.data.effects.hasGuardianAngel)
            {
                toRemove = perk;
                break;
            }
        }

        if (toRemove != null)
        {
            activePerks.Remove(toRemove);
            DebugHelper.Log($"Perk '{toRemove.data.perkName}' removed (one-time effect triggered)");

            // Visual effect can be added later
        }
    }

}

// Class for storing active perk
[System.Serializable]
public class ActivePerk
{
    public PerkData data;
    public int level;

    public ActivePerk(PerkData perkData, int startLevel = 1)
    {
        data = perkData;
        level = startLevel;
    }
}