using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class SupabaseSessionUpdate : MonoBehaviour
{
    [System.Serializable]
    public class SessionUpdateRow
    {
        public int completion_time_seconds;
        public int total_hits;
        public int total_misses;
        public int left_hits;
        public int right_hits;
        public int left_misses;
        public int right_misses;
    }

    public void UpdateCurrentSession()
    {
        if (string.IsNullOrEmpty(SessionData.SessionId))
        {
            Debug.LogError("SessionId is empty. Cannot update session.");
            return;
        }

        StartCoroutine(UpdateSessionCoroutine());
    }

    private IEnumerator UpdateSessionCoroutine()
    {
        string url = $"{SupabaseConfig.ProjectUrl}/rest/v1/sessions?session_id=eq.{SessionData.SessionId}";

        var tracker = LevelSessionTracker.Instance;
        if (tracker == null)
        {
            Debug.LogError("LevelSessionTracker not found.");
            yield break;
        }

        SessionUpdateRow row = new SessionUpdateRow
        {
            completion_time_seconds = tracker.GetCompletionTimeSeconds(),
            total_hits = tracker.totalHits,
            total_misses = tracker.totalMisses,
            left_hits = tracker.leftHits,
            right_hits = tracker.rightHits,
            left_misses = tracker.leftMisses,
            right_misses = tracker.rightMisses
        };

        string json = JsonUtility.ToJson(row);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(url, "PATCH"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("apikey", SupabaseConfig.ApiKey);
            request.SetRequestHeader("Authorization", "Bearer " + SupabaseConfig.ApiKey);
            request.SetRequestHeader("Prefer", "return=representation");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
                Debug.Log("Session updated: " + request.downloadHandler.text);
            else
                Debug.LogError("Session update failed: " + request.responseCode + " - " + request.downloadHandler.text);
        }
    }
}