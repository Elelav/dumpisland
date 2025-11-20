using UnityEngine;
using TMPro;
using System.Collections;

public class MoneyUI : MonoBehaviour
{
    [SerializeField] private PlayerMoney playerMoney;
    [SerializeField] private TextMeshProUGUI moneyText;

    private Vector3 originalScale; // Store the true scale
    private Coroutine currentAnimation; // Track the active coroutine

    void Start()
    {
        // Save the original scale once
        originalScale = transform.localScale;

        if (playerMoney != null)
        {
            playerMoney.OnMoneyChanged.AddListener(UpdateUI);
            UpdateUI(0);
        }
    }

    private void UpdateUI(int money)
    {
        moneyText.text = $"$ {money}";

        // Animation when earning money
        if (money > 0)
        {
            // Stop previous animation if still running
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
                // Always reset scale
                transform.localScale = originalScale;
            }

            currentAnimation = StartCoroutine(MoneyPopAnimation());
        }
    }

    private IEnumerator MoneyPopAnimation()
    {
        // Always use the original scale
        transform.localScale = originalScale * 1.2f;
        yield return new WaitForSecondsRealtime(0.1f); // Realtime: works even if timeScale = 0

        // Always return to original scale
        transform.localScale = originalScale;

        currentAnimation = null; // Clear coroutine reference
    }
}