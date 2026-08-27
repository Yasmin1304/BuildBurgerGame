using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    public Camera cam;
    public GameObject[] burgerObstaclePrefabs;
    public GameObject[] letterAndNumberObstaclePrefabs;
    public float spawnInterval = 3f;
    public float spawnScreenEdgePadding = 0f;
    public float minSpawnXSpacing = 1f;
    public int spawnPositionAttempts = 12;
    public float fallSpeed = 2.5f;
    public float planeZ = 0f;

    [Header("Fair Obstacle Placement")]
    [SerializeField] private bool avoidScreenCenter = true;
    [SerializeField, Range(0f, 0.8f)] private float centerNoSpawnWidthRatio = 0.3f;
    [SerializeField] private float minCatchableItemXSpacing = 2.5f;

    private bool hasStartedSpawning;

    void Start()
    {
        if (cam == null) cam = Camera.main;
    }

    public void StartSpawning()
    {
        CancelInvoke(nameof(Spawn));
        InvokeRepeating(nameof(Spawn), 2f, spawnInterval);
        hasStartedSpawning = true;
    }

    public void PauseSpawning()
    {
        CancelInvoke(nameof(Spawn));
    }

    public void ResumeSpawning()
    {
        if (!hasStartedSpawning || !gameObject.activeInHierarchy || !enabled)
            return;

        CancelInvoke(nameof(Spawn));
        InvokeRepeating(nameof(Spawn), 2f, spawnInterval);
    }

    void Spawn()
    {
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm == null || gm.GameplaySpawningPaused)
            return;

        GameObject[] activePrefabs = GetActiveObstaclePrefabs();
        if (activePrefabs == null || activePrefabs.Length == 0) return;

        var prefab = activePrefabs[Random.Range(0, activePrefabs.Length)];

        if (!SpawnPositionUtility.TryGetVisibleXRange(
            cam,
            planeZ,
            Screen.height,
            spawnScreenEdgePadding,
            out float minX,
            out float maxX,
            out Vector3 topWorld
        ))
        {
            Debug.LogWarning("ObstacleSpawner.Spawn stopped: could not calculate visible spawn range.");
            return;
        }

        if (!TryGetFairObstacleX(
            minX,
            maxX,
            out float x
        ))
        {
            Debug.Log("ObstacleSpawner skipped spawn because no fair obstacle lane was available.");
            return;
        }

        float y = topWorld.y + 1.2f;

        GameObject spawned = Instantiate(prefab, new Vector3(x, y, planeZ), Quaternion.identity);
        ApplyFallSpeed(spawned);
        LockSpawnedRotation(spawned);
    }

    public void StopSpawning()
    {
        CancelInvoke(nameof(Spawn));
    }

    GameObject[] GetActiveObstaclePrefabs()
    {
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm == null)
            return null;

        switch (gm.currentMode)
        {
            case GameMode.Burger:
                if (burgerObstaclePrefabs != null && burgerObstaclePrefabs.Length > 0)
                    return burgerObstaclePrefabs;
                break;

            case GameMode.Letters:
            case GameMode.Numbers:
                if (letterAndNumberObstaclePrefabs != null && letterAndNumberObstaclePrefabs.Length > 0)
                    return letterAndNumberObstaclePrefabs;
                break;
        }

        return null;
    }

    void LockSpawnedRotation(GameObject spawned)
    {
        if (spawned == null)
            return;

        Rigidbody rb = spawned.GetComponent<Rigidbody>();
        if (rb == null)
            return;

        rb.angularVelocity = Vector3.zero;
        rb.constraints |= RigidbodyConstraints.FreezeRotationX |
                          RigidbodyConstraints.FreezeRotationY |
                          RigidbodyConstraints.FreezeRotationZ;
    }

    void ApplyFallSpeed(GameObject spawned)
    {
        if (spawned == null)
            return;

        Rigidbody rb = spawned.GetComponent<Rigidbody>();
        if (rb == null)
            return;

        ControlledFallVelocity controlledFall =
            spawned.GetComponent<ControlledFallVelocity>();
        if (controlledFall == null)
            controlledFall = spawned.AddComponent<ControlledFallVelocity>();

        controlledFall.Configure(fallSpeed);
    }

    bool TryGetFairObstacleX(float minX, float maxX, out float x)
    {
        x = 0f;

        int attempts = Mathf.Max(1, spawnPositionAttempts);
        for (int i = 0; i < attempts; i++)
        {
            float candidate = Random.Range(minX, maxX);
            if (IsFairObstacleX(candidate, minX, maxX))
            {
                x = candidate;
                return true;
            }
        }

        return TryGetBestFairObstacleLane(minX, maxX, out x);
    }

    bool TryGetBestFairObstacleLane(float minX, float maxX, out float x)
    {
        x = 0f;

        const int sampleCount = 24;
        float bestX = 0f;
        float bestClearance = -1f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = sampleCount == 1 ? 0.5f : i / (sampleCount - 1f);
            float candidate = Mathf.Lerp(minX, maxX, t);
            if (!IsFairObstacleX(candidate, minX, maxX))
                continue;

            float clearance = GetNearestFallingItemDistance(candidate);
            if (clearance > bestClearance)
            {
                bestX = candidate;
                bestClearance = clearance;
            }
        }

        if (bestClearance < 0f)
            return false;

        x = bestX;
        return true;
    }

    bool IsFairObstacleX(float candidate, float minX, float maxX)
    {
        if (avoidScreenCenter && IsInsideCenterNoSpawnZone(candidate, minX, maxX))
            return false;

        if (IsTooCloseToFallingTag(candidate, "Ingredient", minCatchableItemXSpacing))
            return false;

        if (IsTooCloseToFallingTag(candidate, "FreeFall", minCatchableItemXSpacing))
            return false;

        if (IsTooCloseToFallingTag(candidate, "Obstacle", minSpawnXSpacing))
            return false;

        return true;
    }

    bool IsInsideCenterNoSpawnZone(float candidate, float minX, float maxX)
    {
        float width = maxX - minX;
        if (width <= 0f)
            return false;

        float center = (minX + maxX) * 0.5f;
        float halfBlockedWidth = width * centerNoSpawnWidthRatio * 0.5f;
        return Mathf.Abs(candidate - center) < halfBlockedWidth;
    }

    bool IsTooCloseToFallingTag(float candidate, string tag, float minSpacing)
    {
        minSpacing = Mathf.Max(0f, minSpacing);

        GameObject[] objects;
        try
        {
            objects = GameObject.FindGameObjectsWithTag(tag);
        }
        catch (UnityException)
        {
            return false;
        }

        foreach (GameObject obj in objects)
        {
            if (!IsActiveFallingObject(obj))
                continue;

            if (Mathf.Abs(candidate - obj.transform.position.x) < minSpacing)
                return true;
        }

        return false;
    }

    float GetNearestFallingItemDistance(float candidate)
    {
        float nearestDistance = float.PositiveInfinity;
        nearestDistance = Mathf.Min(nearestDistance, GetNearestDistanceToTag(candidate, "Ingredient"));
        nearestDistance = Mathf.Min(nearestDistance, GetNearestDistanceToTag(candidate, "FreeFall"));
        nearestDistance = Mathf.Min(nearestDistance, GetNearestDistanceToTag(candidate, "Obstacle"));
        return float.IsPositiveInfinity(nearestDistance) ? float.MaxValue : nearestDistance;
    }

    float GetNearestDistanceToTag(float candidate, string tag)
    {
        GameObject[] objects;
        try
        {
            objects = GameObject.FindGameObjectsWithTag(tag);
        }
        catch (UnityException)
        {
            return float.PositiveInfinity;
        }

        float nearestDistance = float.PositiveInfinity;
        foreach (GameObject obj in objects)
        {
            if (!IsActiveFallingObject(obj))
                continue;

            nearestDistance = Mathf.Min(nearestDistance, Mathf.Abs(candidate - obj.transform.position.x));
        }

        return nearestDistance;
    }

    bool IsActiveFallingObject(GameObject obj)
    {
        if (obj == null || !obj.activeInHierarchy)
            return false;

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        return rb == null || !rb.isKinematic;
    }
}
