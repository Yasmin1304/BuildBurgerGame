using System;
using UnityEngine;

public static class SettingsData
{
    public static int levelCount = 3;
    public static bool enableButtonSounds = true;
    public static bool enableBackgroundMusic = true;
    public static event Action AudioSettingsChanged;

    // You can increase this later if needed
    public static int maxSupportedLevels = 10;

    public static LevelSettings[] levelSettings = new LevelSettings[maxSupportedLevels];

    static SettingsData()
    {
        InitializeDefaults();
    }

    public static void InitializeDefaults()
    {
        for (int i = 0; i < levelSettings.Length; i++)
        {
            levelSettings[i] = new LevelSettings
            {
                enableObstacles = (i > 0),              // example: level 1 off, later levels on
                ingredientSpawnInterval = 1.5f,
                obstacleSpawnInterval = 2.5f,
                ingredientFallSpeed = 2.5f,
                obstacleFallSpeed = 2.5f,
                spawnScreenEdgePadding = 0f,
                maxIngredients = 10
            };
        }
    }

    public static void ResetToDefaults()
    {
        levelCount = 3;
        InitializeDefaults();
    }

    public static LevelSettings GetLevelSettings(int index)
    {
        if (index < 0 || index >= levelSettings.Length)
            return null;

        return levelSettings[index];
    }

    public static void SetButtonSoundsEnabled(bool value)
    {
        if (enableButtonSounds == value)
            return;

        enableButtonSounds = value;
        AudioSettingsChanged?.Invoke();
    }

    public static void SetBackgroundMusicEnabled(bool value)
    {
        if (enableBackgroundMusic == value)
            return;

        enableBackgroundMusic = value;
        AudioSettingsChanged?.Invoke();
    }

    public static void SetAllAudioEnabled(bool value)
    {
        bool changed =
            enableButtonSounds != value ||
            enableBackgroundMusic != value;

        enableButtonSounds = value;
        enableBackgroundMusic = value;

        if (changed)
            AudioSettingsChanged?.Invoke();
    }
}
