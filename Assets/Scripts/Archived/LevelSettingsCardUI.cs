using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
        ingredientSpeedLabel.text = SpeedText(ingredientSpeedSlider.value);
        obstacleSpeedLabel.text = SpeedText(obstacleSpeedSlider.value);
        // maxIngredientsLabel.text = Mathf.RoundToInt(maxIngredientsSlider.value).ToString();
    }

    private string SpeedText(float value)
    {
        if (value < 0.33f) return "Slow";
        if (value < 0.66f) return "Medium";
        return "Fast";
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
}