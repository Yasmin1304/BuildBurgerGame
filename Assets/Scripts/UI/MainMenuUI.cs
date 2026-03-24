using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;


    
public class MainMenuUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject instructionsPanel;
    [SerializeField] private GameObject participantPanel;

    [Header("Participant Input")]
    [SerializeField] private TMP_InputField participantInput;

    [Header("Scene Names")]
    [SerializeField] private string gameplaySceneName = "BurgerGame";
    
    private void Start()
    {
        mainMenuPanel.SetActive(true);
        instructionsPanel.SetActive(false);
        participantPanel.SetActive(false);
    }

    public void OnPlayPressed()
    {
        mainMenuPanel.SetActive(false);
        instructionsPanel.SetActive(false);
        participantPanel.SetActive(true);
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

        participantPanel.SetActive(false);
        instructionsPanel.SetActive(true);
    }

    public void OnCloseInstructionsPressed()
    {
        instructionsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }
    // "Let's Build!" button on instructions popup
    public void OnLetsBuildPressed()
    {
        SceneManager.LoadScene(gameplaySceneName);
    }
    public void OnParticipantBackPressed()
    {
        participantPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }
}