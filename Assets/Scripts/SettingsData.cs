using UnityEngine;

public static class SettingsData
{
    public static int levelCount = 3;

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
}