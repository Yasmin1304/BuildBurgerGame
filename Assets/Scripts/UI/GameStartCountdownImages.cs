using System.Collections;
using System;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class ThemeCountdownSet
{
    public GameMode gameMode = GameMode.Burger;
    public Sprite sprite3;
    public Sprite sprite2;
    public Sprite sprite1;
    public Sprite spriteGo;
}

public class GameStartCountdownImages : MonoBehaviour
{
    public event Action CountdownCompleted;

    [Header("UI")]
    [SerializeField] private GameObject countdownOverlay;
    [SerializeField] private Image countdownImage;

    [Header("Fallback Countdown Sprites")]
    [SerializeField] private Sprite sprite3;
    [SerializeField] private Sprite sprite2;
    [SerializeField] private Sprite sprite1;
    [SerializeField] private Sprite spriteGo;

    [Header("Theme Countdown Sets")]
    [SerializeField] private ThemeCountdownSet[] themeCountdownSets;

    [Header("Gameplay Visuals To Enable After Countdown")]
    [SerializeField] private GameObject gameplayRoot;
    [SerializeField] private GameObject progressBarRoot;
    [SerializeField] private GameObject whiteboardRoot;
    [SerializeField] private GameManager gameManager;

    [Header("Startup")]
    [SerializeField] private bool controlGameplayRootVisibility = false;

    [Header("Timing")]
    [SerializeField] private float timePerNumber = 0.8f;
    [SerializeField] private float timeForGo = 0.7f;

    [Header("Debug")]
    [SerializeField] private bool logCountdownDebug = true;

    private Coroutine countdownCoroutine;

    private void Start()
    {
        SetGameplayVisualsActive(false);

        if (countdownOverlay != null)
            countdownOverlay.SetActive(false);
    }

    public void BeginCountdown()
    {
        if (countdownCoroutine != null)
            StopCoroutine(countdownCoroutine);

        SetGameplayVisualsActive(false);

        if (countdownOverlay != null)
            countdownOverlay.SetActive(true);

        countdownCoroutine = StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        ThemeCountdownSet countdownSet = GetActiveCountdownSet();

        yield return Show(countdownSet != null && countdownSet.sprite3 != null ? countdownSet.sprite3 : sprite3, timePerNumber);
        yield return Show(countdownSet != null && countdownSet.sprite2 != null ? countdownSet.sprite2 : sprite2, timePerNumber);
        yield return Show(countdownSet != null && countdownSet.sprite1 != null ? countdownSet.sprite1 : sprite1, timePerNumber);
        yield return Show(countdownSet != null && countdownSet.spriteGo != null ? countdownSet.spriteGo : spriteGo, timeForGo);

        if (countdownOverlay != null)
            countdownOverlay.SetActive(false);

        SetGameplayVisualsActive(true);
        StartGame();
        CountdownCompleted?.Invoke();
        countdownCoroutine = null;
    }

    private IEnumerator Show(Sprite sprite, float duration)
    {
        if (countdownImage == null)
            yield break;

        countdownImage.sprite = sprite;

        // POP animation
        float t = 0f;
        countdownImage.transform.localScale = Vector3.one * 0.5f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float scale = Mathf.Lerp(0.5f, 1.0f, t / duration);
            countdownImage.transform.localScale = Vector3.one * scale;
            yield return null;
        }
    }

    void SetGameplayVisualsActive(bool isActive)
    {
        if (controlGameplayRootVisibility && gameplayRoot != null)
            gameplayRoot.SetActive(isActive);

        if (progressBarRoot != null)
            progressBarRoot.SetActive(isActive);

        if (whiteboardRoot != null)
            whiteboardRoot.SetActive(isActive);
    }

    void StartGame()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();

        if (logCountdownDebug)
            Debug.Log($"GameStartCountdownImages.StartGame called. GameManager={(gameManager != null ? gameManager.name : "null")}");

        if (gameManager != null)
            gameManager.BeginGame();
        else
            Debug.LogError("GameStartCountdownImages could not start the game because no active GameManager was found.");
    }

    ThemeCountdownSet GetActiveCountdownSet()
    {
        if (themeCountdownSets == null || themeCountdownSets.Length == 0)
            return null;

        GameMode selectedMode = SessionData.SelectedGameMode;
        foreach (ThemeCountdownSet set in themeCountdownSets)
        {
            if (set != null && set.gameMode == selectedMode)
                return set;
        }

        return null;
    }
}
