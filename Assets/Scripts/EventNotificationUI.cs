using UnityEngine;
using TMPro;
using System.Collections;

public class EventNotificationUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private WaveManager waveManager;

    [Header("Settings")]
    [SerializeField] private float displayDuration = 3f;

    void Start()
    {
        if (waveManager != null)
        {
            waveManager.OnWaveStart.AddListener(OnWaveStart);
            waveManager.OnBossSpawn.AddListener(OnBossSpawn);
        }

        // Hide text, keep object active
        if (notificationText != null)
        {
            notificationText.text = "";
            notificationText.enabled = false; // Disable text component only
        }
    }

    private void OnWaveStart(int waveNumber)
    {
        // For waves 3, 6, 9 (elite)
        if (waveNumber % 3 == 0 && waveNumber < 10)
        {
            ShowNotification("ELITE IS COMING!", Color.yellow);
        }
        else if (waveNumber != 10)
        {
            ShowNotification("WAVE STARTED", Color.cyan);
        }
    }

    private void OnBossSpawn()
    {
        ShowNotification("FINAL BOSS IS HERE", Color.red);
    }

    private void ShowNotification(string message, Color color)
    {
        StopAllCoroutines();
        StartCoroutine(ShowNotificationCoroutine(message, color));
    }

    private IEnumerator ShowNotificationCoroutine(string message, Color color)
    {
        notificationText.enabled = true; // Enable text
        notificationText.text = message;
        notificationText.color = color;

        // Appear animation
        notificationText.transform.localScale = Vector3.zero;

        for (float t = 0; t < 0.5f; t += Time.deltaTime)
        {
            float scale = Mathf.Lerp(0, 1.2f, t / 0.5f);
            notificationText.transform.localScale = Vector3.one * scale;
            yield return null;
        }

        notificationText.transform.localScale = Vector3.one;

        // Display duration
        yield return new WaitForSeconds(displayDuration);

        // Fade out
        for (float t = 0; t < 0.3f; t += Time.deltaTime)
        {
            Color c = notificationText.color;
            c.a = 1f - (t / 0.3f);
            notificationText.color = c;
            yield return null;
        }

        notificationText.enabled = false; // Disable component

        // Restore alpha
        Color finalColor = notificationText.color;
        finalColor.a = 1f;
        notificationText.color = finalColor;
    }
}