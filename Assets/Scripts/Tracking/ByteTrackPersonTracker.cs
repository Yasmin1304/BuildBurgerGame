using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight ByteTrack-style person tracker for YOLO detections.
/// It performs high-confidence association first, then retries unmatched tracks
/// with low-confidence detections before declaring them temporarily lost.
/// </summary>
public sealed class ByteTrackPersonTracker
{
    public readonly struct Detection
    {
        public readonly Rect Box;
        public readonly float Score;
        public readonly BodyPoseLandmarks Pose;

        public Detection(Rect box, float score, BodyPoseLandmarks pose)
        {
            Box = box;
            Score = score;
            Pose = pose;
        }
    }

    public sealed class Track
    {
        public int Id { get; private set; }
        public Rect Box { get; private set; }
        public float Score { get; private set; }
        public BodyPoseLandmarks Pose { get; private set; }
        public int MissedFrames { get; private set; }
        public bool UpdatedThisFrame { get; private set; }

        private Vector2 centerVelocity;
        private Vector2 sizeVelocity;

        internal Track(int id, Detection detection)
        {
            Id = id;
            ApplyDetection(detection);
        }

        internal void BeginFrame()
        {
            UpdatedThisFrame = false;
            Vector2 predictedSize = new Vector2(
                Mathf.Max(0.001f, Box.width + sizeVelocity.x),
                Mathf.Max(0.001f, Box.height + sizeVelocity.y)
            );
            Vector2 predictedCenter = Box.center + centerVelocity;
            Box = new Rect(predictedCenter - predictedSize * 0.5f, predictedSize);
        }

        internal void Update(Detection detection)
        {
            Vector2 oldCenter = Box.center;
            Vector2 oldSize = Box.size;
            ApplyDetection(detection);
            centerVelocity = Box.center - oldCenter;
            sizeVelocity = Box.size - oldSize;
        }

        internal void MarkMissed()
        {
            MissedFrames++;
        }

        private void ApplyDetection(Detection detection)
        {
            Box = detection.Box;
            Score = detection.Score;
            Pose = detection.Pose;
            MissedFrames = 0;
            UpdatedThisFrame = true;
        }
    }

    private readonly List<Track> tracks = new();
    private readonly List<int> highDetectionIndices = new();
    private readonly List<int> lowDetectionIndices = new();
    private readonly List<int> unmatchedTrackIndices = new();
    private readonly List<int> unmatchedDetectionIndices = new();
    private readonly List<int> unmatchedHighDetectionIndices = new();

    private int nextTrackId = 1;

    public IReadOnlyList<Track> Tracks => tracks;

    public void Reset()
    {
        tracks.Clear();
        nextTrackId = 1;
    }

    public void Update(
        IReadOnlyList<Detection> detections,
        float highThreshold,
        float lowThreshold,
        float newTrackThreshold,
        float matchThreshold,
        int trackBuffer)
    {
        highDetectionIndices.Clear();
        lowDetectionIndices.Clear();

        for (int i = 0; i < detections.Count; i++)
        {
            float score = detections[i].Score;
            if (score >= highThreshold)
                highDetectionIndices.Add(i);
            else if (score >= lowThreshold)
                lowDetectionIndices.Add(i);
        }

        foreach (Track track in tracks)
            track.BeginFrame();

        unmatchedTrackIndices.Clear();
        for (int i = 0; i < tracks.Count; i++)
            unmatchedTrackIndices.Add(i);

        Associate(
            detections,
            highDetectionIndices,
            unmatchedTrackIndices,
            matchThreshold
        );
        unmatchedHighDetectionIndices.Clear();
        unmatchedHighDetectionIndices.AddRange(unmatchedDetectionIndices);

        // ByteTrack's second association rescues tracks using detections that
        // would normally be discarded because their confidence is temporarily low.
        Associate(
            detections,
            lowDetectionIndices,
            unmatchedTrackIndices,
            matchThreshold
        );

        foreach (int trackIndex in unmatchedTrackIndices)
            tracks[trackIndex].MarkMissed();

        foreach (int detectionIndex in unmatchedHighDetectionIndices)
        {
            Detection detection = detections[detectionIndex];
            if (detection.Score >= newTrackThreshold)
                tracks.Add(new Track(nextTrackId++, detection));
        }

        for (int i = tracks.Count - 1; i >= 0; i--)
        {
            if (tracks[i].MissedFrames > trackBuffer)
                tracks.RemoveAt(i);
        }
    }

    public bool TryGetUpdatedTrack(int trackId, out Track track)
    {
        foreach (Track candidate in tracks)
        {
            if (candidate.Id == trackId && candidate.UpdatedThisFrame)
            {
                track = candidate;
                return true;
            }
        }

        track = null;
        return false;
    }

    private void Associate(
        IReadOnlyList<Detection> detections,
        IReadOnlyList<int> detectionIndices,
        List<int> availableTrackIndices,
        float matchThreshold)
    {
        unmatchedDetectionIndices.Clear();
        foreach (int detectionIndex in detectionIndices)
            unmatchedDetectionIndices.Add(detectionIndex);

        float minimumIou = 1f - Mathf.Clamp01(matchThreshold);

        while (availableTrackIndices.Count > 0 &&
               unmatchedDetectionIndices.Count > 0)
        {
            int bestTrackListIndex = -1;
            int bestDetectionListIndex = -1;
            float bestIou = minimumIou;

            for (int trackListIndex = 0;
                 trackListIndex < availableTrackIndices.Count;
                 trackListIndex++)
            {
                Rect trackBox = tracks[
                    availableTrackIndices[trackListIndex]
                ].Box;

                for (int detectionListIndex = 0;
                     detectionListIndex < unmatchedDetectionIndices.Count;
                     detectionListIndex++)
                {
                    Rect detectionBox = detections[
                        unmatchedDetectionIndices[detectionListIndex]
                    ].Box;
                    float iou = CalculateIou(trackBox, detectionBox);
                    if (iou <= bestIou)
                        continue;

                    bestIou = iou;
                    bestTrackListIndex = trackListIndex;
                    bestDetectionListIndex = detectionListIndex;
                }
            }

            if (bestTrackListIndex < 0)
                break;

            int trackIndex = availableTrackIndices[bestTrackListIndex];
            int detectionIndex =
                unmatchedDetectionIndices[bestDetectionListIndex];
            tracks[trackIndex].Update(detections[detectionIndex]);
            availableTrackIndices.RemoveAt(bestTrackListIndex);
            unmatchedDetectionIndices.RemoveAt(bestDetectionListIndex);
        }
    }

    private static float CalculateIou(Rect a, Rect b)
    {
        float xMin = Mathf.Max(a.xMin, b.xMin);
        float yMin = Mathf.Max(a.yMin, b.yMin);
        float xMax = Mathf.Min(a.xMax, b.xMax);
        float yMax = Mathf.Min(a.yMax, b.yMax);
        float intersection =
            Mathf.Max(0f, xMax - xMin) * Mathf.Max(0f, yMax - yMin);
        float union = a.width * a.height + b.width * b.height - intersection;
        return union > 0f ? intersection / union : 0f;
    }
}
