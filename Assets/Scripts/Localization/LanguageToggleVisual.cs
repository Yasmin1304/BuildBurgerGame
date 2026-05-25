using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LanguageToggleVisual : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RectTransform slider;
    [SerializeField] private TMP_Text englishText;
    [SerializeField] private TMP_Text arabicText;

    [Header("Positions")]
    [SerializeField] private Vector2 englishPos = new Vector2(-52, 0);
    [SerializeField] private Vector2 arabicPos = new Vector2(52, 0);

    [Header("Animation")]
    [SerializeField] private float speed = 12f;

    private Vector2 targetPos;
    private bool subscribedToLanguageChanges;

    private void OnEnable()
    {
        RefreshInstant();
        SubscribeToLanguageChanges();
    }

    private void Start()
    {
        RefreshInstant();
    }

    private void OnDisable()
    {
        UnsubscribeFromLanguageChanges();
    }

    private void Update()
    {
        SubscribeToLanguageChanges();

        if (slider == null)
            return;

        slider.anchoredPosition = Vector2.Lerp(
            slider.anchoredPosition,
            targetPos,
            Time.deltaTime * speed
        );

        UpdateTextStyle();
    }

    public void RefreshInstant()
    {
        bool isArabic = LanguageManager.Instance != null &&
                        LanguageManager.Instance.CurrentLanguage == AppLanguage.Arabic;

        targetPos = isArabic ? arabicPos : englishPos;

        if (slider != null)
        {
            slider.anchoredPosition = targetPos;
            LayoutRebuilder.ForceRebuildLayoutImmediate(slider);
        }

        UpdateTextStyle();
    }

    public void RefreshAnimated()
    {
        bool isArabic = LanguageManager.Instance != null &&
                        LanguageManager.Instance.CurrentLanguage == AppLanguage.Arabic;

        targetPos = isArabic ? arabicPos : englishPos;
    }

    private void UpdateTextStyle()
    {
        bool isArabic = LanguageManager.Instance != null &&
                        LanguageManager.Instance.CurrentLanguage == AppLanguage.Arabic;

        if (englishText != null)
        {
            englishText.transform.localScale = isArabic ? Vector3.one : Vector3.one * 1.1f;
            englishText.ForceMeshUpdate();
        }

        if (arabicText != null)
        {
            arabicText.transform.localScale = isArabic ? Vector3.one * 1.1f : Vector3.one;
            arabicText.ForceMeshUpdate();
        }
    }

    private void HandleLanguageChanged(AppLanguage _)
    {
        RefreshAnimated();
    }

    private void SubscribeToLanguageChanges()
    {
        if (subscribedToLanguageChanges || LanguageManager.Instance == null)
            return;

        LanguageManager.Instance.LanguageChanged += HandleLanguageChanged;
        subscribedToLanguageChanges = true;
    }

    private void UnsubscribeFromLanguageChanges()
    {
        if (!subscribedToLanguageChanges || LanguageManager.Instance == null)
            return;

        LanguageManager.Instance.LanguageChanged -= HandleLanguageChanged;
        subscribedToLanguageChanges = false;
    }
}
