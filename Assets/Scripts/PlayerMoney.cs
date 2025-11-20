using UnityEngine;
using UnityEngine.Events;

public class PlayerMoney : MonoBehaviour
{
    [SerializeField] private int currentMoney = 0;

    // Events for UI
    public UnityEvent<int> OnMoneyChanged;

    void Start()
    {
        OnMoneyChanged?.Invoke(currentMoney);
    }

    // Add money
    public void AddMoney(int amount)
    {
        currentMoney += amount;
        DebugHelper.Log($"Money received: {amount}. Total: {currentMoney}");

        // Update statistics
        GameStats stats = GameObject.FindFirstObjectByType<GameStats>();
        if (stats != null)
        {
            stats.AddMoneyEarned(amount);
        }

        OnMoneyChanged?.Invoke(currentMoney);
    }

    // Spend money
    public bool SpendMoney(int amount)
    {
        if (currentMoney >= amount)
        {
            currentMoney -= amount;
            DebugHelper.Log($"Money spent: {amount}. Remaining: {currentMoney}");
            OnMoneyChanged?.Invoke(currentMoney);
            return true;
        }

        DebugHelper.Log("Insufficient funds!");
        return false;
    }

    public int GetMoney() => currentMoney;
}