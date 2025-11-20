using UnityEngine;
using UnityEngine.Events;

public class GarbageBag : MonoBehaviour
{
    [Header("Capacity")]
    [SerializeField] private int maxCapacity = 999999; // Changed: No limit
    private int currentCapacity = 0;

    [Header("Money per Drop-off")]
    [SerializeField] private int moneyPerUnit = 2; // Changed: Was 5, now 2

    public UnityEvent<int, int> OnCapacityChanged;
    public UnityEvent<int> OnBagEmptied;

    private int baseMaxCapacity;
    private float capacityBonusFromPerks = 0f;
    private float capacityMultiplierFromPerks = 1f;

    void Awake()
    {
        baseMaxCapacity = maxCapacity;
    }

    void Start()
    {
        OnCapacityChanged?.Invoke(currentCapacity, maxCapacity);
    }

    public bool TryAddGarbage(int size)
    {
        if (currentCapacity + size <= maxCapacity)
        {
            currentCapacity += size;
            DebugHelper.Log($"Garbage collected: {size}. Total in bag: {currentCapacity}/{maxCapacity}");

            GameStats stats = GameObject.FindFirstObjectByType<GameStats>();
            if (stats != null)
            {
                stats.AddGarbageCollected(size);
            }

            OnCapacityChanged?.Invoke(currentCapacity, maxCapacity);
            return true;
        }
        else
        {
            DebugHelper.Log("Bag is full! Drop off garbage.");
            return false;
        }
    }

    public bool HasSpace(int size)
    {
        return currentCapacity + size <= maxCapacity;
    }

    public int EmptyBag()
    {
        if (currentCapacity == 0)
        {
            DebugHelper.Log("Bag is empty!");
            return 0;
        }

        float valueMultiplier = GetGarbageValueMultiplier();
        int moneyEarned = Mathf.RoundToInt(currentCapacity * moneyPerUnit * valueMultiplier);

        DebugHelper.Log($"Dropped off garbage: {currentCapacity}. Earned money: {moneyEarned} (base: {currentCapacity * moneyPerUnit}, multiplier: x{valueMultiplier})");

        currentCapacity = 0;
        OnCapacityChanged?.Invoke(currentCapacity, maxCapacity);
        OnBagEmptied?.Invoke(moneyEarned);

        return moneyEarned;
    }

    public void UpgradeCapacity(int amount)
    {
        baseMaxCapacity += amount;
        maxCapacity += amount;

        DebugHelper.Log($"Bag upgraded! Base capacity: {baseMaxCapacity}, current (with perks): {maxCapacity}");
        OnCapacityChanged?.Invoke(currentCapacity, maxCapacity);
    }

    public void UpdateCapacity(float bonus, float multiplier)
    {
        capacityBonusFromPerks = bonus;
        capacityMultiplierFromPerks = multiplier;

        int newMaxCapacity = Mathf.RoundToInt((baseMaxCapacity + bonus) * multiplier);

        maxCapacity = newMaxCapacity;

        DebugHelper.Log($"Bag capacity updated: {maxCapacity} (base with upgrades: {baseMaxCapacity}, perk bonus: {bonus}, multiplier: x{multiplier})");

        OnCapacityChanged?.Invoke(currentCapacity, maxCapacity);
    }

    public float GetGarbageValueMultiplier()
    {
        PlayerPerks perks = FindObjectOfType<PlayerPerks>();
        if (perks != null)
        {
            return perks.GetTotalStatMultiplier("garbageValue");
        }
        return 1f;
    }

    public int GetCurrentCapacity() => currentCapacity;
    public int GetMaxCapacity() => maxCapacity;
    public float GetFillPercentage() => maxCapacity > 0 ? (float)currentCapacity / maxCapacity : 0f; // Protection against division by 0
    public bool IsFull() => currentCapacity >= maxCapacity;
}