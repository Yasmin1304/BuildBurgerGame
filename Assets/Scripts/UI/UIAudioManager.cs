using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIAudioManager : MonoBehaviour
{
    public static UIAudioManager Instance { get; private set; }

    [Header("Button Sounds")]
    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField, Range(0f, 1f)] private float buttonClickVolume = 0.8f;

    [Header("Background Music")]
    [SerializeField] private AudioClip backgroundMusicClip;
    [SerializeField, Range(0f, 1f)] private float backgroundMusicVolume = 0.35f;

    [Header("Shared Audio Toggle Icon")]
    [SerializeField] private Image audioToggleIcon;
    [SerializeField] private Sprite audioOnIcon;
    [SerializeField] private Sprite audioOffIcon;

    private AudioSource sfxSource;
    private AudioSource musicSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureAudioSources();
        SceneManager.sceneLoaded += HandleSceneLoaded;
        SettingsData.AudioSettingsChanged += HandleAudioSettingsChanged;
    }

    private void Start()
    {
        RefreshButtonBindings();
        ApplyAudioSettings();
        RefreshAudioToggleIcon();
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SettingsData.AudioSettingsChanged -= HandleAudioSettingsChanged;
        Instance = null;
    }

    public static void RefreshAll()
    {
        if (Instance == null)
            return;

        Instance.RefreshButtonBindings();
        Instance.ApplyAudioSettings();
        Instance.RefreshAudioToggleIcon();
    }

    public void ToggleAllAudio()
    {
        bool shouldEnableAudio =
            !SettingsData.enableButtonSounds &&
            !SettingsData.enableBackgroundMusic;

        SettingsData.SetAllAudioEnabled(shouldEnableAudio);
        RefreshButtonBindings();
        ApplyAudioSettings();
        RefreshAudioToggleIcon();
    }

    public void SetAllAudioEnabled(bool isEnabled)
    {
        SettingsData.SetAllAudioEnabled(isEnabled);
        RefreshButtonBindings();
        ApplyAudioSettings();
        RefreshAudioToggleIcon();
    }

    public void ApplyAudioSettings()
    {
        EnsureAudioSources();

        if (musicSource == null)
            return;

        musicSource.volume = backgroundMusicVolume;

        if (!SettingsData.enableBackgroundMusic || backgroundMusicClip == null)
        {
            musicSource.Stop();
            return;
        }

        if (musicSource.clip != backgroundMusicClip)
            musicSource.clip = backgroundMusicClip;

        musicSource.loop = true;
        if (!musicSource.isPlaying)
            musicSource.Play();
    }

    private void RefreshButtonBindings()
    {
        Button[] buttons = FindObjectsOfType<Button>(true);
        foreach (Button button in buttons)
        {
            if (button == null)
                continue;

            button.onClick.RemoveListener(PlayButtonClick);
            button.onClick.AddListener(PlayButtonClick);
        }
    }

    private void PlayButtonClick()
    {
        if (!SettingsData.enableButtonSounds ||
            buttonClickClip == null ||
            sfxSource == null)
        {
            return;
        }

        sfxSource.PlayOneShot(buttonClickClip, buttonClickVolume);
    }

    private void EnsureAudioSources()
    {
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
        }

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshButtonBindings();
        ApplyAudioSettings();
        RefreshAudioToggleIcon();
    }

    private void HandleAudioSettingsChanged()
    {
        ApplyAudioSettings();
        RefreshAudioToggleIcon();
    }

    private void RefreshAudioToggleIcon()
    {
        if (audioToggleIcon == null)
            return;

        bool hasAnyAudioEnabled =
            SettingsData.enableButtonSounds ||
            SettingsData.enableBackgroundMusic;

        Sprite targetSprite = hasAnyAudioEnabled ? audioOnIcon : audioOffIcon;
        if (targetSprite != null)
            audioToggleIcon.sprite = targetSprite;
    }
}
