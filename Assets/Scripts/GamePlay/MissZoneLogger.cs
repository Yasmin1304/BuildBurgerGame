using UnityEngine;

public class MissZoneLogger : MonoBehaviour
{
    [SerializeField] private SupabaseSessionEventInsert eventLogger;

    private string GetScreenSide(Transform target)
    {
        if (Camera.main == null || target == null)
            return "unknown";

        Vector3 screenPos = Camera.main.WorldToScreenPoint(target.position);
        return screenPos.x < Screen.width * 0.5f ? "left" : "right";
    }

    private string CleanName(string objName)
    {
        int i = objName.IndexOf("(Clone)");
        return i >= 0 ? objName.Substring(0, i) : objName;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("MissZone hit by: " + other.name + " | tag = " + other.tag);

        if (other.CompareTag("Ingredient") || other.CompareTag("FreeFall"))
        {
            if (other.transform.parent != null)
                return;

            string ingredientName = CleanName(other.name);
            string side = GetScreenSide(other.transform);

            if (eventLogger != null)
            {
                Debug.Log("Logging MISS: " + ingredientName + " | " + side);
                eventLogger.LogEvent(ingredientName, "miss", side);
                LevelSessionTracker.Instance?.RegisterMiss(side);
            }
            else
            {
                Debug.LogError("MissZone eventLogger is NULL");
            }

            LevelItemResolutionTracker.TryResolve(other.gameObject);
            TryCompleteLevelIfResolved();
            Destroy(other.gameObject);
            return;
        }

        if (other.CompareTag("Obstacle"))
        {
            Destroy(other.gameObject);
        }
    }

    void TryCompleteLevelIfResolved()
    {
        IngredientSpawner spawner = FindObjectOfType<IngredientSpawner>();
        if (!LevelItemResolutionTracker.TryRequestCompletion(spawner))
        {
            if (spawner != null && spawner.IsFinished)
                Debug.Log($"MissZone completion not ready: {LevelItemResolutionTracker.GetDebugStatus(spawner)}");
            return;
        }

        foreach (var s in FindObjectsOfType<IngredientSpawner>())
        {
            s.StopSpawning();
            s.enabled = false;
        }

        foreach (var o in FindObjectsOfType<ObstacleSpawner>())
        {
            o.StopSpawning();
            o.enabled = false;
        }

        foreach (var catcher in FindObjectsOfType<HandCatch3D>())
        {
            var col = catcher.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        foreach (var receiver in FindObjectsOfType<FreeDropReceiver>())
        {
            var col = receiver.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        FindObjectOfType<GameManager>()?.RequestNextLevel();
        FindObjectOfType<SupabaseSessionUpdate>()?.UpdateCurrentSession();
    }
}
