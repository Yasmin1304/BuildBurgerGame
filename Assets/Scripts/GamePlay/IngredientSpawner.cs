using UnityEngine;

public class IngredientSpawner : MonoBehaviour
{
    public Camera cam;
    //public GameObject[] ingredientPrefabs;
    public float spawnInterval = 1.5f;
    public float spawnXRange = 3f;
    public float planeZ = 0f;

    [Header("Spawn Limit")]
    public int maxIngredients = 30;

    [Header("Mode Prefabs")]
    public GameObject[] ingredientPrefabs;
    public GameObject[] letterPrefabs;
    public GameObject[] numberPrefabs;

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

    void Start()
    {
        if (cam == null) cam = Camera.main;
        StartSpawning();
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

        enabled = true;

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
            Debug.Log("Max ingredients reached. Spawning stopped.");
            return;
        }

        GameManager gm = FindObjectOfType<GameManager>();
        if (gm == null) return;

        // Calculate spawn position above screen
        float zDistance = Mathf.Abs(cam.transform.position.z - planeZ);
        Vector3 topWorld = cam.ScreenToWorldPoint(new Vector3(0f, Screen.height, zDistance));

        float x = Random.Range(-spawnXRange, spawnXRange);
        float y = topWorld.y + 1.0f;

        GameObject prefabToSpawn = null;

        if (gm.currentMode == GameMode.Burger)
        {
            if (ingredientPrefabs == null || ingredientPrefabs.Length == 0)
                return;

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

        Debug.Log($"Mode = {gm.currentMode}, spawning = {prefabToSpawn.name}");
        Instantiate(prefabToSpawn, new Vector3(x, y, planeZ), Quaternion.identity);
        //Instantiate(prefabToSpawn, new Vector3(x, y, planeZ), Quaternion.identity);

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
                if (letterPrefabs != null && letterPrefabs.Length > 0)
                    return letterPrefabs[Random.Range(0, letterPrefabs.Length)];
                break;

            case GameMode.Numbers:
                if (numberPrefabs != null && numberPrefabs.Length > 0)
                    return numberPrefabs[Random.Range(0, numberPrefabs.Length)];
                break;
        }

        return null;
    }
}