using UnityEngine;
using TMPro;

public class WaveUI : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private TextMeshProUGUI waveNumberText;
    [SerializeField] private TextMeshProUGUI waveTimerText;

    [Header("Timer color")]
    [SerializeField] private Color normalColor = Color.yellow;
    [SerializeField] private Color warningColor = Color.red;
    [SerializeField] private float warningThreshold = 30f;

    void Start()
    {
        if (waveManager != null)
        {
            waveManager.OnWaveStart.AddListener(UpdateWaveNumber);
            waveManager.OnWaveTimerUpdate.AddListener(UpdateTimer);
        }
    }

    private void UpdateWaveNumber(int currentWave)
    {
        waveNumberText.text = $"Wave {currentWave}/{waveManager.GetTotalWaves()}";

        StartCoroutine(WaveChangeAnimation());
    }

    private void UpdateTimer(float timeRemaining)
    {
        // No negative numbers allowed
        timeRemaining = Mathf.Max(0, timeRemaining);
        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);

        waveTimerText.text = $"{minutes}:{seconds:00}";

        // Changing color when time is low
        if (timeRemaining <= warningThreshold && timeRemaining > 0)
        {
            waveTimerText.color = warningColor;

            // Pulse
            if (timeRemaining <= 10f)
            {
                float pulse = Mathf.PingPong(Time.time * 2f, 1f);
                waveTimerText.transform.localScale = Vector3.one * (1f + pulse * 0.2f);
            }
        }
        else
        {
            waveTimerText.color = normalColor;
            waveTimerText.transform.localScale = Vector3.one;
        }
    }

    private System.Collections.IEnumerator WaveChangeAnimation()
    {
        Vector3 originalScale = waveNumberText.transform.localScale;

        for (float t = 0; t < 0.3f; t += Time.deltaTime)
        {
            waveNumberText.transform.localScale = originalScale * (1f + t * 2f);
            yield return null;
        }

        for (float t = 0; t < 0.3f; t += Time.deltaTime)
        {
            waveNumberText.transform.localScale = originalScale * (1.6f - t * 2f);
            yield return null;
        }

        waveNumberText.transform.localScale = originalScale;
    }
}