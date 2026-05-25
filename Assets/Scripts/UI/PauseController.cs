using UnityEngine;
using UnityEngine.UI;

public class PauseController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button pauseResumeButton;
    [SerializeField] private Image buttonImage;

    [Header("Sprites")]
    [SerializeField] private Sprite pauseSprite;
    [SerializeField] private Sprite resumeSprite;

    [Header("Options")]
    [SerializeField] private bool pauseAudio = false;

    private bool isPaused;
    private float previousTimeScale = 1f;

    public bool IsPaused => isPaused;

    private void Awake()
    {
        if (pauseResumeButton != null)
        {
            pauseResumeButton.onClick.RemoveListener(TogglePause);
            pauseResumeButton.onClick.AddListener(TogglePause);
        }

        RefreshUI();
    }

    private void OnDestroy()
    {
        if (isPaused)
            Resume();

        if (pauseResumeButton != null)
            pauseResumeButton.onClick.RemoveListener(TogglePause);
    }

    public void TogglePause()
    {
        if (isPaused)
            Resume();
        else
            Pause();
    }

    public void Pause()
    {
        if (isPaused)
            return;

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        isPaused = true;

        if (pauseAudio)
            AudioListener.pause = true;

        RefreshUI();
    }

    public void Resume()
    {
        if (!isPaused)
            return;

        Time.timeScale = Mathf.Approximately(previousTimeScale, 0f) ? 1f : previousTimeScale;
        isPaused = false;

        if (pauseAudio)
            AudioListener.pause = false;

        RefreshUI();
    }

    private void RefreshUI()
    {
        if (buttonImage != null)
            buttonImage.sprite = isPaused ? resumeSprite : pauseSprite;
    }
}