using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ActivePerksUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject perkSlotPrefab;
    [SerializeField] private Transform perksContainer;
    [SerializeField] private PlayerPerks playerPerks;

    private Dictionary<PerkData, GameObject> activePerkSlots = new Dictionary<PerkData, GameObject>();
    private Canvas canvas; // New

    void Start()
    {
        // New: Getting Canvas
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            // Set low sorting order (below death screen)
            canvas.sortingOrder = 0;
            DebugHelper.Log($"ActivePerksUI Canvas sorting order = {canvas.sortingOrder}");
        }

        if (playerPerks != null)
        {
            playerPerks.OnPerkAdded.AddListener(OnPerkAdded);
            playerPerks.OnPerkLevelUp.AddListener(OnPerkLevelUp);

            // New: Subscribe to player death
            PlayerHealth playerHealth = playerPerks.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.OnDeath.AddListener(OnPlayerDeath);
            }
        }

        RefreshAllPerks();
    }

    // New: Hide panel on death
    private void OnPlayerDeath()
    {
        DebugHelper.Log("Player died - hiding perks panel");
        gameObject.SetActive(false);
    }

    private void OnPerkAdded(PerkData perkData, int level)
    {
        CreatePerkSlot(perkData, level);
    }

    private void OnPerkLevelUp(PerkData perkData, int newLevel)
    {
        UpdatePerkSlot(perkData, newLevel);
    }

    private void CreatePerkSlot(PerkData perkData, int level)
    {
        if (perkData == null) return;

        if (activePerkSlots.ContainsKey(perkData))
        {
            DebugHelper.LogWarning($"Slot for {perkData.perkName} already exists!");
            return;
        }

        GameObject slotObj = Instantiate(perkSlotPrefab, perksContainer);

        Image icon = slotObj.GetComponent<Image>();
        if (icon != null && perkData.icon != null)
        {
            icon.sprite = perkData.icon;
            icon.color = Color.white;
        }

        TextMeshProUGUI levelText = slotObj.GetComponentInChildren<TextMeshProUGUI>();
        if (levelText != null)
        {
            levelText.text = level.ToString();
        }

        Image outline = slotObj.transform.Find("Outline")?.GetComponent<Image>();
        if (outline != null)
        {
            switch (perkData.rarity)
            {
                case PerkRarity.Common:
                    outline.color = new Color(0.5f, 0.5f, 0.5f);
                    break;
                case PerkRarity.Rare:
                    outline.color = new Color(0.3f, 0.6f, 1f);
                    break;
                case PerkRarity.Epic:
                    outline.color = new Color(0.8f, 0.3f, 1f);
                    break;
                case PerkRarity.Legendary:
                    outline.color = new Color(1f, 0.7f, 0f);
                    break;
            }
        }

        activePerkSlots.Add(perkData, slotObj);

        DebugHelper.Log($"Created UI slot for perk: {perkData.perkName} (level {level})");
    }

    private void UpdatePerkSlot(PerkData perkData, int newLevel)
    {
        if (!activePerkSlots.ContainsKey(perkData))
        {
            DebugHelper.LogWarning($"Slot for {perkData.perkName} not found!");
            return;
        }

        GameObject slotObj = activePerkSlots[perkData];

        TextMeshProUGUI levelText = slotObj.GetComponentInChildren<TextMeshProUGUI>();
        if (levelText != null)
        {
            levelText.text = newLevel.ToString();
        }

        StartCoroutine(LevelUpAnimation(slotObj));

        DebugHelper.Log($"Updated UI slot for perk: {perkData.perkName} to level {newLevel}");
    }

    public void RefreshAllPerks()
    {
        if (playerPerks == null) return;

        foreach (var slot in activePerkSlots.Values)
        {
            if (slot != null)
                Destroy(slot);
        }
        activePerkSlots.Clear();

        List<ActivePerk> activePerks = playerPerks.GetActivePerks();
        foreach (var perk in activePerks)
        {
            if (perk != null && perk.data != null)
            {
                CreatePerkSlot(perk.data, perk.level);
            }
        }
    }

    private System.Collections.IEnumerator LevelUpAnimation(GameObject slotObj)
    {
        Vector3 originalScale = slotObj.transform.localScale;

        float elapsed = 0f;
        float duration = 0.2f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float scale = Mathf.Lerp(1f, 1.3f, elapsed / duration);
            slotObj.transform.localScale = originalScale * scale;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float scale = Mathf.Lerp(1.3f, 1f, elapsed / duration);
            slotObj.transform.localScale = originalScale * scale;
            yield return null;
        }

        slotObj.transform.localScale = originalScale;
    }
}