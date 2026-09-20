using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using RTLTMPro;

public class SettingsUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject settingsPanel;

    [Header("Dropdowns")]
    [SerializeField] private TMP_Dropdown levelCountDropdown;
    [SerializeField] private TMP_Dropdown editLevelDropdown;

    [Header("Level Controls")]
    [SerializeField] private Slider ingredientSpeedSlider;
    [SerializeField] private TMP_Text ingredientSpeedValueTitle;
    [SerializeField] private TMP_Text ingredientSpeedValueLabel;

    [SerializeField] private Slider obstacleSpeedSlider;
    [SerializeField] private TMP_Text obstacleSpeedValueTitle;
    [SerializeField] private TMP_Text obstacleSpeedValueLabel;

    [SerializeField] private TMP_InputField maxIngredientsInput;
    [SerializeField] private Toggle obstaclesToggle;

    [Header("Audio Controls")]
    [SerializeField] private Toggle buttonSoundsToggle;
    [SerializeField] private Toggle backgroundMusicToggle;

    [Header("Slider Visuals")]
    [SerializeField] private Color speedSliderFillColor = new Color(0.1f, 0.55f, 0.95f, 1f);
    [SerializeField] private Color speedSliderTrackColor = new Color(0.1f, 0.55f, 0.95f, 0.22f);
    [SerializeField] private Color speedSliderHandleColor = new Color(0f, 0.85f, 0.1f, 1f);
    [SerializeField] private Color speedSliderHandleHighlightedColor = new Color(0f, 1f, 0.12f, 1f);

    private int currentEditedLevelIndex = 0;
    private bool subscribedToLanguageChanges;
    private bool subscribedToAudioSettingsChanges;
    private bool isLoadingValues;
    private Coroutine pendingLayoutRefresh;

    private void Awake()
    {
        if (levelCountDropdown != null)
        {
            levelCountDropdown.onValueChanged.RemoveAllListeners();
            levelCountDropdown.onValueChanged.AddListener(OnLevelCountChanged);
        }

        if (editLevelDropdown != null)
        {
            editLevelDropdown.onValueChanged.RemoveAllListeners();
            editLevelDropdown.onValueChanged.AddListener(OnEditLevelChanged);
        }

        if (ingredientSpeedSlider != null)
        {
            ingredientSpeedSlider.onValueChanged.RemoveAllListeners();
            ingredientSpeedSlider.onValueChanged.AddListener(OnIngredientSpeedChanged);
        }

        if (obstacleSpeedSlider != null)
        {
            obstacleSpeedSlider.onValueChanged.RemoveAllListeners();
            obstacleSpeedSlider.onValueChanged.AddListener(OnObstacleSpeedChanged);
        }

        if (maxIngredientsInput != null)
        {
            maxIngredientsInput.onEndEdit.RemoveAllListeners();
            maxIngredientsInput.onEndEdit.AddListener(OnMaxIngredientsChanged);
        }

        if (obstaclesToggle != null)
        {
            obstaclesToggle.onValueChanged.RemoveAllListeners();
            obstaclesToggle.onValueChanged.AddListener(OnObstaclesToggleChanged);
        }

        if (buttonSoundsToggle != null)
        {
            buttonSoundsToggle.onValueChanged.RemoveAllListeners();
            buttonSoundsToggle.onValueChanged.AddListener(OnButtonSoundsToggleChanged);
        }

        if (backgroundMusicToggle != null)
        {
            backgroundMusicToggle.onValueChanged.RemoveAllListeners();
            backgroundMusicToggle.onValueChanged.AddListener(OnBackgroundMusicToggleChanged);
        }

    }

    private void OnEnable()
    {
        SubscribeToLanguageChanges();
        SubscribeToAudioSettingsChanges();
        LoadCurrentValues();
    }

    private void OnDisable()
    {
        UnsubscribeFromLanguageChanges();
        UnsubscribeFromAudioSettingsChanges();
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);

        LoadCurrentValues();
    }

    public void CloseSettings()
    {
        ResetSettingsDataToDefaults();
        LoadCurrentValues();

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void LoadCurrentValues()
    {
        isLoadingValues = true;

        if (levelCountDropdown != null)
            levelCountDropdown.value = Mathf.Clamp(SettingsData.levelCount - 1, 0, levelCountDropdown.options.Count - 1);

        RebuildEditLevelDropdown();

        currentEditedLevelIndex = Mathf.Clamp(currentEditedLevelIndex, 0, SettingsData.levelCount - 1);

        if (editLevelDropdown != null)
            editLevelDropdown.value = currentEditedLevelIndex;

        RefreshAudioToggles();

        LoadSelectedLevelIntoUI();
        isLoadingValues = false;
        RefreshLocalizedLayoutDirection();
        QueueLocalizedLayoutRefresh();
    }

    private void OnLevelCountChanged(int dropdownIndex)
    {
        if (isLoadingValues)
            return;

        SettingsData.levelCount = dropdownIndex + 1;

        if (currentEditedLevelIndex >= SettingsData.levelCount)
            currentEditedLevelIndex = SettingsData.levelCount - 1;

        RebuildEditLevelDropdown();
        LoadSelectedLevelIntoUI();
    }

    private void RebuildEditLevelDropdown()
    {
        if (editLevelDropdown == null) return;

        List<string> options = new();
        for (int i = 0; i < SettingsData.levelCount; i++)
        {
            options.Add(BuildLocalizedLevelOption(i + 1));
        }

        editLevelDropdown.ClearOptions();
        editLevelDropdown.AddOptions(options);
        editLevelDropdown.value = Mathf.Clamp(currentEditedLevelIndex, 0, Mathf.Max(0, options.Count - 1));
        ApplyDropdownTextDirection(editLevelDropdown);
        editLevelDropdown.RefreshShownValue();
    }

    private void OnEditLevelChanged(int levelIndex)
    {
        if (isLoadingValues)
            return;

        currentEditedLevelIndex = levelIndex;
        LoadSelectedLevelIntoUI();
    }

    private void LoadSelectedLevelIntoUI()
    {
        LevelSettings s = SettingsData.GetLevelSettings(currentEditedLevelIndex);
        if (s == null) return;

        if (ingredientSpeedSlider != null)
            ingredientSpeedSlider.value = FallSpeedToSlider(s.ingredientFallSpeed);

        if (obstacleSpeedSlider != null)
            obstacleSpeedSlider.value = FallSpeedToSlider(s.obstacleFallSpeed);

        if (maxIngredientsInput != null)
        {
            maxIngredientsInput.text = s.maxIngredients.ToString();
            ApplyNumericInputDirection(maxIngredientsInput);
        }

        if (obstaclesToggle != null)
            obstaclesToggle.isOn = s.enableObstacles;

        SetObstacleSpeedControlsVisible(s.enableObstacles);
        RefreshLabels();
    }

    private void OnIngredientSpeedChanged(float value)
    {
        if (isLoadingValues)
            return;

        LevelSettings s = SettingsData.GetLevelSettings(currentEditedLevelIndex);
        if (s == null) return;

        s.ingredientFallSpeed = SliderToFallSpeed(value);
        RefreshLabels();
    }

    private void OnObstacleSpeedChanged(float value)
    {
        if (isLoadingValues)
            return;

        LevelSettings s = SettingsData.GetLevelSettings(currentEditedLevelIndex);
        if (s == null) return;

        s.obstacleFallSpeed = SliderToFallSpeed(value);
        RefreshLabels();
    }

    private void OnMaxIngredientsChanged(string value)
    {
        LevelSettings s = SettingsData.GetLevelSettings(currentEditedLevelIndex);
        if (s == null) return;

        if (int.TryParse(value, out int result))
        {
            result = Mathf.Clamp(result, 1, 50);
            s.maxIngredients = result;
            maxIngredientsInput.text = result.ToString();
        }
        else
        {
            maxIngredientsInput.text = s.maxIngredients.ToString();
        }

        ApplyNumericInputDirection(maxIngredientsInput);
    }

    private void OnObstaclesToggleChanged(bool value)
    {
        if (isLoadingValues)
            return;

        LevelSettings s = SettingsData.GetLevelSettings(currentEditedLevelIndex);
        if (s == null) return;

        s.enableObstacles = value;
        SetObstacleSpeedControlsVisible(value);
        RefreshLabels();
        QueueLocalizedLayoutRefresh();
    }

    private void OnButtonSoundsToggleChanged(bool value)
    {
        if (isLoadingValues)
            return;

        SettingsData.SetButtonSoundsEnabled(value);
        UIAudioManager.RefreshAll();
    }

    private void OnBackgroundMusicToggleChanged(bool value)
    {
        if (isLoadingValues)
            return;

        SettingsData.SetBackgroundMusicEnabled(value);
        UIAudioManager.RefreshAll();
    }

    private void RefreshLabels()
    {
        ApplySpeedSliderVisuals();

        if (ingredientSpeedValueLabel != null)
        {
            UpdateSpeedLabel(ingredientSpeedValueLabel, ingredientSpeedSlider.value);
            ApplyLabelAlignment(ingredientSpeedValueLabel);
        }

        if (obstacleSpeedValueLabel != null)
        {
            UpdateSpeedLabel(obstacleSpeedValueLabel, obstacleSpeedSlider.value);
            ApplyLabelAlignment(obstacleSpeedValueLabel);
        }
    }

    private string SpeedText(float value)
    {
        GetSpeedLocalization(value, out string key, out string fallback);
        bool useArabicLayout = IsArabicActive();
        float fallSpeed = SliderToFallSpeed(value);

        string speedLabel;
        if (useArabicLayout)
            speedLabel = GetArabicSpeedLabel(key);
        else if (LanguageManager.Instance != null)
            speedLabel = LanguageManager.Instance.GetText(key, fallback);
        else
            speedLabel = fallback;

        string speedValueText = useArabicLayout
            ? $"{fallSpeed:0.0} \u0648\u062D\u062F\u0629/\u062B\u0627\u0646\u064A\u0629"
            : $"{fallSpeed:0.0} units/s";
        string text = $"{speedLabel}\n{speedValueText}";

        if (!useArabicLayout)
            return text;

        FastStringBuilder output = new FastStringBuilder(Mathf.Max(RTLSupport.DefaultBufferSize, text.Length * 2));
        RTLSupport.FixText(text, output, true, false, true, true);
        return output.ToString();
    }

    private void GetSpeedLocalization(float value, out string key, out string fallback)
    {
        key = string.Empty;
        fallback = string.Empty;

        if (value < 0.33f)
        {
            key = "TXT_Speed_Slow";
            fallback = "Slow";
        }
        else if (value < 0.66f)
        {
            key = "TXT_Speed_Medium";
            fallback = "Medium";
        }
        else
        {
            key = "TXT_Speed_Fast";
            fallback = "Fast";
        }
    }

    private string GetArabicSpeedLabel(string key)
    {
        return key switch
        {
            "TXT_Speed_Slow" => "\u0628\u0637\u064A\u0621",
            "TXT_Speed_Medium" => "\u0645\u062A\u0648\u0633\u0637",
            "TXT_Speed_Fast" => "\u0633\u0631\u064A\u0639",
            _ => key
        };
    }

    private float SliderToFallSpeed(float sliderValue)
    {
        return Mathf.Lerp(1f, 4f, sliderValue);
    }

    private float FallSpeedToSlider(float fallSpeed)
    {
        return Mathf.InverseLerp(1f, 4f, fallSpeed);
    }

    public void ApplySettings()
    {
        Debug.Log("Per-level settings applied.");

        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null)
            gm.ApplyLevel(gm.currentLevelIndex);
    }

    public void ResetSettings()
    {
        ResetSettingsDataToDefaults();
        currentEditedLevelIndex = 0;
        LoadCurrentValues();
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    private string BuildLocalizedLevelOption(int levelNumber)
    {
        string format = LanguageManager.Instance != null
            ? LanguageManager.Instance.GetText("BTN_EditLevel_Format", "Level {0}")
            : "Level {0}";

        string text = string.Format(format, levelNumber);
        if (!IsArabicActive())
            return text;

        FastStringBuilder output = new FastStringBuilder(Mathf.Max(RTLSupport.DefaultBufferSize, text.Length * 2));
        RTLSupport.FixText(text, output, true, false, true, true);
        return output.ToString();
    }

    private void ApplyDropdownTextDirection(TMP_Dropdown dropdown)
    {
        bool useArabicLayout = IsArabicActive();

        if (dropdown.captionText != null)
        {
            dropdown.captionText.isRightToLeftText = useArabicLayout;
            dropdown.captionText.alignment = useArabicLayout ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            dropdown.captionText.SetAllDirty();
            dropdown.captionText.ForceMeshUpdate();
        }

        if (dropdown.itemText != null)
        {
            dropdown.itemText.isRightToLeftText = useArabicLayout;
            dropdown.itemText.alignment = useArabicLayout ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            dropdown.itemText.SetAllDirty();
            dropdown.itemText.ForceMeshUpdate();
        }
    }

    private bool IsArabicActive()
    {
        return LanguageManager.Instance != null &&
               LanguageManager.Instance.CurrentLanguage == AppLanguage.Arabic;
    }

    private void ApplyLabelAlignment(TMP_Text label)
    {
        if (label == null)
            return;

        bool useArabicLayout = IsArabicActive();
        label.isRightToLeftText = useArabicLayout;
        label.alignment = useArabicLayout ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
        label.SetAllDirty();
        label.ForceMeshUpdate();
    }

    private void UpdateSpeedLabel(TMP_Text label, float value)
    {
        if (label == null)
            return;

        LocalizedText localizedText = label.GetComponent<LocalizedText>();
        if (localizedText != null)
            localizedText.enabled = false;

        label.text = SpeedText(value);
    }

    private void HandleLanguageChanged(AppLanguage _)
    {
        RebuildEditLevelDropdown();
        RefreshLabels();
        ApplyNumericInputDirection(maxIngredientsInput);
        RefreshLocalizedLayoutDirection();
        QueueLocalizedLayoutRefresh();
    }

    private void ApplySpeedSliderVisuals()
    {
        ApplySpeedSliderVisuals(ingredientSpeedSlider);
        ApplySpeedSliderVisuals(obstacleSpeedSlider);
    }

    private void SetObstacleSpeedControlsVisible(bool isVisible)
    {
        SetObjectActive(obstacleSpeedValueTitle, isVisible);
        SetObjectActive(obstacleSpeedSlider, isVisible);
        SetObjectActive(obstacleSpeedValueLabel, isVisible);
    }

    private void SetObjectActive(Component component, bool isActive)
    {
        if (component != null)
            component.gameObject.SetActive(isActive);
    }

    private void ResetSettingsDataToDefaults()
    {
        SettingsData.ResetToDefaults();
        currentEditedLevelIndex = 0;
    }

    public void DiscardChanges()
    {
        ResetSettingsDataToDefaults();
        LoadCurrentValues();
    }

    private void RefreshLocalizedLayoutDirection()
    {
        GameObject root = settingsPanel != null ? settingsPanel : gameObject;
        foreach (LocalizedLayoutDirection layoutDirection in
            root.GetComponentsInChildren<LocalizedLayoutDirection>(true))
        {
            layoutDirection.Refresh();
        }

        ApplySpeedControlGroupDirection(
            ingredientSpeedSlider,
            ingredientSpeedValueLabel
        );
        ApplySpeedControlGroupDirection(
            obstacleSpeedSlider,
            obstacleSpeedValueLabel
        );
        ApplySettingsRowDirection(
            ingredientSpeedValueTitle,
            ingredientSpeedSlider
        );
        ApplySettingsRowDirection(
            obstacleSpeedValueTitle,
            obstacleSpeedSlider
        );
        ApplyObstacleToggleDirection();
        ApplyNumericInputDirection(maxIngredientsInput);

        if (root.TryGetComponent(out RectTransform rectTransform))
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }

    private void ApplyNumericInputDirection(TMP_InputField inputField)
    {
        if (inputField == null)
            return;

        inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
        bool useArabicLayout = IsArabicActive();

        ApplyNumericTextDirection(inputField.textComponent, useArabicLayout);
        if (inputField.placeholder is TMP_Text placeholderText)
            ApplyNumericTextDirection(placeholderText, useArabicLayout);

        inputField.ForceLabelUpdate();
    }

    private void ApplyNumericTextDirection(TMP_Text text, bool useArabicLayout)
    {
        if (text == null)
            return;

        text.isRightToLeftText = false;
        text.alignment = useArabicLayout
            ? TextAlignmentOptions.MidlineRight
            : TextAlignmentOptions.MidlineLeft;
        text.SetAllDirty();
        text.ForceMeshUpdate();
    }

    private void ApplySpeedControlGroupDirection(Slider slider, TMP_Text valueLabel)
    {
        if (slider == null || valueLabel == null)
            return;

        Transform sliderTransform = slider.transform;
        Transform labelTransform = valueLabel.transform;
        if (sliderTransform.parent == null ||
            sliderTransform.parent != labelTransform.parent)
        {
            return;
        }

        sliderTransform.SetSiblingIndex(0);
        labelTransform.SetSiblingIndex(1);
    }

    private void ApplySettingsRowDirection(TMP_Text title, Slider slider)
    {
        if (title == null || slider == null || slider.transform.parent == null)
            return;

        Transform titleTransform = title.transform;
        Transform controlGroupTransform = slider.transform.parent;
        if (titleTransform.parent == null ||
            titleTransform.parent != controlGroupTransform.parent)
        {
            return;
        }

        if (IsArabicActive())
        {
            controlGroupTransform.SetSiblingIndex(0);
            titleTransform.SetSiblingIndex(1);
        }
        else
        {
            titleTransform.SetSiblingIndex(0);
            controlGroupTransform.SetSiblingIndex(1);
        }
    }

    private void ApplyObstacleToggleDirection()
    {
        if (obstaclesToggle == null || obstaclesToggle.targetGraphic == null)
            return;

        Transform checkboxTransform = obstaclesToggle.targetGraphic.transform;
        Transform labelTransform = null;
        TMP_Text[] labels = obstaclesToggle.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text label in labels)
        {
            if (label != null && label.transform.parent == obstaclesToggle.transform)
            {
                labelTransform = label.transform;
                break;
            }
        }

        if (labelTransform == null ||
            checkboxTransform.parent != obstaclesToggle.transform)
        {
            return;
        }

        if (IsArabicActive())
        {
            labelTransform.SetSiblingIndex(0);
            checkboxTransform.SetSiblingIndex(1);
        }
        else
        {
            checkboxTransform.SetSiblingIndex(0);
            labelTransform.SetSiblingIndex(1);
        }
    }

    private void QueueLocalizedLayoutRefresh()
    {
        if (!isActiveAndEnabled)
            return;

        if (pendingLayoutRefresh != null)
            StopCoroutine(pendingLayoutRefresh);

        pendingLayoutRefresh = StartCoroutine(RefreshLocalizedLayoutDirectionNextFrame());
    }

    private IEnumerator RefreshLocalizedLayoutDirectionNextFrame()
    {
        yield return null;
        RefreshLocalizedLayoutDirection();
        pendingLayoutRefresh = null;
    }

    private void ApplySpeedSliderVisuals(Slider slider)
    {
        if (slider == null)
            return;

        slider.direction = IsArabicActive()
            ? Slider.Direction.RightToLeft
            : Slider.Direction.LeftToRight;

        if (slider.fillRect != null &&
            slider.fillRect.TryGetComponent(out Image fillImage))
        {
            fillImage.color = speedSliderFillColor;
        }

        if (slider.transform.Find("Background") is Transform background &&
            background.TryGetComponent(out Image backgroundImage))
        {
            backgroundImage.color = speedSliderTrackColor;
        }

        if (slider.targetGraphic != null)
            slider.targetGraphic.color = speedSliderHandleColor;

        ColorBlock colors = slider.colors;
        colors.normalColor = speedSliderHandleColor;
        colors.highlightedColor = speedSliderHandleHighlightedColor;
        colors.pressedColor = speedSliderHandleHighlightedColor;
        colors.selectedColor = speedSliderHandleColor;
        slider.colors = colors;
    }

    private void RefreshAudioToggles()
    {
        if (buttonSoundsToggle != null)
            buttonSoundsToggle.isOn = SettingsData.enableButtonSounds;

        if (backgroundMusicToggle != null)
            backgroundMusicToggle.isOn = SettingsData.enableBackgroundMusic;
    }

    private void HandleAudioSettingsChanged()
    {
        bool wasLoadingValues = isLoadingValues;
        isLoadingValues = true;
        RefreshAudioToggles();
        isLoadingValues = wasLoadingValues;
    }

    private void SubscribeToAudioSettingsChanges()
    {
        if (subscribedToAudioSettingsChanges)
            return;

        SettingsData.AudioSettingsChanged += HandleAudioSettingsChanged;
        subscribedToAudioSettingsChanges = true;
    }

    private void UnsubscribeFromAudioSettingsChanges()
    {
        if (!subscribedToAudioSettingsChanges)
            return;

        SettingsData.AudioSettingsChanged -= HandleAudioSettingsChanged;
        subscribedToAudioSettingsChanges = false;
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
}
