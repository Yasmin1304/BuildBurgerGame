using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles non-burger collection modes by placing caught items into a controlled
/// "fake physics" pile inside a container. Items are scaled down and animated into
/// a stable resting position instead of relying on runtime rigidbody settling.
/// </summary>
public class FreeDropReceiver : MonoBehaviour
{
    [Header("Container")]
    [SerializeField] private Transform freeDropContainer;
    [SerializeField] private Collider freeDropBoundsCollider;
    [SerializeField] private bool forceReceiverCollidersAsTriggers = true;

    [Header("Pile Layout")]
    [SerializeField] private bool fitLayoutToMaxIngredients = true;
    [SerializeField] private int fallbackMaxLayoutItems = 10;
    [SerializeField] private float containerPadding = 0.2f;
    [SerializeField] private float cellWidth = 6f;
    [SerializeField] private float cellHeight = 6f;
    [SerializeField] private float cellFillPercent = 1f;
    [SerializeField] private float sizeBoost = 20f;
    [SerializeField] private float horizontalGap = 0f;
    [SerializeField] private float verticalGap = 0f;
    [SerializeField] private float rotationRangeZ = 18f;

    [Header("Animation")]
    [SerializeField] private float settleDuration = 0.28f;
    [SerializeField] private float settleArcHeight = 0.18f;

    [Header("Completion")]
    [SerializeField] private float finishCheckInterval = 0.25f;

    [SerializeField] private IngredientSpawner spawner;

    private bool receiverDone;

    private static readonly HashSet<int> processedInstanceIds = new HashSet<int>();
    private static int placedCount;

    private struct PileLayout
    {
        public float MinX;
        public float MinY;
        public float CellWidth;
        public float CellHeight;
        public int Columns;
        public int Rows;
        public float Z;
    }

    public Transform FreeDropContainer => freeDropContainer;

    void Awake()
    {
        ResetSharedState();
        ResetReceiverState();
    }

    public static void ResetSharedState()
    {
        processedInstanceIds.Clear();
        placedCount = 0;
    }

    public void ResetReceiverState()
    {
        StopAllCoroutines();
        receiverDone = false;

        foreach (var col in GetComponentsInChildren<Collider>(true))
        {
            if (col != null)
            {
                if (forceReceiverCollidersAsTriggers)
                    col.isTrigger = true;

                col.enabled = true;
            }
        }

        if (forceReceiverCollidersAsTriggers && freeDropBoundsCollider != null)
            freeDropBoundsCollider.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("FreeFall")) return;

        GameManager gm = FindObjectOfType<GameManager>();
        if (gm == null || gm.currentMode == GameMode.Burger) return;
        if (receiverDone) return;
        if (freeDropContainer == null || freeDropBoundsCollider == null) return;

        Transform caught = other.attachedRigidbody != null
            ? other.attachedRigidbody.transform
            : other.transform;

        int id = caught.GetInstanceID();
        if (processedInstanceIds.Contains(id)) return;

        Rigidbody rb = caught.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        processedInstanceIds.Add(id);
        LevelItemResolutionTracker.TryResolve(caught.gameObject);
        FindObjectOfType<GameManager>()?.PlayCorrectCatchSound();
        FindObjectOfType<BurgerProgressUI>()?.AddIngredient();
        PlaceIntoPile(caught);
    }

    void PlaceIntoPile(Transform item)
    {
        if (!TryGetBounds(out Bounds bounds))
            return;

        item.SetParent(freeDropContainer, true);
        item.SetAsLastSibling();

        Vector3 startPosition = item.position;
        Quaternion startRotation = item.rotation;
        Vector3 startScale = item.localScale;
        Vector2 spriteSize = EstimateBaseSize(item, bounds);
        PileLayout layout = BuildPileLayout(bounds);
        Vector3 targetScale = GetTargetScale(startScale, spriteSize, layout);
        Vector3 targetPosition = GetPilePosition(layout);
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, Random.Range(-rotationRangeZ, rotationRangeZ));

        foreach (var sr in item.GetComponentsInChildren<SpriteRenderer>(true))
        {
            sr.enabled = true;
            sr.sortingOrder = 500 + placedCount;
        }

        placedCount++;

        StartCoroutine(AnimateIntoPile(
            item,
            startPosition,
            targetPosition,
            startRotation,
            targetRotation,
            startScale,
            targetScale
        ));
    }

    IEnumerator AnimateIntoPile(
        Transform item,
        Vector3 startPosition,
        Vector3 targetPosition,
        Quaternion startRotation,
        Quaternion targetRotation,
        Vector3 startScale,
        Vector3 targetScale)
    {
        float duration = Mathf.Max(0.01f, settleDuration);
        float elapsed = 0f;

        while (elapsed < duration && item != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float arc = Mathf.Sin(t * Mathf.PI) * settleArcHeight;

            item.position = Vector3.Lerp(startPosition, targetPosition, eased) + Vector3.up * arc;
            item.rotation = Quaternion.Slerp(startRotation, targetRotation, eased);
            item.localScale = Vector3.Lerp(startScale, targetScale, eased);

            yield return null;
        }

        if (item != null)
        {
            item.position = targetPosition;
            item.rotation = targetRotation;
            item.localScale = targetScale;
        }

        if (AllFreeFallItemsUsedUp())
        {
            StopGame();
            FindObjectOfType<GameManager>()?.RequestNextLevel();
            FindObjectOfType<SupabaseSessionUpdate>()?.UpdateCurrentSession();
        }
    }

    PileLayout BuildPileLayout(Bounds bounds)
    {
        float usableMinX = bounds.min.x + containerPadding;
        float usableMinY = bounds.min.y + containerPadding;
        float usableWidth = Mathf.Max(0.01f, bounds.size.x - containerPadding * 2f);
        float usableHeight = Mathf.Max(0.01f, bounds.size.y - containerPadding * 2f);

        if (!fitLayoutToMaxIngredients)
        {
            float stepX = Mathf.Max(0.01f, cellWidth + horizontalGap);
            float stepY = Mathf.Max(0.01f, cellHeight + verticalGap);

            return new PileLayout
            {
                MinX = usableMinX,
                MinY = usableMinY,
                CellWidth = Mathf.Max(0.01f, cellWidth),
                CellHeight = Mathf.Max(0.01f, cellHeight),
                Columns = Mathf.Max(1, Mathf.FloorToInt((usableWidth + horizontalGap) / stepX)),
                Rows = Mathf.Max(1, Mathf.FloorToInt((usableHeight + verticalGap) / stepY)),
                Z = bounds.center.z
            };
        }

        int maxItems = GetExpectedMaxLayoutItems();
        float boardAspect = usableWidth / Mathf.Max(0.01f, usableHeight);
        int columns = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(maxItems * boardAspect)));
        int rows = Mathf.Max(1, Mathf.CeilToInt(maxItems / (float)columns));

        float totalHorizontalGap = Mathf.Max(0f, horizontalGap) * Mathf.Max(0, columns - 1);
        float totalVerticalGap = Mathf.Max(0f, verticalGap) * Mathf.Max(0, rows - 1);
        float dynamicCellWidth = Mathf.Max(0.01f, (usableWidth - totalHorizontalGap) / columns);
        float dynamicCellHeight = Mathf.Max(0.01f, (usableHeight - totalVerticalGap) / rows);

        return new PileLayout
        {
            MinX = usableMinX,
            MinY = usableMinY,
            CellWidth = dynamicCellWidth,
            CellHeight = dynamicCellHeight,
            Columns = columns,
            Rows = rows,
            Z = bounds.center.z
        };
    }

    Vector3 GetPilePosition(PileLayout layout)
    {
        int columns = Mathf.Max(1, layout.Columns);
        int rows = Mathf.Max(1, layout.Rows);
        int slot = Mathf.Min(placedCount, columns * rows - 1);
        int column = slot % columns;
        int row = Mathf.Min(slot / columns, rows - 1);
        float stepX = layout.CellWidth + Mathf.Max(0f, horizontalGap);
        float stepY = layout.CellHeight + Mathf.Max(0f, verticalGap);
        float x = layout.MinX + column * stepX + layout.CellWidth * 0.5f;
        float y = layout.MinY + row * stepY + layout.CellHeight * 0.5f;

        return new Vector3(x, y, layout.Z);
    }

    Vector2 EstimateBaseSize(Transform item, Bounds bounds)
    {
        Bounds? mergedBounds = null;
        foreach (var sr in item.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (mergedBounds == null) mergedBounds = sr.bounds;
            else
            {
                Bounds current = mergedBounds.Value;
                current.Encapsulate(sr.bounds);
                mergedBounds = current;
            }
        }

        if (mergedBounds == null)
        {
            float fallback = Mathf.Max(0.1f, Mathf.Min(cellWidth, cellHeight));
            return new Vector2(fallback, fallback);
        }
        Vector3 size = mergedBounds.Value.size;

        return new Vector2(
            Mathf.Max(0.06f, size.x),
            Mathf.Max(0.06f, size.y)
        );
    }

    Vector3 GetTargetScale(Vector3 startScale, Vector2 spriteSize, PileLayout layout)
    {
        float targetWidth = Mathf.Max(0.01f, layout.CellWidth * cellFillPercent * Mathf.Max(0.1f, sizeBoost));
        float targetHeight = Mathf.Max(0.01f, layout.CellHeight * cellFillPercent * Mathf.Max(0.1f, sizeBoost));
        float widthScale = targetWidth / Mathf.Max(0.01f, spriteSize.x);
        float heightScale = targetHeight / Mathf.Max(0.01f, spriteSize.y);
        float fitScale = Mathf.Min(widthScale, heightScale);

        return startScale * fitScale;
    }

    int GetExpectedMaxLayoutItems()
    {
        if (spawner == null) spawner = FindObjectOfType<IngredientSpawner>();
        int maxItems = spawner != null ? spawner.maxIngredients : fallbackMaxLayoutItems;
        return Mathf.Max(1, maxItems);
    }

    bool AllFreeFallItemsUsedUp()
    {
        if (spawner == null) spawner = FindObjectOfType<IngredientSpawner>();
        return LevelItemResolutionTracker.TryRequestCompletion(spawner);
    }

    void StopGame()
    {
        receiverDone = true;

        foreach (var s in FindObjectsOfType<IngredientSpawner>())
        {
            s.StopSpawning();
            s.enabled = false;
        }

        foreach (var o in FindObjectsOfType<ObstacleSpawner>())
        {
            o.StopSpawning();
            o.enabled = false;
        }

        DestroyRemainingFreeFallItems();

        foreach (var receiver in FindObjectsOfType<FreeDropReceiver>())
        {
            var col = receiver.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        foreach (var catcher in FindObjectsOfType<HandCatch3D>())
        {
            var col = catcher.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
    }

    void DestroyRemainingFreeFallItems()
    {
        if (freeDropContainer == null) return;

        var all = GameObject.FindGameObjectsWithTag("FreeFall");
        foreach (var go in all)
        {
            if (go == null) continue;
            if (!go.transform.IsChildOf(freeDropContainer))
                Destroy(go);
        }
    }

    bool TryGetBounds(out Bounds bounds)
    {
        if (freeDropBoundsCollider != null)
        {
            bounds = freeDropBoundsCollider.bounds;
            return true;
        }

        bounds = default;
        return false;
    }
}
