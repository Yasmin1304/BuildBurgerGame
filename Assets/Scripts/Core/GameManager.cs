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

    [Header("Obstacle Instructions")]
    [SerializeField] private GameObject obstacleInstructionsPanel;
    [SerializeField] private bool showObstacleInstructionsOnce = true;

    [Header("Freeze On Level Complete")]
    public GameObject trackingSystemsRoot; // Parent of Solution + HandCircles (hand tracking)
    public GameObject burgerStackRoot;     // The visual burger stack parent
    public GameObject platePlaceholderL;      // Plate_placeholder_L
    public GameObject platePlaceholderR;      // Plate_placeholder_R
    public GameObject whiteboardRoot;

    [Header("Audio")]
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip levelCompleteClip;
    [SerializeField] private AudioClip correctCatchClip;

    [Header("Debug")]
    [SerializeField] private bool logGameStartDebug = true;

    private bool gameStarted;
    private bool pausedForCalibration;
    private bool pausedForHandTracking;
    private bool sceneLevelDefaultsApplied;
    private bool obstacleInstructionsShown;
    private bool waitingForObstacleInstructions;
    private bool nextLevelTransitionInProgress;
    private readonly Dictionary<Rigidbody, FallingRigidbodyState> handPauseStates = new();

    private struct FallingRigidbodyState
    {
        public Vector3 LinearVelocity;
        public Vector3 AngularVelocity;
        public bool UseGravity;
    }

    public bool GameStarted => gameStarted;
    public bool PausedForHandTracking => pausedForHandTracking;
    public bool GameplaySpawningPaused => pausedForCalibration || pausedForHandTracking;

    void Start()
    {
        ApplySceneLevelDefaultsToSettings();

        currentMode = SessionData.SelectedGameMode;
        if (ingredientSpawner != null)
            ingredientSpawner.StopSpawning();

        if (obstacleSpawner != null)
            obstacleSpawner.StopSpawning();

        if (obstacleInstructionsPanel != null)
            obstacleInstructionsPanel.SetActive(false);
    }

    private void ApplySceneLevelDefaultsToSettings()
    {
        if (sceneLevelDefaultsApplied || levels == null)
            return;

        int count = Mathf.Min(levels.Length, SettingsData.levelSettings.Length);
        for (int i = 0; i < count; i++)
        {
            LevelConfig cfg = levels[i];
            LevelSettings runtimeSettings = SettingsData.GetLevelSettings(i);

            if (cfg == null || runtimeSettings == null)
                continue;

            runtimeSettings.ingredientSpawnInterval = cfg.ingredientSpawnInterval;
            runtimeSettings.ingredientFallSpeed = cfg.ingredientFallSpeed;
            runtimeSettings.obstacleSpawnInterval = cfg.obstacleSpawnInterval;
            runtimeSettings.obstacleFallSpeed = cfg.obstacleFallSpeed;
            runtimeSettings.spawnScreenEdgePadding = cfg.spawnScreenEdgePadding;
            runtimeSettings.enableObstacles = cfg.enableObstacles;
            runtimeSettings.maxIngredients = cfg.maxIngredients;
        }

        sceneLevelDefaultsApplied = true;
    }

    public void BeginGame()
    {
        if (gameStarted)
        {
            if (pausedForCalibration)
            {
                ResumeAfterRecalibration();
                return;
            }

            if (logGameStartDebug)
                Debug.Log("GameManager.BeginGame ignored because the game already started.");
            return;
        }

        gameStarted = true;
        currentMode = SessionData.SelectedGameMode;

        EnsureSpawnerReferences();

        if (logGameStartDebug)
        {
            Debug.Log(
                "GameManager.BeginGame called. " +
                $"mode={currentMode}, " +
                $"ingredientSpawner={(ingredientSpawner != null ? ingredientSpawner.name : "null")}, " +
                $"obstacleSpawner={(obstacleSpawner != null ? obstacleSpawner.name : "null")}, " +
                $"levels={(levels != null ? levels.Length : 0)}"
            );
        }

        ApplyLevel(currentLevelIndex);
    }

    public void PauseForRecalibration()
    {
        if (!gameStarted)
            return;

        pausedForCalibration = true;

        if (ingredientSpawner != null)
            ingredientSpawner.PauseSpawning();

        if (obstacleSpawner != null)
            obstacleSpawner.PauseSpawning();

        if (logGameStartDebug)
            Debug.Log("GameManager paused spawning for recalibration.");
    }

    public void ResumeAfterRecalibration()
    {
        if (!gameStarted)
            return;

        pausedForCalibration = false;

        if (!pausedForHandTracking)
        {
            if (ingredientSpawner != null)
                ingredientSpawner.ResumeSpawning();

            if (obstacleSpawner != null)
                obstacleSpawner.ResumeSpawning();
        }

        if (logGameStartDebug)
            Debug.Log("GameManager resumed spawning after recalibration.");
    }

    public void PauseForHandTrackingLost()
    {
        if (!gameStarted || pausedForHandTracking)
            return;

        pausedForHandTracking = true;

        if (ingredientSpawner != null)
            ingredientSpawner.PauseSpawning();

        if (obstacleSpawner != null)
            obstacleSpawner.PauseSpawning();

        FreezeFallingItemsForHandPause();

        if (logGameStartDebug)
            Debug.Log("GameManager paused spawning and falling items because hand tracking was lost.");
    }

    public void ResumeAfterHandTrackingRecovered()
    {
        if (!gameStarted || !pausedForHandTracking)
            return;

        pausedForHandTracking = false;

        if (!pausedForCalibration)
        {
            if (ingredientSpawner != null)
                ingredientSpawner.ResumeSpawning();

            if (obstacleSpawner != null)
                obstacleSpawner.ResumeSpawning();
        }

        ResumeFallingItemsAfterHandPause();

        if (logGameStartDebug)
            Debug.Log("GameManager resumed spawning and falling items because hand tracking recovered.");
    }

    private void FreezeFallingItemsForHandPause()
    {
        handPauseStates.Clear();

        Rigidbody[] rigidbodies = FindObjectsByType<Rigidbody>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (Rigidbody rb in rigidbodies)
        {
            if (rb == null ||
                rb.isKinematic ||
                rb.GetComponentInParent<FallingIngredient>() == null)
            {
                continue;
            }

            handPauseStates[rb] = new FallingRigidbodyState
            {
                LinearVelocity = rb.linearVelocity,
                AngularVelocity = rb.angularVelocity,
                UseGravity = rb.useGravity
            };

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        if (logGameStartDebug)
            Debug.Log($"GameManager froze {handPauseStates.Count} falling items.");
    }

    private void ResumeFallingItemsAfterHandPause()
    {
        foreach (KeyValuePair<Rigidbody, FallingRigidbodyState> entry in handPauseStates)
        {
            Rigidbody rb = entry.Key;
            if (rb == null)
                continue;

            FallingRigidbodyState state = entry.Value;
            rb.isKinematic = false;
            rb.useGravity = state.UseGravity;
            rb.linearVelocity = state.LinearVelocity;
            rb.angularVelocity = state.AngularVelocity;
            rb.WakeUp();
        }

        handPauseStates.Clear();
    }

    public void ApplyLevel(int index)
    {
        EnsureSpawnerReferences();

        if (levels == null || levels.Length == 0)
        {
            Debug.LogWarning("GameManager.ApplyLevel stopped: no levels are assigned.");
            return;
        }

        if (ingredientSpawner == null)
        {
            Debug.LogError("GameManager.ApplyLevel stopped: IngredientSpawner is not assigned or could not be found.");
            return;
        }

        index = Mathf.Clamp(index, 0, levels.Length - 1);

        var cfg = levels[index];

        // Per level settings
        var runtimeSettings = SettingsData.GetLevelSettings(index);
        if (runtimeSettings == null)
        {
            Debug.LogWarning($"GameManager.ApplyLevel stopped: no runtime settings found for level index {index}.");
            return;
        }

        // Ingredients
        ingredientSpawner.spawnInterval = runtimeSettings.ingredientSpawnInterval;
        ingredientSpawner.fallSpeed = runtimeSettings.ingredientFallSpeed;
        ingredientSpawner.spawnScreenEdgePadding = runtimeSettings.spawnScreenEdgePadding;
        ingredientSpawner.maxIngredients = runtimeSettings.maxIngredients;

        // Tell the progress container how many ingredients this level needs
        var progressUI = FindObjectOfType<BurgerProgressUI>();
        if (progressUI != null)
            progressUI.SetTarget(runtimeSettings.maxIngredients);

        ingredientSpawner.guaranteeWithinFirst = cfg.bottomBunWithinFirst;
        ingredientSpawner.bottomBunPrefab = ingredientSpawner.bottomBunPrefab; // already assigned in inspector
        ingredientSpawner.StartSpawning(); // restart counts + InvokeRepeating

        if (logGameStartDebug)
            Debug.Log($"GameManager.ApplyLevel started IngredientSpawner. interval={ingredientSpawner.spawnInterval}, max={ingredientSpawner.maxIngredients}");

        // Guarantee toggle
        //ingredientSpawner.enableBottomBunGuarantee = cfg.guaranteeBottomBun;

        // Obstacles
        if (obstacleSpawner != null)
        {
            obstacleSpawner.spawnInterval = runtimeSettings.obstacleSpawnInterval;
            obstacleSpawner.fallSpeed = runtimeSettings.obstacleFallSpeed;
            obstacleSpawner.spawnScreenEdgePadding = runtimeSettings.spawnScreenEdgePadding;
            obstacleSpawner.enabled = runtimeSettings.enableObstacles;

            if (runtimeSettings.enableObstacles) obstacleSpawner.StartSpawning();
            else obstacleSpawner.StopSpawning();
        }

        if (GameplaySpawningPaused)
        {
            ingredientSpawner.PauseSpawning();
            if (obstacleSpawner != null)
                obstacleSpawner.PauseSpawning();

            if (logGameStartDebug)
                Debug.Log("GameManager.ApplyLevel kept the new level paused.");
        }

        if (levelText != null)
            levelText.text = cfg.levelName;

        UpdateThemeVisuals();
        
        FindObjectOfType<SupabaseSessionInsert>()?.CreateSessionForCurrentLevel();
    }

    void ShowFinalCompletePanel()
    {
        if (finalCompletePanel != null)
            finalCompletePanel.SetActive(true);
    }

    void EnsureSpawnerReferences()
    {
        if (ingredientSpawner == null)
            ingredientSpawner = FindObjectOfType<IngredientSpawner>(true);

        if (obstacleSpawner == null)
            obstacleSpawner = FindObjectOfType<ObstacleSpawner>(true);
    }

    public void NextLevel()
    {
        gameStarted = true;
        pausedForCalibration = true;
        currentLevelIndex++;

        //if (currentLevelIndex >= levels.Length)
        if (currentLevelIndex >= Mathf.Min(SettingsData.levelCount, levels.Length))
        {
            Debug.Log("All levels complete!");

            ShowFinalCompletePanel();
            nextLevelTransitionInProgress = false;
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
        Debug.Log($"GameManager.RequestNextLevel called. currentLevel={currentLevelIndex + 1}, levelCompleteUI={(levelCompleteUI != null ? levelCompleteUI.name : "null")}, SettingsData.levelCount={SettingsData.levelCount}, levels={(levels != null ? levels.Length : 0)}");
        SetCalibrationOverlayPaused(true);

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
        else
            Debug.LogError("GameManager.RequestNextLevel could not show level complete UI because levelCompleteUI is null.");
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

        foreach (var receiver in GetAllFreeDropReceivers())
        {
            if (receiver.FreeDropContainer != null)
                uniqueRoots.Add(receiver.FreeDropContainer);
        }

        return uniqueRoots;
    }

    IEnumerable<HandCatch3D> GetAllHandCatchers()
    {
        foreach (var catcher in Resources.FindObjectsOfTypeAll<HandCatch3D>())
        {
            if (catcher == null) continue;
            if (!catcher.gameObject.scene.IsValid()) continue;
            yield return catcher;
        }
    }

    IEnumerable<FreeDropReceiver> GetAllFreeDropReceivers()
    {
        foreach (var receiver in Resources.FindObjectsOfTypeAll<FreeDropReceiver>())
        {
            if (receiver == null) continue;
            if (!receiver.gameObject.scene.IsValid()) continue;
            yield return receiver;
        }
    }

    void HidePlates()
    {
        if (platePlaceholderL != null) platePlaceholderL.SetActive(false);
        if (platePlaceholderR != null) platePlaceholderR.SetActive(false);
        if (whiteboardRoot != null) whiteboardRoot.SetActive(false);
    }

    void ShowPlates()
    {
        if (platePlaceholderL != null) platePlaceholderL.SetActive(true);
        if (platePlaceholderR != null) platePlaceholderR.SetActive(true);
    }

    void UpdateThemeVisuals()
    {
        if (whiteboardRoot != null)
        {
            bool showWhiteboard = currentMode == GameMode.Letters || currentMode == GameMode.Numbers;
            whiteboardRoot.SetActive(showWhiteboard);
        }
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

    public void PlayCorrectCatchSound()
    {
        if (correctCatchClip == null)
            return;

        if (uiAudioSource != null)
        {
            uiAudioSource.PlayOneShot(correctCatchClip);
            return;
        }

        AudioSource.PlayClipAtPoint(correctCatchClip, Camera.main != null ? Camera.main.transform.position : Vector3.zero);
    }

    public void ConfirmNextLevel()
    {
        if (nextLevelTransitionInProgress)
        {
            if (logGameStartDebug)
                Debug.Log("GameManager.ConfirmNextLevel ignored because a level transition is already in progress.");
            return;
        }

        nextLevelTransitionInProgress = true;

        // Researcher pressed Next Level
        NextLevel(); // your existing method that resets + ApplyLevel
    }

    public void ContinueAfterObstacleInstructions()
    {
        if (obstacleInstructionsPanel != null)
            obstacleInstructionsPanel.SetActive(false);

        obstacleInstructionsShown = true;
        waitingForObstacleInstructions = false;
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
        foreach (var receiver in GetAllFreeDropReceivers())
            receiver.ResetReceiverState();

        // 4) Reset score && Reset the burger progress container
        if (scoreManager != null) scoreManager.ResetScore();
        FindObjectOfType<BurgerProgressUI>()?.ResetProgress();

        // 5) Re-enable both hand colliders (they were disabled on StopGame)
        foreach (var catcher in GetAllHandCatchers())
        {
            foreach (var col in catcher.GetComponents<Collider>())
            {
                if (col != null) col.enabled = true;
            }
        }

        foreach (var receiver in GetAllFreeDropReceivers())
        {
            foreach (var col in receiver.GetComponents<Collider>())
            {
                if (col != null) col.enabled = true;
            }
        }

        // (Optional) small delay so the scene visually “breathes”
        yield return new WaitForSeconds(0.2f);

        if (burgerStackRoot != null)
            burgerStackRoot.SetActive(true);

        if (ShouldShowObstacleInstructionsForCurrentLevel())
        {
            waitingForObstacleInstructions = true;
            if (obstacleInstructionsPanel != null)
                obstacleInstructionsPanel.SetActive(true);

            yield return new WaitUntil(() => !waitingForObstacleInstructions);
        }

        // 6) Prepare the next level while calibration keeps spawning paused.
        ApplyLevel(currentLevelIndex);
        UpdateThemeVisuals();

        BodyPositionCalibrationManager calibration =
            FindObjectOfType<BodyPositionCalibrationManager>();
        bool waitingForCalibration =
            calibration != null && calibration.BeginNextLevelCalibration();

        if (!waitingForCalibration)
        {
            // Testing mode or a scene without calibration should not leave the
            // prepared level permanently paused.
            ShowPlates();
            ResumeAfterRecalibration();
        }

        nextLevelTransitionInProgress = false;
    }

    private bool ShouldShowObstacleInstructionsForCurrentLevel()
    {
        if (obstacleInstructionsPanel == null)
            return false;

        if (showObstacleInstructionsOnce && obstacleInstructionsShown)
            return false;

        LevelSettings runtimeSettings =
            SettingsData.GetLevelSettings(currentLevelIndex);
        if (runtimeSettings != null)
            return runtimeSettings.enableObstacles;

        return levels != null &&
            currentLevelIndex >= 0 &&
            currentLevelIndex < levels.Length &&
            levels[currentLevelIndex] != null &&
            levels[currentLevelIndex].enableObstacles;
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

    void SetCalibrationOverlayPaused(bool isPaused)
    {
        BodyPositionCalibrationManager calibration = FindObjectOfType<BodyPositionCalibrationManager>();
        if (calibration == null)
            return;

        calibration.SetRuntimeMonitoringPaused(isPaused);

        if (isPaused)
            calibration.HideCalibrationPanel();
    }
}
