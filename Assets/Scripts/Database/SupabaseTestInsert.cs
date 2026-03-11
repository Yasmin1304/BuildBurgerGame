using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class SupabaseTestInsert : MonoBehaviour
{
    [System.Serializable]
    public class ParticipantRow
    {
        public string participant_code;
    }

    void Start()
    {
        TestInsertParticipant("TEST001");
    }

    public void TestInsertParticipant(string participantCode)
    {
        StartCoroutine(InsertParticipant(participantCode));
    }

    private IEnumerator InsertParticipant(string participantCode)
    {
        string url = $"{SupabaseConfig.ProjectUrl}/rest/v1/participants";

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
                Debug.Log("Participant inserted: " + request.downloadHandler.text);
            else
                Debug.LogError("Insert failed: " + request.responseCode + " - " + request.downloadHandler.text);
        }
    }
}