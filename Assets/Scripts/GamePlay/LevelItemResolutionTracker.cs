using System.Collections.Generic;
using UnityEngine;

public static class LevelItemResolutionTracker
{
    private static readonly HashSet<int> spawnedInstanceIds = new HashSet<int>();
    private static readonly HashSet<int> resolvedInstanceIds = new HashSet<int>();
    private static readonly Dictionary<int, int> instanceIdToSpawnedRootId = new Dictionary<int, int>();
    private static bool completionRequested;

    public static int SpawnedCount => spawnedInstanceIds.Count;
    public static int ResolvedCount => resolvedInstanceIds.Count;
    public static int UnresolvedCount => Mathf.Max(0, spawnedInstanceIds.Count - resolvedInstanceIds.Count);
    public static bool CompletionRequested => completionRequested;

    public static void Reset()
    {
        spawnedInstanceIds.Clear();
        resolvedInstanceIds.Clear();
        instanceIdToSpawnedRootId.Clear();
        completionRequested = false;
    }

    public static void RegisterSpawn(GameObject go)
    {
        if (go == null) return;

        int rootId = go.GetInstanceID();
        spawnedInstanceIds.Add(rootId);
        instanceIdToSpawnedRootId[rootId] = rootId;

        foreach (Transform child in go.GetComponentsInChildren<Transform>(true))
        {
            if (child != null)
                instanceIdToSpawnedRootId[child.gameObject.GetInstanceID()] = rootId;
        }
    }

    public static bool TryResolve(GameObject go)
    {
        if (go == null) return false;

        int objectId = go.GetInstanceID();
        if (!instanceIdToSpawnedRootId.TryGetValue(objectId, out int id))
        {
            Debug.LogWarning($"LevelItemResolutionTracker ignored untracked resolved item: {go.name}");
            return false;
        }

        if (resolvedInstanceIds.Contains(id)) return false;

        resolvedInstanceIds.Add(id);
        return true;
    }

    public static bool TryRequestCompletion(IngredientSpawner spawner)
    {
        if (completionRequested) return false;
        if (spawner == null || !spawner.IsFinished) return false;
        if (spawnedInstanceIds.Count < spawner.SpawnedCount) return false;
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
