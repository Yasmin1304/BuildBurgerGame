using UnityEngine;

public class IngredientSpawner : MonoBehaviour
{
    public Camera cam;
    //public GameObject[] ingredientPrefabs;
    public float spawnInterval = 1.5f;
    public float spawnScreenEdgePadding = 0f;
    public float minSpawnXSpacing = 1f;
    public int spawnPositionAttempts = 12;
    public float planeZ = 0f;
    public bool logSpawnDebug;

    [Header("Spawn Limit")]
    public int maxIngredients = 30;

    [Header("Mode Prefabs")]
    public GameObject[] ingredientPrefabs;
    public GameObject[] letterPrefabs;
    public GameObject[] englishLetterPrefabs;
    public GameObject[] arabicLetterPrefabs;
    public GameObject[] englishNumberPrefabs;
    public GameObject[] arabicNumberPrefabs;

    [Header("Bottom Bun Guarantee")]
    [SerializeField] private bool enableBottomBunGuarantee = true;
    public GameObject bottomBunPrefab;
    public int guaranteeWithinFirst = 5;

    [Header("Top Bun Guarantee")]
    [SerializeField] private bool forceTopBunAsLastSpawn = true;
    public GameObject topBunPrefab;

    // --- NEW: progress tracking ---
    public int SpawnedCount { get; private set; }
    public bool IsFinished => SpawnedCount >= maxIngredients;

    private bool bottomBunSpawned = false;
    private bool hasStartedSpawning;

    void Start()
    {
        if (cam == null) cam = Camera.main;
    }

    public void StopSpawning()
    {
        CancelInvoke(nameof(Spawn));
    }

    public void StartSpawning()
    {
        CancelInvoke(nameof(Spawn));

        // Reset level state
        SpawnedCount = 0;
        bottomBunSpawned = false;
        LevelItemResolutionTracker.Reset();

        enabled = true;

        if (cam == null) cam = Camera.main;

        if (logSpawnDebug)
            Debug.Log($"IngredientSpawner.StartSpawning called on {name}. active={gameObject.activeInHierarchy}, enabled={enabled}, interval={spawnInterval}, max={maxIngredients}, cam={(cam != null ? cam.name : "null")}");

        InvokeRepeating(nameof(Spawn), 1f, spawnInterval);
        hasStartedSpawning = true;
    }

    public void PauseSpawning()
    {
        CancelInvoke(nameof(Spawn));
    }

    public void ResumeSpawning()
    {
        if (!hasStartedSpawning || !gameObject.activeInHierarchy)
            return;

        CancelInvoke(nameof(Spawn));
        if (cam == null) cam = Camera.main;
        InvokeRepeating(nameof(Spawn), 1f, spawnInterval);
    }

    // void Spawn()
    // {
    //     // Stop when limit reached
    //     if (SpawnedCount >= maxIngredients)
    //     {
    //         StopSpawning();
    //         enabled = false;
    //         Debug.Log("Max ingredients reached. Spawning stopped.");
    //         return;
    //     }

    //     if (ingredientPrefabs == null || ingredientPrefabs.Length == 0)
    //         return;

    //     // Calculate spawn position above screen
    //     float zDistance = Mathf.Abs(cam.transform.position.z - planeZ);
    //     Vector3 topWorld = cam.ScreenToWorldPoint(new Vector3(0f, Screen.height, zDistance));

    //     float x = Random.Range(-spawnXRange, spawnXRange);
    //     float y = topWorld.y + 1.0f;

    //     GameObject prefabToSpawn;

    //     // 1️⃣ Force TOP bun as final spawn
    //     if (forceTopBunAsLastSpawn && topBunPrefab != null && SpawnedCount == maxIngredients - 1)
    //     {
    //         prefabToSpawn = topBunPrefab;
    //         Debug.Log($"Forced TOP bun as last spawn #{SpawnedCount + 1}");
    //     }
    //     // 2️⃣ Guarantee bottom bun early
    //     else if (enableBottomBunGuarantee && !bottomBunSpawned && bottomBunPrefab != null && SpawnedCount == guaranteeWithinFirst - 1)
    //     {
    //         prefabToSpawn = bottomBunPrefab;
    //         bottomBunSpawned = true;
    //         Debug.Log($"Forced Bottom Bun at spawn #{SpawnedCount + 1}");
    //     }
    //     // 3️⃣ Normal random spawn
    //     else
    //     {
    //         prefabToSpawn = ingredientPrefabs[Random.Range(0, ingredientPrefabs.Length)];

    //         if (bottomBunPrefab != null && prefabToSpawn == bottomBunPrefab)
    //             bottomBunSpawned = true;
    //     }

    //     Instantiate(prefabToSpawn, new Vector3(x, y, planeZ), Quaternion.identity);

    //     SpawnedCount++;
    // }

    void Spawn()
    {
        // Stop when limit reached
        if (SpawnedCount >= maxIngredients)
        {
            StopSpawning();
            enabled = false;
            Debug.Log($"Max ingredients reached. Spawning stopped. {LevelItemResolutionTracker.GetDebugStatus(this)}");
            return;
        }

        GameManager gm = FindObjectOfType<GameManager>();
        if (gm == null)
        {
            Debug.LogWarning("IngredientSpawner.Spawn stopped: no active GameManager found.");
            return;
        }

        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("IngredientSpawner.Spawn stopped: no camera assigned and no Camera.main found.");
                return;
            }
        }

        // Calculate spawn position above screen
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
            Debug.LogWarning("IngredientSpawner.Spawn stopped: could not calculate visible spawn range.");
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
            if (logSpawnDebug)
                Debug.Log("IngredientSpawner skipped spawn because no non-overlapping X position was available.");
            return;
        }

        float y = topWorld.y + 1.0f;

        GameObject prefabToSpawn = null;

        if (gm.currentMode == GameMode.Burger)
        {
            if (ingredientPrefabs == null || ingredientPrefabs.Length == 0)
            {
                Debug.LogWarning("IngredientSpawner.Spawn stopped: burger mode has no ingredientPrefabs assigned.");
                return;
            }

            // 1. Force TOP bun as final spawn
            if (forceTopBunAsLastSpawn && topBunPrefab != null && SpawnedCount == maxIngredients - 1)
            {
                prefabToSpawn = topBunPrefab;
                Debug.Log($"Forced TOP bun as last spawn #{SpawnedCount + 1}");
            }
            // 2. Guarantee bottom bun early
            else if (enableBottomBunGuarantee && !bottomBunSpawned && bottomBunPrefab != null && SpawnedCount == guaranteeWithinFirst - 1)
            {
                prefabToSpawn = bottomBunPrefab;
                bottomBunSpawned = true;
                Debug.Log($"Forced Bottom Bun at spawn #{SpawnedCount + 1}");
            }
            // 3. Normal burger spawn
            else
            {
                prefabToSpawn = ingredientPrefabs[Random.Range(0, ingredientPrefabs.Length)];

                if (bottomBunPrefab != null && prefabToSpawn == bottomBunPrefab)
                    bottomBunSpawned = true;
            }
        }
        else
        {
            // Letters / Numbers
            prefabToSpawn = GetRandomPrefabForCurrentMode();

            if (prefabToSpawn == null)
            {
                Debug.LogWarning("No prefab found for current game mode.");
                return;
            }
        }

        GameObject spawned = Instantiate(prefabToSpawn, new Vector3(x, y, planeZ), Quaternion.identity);
        if (logSpawnDebug)
            Debug.Log($"IngredientSpawner spawned {spawned.name} at {spawned.transform.position}.");

        if (gm.currentMode != GameMode.Burger)
            LockSpawnedRotation(spawned);
        LevelItemResolutionTracker.RegisterSpawn(spawned);

        SpawnedCount++;
    }

    GameObject GetRandomPrefabForCurrentMode()
    {
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm == null) return null;

        switch (gm.currentMode)
        {
            case GameMode.Burger:
                if (ingredientPrefabs != null && ingredientPrefabs.Length > 0)
                    return ingredientPrefabs[Random.Range(0, ingredientPrefabs.Length)];
                break;

            case GameMode.Letters:
                GameObject[] activeLetterPrefabs = GetActiveLetterPrefabs();
                if (activeLetterPrefabs != null && activeLetterPrefabs.Length > 0)
                    return activeLetterPrefabs[Random.Range(0, activeLetterPrefabs.Length)];
                break;

            case GameMode.Numbers:
                GameObject[] activeNumberPrefabs = GetActiveNumberPrefabs();
                if (activeNumberPrefabs != null && activeNumberPrefabs.Length > 0)
                    return activeNumberPrefabs[Random.Range(0, activeNumberPrefabs.Length)];
                break;
        }

        return null;
    }

    GameObject[] GetActiveLetterPrefabs()
    {
        if (LanguageManager.Instance != null)
        {
            if (LanguageManager.Instance.CurrentLanguage == AppLanguage.Arabic &&
                arabicLetterPrefabs != null && arabicLetterPrefabs.Length > 0)
            {
                return arabicLetterPrefabs;
            }

            if (LanguageManager.Instance.CurrentLanguage == AppLanguage.English &&
                englishLetterPrefabs != null && englishLetterPrefabs.Length > 0)
            {
                return englishLetterPrefabs;
            }
        }

        return letterPrefabs;
    }

    GameObject[] GetActiveNumberPrefabs()
    {
        if (LanguageManager.Instance != null)
        {
            if (LanguageManager.Instance.CurrentLanguage == AppLanguage.Arabic &&
                arabicNumberPrefabs != null && arabicNumberPrefabs.Length > 0)
            {
                return arabicNumberPrefabs;
            }

            if (LanguageManager.Instance.CurrentLanguage == AppLanguage.English &&
                englishNumberPrefabs != null && englishNumberPrefabs.Length > 0)
            {
                return englishNumberPrefabs;
            }
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
}
