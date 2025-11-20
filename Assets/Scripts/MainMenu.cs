using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenu : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button startButton;

    [Header("Volume Settings")]
    [SerializeField] private Slider musicVolumeSlider;   // Music volume slider
    [SerializeField] private Slider sfxVolumeSlider;     // SFX volume slider

    [Header("Volume Text (optional)")]
    [SerializeField] private TextMeshProUGUI musicVolumeText; // Displays percentage
    [SerializeField] private TextMeshProUGUI sfxVolumeText;   // Displays percentage

    void Start()
    {
        Time.timeScale = 1f;

        // Assign start button
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartGame);
        }

        // Play menu music
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMenuMusic();
        }

        // Music volume slider setup
        if (musicVolumeSlider != null)
        {
            float savedMusicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
            musicVolumeSlider.value = savedMusicVolume;
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

            UpdateMusicVolumeText(savedMusicVolume);
        }

        // SFX volume slider setup
        if (sfxVolumeSlider != null)
        {
            float savedSFXVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            sfxVolumeSlider.value = savedSFXVolume;
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

            UpdateSFXVolumeText(savedSFXVolume);
        }
    }

    private void StartGame()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }

        SceneManager.LoadScene("GameScene");
    }

    // Update music volume
    private void OnMusicVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(value);
        }

        UpdateMusicVolumeText(value);
    }

    // Update SFX volume
    private void OnSFXVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(value);
        }

        UpdateSFXVolumeText(value);

        // Play test sound on change
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySelectedReward();
        }
    }

    // Update music volume text
    private void UpdateMusicVolumeText(float value)
    {
        if (musicVolumeText != null)
        {
            musicVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
        }
    }

    // Update SFX volume text
    private void UpdateSFXVolumeText(float value)
    {
        if (sfxVolumeText != null)
        {
            sfxVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
        }
    }
}