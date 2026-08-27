using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RTLTMPro;

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
        ResolveReferences();
        Hide();
    }

    public void Show(int levelNumber, int score)
    {
        ResolveReferences();
        panel.SetActive(true);

        SetLocalizedText(titleText, "TXT_LevelComplete_Format", "Level {0} Complete!", levelNumber);
        SetLocalizedText(scoreText, "TXT_Score_Format", "Score: {0}", score);

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
        if (nextLevelButton == null)
        {
            Debug.LogError("LevelCompleteUI cannot continue because nextLevelButton is not assigned.");
            return;
        }

        nextLevelButton.onClick.RemoveAllListeners();
        nextLevelButton.onClick.AddListener(() =>
        {
            Hide();
            gameManager.ConfirmNextLevel(); // researcher-controlled
        });
    }

    void ResolveReferences()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();

        if (nextLevelButton == null && panel != null)
            nextLevelButton = panel.GetComponentInChildren<Button>(true);
    }

    public void Hide()
    {
        panel.SetActive(false);
    }

    string BuildLocalizedText(string key, string fallbackFormat, params object[] args)
    {
        string format = LanguageManager.Instance != null
            ? LanguageManager.Instance.GetText(key, fallbackFormat)
            : fallbackFormat;

        return string.Format(format, args);
    }

    void SetLocalizedText(TMP_Text target, string key, string fallbackFormat, params object[] args)
    {
        if (target == null)
            return;

        string text = BuildLocalizedText(key, fallbackFormat, args);
        bool useArabicLayout = LanguageManager.Instance != null &&
            LanguageManager.Instance.CurrentLanguage == AppLanguage.Arabic;

        target.isRightToLeftText = useArabicLayout;

        if (target is RTLTextMeshPro rtlText)
        {
            rtlText.text = text;
            rtlText.UpdateText();
        }
        else
        {
            target.text = useArabicLayout ? ShapeArabicText(text) : text;
        }

        ApplyTextDirection(target, useArabicLayout);
    }

    void ApplyTextDirection(TMP_Text target, bool useArabicLayout)
    {
        if (target == null)
            return;

        target.isRightToLeftText = useArabicLayout;
        target.SetAllDirty();
        target.ForceMeshUpdate();
    }

    string ShapeArabicText(string text)
    {
        FastStringBuilder output = new FastStringBuilder(Mathf.Max(RTLSupport.DefaultBufferSize, text.Length * 2));
        RTLSupport.FixText(text, output, true, false, true, true);
        return output.ToString();
    }
}
