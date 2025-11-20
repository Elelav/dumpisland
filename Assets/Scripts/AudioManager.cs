using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null || _instance.gameObject == null)
            {
                _instance = FindObjectOfType<AudioManager>();

                if (_instance == null)
                {
                    GameObject prefab = Resources.Load<GameObject>("AudioManager");

                    if (prefab != null)
                    {
                        GameObject go = Instantiate(prefab);
                        go.name = "AudioManager";
                        _instance = go.GetComponent<AudioManager>();
                        DontDestroyOnLoad(go);
                    }
                    else
                    {
                        Debug.LogWarning("AudioManager prefab not found in Resources! Creating empty.");
                        GameObject go = new GameObject("AudioManager");
                        _instance = go.AddComponent<AudioManager>();
                        DontDestroyOnLoad(go);
                    }
                }
            }
            return _instance;
        }
    }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Music")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip gameMusic;
    [SerializeField] private AudioClip bossMusic;

    [Header("Sound Effects - Player")]
    [SerializeField] private AudioClip playerHitSound;
    [SerializeField] private AudioClip playerDeathSound;
    [SerializeField] private AudioClip levelUpSound;

    [Header("Sound Effects - Enemies")]
    [SerializeField] private AudioClip enemyHitSound;
    [SerializeField] private AudioClip enemyDeathSound;
    [SerializeField] private AudioClip bossAbilitySound;
    [SerializeField] private AudioClip eliteAbilitySound;

    [Header("Sound Effects - Weapons")]
    [SerializeField] private AudioClip swordAttackSound;
    [SerializeField] private AudioClip bowAttackSound;
    [SerializeField] private AudioClip critSound;

    [Header("Sound Effects - UI")]
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private AudioClip dropOffSound;
    [SerializeField] private AudioClip purchaseSound;
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip waveStartSound;
    [SerializeField] private AudioClip selectedReward;

    [Header("Volume Settings")]
    [SerializeField] private float musicFadeTime = 1f;
    [SerializeField][Range(0f, 1f)] private float baseMusicVolume = 0.1f;
    [SerializeField][Range(0f, 1f)] private float baseSFXVolume = 1f;

    [Header("Hit Sound Settings")]
    [SerializeField] private int maxSimultaneousHitSounds = 5;
    [SerializeField] private float hitSoundMinCooldown = 0.03f;
    [SerializeField] private bool useVolumeScaling = true;

    // New: Separate volumes
    private float musicVolume = 1f;
    private float sfxVolume = 1f;

    private int currentHitSoundsPlaying = 0;
    private float lastHitSoundTime = -999f;
    private int hitsThisFrame = 0;
    private int lastHitFrame = -1;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += OnSceneLoaded;

            InitializeAudioSources();

            // Updated: Load separate volumes
            float savedMusicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
            float savedSFXVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            SetMusicVolume(savedMusicVolume);
            SetSFXVolume(savedSFXVolume);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (_instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (this == null || gameObject == null) return;

        InitializeAudioSources();
    }

    private void InitializeAudioSources()
    {
        if (this == null || gameObject == null)
        {
            Debug.LogWarning("AudioManager: Attempting to initialize destroyed object!");
            return;
        }

        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>();

            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
            }

            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            AudioSource[] sources = GetComponents<AudioSource>();

            if (sources.Length > 1)
            {
                sfxSource = sources[1];
            }
            else
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
            }

            sfxSource.playOnAwake = false;
        }
    }

    public void PlayMusic(AudioClip clip, bool fade = true)
    {
        if (clip == null) return;

        if (this == null || musicSource == null)
        {
            InitializeAudioSources();
            if (musicSource == null) return;
        }

        if (fade)
        {
            StartCoroutine(FadeMusic(clip));
        }
        else
        {
            musicSource.clip = clip;
            musicSource.volume = GetMusicVolume();
            musicSource.Play();
        }
    }

    public void PlayMenuMusic() => PlayMusic(menuMusic);
    public void PlayGameMusic() => PlayMusic(gameMusic);
    public void PlayBossMusic() => PlayMusic(bossMusic);

    private IEnumerator FadeMusic(AudioClip newClip)
    {
        float targetVolume = GetMusicVolume();
        float startVolume = musicSource.volume;

        float elapsed = 0f;
        while (elapsed < musicFadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / musicFadeTime);
            yield return null;
        }

        musicSource.Stop();
        musicSource.clip = newClip;
        musicSource.volume = 0f;
        musicSource.Play();

        elapsed = 0f;
        while (elapsed < musicFadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / musicFadeTime);
            yield return null;
        }

        musicSource.volume = targetVolume;
    }

    // Updated: Calculation with separate volume
    private float GetMusicVolume()
    {
        return baseMusicVolume * musicVolume;
    }

    // Updated: Calculation with separate volume
    private float GetSFXVolume()
    {
        return baseSFXVolume * sfxVolume;
    }

    public void PlaySound(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;

        if (this == null || sfxSource == null)
        {
            InitializeAudioSources();
            if (sfxSource == null) return;
        }

        sfxSource.PlayOneShot(clip, volumeScale * GetSFXVolume());
    }

    public void PlayWeaponSound(AudioClip clip, float volume = 0.4f, float pitchVariation = 0.1f)
    {
        if (clip == null) return;

        if (this == null || sfxSource == null)
        {
            InitializeAudioSources();
            if (sfxSource == null) return;
        }

        float originalPitch = sfxSource.pitch;
        sfxSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        sfxSource.PlayOneShot(clip, volume * GetSFXVolume());
        sfxSource.pitch = originalPitch;
    }

    public void PlayPlayerHit()
    {
        if (playerHitSound != null) PlaySound(playerHitSound, 0.2f);
    }

    public void PlayPlayerDeath()
    {
        if (playerDeathSound != null) PlaySound(playerDeathSound);
    }

    public void PlayLevelUp()
    {
        if (levelUpSound != null) PlaySound(levelUpSound, 0.3f);
    }

    public void PlayEnemyHit()
    {
        if (enemyHitSound == null) return;

        if (Time.time - lastHitSoundTime < hitSoundMinCooldown)
            return;

        if (Time.frameCount != lastHitFrame)
        {
            hitsThisFrame = 0;
            lastHitFrame = Time.frameCount;
        }
        hitsThisFrame++;

        if (currentHitSoundsPlaying >= maxSimultaneousHitSounds)
            return;

        float volume = 0.05f;
        if (useVolumeScaling && hitsThisFrame > 1)
        {
            volume = 0.05f / Mathf.Sqrt(hitsThisFrame);
        }

        lastHitSoundTime = Time.time;
        currentHitSoundsPlaying++;
        PlaySound(enemyHitSound, volume);

        StartCoroutine(ResetHitSoundCounter());
    }

    private IEnumerator ResetHitSoundCounter()
    {
        float duration = enemyHitSound != null ? enemyHitSound.length : 0.2f;
        yield return new WaitForSeconds(duration);
        currentHitSoundsPlaying = Mathf.Max(0, currentHitSoundsPlaying - 1);
    }

    public void PlayEnemyDeath()
    {
        if (enemyDeathSound != null) PlaySound(enemyDeathSound, 0.5f);
    }

    public void PlayBossAbility()
    {
        if (bossAbilitySound != null) PlaySound(bossAbilitySound, 0.1f);
    }

    public void PlayEliteAbility()
    {
        if (eliteAbilitySound != null) PlaySound(eliteAbilitySound, 0.1f);
    }

    public void PlaySwordAttack()
    {
        if (swordAttackSound != null) PlaySound(swordAttackSound, 0.3f);
    }

    public void PlayBowAttack()
    {
        if (bowAttackSound != null) PlaySound(bowAttackSound, 0.3f);
    }

    public void PlayCrit()
    {
        if (critSound != null) PlaySound(critSound, 0.6f);
    }

    public void PlayCollect()
    {
        if (collectSound != null) PlaySound(collectSound, 0.2f);
    }

    public void PlayDropOff()
    {
        if (dropOffSound != null) PlaySound(dropOffSound, 0.3f);
    }

    public void PlayPurchase()
    {
        if (purchaseSound != null) PlaySound(purchaseSound, 0.3f);
    }

    public void PlayButtonClick()
    {
        if (buttonClickSound != null) PlaySound(buttonClickSound, 0.3f);
    }

    public void PlayWaveStart()
    {
        if (waveStartSound != null) PlaySound(waveStartSound, 0.3f);
    }

    public void PlaySelectedReward()
    {
        if (selectedReward != null)
        {
            PlaySound(selectedReward, 0.3f);
        }
        else
        {
            PlayButtonClick();
        }
    }

    // New: Set music volume
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);

        if (this != null && musicSource != null && musicSource.isPlaying)
        {
            musicSource.volume = GetMusicVolume();
        }

        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
    }

    // New: Set SFX volume
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
    }

    // Deprecated: Keeping for backward compatibility
    public void SetVolume(float volume)
    {
        // Set both volumes equally
        SetMusicVolume(volume);
        SetSFXVolume(volume);
    }

    // New: Get music volume
    public float GetMusicVolumeValue()
    {
        return musicVolume;
    }

    // New: Get SFX volume
    public float GetSFXVolumeValue()
    {
        return sfxVolume;
    }

    // Deprecated: Keeping for backward compatibility
    public float GetMasterVolume()
    {
        return (musicVolume + sfxVolume) / 2f;
    }
}