using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RTLTMPro;

public class LevelSettingsCardUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text obstaclesToggleText;
    [SerializeField] private Toggle obstaclesToggle;

    [SerializeField] private Slider ingredientSpeedSlider;
    [SerializeField] private TMP_Text ingredientSpeedLabel;

    [SerializeField] private Slider obstacleSpeedSlider;
    [SerializeField] private TMP_Text obstacleSpeedLabel;

    [SerializeField] private TMP_InputField maxIngredientsInput;
    [SerializeField] private TMP_Text maxIngredientsLabel;

    private int levelIndex;

    public void Setup(int index, LevelSettings settings)
    {
        levelIndex = index;

        obstaclesToggleText.text = $"Level {index + 1}";

        obstaclesToggle.isOn = settings.enableObstacles;

        ingredientSpeedSlider.value = IntervalToSlider(settings.ingredientSpawnInterval);
        obstacleSpeedSlider.value = IntervalToSlider(settings.obstacleSpawnInterval);

        maxIngredientsInput.text = settings.maxIngredients.ToString();

        RefreshLabels();
        RegisterListeners();
    }

    private void RegisterListeners()
    {
        obstaclesToggle.onValueChanged.RemoveAllListeners();
        ingredientSpeedSlider.onValueChanged.RemoveAllListeners();
        obstacleSpeedSlider.onValueChanged.RemoveAllListeners();
        maxIngredientsInput.onEndEdit.RemoveAllListeners();


        obstaclesToggle.onValueChanged.AddListener(OnObstacleToggleChanged);
        ingredientSpeedSlider.onValueChanged.AddListener(OnIngredientSpeedChanged);
        obstacleSpeedSlider.onValueChanged.AddListener(OnObstacleSpeedChanged);
        maxIngredientsInput.onEndEdit.AddListener(OnMaxIngredientsChanged);
    }

    private void OnObstacleToggleChanged(bool value)
    {
        var s = SettingsData.GetLevelSettings(levelIndex);
        if (s == null) return;

        s.enableObstacles = value;
    }

    private void OnIngredientSpeedChanged(float value)
    {
        var s = SettingsData.GetLevelSettings(levelIndex);
        if (s == null) return;

        s.ingredientSpawnInterval = SliderToInterval(value);
        RefreshLabels();
    }

    private void OnObstacleSpeedChanged(float value)
    {
        var s = SettingsData.GetLevelSettings(levelIndex);
        if (s == null) return;

        s.obstacleSpawnInterval = SliderToInterval(value);
        RefreshLabels();
    }

    private void OnMaxIngredientsChanged(string value)
    {
        var s = SettingsData.GetLevelSettings(levelIndex);
        if (s == null) return;

        if (int.TryParse(value, out int result))
        {
            result = Mathf.Clamp(result, 1, 50); // optional limits
            s.maxIngredients = result;
            maxIngredientsInput.text = result.ToString(); // normalize input
        }
        else
        {
            // revert if invalid input
            maxIngredientsInput.text = s.maxIngredients.ToString();
        }
    }

    private void RefreshLabels()
    {
        UpdateSpeedLabel(ingredientSpeedLabel, ingredientSpeedSlider.value);
        ApplyLabelAlignment(ingredientSpeedLabel);

        UpdateSpeedLabel(obstacleSpeedLabel, obstacleSpeedSlider.value);
        ApplyLabelAlignment(obstacleSpeedLabel);
        // maxIngredientsLabel.text = Mathf.RoundToInt(maxIngredientsSlider.value).ToString();
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

    // slider 0..1  -> interval 2.5 .. 0.5
    private float SliderToInterval(float sliderValue)
    {
        return Mathf.Lerp(2.5f, 0.5f, sliderValue);
    }

    // interval 2.5 .. 0.5 -> slider 0..1
    private float IntervalToSlider(float interval)
    {
        return Mathf.InverseLerp(2.5f, 0.5f, interval);
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
