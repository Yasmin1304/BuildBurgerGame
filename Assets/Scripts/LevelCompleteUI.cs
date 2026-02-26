using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelCompleteUI : MonoBehaviour
{   
    public ParticleSystem confettiLeft;
    public ParticleSystem confettiRight;

    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private Button nextLevelButton;

    private GameManager gameManager;

    void Awake()
    {
        gameManager = FindObjectOfType<GameManager>();
        Hide();
    }

    public void Show(int levelNumber, int score)
    {
        panel.SetActive(true);

        titleText.text = $"Level {levelNumber} Complete!";
        scoreText.text = $"Score: {score}";

        if (confettiLeft != null && confettiRight != null)
        {
            confettiLeft.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            confettiLeft.Play();

            confettiRight.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            confettiRight.Play();
        }
        Debug.Log("LevelCompleteUI.Show() fired - trying confetti");
        if (confettiLeft == null) Debug.LogError("confettiLeft reference is NULL!");
        else Debug.Log("confettiLeft is assigned: " + confettiLeft.name);


        if (confettiRight == null) Debug.LogError("confettiRight reference is NULL!");
        else Debug.Log("confettiRight is assigned: " + confettiRight.name);


        // Make sure we don’t stack listeners multiple times
        nextLevelButton.onClick.RemoveAllListeners();
        nextLevelButton.onClick.AddListener(() =>
        {
            Hide();
            gameManager.ConfirmNextLevel(); // researcher-controlled
        });
    }

    public void Hide()
    {
        panel.SetActive(false);
    }
}
