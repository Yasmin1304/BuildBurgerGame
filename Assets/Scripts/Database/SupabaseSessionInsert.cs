using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class SupabaseSessionInsert : MonoBehaviour
{
    [System.Serializable]
    public class SessionRow
    {
        public string participant_id;
        public string device_id;
        public string game_version;
    }

    [System.Serializable]
    public class SessionResponse
    {
        public string session_id;
        public string participant_id;
    }

    public static class JsonHelper
    {
        public static T[] FromJson<T>(string json)
        {
            string wrapped = "{\"items\":" + json + "}";
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(wrapped);
            return wrapper.items;
        }

        [System.Serializable]
        private class Wrapper<T>
        {
            public T[] items;
        }
    }

    // void Start()
    // {
    //     Debug.Log("SupabaseSessionInsert Start called");
    //     Debug.Log("ParticipantId before session insert: " + SessionData.ParticipantId);
        
    //     if (!string.IsNullOrEmpty(SessionData.ParticipantId))
    //     {
    //         StartCoroutine(InsertSession());
    //     }
    //     else
    //     {
    //         Debug.LogError("ParticipantId is empty. Cannot create session.");
    //     }
    // }

    public void CreateSessionForCurrentLevel()
    {
        Debug.Log("CreateSessionForCurrentLevel called");
        Debug.Log("ParticipantId before session insert: " + SessionData.ParticipantId);

        if (!string.IsNullOrEmpty(SessionData.ParticipantId))
        {
            SessionData.SessionId = "";
            LevelSessionTracker.Instance?.StartLevelTracking();
            StartCoroutine(InsertSession());
        }
        else
        {
            Debug.LogError("ParticipantId is empty. Cannot create session.");
        }
    }

    private IEnumerator InsertSession()
    {
        string url = $"{SupabaseConfig.ProjectUrl}/rest/v1/sessions?select=session_id,participant_id";

        SessionRow row = new SessionRow
        {
            participant_id = SessionData.ParticipantId,
            device_id = SystemInfo.deviceUniqueIdentifier,
            game_version = Application.version
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
            request.SetRequestHeader("Prefer", "return=representation");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Session inserted: " + request.downloadHandler.text);

                var results = JsonHelper.FromJson<SessionResponse>(request.downloadHandler.text);

                if (results != null && results.Length > 0)
                {
                    SessionData.SessionId = results[0].session_id;
                    LevelSessionTracker.Instance?.StartLevelTracking();
                    Debug.Log("Saved SessionId: " + SessionData.SessionId);
                }
            }
            else
            {
                Debug.LogError("Session insert failed: " + request.responseCode + " - " + request.downloadHandler.text);
            }
        }
    }
}