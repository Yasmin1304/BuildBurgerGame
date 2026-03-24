using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class SupabaseParticipantCodeInsert : MonoBehaviour
{
    [SerializeField] private TMP_InputField participantInput;
    [SerializeField] private GameObject participantPanel;
    [SerializeField] private GameObject instructionsPanel;

    [System.Serializable]
    public class ParticipantRow
    {
        public string participant_code;
    }

    [System.Serializable]
    public class ParticipantResponse
    {
        public string participant_id;
        public string participant_code;
    }

    [System.Serializable]
    public class ParticipantResponseArray
    {
        public ParticipantResponse[] items;
    }

    public static class JsonHelper
    {
        public static T[] FromJson<T>(string json)
        {
            string wrapped = "{\"items\":" + json + "}";
            Wrapper<T> wrapper = UnityEngine.JsonUtility.FromJson<Wrapper<T>>(wrapped);
            return wrapper.items;
        }

        [System.Serializable]
        private class Wrapper<T>
        {
            public T[] items;
        }
    }

    public void SubmitParticipant()
    {
        string participantCode = participantInput.text.Trim();

        if (string.IsNullOrEmpty(participantCode))
        {
            Debug.LogWarning("Participant number is empty");
            return;
        }

        TestInsertParticipant(participantCode);
    }

    public void TestInsertParticipant(string participantCode)
    {
        StartCoroutine(InsertParticipant(participantCode));
    }

    private IEnumerator InsertParticipant(string participantCode)
    {
        //string url = $"{SupabaseConfig.ProjectUrl}/rest/v1/participants";
        string url = $"{SupabaseConfig.ProjectUrl}/rest/v1/participants?select=participant_id,participant_code";
        ParticipantRow row = new ParticipantRow
        {
            participant_code = participantCode
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
                Debug.Log("Participant inserted: " + request.downloadHandler.text);

                var results = JsonHelper.FromJson<ParticipantResponse>(request.downloadHandler.text);
                Debug.Log(results);
                if (results != null && results.Length > 0)
                {
                    SessionData.ParticipantCode = results[0].participant_code;
                    SessionData.ParticipantId = results[0].participant_id;

                    Debug.Log("Saved ParticipantCode: " + SessionData.ParticipantCode);
                    Debug.Log("Saved ParticipantId: " + SessionData.ParticipantId);

                    // ONLY NOW move forward
                    participantPanel.SetActive(false);
                    instructionsPanel.SetActive(true);
                }
            }
            else
            {
                Debug.LogError("Insert failed: " + request.responseCode + " - " + request.downloadHandler.text);
            }
        }
    }
}