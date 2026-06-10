using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Moves one gameplay basket using YOLO pose wrist coordinates.
/// Slot 0 follows the screen-left wrist and slot 1 follows the screen-right wrist.
/// </summary>
public class FollowNearestHandCluster : MonoBehaviour
{
    [Header("0 = left circle, 1 = right circle")]
    public int handSlot;

    [SerializeField] private YoloBodyPoseProvider trackingProvider;
    public Camera cam;
    public float planeZ;

    private Renderer cachedRenderer;
    private Collider cachedCollider;

    private void Awake()
    {
        if (cam == null)
            cam = Camera.main;
        if (trackingProvider == null)
            trackingProvider = FindObjectOfType<YoloBodyPoseProvider>();

        cachedRenderer = GetComponent<Renderer>();
        cachedCollider = GetComponent<Collider>();
    }

    private void Update()
    {
        if (trackingProvider == null)
            trackingProvider = FindObjectOfType<YoloBodyPoseProvider>();

        if (trackingProvider == null ||
            !trackingProvider.TryGetWrists(
                out IReadOnlyList<YoloBodyPoseProvider.WristDetection> wrists))
        {
            SetVisible(false);
            return;
        }

        if (!TryChooseDetection(
                wrists,
                out YoloBodyPoseProvider.WristDetection detection))
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
        MoveToNormalizedCameraPoint(detection.Center);
    }

    private bool TryChooseDetection(
        IReadOnlyList<YoloBodyPoseProvider.WristDetection> wrists,
        out YoloBodyPoseProvider.WristDetection detection)
    {
        detection = default;

        if (wrists == null || wrists.Count == 0)
            return false;

        // The provider sorts wrists from screen-left to screen-right.
        if (wrists.Count >= 2)
        {
            int index = handSlot == 0 ? 0 : wrists.Count - 1;
            detection = wrists[index];
            return true;
        }

        detection = wrists[0];
        bool detectionIsOnLeft = detection.Center.x < 0.5f;
        return handSlot == (detectionIsOnLeft ? 0 : 1);
    }

    private void MoveToNormalizedCameraPoint(Vector2 normalizedPoint)
    {
        if (cam == null)
            return;

        Vector2 previewPoint = trackingProvider != null
            ? trackingProvider.ToScreenPoint(normalizedPoint)
            : new Vector2(
                normalizedPoint.x * Screen.width,
                (1f - normalizedPoint.y) * Screen.height
            );
        Vector3 screenPoint = new Vector3(previewPoint.x, previewPoint.y, 0f);
        Ray ray = cam.ScreenPointToRay(screenPoint);

        float denominator = ray.direction.z;
        if (Mathf.Abs(denominator) < 1e-6f)
            return;

        float distance = (planeZ - ray.origin.z) / denominator;
        transform.position = ray.origin + ray.direction * distance;
    }

    private void SetVisible(bool visible)
    {
        if (cachedRenderer != null)
            cachedRenderer.enabled = visible;
        if (cachedCollider != null)
            cachedCollider.enabled = visible;
    }
}
