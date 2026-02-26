using UnityEngine;
using UnityEngine.SceneManagement;


    
public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject instructionsPanel;
    [SerializeField] private string gameplaySceneName = "BurgerGame";
    
    private void Start()
    {
        mainMenuPanel.SetActive(true);
        instructionsPanel.SetActive(false);
    }

    public void OnPlayPressed()
    {
        mainMenuPanel.SetActive(false);
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
}