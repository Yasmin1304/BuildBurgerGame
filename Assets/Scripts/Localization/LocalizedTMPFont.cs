using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class LocalizedTMPFont : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset englishFont;
    [SerializeField] private TMP_FontAsset arabicFont;

    private TMP_Text targetText;

    void Awake()
    {
        targetText = GetComponent<TMP_Text>();
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

    public void Refresh()
    {
        if (targetText == null)
            targetText = GetComponent<TMP_Text>();

        if (LanguageManager.Instance == null)
            return;

        TMP_FontAsset selectedFont = LanguageManager.Instance.CurrentLanguage == AppLanguage.Arabic
            ? arabicFont
            : englishFont;

        if (selectedFont != null)
        {
            targetText.font = selectedFont;
            if (selectedFont.material != null)
                targetText.fontSharedMaterial = selectedFont.material;

            targetText.UpdateMeshPadding();
            targetText.SetAllDirty();
            targetText.SetMaterialDirty();
            targetText.SetVerticesDirty();
            targetText.ForceMeshUpdate();
        }
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
}
