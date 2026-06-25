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

        if (!SpawnPositionUtility.TryGetRandomXAvoidingFallingItems(
            minX,
            maxX,
            minSpawnXSpacing,
            spawnPositionAttempts,
            out float x
        ))
        {
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
}
