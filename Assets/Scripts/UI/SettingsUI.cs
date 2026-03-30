using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class SettingsUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject settingsPanel;

    [Header("Dropdowns")]
    [SerializeField] private TMP_Dropdown levelCountDropdown;
    [SerializeField] private TMP_Dropdown editLevelDropdown;

    [Header("Level Controls")]
    [SerializeField] private Slider ingredientSpeedSlider;
    [SerializeField] private TMP_Text ingredientSpeedValueLabel;

    [SerializeField] private Slider obstacleSpeedSlider;
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

        editLevelDropdown.ClearOptions();

        List<string> options = new();
        for (int i = 0; i < SettingsData.levelCount; i++)
        {
            options.Add($"Level {i + 1}");
        }

        editLevelDropdown.AddOptions(options);
        editLevelDropdown.value = Mathf.Clamp(currentEditedLevelIndex, 0, Mathf.Max(0, options.Count - 1));
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
            ingredientSpeedSlider.value = IntervalToSlider(s.ingredientSpawnInterval);

        if (obstacleSpeedSlider != null)
            obstacleSpeedSlider.value = IntervalToSlider(s.obstacleSpawnInterval);

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

        s.ingredientSpawnInterval = SliderToInterval(value);
        RefreshLabels();
    }

    private void OnObstacleSpeedChanged(float value)
    {
        LevelSettings s = SettingsData.GetLevelSettings(currentEditedLevelIndex);
        if (s == null) return;

        s.obstacleSpawnInterval = SliderToInterval(value);
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
            ingredientSpeedValueLabel.text = SpeedText(ingredientSpeedSlider.value);

        if (obstacleSpeedValueLabel != null)
            obstacleSpeedValueLabel.text = SpeedText(obstacleSpeedSlider.value);
    }

    private string SpeedText(float value)
    {
        if (value < 0.33f) return "Slow";
        if (value < 0.66f) return "Medium";
        return "Fast";
    }

    private float SliderToInterval(float sliderValue)
    {
        return Mathf.Lerp(2.5f, 0.5f, sliderValue);
    }

    private float IntervalToSlider(float interval)
    {
        return Mathf.InverseLerp(2.5f, 0.5f, interval);
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
}