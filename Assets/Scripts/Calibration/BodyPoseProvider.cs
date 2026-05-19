using UnityEngine;

/// <summary>
/// A single MediaPipe body landmark in normalized camera space.
/// X and Y are values from 0 to 1, not Unity world positions.
/// X = 0 is the left side of the camera image, X = 1 is the right side.
/// Y = 0 is the top of the camera image, Y = 1 is the bottom.
/// </summary>
public struct BodyLandmark
{
    public float X;
    public float Y;

    // MediaPipe confidence values. Visibility means the point is not hidden.
    // Presence means MediaPipe believes the landmark exists in the image.
    public float Visibility;
    public float Presence;

    public BodyLandmark(float x, float y, float visibility = 1f, float presence = 1f)
    {
        X = x;
        Y = y;
        Visibility = visibility;
        Presence = presence;
    }
}

/// <summary>
/// The subset of MediaPipe pose landmarks that the game cares about.
/// MediaPipe detects 33 pose landmarks, but calibration does not need all of them.
/// </summary>
public struct BodyPoseLandmarks
{
    // Main body anchors used to estimate whether the full body is visible.
    public BodyLandmark Nose;
    public BodyLandmark LeftShoulder;
    public BodyLandmark RightShoulder;

    // Used by gameplay as a fallback when detailed hand landmarks disappear.
    public BodyLandmark LeftWrist;
    public BodyLandmark RightWrist;

    // Lower-body anchors used to make sure the child is far enough from the screen.
    public BodyLandmark LeftHip;
    public BodyLandmark RightHip;
    public BodyLandmark LeftAnkle;
    public BodyLandmark RightAnkle;
}

/// <summary>
/// Small abstraction between the game and the actual pose-tracking implementation.
/// The calibration manager only knows about BodyPoseProvider, not MediaPipe classes.
/// This keeps the calibration code easier to test and change later.
/// </summary>
public abstract class BodyPoseProvider : MonoBehaviour
{
    /// <summary>
    /// Returns true only when the required body anchors are confident enough for calibration.
    /// </summary>
    public abstract bool TryGetPose(out BodyPoseLandmarks pose);

    /// <summary>
    /// Returns the latest pose even if some anchors are weak.
    /// Runtime hints and wrist fallback use this because partial information is still useful.
    /// </summary>
    public virtual bool TryGetLatestPose(out BodyPoseLandmarks pose)
    {
        return TryGetPose(out pose);
    }
}
