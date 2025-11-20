using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Achievement", menuName = "Achievements/Achievement")]
public class Achievment : ScriptableObject
{
    [Header("Basic Information")]
    public string description = "Description";
    public Sprite icon;

    [Header("Requirements")]
    public bool requiresOnlyRangedWeapon = false;
    public bool requiresOnlyMeleeWeapon = false;
    public bool requiresNoHit = false;
    public bool requiresRich = false;
    public int moneyNeeded = 0;
    public bool requiresOnlyOneWeapon = false;
    public bool requiresLevel = false;
    public int levelNeeded = 0;

    public bool isAchievmentCompleted(PlayerInventory playerInventory, GameStats gameStats, PlayerHealth playerHealth)
    {
        List<Weapon> weapons = playerInventory.GetWeapons();
        if (requiresOnlyRangedWeapon)
        {
            foreach (Weapon weapon in weapons)
            {
                if (weapon == null) return false;
                if (weapon.data.weaponName.Contains("(M)")) { return false; }                
            }
            return true;
        }
        if (requiresOnlyMeleeWeapon)
        {
            foreach (Weapon weapon in weapons)
            {
                if (weapon == null) return false;
                if (weapon.data.weaponName.Contains("(R)") || weapon.data.weaponName.Contains("(S)")) { return false; }                
            }
            return true;
        }
        if (requiresNoHit)
        {
            return playerHealth.IsNoHitViable();
        }
        if (requiresRich)
        {
            if (gameStats.GetMoneyEarned() >= moneyNeeded) return true;
            return false;
        }
        if (requiresOnlyOneWeapon)
        {
            if (weapons.Count > 1) return false;
            return true;
        }
        if (requiresLevel)
        {
            if (gameStats.GetPlayerLevel() < levelNeeded) return false;
            return true;
        }
        return false;
    }

}