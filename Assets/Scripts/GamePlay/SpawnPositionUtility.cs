using System.Collections.Generic;
using UnityEngine;

public static class SpawnPositionUtility
{
    private static readonly string[] FallingTags = { "Ingredient", "FreeFall", "Obstacle" };

    public static bool TryGetVisibleXRange(
        Camera cam,
        float planeZ,
        float screenY,
        float edgePadding,
        out float minX,
        out float maxX,
        out Vector3 leftWorld
    )
    {
        minX = 0f;
        maxX = 0f;
        leftWorld = Vector3.zero;

        if (cam == null)
            return false;

        float zDistance = Mathf.Abs(cam.transform.position.z - planeZ);
        leftWorld = cam.ScreenToWorldPoint(new Vector3(0f, screenY, zDistance));
        Vector3 rightWorld = cam.ScreenToWorldPoint(new Vector3(Screen.width, screenY, zDistance));

        minX = Mathf.Min(leftWorld.x, rightWorld.x) + edgePadding;
        maxX = Mathf.Max(leftWorld.x, rightWorld.x) - edgePadding;

        if (minX <= maxX)
            return true;

        float centerX = (leftWorld.x + rightWorld.x) * 0.5f;
        minX = centerX;
        maxX = centerX;
        return true;
    }

    public static bool TryGetRandomXAvoidingFallingItems(
        float minX,
        float maxX,
        float minSpacing,
        int attempts,
        out float x
    )
    {
        x = 0f;
        minSpacing = Mathf.Max(0f, minSpacing);
        attempts = Mathf.Max(1, attempts);

        List<float> blockedXPositions = GetBlockedXPositions(minX, maxX);

        for (int i = 0; i < attempts; i++)
        {
            float candidate = Random.Range(minX, maxX);
            if (HasEnoughSpacing(candidate, blockedXPositions, minSpacing))
            {
                x = candidate;
                return true;
            }
        }

        return TryGetWidestAvailableGapCenter(minX, maxX, blockedXPositions, minSpacing, out x);
    }

    private static List<float> GetBlockedXPositions(float minX, float maxX)
    {
        List<float> xPositions = new List<float>();

        foreach (string tag in FallingTags)
        {
            GameObject[] objects;
            try
            {
                objects = GameObject.FindGameObjectsWithTag(tag);
            }
            catch (UnityException)
            {
                continue;
            }

            foreach (GameObject obj in objects)
            {
                if (obj == null || !obj.activeInHierarchy)
                    continue;

                Rigidbody rb = obj.GetComponent<Rigidbody>();
                if (rb != null && rb.isKinematic)
                    continue;

                float x = obj.transform.position.x;
                if (x >= minX && x <= maxX)
                    xPositions.Add(x);
            }
        }

        xPositions.Sort();
        return xPositions;
    }

    private static bool HasEnoughSpacing(float candidate, List<float> blockedXPositions, float minSpacing)
    {
        foreach (float blockedX in blockedXPositions)
        {
            if (Mathf.Abs(candidate - blockedX) < minSpacing)
                return false;
        }

        return true;
    }

    private static bool TryGetWidestAvailableGapCenter(
        float minX,
        float maxX,
        List<float> blockedXPositions,
        float minSpacing,
        out float x
    )
    {
        x = 0f;
        float bestStart = 0f;
        float bestEnd = 0f;
        float bestWidth = -1f;
        float previousEnd = minX;

        foreach (float blockedX in blockedXPositions)
        {
            float gapEnd = Mathf.Clamp(blockedX - minSpacing, minX, maxX);
            float gapWidth = gapEnd - previousEnd;
            if (gapWidth > bestWidth)
            {
                bestStart = previousEnd;
                bestEnd = gapEnd;
                bestWidth = gapWidth;
            }

            previousEnd = Mathf.Max(previousEnd, Mathf.Clamp(blockedX + minSpacing, minX, maxX));
        }

        float finalGapWidth = maxX - previousEnd;
        if (finalGapWidth > bestWidth)
        {
            bestStart = previousEnd;
            bestEnd = maxX;
            bestWidth = finalGapWidth;
        }

        if (bestWidth < 0f)
            return false;

        x = (bestStart + bestEnd) * 0.5f;
        return HasEnoughSpacing(x, blockedXPositions, minSpacing);
    }
}
