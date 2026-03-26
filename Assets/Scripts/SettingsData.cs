using UnityEngine;

public static class SettingsData
{
    // Defaults (same as your current values)
    public static int levelCount = 3;
    public static float ingredientSpawnInterval = 1.5f;
    public static float obstacleSpawnInterval = 2.5f;
    public static int maxIngredients = 10;
    public static bool enableObstacles = true;

    // OPTIONAL: reset to defaults
    public static void ResetToDefaults()
    {
        ingredientSpawnInterval = 1.5f;
        obstacleSpawnInterval = 2.5f;
        maxIngredients = 10;
        enableObstacles = true;
    }
}