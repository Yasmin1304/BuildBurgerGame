using System.Collections.Generic;
using UnityEngine;

public static class LevelItemResolutionTracker
{
    private static readonly HashSet<int> spawnedInstanceIds = new HashSet<int>();
    private static readonly HashSet<int> resolvedInstanceIds = new HashSet<int>();
    private static bool completionRequested;

    public static int SpawnedCount => spawnedInstanceIds.Count;
    public static int ResolvedCount => resolvedInstanceIds.Count;
    public static int UnresolvedCount => Mathf.Max(0, spawnedInstanceIds.Count - resolvedInstanceIds.Count);
    public static bool CompletionRequested => completionRequested;

    public static void Reset()
    {
        spawnedInstanceIds.Clear();
        resolvedInstanceIds.Clear();
        completionRequested = false;
    }

    public static void RegisterSpawn(GameObject go)
    {
        if (go == null) return;
        spawnedInstanceIds.Add(go.GetInstanceID());
    }

    public static bool TryResolve(GameObject go)
    {
        if (go == null) return false;

        int id = go.GetInstanceID();
        if (resolvedInstanceIds.Contains(id)) return false;

        resolvedInstanceIds.Add(id);
        return true;
    }

    public static bool TryRequestCompletion(IngredientSpawner spawner)
    {
        if (completionRequested) return false;
        if (spawner == null || !spawner.IsFinished) return false;
        if (resolvedInstanceIds.Count < spawner.SpawnedCount) return false;

        completionRequested = true;
        return true;
    }

    public static string GetDebugStatus(IngredientSpawner spawner)
    {
        string spawnerStatus = spawner == null
            ? "spawner=null"
            : $"spawnerFinished={spawner.IsFinished}, spawnerSpawned={spawner.SpawnedCount}/{spawner.maxIngredients}";

        return $"{spawnerStatus}, trackedSpawned={spawnedInstanceIds.Count}, resolved={resolvedInstanceIds.Count}, unresolved={UnresolvedCount}, completionRequested={completionRequested}";
    }
}
