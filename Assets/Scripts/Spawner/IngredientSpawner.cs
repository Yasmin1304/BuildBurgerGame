using UnityEngine;

public class IngredientSpawner : MonoBehaviour
{
    public Camera cam;
    public GameObject[] ingredientPrefabs;
    public float spawnInterval = 1.5f;
    public float spawnXRange = 3f;
    public float planeZ = 0f;

    [Header("Spawn Limit")]
    public int maxIngredients = 30;

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

        if (ingredientPrefabs == null || ingredientPrefabs.Length == 0)
            return;

        // Calculate spawn position above screen
        float zDistance = Mathf.Abs(cam.transform.position.z - planeZ);
        Vector3 topWorld = cam.ScreenToWorldPoint(new Vector3(0f, Screen.height, zDistance));

        float x = Random.Range(-spawnXRange, spawnXRange);
        float y = topWorld.y + 1.0f;

        GameObject prefabToSpawn;

        // 1️⃣ Force TOP bun as final spawn
        if (forceTopBunAsLastSpawn && topBunPrefab != null && SpawnedCount == maxIngredients - 1)
        {
            prefabToSpawn = topBunPrefab;
            Debug.Log($"Forced TOP bun as last spawn #{SpawnedCount + 1}");
        }
        // 2️⃣ Guarantee bottom bun early
        else if (enableBottomBunGuarantee && !bottomBunSpawned && bottomBunPrefab != null && SpawnedCount == guaranteeWithinFirst - 1)
        {
            prefabToSpawn = bottomBunPrefab;
            bottomBunSpawned = true;
            Debug.Log($"Forced Bottom Bun at spawn #{SpawnedCount + 1}");
        }
        // 3️⃣ Normal random spawn
        else
        {
            prefabToSpawn = ingredientPrefabs[Random.Range(0, ingredientPrefabs.Length)];

            if (bottomBunPrefab != null && prefabToSpawn == bottomBunPrefab)
                bottomBunSpawned = true;
        }

        Instantiate(prefabToSpawn, new Vector3(x, y, planeZ), Quaternion.identity);

        SpawnedCount++;
    }
}