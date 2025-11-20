using UnityEngine;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private WeaponSlotUI[] weaponSlots;

    void Start()
    {
        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged.AddListener(UpdateUI);
        }

        UpdateUI(playerInventory.GetWeapons());
    }

    private void UpdateUI(List<Weapon> weapons)
    {
        for (int i = 0; i < weaponSlots.Length; i++)
        {
            if (i < weapons.Count)
            {
                weaponSlots[i].SetWeapon(weapons[i]);
            }
            else
            {
                weaponSlots[i].SetWeapon(null);
            }
        }
    }
}