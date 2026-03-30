using UnityEngine;

[System.Serializable]
public class LevelConfig
{
    public string levelName = "Level 1";

    [Header("Ingredient Spawning")]
    public float ingredientSpawnInterval = 1.5f;
    public int maxIngredients = 30;

    [Header("Obstacle Spawning")]
    public bool enableObstacles = false;
    public float obstacleSpawnInterval = 3.5f;

    [Header("Bottom Bun")]
    //public bool guaranteeBottomBun = true;
    public int bottomBunWithinFirst = 5;
}
