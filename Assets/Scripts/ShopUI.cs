using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;

    [Header("Item Buttons")]
    [SerializeField] private Button upgradeBagButton;
    [SerializeField] private Button buyMaxHPButton;
    [SerializeField] private Button buyHealButton;
    [SerializeField] private Button buyPerkButton;
    [SerializeField] private Button buyWeaponButton;

    [Header("Button Texts")]
    [SerializeField] private TextMeshProUGUI upgradeBagText;
    [SerializeField] private TextMeshProUGUI buyMaxHPText;
    [SerializeField] private TextMeshProUGUI buyHealText;
    [SerializeField] private TextMeshProUGUI buyPerkText;
    [SerializeField] private TextMeshProUGUI buyWeaponText;

    [Header("Base Prices")]
    [SerializeField] private int bagUpgradeBaseCost = 50;
    [SerializeField] private int maxHPUpgradeBaseCost = 100;
    [SerializeField] private int healBaseCost = 50;
    [SerializeField] private int perkBaseCost = 150;
    [SerializeField] private int weaponBaseCost = 200;

    [Header("Settings")]
    [SerializeField] private int bagUpgradeAmount = 5;
    [SerializeField] private int maxHPUpgradeAmount = 50;
    [SerializeField] private float priceMultiplier = 1.5f; // Price scaling multiplier

    [Header("Dependencies")]
    [SerializeField] private GarbageBag garbageBag;
    [SerializeField] private PlayerMoney playerMoney;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private RewardChoiceManager rewardManager;
    [SerializeField] private AudioManager audioManager;

    // Purchase counters
    private int bagPurchaseCount = 0;
    private int maxHPPurchaseCount = 0;
    private int healPurchaseCount = 0;
    private int perkPurchaseCount = 0;
    private int weaponPurchaseCount = 0;

    private bool isShopOpen = false;

    void Start()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }

        if (openButton != null)
            openButton.onClick.AddListener(OnOpenClicked);

        if (upgradeBagButton != null)
            upgradeBagButton.onClick.AddListener(OnUpgradeBagClicked);

        if (buyMaxHPButton != null)
            buyMaxHPButton.onClick.AddListener(OnBuyMaxHPClicked);

        if (buyHealButton != null)
            buyHealButton.onClick.AddListener(OnBuyHealClicked);

        if (buyPerkButton != null)
            buyPerkButton.onClick.AddListener(OnBuyPerkClicked);

        if (buyWeaponButton != null)
            buyWeaponButton.onClick.AddListener(OnBuyWeaponClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseShop);
    }

    void Update()
    {
        GameObject pauseMenu = GameObject.Find("PauseMenu");
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (isShopOpen)
            {
                CloseShop();
            }
            else
            {
                OpenShop();
            }
        }

        if (isShopOpen)
        {
            UpdateAllButtons();
        }
    }

    public void OpenShop()
    {
        AudioManager.Instance.PlayButtonClick();
        shopPanel.SetActive(true);
        isShopOpen = true;
        Time.timeScale = 0f;
        UpdateAllButtons();
    }

    public void CloseShop()
    {
        shopPanel.SetActive(false);
        isShopOpen = false;
        Time.timeScale = 1f;
    }

    private void OnOpenClicked()
    {
        OpenShop();
    }

    // Calculate scaled price based on purchase count
    private int GetScaledPrice(int basePrice, int purchaseCount)
    {
        return Mathf.Clamp(Mathf.RoundToInt(basePrice * Mathf.Pow(priceMultiplier, purchaseCount)),1, 999999);
    }

    // ========== PURCHASES ==========

    private void OnUpgradeBagClicked()
    {
        int currentPrice = GetScaledPrice(bagUpgradeBaseCost, bagPurchaseCount);
        int finalCost = GetDiscountedPrice(currentPrice);
        AudioManager.Instance.PlayPurchase();

        if (playerMoney.SpendMoney(finalCost))
        {
            garbageBag.UpgradeCapacity(bagUpgradeAmount);
            bagPurchaseCount++; // Increment counter
            DebugHelper.Log($"Bag upgraded! Purchase #{bagPurchaseCount}");
            UpdateAllButtons();
        }
        else
        {
            DebugHelper.Log("Insufficient funds!");
        }
    }

    private void OnBuyMaxHPClicked()
    {
        int currentPrice = GetScaledPrice(maxHPUpgradeBaseCost, maxHPPurchaseCount);
        int finalCost = GetDiscountedPrice(currentPrice);
        AudioManager.Instance.PlayPurchase();

        if (playerMoney.SpendMoney(finalCost))
        {
            if (playerHealth != null)
            {
                playerHealth.IncreaseMaxHealth(maxHPUpgradeAmount);
                maxHPPurchaseCount++; // Increment counter
                DebugHelper.Log($"Max HP increased! Purchase #{maxHPPurchaseCount}");
            }
            UpdateAllButtons();
        }
        else
        {
            DebugHelper.Log("Insufficient funds!");
        }
    }

    private void OnBuyHealClicked()
    {
        int currentPrice = GetScaledPrice(healBaseCost, healPurchaseCount);
        int finalCost = GetDiscountedPrice(currentPrice);
        AudioManager.Instance.PlayPurchase();

        if (playerMoney.SpendMoney(finalCost))
        {
            if (playerHealth != null)
            {
                float maxHP = playerHealth.GetMaxHealth();
                playerHealth.Heal(maxHP);
                healPurchaseCount++; // Increment counter
                DebugHelper.Log($"Health restored! Purchase #{healPurchaseCount}");
            }
            UpdateAllButtons();
        }
        else
        {
            DebugHelper.Log("Insufficient funds!");
        }
    }

    private void OnBuyPerkClicked()
    {
        int currentPrice = GetScaledPrice(perkBaseCost, perkPurchaseCount);
        int finalCost = GetDiscountedPrice(currentPrice);
        AudioManager.Instance.PlayPurchase();

        if (playerMoney.SpendMoney(finalCost))
        {
            perkPurchaseCount++; // Increment counter
            DebugHelper.Log($"Random perk purchased! Purchase #{perkPurchaseCount}");

            CloseShop();

            if (rewardManager != null)
            {
                rewardManager.ShowRewardChoice(RewardFilterType.OnlyPerks);
            }
        }
        else
        {
            DebugHelper.Log("Insufficient funds!");
        }
    }

    private void OnBuyWeaponClicked()
    {
        int currentPrice = GetScaledPrice(weaponBaseCost, weaponPurchaseCount);
        int finalCost = GetDiscountedPrice(currentPrice);

        AudioManager.Instance.PlayPurchase();

        if (playerMoney.SpendMoney(finalCost))
        {
            weaponPurchaseCount++; // Increment counter
            DebugHelper.Log($"Random weapon purchased! Purchase #{weaponPurchaseCount}");

            CloseShop();

            if (rewardManager != null)
            {
                rewardManager.ShowRewardChoice(RewardFilterType.OnlyWeapons);
            }
        }
        else
        {
            DebugHelper.Log("Insufficient funds!");
        }
    }

    // ========== UPDATE UI ==========

    private void UpdateAllButtons()
    {
        if (playerMoney == null) return;

        int currentMoney = playerMoney.GetMoney();

        // Pass current price considering purchase count
        UpdateButton(
            upgradeBagButton,
            upgradeBagText,
            bagUpgradeBaseCost,
            bagPurchaseCount,
            currentMoney,
            $"Bag Upgrade (+{bagUpgradeAmount})",
            garbageBag != null ? $"Total: {garbageBag.GetMaxCapacity()}" : ""
        );

        UpdateButton(
            buyMaxHPButton,
            buyMaxHPText,
            maxHPUpgradeBaseCost,
            maxHPPurchaseCount,
            currentMoney,
            $"Increase HP (+{maxHPUpgradeAmount})",
            ""
        );

        UpdateButton(
            buyHealButton,
            buyHealText,
            healBaseCost,
            healPurchaseCount,
            currentMoney,
            "Restore HP",
            playerHealth != null ? GetHealthStatus() : ""
        );

        UpdateButton(
            buyPerkButton,
            buyPerkText,
            perkBaseCost,
            perkPurchaseCount,
            currentMoney,
            "Random Perk Choice",
            ""
        );

        UpdateButton(
            buyWeaponButton,
            buyWeaponText,
            weaponBaseCost,
            weaponPurchaseCount,
            currentMoney,
            "Random Weapon Choice",
            ""
        );
    }

    // Updated: Added purchaseCount parameter
    private void UpdateButton(Button button, TextMeshProUGUI text, int basePrice, int purchaseCount, int playerMoney, string itemName, string extraInfo)
    {
        if (button == null || text == null) return;

        int currentPrice = GetScaledPrice(basePrice, purchaseCount);
        int finalCost = GetDiscountedPrice(currentPrice);
        bool canAfford = playerMoney >= finalCost;

        button.interactable = canAfford;
        text.color = canAfford ? Color.white : new Color(0.5f, 0.5f, 0.5f);

        string priceText = $"${finalCost}";

        // Show discount if applicable
        if (finalCost < currentPrice)
        {
            priceText = $"<s>{currentPrice}</s> {finalCost}";
        }

        text.text = $"{itemName}\n{priceText}";

        if (!string.IsNullOrEmpty(extraInfo))
        {
            text.text += $"\n<size=80%>{extraInfo}</size>";
        }
    }

    private int GetDiscountedPrice(int basePrice)
    {
        PlayerPerks perks = FindObjectOfType<PlayerPerks>();
        if (perks != null)
        {
            float discount = perks.GetTotalStatMultiplier("shopDiscount");
            return Mathf.RoundToInt(basePrice * discount);
        }
        return basePrice;
    }

    private string GetHealthStatus()
    {
        if (playerHealth == null) return "";
        return "Full restoration";
    }
}