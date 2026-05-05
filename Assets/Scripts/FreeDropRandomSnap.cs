using System.Collections.Generic;
using UnityEngine;

public class FreeDropRandomSnap : MonoBehaviour
{
    private Rigidbody rb;
    private Vector3 targetPosition;
    private bool snapped = false;

    private List<Vector3> placedPositions;
    private float minLetterDistance;

    [SerializeField] private float snapThresholdY = 0.15f;

    public void Init(
        Rigidbody targetRb,
        Vector3 targetPos,
        List<Vector3> existingPlacedPositions,
        float targetMinLetterDistance)
    {
        rb = targetRb;
        targetPosition = targetPos;
        placedPositions = existingPlacedPositions;
        minLetterDistance = targetMinLetterDistance;
        snapped = false;
    }

    void Update()
    {
        if (snapped || rb == null) return;

        if (transform.position.y <= targetPosition.y + snapThresholdY)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;

            transform.position = targetPosition;
            snapped = true;
        }
    }
}
