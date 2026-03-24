using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class SupabaseSessionEventInsert : MonoBehaviour
{
    [System.Serializable]
    public class SessionEventRow
    {
        public string session_id;
        public string timestamp;
        public string element_type;
        public string outcome;
        public string side;
    }

    public void LogEvent(string elementType, string outcome, string side)
    {
        if (string.IsNullOrEmpty(SessionData.SessionId))
        {
            Debug.LogWarning("SessionId is empty. Cannot log event.");
            return;
        }

        StartCoroutine(InsertEvent(elementType, outcome, side));
    }

    private IEnumerator InsertEvent(string elementType, string outcome, string side)
    {
        string url = $"{SupabaseConfig.ProjectUrl}/rest/v1/session_events";

        SessionEventRow row = new SessionEventRow
        {
            session_id = SessionData.SessionId,
            timestamp = System.DateTime.UtcNow.ToString("o"),
            element_type = elementType,
            outcome = outcome,
            side = side
        };

        string json = JsonUtility.ToJson(row);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("apikey", SupabaseConfig.ApiKey);
            request.SetRequestHeader("Authorization", "Bearer " + SupabaseConfig.ApiKey);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
                Debug.Log($"Event inserted: {elementType}, {outcome}, {side}");
            else
                Debug.LogError("Event insert failed: " + request.responseCode + " - " + request.downloadHandler.text);
        }
    }
}