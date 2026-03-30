using UnityEngine;

public class LevelSessionTracker : MonoBehaviour
{
    public static LevelSessionTracker Instance;

    public int totalHits;
    public int totalMisses;
    public int leftHits;
    public int rightHits;
    public int leftMisses;
    public int rightMisses;

    private float levelStartTime;

    private void Awake()
    {
        Instance = this;
    }

    public void StartLevelTracking()
    {
        totalHits = 0;
        totalMisses = 0;
        leftHits = 0;
        rightHits = 0;
        leftMisses = 0;
        rightMisses = 0;

        levelStartTime = Time.time;
    }

    public void RegisterHit(string side)
    {
        totalHits++;

        if (side == "left") leftHits++;
        else if (side == "right") rightHits++;
    }

    public void RegisterMiss(string side)
    {
        totalMisses++;

        if (side == "left") leftMisses++;
        else if (side == "right") rightMisses++;
    }

    public int GetCompletionTimeSeconds()
    {
        return Mathf.RoundToInt(Time.time - levelStartTime);
    }
}