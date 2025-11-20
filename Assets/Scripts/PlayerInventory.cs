using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

public class PlayerInventory : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxWeaponSlots = 5;

    private List<Weapon> weapons = new List<Weapon>();

    // Events for UI
    public UnityEvent<List<Weapon>> OnInventoryChanged;

    void Start()
    {
        // Initialize empty slots
        for (int i = 0; i < maxWeaponSlots; i++)
        {
            weapons.Add(null);
        }
    }

    // Add weapon
    public bool AddWeapon(WeaponData weaponData)
    {
        // Check if weapon already exists
        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i] != null && weapons[i].data == weaponData)
            {
                // If weapon can be upgraded — upgrade it
                if (weapons[i].CanLevelUp())
                {
                    weapons[i].LevelUp();
                    OnInventoryChanged?.Invoke(weapons);
                    return true;
                }
                else
                {
                    DebugHelper.Log($"{weaponData.weaponName} is already at max level!");
                    // Optionally convert to money here
                    return false;
                }
            }
        }

        // Find empty slot
        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i] == null)
            {
                // Check weapon type
                Weapon newWeapon;

                if (weaponData.weaponName.Contains("Flamethrower"))
                {
                    // Create special flamethrower class
                    newWeapon = new FlameThrowerWeapon(weaponData);
                    DebugHelper.Log("Flamethrower obtained!");
                }
                else
                {
                    // Regular weapon
                    newWeapon = new Weapon(weaponData);
                }

                weapons[i] = newWeapon;
                DebugHelper.Log($"New weapon obtained: {weaponData.weaponName}");
                OnInventoryChanged?.Invoke(weapons);
                return true;
            }
        }

        DebugHelper.Log("No free slots available!");
        return false;
    }

    // Get all weapons
    public List<Weapon> GetWeapons()
    {
        return weapons;
    }

    // Get count of active weapons
    public int GetActiveWeaponCount()
    {
        int count = 0;
        foreach (Weapon weapon in weapons)
        {
            if (weapon != null) count++;
        }
        return count;
    }
}