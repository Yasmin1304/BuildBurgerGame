using System;
using System.Collections.Generic;
using Unity.InferenceEngine;
using UnityEngine;

/// <summary>
/// Runs YOLO pose detection, tracks people with ByteTrack, and exposes the
/// selected player's body anchors to calibration and gameplay.
/// YOLO COCO pose keypoints are converted into normalized camera coordinates.
/// </summary>
public sealed class YoloBodyPoseProvider : BodyPoseProvider
{
    [Serializable]
    public struct WristDetection
    {
        public Vector2 Center;
        public float Confidence;
    }

    private const int ImageWidth = 640;
    private const int ImageHeight = 640;
    private const int KeypointValueCount = 17 * 3;
    private const BackendType Backend = BackendType.GPUCompute;

    // COCO pose keypoint indices.
    private const int Nose = 0;
    private const int LeftShoulder = 5;
    private const int RightShoulder = 6;
    private const int LeftElbow = 7;
    private const int RightElbow = 8;
    private const int LeftWrist = 9;
    private const int RightWrist = 10;
    private const int LeftHip = 11;
    private const int RightHip = 12;
    private const int LeftAnkle = 15;
    private const int RightAnkle = 16;

    [Header("Input")]
    [SerializeField] private WebCamInputProvider cameraInput;
    [SerializeField] private ModelAsset modelAsset;

    [Header("Detection")]
    [SerializeField, Range(0f, 1f)] private float iouThreshold = 0.5f;
    [SerializeField, Range(0f, 1f)] private float keypointConfidenceThreshold = 0.5f;
    [SerializeField, Min(0f)] private float inferenceIntervalSeconds;
    [SerializeField, Min(0f)] private float initialInferenceDelay = 0.025f;
    [SerializeField, Min(0.05f)] private float staleAfterSeconds = 0.35f;

    [Header("Gameplay Wrists")]
    [SerializeField, Range(0f, 1f)] private float wristConfidenceThreshold = 0.6f;
    [SerializeField, Min(0.05f)] private float wristStaleAfterSeconds = 0.2f;
    [SerializeField, Min(0f)] private float mergeShoulderWidthMultiplier = 0.45f;
    [SerializeField, Min(0f)] private float splitShoulderWidthMultiplier = 0.6f;
    [SerializeField, Range(0f, 1f)] private float minimumWristMergeDistance = 0.02f;
    [SerializeField, Range(0f, 1f)] private float maximumWristMergeDistance = 0.14f;
    [SerializeField] private bool logDebugInfo;

    [Header("Locked Player Wrist Filter")]
    [SerializeField] private bool requireConnectedElbowForWrists = true;
    [SerializeField, Range(0f, 1f)] private float elbowConfidenceThreshold = 0.45f;
    [SerializeField, Min(0f)] private float maxWristToElbowShoulderWidthMultiplier = 2.4f;

    [Header("ByteTrack")]
    [SerializeField, Range(0f, 1f)] private float trackHighThreshold = 0.5f;
    [SerializeField, Range(0f, 1f)] private float trackLowThreshold = 0.1f;
    [SerializeField, Range(0f, 1f)] private float newTrackThreshold = 0.5f;
    [SerializeField, Range(0f, 1f)] private float trackMatchThreshold = 0.8f;
    [SerializeField, Min(1)] private int trackBufferFrames = 30;
    [SerializeField] private bool reacquireLostPlayerLock = true;

    private readonly List<WristDetection> gameplayWrists = new(2);
    private readonly List<ByteTrackPersonTracker.Detection> personDetections = new();
    private readonly ByteTrackPersonTracker personTracker = new();
    private Worker worker;
    private Tensor<float> centersToCorners;
    private Tensor<float> pendingInput;
    private Tensor<float> pendingKeypoints;
    private RenderTexture inferenceTexture;
    private BodyPoseLandmarks currentPose;
    private Rect inferenceCrop;
    private float lastInferenceTime = -999f;
    private float lastResultTime = -999f;
    private float lastDebugLogTime;
    private float lastLeftWristTime = -999f;
    private float lastRightWristTime = -999f;
    private BodyLandmark lastLeftWrist;
    private BodyLandmark lastRightWrist;
    private bool initialized;
    private bool hasCurrentPose;
    private bool inferencePending;
    private bool wristsMerged;
    private bool usesEndToEndOutput;
    private bool playerLockRequested;
    private int lockedTrackId = -1;
    private int detectionScoreOffset;
    private int detectionClassOffset = -1;
    private int keypointValueOffset;

    private bool HasFreshPose =>
        initialized &&
        hasCurrentPose &&
        Time.unscaledTime - lastResultTime <= staleAfterSeconds;

    private void Awake()
    {
        if (cameraInput == null)
            cameraInput = FindObjectOfType<WebCamInputProvider>();

        lastInferenceTime =
            Time.unscaledTime - inferenceIntervalSeconds + initialInferenceDelay;
        InitializeModel();
    }

    private void Update()
    {
        if (!initialized || cameraInput == null)
            return;

        if (inferencePending)
        {
            TryCompleteInference();
            return;
        }

        if (!cameraInput.DidUpdateThisFrame)
            return;

        if (Time.unscaledTime - lastInferenceTime < inferenceIntervalSeconds)
            return;

        ScheduleInference();
    }

    public override bool TryGetPose(out BodyPoseLandmarks pose)
    {
        pose = currentPose;
        return HasFreshPose && HasRequiredCalibrationAnchors(currentPose);
    }

    public override bool TryGetLatestPose(out BodyPoseLandmarks pose)
    {
        pose = currentPose;
        return HasFreshPose;
    }

    public bool TryGetWrists(out IReadOnlyList<WristDetection> wrists)
    {
        RefreshGameplayWrists();
        wrists = gameplayWrists;
        return gameplayWrists.Count > 0;
    }

    public bool HasFreshWrists
    {
        get
        {
            RefreshGameplayWrists();
            return gameplayWrists.Count > 0;
        }
    }

    public bool PlayerLocked => lockedTrackId >= 0;

    /// <summary>
    /// Locks gameplay to the ByteTrack ID currently selected by calibration.
    /// If no track is ready yet, the next centered track becomes the player.
    /// </summary>
    public void RequestMainPlayerLock()
    {
        playerLockRequested = true;
        if (lockedTrackId < 0)
            lockedTrackId = FindBestUpdatedTrackId();
    }

    public void ResetMainPlayerLock()
    {
        playerLockRequested = false;
        lockedTrackId = -1;
        personTracker.Reset();
    }

    public Vector2 ToScreenPoint(Vector2 normalizedPoint)
    {
        return cameraInput != null
            ? cameraInput.PreviewNormalizedToScreenPoint(normalizedPoint)
            : new Vector2(
                normalizedPoint.x * Screen.width,
                (1f - normalizedPoint.y) * Screen.height
            );
    }

    public override Vector2 ToScreenPoint(BodyLandmark landmark)
    {
        return cameraInput != null
            ? cameraInput.PreviewNormalizedToScreenPoint(
                new Vector2(landmark.X, landmark.Y)
            )
            : base.ToScreenPoint(landmark);
    }

    private void InitializeModel()
    {
        if (modelAsset == null)
        {
            Debug.LogError("YoloBodyPoseProvider: Assign a YOLO pose ModelAsset.");
            return;
        }

        Model sourceModel = ModelLoader.Load(modelAsset);
        centersToCorners = new Tensor<float>(
            new TensorShape(4, 4),
            new[]
            {
                1f, 0f, 1f, 0f,
                0f, 1f, 0f, 1f,
                -0.5f, 0f, 0.5f, 0f,
                0f, -0.5f, 0f, 0.5f
            }
        );

        FunctionalGraph graph = new FunctionalGraph();
        FunctionalTensor[] inputs = graph.AddInputs(sourceModel);
        FunctionalTensor output = Functional.Forward(sourceModel, inputs)[0];
        usesEndToEndOutput = modelAsset.name.StartsWith(
            "yolo26",
            StringComparison.OrdinalIgnoreCase
        );

        FunctionalTensor selectedDetections;
        if (usesEndToEndOutput)
        {
            // YOLO26 pose exports [1, 300, 57]:
            // xyxy, score, class, then 17 keypoints with x/y/confidence.
            selectedDetections = output[0, .., ..];
            detectionScoreOffset = 4;
            detectionClassOffset = 5;
            keypointValueOffset = 6;
        }
        else
        {
            // YOLOv8/YOLO11 pose exports raw [1, 56, 8400] predictions.
            FunctionalTensor boxCoords = output[0, 0..4, ..].Transpose(0, 1);
            FunctionalTensor scores = output[0, 4, ..];
            FunctionalTensor keypoints = output[0, 5.., ..].Transpose(0, 1);
            FunctionalTensor boxCorners =
                Functional.MatMul(boxCoords, Functional.Constant(centersToCorners));
            FunctionalTensor indices =
                Functional.NMS(boxCorners, scores, iouThreshold, trackLowThreshold);
            FunctionalTensor selectedBoxes =
                Functional.IndexSelect(boxCorners, 0, indices);
            FunctionalTensor selectedScores =
                Functional.IndexSelect(scores, 0, indices).Unsqueeze(1);
            FunctionalTensor selectedKeypoints =
                Functional.IndexSelect(keypoints, 0, indices);
            selectedDetections = Functional.Concat(
                new[] { selectedBoxes, selectedScores, selectedKeypoints },
                1
            );
            detectionScoreOffset = 4;
            keypointValueOffset = 5;
        }

        worker = new Worker(
            graph.Compile(selectedDetections),
            Backend
        );
        inferenceTexture = new RenderTexture(ImageWidth, ImageHeight, 0);
        inferenceTexture.Create();
        initialized = true;
    }

    private void ScheduleInference()
    {
        lastInferenceTime = Time.unscaledTime;
        PrepareInferenceTexture();

        pendingInput = new Tensor<float>(
            new TensorShape(1, 3, ImageHeight, ImageWidth)
        );
        TextureConverter.ToTensor(inferenceTexture, pendingInput, default);
        worker.Schedule(pendingInput);

        pendingKeypoints = worker.PeekOutput(0) as Tensor<float>;

        if (pendingKeypoints == null)
        {
            Debug.LogError("YoloBodyPoseProvider: Model outputs were unavailable.");
            pendingInput.Dispose();
            pendingInput = null;
            inferencePending = false;
            return;
        }

        pendingKeypoints.ReadbackRequest();
        inferencePending = true;
    }

    private void TryCompleteInference()
    {
        if (pendingKeypoints == null)
        {
            pendingInput?.Dispose();
            pendingInput = null;
            inferencePending = false;
            return;
        }

        if (!pendingKeypoints.IsReadbackRequestDone())
            return;

        using Tensor<float> keypoints = pendingKeypoints.ReadbackAndClone();
        pendingInput?.Dispose();
        pendingInput = null;
        inferencePending = false;
        pendingKeypoints = null;

        if (keypoints == null || keypoints.shape[0] == 0)
        {
            AdvanceTrackerWithoutDetections();
            hasCurrentPose = false;
            LogPoseState(false, 0f);
            return;
        }

        if (keypoints.shape[1] < keypointValueOffset + KeypointValueCount)
        {
            hasCurrentPose = false;
            LogPoseState(false, 0f);
            return;
        }

        UpdatePersonTracks(keypoints);
        if (!TrySelectTrackedPlayer(
                out float personScore,
                out BodyPoseLandmarks detectedPose))
        {
            hasCurrentPose = false;
            LogPoseState(false, 0f);
            return;
        }

        currentPose = hasCurrentPose
            ? SmoothPose(currentPose, detectedPose)
            : detectedPose;
        hasCurrentPose = true;
        lastResultTime = Time.unscaledTime;

        UpdateWristCache(currentPose);
        LogPoseState(true, personScore);
    }

    private void UpdatePersonTracks(Tensor<float> detections)
    {
        personDetections.Clear();

        for (int personIndex = 0; personIndex < detections.shape[0]; personIndex++)
        {
            if (!TryGetPersonScore(detections, personIndex, out float score))
                continue;

            Rect box = ReadPersonBox(detections, personIndex);
            if (box.width <= 0f || box.height <= 0f)
                continue;

            personDetections.Add(new ByteTrackPersonTracker.Detection(
                box,
                score,
                ReadPose(detections, personIndex)
            ));
        }

        personTracker.Update(
            personDetections,
            trackHighThreshold,
            trackLowThreshold,
            newTrackThreshold,
            trackMatchThreshold,
            trackBufferFrames
        );
    }

    private void AdvanceTrackerWithoutDetections()
    {
        personDetections.Clear();
        personTracker.Update(
            personDetections,
            trackHighThreshold,
            trackLowThreshold,
            newTrackThreshold,
            trackMatchThreshold,
            trackBufferFrames
        );
    }

    private bool TrySelectTrackedPlayer(
        out float personScore,
        out BodyPoseLandmarks selectedPose)
    {
        personScore = 0f;
        selectedPose = default;

        if (lockedTrackId >= 0)
        {
            if (!personTracker.TryGetUpdatedTrack(
                    lockedTrackId,
                    out ByteTrackPersonTracker.Track lockedTrack))
            {
                if (!reacquireLostPlayerLock)
                    return false;

                lockedTrackId = -1;
            }
            else
            {
                personScore = lockedTrack.Score;
                selectedPose = lockedTrack.Pose;
                return true;
            }
        }

        int bestTrackId = FindBestUpdatedTrackId();
        if (bestTrackId < 0 ||
            !personTracker.TryGetUpdatedTrack(
                bestTrackId,
                out ByteTrackPersonTracker.Track selectedTrack))
        {
            return false;
        }

        if (playerLockRequested)
            lockedTrackId = bestTrackId;

        personScore = selectedTrack.Score;
        selectedPose = selectedTrack.Pose;
        return true;
    }

    private bool TryGetPersonScore(
        Tensor<float> detections,
        int personIndex,
        out float score)
    {
        score = detections[personIndex, detectionScoreOffset];
        if (score < trackLowThreshold)
            return false;

        return detectionClassOffset < 0 ||
            Mathf.Abs(detections[personIndex, detectionClassOffset]) < 0.5f;
    }

    private BodyPoseLandmarks ReadPose(
        Tensor<float> keypoints,
        int personIndex)
    {
        return new BodyPoseLandmarks
        {
            Nose = ReadKeypoint(keypoints, personIndex, Nose),
            LeftShoulder = ReadKeypoint(keypoints, personIndex, LeftShoulder),
            RightShoulder = ReadKeypoint(keypoints, personIndex, RightShoulder),
            LeftElbow = ReadKeypoint(keypoints, personIndex, LeftElbow),
            RightElbow = ReadKeypoint(keypoints, personIndex, RightElbow),
            LeftWrist = ReadKeypoint(keypoints, personIndex, LeftWrist),
            RightWrist = ReadKeypoint(keypoints, personIndex, RightWrist),
            LeftHip = ReadKeypoint(keypoints, personIndex, LeftHip),
            RightHip = ReadKeypoint(keypoints, personIndex, RightHip),
            LeftAnkle = ReadKeypoint(keypoints, personIndex, LeftAnkle),
            RightAnkle = ReadKeypoint(keypoints, personIndex, RightAnkle)
        };
    }

    private Rect ReadPersonBox(Tensor<float> detections, int personIndex)
    {
        Vector2 min = WebCamInputProvider.LetterboxInferenceToPreview(
            new Vector2(
                detections[personIndex, 0] / ImageWidth,
                detections[personIndex, 1] / ImageHeight
            ),
            inferenceCrop
        );
        Vector2 max = WebCamInputProvider.LetterboxInferenceToPreview(
            new Vector2(
                detections[personIndex, 2] / ImageWidth,
                detections[personIndex, 3] / ImageHeight
            ),
            inferenceCrop
        );
        return Rect.MinMaxRect(
            Mathf.Clamp01(Mathf.Min(min.x, max.x)),
            Mathf.Clamp01(Mathf.Min(min.y, max.y)),
            Mathf.Clamp01(Mathf.Max(min.x, max.x)),
            Mathf.Clamp01(Mathf.Max(min.y, max.y))
        );
    }

    private int FindBestUpdatedTrackId()
    {
        int bestTrackId = -1;
        float bestScore = float.PositiveInfinity;

        foreach (ByteTrackPersonTracker.Track track in personTracker.Tracks)
        {
            if (!track.UpdatedThisFrame)
                continue;

            float score =
                Vector2.Distance(track.Box.center, new Vector2(0.5f, 0.5f)) -
                track.Score * 0.1f;
            if (score >= bestScore)
                continue;

            bestScore = score;
            bestTrackId = track.Id;
        }

        return bestTrackId;
    }

    private void UpdateWristCache(BodyPoseLandmarks pose)
    {
        if (pose.LeftWrist.Presence >= wristConfidenceThreshold &&
            IsAcceptedPlayerWrist(pose.LeftWrist, pose.LeftElbow, pose))
        {
            lastLeftWrist = pose.LeftWrist;
            lastLeftWristTime = Time.unscaledTime;
        }
        else
        {
            lastLeftWristTime = -999f;
        }

        if (pose.RightWrist.Presence >= wristConfidenceThreshold &&
            IsAcceptedPlayerWrist(pose.RightWrist, pose.RightElbow, pose))
        {
            lastRightWrist = pose.RightWrist;
            lastRightWristTime = Time.unscaledTime;
        }
        else
        {
            lastRightWristTime = -999f;
        }
    }

    private bool IsAcceptedPlayerWrist(
        BodyLandmark wrist,
        BodyLandmark elbow,
        BodyPoseLandmarks pose)
    {
        if (!requireConnectedElbowForWrists)
            return true;

        if (elbow.Presence < elbowConfidenceThreshold)
            return false;

        float shoulderWidth = Mathf.Abs(pose.LeftShoulder.X - pose.RightShoulder.X);
        if (shoulderWidth <= 0.001f)
            return true;

        float maxDistance = shoulderWidth * maxWristToElbowShoulderWidthMultiplier;
        float wristToElbowDistance = Vector2.Distance(
            new Vector2(wrist.X, wrist.Y),
            new Vector2(elbow.X, elbow.Y)
        );

        return wristToElbowDistance <= maxDistance;
    }

    private void RefreshGameplayWrists()
    {
        gameplayWrists.Clear();

        if (Time.unscaledTime - lastLeftWristTime <= wristStaleAfterSeconds)
        {
            gameplayWrists.Add(new WristDetection
            {
                Center = new Vector2(lastLeftWrist.X, lastLeftWrist.Y),
                Confidence = lastLeftWrist.Presence
            });
        }

        if (Time.unscaledTime - lastRightWristTime <= wristStaleAfterSeconds)
        {
            gameplayWrists.Add(new WristDetection
            {
                Center = new Vector2(lastRightWrist.X, lastRightWrist.Y),
                Confidence = lastRightWrist.Presence
            });
        }

        gameplayWrists.Sort((left, right) => left.Center.x.CompareTo(right.Center.x));
        MergeCloseWrists();
    }

    private void MergeCloseWrists()
    {
        if (gameplayWrists.Count != 2)
        {
            wristsMerged = false;
            return;
        }

        WristDetection left = gameplayWrists[0];
        WristDetection right = gameplayWrists[1];
        float shoulderWidth = Mathf.Abs(
            currentPose.LeftShoulder.X - currentPose.RightShoulder.X
        );
        float multiplier = wristsMerged
            ? Mathf.Max(mergeShoulderWidthMultiplier, splitShoulderWidthMultiplier)
            : mergeShoulderWidthMultiplier;
        float threshold = Mathf.Clamp(
            shoulderWidth * multiplier,
            minimumWristMergeDistance,
            maximumWristMergeDistance
        );

        if (Vector2.Distance(left.Center, right.Center) > threshold)
        {
            wristsMerged = false;
            return;
        }

        float totalConfidence = Mathf.Max(
            0.0001f,
            left.Confidence + right.Confidence
        );
        gameplayWrists.Clear();
        gameplayWrists.Add(new WristDetection
        {
            Center =
                (left.Center * left.Confidence + right.Center * right.Confidence) /
                totalConfidence,
            Confidence = Mathf.Max(left.Confidence, right.Confidence)
        });
        wristsMerged = true;
    }

    private BodyLandmark ReadKeypoint(
        Tensor<float> keypoints,
        int personIndex,
        int keypointIndex)
    {
        int offset = keypointValueOffset + keypointIndex * 3;
        Vector2 previewPoint = WebCamInputProvider.LetterboxInferenceToPreview(
            new Vector2(
                keypoints[personIndex, offset] / ImageWidth,
                keypoints[personIndex, offset + 1] / ImageHeight
            ),
            inferenceCrop
        );
        float confidence = Mathf.Clamp01(keypoints[personIndex, offset + 2]);

        return new BodyLandmark(
            Mathf.Clamp01(previewPoint.x),
            Mathf.Clamp01(previewPoint.y),
            confidence,
            confidence
        );
    }

    private bool HasRequiredCalibrationAnchors(BodyPoseLandmarks pose)
    {
        return IsConfident(pose.Nose)
            && IsConfident(pose.LeftShoulder)
            && IsConfident(pose.RightShoulder)
            && IsConfident(pose.LeftHip)
            && IsConfident(pose.RightHip)
            && IsConfident(pose.LeftAnkle)
            && IsConfident(pose.RightAnkle);
    }

    private bool IsConfident(BodyLandmark landmark)
    {
        return landmark.Presence >= keypointConfidenceThreshold;
    }

    private void PrepareInferenceTexture()
    {
        inferenceCrop = cameraInput.GetLetterboxInferenceRect();
        cameraInput.DrawLetterboxedFrame(inferenceTexture);
    }

    private static BodyPoseLandmarks SmoothPose(
        BodyPoseLandmarks previous,
        BodyPoseLandmarks current)
    {
        const float positionWeight = 0.45f;

        current.Nose = SmoothLandmark(previous.Nose, current.Nose, positionWeight);
        current.LeftShoulder =
            SmoothLandmark(previous.LeftShoulder, current.LeftShoulder, positionWeight);
        current.RightShoulder =
            SmoothLandmark(previous.RightShoulder, current.RightShoulder, positionWeight);
        current.LeftElbow =
            SmoothLandmark(previous.LeftElbow, current.LeftElbow, positionWeight);
        current.RightElbow =
            SmoothLandmark(previous.RightElbow, current.RightElbow, positionWeight);
        current.LeftWrist =
            SmoothLandmark(previous.LeftWrist, current.LeftWrist, positionWeight);
        current.RightWrist =
            SmoothLandmark(previous.RightWrist, current.RightWrist, positionWeight);
        current.LeftHip = SmoothLandmark(previous.LeftHip, current.LeftHip, positionWeight);
        current.RightHip = SmoothLandmark(previous.RightHip, current.RightHip, positionWeight);
        current.LeftAnkle =
            SmoothLandmark(previous.LeftAnkle, current.LeftAnkle, positionWeight);
        current.RightAnkle =
            SmoothLandmark(previous.RightAnkle, current.RightAnkle, positionWeight);
        return current;
    }

    private static BodyLandmark SmoothLandmark(
        BodyLandmark previous,
        BodyLandmark current,
        float weight)
    {
        current.X = Mathf.Lerp(previous.X, current.X, weight);
        current.Y = Mathf.Lerp(previous.Y, current.Y, weight);
        return current;
    }

    private void LogPoseState(bool poseFound, float score)
    {
        if (!logDebugInfo || Time.unscaledTime - lastDebugLogTime < 1f)
            return;

        lastDebugLogTime = Time.unscaledTime;
        float leftConfidence = poseFound ? currentPose.LeftWrist.Presence : 0f;
        float rightConfidence = poseFound ? currentPose.RightWrist.Presence : 0f;
        Debug.Log(
            "YoloBodyPoseProvider: " +
            $"leftWrist={leftConfidence:F3}, " +
            $"rightWrist={rightConfidence:F3}, " +
            $"threshold={wristConfidenceThreshold:F2}, " +
            $"trackId={lockedTrackId}."
        );
    }

    private void OnDestroy()
    {
        initialized = false;
        inferencePending = false;
        pendingInput?.Dispose();
        pendingInput = null;
        pendingKeypoints = null;
        worker?.Dispose();
        worker = null;
        centersToCorners?.Dispose();
        centersToCorners = null;

        if (inferenceTexture != null)
        {
            inferenceTexture.Release();
            Destroy(inferenceTexture);
            inferenceTexture = null;
        }
    }
}
