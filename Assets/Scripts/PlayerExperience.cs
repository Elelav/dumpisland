using UnityEngine;
using UnityEngine.Events;

public class PlayerExperience : MonoBehaviour
{
    [Header("Experience")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private float currentExp = 0f;
    [SerializeField] private float expToNextLevel = 100f;
    [SerializeField] private float expMultiplier = 1.5f; // Multiplier for next level's required exp

    [Header("Level Rewards")]
    [SerializeField] private int weaponChoicesCount = 3; // How many weapon options to offer

    // Events
    public UnityEvent<float, float> OnExpChanged; // (current, max)
    public UnityEvent<int> OnLevelUp; // New level
    public UnityEvent OnShowWeaponChoice; // Show weapon selection

    void Start()
    {
        OnExpChanged?.Invoke(currentExp, expToNextLevel);
    }

    public void AddExperience(float amount)
    {
        // Experience multiplier from perks
        PlayerPerks perks = GetComponent<PlayerPerks>();
        if (perks != null)
        {
            float expMultiplier = perks.GetTotalStatMultiplier("exp");
            amount *= expMultiplier;

            if (expMultiplier > 1f)
            {
                DebugHelper.Log($"Experience bonus! x{expMultiplier:F2}");
            }
        }

        currentExp += amount;

        // Check for level up
        while (currentExp >= expToNextLevel)
        {
            LevelUp();
        }

        OnExpChanged?.Invoke(currentExp, expToNextLevel);
    }

    private void LevelUp()
    {
        currentExp -= expToNextLevel;
        currentLevel++;

        expToNextLevel *= expMultiplier;

        DebugHelper.Log($"LEVEL UP! Now at level {currentLevel}");

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayLevelUp();
        }

        OnLevelUp?.Invoke(currentLevel);
        OnShowWeaponChoice?.Invoke();
    }

    public int GetLevel() => currentLevel;
    public float GetCurrentExp() => currentExp;
    public float GetExpToNextLevel() => expToNextLevel;
}