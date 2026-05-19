using System.Collections.Generic;
using UnityEngine;
using Mediapipe;
using Mediapipe.Tasks.Vision.PoseLandmarker;

using TasksLandmark = Mediapipe.Tasks.Components.Containers.NormalizedLandmark;
using TasksLandmarks = Mediapipe.Tasks.Components.Containers.NormalizedLandmarks;

/// <summary>
/// Converts MediaPipe pose results into the small BodyPoseLandmarks structure
/// used by our calibration and gameplay scripts.
///
/// MediaPipe's PoseLandmarker detects 33 body landmarks. This class picks out
/// only the anchors we need: nose, shoulders, wrists, hips, and ankles.
/// </summary>
public class MediaPipeBodyPoseProvider : BodyPoseProvider
{
    // MediaPipe pose landmark indices:
    // https://developers.google.com/mediapipe/solutions/vision/pose_landmarker
    // 0 = nose, 11/12 = shoulders, 15/16 = wrists, 23/24 = hips, 27/28 = ankles.
    private const int Nose = 0;
    private const int LeftShoulder = 11;
    private const int RightShoulder = 12;
    private const int LeftWrist = 15;
    private const int RightWrist = 16;
    private const int LeftHip = 23;
    private const int RightHip = 24;
    private const int LeftAnkle = 27;
    private const int RightAnkle = 28;
    private const int RequiredPoseLandmarkCount = 33;

    [Header("Confidence")]
    [SerializeField] private float minVisibility = 0.5f;
    [SerializeField] private float minPresence = 0.5f;
    [SerializeField] private bool logDebugInfo;

    // Stores the newest pose result received from PoseLandmarkerRunner.
    private BodyPoseLandmarks currentPose;
    private bool hasCurrentPose;
    private float lastDebugLogTime;

    /// <summary>
    /// Strict pose request used by calibration.
    /// It returns true only when the body anchors needed for full-body calibration
    /// meet the configured visibility/presence thresholds.
    /// </summary>
    public override bool TryGetPose(out BodyPoseLandmarks pose)
    {
        pose = currentPose;
        bool isReady = hasCurrentPose && IsPoseConfident(currentPose);

        if (logDebugInfo)
            LogDebugInfo(isReady);

        return isReady;
    }

    /// <summary>
    /// Loose pose request used for runtime hints and wrist fallback.
    /// It returns the latest pose even if the full body is not currently perfect.
    /// </summary>
    public override bool TryGetLatestPose(out BodyPoseLandmarks pose)
    {
        pose = currentPose;
        return hasCurrentPose;
    }

    /// <summary>
    /// Called when MediaPipe reports no pose. This prevents old pose data from
    /// being reused forever after the child leaves the camera view.
    /// </summary>
    public void ClearPose()
    {
        currentPose = default;
        hasCurrentPose = false;
    }

    /// <summary>
    /// Entry point for MediaPipe Tasks PoseLandmarker results.
    /// PoseLandmarkerRunner sends this result to us whenever MediaPipe has output.
    /// </summary>
    public void SetPose(PoseLandmarkerResult result)
    {
        SetPose(result, 0);
    }

    /// <summary>
    /// MediaPipe can detect multiple people. We use poseIndex 0 because this game
    /// expects one child in front of the camera.
    /// </summary>
    public void SetPose(PoseLandmarkerResult result, int poseIndex)
    {
        if (result.poseLandmarks == null || poseIndex < 0 || poseIndex >= result.poseLandmarks.Count)
        {
            if (logDebugInfo)
                Debug.Log("MediaPipeBodyPoseProvider: PoseLandmarkerResult has no pose landmarks.");

            ClearPose();
            return;
        }

        SetPose(result.poseLandmarks[poseIndex]);
    }

    /// <summary>
    /// Adapter for the newer MediaPipe Tasks normalized landmark list type.
    /// </summary>
    public void SetPose(TasksLandmarks landmarks)
    {
        SetPose(landmarks.landmarks);
    }

    /// <summary>
    /// Reads the landmarks produced by the MediaPipe Tasks API and converts them
    /// into our BodyPoseLandmarks structure.
    /// </summary>
    public void SetPose(IReadOnlyList<TasksLandmark> landmarks)
    {
        if (landmarks == null || landmarks.Count < RequiredPoseLandmarkCount)
        {
            if (logDebugInfo)
                Debug.Log($"MediaPipeBodyPoseProvider: Tasks pose landmarks missing or incomplete. Count: {landmarks?.Count ?? 0}");

            ClearPose();
            return;
        }

        currentPose = new BodyPoseLandmarks
        {
            Nose = ToBodyLandmark(landmarks[Nose]),
            LeftShoulder = ToBodyLandmark(landmarks[LeftShoulder]),
            RightShoulder = ToBodyLandmark(landmarks[RightShoulder]),
            LeftWrist = ToBodyLandmark(landmarks[LeftWrist]),
            RightWrist = ToBodyLandmark(landmarks[RightWrist]),
            LeftHip = ToBodyLandmark(landmarks[LeftHip]),
            RightHip = ToBodyLandmark(landmarks[RightHip]),
            LeftAnkle = ToBodyLandmark(landmarks[LeftAnkle]),
            RightAnkle = ToBodyLandmark(landmarks[RightAnkle])
        };

        // We received a real pose result. It may still be weak for calibration,
        // but TryGetLatestPose can use it for helpful runtime hints.
        hasCurrentPose = true;
    }

    /// <summary>
    /// Adapter for the older MediaPipe normalized landmark list type.
    /// Kept so this provider works with either Homuler API shape.
    /// </summary>
    public void SetPose(NormalizedLandmarkList landmarks)
    {
        SetPose(landmarks?.Landmark);
    }

    /// <summary>
    /// Reads the landmarks produced by the legacy MediaPipe API and converts them
    /// into our BodyPoseLandmarks structure.
    /// </summary>
    public void SetPose(IReadOnlyList<NormalizedLandmark> landmarks)
    {
        if (landmarks == null || landmarks.Count < RequiredPoseLandmarkCount)
        {
            if (logDebugInfo)
                Debug.Log($"MediaPipeBodyPoseProvider: Legacy pose landmarks missing or incomplete. Count: {landmarks?.Count ?? 0}");

            ClearPose();
            return;
        }

        currentPose = new BodyPoseLandmarks
        {
            Nose = ToBodyLandmark(landmarks[Nose]),
            LeftShoulder = ToBodyLandmark(landmarks[LeftShoulder]),
            RightShoulder = ToBodyLandmark(landmarks[RightShoulder]),
            LeftWrist = ToBodyLandmark(landmarks[LeftWrist]),
            RightWrist = ToBodyLandmark(landmarks[RightWrist]),
            LeftHip = ToBodyLandmark(landmarks[LeftHip]),
            RightHip = ToBodyLandmark(landmarks[RightHip]),
            LeftAnkle = ToBodyLandmark(landmarks[LeftAnkle]),
            RightAnkle = ToBodyLandmark(landmarks[RightAnkle])
        };

        // Same idea as the Tasks overload: store the latest pose even if it is
        // not strong enough for full calibration.
        hasCurrentPose = true;
    }

    /// <summary>
    /// Converts a MediaPipe Tasks landmark to our neutral BodyLandmark type.
    /// Visibility/presence can be missing, so we treat missing values as confident.
    /// </summary>
    private BodyLandmark ToBodyLandmark(TasksLandmark landmark)
    {
        return new BodyLandmark(
            landmark.x,
            landmark.y,
            landmark.visibility ?? 1f,
            landmark.presence ?? 1f
        );
    }

    /// <summary>
    /// Converts a legacy MediaPipe landmark to our neutral BodyLandmark type.
    /// </summary>
    private BodyLandmark ToBodyLandmark(NormalizedLandmark landmark)
    {
        return new BodyLandmark(
            landmark.X,
            landmark.Y,
            landmark.HasVisibility ? landmark.Visibility : 1f,
            landmark.HasPresence ? landmark.Presence : 1f
        );
    }

    /// <summary>
    /// Full-body calibration requires these anchors to be visible:
    /// head, shoulders, hips, and ankles. Wrists are intentionally not required
    /// here because hand tracking is a separate gameplay problem.
    /// </summary>
    private bool IsPoseConfident(BodyPoseLandmarks pose)
    {
        return IsConfident(pose.Nose)
            && IsConfident(pose.LeftShoulder)
            && IsConfident(pose.RightShoulder)
            && IsConfident(pose.LeftHip)
            && IsConfident(pose.RightHip)
            && IsConfident(pose.LeftAnkle)
            && IsConfident(pose.RightAnkle);
    }

    /// <summary>
    /// Checks MediaPipe's confidence for one landmark.
    /// Both visibility and presence must pass.
    /// </summary>
    private bool IsConfident(BodyLandmark landmark)
    {
        return landmark.Visibility >= minVisibility && landmark.Presence >= minPresence;
    }

    /// <summary>
    /// Debug helper for tuning thresholds in Unity.
    /// The log is throttled to once per second so Play Mode remains readable.
    /// </summary>
    private void LogDebugInfo(bool isReady)
    {
        if (Time.unscaledTime - lastDebugLogTime < 1f)
            return;

        lastDebugLogTime = Time.unscaledTime;

        if (!hasCurrentPose)
        {
            Debug.Log("MediaPipeBodyPoseProvider: No current pose has been received yet.");
            return;
        }

        Debug.Log(
            "MediaPipeBodyPoseProvider: Pose received. " +
            $"Ready={isReady}, " +
            $"Nose v/p={currentPose.Nose.Visibility:F2}/{currentPose.Nose.Presence:F2}, " +
            $"LShoulder v/p={currentPose.LeftShoulder.Visibility:F2}/{currentPose.LeftShoulder.Presence:F2}, " +
            $"RShoulder v/p={currentPose.RightShoulder.Visibility:F2}/{currentPose.RightShoulder.Presence:F2}, " +
            $"LWrist v/p={currentPose.LeftWrist.Visibility:F2}/{currentPose.LeftWrist.Presence:F2}, " +
            $"RWrist v/p={currentPose.RightWrist.Visibility:F2}/{currentPose.RightWrist.Presence:F2}, " +
            $"LHip v/p={currentPose.LeftHip.Visibility:F2}/{currentPose.LeftHip.Presence:F2}, " +
            $"RHip v/p={currentPose.RightHip.Visibility:F2}/{currentPose.RightHip.Presence:F2}, " +
            $"LAnkle v/p={currentPose.LeftAnkle.Visibility:F2}/{currentPose.LeftAnkle.Presence:F2}, " +
            $"RAnkle v/p={currentPose.RightAnkle.Visibility:F2}/{currentPose.RightAnkle.Presence:F2}"
        );
    }
}
