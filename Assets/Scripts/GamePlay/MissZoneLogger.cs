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

        if (other.CompareTag("Ingredient"))
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

            Destroy(other.gameObject);
            return;
        }

        if (other.CompareTag("Obstacle"))
        {
            Destroy(other.gameObject);
        }
    }
}