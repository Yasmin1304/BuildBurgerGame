using System.Collections;
using UnityEngine;

/// <summary>
/// Shows a short visual "wrong catch" feedback, such as an X image,
/// when the child catches an obstacle.
/// </summary>
public class BadCatchFeedbackUI : MonoBehaviour
{
    [SerializeField] private GameObject xImage;
    [SerializeField] private float showDuration = 0.6f;

    private Coroutine showRoutine;

    private void Awake()
    {
        if (xImage != null)
            xImage.SetActive(false);
    }

    public void Show()
    {
        if (showRoutine != null)
            StopCoroutine(showRoutine);

        showRoutine = StartCoroutine(ShowRoutine());
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
