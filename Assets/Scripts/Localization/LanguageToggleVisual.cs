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
    [SerializeField] private Vector2 englishLabelPos = new Vector2(-52, 0);
    [SerializeField] private Vector2 arabicLabelPos = new Vector2(52, 0);
    [SerializeField] private Vector2 labelSize = new Vector2(90, 56);

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
        NormalizeToggleLabel(englishText, englishLabelPos);
        NormalizeToggleLabel(arabicText, arabicLabelPos);
    }

    private void NormalizeToggleLabel(TMP_Text text, Vector2 labelPos)
    {
        if (text == null)
            return;

        RectTransform rectTransform = text.rectTransform;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = labelPos;
        rectTransform.sizeDelta = labelSize;

        text.transform.localScale = Vector3.one;
        text.margin = Vector4.zero;
        text.horizontalAlignment = HorizontalAlignmentOptions.Center;
        text.verticalAlignment = VerticalAlignmentOptions.Middle;
        text.ForceMeshUpdate();
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
