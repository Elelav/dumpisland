using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class RewardChoiceUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private TextMeshProUGUI levelUpTitle;

    [Header("Choice Buttons")]
    [SerializeField] private Button[] choiceButtons = new Button[3];
    [SerializeField] private Image[] weaponIcons = new Image[3];
    [SerializeField] private TextMeshProUGUI[] weaponNames = new TextMeshProUGUI[3];
    [SerializeField] private TextMeshProUGUI[] weaponLevels = new TextMeshProUGUI[3];
    [SerializeField] private TextMeshProUGUI[] weaponStats = new TextMeshProUGUI[3];

    [Header("Weapon Pool")]
    [SerializeField] private WeaponData[] availableWeapons;

    [Header("Dependencies")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerExperience playerExperience;

    private WeaponData[] currentChoices = new WeaponData[3];

    void Start()
    {
        if (choicePanel != null)
        {
            choicePanel.SetActive(false);
        }

        // Subscribe to level-up event
        if (playerExperience != null)
        {
            playerExperience.OnShowWeaponChoice.AddListener(ShowWeaponChoice);
        }

        // Bind buttons
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            int index = i; // Important! Capture index for closure
            if (choiceButtons[i] != null)
            {
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => OnWeaponChosen(index));
            }
        }
    }

    private void ShowWeaponChoice()
    {
        DebugHelper.Log("Showing weapon selection");

        GenerateWeaponChoices();
        UpdateChoicesUI();

        // Show with animation
        StartCoroutine(ShowChoicePanelAnimated());
    }

    private System.Collections.IEnumerator ShowChoicePanelAnimated()
    {
        choicePanel.SetActive(true);

        // Add CanvasGroup if not exists
        CanvasGroup canvasGroup = choicePanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = choicePanel.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0f;

        // Pause game
        Time.timeScale = 0f;

        // Update title
        if (levelUpTitle != null)
        {
            levelUpTitle.text = $"LEVEL {playerExperience.GetLevel()}!";
        }

        // Fade in
        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // Use unscaled time to work during pause
            canvasGroup.alpha = elapsed / duration;
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    private void GenerateWeaponChoices()
    {
        if (availableWeapons == null || availableWeapons.Length == 0)
        {
            DebugHelper.LogError("No weapons available for selection!");
            return;
        }

        // Get player's current weapons
        List<Weapon> playerWeapons = playerInventory.GetWeapons();

        for (int i = 0; i < 3; i++)
        {
            // Random weapon
            WeaponData randomWeapon = availableWeapons[Random.Range(0, availableWeapons.Length)];
            currentChoices[i] = randomWeapon;

            // You can add logic to avoid duplicate options
            // But for simplicity, we'll leave it as is
        }
    }

    private void UpdateChoicesUI()
    {
        for (int i = 0; i < 3; i++)
        {
            if (currentChoices[i] == null) continue;

            WeaponData weapon = currentChoices[i];

            // Check if player already has this weapon
            Weapon existingWeapon = FindExistingWeapon(weapon);
            int currentLevel = existingWeapon != null ? existingWeapon.level : 0;
            int nextLevel = currentLevel + 1;

            // Icon
            if (weaponIcons[i] != null && weapon.icon != null)
            {
                weaponIcons[i].sprite = weapon.icon;
                weaponIcons[i].enabled = true;
            }
            else if (weaponIcons[i] != null)
            {
                weaponIcons[i].enabled = false;
            }

            // Name
            if (weaponNames[i] != null)
            {
                weaponNames[i].text = weapon.weaponName;
            }

            // Level
            if (weaponLevels[i] != null)
            {
                if (currentLevel == 0)
                {
                    weaponLevels[i].text = "NEW!";
                    weaponLevels[i].color = Color.green;
                }
                else if (currentLevel >= 10)
                {
                    weaponLevels[i].text = "MAX LEVEL!";
                    weaponLevels[i].color = Color.red;
                }
                else
                {
                    weaponLevels[i].text = $"New {currentLevel} → {nextLevel}";
                    weaponLevels[i].color = Color.yellow;
                }
            }

            // Stats
            if (weaponStats[i] != null)
            {
                string stats = $"Damage: {weapon.GetDamage(nextLevel):F0}\n" +
                              $"Attack Speed: {weapon.GetAttackSpeed(nextLevel):F1}\n" +
                              $"Range: {weapon.GetRange(nextLevel):F1}\n" +
                              $"Crit Chance: {weapon.GetCritChance(nextLevel) * 100:F0}%";

                weaponStats[i].text = stats;
            }

            // Disable button if weapon is maxed and no slots available
            if (choiceButtons[i] != null)
            {
                bool canTake = CanTakeWeapon(weapon);
                choiceButtons[i].interactable = canTake;
            }
        }
    }

    private Weapon FindExistingWeapon(WeaponData weaponData)
    {
        List<Weapon> weapons = playerInventory.GetWeapons();
        foreach (Weapon w in weapons)
        {
            if (w != null && w.data == weaponData)
            {
                return w;
            }
        }
        return null;
    }

    private bool CanTakeWeapon(WeaponData weaponData)
    {
        Weapon existing = FindExistingWeapon(weaponData);

        if (existing != null)
        {
            // If exists - can take only if not max level
            return existing.CanLevelUp();
        }
        else
        {
            // If doesn't exist - can take if there's a free slot
            return playerInventory.GetActiveWeaponCount() < 5;
        }
    }

    private void OnWeaponChosen(int index)
    {
        if (currentChoices[index] == null)
        {
            DebugHelper.LogError($"Choice {index} is empty!");
            return;
        }

        WeaponData chosenWeapon = currentChoices[index];

        DebugHelper.Log($"Weapon selected: {chosenWeapon.weaponName}");

        bool success = playerInventory.AddWeapon(chosenWeapon);

        if (success)
        {
            StartCoroutine(HideChoicePanelAnimated());
        }
        else
        {
            DebugHelper.LogWarning("Failed to add weapon!");
        }
    }

    private System.Collections.IEnumerator HideChoicePanelAnimated()
    {
        CanvasGroup canvasGroup = choicePanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            choicePanel.SetActive(false);
            Time.timeScale = 1f;
            yield break;
        }

        // Fade out
        float elapsed = 0f;
        float duration = 0.2f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = 1f - (elapsed / duration);
            yield return null;
        }

        choicePanel.SetActive(false);

        // Resume game
        Time.timeScale = 1f;
    }
}