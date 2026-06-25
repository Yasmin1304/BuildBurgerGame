using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Reverses the direct children of a layout container for Arabic and restores
/// their original order for English.
/// </summary>
[DisallowMultipleComponent]
public sealed class LocalizedLayoutDirection : MonoBehaviour
{
    [SerializeField] private bool reverseForArabic = true;
    [SerializeField] private bool mirrorLayoutPadding = true;
    [SerializeField] private bool normalizeRowChildWidths = true;
    [SerializeField] private float arabicHorizontalOffset;

    private readonly List<Transform> originalOrder = new();
    private RectTransform rectTransform;
    private HorizontalOrVerticalLayoutGroup layoutGroup;
    private RectOffset originalPadding;
    private TextAnchor originalChildAlignment;
    private Vector2 originalAnchoredPosition;
    private bool subscribed;
    private bool controlsLayout;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        layoutGroup = GetComponent<HorizontalOrVerticalLayoutGroup>();
        controlsLayout =
            layoutGroup != null &&
            GetComponentInParent<Selectable>() == null;
        originalAnchoredPosition =
            rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;

        if (layoutGroup != null)
        {
            originalPadding = CopyPadding(layoutGroup.padding);
            originalChildAlignment = layoutGroup.childAlignment;
        }

        CaptureOriginalOrder();
        NormalizeRowChildWidths();
    }

    private void OnEnable()
    {
        Subscribe();
        Refresh();
    }

    private void Start()
    {
        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void Refresh()
    {
        if (!controlsLayout)
            return;

        RemoveDestroyedChildren();

        bool useReversedOrder =
            reverseForArabic &&
            LanguageManager.Instance != null &&
            LanguageManager.Instance.CurrentLanguage == AppLanguage.Arabic;

        int childCount = originalOrder.Count;
        for (int siblingIndex = 0; siblingIndex < childCount; siblingIndex++)
        {
            int sourceIndex = useReversedOrder
                ? childCount - 1 - siblingIndex
                : siblingIndex;

            originalOrder[sourceIndex].SetSiblingIndex(siblingIndex);
        }

        ApplyLayoutDirection(useReversedOrder);

        if (rectTransform != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }

    public void RecaptureOriginalOrder()
    {
        CaptureOriginalOrder();
        Refresh();
    }

    private void CaptureOriginalOrder()
    {
        originalOrder.Clear();
        for (int i = 0; i < transform.childCount; i++)
            originalOrder.Add(transform.GetChild(i));
    }

    private void RemoveDestroyedChildren()
    {
        originalOrder.RemoveAll(child => child == null);
    }

    private void ApplyLayoutDirection(bool useArabicLayout)
    {
        if (layoutGroup != null && originalPadding != null)
        {
            layoutGroup.padding = useArabicLayout && mirrorLayoutPadding
                ? new RectOffset(
                    originalPadding.right,
                    originalPadding.left,
                    originalPadding.top,
                    originalPadding.bottom
                )
                : CopyPadding(originalPadding);

            // Keep the row container itself in its original alignment. The
            // localized title text handles left/right alignment separately.
            layoutGroup.childAlignment = originalChildAlignment;
        }

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition =
                originalAnchoredPosition +
                (useArabicLayout
                    ? new Vector2(arabicHorizontalOffset, 0f)
                    : Vector2.zero);
        }
    }

    private static RectOffset CopyPadding(RectOffset source)
    {
        return new RectOffset(
            source.left,
            source.right,
            source.top,
            source.bottom
        );
    }

    private void NormalizeRowChildWidths()
    {
        if (!controlsLayout || !normalizeRowChildWidths)
            return;

        RectTransform title = null;
        foreach (Transform child in originalOrder)
        {
            if (child != null &&
                child.name.EndsWith(
                    "Title",
                    System.StringComparison.OrdinalIgnoreCase))
            {
                title = child as RectTransform;
                break;
            }
        }

        if (title == null || title.rect.width <= 0f)
            return;

        float rowColumnWidth = title.rect.width;
        foreach (Transform child in originalOrder)
        {
            if (child is RectTransform childRect)
                childRect.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Horizontal,
                    rowColumnWidth
                );
        }
    }

    private void HandleLanguageChanged(AppLanguage _)
    {
        Refresh();
    }

    private void Subscribe()
    {
        if (subscribed || LanguageManager.Instance == null)
            return;

        LanguageManager.Instance.LanguageChanged += HandleLanguageChanged;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || LanguageManager.Instance == null)
            return;

        LanguageManager.Instance.LanguageChanged -= HandleLanguageChanged;
        subscribed = false;
    }
}
