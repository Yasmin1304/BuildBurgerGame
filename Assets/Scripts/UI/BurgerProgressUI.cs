using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BurgerProgressUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image fillImage;     // the FillImage (Filled Vertical)
    [SerializeField] private GameObject[] stars;

    private int ingredientsNeeded = 1;

    private int currentCount = 0;

    void Awake()
    {
        ResetProgress();
    }

    public void SetTarget(int target)
    {
        ingredientsNeeded = Mathf.Max(1, target);
        ResetProgress();
    }

    public void AddIngredient()
    {
        currentCount++;
        float p = Mathf.Clamp01((float)currentCount / ingredientsNeeded);

        if (fillImage != null) fillImage.fillAmount = p;
        UpdateStars(p);
    }

    public void ResetProgress()
    {
        currentCount = 0;
        if (fillImage != null) fillImage.fillAmount = 0f;

        if (stars[0] != null) stars[0].SetActive(false);
        if (stars[1] != null) stars[1].SetActive(false);
        if (stars[2] != null) stars[2].SetActive(false);
    }

    // private void UpdateStars(float p)
    // {
    //     if (stars[0] != null) stars[0].SetActive(p >= 0.33f);
    //     if (stars[1] != null) stars[1].SetActive(p >= 0.66f);
    //     if (stars[2] != null) stars[2].SetActive(p >= 0.99f);
    // }

    void UpdateStars(float progress)
    {
        if (progress >= 0.33f && !stars[0].activeSelf)
            ShowStar(0);

        if (progress >= 0.66f && !stars[1].activeSelf)
            ShowStar(1);

        if (progress >= 1.0f && !stars[2].activeSelf)
            ShowStar(2);
    }

    void ShowStar(int index)
    {
        stars[index].SetActive(true);
        StartCoroutine(PopAnimation(stars[index].transform));
    }
    
    IEnumerator PopAnimation(Transform t)
    {
        float time = 0f;
        float duration = 0.25f;

        Vector3 start = Vector3.zero;
        Vector3 overshoot = Vector3.one * 1.20f;
        Vector3 end = Vector3.one;

        t.localScale = start;

        while (time < duration)
        {
            time += Time.deltaTime;
            float p = time / duration;
            t.localScale = Vector3.Lerp(start, overshoot, p);
            yield return null;
        }

        t.localScale = end;
    }
}