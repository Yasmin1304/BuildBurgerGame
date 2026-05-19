using Mediapipe.Unity;
using UnityEngine;

/// <summary>
/// Decides whether gameplay currently has usable hand input.
///
/// The game can use two possible signals:
/// 1. Real MediaPipe hand landmark results from HandLandmarkResultProvider.
/// 2. Pose wrists from BodyPoseProvider as a fallback when detailed hand landmarks drop.
///
/// If neither signal is available for a short grace period, this script asks
/// GameManager to pause spawning. When hand input returns, spawning resumes.
/// </summary>
public class HandTrackingAvailabilityMonitor : MonoBehaviour
{
    // Optional annotation root used only for debug/fallback counting.
    // Real hand availability should normally come from HandLandmarkResultProvider.
    [SerializeField] private Transform handAnnotationRoot;
    [SerializeField] private int minimumVisibleLandmarks = 10;

    // Grace periods avoid rapid pause/resume flicker from one noisy frame.
    [SerializeField] private float lostGraceTime = 0.75f;
    [SerializeField] private float regainedGraceTime = 0.25f;

    [SerializeField] private bool pauseSpawningWhenHandsLost = true;

    // Preferred hand signal: actual latest hand count from MediaPipe HandLandmarker.
    [SerializeField] private HandLandmarkResultProvider handResultProvider;

    // Fallback signal: pose wrists from the full-body pose model.
    // This is less precise than hands, so we use high confidence thresholds.
    [SerializeField] private bool usePoseWristFallback = true;
    [SerializeField] private BodyPoseProvider bodyPoseProvider;
    [SerializeField] private float minWristVisibility = 0.8f;
    [SerializeField] private float minWristPresence = 0.8f;
    [SerializeField] private bool logDebugInfo;

    private GameManager gameManager;
    private bool handsAvailable;
    private bool handLossNotified;
    private float lostTimer;
    private float regainedTimer;
    private float lastDebugLogTime;

    public bool HandsAvailable => handsAvailable;

    private void Awake()
    {
        // Inspector references are preferred, but auto-find makes setup easier.
        gameManager = FindObjectOfType<GameManager>();
        if (handResultProvider == null)
            handResultProvider = FindObjectOfType<HandLandmarkResultProvider>();
        if (bodyPoseProvider == null)
            bodyPoseProvider = FindObjectOfType<BodyPoseProvider>();
    }

    private void Update()
    {
        // visibleLandmarks is kept for diagnostics. It can be stale, so it does
        // not decide availability when HandLandmarkResultProvider is assigned.
        int visibleLandmarks = CountVisibleHandLandmarks();
        bool hasPoseWrists = HasVisiblePoseWrist();
        bool hasRealHandResult = HasRealHandResult();

        // Gameplay can continue if either real hand landmarks or confident pose wrists exist.
        bool hasEnoughLandmarks = hasRealHandResult || hasPoseWrists;

        if (hasEnoughLandmarks)
        {
            lostTimer = 0f;
            regainedTimer += Time.deltaTime;

            if (!handsAvailable && regainedTimer >= regainedGraceTime)
                SetHandsAvailable(true);
        }
        else
        {
            regainedTimer = 0f;
            lostTimer += Time.deltaTime;

            if (!handLossNotified && lostTimer >= lostGraceTime)
                SetHandsAvailable(false);
        }

        LogDebug(visibleLandmarks, hasRealHandResult, hasPoseWrists);
    }

    private bool HasRealHandResult()
    {
        if (handResultProvider == null)
            handResultProvider = FindObjectOfType<HandLandmarkResultProvider>();

        if (handResultProvider != null)
            return handResultProvider.HasFreshHands;

        // Fallback for scenes that have not been wired to HandLandmarkResultProvider yet.
        // Less reliable because annotation points can remain active briefly.
        return CountVisibleHandLandmarks() >= minimumVisibleLandmarks;
    }

    /// <summary>
    /// Checks whether the body pose model currently sees at least one wrist confidently.
    /// This keeps the game playable at full-body distance where detailed hand
    /// landmarks may be unstable.
    /// </summary>
    private bool HasVisiblePoseWrist()
    {
        if (!usePoseWristFallback)
            return false;

        if (bodyPoseProvider == null)
            bodyPoseProvider = FindObjectOfType<BodyPoseProvider>();

        if (bodyPoseProvider == null || !bodyPoseProvider.TryGetLatestPose(out BodyPoseLandmarks pose))
            return false;

        return IsWristReady(pose.LeftWrist) || IsWristReady(pose.RightWrist);
    }

    /// <summary>
    /// Pose wrists are accepted only when both MediaPipe confidence values pass.
    /// </summary>
    private bool IsWristReady(BodyLandmark wrist)
    {
        return wrist.Visibility >= minWristVisibility && wrist.Presence >= minWristPresence;
    }

    /// <summary>
    /// Counts visible hand annotation points. This is mainly a debug/fallback tool.
    /// It should not be the main truth source because annotations can be stale.
    /// </summary>
    private int CountVisibleHandLandmarks()
    {
        PointAnnotation[] points = handAnnotationRoot != null
            ? handAnnotationRoot.GetComponentsInChildren<PointAnnotation>(true)
            : FindObjectsOfType<PointAnnotation>(true);
        int count = 0;

        foreach (PointAnnotation point in points)
        {
            if (point != null && point.gameObject.activeInHierarchy)
                count++;
        }

        return count;
    }

    /// <summary>
    /// Applies the hand availability decision to gameplay.
    /// Pausing stops new falling items; resuming continues the same level.
    /// </summary>
    private void SetHandsAvailable(bool available)
    {
        handsAvailable = available;
        handLossNotified = !available;

        if (!pauseSpawningWhenHandsLost)
            return;

        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();

        if (gameManager == null)
            return;

        if (available)
            gameManager.ResumeAfterHandTrackingRecovered();
        else
            gameManager.PauseForHandTrackingLost();
    }

    /// <summary>
    /// Throttled debug log for tuning thresholds during play testing.
    /// </summary>
    private void LogDebug(int visibleLandmarks, bool hasRealHandResult, bool hasPoseWrists)
    {
        if (!logDebugInfo || Time.unscaledTime - lastDebugLogTime < 1f)
            return;

        lastDebugLogTime = Time.unscaledTime;
        Debug.Log($"HandTrackingAvailabilityMonitor: visibleLandmarks={visibleLandmarks}, hasRealHandResult={hasRealHandResult}, hasPoseWrists={hasPoseWrists}, handsAvailable={handsAvailable}, lostTimer={lostTimer:F2}, regainedTimer={regainedTimer:F2}");
    }
}
