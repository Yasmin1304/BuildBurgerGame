using System;
using TMPro;
using UnityEngine;
using RTLTMPro;

[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string localizationKey;
    [TextArea] [SerializeField] private string fallbackText;

    private TMP_Text targetText;
    private HorizontalAlignmentOptions originalHorizontalAlignment;
    private bool alignAsLocalizedTitle;

    void Awake()
    {
        targetText = GetComponent<TMP_Text>();
        originalHorizontalAlignment = targetText.horizontalAlignment;
        alignAsLocalizedTitle = gameObject.name.EndsWith(
            "Title",
            StringComparison.OrdinalIgnoreCase
        );
    }

    void OnEnable()
    {
        Subscribe();
        Refresh();
    }

    void Start()
    {
        Subscribe();
        Refresh();
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    public void SetKey(string key)
    {
        localizationKey = key;
        Refresh();
    }

    public void Refresh()
    {
        if (targetText == null)
            targetText = GetComponent<TMP_Text>();

        string resolvedText;

        if (LanguageManager.Instance == null)
        {
            resolvedText = BuildFallbackValue();
        }
        else
        {
            resolvedText = LanguageManager.Instance.GetText(localizationKey, BuildFallbackValue());
        }

        ApplyText(resolvedText);
    }

    void HandleLanguageChanged(AppLanguage _)
    {
        Refresh();
    }

    void Subscribe()
    {
        if (LanguageManager.Instance != null)
            LanguageManager.Instance.LanguageChanged += HandleLanguageChanged;
    }

    void Unsubscribe()
    {
        if (LanguageManager.Instance != null)
            LanguageManager.Instance.LanguageChanged -= HandleLanguageChanged;
    }

    string BuildFallbackValue()
    {
        if (!string.IsNullOrEmpty(fallbackText))
            return fallbackText;

        if (!string.IsNullOrEmpty(localizationKey))
            return localizationKey;

        return targetText != null ? targetText.text : string.Empty;
    }

    void ApplyText(string value)
    {
        if (targetText == null)
            return;

        bool useArabicLayout = LanguageManager.Instance != null &&
            LanguageManager.Instance.CurrentLanguage == AppLanguage.Arabic;

        string displayValue = useArabicLayout ? ShapeArabicText(value) : value;

        targetText.isRightToLeftText = useArabicLayout;
        if (alignAsLocalizedTitle)
        {
            targetText.horizontalAlignment = useArabicLayout
                ? MirrorHorizontalAlignment(originalHorizontalAlignment)
                : originalHorizontalAlignment;
        }

        targetText.text = displayValue;
        targetText.SetAllDirty();
        targetText.ForceMeshUpdate();
    }

    static HorizontalAlignmentOptions MirrorHorizontalAlignment(
        HorizontalAlignmentOptions alignment)
    {
        return alignment switch
        {
            HorizontalAlignmentOptions.Left => HorizontalAlignmentOptions.Right,
            HorizontalAlignmentOptions.Right => HorizontalAlignmentOptions.Left,
            _ => alignment
        };
    }

    string ShapeArabicText(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        FastStringBuilder output = new FastStringBuilder(Mathf.Max(RTLSupport.DefaultBufferSize, value.Length * 2));
        RTLSupport.FixText(value, output, true, false, true, true);
        return output.ToString();
    }
}
