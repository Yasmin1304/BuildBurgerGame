using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonPress : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerExitHandler
{
    private Vector3 originalScale;
    private bool hasOriginalScale;

    [SerializeField] private float pressedScale = 0.9f;
    [SerializeField] private float animationSpeed = 12f;

    private Vector3 targetScale;

    private void Awake()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
        hasOriginalScale = true;
    }

    private void OnEnable()
    {
        ResetScaleImmediately();
    }

    private void OnDisable()
    {
        ResetScaleImmediately();
    }

    private void Update()
    {
        if (!hasOriginalScale)
            return;

        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.unscaledDeltaTime * animationSpeed
        );
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!hasOriginalScale)
            originalScale = transform.localScale;

        targetScale = originalScale * pressedScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        targetScale = originalScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
    }

    private void ResetScaleImmediately()
    {
        if (!hasOriginalScale)
            return;

        targetScale = originalScale;
        transform.localScale = originalScale;
    }
}
