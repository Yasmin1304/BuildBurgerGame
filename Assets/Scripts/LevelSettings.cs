using System;

[Serializable]
public class LevelSettings
{
    public bool enableObstacles = true;

    // These are actual spawn intervals used by the game
    public float ingredientSpawnInterval = 1.5f;
    public float obstacleSpawnInterval = 2.5f;

    public int maxIngredients = 10;
}