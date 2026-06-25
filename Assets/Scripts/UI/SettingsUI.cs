using System.Collections.Generic;
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

    private int currentEditedLevelIndex = 0;

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
    }

    private void OnEnable()
    {
        LoadCurrentValues();
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);

        LoadCurrentValues();
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void LoadCurrentValues()
    {
        if (levelCountDropdown != null)
            levelCountDropdown.value = Mathf.Clamp(SettingsData.levelCount - 1, 0, levelCountDropdown.options.Count - 1);

        RebuildEditLevelDropdown();

        currentEditedLevelIndex = Mathf.Clamp(currentEditedLevelIndex, 0, SettingsData.levelCount - 1);

        if (editLevelDropdown != null)
            editLevelDropdown.value = currentEditedLevelIndex;

        LoadSelectedLevelIntoUI();
    }

    private void OnLevelCountChanged(int dropdownIndex)
    {
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
            maxIngredientsInput.text = s.maxIngredients.ToString();

        if (obstaclesToggle != null)
            obstaclesToggle.isOn = s.enableObstacles;

        RefreshLabels();
    }

    private void OnIngredientSpeedChanged(float value)
    {
        LevelSettings s = SettingsData.GetLevelSettings(currentEditedLevelIndex);
        if (s == null) return;

        s.ingredientFallSpeed = SliderToFallSpeed(value);
        RefreshLabels();
    }

    private void OnObstacleSpeedChanged(float value)
    {
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
    }

    private void OnObstaclesToggleChanged(bool value)
    {
        LevelSettings s = SettingsData.GetLevelSettings(currentEditedLevelIndex);
        if (s == null) return;

        s.enableObstacles = value;
    }

    private void RefreshLabels()
    {
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

        string text = LanguageManager.Instance != null
            ? LanguageManager.Instance.GetText(key, fallback)
            : fallback;

        if (!IsArabicActive())
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
        SettingsData.ResetToDefaults();
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

        label.alignment = IsArabicActive() ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
        label.SetAllDirty();
        label.ForceMeshUpdate();
    }

    private void UpdateSpeedLabel(TMP_Text label, float value)
    {
        if (label == null)
            return;

        GetSpeedLocalization(value, out string key, out _);

        LocalizedText localizedText = label.GetComponent<LocalizedText>();
        if (localizedText != null)
            localizedText.SetKey(key);
        else
            label.text = SpeedText(value);
    }
}
