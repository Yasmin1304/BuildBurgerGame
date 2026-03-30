using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    public Camera cam;
    public GameObject[] obstaclePrefabs;
    public float spawnInterval = 3f;
    public float spawnXRange = 3f;
    public float planeZ = 0f;

    void Start()
    {
        if (cam == null) cam = Camera.main;
        StartSpawning();
        //InvokeRepeating(nameof(Spawn), 2f, spawnInterval);
    }

    public void StartSpawning()
    {
        CancelInvoke(nameof(Spawn));
        InvokeRepeating(nameof(Spawn), 2f, spawnInterval);
    }

    void Spawn()
    {
        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0) return;

        var prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];

        float zDistance = Mathf.Abs(cam.transform.position.z - planeZ);
        Vector3 topWorld = cam.ScreenToWorldPoint(
            new Vector3(0f, Screen.height, zDistance)
        );

        float x = Random.Range(-spawnXRange, spawnXRange);
        float y = topWorld.y + 1.2f;

        Instantiate(prefab, new Vector3(x, y, planeZ), Quaternion.identity);
    }

    public void StopSpawning()
    {
        CancelInvoke(nameof(Spawn));
    }
}
