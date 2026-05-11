using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;


    
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


    [Header("Participant Input")]
    [SerializeField] private TMP_InputField participantInput;

    [Header("Scene Names")]
    [SerializeField] private string gameplaySceneName = "GameScene";
    
    private void Start()
    {
        ShowPanel(mainMenuPanel);
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

        ShowPanel(instructionsPanel);
    }

    public void OnCloseInstructionsPressed()
    {
        ShowPanel(GetActiveThemeMenuPanel());
    }
    // "Let's Build!" button on instructions popup
    public void OnLetsBuildPressed()
    {
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
        SetPanelActive(mainMenuPanel, targetPanel == mainMenuPanel);
        SetPanelActive(burgerMenuPanel, targetPanel == burgerMenuPanel);
        SetPanelActive(lettersMenuPanel, targetPanel == lettersMenuPanel);
        SetPanelActive(numbersMenuPanel, targetPanel == numbersMenuPanel);
        SetPanelActive(instructionsPanel, targetPanel == instructionsPanel);
        SetPanelActive(participantPanel, targetPanel == participantPanel);
        SetPanelActive(settingsPanel, targetPanel == settingsPanel);
    }

    void SetPanelActive(GameObject panel, bool isActive)
    {
        if (panel != null)
            panel.SetActive(isActive);
    }
}
