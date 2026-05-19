using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    public Camera cam;
    public GameObject[] burgerObstaclePrefabs;
    public GameObject[] letterAndNumberObstaclePrefabs;
    public float spawnInterval = 3f;
    public float spawnXRange = 3f;
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
        GameObject[] activePrefabs = GetActiveObstaclePrefabs();
        if (activePrefabs == null || activePrefabs.Length == 0) return;

        var prefab = activePrefabs[Random.Range(0, activePrefabs.Length)];

        float zDistance = Mathf.Abs(cam.transform.position.z - planeZ);
        Vector3 topWorld = cam.ScreenToWorldPoint(
            new Vector3(0f, Screen.height, zDistance)
        );

        float x = Random.Range(-spawnXRange, spawnXRange);
        float y = topWorld.y + 1.2f;

        GameObject spawned = Instantiate(prefab, new Vector3(x, y, planeZ), Quaternion.identity);
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
}
