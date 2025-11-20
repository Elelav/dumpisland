using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DeathScreenUI : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private GameObject deathScreenPanel;
    [SerializeField] private TextMeshProUGUI deathTitle;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private TextMeshProUGUI weaponStatsText;
    [SerializeField] private Button retryButton;
    [SerializeField] private GameObject achievmentsPanel;
    //[SerializeField] private GameObject[] achievmentsPanelList = new GameObject[6];
    [SerializeField] private AchievmentInfoUI[] achievmentInfoList = new AchievmentInfoUI[6];
    [SerializeField] private Achievment[] achievmentList = new Achievment[6];

    [Header("Зависимости")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private GameStats gameStats;

    void Start()
    {
        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        AudioListener.volume = savedVolume;

        if (deathScreenPanel != null)
        {
            deathScreenPanel.SetActive(false);
        }

        if (playerHealth != null)
        {
            playerHealth.OnDeath.AddListener(ShowDeathScreen);
        }

        if (retryButton != null)
        {
            retryButton.onClick.AddListener(OnRetryClicked);
        }

    }

    private void ShowDeathScreen()
    {
        if (gameStats != null)
        {
            gameStats.StopTracking();
        }
        UpdateAchievments();

        StartCoroutine(ShowDeathScreenWithFade());
    }

    private IEnumerator ShowDeathScreenWithFade()
    {
        Time.timeScale = 0f;
        deathScreenPanel.SetActive(true);

        CanvasGroup canvasGroup = deathScreenPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = deathScreenPanel.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0f;

        // Smooth
        float elapsed = 0f;
        float duration = 1f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; 
            canvasGroup.alpha = elapsed / duration;
            yield return null;
        }

        canvasGroup.alpha = 1f;
        // Getting stats
        UpdateStatsText();

    }

    private void UpdateStatsText()
    {
        if (gameStats == null || statsText == null) return;

        if (gameStats.GetIsVictory() == true) deathTitle.text = "<color=yellow>COMPLETED!</color>";

        int totalScore = gameStats.CalculateScore();
        List<WeaponStat> topWeapons = gameStats.GetTopWeapons(10);

        string stats = $"<b><size=150%>GAME STATS</size></b>\n\n" +
                      $"<b><color=yellow>SCORE: {totalScore}</color></b>\n\n" +
                      $"Enemies killed: <color=yellow>{gameStats.GetEnemiesKilled()}</color>\n" +
                      $"Trash collected: <color=yellow>{gameStats.GetGarbageCollected()}</color>\n" +
                      $"Money earned: <color=yellow>{gameStats.GetMoneyEarned()}</color>\n" +
                      $"Damage dealt: <color=yellow>{gameStats.GetDamageDealt():F0}</color>\n" +
                      $"Time survived: <color=yellow>{gameStats.GetFormattedTime()}</color>\n\n";

        string weaponStats = "<b><size=150%>WEAPON TOP</size></b>\n";
        if (topWeapons.Count > 0)
        {
            for (int i = 0; i < topWeapons.Count; i++)
            {
                WeaponStat weapon = topWeapons[i];
                string medal = i == 0 ? "" : i == 1 ? "" : "";

                weaponStats += $"{medal} <color=yellow>{weapon.weaponName}</color>\n" +
                        $"   DMG: {weapon.damage:F0} ({weapon.percentage:F1}%)\n";
            }
        }

        statsText.text = stats;
        weaponStatsText.text = weaponStats;
    }

    private void UpdateAchievments()
    {
        if (gameStats.GetIsVictory() == false)
        {
            achievmentsPanel.gameObject.SetActive(false);
            return;
        }
        for (int i = 0; i < 6; i++)
        {
            Achievment achievment = achievmentList[i];
            AchievmentInfoUI achievmentInfo = achievmentInfoList[i];
            if (achievment.isAchievmentCompleted(playerInventory, gameStats, playerHealth))
            {
                achievmentInfo.SetupAchievment(achievment);
                achievmentInfoList[i] = achievmentInfo;
            }
            else
            {
                achievmentInfo.setPanelActive(false);
                achievmentInfoList[i] = achievmentInfo;
            }
        }
    }

    private void OnRetryClicked()
    {
        
        Time.timeScale = 1f;        
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}