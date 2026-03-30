using TMPro;
using UnityEngine;
using System.Collections;

public class ScoreManager : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    
    [Header("Scoring")]
    [SerializeField] private int scorePerIngredient = 1;
    [SerializeField] private int minScore = 0; // prevent negative scores (set to -999 if you want negatives)
    
    private int score = 0;
    public int CurrentScore => score;


    void Start()
    {
        UpdateUI();
    }

    public void ResetScore()
    {
        score = 0;
        UpdateUI();
    }

    IEnumerator PopEffect()
    {
        Vector3 originalScale = scoreText.transform.localScale;
        scoreText.transform.localScale = originalScale * 1.2f;
        yield return new WaitForSeconds(0.1f);
        scoreText.transform.localScale = originalScale;
    }

    public void AddIngredientScore()
    {
        score += scorePerIngredient;
        UpdateUI();
        StartCoroutine(PopEffect());
    }

    // Called when an obstacle is caught (or any penalty event)
    public void AddPenalty(int penaltyAmount)
    {
        // penaltyAmount should be positive (e.g., 5 means -5)
        AddScore(-Mathf.Abs(penaltyAmount));
        StartCoroutine(ShakeEffect());
    }

    

    // Optional: generic method if you ever want + or - any value
    public void AddScore(int amount)
    {
        score += amount;
        if (score < minScore) score = minScore;
        UpdateUI();
    }

    IEnumerator ShakeEffect()
    {
        if (scoreText == null) yield break;

        // small shake when penalty happens
        Vector3 originalPos = scoreText.transform.localPosition;

        float duration = 0.15f;
        float elapsed = 0f;
        float strength = 6f;

        while (elapsed < duration)
        {
            float x = Random.Range(-strength, strength);
            float y = Random.Range(-strength, strength);
            scoreText.transform.localPosition = originalPos + new Vector3(x, y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        scoreText.transform.localPosition = originalPos;
    }

    public void FlashColor(Color flashColor, float duration = 0.15f)
    {
        StartCoroutine(FlashRoutine(flashColor, duration));
    }

    IEnumerator FlashRoutine(Color flashColor, float duration)
    {
        if (scoreText == null) yield break;

        Color originalColor = scoreText.color;   // save original color
        scoreText.color = flashColor;             // change color

        yield return new WaitForSeconds(duration);

        scoreText.color = originalColor;          // restore original color
    }

    IEnumerator Flash(Color c, float duration)
    {
        if (scoreText == null) yield break;

        Color original = scoreText.color;
        scoreText.color = c;
        yield return new WaitForSeconds(duration);
        scoreText.color = original;
    }



    public void SetScoreText(TMP_Text text)
    {
        scoreText = text;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";
    }
}
