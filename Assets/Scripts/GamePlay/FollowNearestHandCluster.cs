using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Moves one gameplay basket using YOLO pose wrist coordinates.
/// In single-basket mode, slot 0 follows the midpoint between both wrists and
/// other slots hide themselves. Legacy mode keeps slot 0 on the screen-left
/// wrist and slot 1 on the screen-right wrist.
/// </summary>
public class FollowNearestHandCluster : MonoBehaviour
{
    [Header("0 = left circle, 1 = right circle")]
    public int handSlot;

    [Header("Single Basket")]
    [SerializeField] private bool useSingleBasketMode = true;
    [SerializeField] private int primarySingleBasketSlot = 0;
    [SerializeField] private bool requireBothHandsForSingleBasket = true;
    [SerializeField, Range(0f, 1f)] private float maxHandDistanceForSingleBasket = 0.32f;
    [SerializeField] private bool useConfidenceWeightedMidpoint = true;

    [Header("Relative Plate Size")]
    [SerializeField] private bool scaleWithBodyDistance = true;
    [SerializeField] private bool useFirstValidPoseAsReference = true;
    [SerializeField, Range(0.05f, 1f)] private float referenceShoulderWidth = 0.22f;
    [SerializeField, Range(0.2f, 3f)] private float minRelativeScale = 0.65f;
    [SerializeField, Range(0.2f, 3f)] private float maxRelativeScale = 1.25f;
    [SerializeField, Min(0f)] private float scaleSmoothing = 12f;

    [SerializeField] private YoloBodyPoseProvider trackingProvider;
    public Camera cam;
    public float planeZ;

    private Renderer[] cachedRenderers;
    private Graphic[] cachedGraphics;
    private Collider[] cachedColliders;
    private Vector3 originalLocalScale;
    private bool hasReferenceShoulderWidth;

    private void Awake()
    {
        if (cam == null)
            cam = Camera.main;
        if (trackingProvider == null)
            trackingProvider = FindObjectOfType<YoloBodyPoseProvider>();

        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        cachedGraphics = GetComponentsInChildren<Graphic>(true);
        cachedColliders = GetComponentsInChildren<Collider>(true);
        originalLocalScale = transform.localScale;
        hasReferenceShoulderWidth = !useFirstValidPoseAsReference && referenceShoulderWidth > 0f;
    }

    private void Update()
    {
        if (trackingProvider == null)
            trackingProvider = FindObjectOfType<YoloBodyPoseProvider>();

        if (useSingleBasketMode)
        {
            UpdateSingleBasket();
            return;
        }

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
        UpdateRelativeScale();
    }

    private void UpdateSingleBasket()
    {
        if (handSlot != primarySingleBasketSlot)
        {
            SetVisible(false);
            return;
        }

        if (trackingProvider == null)
        {
            SetVisible(false);
            return;
        }

        if (requireBothHandsForSingleBasket)
        {
            if (!trackingProvider.TryGetTwoHandMidpoint(
                    out YoloBodyPoseProvider.WristDetection midpoint,
                    maxHandDistanceForSingleBasket,
                    useConfidenceWeightedMidpoint))
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);
            MoveToNormalizedCameraPoint(midpoint.Center);
            UpdateRelativeScale();
            return;
        }

        if (!trackingProvider.TryGetWrists(
                out IReadOnlyList<YoloBodyPoseProvider.WristDetection> wrists) ||
            !TryChooseDetection(wrists, out YoloBodyPoseProvider.WristDetection detection))
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
        MoveToNormalizedCameraPoint(detection.Center);
        UpdateRelativeScale();
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

    private void UpdateRelativeScale()
    {
        if (!scaleWithBodyDistance || trackingProvider == null)
            return;

        if (!trackingProvider.TryGetLatestPose(out BodyPoseLandmarks pose))
            return;

        float shoulderWidth = Mathf.Abs(
            pose.LeftShoulder.X - pose.RightShoulder.X
        );

        if (shoulderWidth <= 0.001f)
            return;

        if (useFirstValidPoseAsReference && !hasReferenceShoulderWidth)
        {
            referenceShoulderWidth = shoulderWidth;
            hasReferenceShoulderWidth = true;
        }

        float safeReferenceWidth = Mathf.Max(0.001f, referenceShoulderWidth);
        float targetScaleMultiplier = Mathf.Clamp(
            shoulderWidth / safeReferenceWidth,
            minRelativeScale,
            maxRelativeScale
        );
        Vector3 targetScale = originalLocalScale * targetScaleMultiplier;

        if (scaleSmoothing <= 0f)
        {
            transform.localScale = targetScale;
            return;
        }

        float t = 1f - Mathf.Exp(-scaleSmoothing * Time.deltaTime);
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, t);
    }

    private void SetVisible(bool visible)
    {
        if (cachedRenderers != null)
        {
            foreach (Renderer cachedRenderer in cachedRenderers)
            {
                if (cachedRenderer != null)
                    cachedRenderer.enabled = visible;
            }
        }

        if (cachedColliders != null)
        {
            foreach (Collider cachedCollider in cachedColliders)
            {
                if (cachedCollider != null)
                    cachedCollider.enabled = visible;
            }
        }

        if (cachedGraphics != null)
        {
            foreach (Graphic cachedGraphic in cachedGraphics)
            {
                if (cachedGraphic != null)
                    cachedGraphic.enabled = visible;
            }
        }
    }
}
