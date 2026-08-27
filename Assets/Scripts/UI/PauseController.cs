using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using RTLTMPro;
using System.Collections;
using System.Collections.Generic;

public class PauseController : MonoBehaviour
{
    private static PauseController openMenuController;

    [Header("UI")]
    [SerializeField] private Button pauseResumeButton;
    [SerializeField] private Image buttonImage;
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private RawImage blurredBackgroundImage;
    [SerializeField] private bool hidePauseButtonWhileMenuOpen = true;
    [SerializeField] private GameObject[] hideWhilePauseMenuOpen;

    [Header("Home Exit Confirmation")]
    [SerializeField] private GameObject homeExitConfirmationPanel;
    [SerializeField] private TMP_Text homeExitConfirmationText;
    [SerializeField] private Button confirmHomeExitButton;
    [SerializeField] private Button cancelHomeExitButton;
    [SerializeField] private TMP_Text confirmHomeExitButtonText;
    [SerializeField] private TMP_Text cancelHomeExitButtonText;
    [SerializeField] private string homeExitMessageEnglish = "Are you sure you want to exit the game?";
    [SerializeField] private string homeExitMessageArabic = "هل أنت متأكد أنك تريد الخروج من اللعبة؟";
    [SerializeField] private string confirmHomeExitEnglish = "Yes, exit";
    [SerializeField] private string confirmHomeExitArabic = "نعم، خروج";
    [SerializeField] private string cancelHomeExitEnglish = "Stay";
    [SerializeField] private string cancelHomeExitArabic = "لا، ابقَ";

    [Header("Gameplay Visibility")]
    [SerializeField] private bool hideFallingObjectsWhileMenuOpen = true;
    [SerializeField] private bool hideHandMarkersWhileMenuOpen = true;

    [Header("Background Blur")]
    [SerializeField] private bool captureBlurredBackground = false;
    [SerializeField] private int blurDownsample = 8;
    [SerializeField] private int blurIterations = 2;
    [SerializeField] private int blurRadius = 2;

    [Header("Sprites")]
    [SerializeField] private Sprite pauseSprite;
    [SerializeField] private Sprite resumeSprite;

    [Header("Options")]
    [SerializeField] private bool pauseAudio = false;

    private bool isPaused;
    private Coroutine openPauseMenuRoutine;
    private Texture2D generatedBlurTexture;
    private float previousTimeScale = 1f;
    private bool[] hiddenObjectOriginalActiveStates;
    private bool isRestoringFromPauseMenu;
    private readonly Dictionary<GameObject, bool> runtimeHiddenOriginalActiveStates = new Dictionary<GameObject, bool>();

    public bool IsPaused => isPaused;

    private void Awake()
    {
        if (pauseResumeButton != null)
        {
            pauseResumeButton.onClick.RemoveListener(TogglePause);
            pauseResumeButton.onClick.AddListener(OpenPauseMenu);
        }

        CreateDefaultHomeExitConfirmationIfNeeded();
        SetPauseMenuVisible(false);
        SetHomeExitConfirmationVisible(false);
        WireHomeExitConfirmationButtons();
        RefreshUI();
    }

    private void OnDestroy()
    {
        if (openMenuController == this)
            openMenuController = null;

        if (openPauseMenuRoutine != null)
            StopCoroutine(openPauseMenuRoutine);

        ClearGeneratedBlurTexture();

        if (isPaused)
            Resume();

        if (pauseResumeButton != null)
            pauseResumeButton.onClick.RemoveListener(OpenPauseMenu);

        UnwireHomeExitConfirmationButtons();
    }

    public void TogglePause()
    {
        if (isPaused)
            ResumeGame();
        else
            OpenPauseMenu();
    }

    public void OpenPauseMenu()
    {
        openMenuController = this;
        Pause();

        if (!captureBlurredBackground)
        {
            SetPauseMenuHiddenObjectsVisible(false);
            SetPauseMenuVisible(true);
            return;
        }

        if (openPauseMenuRoutine != null)
            StopCoroutine(openPauseMenuRoutine);

        openPauseMenuRoutine = StartCoroutine(OpenPauseMenuRoutine());
    }

    public void ResumeGame()
    {
        if (openMenuController != null && openMenuController != this)
        {
            openMenuController.ResumeGame();
            return;
        }

        if (openPauseMenuRoutine != null)
        {
            StopCoroutine(openPauseMenuRoutine);
            openPauseMenuRoutine = null;
        }

        SetPauseMenuVisible(false);
        RestorePauseMenuHiddenObjects();
        ClearGeneratedBlurTexture();
        isRestoringFromPauseMenu = true;
        Resume();
        isRestoringFromPauseMenu = false;

        if (openMenuController == this)
            openMenuController = null;
    }

    public void ReplayLevel()
    {
        if (openMenuController != null && openMenuController != this)
        {
            openMenuController.ReplayLevel();
            return;
        }

        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
            SessionData.RequestedStartLevelIndex = gameManager.currentLevelIndex;

        RestoreTimeBeforeSceneChange();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoHome()
    {
        if (openMenuController != null && openMenuController != this)
        {
            openMenuController.GoHome();
            return;
        }

        if (homeExitConfirmationPanel != null)
        {
            ShowHomeExitConfirmation();
            return;
        }

        ConfirmGoHome();
    }

    public void ConfirmGoHome()
    {
        if (openMenuController != null && openMenuController != this)
        {
            openMenuController.ConfirmGoHome();
            return;
        }

        RestoreTimeBeforeSceneChange();
        SceneManager.LoadScene("MainMenu");
    }

    public void CancelGoHome()
    {
        if (openMenuController != null && openMenuController != this)
        {
            openMenuController.CancelGoHome();
            return;
        }

        SetHomeExitConfirmationVisible(false);
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

        if (!isRestoringFromPauseMenu)
        {
            ResumeGame();
            return;
        }

        Time.timeScale = Mathf.Approximately(previousTimeScale, 0f) ? 1f : previousTimeScale;
        isPaused = false;

        if (pauseAudio)
            AudioListener.pause = false;

        RefreshUI();
    }

    private void RestoreTimeBeforeSceneChange()
    {
        if (openPauseMenuRoutine != null)
        {
            StopCoroutine(openPauseMenuRoutine);
            openPauseMenuRoutine = null;
        }

        SetPauseMenuVisible(false);
        RestorePauseMenuHiddenObjects();
        ClearGeneratedBlurTexture();
        Time.timeScale = 1f;
        isPaused = false;

        if (pauseAudio)
            AudioListener.pause = false;

        if (openMenuController == this)
            openMenuController = null;
    }

    private void SetPauseMenuVisible(bool isVisible)
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(isVisible);
    }

    private void ShowHomeExitConfirmation()
    {
        ApplyHomeExitConfirmationText();
        SetHomeExitConfirmationVisible(true);
    }

    private void SetHomeExitConfirmationVisible(bool isVisible)
    {
        if (homeExitConfirmationPanel != null)
            homeExitConfirmationPanel.SetActive(isVisible);
    }

    private void WireHomeExitConfirmationButtons()
    {
        if (confirmHomeExitButton != null)
        {
            confirmHomeExitButton.onClick.RemoveListener(ConfirmGoHome);
            confirmHomeExitButton.onClick.AddListener(ConfirmGoHome);
        }

        if (cancelHomeExitButton != null)
        {
            cancelHomeExitButton.onClick.RemoveListener(CancelGoHome);
            cancelHomeExitButton.onClick.AddListener(CancelGoHome);
        }
    }

    private void UnwireHomeExitConfirmationButtons()
    {
        if (confirmHomeExitButton != null)
            confirmHomeExitButton.onClick.RemoveListener(ConfirmGoHome);

        if (cancelHomeExitButton != null)
            cancelHomeExitButton.onClick.RemoveListener(CancelGoHome);
    }

    private void ApplyHomeExitConfirmationText()
    {
        if (homeExitConfirmationText == null)
            return;

        bool useArabic = LanguageManager.Instance != null &&
            LanguageManager.Instance.CurrentLanguage == AppLanguage.Arabic;

        string message = useArabic ? homeExitMessageArabic : homeExitMessageEnglish;
        homeExitConfirmationText.isRightToLeftText = useArabic;
        homeExitConfirmationText.alignment = TextAlignmentOptions.Midline;
        homeExitConfirmationText.text = useArabic ? ShapeArabicText(message) : message;
        homeExitConfirmationText.SetAllDirty();
        homeExitConfirmationText.ForceMeshUpdate();

        ApplyButtonText(
            confirmHomeExitButtonText,
            useArabic ? confirmHomeExitArabic : confirmHomeExitEnglish,
            useArabic
        );
        ApplyButtonText(
            cancelHomeExitButtonText,
            useArabic ? cancelHomeExitArabic : cancelHomeExitEnglish,
            useArabic
        );
    }

    private void ApplyButtonText(TMP_Text target, string value, bool useArabic)
    {
        if (target == null)
            return;

        target.isRightToLeftText = useArabic;
        target.alignment = TextAlignmentOptions.Midline;
        target.text = useArabic ? ShapeArabicText(value) : value;
        target.SetAllDirty();
        target.ForceMeshUpdate();
    }

    private static string ShapeArabicText(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        FastStringBuilder output = new FastStringBuilder(Mathf.Max(RTLSupport.DefaultBufferSize, value.Length * 2));
        RTLSupport.FixText(value, output, true, false, true, true);
        return output.ToString();
    }

    private void CreateDefaultHomeExitConfirmationIfNeeded()
    {
        if (homeExitConfirmationPanel != null || pauseMenuPanel == null)
            return;

        homeExitConfirmationPanel = CreateUiObject("HomeExitConfirmationPanel", pauseMenuPanel.transform);

        RectTransform overlayRect = homeExitConfirmationPanel.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayImage = homeExitConfirmationPanel.AddComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.45f);

        GameObject dialog = CreateUiObject("Dialog", homeExitConfirmationPanel.transform);
        RectTransform dialogRect = dialog.GetComponent<RectTransform>();
        dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
        dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
        dialogRect.pivot = new Vector2(0.5f, 0.5f);
        dialogRect.anchoredPosition = Vector2.zero;
        dialogRect.sizeDelta = new Vector2(900f, 420f);

        Image dialogImage = dialog.AddComponent<Image>();
        dialogImage.color = new Color(1f, 1f, 1f, 0.96f);

        homeExitConfirmationText = CreateText(
            "Message",
            dialog.transform,
            new Vector2(760f, 170f),
            new Vector2(0f, 75f),
            52f,
            new Color(0.12f, 0.12f, 0.12f, 1f)
        );

        confirmHomeExitButton = CreateButton(
            "ConfirmButton",
            dialog.transform,
            new Vector2(-170f, -115f),
            new Color(0.72f, 0.18f, 0.18f, 1f),
            out confirmHomeExitButtonText
        );

        cancelHomeExitButton = CreateButton(
            "CancelButton",
            dialog.transform,
            new Vector2(170f, -115f),
            new Color(0.18f, 0.45f, 0.24f, 1f),
            out cancelHomeExitButtonText
        );
    }

    private GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private TMP_Text CreateText(
        string objectName,
        Transform parent,
        Vector2 size,
        Vector2 position,
        float fontSize,
        Color color
    )
    {
        GameObject obj = CreateUiObject(objectName, parent);
        RectTransform rectTransform = obj.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;

        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Midline;
        text.fontSize = fontSize;
        text.enableAutoSizing = true;
        text.fontSizeMin = 24f;
        text.fontSizeMax = fontSize;
        text.color = color;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    private Button CreateButton(
        string objectName,
        Transform parent,
        Vector2 position,
        Color color,
        out TMP_Text label
    )
    {
        GameObject obj = CreateUiObject(objectName, parent);
        RectTransform rectTransform = obj.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(280f, 95f);

        Image image = obj.AddComponent<Image>();
        image.color = color;

        Button button = obj.AddComponent<Button>();
        button.targetGraphic = image;

        label = CreateText(
            "Label",
            obj.transform,
            new Vector2(250f, 80f),
            Vector2.zero,
            34f,
            Color.white
        );

        return button;
    }

    private IEnumerator OpenPauseMenuRoutine()
    {
        SetPauseMenuVisible(false);

        if (blurredBackgroundImage != null)
            blurredBackgroundImage.enabled = false;

        if (captureBlurredBackground && blurredBackgroundImage != null)
        {
            CaptureBlurredBackground();
        }

        SetPauseMenuHiddenObjectsVisible(false);
        SetPauseMenuVisible(true);

        if (blurredBackgroundImage != null && blurredBackgroundImage.texture != null)
            blurredBackgroundImage.enabled = true;

        openPauseMenuRoutine = null;
        yield break;
    }

    private void CaptureBlurredBackground()
    {
        Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
        if (screenshot == null)
            return;

        ClearGeneratedBlurTexture();

        int downsample = Mathf.Max(1, blurDownsample);
        int width = Mathf.Max(1, screenshot.width / downsample);
        int height = Mathf.Max(1, screenshot.height / downsample);

        generatedBlurTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            float v = height <= 1 ? 0f : (float)y / (height - 1);

            for (int x = 0; x < width; x++)
            {
                float u = width <= 1 ? 0f : (float)x / (width - 1);
                pixels[y * width + x] = screenshot.GetPixelBilinear(u, v);
            }
        }

        Object.Destroy(screenshot);

        int radius = Mathf.Max(1, blurRadius);
        int iterations = Mathf.Max(1, blurIterations);

        for (int i = 0; i < iterations; i++)
            pixels = BoxBlur(pixels, width, height, radius);

        generatedBlurTexture.SetPixels(pixels);
        generatedBlurTexture.Apply(false, false);

        blurredBackgroundImage.texture = generatedBlurTexture;
    }

    private Color[] BoxBlur(Color[] source, int width, int height, int radius)
    {
        Color[] horizontal = new Color[source.Length];
        Color[] result = new Color[source.Length];
        int diameter = radius * 2 + 1;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color color = Color.clear;

                for (int offsetX = -radius; offsetX <= radius; offsetX++)
                {
                    int sampleX = Mathf.Clamp(x + offsetX, 0, width - 1);
                    color += source[y * width + sampleX];
                }

                horizontal[y * width + x] = color / diameter;
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color color = Color.clear;

                for (int offsetY = -radius; offsetY <= radius; offsetY++)
                {
                    int sampleY = Mathf.Clamp(y + offsetY, 0, height - 1);
                    color += horizontal[sampleY * width + x];
                }

                result[y * width + x] = color / diameter;
            }
        }

        return result;
    }

    private void ClearGeneratedBlurTexture()
    {
        if (blurredBackgroundImage != null)
            blurredBackgroundImage.texture = null;

        if (generatedBlurTexture != null)
        {
            Object.Destroy(generatedBlurTexture);
            generatedBlurTexture = null;
        }
    }

    private void SetPauseMenuHiddenObjectsVisible(bool isVisible)
    {
        if (!isVisible)
        {
            CacheHiddenObjectActiveStates();
            CacheRuntimeHiddenObjects();

            if (hidePauseButtonWhileMenuOpen && pauseResumeButton != null)
                pauseResumeButton.gameObject.SetActive(false);

            if (hideWhilePauseMenuOpen != null)
            {
                foreach (GameObject obj in hideWhilePauseMenuOpen)
                {
                    if (obj != null && obj != pauseMenuPanel)
                        obj.SetActive(false);
                }
            }

            HideRuntimeGameplayObjects();
            return;
        }

        RestorePauseMenuHiddenObjects();
    }

    private void RestorePauseMenuHiddenObjects()
    {
        if (hidePauseButtonWhileMenuOpen && pauseResumeButton != null)
            pauseResumeButton.gameObject.SetActive(true);

        if (hideWhilePauseMenuOpen == null || hiddenObjectOriginalActiveStates == null)
        {
            RestoreRuntimeHiddenObjects();
            return;
        }

        int count = Mathf.Min(hideWhilePauseMenuOpen.Length, hiddenObjectOriginalActiveStates.Length);
        for (int i = 0; i < count; i++)
        {
            if (hideWhilePauseMenuOpen[i] != null && hideWhilePauseMenuOpen[i] != pauseMenuPanel)
                hideWhilePauseMenuOpen[i].SetActive(hiddenObjectOriginalActiveStates[i]);
        }

        RestoreRuntimeHiddenObjects();
    }

    private void CacheHiddenObjectActiveStates()
    {
        if (hideWhilePauseMenuOpen == null)
        {
            hiddenObjectOriginalActiveStates = null;
            return;
        }

        hiddenObjectOriginalActiveStates = new bool[hideWhilePauseMenuOpen.Length];
        for (int i = 0; i < hideWhilePauseMenuOpen.Length; i++)
            hiddenObjectOriginalActiveStates[i] = hideWhilePauseMenuOpen[i] != null && hideWhilePauseMenuOpen[i].activeSelf;
    }

    private void CacheRuntimeHiddenObjects()
    {
        runtimeHiddenOriginalActiveStates.Clear();

        if (hideFallingObjectsWhileMenuOpen)
        {
            foreach (FallingIngredient fallingIngredient in FindObjectsOfType<FallingIngredient>())
                CacheRuntimeHiddenObject(fallingIngredient.gameObject);

            foreach (ControlledFallVelocity fallingObject in FindObjectsOfType<ControlledFallVelocity>())
                CacheRuntimeHiddenObject(fallingObject.gameObject);
        }

        if (hideHandMarkersWhileMenuOpen)
        {
            foreach (FollowNearestHandCluster handMarker in FindObjectsOfType<FollowNearestHandCluster>())
                CacheRuntimeHiddenObject(handMarker.gameObject);

            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                CacheRuntimeHiddenObject(gameManager.platePlaceholderL);
                CacheRuntimeHiddenObject(gameManager.platePlaceholderR);
            }
        }
    }

    private void CacheRuntimeHiddenObject(GameObject obj)
    {
        if (obj == null || obj == pauseMenuPanel || runtimeHiddenOriginalActiveStates.ContainsKey(obj))
            return;

        runtimeHiddenOriginalActiveStates.Add(obj, obj.activeSelf);
    }

    private void HideRuntimeGameplayObjects()
    {
        foreach (GameObject obj in runtimeHiddenOriginalActiveStates.Keys)
        {
            if (obj != null && obj != pauseMenuPanel)
                obj.SetActive(false);
        }
    }

    private void RestoreRuntimeHiddenObjects()
    {
        foreach (KeyValuePair<GameObject, bool> entry in runtimeHiddenOriginalActiveStates)
        {
            if (entry.Key != null && entry.Key != pauseMenuPanel)
                entry.Key.SetActive(entry.Value);
        }

        runtimeHiddenOriginalActiveStates.Clear();
    }

    private void RefreshUI()
    {
        if (buttonImage != null)
            buttonImage.sprite = isPaused ? resumeSprite : pauseSprite;
    }
}
