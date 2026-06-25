using System;

[Serializable]
public class LevelSettings
{
    public bool enableObstacles = true;

    // These are actual spawn intervals used by the game
    public float ingredientSpawnInterval = 1.5f;
    public float obstacleSpawnInterval = 2.5f;

    // These are actual downward fall speeds in Unity units per second.
    public float ingredientFallSpeed = 2.5f;
    public float obstacleFallSpeed = 2.5f;

    // Higher padding keeps spawns closer to the center. Lower padding uses more screen width.
    public float spawnScreenEdgePadding = 0f;

    public int maxIngredients = 10;
}
