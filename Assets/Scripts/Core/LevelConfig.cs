using UnityEngine;

[System.Serializable]
public class LevelConfig
{
    public string levelName = "Level 1";

    [Header("Ingredient Spawning")]
    public float ingredientSpawnInterval = 1.5f;
    public float ingredientFallSpeed = 2.5f;
    public int maxIngredients = 30;

    [Header("Spawn Range")]
    public float spawnScreenEdgePadding = 0f;

    [Header("Obstacle Spawning")]
    public bool enableObstacles = false;
    public float obstacleSpawnInterval = 3.5f;
    public float obstacleFallSpeed = 2.5f;
}
