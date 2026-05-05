using System.Collections.Generic;
using TMPro;
using UnityEngine;
using RTLTMPro;

[RequireComponent(typeof(TMP_Dropdown))]
public class LocalizedDropdown : MonoBehaviour
{
    [SerializeField] private List<string> optionKeys = new List<string>();
    [SerializeField] private List<string> fallbackOptions = new List<string>();

    private TMP_Dropdown dropdown;

    void Awake()
    {
        dropdown = GetComponent<TMP_Dropdown>();
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

    public void SetOptions(IList<string> keys, IList<string> fallbacks = null)
    {
        optionKeys.Clear();
        fallbackOptions.Clear();

        if (keys != null)
            optionKeys.AddRange(keys);

        if (fallbacks != null)
            fallbackOptions.AddRange(fallbacks);

        Refresh();
    }

    public void Refresh()
    {
        if (dropdown == null)
            dropdown = GetComponent<TMP_Dropdown>();

        if (dropdown == null)
            return;

        int selectedIndex = dropdown.value;
        dropdown.options.Clear();

        int optionCount = Mathf.Max(optionKeys.Count, fallbackOptions.Count);
        for (int i = 0; i < optionCount; i++)
        {
            string key = i < optionKeys.Count ? optionKeys[i] : string.Empty;
            string fallback = i < fallbackOptions.Count ? fallbackOptions[i] : key;

            string text = LanguageManager.Instance != null
                ? LanguageManager.Instance.GetText(key, fallback)
                : fallback;

            if (IsArabicActive())
                text = ShapeArabicText(text);

            dropdown.options.Add(new TMP_Dropdown.OptionData(text));
        }

        if (dropdown.options.Count == 0)
            return;

        dropdown.value = Mathf.Clamp(selectedIndex, 0, dropdown.options.Count - 1);
        ApplyTextDirection();
        dropdown.RefreshShownValue();
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

    bool IsArabicActive()
    {
        return LanguageManager.Instance != null &&
            LanguageManager.Instance.CurrentLanguage == AppLanguage.Arabic;
    }

    void ApplyTextDirection()
    {
        bool useArabicLayout = IsArabicActive();

        if (dropdown.captionText != null)
        {
            dropdown.captionText.isRightToLeftText = useArabicLayout;
            dropdown.captionText.alignment = useArabicLayout ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            dropdown.captionText.SetAllDirty();
            dropdown.captionText.ForceMeshUpdate();
        }

        if (dropdown.itemText != null)
        {
            dropdown.itemText.isRightToLeftText = useArabicLayout;
            dropdown.itemText.alignment = useArabicLayout ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            dropdown.itemText.SetAllDirty();
            dropdown.itemText.ForceMeshUpdate();
        }
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
