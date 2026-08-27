using TMPro;
using UnityEngine;
using RTLTMPro;

public class ArabicInputPreview : MonoBehaviour
{
    [SerializeField] private TMP_InputField sourceInput;
    [SerializeField] private TMP_Text previewText;
    [SerializeField] private TMP_FontAsset englishFont;
    [SerializeField] private TMP_FontAsset arabicFont;
    [SerializeField] private bool showOnlyForArabic = true;

    private void OnEnable()
    {
        if (sourceInput != null)
            sourceInput.onValueChanged.AddListener(HandleInputChanged);

        RefreshPreview();
    }

    private void OnDisable()
    {
        if (sourceInput != null)
            sourceInput.onValueChanged.RemoveListener(HandleInputChanged);
    }

    private void HandleInputChanged(string _)
    {
        RefreshPreview();
    }

    public void RefreshPreview()
    {
        if (sourceInput == null || previewText == null)
            return;

        string rawText = sourceInput.text;
        bool containsArabic = ContainsArabic(rawText);
        bool shouldShow = !string.IsNullOrWhiteSpace(rawText) &&
            (!showOnlyForArabic || containsArabic);

        previewText.gameObject.SetActive(shouldShow);

        if (!shouldShow)
        {
            previewText.text = string.Empty;
            return;
        }

        TMP_FontAsset selectedFont = containsArabic ? arabicFont : englishFont;
        if (selectedFont != null)
        {
            previewText.font = selectedFont;

            if (selectedFont.material != null)
                previewText.fontSharedMaterial = selectedFont.material;
        }

        previewText.isRightToLeftText = containsArabic;
        previewText.alignment = containsArabic
            ? TextAlignmentOptions.MidlineRight
            : TextAlignmentOptions.MidlineLeft;
        previewText.text = containsArabic ? ShapeArabicText(rawText) : rawText;
        previewText.SetAllDirty();
        previewText.ForceMeshUpdate();
    }

    private static bool ContainsArabic(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        foreach (char character in value)
        {
            if ((character >= '\u0600' && character <= '\u06FF') ||
                (character >= '\u0750' && character <= '\u077F') ||
                (character >= '\u08A0' && character <= '\u08FF'))
            {
                return true;
            }
        }

        return false;
    }

    private static string ShapeArabicText(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        FastStringBuilder output = new FastStringBuilder(Mathf.Max(RTLSupport.DefaultBufferSize, value.Length * 2));
        RTLSupport.FixText(value, output, true, false, true, true);
        return output.ToString();
    }
}
