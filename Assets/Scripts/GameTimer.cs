using UnityEngine;
using TMPro;

public class GameTimer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;

    private float gameTime = 0f;
    private bool isRunning = true;

    void Update()
    {
        if (!isRunning) return;

        gameTime += Time.deltaTime;
        UpdateTimerDisplay();
    }

    private void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(gameTime / 60f);
        int seconds = Mathf.FloorToInt(gameTime % 60f);

        timerText.text = $"{minutes}:{seconds:00}";
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    public float GetGameTime() => gameTime;
}