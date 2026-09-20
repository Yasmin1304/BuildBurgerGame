using System.Collections;
using System;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class ThemeCountdownSet
{
    public CountdownGameMode gameMode = CountdownGameMode.Burger;

    [Header("English")]
    public Sprite englishSprite3;
    public Sprite englishSprite2;
    public Sprite englishSprite1;
    public Sprite englishSpriteGo;

    [Header("Arabic")]
    public Sprite arabicSprite3;
    public Sprite arabicSprite2;
    public Sprite arabicSprite1;
    public Sprite arabicSpriteGo;

    public Sprite GetSprite(CountdownStep step, bool useArabic)
    {
        switch (step)
        {
            case CountdownStep.Three:
                return useArabic ? arabicSprite3 : englishSprite3;

            case CountdownStep.Two:
                return useArabic ? arabicSprite2 : englishSprite2;

            case CountdownStep.One:
                return useArabic ? arabicSprite1 : englishSprite1;

            case CountdownStep.Go:
            default:
                return useArabic ? arabicSpriteGo : englishSpriteGo;
        }
    }
}

public enum CountdownStep
{
    Three,
    Two,
    One,
    Go
}

public enum CountdownGameMode
{
    Burger,
    Letters,
    Numbers,
    LettersAndNumbers
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
    [SerializeField] private bool showProgressBar = false;
    [SerializeField] private GameObject whiteboardRoot;
    [SerializeField] private GameObject[] hideDuringCountdown;
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
        SetCountdownHiddenObjectsActive(false);

        if (countdownOverlay != null)
            countdownOverlay.SetActive(true);

        countdownCoroutine = StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        ThemeCountdownSet countdownSet = GetActiveCountdownSet();
        bool useArabic = LanguageManager.Instance != null &&
            LanguageManager.Instance.CurrentLanguage == AppLanguage.Arabic;

        yield return Show(GetCountdownSprite(countdownSet, CountdownStep.Three, useArabic), timePerNumber);
        yield return Show(GetCountdownSprite(countdownSet, CountdownStep.Two, useArabic), timePerNumber);
        yield return Show(GetCountdownSprite(countdownSet, CountdownStep.One, useArabic), timePerNumber);
        yield return Show(GetCountdownSprite(countdownSet, CountdownStep.Go, useArabic), timeForGo);

        if (countdownOverlay != null)
            countdownOverlay.SetActive(false);

        SetGameplayVisualsActive(true);
        SetCountdownHiddenObjectsActive(true);
        StartGame();
        CountdownCompleted?.Invoke();
        countdownCoroutine = null;
    }

    private IEnumerator Show(Sprite sprite, float duration)
    {
        if (countdownImage == null)
            yield break;

        countdownImage.sprite = sprite;
        countdownImage.transform.localScale = Vector3.one;

        yield return new WaitForSeconds(duration);
    }

    private Sprite GetCountdownSprite(
        ThemeCountdownSet countdownSet,
        CountdownStep step,
        bool useArabic)
    {
        Sprite themeSprite = countdownSet != null
            ? countdownSet.GetSprite(step, useArabic)
            : null;

        if (themeSprite != null)
            return themeSprite;

        switch (step)
        {
            case CountdownStep.Three:
                return sprite3;

            case CountdownStep.Two:
                return sprite2;

            case CountdownStep.One:
                return sprite1;

            case CountdownStep.Go:
            default:
                return spriteGo;
        }
    }

    void SetGameplayVisualsActive(bool isActive)
    {
        if (controlGameplayRootVisibility && gameplayRoot != null)
            gameplayRoot.SetActive(isActive);

        if (progressBarRoot != null)
            progressBarRoot.SetActive(isActive && showProgressBar);

        if (whiteboardRoot != null)
            whiteboardRoot.SetActive(isActive);
    }

    void SetCountdownHiddenObjectsActive(bool isActive)
    {
        if (hideDuringCountdown == null)
            return;

        foreach (GameObject obj in hideDuringCountdown)
        {
            if (obj != null)
                obj.SetActive(isActive);
        }
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
            if (set != null && IsExactCountdownMode(set.gameMode, selectedMode))
                return set;
        }

        foreach (ThemeCountdownSet set in themeCountdownSets)
        {
            if (set != null && IsSharedLettersAndNumbersMode(set.gameMode, selectedMode))
                return set;
        }

        return null;
    }

    private bool IsExactCountdownMode(CountdownGameMode countdownMode, GameMode selectedMode)
    {
        switch (countdownMode)
        {
            case CountdownGameMode.Letters:
                return selectedMode == GameMode.Letters;

            case CountdownGameMode.Numbers:
                return selectedMode == GameMode.Numbers;

            case CountdownGameMode.Burger:
                return selectedMode == GameMode.Burger;

            case CountdownGameMode.LettersAndNumbers:
            default:
                return false;
        }
    }

    private bool IsSharedLettersAndNumbersMode(CountdownGameMode countdownMode, GameMode selectedMode)
    {
        return countdownMode == CountdownGameMode.LettersAndNumbers &&
               (selectedMode == GameMode.Letters || selectedMode == GameMode.Numbers);
    }
}
