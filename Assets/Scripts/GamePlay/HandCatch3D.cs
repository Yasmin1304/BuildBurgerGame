using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Catches falling "Ingredient" objects when they enter this trigger (e.g., the plate),
/// then stacks them under burgerStack. Supports two hands by sharing state via static fields.
/// </summary>
public class HandCatch3D : MonoBehaviour
{
    public Transform burgerStack;        // Parent transform where caught items will be placed (the burger stack root)
    public float stackHeight = 0.5f;     // How much higher each next ingredient is placed on Y (vertical spacing)
    public int baseSortingOrder = 0;     // Base render order so newer pieces can be forced on top
    public float depthStep = -0.01f;     // Small Z offset per ingredient to avoid depth-fighting (3D overlap issues)
    public ScoreManager scoreManager;    
    [SerializeField] private BadCatchFeedbackUI badCatchFeedbackUI;


    public float baseOffset = 0.1f;      // Initial Y offset from burgerStack origin before stacking starts

    // --- Shared state (static = shared by BOTH hands / both HandCatch3D instances) ---

    // How many ingredients have been stacked so far (used to compute the next position & sorting order)
    private static int stackCount = 0;

    // Tracks which *specific spawned objects* have already been stacked (prevents double-stacking the same clone)
    private static HashSet<int> stackedInstanceIds = new HashSet<int>();

    // Rule gate: we must place the bottom bun first before allowing other ingredients
    private static bool hasBottomBun = false;

    // When true: burger is finished; ignore any further catches
    private static bool burgerDone = false;

    //SupabaseEventManager
    [SerializeField] private SupabaseSessionEventInsert eventLogger;

    // --- Burger rules (Inspector) ---

    [Header("Burger Rules")]
    public string bottomBunName = "Ingredient_BreadDown"; // Prefab name (or part of it) for the bottom bun
    public string topBunName = "Ingredient_BreadUp";      // Prefab name (or part of it) for the top bun


    public IngredientSpawner spawner;  // Optional reference; we stop spawners on completion (we also search all spawners)

    /// <summary>
    /// Reset shared game state when the scene loads.
    /// Note: Because fields are static, without this, old state can sometimes persist between Play sessions.
    /// </summary>
    void Awake()
    {
        stackCount = 0;
        stackedInstanceIds.Clear();
        hasBottomBun = false;
        burgerDone = false;
        
    }

    /// <summary>
    /// Unity names spawned objects like "Ingredient_BreadDown(Clone)".
    /// This removes "(Clone)" so rule checks match the prefab name.
    /// </summary>
    string CleanName(string objName)
    {
        int i = objName.IndexOf("(Clone)");
        return i >= 0 ? objName.Substring(0, i) : objName;
    }

    /// <summary>
    /// Ends the game:
    /// - blocks any further catching
    /// - stops spawners
    /// - destroys any remaining falling ingredients
    /// - disables both hand catch colliders
    /// </summary>
    void StopGame()
    {
        Debug.Log("Burger completed! Game stopped.");

        // Block any further catches immediately (important if multiple colliders trigger in the same frame)
        burgerDone = true;

        // Stop every spawner in the scene (covers the case where you have more than one spawner)
        foreach (var s in FindObjectsOfType<IngredientSpawner>())
        {
            s.StopSpawning(); // your spawner's method that cancels InvokeRepeating
            s.enabled = false; // extra safety: disables the component so it can’t restart
        }

        foreach (var o in FindObjectsOfType<ObstacleSpawner>())
        {
            o.StopSpawning(); // your spawner's method that cancels InvokeRepeating
            o.enabled = false; // extra safety: disables the component so it can’t restart
        }

        // Remove any ingredients that are still falling / not part of the burger stack
        // DestroyRemainingIngredients();
        DestroyRemainingItems();

        // Disable all HandCatch3D colliders (both hands) so the plate can’t catch anymore items
        foreach (var catcher in FindObjectsOfType<HandCatch3D>())
        {
            var col = catcher.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
        
    }

    public static void ResetSharedState()
    {
        stackCount = 0;
        stackedInstanceIds.Clear();
        hasBottomBun = false;
        burgerDone = false;
    }

    /// <summary>
    /// Destroys all objects tagged "Ingredient" that are NOT already parented under burgerStack.
    /// This clears the scene from leftover falling objects after the burger is finished.
    /// </summary>
    // void DestroyRemainingIngredients()
    // {
    //     var all = GameObject.FindGameObjectsWithTag("Ingredient");

    //     foreach (var go in all)
    //     {
    //         if (go == null) continue;

    //         // If the ingredient is not already stacked under burgerStack, delete it
    //         if (!go.transform.IsChildOf(burgerStack))
    //             Destroy(go);
    //     }
    // }

    void DestroyRemainingItems()
    {
        var all = GameObject.FindGameObjectsWithTag("Ingredient");

        foreach (var go in all)
        {
            if (go == null) continue;

            if (burgerStack != null && !go.transform.IsChildOf(burgerStack))
                Destroy(go);
        }
    }

    void DestroyRemainingObstacles()
    {
        var all = GameObject.FindGameObjectsWithTag("Obstacle");

        foreach (var go in all)
        {
            if (go == null) continue;

            // If the ingredient is not already stacked under burgerStack, delete it
            if (!go.transform.IsChildOf(burgerStack))
                Destroy(go);
        }
    }

    /// Helper function to check if all ingredients have fallen already and 
    /// no new ingredients will fall.
    // bool AllIngredientsUsedUp()
    // {
    //     if (spawner == null) spawner = FindObjectOfType<IngredientSpawner>();
    //     if (spawner == null) return false;

    //     // Must have spawned everything
    //     if (!spawner.IsFinished) return false;

    //     // And nothing is still falling (Ingredient tag but not stacked)
    //     var all = GameObject.FindGameObjectsWithTag("Ingredient");
    //     foreach (var go in all)
    //     {
    //         if (go == null) continue;
    //         if (!go.transform.IsChildOf(burgerStack))
    //             return false;
    //     }
    //     return true;
    // }

    bool AllIngredientsUsedUp()
    {
        if (spawner == null) spawner = FindObjectOfType<IngredientSpawner>();
        bool complete = LevelItemResolutionTracker.TryRequestCompletion(spawner);
        if (!complete && spawner != null && spawner.IsFinished)
            Debug.Log($"HandCatch3D completion not ready: {LevelItemResolutionTracker.GetDebugStatus(spawner)}");

        return complete;
    }

    /// Helper function to determine if the falling ingredient fell on the left or right 
    /// side of the screen.
    string GetScreenSide(Transform target)
    {
        if (Camera.main == null || target == null)
            return "unknown";

        Vector3 screenPos = Camera.main.WorldToScreenPoint(target.position);

        return screenPos.x < Screen.width * 0.5f ? "left" : "right";
    }

    /// <summary>
    /// Trigger hit when something enters the plate/hand trigger.
    /// We:
    /// - validate it's an ingredient
    /// - apply burger rules (bottom bun first, only one bottom bun)
    /// - freeze physics and parent it under burgerStack
    /// - position + sort it
    /// - if it's the top bun -> end game
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        GameManager gm = FindObjectOfType<GameManager>();
        GameMode mode = gm != null ? gm.currentMode : GameMode.Burger;

        // Only react to falling ingredients
        //if (!other.CompareTag("Ingredient")) return;
        if (other.CompareTag("Obstacle"))
        {
            Debug.Log("Obstacle caught!");
            badCatchFeedbackUI?.Show();

            if (scoreManager != null)
            {
                scoreManager.FlashColor(Color.red, 0.4f);
                scoreManager.AddPenalty(5); // −5 points
                scoreManager.FlashColor(Color.red);
            }
            if (eventLogger != null)
            {
                Transform obstacle = other.attachedRigidbody != null
                    ? other.attachedRigidbody.transform
                    : other.transform;

                string obstacleName = CleanName(obstacle.name);
                string obstacleSide = GetScreenSide(obstacle);

                eventLogger.LogEvent(obstacleName, "hit", obstacleSide);

                // string obstacleSide = GetScreenSide(other.transform);
                // Debug.Log("Logging obstacle miss, side = " + obstacleSide);
                // eventLogger.LogEvent("Obstacle", "hit", obstacleSide);
            }
            else
            {
                Debug.LogError("eventLogger is NULL in obstacle block");
            }
            Destroy(other.gameObject);
            return;
        }

        if (mode != GameMode.Burger) return;

        // Below is the code for burger game handling --------> 
        // If burger is already completed, ignore everything
        if (burgerDone) return;

        // If the collider belongs to a Rigidbody, "attachedRigidbody" returns it.
        // This ensures we move the whole ingredient object (not a child collider).
        Transform caught = other.attachedRigidbody != null
            ? other.attachedRigidbody.transform
            : other.transform;

        // Prevent stacking the same physical object twice (in case it triggers again)
        int id = caught.GetInstanceID();
        if (stackedInstanceIds.Contains(id)) return;

        // Clean the name for rule checks (remove "(Clone)")
        string ingredientName = CleanName(caught.name);

        // --- RULE 1: Must start with bottom bun ---
        // If we haven't placed the bottom bun yet, reject anything else.
        if (!hasBottomBun && !ingredientName.Contains(bottomBunName))
        {
            Debug.Log($"Rejected {ingredientName}. First must be {bottomBunName}");
            return; // let it keep falling (you could Destroy it instead if you prefer)
        }

        // ---RULE 2: allow exactly ONE bottom bun total.
        // - If we catch a bottom bun after one is already placed, reject it.
        // - Otherwise, mark bottom bun as placed.
        if (ingredientName.Contains(bottomBunName))
        {
            if (hasBottomBun)
            {
                Debug.Log("Rejected extra bottom bun");
                return;
            }
            hasBottomBun = true;

            if (spawner == null)
                spawner = FindObjectOfType<IngredientSpawner>();

            spawner?.NotifyBottomBunCaught();
        }

        // --- Freeze physics so it stops falling and becomes part of the stack ---
        var rb = caught.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // Kinematic means physics won't move it anymore (we control its position manually)
            rb.isKinematic = true;
        }

        // --- Stack it under burgerStack ---
        // Parent it to burgerStack so all stacked pieces move together
        string side = GetScreenSide(caught);
        caught.SetParent(burgerStack, false);

        // Reset rotation so it sits nicely aligned
        caught.localRotation = Quaternion.identity;

        // Place it at the correct stack height:
        // - X = 0 so it centers on stack
        // - Y increases with stackCount
        // - Z slightly moves forward/backward to avoid depth ties
        caught.localPosition = new Vector3(
            0f,
            baseOffset + stackCount * stackHeight,
            stackCount * depthStep
        );

        // --- Ensure sprites render on top in the same order they were stacked ---
        // If the ingredient has multiple SpriteRenderers, update all of them.
        foreach (var sr in caught.GetComponentsInChildren<SpriteRenderer>(true))
            sr.sortingOrder = baseSortingOrder + stackCount;

        // Keep newest stacked ingredient last in hierarchy (sometimes helps with renderers/UI ordering)
        caught.SetAsLastSibling();

        // Mark this object as stacked so it can’t be stacked again
        stackedInstanceIds.Add(id);
        LevelItemResolutionTracker.TryResolve(caught.gameObject);
        FindObjectOfType<GameManager>()?.PlayCorrectCatchSound();

        // Increase stackCount for next ingredient placement
        stackCount++;

        //Save hits to Database 
        if (eventLogger != null)
        {
            Debug.Log("Logging ingredient hit: " + ingredientName + ", side = " + side);
            eventLogger.LogEvent(ingredientName, "hit", side);
            LevelSessionTracker.Instance?.RegisterHit(side);
        }
        else
        {
            Debug.LogError("eventLogger is NULL in ingredient block");
        }

        if (scoreManager != null && !ingredientName.Contains(topBunName) && !ingredientName.Contains(bottomBunName))
            scoreManager.AddIngredientScore();
            //Fill burger progress container
            FindObjectOfType<BurgerProgressUI>()?.AddIngredient();


        // --- RULE 2: If top bun was stacked, complete burger and stop game ---
        if (ingredientName.Contains(topBunName) && AllIngredientsUsedUp())
        {
            StopGame();
            FindObjectOfType<GameManager>()?.RequestNextLevel();
            FindObjectOfType<SupabaseSessionUpdate>()?.UpdateCurrentSession();
        }

        // --- RULE 3: Even if the top bun was not stacked, we stop game if no more ingredients can be spawned ---
        if (AllIngredientsUsedUp())
        {
            StopGame();
            FindObjectOfType<GameManager>()?.RequestNextLevel();
            FindObjectOfType<SupabaseSessionUpdate>()?.UpdateCurrentSession();
        }
    }
}
