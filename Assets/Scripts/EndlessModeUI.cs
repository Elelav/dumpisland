using UnityEngine;
using TMPro;

public class EndlessModeUI : MonoBehaviour
{
    [SerializeField] private EndlessMode endlessMode;
    [SerializeField] private TextMeshProUGUI endlessText;
    [SerializeField] private WaveManager waveManager;

    void Start()
    {
        if (endlessText != null)
        {
            endlessText.gameObject.SetActive(false);
        }

        if (waveManager != null)
        {
            waveManager.OnAllWavesComplete.AddListener(OnVictory);
        }
    }

    void Update()
    {
        if (endlessMode != null && endlessMode.IsActive())
        {
            if (endlessText != null && !endlessText.gameObject.activeSelf)
            {
                endlessText.gameObject.SetActive(true);
            }

            UpdateEndlessUI();
        }
    }

    private void OnVictory()
    {
        // Show victory message
        StartCoroutine(ShowVictoryMessage());
    }

    private System.Collections.IEnumerator ShowVictoryMessage()
    {
        if (endlessText == null) yield break;

        endlessText.gameObject.SetActive(true);
        endlessText.text = "VICTORY!";
        endlessText.color = Color.yellow;

        yield return new WaitForSeconds(2f);

        endlessText.text = "Preparing for endless mode...";

        yield return new WaitForSeconds(3f);

        endlessText.text = "ENDLESS MODE!";
        endlessText.color = new Color(1f, 0.5f, 0f); // Orange
    }

    private void UpdateEndlessUI()
    {
        if (endlessText == null || endlessMode == null) return;

        int level = endlessMode.GetDifficultyLevel();
        endlessText.text = $"ENDLESS MODE\nDifficulty level: {level}";

        // Pulsing
        float pulse = 1f + Mathf.Sin(Time.time * 2f) * 0.1f;
        endlessText.transform.localScale = Vector3.one * pulse;
    }
}