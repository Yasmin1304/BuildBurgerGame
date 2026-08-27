using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


    
public class MainMenuUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject burgerMenuPanel;
    [SerializeField] private GameObject lettersMenuPanel;
    [SerializeField] private GameObject numbersMenuPanel;
    [SerializeField] private GameObject instructionsPanel;
    [SerializeField] private GameObject participantPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Shared UI")]
    [SerializeField] private GameObject sharedPanel;

    [Header("Participant Input")]
    [SerializeField] private TMP_InputField participantInput;

    [Header("Instruction Slides")]
    [SerializeField] private Image instructionImage;
    [SerializeField] private Sprite[] instructionSprites;

    [Header("Burger Instruction Slides")]
    [SerializeField] private Sprite[] burgerEnglishInstructionSprites;
    [SerializeField] private Sprite[] burgerArabicInstructionSprites;

    [Header("Letters Instruction Slides")]
    [SerializeField] private Sprite[] lettersEnglishInstructionSprites;
    [SerializeField] private Sprite[] lettersArabicInstructionSprites;

    [Header("Numbers Instruction Slides")]
    [SerializeField] private Sprite[] numbersEnglishInstructionSprites;
    [SerializeField] private Sprite[] numbersArabicInstructionSprites;

    [Header("Instruction Narration")]
    [SerializeField] private AudioSource instructionNarrationSource;
    [SerializeField] private bool playInstructionNarration = true;
    [SerializeField] private AudioClip[] instructionAudioClips;

    [Header("Burger Instruction Narration")]
    [SerializeField] private AudioClip[] burgerEnglishInstructionAudioClips;
    [SerializeField] private AudioClip[] burgerArabicInstructionAudioClips;

    [Header("Letters Instruction Narration")]
    [SerializeField] private AudioClip[] lettersEnglishInstructionAudioClips;
    [SerializeField] private AudioClip[] lettersArabicInstructionAudioClips;

    [Header("Numbers Instruction Narration")]
    [SerializeField] private AudioClip[] numbersEnglishInstructionAudioClips;
    [SerializeField] private AudioClip[] numbersArabicInstructionAudioClips;

    [Header("Instruction Navigation")]
    [SerializeField] private Button instructionPreviousButton;
    [SerializeField] private Button instructionNextButton;
    [SerializeField] private GameObject letsBuildButton;
    [SerializeField] private bool showLetsBuildButtonOnlyOnLastInstruction = true;

    [Header("Scene Names")]
    [SerializeField] private string gameplaySceneName = "GameScene";

    private GameObject currentPanel;
    private int instructionSlideIndex;
    private bool subscribedToLanguageChanges;

    private void OnEnable()
    {
        SubscribeToLanguageChanges();
    }
    
    private void Start()
    {
        SubscribeToLanguageChanges();
        WireInstructionButtons();
        EnsureInstructionNarrationSource();
        SetSharedPanelVisible(true);
        ShowPanel(mainMenuPanel);
    }

    private void OnDestroy()
    {
        StopInstructionNarration();
        UnwireInstructionButtons();
        UnsubscribeFromLanguageChanges();
    }

    private void OnDisable()
    {
        UnsubscribeFromLanguageChanges();
    }

    public void OnPlayPressed()
    {
        ShowPanel(participantPanel);
    }

    public void OnBurgerThemePressed()
    {
        SessionData.SelectedGameMode = GameMode.Burger;
        ShowPanel(burgerMenuPanel);
    }

    public void OnLettersThemePressed()
    {
        SessionData.SelectedGameMode = GameMode.Letters;
        ShowPanel(lettersMenuPanel);
    }

    public void OnNumbersThemePressed()
    {
        SessionData.SelectedGameMode = GameMode.Numbers;
        ShowPanel(numbersMenuPanel);
    }

    public void OnThemeMenuBackPressed()
    {
        ShowPanel(mainMenuPanel);
    }

    public void OnParticipantContinuePressed()
    {
        string enteredId = participantInput != null ? participantInput.text.Trim() : "";

        if (string.IsNullOrEmpty(enteredId))
        {
            Debug.LogWarning("Participant number is required.");
            return;
        }

        SessionData.ParticipantCode = enteredId;

        ResetInstructionSlides();
        ShowPanel(instructionsPanel);
    }

    public void OnCloseInstructionsPressed()
    {
        StopInstructionNarration();
        ResetInstructionSlides();
        ShowPanel(participantPanel);
    }
    // "Let's Build!" button on instructions popup
    public void OnLetsBuildPressed()
    {
        StopInstructionNarration();
        SessionData.RequestedStartLevelIndex = -1;
        SceneManager.LoadScene(gameplaySceneName);
    }
    public void OnParticipantBackPressed()
    {
        ShowPanel(GetActiveThemeMenuPanel());
    }
    public void OnSettingsPressed()
    {
        ShowPanel(settingsPanel);
    }
    public void OnSettingsClosePressed()
    {
        ShowPanel(GetActiveThemeMenuPanel());
    }

    public void OnHomePressed()
    {
        ShowPanel(mainMenuPanel);
    }

    public void OnBackPressed()
    {
        if (currentPanel == instructionsPanel)
        {
            if (CanGoToPreviousInstructionSlide())
                ShowPreviousInstructionSlide();
            else
                ShowPanel(participantPanel);

            return;
        }

        if (currentPanel == participantPanel || currentPanel == settingsPanel)
        {
            ShowPanel(GetActiveThemeMenuPanel());
            return;
        }

        if (currentPanel == burgerMenuPanel || currentPanel == lettersMenuPanel || currentPanel == numbersMenuPanel)
        {
            ShowPanel(mainMenuPanel);
            return;
        }

        ShowPanel(mainMenuPanel);
    }

    GameObject GetActiveThemeMenuPanel()
    {
        switch (SessionData.SelectedGameMode)
        {
            case GameMode.Letters:
                return lettersMenuPanel != null ? lettersMenuPanel : mainMenuPanel;

            case GameMode.Numbers:
                return numbersMenuPanel != null ? numbersMenuPanel : mainMenuPanel;

            case GameMode.Burger:
            default:
                return burgerMenuPanel != null ? burgerMenuPanel : mainMenuPanel;
        }
    }

    void ShowPanel(GameObject targetPanel)
    {
        if (targetPanel == null)
            targetPanel = mainMenuPanel;

        if (targetPanel == currentPanel)
            return;

        PreparePanelForDisplay(targetPanel);

        if (targetPanel == instructionsPanel)
            RefreshInstructionSlides(true);
        else
            StopInstructionNarration();

        SetPanelActive(mainMenuPanel, targetPanel == mainMenuPanel);
        SetPanelActive(burgerMenuPanel, targetPanel == burgerMenuPanel);
        SetPanelActive(lettersMenuPanel, targetPanel == lettersMenuPanel);
        SetPanelActive(numbersMenuPanel, targetPanel == numbersMenuPanel);
        SetPanelActive(instructionsPanel, targetPanel == instructionsPanel);
        SetPanelActive(participantPanel, targetPanel == participantPanel);
        SetPanelActive(settingsPanel, targetPanel == settingsPanel);
        SetSharedPanelVisible(true);

        currentPanel = targetPanel;

        if (currentPanel != null && !currentPanel.activeInHierarchy)
            Debug.LogWarning($"Target panel '{currentPanel.name}' is active but not visible in hierarchy. Check that all of its parent objects are active.");
    }

    void PreparePanelForDisplay(GameObject panel)
    {
        if (panel == null)
            return;

        if (panel.TryGetComponent(out RectTransform rectTransform) && rectTransform.localScale == Vector3.zero)
        {
            //rectTransform.localScale = Vector3.two;
            //rectTransform.localScale = new Vector3(2f, 2f, 2f);
            //Debug.LogWarning($"Panel '{panel.name}' had scale 0 and was reset to scale 1.");
        }
    }

    void SetSharedPanelVisible(bool isVisible)
    {
        SetPanelActive(sharedPanel, isVisible);

        if (isVisible && sharedPanel != null && !sharedPanel.activeInHierarchy)
            Debug.LogWarning("Shared panel is active but not visible in hierarchy. Make sure it is not inside a disabled page panel and that all parent objects are active.");
    }

    void SetPanelActive(GameObject panel, bool isActive)
    {
        if (panel != null)
            panel.SetActive(isActive);
    }

    public void OnInstructionNextPressed()
    {
        Sprite[] activeInstructionSprites = GetActiveInstructionSprites();
        if (activeInstructionSprites == null || activeInstructionSprites.Length <= 0)
            return;

        if (instructionSlideIndex < activeInstructionSprites.Length - 1)
        {
            instructionSlideIndex++;
            RefreshInstructionSlides(true);
        }
    }

    public void OnInstructionPreviousPressed()
    {
        ShowPreviousInstructionSlide();
    }

    private bool CanGoToPreviousInstructionSlide()
    {
        Sprite[] activeInstructionSprites = GetActiveInstructionSprites();
        return activeInstructionSprites != null &&
               activeInstructionSprites.Length > 1 &&
               instructionSlideIndex > 0;
    }

    private void ShowPreviousInstructionSlide()
    {
        if (!CanGoToPreviousInstructionSlide())
            return;

        instructionSlideIndex--;
        RefreshInstructionSlides(true);
    }

    private void ResetInstructionSlides()
    {
        instructionSlideIndex = 0;
        RefreshInstructionSlides();
    }

    private void RefreshInstructionSlides(bool narrateSlide = false)
    {
        Sprite[] activeInstructionSprites = GetActiveInstructionSprites();
        int slideCount = activeInstructionSprites != null ? activeInstructionSprites.Length : 0;
        bool hasSlides = slideCount > 0;

        if (hasSlides)
            instructionSlideIndex = Mathf.Clamp(instructionSlideIndex, 0, slideCount - 1);
        else
            instructionSlideIndex = 0;

        if (instructionImage != null && hasSlides)
        {
            instructionImage.sprite = activeInstructionSprites[instructionSlideIndex];
            instructionImage.preserveAspect = true;
        }

        bool hasMultipleSlides = slideCount > 1;
        bool isFirstSlide = instructionSlideIndex <= 0;
        bool isLastSlide = !hasSlides || instructionSlideIndex >= slideCount - 1;

        if (instructionPreviousButton != null)
            instructionPreviousButton.gameObject.SetActive(hasMultipleSlides && !isFirstSlide);

        if (instructionNextButton != null)
            instructionNextButton.gameObject.SetActive(hasMultipleSlides && !isLastSlide);

        if (letsBuildButton != null)
            letsBuildButton.SetActive(!showLetsBuildButtonOnlyOnLastInstruction || isLastSlide);

        if (narrateSlide)
            PlayInstructionNarration();
    }

    private Sprite[] GetActiveInstructionSprites()
    {
        bool isArabic = LanguageManager.Instance != null &&
            LanguageManager.Instance.CurrentLanguage == AppLanguage.Arabic;

        Sprite[] selectedSprites;
        switch (SessionData.SelectedGameMode)
        {
            case GameMode.Letters:
                selectedSprites = isArabic ? lettersArabicInstructionSprites : lettersEnglishInstructionSprites;
                break;

            case GameMode.Numbers:
                selectedSprites = isArabic ? numbersArabicInstructionSprites : numbersEnglishInstructionSprites;
                break;

            case GameMode.Burger:
            default:
                selectedSprites = isArabic ? burgerArabicInstructionSprites : burgerEnglishInstructionSprites;
                break;
        }

        return selectedSprites != null && selectedSprites.Length > 0
            ? selectedSprites
            : instructionSprites;
    }

    private AudioClip[] GetActiveInstructionAudioClips()
    {
        bool isArabic = LanguageManager.Instance != null &&
            LanguageManager.Instance.CurrentLanguage == AppLanguage.Arabic;

        AudioClip[] selectedClips;
        switch (SessionData.SelectedGameMode)
        {
            case GameMode.Letters:
                selectedClips = isArabic ? lettersArabicInstructionAudioClips : lettersEnglishInstructionAudioClips;
                break;

            case GameMode.Numbers:
                selectedClips = isArabic ? numbersArabicInstructionAudioClips : numbersEnglishInstructionAudioClips;
                break;

            case GameMode.Burger:
            default:
                selectedClips = isArabic ? burgerArabicInstructionAudioClips : burgerEnglishInstructionAudioClips;
                break;
        }

        return selectedClips != null && selectedClips.Length > 0
            ? selectedClips
            : instructionAudioClips;
    }

    private void PlayInstructionNarration()
    {
        if (!playInstructionNarration)
            return;

        AudioClip[] activeInstructionAudioClips = GetActiveInstructionAudioClips();
        if (activeInstructionAudioClips == null ||
            instructionSlideIndex < 0 ||
            instructionSlideIndex >= activeInstructionAudioClips.Length)
        {
            StopInstructionNarration();
            return;
        }

        AudioClip clip = activeInstructionAudioClips[instructionSlideIndex];
        if (clip == null)
        {
            StopInstructionNarration();
            return;
        }

        EnsureInstructionNarrationSource();

        if (instructionNarrationSource == null)
            return;

        instructionNarrationSource.Stop();
        instructionNarrationSource.clip = clip;
        instructionNarrationSource.Play();
    }

    private void StopInstructionNarration()
    {
        if (instructionNarrationSource == null)
            return;

        instructionNarrationSource.Stop();
        instructionNarrationSource.clip = null;
    }

    private void EnsureInstructionNarrationSource()
    {
        if (instructionNarrationSource != null)
        {
            instructionNarrationSource.playOnAwake = false;
            return;
        }

        instructionNarrationSource = GetComponent<AudioSource>();
        if (instructionNarrationSource == null)
            instructionNarrationSource = gameObject.AddComponent<AudioSource>();

        instructionNarrationSource.playOnAwake = false;
    }

    private void HandleLanguageChanged(AppLanguage _)
    {
        RefreshInstructionSlides(currentPanel == instructionsPanel);
    }

    private void SubscribeToLanguageChanges()
    {
        if (subscribedToLanguageChanges || LanguageManager.Instance == null)
            return;

        LanguageManager.Instance.LanguageChanged += HandleLanguageChanged;
        subscribedToLanguageChanges = true;
    }

    private void UnsubscribeFromLanguageChanges()
    {
        if (!subscribedToLanguageChanges || LanguageManager.Instance == null)
            return;

        LanguageManager.Instance.LanguageChanged -= HandleLanguageChanged;
        subscribedToLanguageChanges = false;
    }

    private void WireInstructionButtons()
    {
        if (instructionNextButton != null)
        {
            instructionNextButton.onClick.RemoveListener(OnInstructionNextPressed);
            instructionNextButton.onClick.AddListener(OnInstructionNextPressed);
        }

        if (instructionPreviousButton != null)
        {
            instructionPreviousButton.onClick.RemoveListener(OnInstructionPreviousPressed);
            instructionPreviousButton.onClick.AddListener(OnInstructionPreviousPressed);
        }
    }

    private void UnwireInstructionButtons()
    {
        if (instructionNextButton != null)
            instructionNextButton.onClick.RemoveListener(OnInstructionNextPressed);

        if (instructionPreviousButton != null)
            instructionPreviousButton.onClick.RemoveListener(OnInstructionPreviousPressed);
    }
}
