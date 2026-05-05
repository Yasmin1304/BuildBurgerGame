using System;
using UnityEngine;

public class LanguageManager : MonoBehaviour
{
    private const string PlayerPrefsKey = "app_language";

    public static LanguageManager Instance { get; private set; }

    [SerializeField] private AppLanguage defaultLanguage = AppLanguage.English;
    [SerializeField] private LocalizationDatabase localizationDatabase;

    public AppLanguage CurrentLanguage { get; private set; }
    public LocalizationDatabase Database => localizationDatabase;

    public event Action<AppLanguage> LanguageChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CurrentLanguage = LoadSavedLanguage();
    }

    public void SetLanguage(AppLanguage language)
    {
        if (CurrentLanguage == language)
            return;

        CurrentLanguage = language;
        PlayerPrefs.SetInt(PlayerPrefsKey, (int)language);
        PlayerPrefs.Save();

        LanguageChanged?.Invoke(CurrentLanguage);
    }

    public void SetEnglish()
    {
        SetLanguage(AppLanguage.English);
    }

    public void SetArabic()
    {
        SetLanguage(AppLanguage.Arabic);
    }

    public string GetText(string key, string fallback = "")
    {
        if (localizationDatabase == null)
            return string.IsNullOrEmpty(fallback) ? key : fallback;

        return localizationDatabase.GetText(key, CurrentLanguage, fallback);
    }

    AppLanguage LoadSavedLanguage()
    {
        if (!PlayerPrefs.HasKey(PlayerPrefsKey))
            return defaultLanguage;

        int savedValue = PlayerPrefs.GetInt(PlayerPrefsKey, (int)defaultLanguage);
        if (!Enum.IsDefined(typeof(AppLanguage), savedValue))
            return defaultLanguage;

        return (AppLanguage)savedValue;
    }
}
