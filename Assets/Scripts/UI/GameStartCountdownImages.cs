using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameStartCountdownImages : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject countdownOverlay;
    [SerializeField] private Image countdownImage;

    [Header("Countdown Sprites (Assign in order)")]
    [SerializeField] private Sprite sprite3;
    [SerializeField] private Sprite sprite2;
    [SerializeField] private Sprite sprite1;
    [SerializeField] private Sprite spriteGo;

    [Header("Gameplay Root To Enable After Countdown")]
    [SerializeField] private GameObject gameplayRoot;

    [Header("Timing")]
    [SerializeField] private float timePerNumber = 0.8f;
    [SerializeField] private float timeForGo = 0.7f;

    private void Start()
    {
        gameplayRoot.SetActive(false);
        countdownOverlay.SetActive(true);

        StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        yield return Show(sprite3, timePerNumber);
        yield return Show(sprite2, timePerNumber);
        yield return Show(sprite1, timePerNumber);
        yield return Show(spriteGo, timeForGo);

        countdownOverlay.SetActive(false);
        gameplayRoot.SetActive(true);
    }

    private IEnumerator Show(Sprite sprite, float duration)
    {
        countdownImage.sprite = sprite;

        // POP animation
        float t = 0f;
        countdownImage.transform.localScale = Vector3.one * 0.5f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float scale = Mathf.Lerp(0.5f, 1.0f, t / duration);
            countdownImage.transform.localScale = Vector3.one * scale;
            yield return null;
        }
    }
}