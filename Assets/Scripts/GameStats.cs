using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GameStats : MonoBehaviour
{
    // Basic stats
    private int enemiesKilled = 0;
    private int garbageCollected = 0;
    private int moneyEarned = 0;
    private float damageDealt = 0f;
    private float timePlayed = 0f;

    // New: Per-weapon damage stats
    private Dictionary<string, float> weaponDamage = new Dictionary<string, float>();

    // New: For scoring
    private int playerLevel = 1;
    private int currentWave = 0;
    private bool isVictory = false;

    private bool isTracking = true;

    void Start()
    {
        // Subscribe to events for automatic updates

        // Level updates
        PlayerExperience exp = FindObjectOfType<PlayerExperience>();
        if (exp != null)
        {
            exp.OnLevelUp.AddListener(OnPlayerLevelUp);
            playerLevel = exp.GetLevel();
        }

        // Wave updates
        WaveManager waveManager = FindObjectOfType<WaveManager>();
        if (waveManager != null)
        {
            waveManager.OnWaveStart.AddListener(OnWaveStart);
            waveManager.OnAllWavesComplete.AddListener(OnVictory);
            currentWave = waveManager.GetCurrentWave();
        }
    }

    void Update()
    {
        if (isTracking)
        {
            timePlayed += Time.deltaTime;
        }
    }

    // Event handlers
    private void OnPlayerLevelUp(int newLevel)
    {
        playerLevel = newLevel;
        DebugHelper.Log($"GameStats: level updated to {playerLevel}");
    }

    private void OnWaveStart(int waveNumber)
    {
        currentWave = waveNumber;
        DebugHelper.Log($"GameStats: wave updated to {currentWave}");
    }

    private void OnVictory()
    {
        isVictory = true;
        DebugHelper.Log("GameStats: VICTORY! Bonus +5000 points");
    }

    // Methods for tracking stats
    public void AddEnemyKilled() => enemiesKilled++;
    public void AddGarbageCollected(int amount) => garbageCollected += amount;
    public void AddMoneyEarned(int amount) => moneyEarned += amount;
    public void AddDamageDealt(float amount) => damageDealt += amount;

    // New: Add damage from a specific weapon
    public void AddWeaponDamage(string weaponName, float damage)
    {
        if (string.IsNullOrEmpty(weaponName)) return;

        if (weaponDamage.ContainsKey(weaponName))
        {
            weaponDamage[weaponName] += damage;
        }
        else
        {
            weaponDamage[weaponName] = damage;
        }
    }

    // New: Set player level
    public void SetPlayerLevel(int level) => playerLevel = level;

    // New: Set current wave
    public void SetCurrentWave(int wave) => currentWave = wave;

    // New: Mark as victory
    public void MarkAsVictory() => isVictory = true;

    public void StopTracking() => isTracking = false;

    // Getters
    public int GetEnemiesKilled() => enemiesKilled;
    public int GetGarbageCollected() => garbageCollected;
    public int GetMoneyEarned() => moneyEarned;

    public int GetPlayerLevel() => playerLevel;
    public float GetDamageDealt() => damageDealt;
    public float GetTimePlayed() => timePlayed;

    // New: Score calculation
    public int CalculateScore()
    {
        /*
        Formula:
        Score = 
          (EnemiesKilled × 10) +
          (DamageDealt × 0.5) +
          (GarbageCollected × 5) +
          (MoneyEarned × 2) +
          (PlayerLevel × 100) +
          (Wave × 200) +
          Victory bonus (5000)
        */

        int score = 0;

        score += enemiesKilled * 10;
        score += Mathf.RoundToInt(damageDealt * 0.5f);
        score += garbageCollected * 5;
        score += moneyEarned * 2;
        score += playerLevel * 100;
        score += currentWave * 200;

        if (isVictory)
        {
            score += 5000; // Victory bonus
        }

        return score;
    }

    // Get top N weapons by damage
    public List<WeaponStat> GetTopWeapons(int count = 10)
    {
        List<WeaponStat> stats = new List<WeaponStat>();

        if (weaponDamage.Count == 0)
        {
            return stats;
        }

        // Sort by damage (descending)
        var sortedWeapons = weaponDamage.OrderByDescending(kvp => kvp.Value);

        // Take top N
        foreach (var weapon in sortedWeapons.Take(count))
        {
            float percentage = (weapon.Value / damageDealt) * 100f;

            stats.Add(new WeaponStat
            {
                weaponName = weapon.Key,
                damage = weapon.Value,
                percentage = percentage
            });
        }

        return stats;
    }

    // Formatted time
    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(timePlayed / 60f);
        int seconds = Mathf.FloorToInt(timePlayed % 60f);
        return $"{minutes}:{seconds:00}";
    }

    public bool GetIsVictory() => isVictory;
}

// New class: weapon statistics
[System.Serializable]
public class WeaponStat
{
    public string weaponName;
    public float damage;
    public float percentage;
}