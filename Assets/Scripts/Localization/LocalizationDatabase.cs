using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LocalizationDatabase", menuName = "Localization/Database")]
public class LocalizationDatabase : ScriptableObject
{
    [SerializeField] private List<LocalizationEntry> entries = new List<LocalizationEntry>();

    private Dictionary<string, LocalizationEntry> entryLookup;

    public string GetText(string key, AppLanguage language, string fallback = "")
    {
        if (string.IsNullOrWhiteSpace(key))
            return fallback;

        EnsureLookup();

        if (!entryLookup.TryGetValue(key, out LocalizationEntry entry))
            return string.IsNullOrEmpty(fallback) ? key : fallback;

        string value = entry.GetValue(language);
        if (!string.IsNullOrEmpty(value))
            return value;

        return string.IsNullOrEmpty(fallback) ? key : fallback;
    }

    void OnEnable()
    {
        RebuildLookup();
    }

    void OnValidate()
    {
        RebuildLookup();
    }

    void EnsureLookup()
    {
        if (entryLookup == null)
            RebuildLookup();
    }

    void RebuildLookup()
    {
        entryLookup = new Dictionary<string, LocalizationEntry>(StringComparer.OrdinalIgnoreCase);

        foreach (LocalizationEntry entry in entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.Key))
                continue;

            entryLookup[entry.Key] = entry;
        }
    }
}

[Serializable]
public class LocalizationEntry
{
    [SerializeField] private string key;
    [TextArea] [SerializeField] private string english;
    [TextArea] [SerializeField] private string arabic;

    public string Key => key;

    public string GetValue(AppLanguage language)
    {
        switch (language)
        {
            case AppLanguage.Arabic:
                return arabic;

            case AppLanguage.English:
            default:
                return english;
        }
    }
}
