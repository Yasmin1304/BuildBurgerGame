using TMPro;
using UnityEngine;

public class ThemeSelector : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown themeDropdown;
    [SerializeField] private GameMode defaultTheme = GameMode.Burger;

    void Awake()
    {
        SessionData.SelectedGameMode = defaultTheme;

        if (themeDropdown == null)
            return;

        themeDropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
        themeDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        ApplyDropdownValue(themeDropdown.value);
    }

    void OnDestroy()
    {
        if (themeDropdown != null)
            themeDropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
    }

    public void SetBurgerTheme()
    {
        SessionData.SelectedGameMode = GameMode.Burger;
    }

    public void SetLettersTheme()
    {
        SessionData.SelectedGameMode = GameMode.Letters;
    }

    public void SetNumbersTheme()
    {
        SessionData.SelectedGameMode = GameMode.Numbers;
    }

    public void SetTheme(int dropdownIndex)
    {
        ApplyDropdownValue(dropdownIndex);
    }

    void OnDropdownValueChanged(int dropdownIndex)
    {
        ApplyDropdownValue(dropdownIndex);
    }

    void ApplyDropdownValue(int dropdownIndex)
    {
        switch (dropdownIndex)
        {
            case 1:
                SessionData.SelectedGameMode = GameMode.Letters;
                break;

            case 2:
                SessionData.SelectedGameMode = GameMode.Numbers;
                break;

            case 0:
            default:
                SessionData.SelectedGameMode = GameMode.Burger;
                break;
        }
    }
}
