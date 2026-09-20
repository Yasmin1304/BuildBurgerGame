using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows a short visual "wrong catch" feedback, such as an X image,
/// when the child catches an obstacle.
/// </summary>
public class BadCatchFeedbackUI : MonoBehaviour
{
    [SerializeField] private GameObject xImage;
    [SerializeField] private float showDuration = 0.6f;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Vector2 screenOffset;
    [SerializeField] private bool forceFreeUiPositioning = true;

    private Coroutine showRoutine;

    private void Awake()
    {
        if (xImage != null)
            xImage.SetActive(false);
    }

    public void Show()
    {
        ShowAtScreenCenter();
    }

    public void ShowAtWorldPosition(Vector3 worldPosition)
    {
        if (showRoutine != null)
            StopCoroutine(showRoutine);

        MoveFeedbackToWorldPosition(worldPosition);
        showRoutine = StartCoroutine(ShowRoutine());
    }

    private void ShowAtScreenCenter()
    {
        if (showRoutine != null)
            StopCoroutine(showRoutine);

        MoveFeedbackToScreenPoint(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
        showRoutine = StartCoroutine(ShowRoutine());
    }

    private void MoveFeedbackToWorldPosition(Vector3 worldPosition)
    {
        if (xImage != null && !xImage.TryGetComponent(out RectTransform _))
        {
            xImage.transform.position = worldPosition;
            return;
        }

        Camera cameraToUse = worldCamera != null ? worldCamera : Camera.main;
        if (cameraToUse == null)
            return;

        MoveFeedbackToScreenPoint((Vector2)cameraToUse.WorldToScreenPoint(worldPosition) + screenOffset);
    }

    private void MoveFeedbackToScreenPoint(Vector2 screenPoint)
    {
        if (xImage == null)
            return;

        if (!xImage.TryGetComponent(out RectTransform xRectTransform))
        {
            xImage.transform.position = screenPoint;
            return;
        }

        PrepareFreeUiPositioning(xRectTransform);

        Canvas canvas = xImage.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            xRectTransform.position = screenPoint;
            return;
        }

        RectTransform parentRectTransform = xRectTransform.parent as RectTransform;
        if (parentRectTransform == null)
        {
            xRectTransform.position = screenPoint;
            return;
        }

        Camera canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera != null ? canvas.worldCamera : worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRectTransform,
                screenPoint,
                canvasCamera,
                out Vector2 localPoint))
        {
            xRectTransform.anchoredPosition = localPoint;
        }
    }

    private void PrepareFreeUiPositioning(RectTransform xRectTransform)
    {
        if (!forceFreeUiPositioning)
            return;

        xRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        xRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        xRectTransform.pivot = new Vector2(0.5f, 0.5f);
        xRectTransform.SetAsLastSibling();

        LayoutElement layoutElement = xImage.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = xImage.AddComponent<LayoutElement>();

        layoutElement.ignoreLayout = true;
    }

    private IEnumerator ShowRoutine()
    {
        if (xImage != null)
            xImage.SetActive(true);

        yield return new WaitForSeconds(showDuration);

        if (xImage != null)
            xImage.SetActive(false);

        showRoutine = null;
    }
}
