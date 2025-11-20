using UnityEngine;

public class ChestPickup : InteractableObject
{
    [Header("Chest Settings")]
    [SerializeField] private int minMoney = 50;
    [SerializeField] private int maxMoney = 150;
    [SerializeField] private float chanceForPerk = 0.3f; // 30% chance for perk
    [SerializeField] private float chanceForWeapon = 0.2f; // 20% chance for weapon

    protected override void OnPickup(GameObject player)
    {
        DebugHelper.Log("Opened chest!");

        float roll = Random.value;

        if (roll < chanceForPerk)
        {
            // Give random perk
            GiveRandomPerk(player);
        }
        else if (roll < chanceForPerk + chanceForWeapon)
        {
            // Give random weapon
            GiveRandomWeapon(player);
        }
        else
        {
            // Give money
            GiveMoney(player);
        }
    }

    private void GiveRandomPerk(GameObject player)
    {
        RewardChoiceManager rewardManager = FindObjectOfType<RewardChoiceManager>();

        if (rewardManager != null)
        {
            // Show perk selection window (like on level up)
            rewardManager.ShowRewardChoice();
            DebugHelper.Log("Chest gave perk choice!");
        }
        else
        {
            // If no system - give money
            GiveMoney(player);
        }
    }

    private void GiveRandomWeapon(GameObject player)
    {
        RewardChoiceManager rewardManager = FindObjectOfType<RewardChoiceManager>();

        if (rewardManager != null)
        {
            rewardManager.ShowRewardChoice();
            DebugHelper.Log("Chest gave weapon choice!");
        }
        else
        {
            GiveMoney(player);
        }
    }

    private void GiveMoney(GameObject player)
    {
        PlayerMoney money = player.GetComponent<PlayerMoney>();

        if (money != null)
        {
            int amount = Random.Range(minMoney, maxMoney + 1);
            money.AddMoney(amount);

            DebugHelper.Log($"Chest gave {amount} money!");

            // Notification
            EventNotificationUI notification = FindObjectOfType<EventNotificationUI>();
            if (notification != null)
            {
                //notification.ShowNotification($"+{amount} money!", 2f);
            }
        }
    }
}