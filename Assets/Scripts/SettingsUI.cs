using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private TMP_Dropdown levelCountDropdown;
    [SerializeField] private TMP_InputField ingredientIntervalInput;
    [SerializeField] private TMP_InputField obstacleIntervalInput;
    [SerializeField] private TMP_InputField maxIngredientsInput;
    [SerializeField] private Toggle obstaclesToggle;

    void OnEnable()
    {
        LoadCurrentValues();
    }

    public void LoadCurrentValues()
    {
        levelCountDropdown.value = SettingsData.levelCount - 1;

        ingredientIntervalInput.text = SettingsData.ingredientSpawnInterval.ToString();
        obstacleIntervalInput.text = SettingsData.obstacleSpawnInterval.ToString();
        maxIngredientsInput.text = SettingsData.maxIngredients.ToString();

        obstaclesToggle.isOn = SettingsData.enableObstacles;
    }

    public void ApplySettings()
    {
        SettingsData.levelCount = levelCountDropdown.value + 1;

        SettingsData.ingredientSpawnInterval = float.Parse(ingredientIntervalInput.text);
        SettingsData.obstacleSpawnInterval = float.Parse(obstacleIntervalInput.text);
        SettingsData.maxIngredients = int.Parse(maxIngredientsInput.text);

        SettingsData.enableObstacles = obstaclesToggle.isOn;

        Debug.Log("Settings Applied");

        // OPTIONAL: apply immediately
        var gm = FindObjectOfType<GameManager>();
        if (gm != null)
        {
            gm.ApplyLevel(gm.currentLevelIndex);
        }

        settingsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void ResetSettings()
    {
        SettingsData.ResetToDefaults();
        LoadCurrentValues();
    }

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }
}