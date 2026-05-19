using Mediapipe.Tasks.Vision.HandLandmarker;
using UnityEngine;

/// <summary>
/// Stores the latest real hand count reported by MediaPipe HandLandmarker.
///
/// This exists because the visible hand landmark annotation objects can stay
/// active briefly even after MediaPipe stops detecting a real hand. Gameplay
/// should trust the actual MediaPipe result, not only the annotation spheres.
/// </summary>
public class HandLandmarkResultProvider : MonoBehaviour
{
    // If no fresh result arrives within this time, treat the hand result as stale.
    // This prevents old hand data from keeping the basket visible forever.
    [SerializeField] private float staleAfterSeconds = 0.35f;
    [SerializeField] private bool logDebugInfo;

    // Latest number of hands reported by MediaPipe.
    private int currentHandCount;

    // Uses unscaled time so UI pauses or gameplay time scale changes do not affect tracking freshness.
    private float lastResultTime = -999f;
    private float lastDebugLogTime;

    /// <summary>
    /// Returns the latest hand count only if it is fresh.
    /// If MediaPipe has not reported recently, this returns 0.
    /// </summary>
    public int CurrentHandCount
    {
        get
        {
            if (Time.unscaledTime - lastResultTime > staleAfterSeconds)
                return 0;

            return currentHandCount;
        }
    }

    /// <summary>
    /// True when MediaPipe recently detected at least one real hand.
    /// </summary>
    public bool HasFreshHands => CurrentHandCount > 0;

    /// <summary>
    /// Called by HandLandmarkerRunner after each MediaPipe result.
    /// The runner sends only the count so this script stays simple.
    /// </summary>
    public void SetHandCount(int handCount)
    {
        currentHandCount = Mathf.Max(0, handCount);
        lastResultTime = Time.unscaledTime;

        if (logDebugInfo && Time.unscaledTime - lastDebugLogTime >= 1f)
        {
            lastDebugLogTime = Time.unscaledTime;
            Debug.Log($"HandLandmarkResultProvider: currentHandCount={currentHandCount}");
        }
    }

    /// <summary>
    /// Alternate entry point if another script wants to pass the full MediaPipe result.
    /// </summary>
    public void SetHandResult(HandLandmarkerResult result)
    {
        SetHandCount(result.handLandmarks?.Count ?? 0);
    }

    /// <summary>
    /// Manually clears the hand state.
    /// </summary>
    public void ClearHands()
    {
        SetHandCount(0);
    }
}
