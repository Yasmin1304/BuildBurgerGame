using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public Transform burgerStack;     // drag same burgerStack used by HandCatch
    public ScoreManager scoreManager; // drag it
    public LevelCompleteUI levelCompleteUI;

    public GameMode currentMode; //Theme of the game: Burger, Letters, Numbers 
    //currentMode = Letters

    [SerializeField] private GameObject finalCompletePanel;
    

    [Header("Level Settings")]
    public LevelConfig[] levels;
    public int currentLevelIndex = 0;
    public int CurrentLevelNumber => currentLevelIndex + 1;

    [Header("References")]
    public IngredientSpawner ingredientSpawner;
    public ObstacleSpawner obstacleSpawner;

    [Header("UI (optional)")]
    public TMPro.TMP_Text levelText;

    [Header("Freeze On Level Complete")]
    public GameObject trackingSystemsRoot; // Parent of Solution + HandCircles (hand tracking)
    public GameObject burgerStackRoot;     // The visual burger stack parent
    public GameObject platePlaceholderL;      // Plate_placeholder_L
    public GameObject platePlaceholderR;      // Plate_placeholder_R

    [Header("Audio")]
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip levelCompleteClip;

    void Start()
    {
        ApplyLevel(currentLevelIndex);
    }

    public void ApplyLevel(int index)
    {
        if (levels == null || levels.Length == 0) return;
        index = Mathf.Clamp(index, 0, levels.Length - 1);

        var cfg = levels[index];

        // Per level settings
        var runtimeSettings = SettingsData.GetLevelSettings(index);
        if (runtimeSettings == null) return;

        // Ingredients
        ingredientSpawner.spawnInterval = runtimeSettings.ingredientSpawnInterval;
        ingredientSpawner.maxIngredients = runtimeSettings.maxIngredients;

        // Tell the progress container how many ingredients this level needs
        var progressUI = FindObjectOfType<BurgerProgressUI>();
        if (progressUI != null)
            progressUI.SetTarget(runtimeSettings.maxIngredients);

        ingredientSpawner.guaranteeWithinFirst = cfg.bottomBunWithinFirst;
        ingredientSpawner.bottomBunPrefab = ingredientSpawner.bottomBunPrefab; // already assigned in inspector
        ingredientSpawner.StartSpawning(); // restart counts + InvokeRepeating

        // Guarantee toggle
        //ingredientSpawner.enableBottomBunGuarantee = cfg.guaranteeBottomBun;

        // Obstacles
        if (obstacleSpawner != null)
        {
            obstacleSpawner.spawnInterval = runtimeSettings.obstacleSpawnInterval;
            obstacleSpawner.enabled = runtimeSettings.enableObstacles;

            if (runtimeSettings.enableObstacles) obstacleSpawner.StartSpawning();
            else obstacleSpawner.StopSpawning();
        }

        if (levelText != null)
            levelText.text = cfg.levelName;
        
        FindObjectOfType<SupabaseSessionInsert>()?.CreateSessionForCurrentLevel();
    }

    void ShowFinalCompletePanel()
    {
        if (finalCompletePanel != null)
            finalCompletePanel.SetActive(true);
    }

    public void NextLevel()
    {
        currentLevelIndex++;

        //if (currentLevelIndex >= levels.Length)
        if (currentLevelIndex >= Mathf.Min(SettingsData.levelCount, levels.Length))
        {
            Debug.Log("All levels complete!");

            ShowFinalCompletePanel();
            return;
        }

        StartCoroutine(NextLevelRoutine());
    }

    // public void RequestNextLevel()
    // {
    //     // Hide only the burger ingredients (NOT confetti)
    //     HideBurgerVisuals();
    //     HidePlates();

    //     int score = scoreManager != null ? scoreManager.CurrentScore : 0;
    //     FindObjectOfType<SupabaseSessionUpdate>()?.UpdateCurrentSession();
        
    //     if (levelCompleteUI != null)
    //         levelCompleteUI.Show(CurrentLevelNumber, score);
    // }
    public void RequestNextLevel()
    {
        // Hide gameplay visuals
        HideCaughtVisuals();
        HidePlates();

        // Always save/update the current session first
        FindObjectOfType<SupabaseSessionUpdate>()?.UpdateCurrentSession();

        int totalPlayableLevels = Mathf.Min(SettingsData.levelCount, levels.Length);
        bool isLastPlayableLevel = currentLevelIndex >= totalPlayableLevels - 1;

        if (isLastPlayableLevel)
        {
            Debug.Log("Last playable level completed!");
            ShowFinalCompletePanel();
            return;
        }

        int score = scoreManager != null ? scoreManager.CurrentScore : 0;

        PlayLevelCompleteSound();

        if (levelCompleteUI != null)
            levelCompleteUI.Show(CurrentLevelNumber, score);
    }
    
    void HideCaughtVisuals()
    {
        foreach (Transform root in GetCatchRoots())
        {
            for (int i = 0; i < root.childCount; i++)
                root.GetChild(i).gameObject.SetActive(false);
        }
    }

    IEnumerable<Transform> GetCatchRoots()
    {
        HashSet<Transform> uniqueRoots = new HashSet<Transform>();

        if (burgerStack != null)
            uniqueRoots.Add(burgerStack);

        foreach (var receiver in FindObjectsOfType<FreeDropReceiver>())
        {
            if (receiver.FreeDropContainer != null)
                uniqueRoots.Add(receiver.FreeDropContainer);
        }

        return uniqueRoots;
    }

    void HidePlates()
    {
        if (platePlaceholderL != null) platePlaceholderL.SetActive(false);
        if (platePlaceholderR != null) platePlaceholderR.SetActive(false);
    }

    void ShowPlates()
    {
        if (platePlaceholderL != null) platePlaceholderL.SetActive(true);
        if (platePlaceholderR != null) platePlaceholderR.SetActive(true);
    }

    void PlayLevelCompleteSound()
    {
        if (levelCompleteClip == null)
            return;

        if (uiAudioSource != null)
        {
            uiAudioSource.PlayOneShot(levelCompleteClip);
            return;
        }

        AudioSource.PlayClipAtPoint(levelCompleteClip, Camera.main != null ? Camera.main.transform.position : Vector3.zero);
    }

    public void ConfirmNextLevel()
    {
        // Researcher pressed Next Level
        NextLevel(); // your existing method that resets + ApplyLevel
    }


    System.Collections.IEnumerator NextLevelRoutine()
    {
        // 1) Stop spawners first
        if (ingredientSpawner != null) ingredientSpawner.StopSpawning();
        if (obstacleSpawner != null) obstacleSpawner.StopSpawning();

        // 2) Clear stacked burger visuals
        ClearCaughtItems();

        // 3) Reset shared catch state (VERY important because yours is static)
        HandCatch3D.ResetSharedState();
        FreeDropReceiver.ResetSharedState();
        foreach (var receiver in FindObjectsOfType<FreeDropReceiver>())
            receiver.ResetReceiverState();

        // 4) Reset score && Reset the burger progress container
        if (scoreManager != null) scoreManager.ResetScore();
        FindObjectOfType<BurgerProgressUI>()?.ResetProgress();

        // 5) Re-enable both hand colliders (they were disabled on StopGame)
        foreach (var catcher in FindObjectsOfType<HandCatch3D>())
        {
            var col = catcher.GetComponent<Collider>();
            if (col != null) col.enabled = true;
        }

        foreach (var receiver in FindObjectsOfType<FreeDropReceiver>())
        {
            var col = receiver.GetComponent<Collider>();
            if (col != null) col.enabled = true;
        }

        // (Optional) small delay so the scene visually “breathes”
        yield return new WaitForSeconds(0.2f);

        if (burgerStackRoot != null)
            burgerStackRoot.SetActive(true);

        // Re-enable plate visuals for next level
        ShowPlates();

        // 6) Apply next level settings (spawn intervals, obstacle enable, max ingredients, etc.)
        ApplyLevel(currentLevelIndex);

        // 7) Restart spawners with new settings
        if (ingredientSpawner != null) ingredientSpawner.StartSpawning();

        if (obstacleSpawner != null)
        {
            var runtimeSettings = SettingsData.GetLevelSettings(currentLevelIndex);

            if (runtimeSettings != null)
            {
                obstacleSpawner.spawnInterval = runtimeSettings.obstacleSpawnInterval;

                if (runtimeSettings.enableObstacles)
                {
                    obstacleSpawner.enabled = true;
                    obstacleSpawner.StartSpawning();
                }
                else
                {
                    obstacleSpawner.StopSpawning();
                    obstacleSpawner.enabled = false;
                }
            }
        }
    }

    void ClearCaughtItems()
    {
        foreach (Transform root in GetCatchRoots())
        {
            for (int i = root.childCount - 1; i >= 0; i--)
                Destroy(root.GetChild(i).gameObject);
        }
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void PlayAgain()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
