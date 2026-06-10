using UnityEngine;

/// <summary>
/// Pauses spawning when YOLO pose has not detected a fresh wrist for a short period,
/// and resumes when hand tracking returns.
/// </summary>
public class HandTrackingAvailabilityMonitor : MonoBehaviour
{
    [SerializeField] private YoloBodyPoseProvider trackingProvider;
    [SerializeField] private float lostGraceTime = 0.75f;
    [SerializeField] private float regainedGraceTime = 0.25f;
    [SerializeField] private bool pauseSpawningWhenHandsLost = true;
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
        gameManager = FindObjectOfType<GameManager>();
        if (trackingProvider == null)
            trackingProvider = FindObjectOfType<YoloBodyPoseProvider>();
    }

    private void Update()
    {
        if (trackingProvider == null)
            trackingProvider = FindObjectOfType<YoloBodyPoseProvider>();

        bool hasFreshHands = trackingProvider != null && trackingProvider.HasFreshWrists;

        if (hasFreshHands)
        {
            lostTimer = 0f;
            regainedTimer += Time.unscaledDeltaTime;

            if (!handsAvailable && regainedTimer >= regainedGraceTime)
                SetHandsAvailable(true);
        }
        else
        {
            regainedTimer = 0f;
            lostTimer += Time.unscaledDeltaTime;

            bool gameStillNeedsPause =
                gameManager != null &&
                gameManager.GameStarted &&
                !gameManager.PausedForHandTracking;

            if ((!handLossNotified || gameStillNeedsPause) &&
                lostTimer >= lostGraceTime)
            {
                SetHandsAvailable(false);
            }
        }

        LogDebug(hasFreshHands);
    }

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

    private void LogDebug(bool hasFreshHands)
    {
        if (!logDebugInfo || Time.unscaledTime - lastDebugLogTime < 1f)
            return;

        lastDebugLogTime = Time.unscaledTime;
        Debug.Log(
            $"HandTrackingAvailabilityMonitor: hasFreshHands={hasFreshHands}, " +
            $"handsAvailable={handsAvailable}, lostTimer={lostTimer:F2}, " +
            $"regainedTimer={regainedTimer:F2}"
        );
    }
}
