using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private Slider volumeSlider;

    private bool isPaused = false;
    private float savedTimeScale = 1f;

    void Start()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        // Bind buttons through code (more reliable than via Inspector)
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners(); // Clear previous bindings
            resumeButton.onClick.AddListener(Resume);
        }

        if (menuButton != null)
        {
            menuButton.onClick.RemoveAllListeners();
            menuButton.onClick.AddListener(ReturnToMenu);
        }

        // Load saved volume
        if (volumeSlider != null)
        {
            float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            volumeSlider.value = savedVolume;
            AudioListener.volume = savedVolume;

            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Prevent pause if weapon selection window is open
            GameObject weaponChoice = GameObject.Find("WeaponChoicePanel");
            if (weaponChoice != null && weaponChoice.activeSelf)
            {
                return; // Don’t pause when weapon selection is active
            }

            if (isPaused)
            {
                Resume();
            }
            else
            {
                // Ensure time isn't already stopped by another system
                if (Time.timeScale > 0f)
                {
                    Pause();
                }
            }
        }

        if (isPaused)
        {
            Time.timeScale = 0f;
        }
    }

    public void Pause()
    {
        if (isPaused)
        {
            DebugHelper.LogWarning("Attempted to pause an already stopped game");
            return;
        }

        DebugHelper.Log("=== PAUSED ===");

        savedTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        isPaused = true;

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }
    }

    public void Resume()
    {
        if (!isPaused)
        {
            DebugHelper.LogWarning("Attempted to resume a game that isn’t paused");
            return;
        }

        DebugHelper.Log("=== RESUMED ===");

        Time.timeScale = savedTimeScale;
        isPaused = false;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    public void ReturnToMenu()
    {
        DebugHelper.Log("=== RETURNING TO MENU ===");

        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    void OnDestroy()
    {
        // Ensure time is restored when object is destroyed
        Time.timeScale = 1f;
    }

    private void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("MasterVolume", value);
        PlayerPrefs.Save();
    }
}