using UnityEngine;

public class LanguageSwitcher : MonoBehaviour
{
    [SerializeField] private AppLanguage targetLanguage = AppLanguage.English;

    public void ApplyTargetLanguage()
    {
        if (LanguageManager.Instance == null)
            return;

        LanguageManager.Instance.SetLanguage(targetLanguage);
    }

    public void SetEnglish()
    {
        if (LanguageManager.Instance == null)
            return;

        LanguageManager.Instance.SetEnglish();
    }

    public void SetArabic()
    {
        if (LanguageManager.Instance == null)
            return;

        LanguageManager.Instance.SetArabic();
    }

    public void ToggleLanguage()
    {
        if (LanguageManager.Instance == null)
            return;

        AppLanguage nextLanguage = LanguageManager.Instance.CurrentLanguage == AppLanguage.English
            ? AppLanguage.Arabic
            : AppLanguage.English;

        LanguageManager.Instance.SetLanguage(nextLanguage);
    }
}
