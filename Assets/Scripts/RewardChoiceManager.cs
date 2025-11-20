using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.UI;

// Reward filter type
public enum RewardFilterType
{
    All,         // Everything (perks + weapons)
    OnlyPerks,   // Perks only
    OnlyWeapons  // Weapons only
}

public class RewardChoiceManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private TextMeshProUGUI levelUpText;
    [SerializeField] private PerkCardUI[] rewardCards = new PerkCardUI[5];
    [SerializeField] private Button rerollButton;
    [SerializeField] private Button emergencyHPButton;
    [SerializeField] private TextMeshProUGUI rerollButtonText;

    [Header("Settings")]
    [SerializeField] private int baseRewardSlots = 3; // Base number of slots
    [SerializeField] private int bonusSlotsFromPerks = 0; // Bonus slots from perks
    [SerializeField] private int baseRerollCost = 50; // Base reroll cost
    [SerializeField] private int emergencyHPAmount = 10;

    [Header("Layout Settings")]
    [SerializeField] private float cardSpacing = 290f; // Distance between card centers

    [Header("Pools")]
    [SerializeField] private List<PerkData> allPerks = new List<PerkData>();
    [SerializeField] private List<WeaponData> allWeapons = new List<WeaponData>();

    [Header("Dependencies")]
    [SerializeField] private PlayerPerks playerPerks;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerExperience playerExperience;
    [SerializeField] private PlayerMoney playerMoney;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private AudioManager audioManager;

    private List<RewardOption> currentOptions = new List<RewardOption>();
    private int currentRerollCost; // Current reroll cost
    private RewardFilterType currentFilter = RewardFilterType.All; // Current filter

    void Start()
    {
        currentRerollCost = baseRerollCost; // Initialize

        if (choicePanel != null)
        {
            choicePanel.SetActive(false);
        }

        // Subscribe to level up event
        if (playerExperience != null)
        {
            playerExperience.OnShowWeaponChoice.RemoveAllListeners();
            playerExperience.OnShowWeaponChoice.AddListener(ShowRewardChoice);
        }

        // Setup buttons
        if (rerollButton != null)
        {
            rerollButton.onClick.AddListener(OnRerollClicked);
        }

        if (emergencyHPButton != null)
        {
            emergencyHPButton.onClick.AddListener(OnEmergencyHPClicked);
        }
    }

    // Main method (for backward compatibility)
    public void ShowRewardChoice()
    {
        ShowRewardChoice(RewardFilterType.All);
    }

    // New overload: with filter
    public void ShowRewardChoice(RewardFilterType filter)
    {
        DebugHelper.Log($"=== Showing reward selection (filter: {filter}) ===");

        if (playerHealth.GetCurrentHealth() <= 0) { return; }

        // Save filter
        currentFilter = filter;

        // Reset reroll cost
        currentRerollCost = baseRerollCost;

        // Number of slots
        int slotsToShow = baseRewardSlots + bonusSlotsFromPerks;
        slotsToShow = Mathf.Clamp(slotsToShow, 1, 5);

        // Generate options considering filter
        GenerateRewardOptions(slotsToShow, filter);

        // Check BEFORE showing panel
        if (currentOptions.Count == 0)
        {
            DebugHelper.Log("Everything maxed out! Granting 10 HP without UI.");

            if (playerHealth != null)
            {
                playerHealth.Heal(10);
            }

            // Don't show panel
            return;
        }

        // Update UI
        UpdateUI();

        // Show panel
        choicePanel.SetActive(true);

        // Pause game
        Time.timeScale = 0f;

        // Update level text
        if (levelUpText != null && playerExperience != null)
        {
            levelUpText.text = $"Level {playerExperience.GetLevel()}";
        }
    }

    // Legacy signature (for backward compatibility)
    private void GenerateRewardOptions(int count)
    {
        GenerateRewardOptions(count, RewardFilterType.All);
    }

    // New version: with filter
    private void GenerateRewardOptions(int count, RewardFilterType filter)
    {
        currentOptions.Clear();

        // Check slot availability
        int activeWeapons = playerInventory != null ? playerInventory.GetActiveWeaponCount() : 5;
        int activePerks = playerPerks != null ? playerPerks.GetActivePerks().Count : 0;
        int maxPerks = playerPerks != null ? playerPerks.maxPerks : 10;

        bool weaponSlotsAvailable = activeWeapons < 5;
        bool perkSlotsAvailable = activePerks < maxPerks;

        DebugHelper.Log($"Slots: Weapons {activeWeapons}/5, Perks {activePerks}/{maxPerks}, Filter: {filter}");

        List<RewardOption> availableRewards = new List<RewardOption>();

        // Add perks only if filter allows
        if (filter == RewardFilterType.All || filter == RewardFilterType.OnlyPerks)
        {
            foreach (PerkData perk in allPerks)
            {
                if (perk == null) continue;

                if (!perk.CheckRequirements(playerInventory))
                    continue;

                int currentLevel = playerPerks != null ? playerPerks.GetPerkLevel(perk) : 0;

                // Upgrade existing perk
                if (currentLevel > 0 && currentLevel < perk.maxLevel)
                {
                    availableRewards.Add(new RewardOption { perkData = perk, currentLevel = currentLevel });
                }
                // New perk (if slots available)
                else if (currentLevel == 0 && perkSlotsAvailable)
                {
                    availableRewards.Add(new RewardOption { perkData = perk, currentLevel = currentLevel });
                }
            }
        }

        // Add weapons only if filter allows
        if (filter == RewardFilterType.All || filter == RewardFilterType.OnlyWeapons)
        {
            foreach (WeaponData weapon in allWeapons)
            {
                if (weapon == null) continue;

                int currentLevel = GetWeaponLevel(weapon);

                // Upgrade existing weapon
                if (currentLevel > 0 && currentLevel < 10)
                {
                    availableRewards.Add(new RewardOption { weaponData = weapon, currentLevel = currentLevel });
                }
                // New weapon (if slots available)
                else if (currentLevel == 0 && weaponSlotsAvailable)
                {
                    availableRewards.Add(new RewardOption { weaponData = weapon, currentLevel = currentLevel });
                }
            }
        }

        DebugHelper.Log($"Available rewards: {availableRewards.Count}");

        // If nothing to offer - exit
        if (availableRewards.Count == 0)
        {
            DebugHelper.Log($"Nothing to offer for filter {filter}!");
            return;
        }

        // Guarantee weapon only if filter is All and slots available
        if (filter == RewardFilterType.All && weaponSlotsAvailable)
        {
            var weaponOptions = availableRewards.Where(r => r.IsWeapon()).ToList();
            if (weaponOptions.Count > 0)
            {
                var guaranteedWeapon = weaponOptions[Random.Range(0, weaponOptions.Count)];
                currentOptions.Add(guaranteedWeapon);
                availableRewards.Remove(guaranteedWeapon);
                count--;
            }
        }

        // Fill remaining slots
        for (int i = 0; i < count && availableRewards.Count > 0; i++)
        {
            var chosen = availableRewards[Random.Range(0, availableRewards.Count)];
            currentOptions.Add(chosen);
            availableRewards.Remove(chosen);
        }

        DebugHelper.Log($"Generated {currentOptions.Count} rewards");
    }

    private void UpdateUI()
    {
        int activeCount = currentOptions.Count;

        if (currentOptions.Count == 0)
        {
            DebugHelper.Log("No options to display");
            return;
        }

        // Dynamically center cards
        // Formula: startX = -(count - 1) * spacing / 2
        float startX = -(activeCount - 1) * cardSpacing / 2f;

        // First disable ALL cards
        foreach (var card in rewardCards)
        {
            if (card != null)
                card.gameObject.SetActive(false);
        }

        // Then enable and position only needed ones
        for (int i = 0; i < activeCount && i < rewardCards.Length; i++)
        {
            if (rewardCards[i] == null) continue;

            rewardCards[i].gameObject.SetActive(true);

            // Calculate new X position
            float xPos = startX + i * cardSpacing;

            // Apply position (preserve Y and Z)
            RectTransform rect = rewardCards[i].GetComponent<RectTransform>();
            if (rect != null)
            {
                Vector2 anchoredPos = rect.anchoredPosition;
                anchoredPos.x = xPos;
                rect.anchoredPosition = anchoredPos;

                DebugHelper.Log($"Card {i}: X = {xPos}");
            }

            // Configure card content
            RewardOption option = currentOptions[i];
            int index = i; // Capture for closure

            if (option.IsPerk())
            {
                rewardCards[i].SetupPerk(option.perkData, option.currentLevel, () => OnRewardSelected(index));
            }
            else if (option.IsWeapon())
            {
                rewardCards[i].SetupWeapon(option.weaponData, option.currentLevel, () => OnRewardSelected(index));
            }
        }

        // Update Reroll button
        if (rerollButton != null && playerMoney != null)
        {
            bool canAfford = playerMoney.GetMoney() >= currentRerollCost;
            rerollButton.interactable = canAfford;

            if (rerollButtonText != null)
            {
                rerollButtonText.text = $"Reroll ${currentRerollCost}";
            }
        }

        // HP button always active
        if (emergencyHPButton != null)
        {
            emergencyHPButton.gameObject.SetActive(true);
        }
    }

    private void OnRewardSelected(int index)
    {
        if (index >= currentOptions.Count) return;

        RewardOption option = currentOptions[index];

        if (option.IsPerk())
        {
            DebugHelper.Log($"Perk selected: {option.perkData.perkName}");

            if (playerPerks != null)
            {
                playerPerks.AddPerk(option.perkData);
            }
        }
        else if (option.IsWeapon())
        {
            DebugHelper.Log($"Weapon selected: {option.weaponData.weaponName}");

            if (playerInventory != null)
            {
                playerInventory.AddWeapon(option.weaponData);
            }
        }

        ClosePanel();
    }

    public void OnMouseOver()
    {
        AudioManager.Instance.PlaySelectedReward();
    }

    private void OnRerollClicked()
    {
        if (playerMoney == null || playerMoney.GetMoney() < currentRerollCost)
        {
            DebugHelper.Log("Not enough money to reroll!");
            return;
        }

        playerMoney.SpendMoney(currentRerollCost);

        DebugHelper.Log($"Rerolling options! Spent: {currentRerollCost}");

        currentRerollCost = Mathf.RoundToInt(currentRerollCost * 1.5f);

        // Fixed: generate with same filter
        GenerateRewardOptions(currentOptions.Count, currentFilter);

        UpdateUI();
    }

    private void OnEmergencyHPClicked()
    {
        DebugHelper.Log($"Emergency HP selected: +{emergencyHPAmount}");

        if (playerHealth != null)
        {
            playerHealth.Heal(emergencyHPAmount);
        }

        ClosePanel();
    }

    private void ClosePanel()
    {
        choicePanel.SetActive(false);
        Time.timeScale = 1f;
        currentOptions.Clear();
    }

    private int GetWeaponLevel(WeaponData weaponData)
    {
        if (playerInventory == null) return 0;

        foreach (Weapon weapon in playerInventory.GetWeapons())
        {
            if (weapon != null && weapon.data == weaponData)
            {
                return weapon.level;
            }
        }

        return 0;
    }

    // Method for perk that adds slots
    public void AddBonusSlots(int amount)
    {
        bonusSlotsFromPerks += amount;
        bonusSlotsFromPerks = Mathf.Clamp(bonusSlotsFromPerks, 0, 2); // Max +2

        DebugHelper.Log($"Bonus reward slots: {bonusSlotsFromPerks}");
    }

    // Notification when nothing to offer
    private void ShowNoRewardsNotification()
    {
        // Create temporary notification
        GameObject notification = new GameObject("NoRewardsNotification");
        notification.transform.SetParent(choicePanel.transform.parent, false);

        RectTransform rect = notification.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(600, 200);

        // Background
        Image bg = notification.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        // Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(notification.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "EVERYTHING MAXED OUT!\n\n+10 ❤️ HP";
        text.fontSize = 36;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.green;

        // Destroy after 2 seconds
        Destroy(notification, 2f);

        DebugHelper.Log("Showing notification: EVERYTHING MAXED!");
    }
}

// Class for storing reward option
[System.Serializable]
public class RewardOption
{
    public PerkData perkData;
    public WeaponData weaponData;
    public int currentLevel;

    public bool IsPerk() => perkData != null;
    public bool IsWeapon() => weaponData != null;

    public bool IsMaxLevel()
    {
        if (IsPerk())
            return currentLevel >= perkData.maxLevel;
        if (IsWeapon())
            return currentLevel >= 10;
        return true;
    }
}