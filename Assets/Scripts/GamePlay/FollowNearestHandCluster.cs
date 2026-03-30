using UnityEngine;
using Mediapipe.Unity;
using System.Collections.Generic;

public class FollowNearestHandCluster : MonoBehaviour
{
    [Header("0 = left circle, 1 = right circle (two-hand mode)")]
    public int handSlot = 0;

    public Camera cam;
    public float planeZ = 0f;
    public float smooth = 15f;
    public float minSplitGapPixels = 120f; // tweak if needed
    
    Vector3 velocity;
    Renderer rend;
    Collider col;
    

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        rend = GetComponent<Renderer>();
        col  = GetComponent<Collider>();
    }

    void SetVisible(bool v)
    {
        if (rend != null) rend.enabled = v;
        if (col  != null) col.enabled  = v;
    }

    void Update()
    {
        // Get all active landmark spheres (both hands)
        var points = GameObject.FindObjectsOfType<PointAnnotation>(true);

        List<Vector3> screenPts = new();
        foreach (var p in points)
        {
            if (!p.gameObject.activeInHierarchy) continue;
            screenPts.Add(cam.WorldToScreenPoint(p.transform.position));
        }

        if (screenPts.Count < 10) { SetVisible(false); return; } // not enough points
        SetVisible(true);

        // --- Split into 2 clusters using screen X ---
        screenPts.Sort((a, b) => a.x.CompareTo(b.x));

        // Find biggest gap in X -> split point
        float maxGap = 0f;
        int splitIndex = screenPts.Count / 2;
        for (int i = 1; i < screenPts.Count; i++)
        {
            float gap = screenPts[i].x - screenPts[i - 1].x;
            if (gap > maxGap)
            {
                maxGap = gap;
                splitIndex = i;
            }
        }

        // Compute overall (single-hand) screen center
        Vector3 overallSum = Vector3.zero;
        for (int i = 0; i < screenPts.Count; i++) overallSum += screenPts[i];
        Vector3 overallCenter = overallSum / screenPts.Count;

        // If the biggest gap is small, it's just ONE hand (points spread), not two hands.
        if (maxGap < minSplitGapPixels)
        {
            // Decide which ball to show based on where the hand is on screen
            bool handIsOnLeftSide = overallCenter.x < (UnityEngine.Screen.width * 0.5f);

            if (handIsOnLeftSide)
            {
                if (handSlot == 0) { SetVisible(true); MoveToScreenCenter(overallCenter); }
                else { SetVisible(false); }
            }
            else
            {
                if (handSlot == 1) { SetVisible(true); MoveToScreenCenter(overallCenter); }
                else { SetVisible(false); }
            }
            return;
        }

        // Otherwise, treat it as TWO hands: cluster A (left) and cluster B (right)
        Vector3 sumA = Vector3.zero; int countA = 0;
        for (int i = 0; i < splitIndex; i++) { sumA += screenPts[i]; countA++; }

        Vector3 sumB = Vector3.zero; int countB = 0;
        for (int i = splitIndex; i < screenPts.Count; i++) { sumB += screenPts[i]; countB++; }

        if (countA < 5 || countB < 5) { SetVisible(false); return; }

        Vector3 centerA = sumA / countA; // left cluster
        Vector3 centerB = sumB / countB; // right cluster

        Vector3 chosen = (handSlot == 0) ? centerA : centerB;
        SetVisible(true);
        MoveToScreenCenter(chosen);

    }

    void MoveToScreenCenter(Vector3 screenCenter)
    {
        Ray ray = cam.ScreenPointToRay(screenCenter);

        float denom = ray.direction.z;
        if (Mathf.Abs(denom) < 1e-6f) return;

        float t = (planeZ - ray.origin.z) / denom;
        Vector3 world = ray.origin + ray.direction * t;

        transform.position = Vector3.SmoothDamp(transform.position, world, ref velocity, 1f / smooth);
    }
}

// Note:
// This script splits hands by left-vs-right on screen.
// If the child crosses hands or overlaps them, the split can get confused.
// If you ever want it to be rock-solid even when hands cross, the “next level” is using MediaPipe handedness (Left/Right) output — 
//  but we can keep this simple for your burger game.