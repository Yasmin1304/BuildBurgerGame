using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RTLTMPro;

/// <summary>
/// Controls the body-position calibration flow before gameplay starts.
///
/// The flow is:
/// 1. Show a calibration UI shape.
/// 2. Read the child's body anchors from BodyPoseProvider.
/// 3. Convert normalized pose coordinates into screen pixels.
/// 4. Check that the body is centered and roughly the right size.
/// 5. Require the child to hold that position for a short time.
/// 6. Hide calibration and start the countdown/game.
///
/// After the game starts, this script can also monitor whether the body anchors
/// are still visible and briefly pause gameplay for a position correction.
/// </summary>
public class BodyPositionCalibrationManager : MonoBehaviour
{
    [System.Serializable]
    private class CalibrationInstructionAudio
    {
        public AudioClip englishClip;
        public AudioClip arabicClip;
        public AudioClip burgerEnglishClip;
        public AudioClip burgerArabicClip;
        public AudioClip lettersEnglishClip;
        public AudioClip lettersArabicClip;
        public AudioClip numbersEnglishClip;
        public AudioClip numbersArabicClip;

        public AudioClip GetClip(GameMode gameMode, bool useArabic)
        {
            AudioClip themedClip;
            switch (gameMode)
            {
                case GameMode.Letters:
                    themedClip = useArabic ? lettersArabicClip : lettersEnglishClip;
                    break;

                case GameMode.Numbers:
                    themedClip = useArabic ? numbersArabicClip : numbersEnglishClip;
                    break;

                case GameMode.Burger:
                default:
                    themedClip = useArabic ? burgerArabicClip : burgerEnglishClip;
                    break;
            }

            if (themedClip != null)
                return themedClip;

            return useArabic ? arabicClip : englishClip;
        }
    }

    private enum CalibrationInstruction
    {
        StandInsideShape,
        StandWhereVisible,
        GreatStayThere,
        MoveRight,
        MoveLeft,
        MoveCloser,
        MoveBack,
        MoveBackFeetVisible,
        MoveBackBodyVisible,
        ShowHeadAndShoulders
    }

    [Header("UI")]
    // Root object for the calibration overlay.
    [SerializeField] private GameObject calibrationPanel;

    // The UI rectangle/shape the child should stand inside.
    // Its screen-space size is also used as the target body size.
    [SerializeField] private RectTransform bodyTargetOutline;

    // Text shown to the child: "Move left", "Move back", "Great! Stay there", etc.
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private Color instructionTextColor = Color.white;
    [SerializeField] private Color instructionOutlineColor = Color.black;
    [SerializeField, Range(0f, 1f)] private float instructionOutlineWidth = 0.32f;
    [SerializeField] private bool fitInstructionTextToScreen = true;
    [SerializeField] private float instructionHorizontalMargin = 120f;
    [SerializeField] private float instructionTextHeight = 180f;
    [SerializeField] private float instructionMinFontSize = 44f;
    [SerializeField] private float instructionMaxFontSize = 160f;
    [SerializeField] private Color instructionShadowColor = new Color(0f, 0f, 0f, 0.9f);
    [SerializeField] private Vector2 instructionShadowDistance = new Vector2(2f, -2f);

    [Header("Instruction Narration")]
    [SerializeField] private AudioSource instructionNarrationSource;
    [SerializeField] private bool playInstructionNarration = true;
    [SerializeField] private float instructionNarrationRepeatDelay = 3f;
    [SerializeField] private CalibrationInstructionAudio standInsideShapeAudio;
    [SerializeField] private CalibrationInstructionAudio standWhereVisibleAudio;
    [SerializeField] private CalibrationInstructionAudio greatStayThereAudio;
    [SerializeField] private CalibrationInstructionAudio moveRightAudio;
    [SerializeField] private CalibrationInstructionAudio moveLeftAudio;
    [SerializeField] private CalibrationInstructionAudio moveCloserAudio;
    [SerializeField] private CalibrationInstructionAudio moveBackAudio;
    [SerializeField] private CalibrationInstructionAudio moveBackFeetVisibleAudio;
    [SerializeField] private CalibrationInstructionAudio moveBackBodyVisibleAudio;
    [SerializeField] private CalibrationInstructionAudio showHeadAndShouldersAudio;

    // Optional radial/filled image that fills while the child holds the correct position.
    [SerializeField] private Image progressFill;

    [Header("Game Flow")]
    // Optional root for gameplay systems. Usually this should stay active,
    // so controlGameRootVisibility is false by default.
    [SerializeField] private GameObject gameRoot;
    [SerializeField] private GameObject countdownPanel;
    [SerializeField] private GameStartCountdownImages countdown;
    [SerializeField] private bool controlGameRootVisibility = false;
    [SerializeField] private GameObject[] hideDuringCalibration;

    [Header("Pose Tracking")]
    // Source of body landmarks. In this project it is YoloBodyPoseProvider.
    [SerializeField] private BodyPoseProvider poseProvider;

    [Header("Calibration Settings")]
    [SerializeField] private bool skipCalibrationForTesting = false;

    // How long the child must remain in the correct position before calibration passes.
    [SerializeField] private float requiredHoldTime = 2.5f;

    // Body height is compared against the UI target height.
    // Too small usually means the child is too far away.
    // Too large usually means the child is too close.
    [SerializeField] private float minBodyHeightRatio = 0.8f;
    [SerializeField] private float maxBodyHeightRatio = 1.25f;

    // Horizontal tolerance as a fraction of target width.
    // We intentionally do not care much about vertical movement.
    [SerializeField] private float centerToleranceRatio = 0.25f;
    [SerializeField] private bool logDebugInfo;

    [Header("Runtime Monitoring")]
    // If true, the game asks for correction when important body anchors disappear.
    [SerializeField] private bool monitorBodyAnchorsAfterCalibration = true;

    // Optional stricter distance check after calibration.
    // Currently false because anchor visibility is a better signal for this game.
    [SerializeField] private bool monitorDistanceAfterCalibration = false;
    [SerializeField] private float distanceOutOfRangeGraceTime = 1.25f;
    [SerializeField] private float runtimeMinBodyHeightRatio = 0.75f;
    [SerializeField] private float runtimeMaxBodyHeightRatio = 1.2f;

    private float holdTimer;
    private float distanceOutOfRangeTimer;
    private float calibratedBodyHeight;
    private bool calibrationCompleted;
    private bool countdownRunning;
    private bool hasStartedGame;
    private bool runtimeCorrectionActive;
    private bool runtimeMonitoringPaused;
    private float lastDebugLogTime;
    private CalibrationInstruction lastNarratedInstruction;
    private bool hasNarratedInstruction;
    private float lastNarrationTime = -999f;

    private void Start()
    {
        // Allow scene wiring in the Inspector, but auto-find as a backup.
        if (poseProvider == null)
            poseProvider = FindObjectOfType<BodyPoseProvider>();

        if (countdown == null)
            countdown = FindObjectOfType<GameStartCountdownImages>();

        if (countdown != null)
            countdown.CountdownCompleted += HandleCountdownCompleted;

        ApplyInstructionTextStyle();
        EnsureInstructionNarrationSource();

        if (skipCalibrationForTesting)
            SkipCalibrationForTesting();
        else
            StartCalibration();
    }

    private void OnDestroy()
    {
        StopInstructionNarration();

        if (countdown != null)
            countdown.CountdownCompleted -= HandleCountdownCompleted;
    }

    public void StartCalibration()
    {
        if (skipCalibrationForTesting)
        {
            SkipCalibrationForTesting();
            return;
        }

        GetYoloPoseProvider()?.ResetMainPlayerLock();

        // This is the initial calibration before the first countdown.
        calibrationCompleted = false;
        countdownRunning = false;
        runtimeCorrectionActive = false;
        runtimeMonitoringPaused = false;
        holdTimer = 0f;
        distanceOutOfRangeTimer = 0f;

        if (calibrationPanel != null)
            calibrationPanel.SetActive(true);

        if (controlGameRootVisibility && gameRoot != null)
            gameRoot.SetActive(false);

        SetHiddenObjectsActive(false);

        if (progressFill != null)
            progressFill.fillAmount = 0f;

        SetInstruction(CalibrationInstruction.StandInsideShape);
    }

    /// <summary>
    /// Requires the already-selected player to pass the positioning check again
    /// before a newly prepared level is allowed to start spawning.
    /// </summary>
    public bool BeginNextLevelCalibration()
    {
        if (skipCalibrationForTesting)
            return false;

        // The previous ByteTrack ID may have expired while the level-complete
        // screen was open. Reacquire the visible player during this calibration
        // instead of waiting forever for an old track ID.
        GetYoloPoseProvider()?.ResetMainPlayerLock();

        calibrationCompleted = false;
        countdownRunning = false;
        runtimeCorrectionActive = true;
        runtimeMonitoringPaused = false;
        holdTimer = 0f;
        distanceOutOfRangeTimer = 0f;

        if (calibrationPanel != null)
            calibrationPanel.SetActive(true);

        SetHiddenObjectsActive(false);

        if (progressFill != null)
            progressFill.fillAmount = 0f;

        SetInstruction(CalibrationInstruction.StandInsideShape);
        return true;
    }

    private void Update()
    {
        if (skipCalibrationForTesting)
        {
            // The Inspector value may be enabled while Play mode is already
            // running. Complete the skip flow instead of only stopping the
            // calibration checks and leaving gameplay hidden.
            if (!calibrationCompleted && !countdownRunning)
                SkipCalibrationForTesting();

            return;
        }

        // Once initial calibration succeeds, Update switches to runtime monitoring.
        if (calibrationCompleted)
        {
            MonitorRuntimeDistance();
            return;
        }

        // TryGetPose is strict: it only succeeds when required anchors are visible.
        if (!TryGetChildBodyRect(out Rect childBodyRect))
        {
            holdTimer = 0f;

            if (progressFill != null)
                progressFill.fillAmount = 0f;

            SetInstruction(CalibrationInstruction.StandWhereVisible);
            return;
        }

        Rect targetRect = GetScreenRect(bodyTargetOutline);

        // Compare the detected body rectangle with the UI target rectangle.
        bool isCorrect = IsChildPositionCorrect(childBodyRect, targetRect);
        LogCalibrationDebug(childBodyRect, targetRect, isCorrect);

        if (isCorrect)
        {
            // The child is in the right place. Fill the progress circle.
            holdTimer += Time.deltaTime;

            if (progressFill != null)
                progressFill.fillAmount = Mathf.Clamp01(holdTimer / requiredHoldTime);

            SetInstruction(CalibrationInstruction.GreatStayThere);

            if (holdTimer >= requiredHoldTime)
            {
                // Store the calibrated height in case runtime distance monitoring is enabled.
                calibratedBodyHeight = childBodyRect.height;

                if (runtimeCorrectionActive)
                    CompleteRuntimeCorrection();
                else
                    CompleteCalibration();
            }
        }
        else
        {
            // Any wrong frame resets the hold timer. This makes the child stay
            // stable in the target area instead of briefly passing through it.
            holdTimer = 0f;

            if (progressFill != null)
                progressFill.fillAmount = 0f;

            SetInstruction(GetInstruction(childBodyRect, targetRect));
        }
    }

    private bool TryGetChildBodyRect(out Rect bodyRect)
    {
        bodyRect = default;

        if (poseProvider == null || !poseProvider.TryGetPose(out BodyPoseLandmarks pose))
            return false;

        // Pose landmarks are normalized camera coordinates.
        // Convert the selected anchors into screen pixels, then make a bounding box.
        Vector2 nose = ToScreenPoint(pose.Nose);
        Vector2 leftShoulder = ToScreenPoint(pose.LeftShoulder);
        Vector2 rightShoulder = ToScreenPoint(pose.RightShoulder);
        Vector2 leftHip = ToScreenPoint(pose.LeftHip);
        Vector2 rightHip = ToScreenPoint(pose.RightHip);
        Vector2 leftAnkle = ToScreenPoint(pose.LeftAnkle);
        Vector2 rightAnkle = ToScreenPoint(pose.RightAnkle);

        float minX = Mathf.Min(
            nose.x,
            leftShoulder.x,
            rightShoulder.x,
            leftHip.x,
            rightHip.x,
            leftAnkle.x,
            rightAnkle.x
        );

        float maxX = Mathf.Max(
            nose.x,
            leftShoulder.x,
            rightShoulder.x,
            leftHip.x,
            rightHip.x,
            leftAnkle.x,
            rightAnkle.x
        );

        float minY = Mathf.Min(
            nose.y,
            leftShoulder.y,
            rightShoulder.y,
            leftHip.y,
            rightHip.y,
            leftAnkle.y,
            rightAnkle.y
        );

        float maxY = Mathf.Max(
            nose.y,
            leftShoulder.y,
            rightShoulder.y,
            leftHip.y,
            rightHip.y,
            leftAnkle.y,
            rightAnkle.y
        );

        bodyRect = Rect.MinMaxRect(minX, minY, maxX, maxY);
        return true;
    }

    /// <summary>
    /// Checks whether the child is horizontally centered and at a good distance.
    /// Distance is estimated from body height in the camera image.
    /// </summary>
    private bool IsChildPositionCorrect(Rect childRect, Rect targetRect)
    {
        Vector2 childCenter = childRect.center;
        Vector2 targetCenter = targetRect.center;

        float xTolerance = targetRect.width * centerToleranceRatio;

        bool centeredX = Mathf.Abs(childCenter.x - targetCenter.x) <= xTolerance;

        float childHeight = childRect.height;
        float targetHeight = targetRect.height;

        bool correctDistance =
            childHeight >= targetHeight * minBodyHeightRatio &&
            childHeight <= targetHeight * maxBodyHeightRatio;

        return centeredX && correctDistance;
    }

    /// <summary>
    /// Converts the current mismatch into a child-friendly instruction.
    /// We only tell the child left/right/closer/back because those are the actions
    /// that matter for camera quality.
    /// </summary>
    private CalibrationInstruction GetInstruction(Rect childRect, Rect targetRect)
    {
        Vector2 childCenter = childRect.center;
        Vector2 targetCenter = targetRect.center;

        float xTolerance = targetRect.width * centerToleranceRatio;

        float xDiff = childCenter.x - targetCenter.x;

        if (Mathf.Abs(xDiff) > xTolerance)
            return xDiff < 0
                ? CalibrationInstruction.MoveRight
                : CalibrationInstruction.MoveLeft;

        if (childRect.height < targetRect.height * minBodyHeightRatio)
            return CalibrationInstruction.MoveCloser;

        if (childRect.height > targetRect.height * maxBodyHeightRatio)
            return CalibrationInstruction.MoveBack;

        return CalibrationInstruction.StandInsideShape;
    }

    /// <summary>
    /// Converts a RectTransform from the UI canvas into a screen-pixel Rect.
    /// This lets us compare the UI target shape with tracked screen points.
    /// </summary>
    private Rect GetScreenRect(RectTransform rectTransform)
    {
        if (rectTransform == null)
            return default;

        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        Camera uiCamera = null;
        Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;

        Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[0]);
        Vector2 topRight = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[2]);

        return Rect.MinMaxRect(
            bottomLeft.x,
            bottomLeft.y,
            topRight.x,
            topRight.y
        );
    }

    /// <summary>
    /// Converts normalized camera coordinates into Unity screen pixels.
    /// Camera Y is top-to-bottom, but Unity screen Y is bottom-to-top,
    /// so Y must be flipped with 1 - Y.
    /// </summary>
    private Vector2 ToScreenPoint(BodyLandmark landmark)
    {
        return poseProvider != null
            ? poseProvider.ToScreenPoint(landmark)
            : new Vector2(
                landmark.X * Screen.width,
                (1f - landmark.Y) * Screen.height
            );
    }

    private void CompleteCalibration()
    {
        // Initial calibration passed. Hide the overlay and start the 3,2,1 countdown.
        GetYoloPoseProvider()?.RequestMainPlayerLock();
        StopInstructionNarration();

        calibrationCompleted = true;
        countdownRunning = true;
        distanceOutOfRangeTimer = 0f;

        if (calibrationPanel != null)
            calibrationPanel.SetActive(false);

        SetHiddenObjectsActive(true);

        if (countdown != null)
        {
            countdown.BeginCountdown();
            return;
        }

        if (countdownPanel != null)
            countdownPanel.SetActive(true);

        Invoke(nameof(StartGameAfterCountdown), 3f);
    }

    private void SkipCalibrationForTesting()
    {
        GetYoloPoseProvider()?.RequestMainPlayerLock();
        StopInstructionNarration();

        calibrationCompleted = true;
        countdownRunning = true;
        runtimeCorrectionActive = false;
        runtimeMonitoringPaused = true;
        holdTimer = 0f;
        distanceOutOfRangeTimer = 0f;

        if (calibrationPanel != null)
            calibrationPanel.SetActive(false);

        SetHiddenObjectsActive(true);

        if (progressFill != null)
            progressFill.fillAmount = 0f;

        if (countdown != null)
        {
            countdown.BeginCountdown();
            return;
        }

        if (countdownPanel != null)
            countdownPanel.SetActive(true);

        Invoke(nameof(StartGameAfterCountdown), 3f);
    }

    private void StartGameAfterCountdown()
    {
        // Fallback path if the image countdown controller is not assigned.
        if (countdownPanel != null)
            countdownPanel.SetActive(false);

        if (controlGameRootVisibility && gameRoot != null)
            gameRoot.SetActive(true);

        SetHiddenObjectsActive(true);

        FindObjectOfType<GameManager>()?.BeginGame();
        hasStartedGame = true;
        countdownRunning = false;
    }

    private void HandleCountdownCompleted()
    {
        // Called by GameStartCountdownImages when "Go" finishes.
        hasStartedGame = true;
        countdownRunning = false;
    }

    private void SetInstruction(CalibrationInstruction instruction)
    {
        bool useArabic = LanguageManager.Instance != null &&
            LanguageManager.Instance.CurrentLanguage == AppLanguage.Arabic;

        if (instructionText != null)
        {
            instructionText.isRightToLeftText = useArabic;
            instructionText.alignment = TextAlignmentOptions.Midline;

            string message = GetInstructionMessage(instruction, useArabic);
            instructionText.text = useArabic ? ShapeArabicText(message) : message;
            instructionText.SetAllDirty();
            instructionText.ForceMeshUpdate();
        }

        PlayInstructionNarration(instruction, useArabic);
    }

    private string GetInstructionMessage(CalibrationInstruction instruction, bool useArabic)
    {
        if (useArabic)
        {
            switch (instruction)
            {
                case CalibrationInstruction.StandWhereVisible:
                    return "قف أمام الكاميرا حتى نراك بوضوح";

                case CalibrationInstruction.GreatStayThere:
                    return "ممتاز! ابقَ ثابتًا هنا";

                case CalibrationInstruction.MoveRight:
                    return "تحرّك خطوة صغيرة إلى اليمين";

                case CalibrationInstruction.MoveLeft:
                    return "تحرّك خطوة صغيرة إلى اليسار";

                case CalibrationInstruction.MoveCloser:
                    return "اقترب خطوة صغيرة من الكاميرا";

                case CalibrationInstruction.MoveBack:
                    return "ارجع خطوة صغيرة إلى الخلف";

                case CalibrationInstruction.MoveBackFeetVisible:
                    return "ارجع خطوة صغيرة حتى نرى قدميك";

                case CalibrationInstruction.MoveBackBodyVisible:
                    return "ارجع خطوة صغيرة حتى نرى جسمك كاملًا";

                case CalibrationInstruction.ShowHeadAndShoulders:
                    return "قف أمام الكاميرا حتى نرى رأسك وكتفيك";

                case CalibrationInstruction.StandInsideShape:
                default:
                    return "قف داخل الشكل";
            }
        }

        switch (instruction)
        {
            case CalibrationInstruction.StandWhereVisible:
                return "Stand in front of the camera so we can see you";

            case CalibrationInstruction.GreatStayThere:
                return "Great! Stay still right there";

            case CalibrationInstruction.MoveRight:
                return "Take one small step to the right";

            case CalibrationInstruction.MoveLeft:
                return "Take one small step to the left";

            case CalibrationInstruction.MoveCloser:
                return "Take one small step closer to the camera";

            case CalibrationInstruction.MoveBack:
                return "Take one small step back";

            case CalibrationInstruction.MoveBackFeetVisible:
                return "Take one small step back so we can see your feet";

            case CalibrationInstruction.MoveBackBodyVisible:
                return "Take one small step back so we can see your whole body";

            case CalibrationInstruction.ShowHeadAndShoulders:
                return "Stand in front of the camera so we can see your head and shoulders";

            case CalibrationInstruction.StandInsideShape:
            default:
                return "Stand inside the shape";
        }
    }

    private static string ShapeArabicText(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        FastStringBuilder output = new FastStringBuilder(Mathf.Max(RTLSupport.DefaultBufferSize, value.Length * 2));
        RTLSupport.FixText(value, output, true, false, true, true);
        return output.ToString();
    }

    private void PlayInstructionNarration(CalibrationInstruction instruction, bool useArabic)
    {
        if (!playInstructionNarration)
            return;

        bool isRepeatedInstruction = hasNarratedInstruction && lastNarratedInstruction == instruction;
        if (isRepeatedInstruction && Time.unscaledTime - lastNarrationTime < instructionNarrationRepeatDelay)
            return;

        AudioClip clip = GetInstructionAudio(instruction, useArabic);
        if (clip == null)
            return;

        EnsureInstructionNarrationSource();

        if (instructionNarrationSource == null)
            return;

        instructionNarrationSource.Stop();
        instructionNarrationSource.clip = clip;
        instructionNarrationSource.Play();

        lastNarratedInstruction = instruction;
        hasNarratedInstruction = true;
        lastNarrationTime = Time.unscaledTime;
    }

    private AudioClip GetInstructionAudio(CalibrationInstruction instruction, bool useArabic)
    {
        CalibrationInstructionAudio instructionAudio;
        switch (instruction)
        {
            case CalibrationInstruction.StandWhereVisible:
                instructionAudio = standWhereVisibleAudio;
                break;

            case CalibrationInstruction.GreatStayThere:
                instructionAudio = greatStayThereAudio;
                break;

            case CalibrationInstruction.MoveRight:
                instructionAudio = moveRightAudio;
                break;

            case CalibrationInstruction.MoveLeft:
                instructionAudio = moveLeftAudio;
                break;

            case CalibrationInstruction.MoveCloser:
                instructionAudio = moveCloserAudio;
                break;

            case CalibrationInstruction.MoveBack:
                instructionAudio = moveBackAudio;
                break;

            case CalibrationInstruction.MoveBackFeetVisible:
                instructionAudio = moveBackFeetVisibleAudio;
                break;

            case CalibrationInstruction.MoveBackBodyVisible:
                instructionAudio = moveBackBodyVisibleAudio;
                break;

            case CalibrationInstruction.ShowHeadAndShoulders:
                instructionAudio = showHeadAndShouldersAudio;
                break;

            case CalibrationInstruction.StandInsideShape:
            default:
                instructionAudio = standInsideShapeAudio;
                break;
        }

        return instructionAudio != null
            ? instructionAudio.GetClip(SessionData.SelectedGameMode, useArabic)
            : null;
    }

    private void StopInstructionNarration()
    {
        if (instructionNarrationSource == null)
            return;

        instructionNarrationSource.Stop();
        instructionNarrationSource.clip = null;
        hasNarratedInstruction = false;
    }

    private void EnsureInstructionNarrationSource()
    {
        if (instructionNarrationSource != null)
        {
            instructionNarrationSource.playOnAwake = false;
            return;
        }

        instructionNarrationSource = GetComponent<AudioSource>();
        if (instructionNarrationSource == null)
            instructionNarrationSource = gameObject.AddComponent<AudioSource>();

        instructionNarrationSource.playOnAwake = false;
    }

    private void ApplyInstructionTextStyle()
    {
        if (instructionText == null)
            return;

        instructionText.color = instructionTextColor;
        instructionText.outlineColor = instructionOutlineColor;
        instructionText.outlineWidth = instructionOutlineWidth;
        instructionText.enableAutoSizing = true;
        instructionText.fontSizeMin = instructionMinFontSize;
        instructionText.fontSizeMax = instructionMaxFontSize;
        instructionText.textWrappingMode = TextWrappingModes.Normal;
        instructionText.overflowMode = TextOverflowModes.Overflow;
        instructionText.SetMaterialDirty();

        if (fitInstructionTextToScreen &&
            instructionText.TryGetComponent(out RectTransform rectTransform))
        {
            rectTransform.anchorMin = new Vector2(0f, rectTransform.anchorMin.y);
            rectTransform.anchorMax = new Vector2(1f, rectTransform.anchorMax.y);
            rectTransform.pivot = new Vector2(0.5f, rectTransform.pivot.y);
            rectTransform.anchoredPosition = new Vector2(0f, rectTransform.anchoredPosition.y);
            rectTransform.sizeDelta = new Vector2(
                -instructionHorizontalMargin * 2f,
                Mathf.Max(rectTransform.sizeDelta.y, instructionTextHeight)
            );
        }

        Shadow shadow = instructionText.GetComponent<Shadow>();
        if (shadow == null)
            shadow = instructionText.gameObject.AddComponent<Shadow>();

        shadow.effectColor = instructionShadowColor;
        shadow.effectDistance = instructionShadowDistance;
        shadow.useGraphicAlpha = false;
    }

    private void LogCalibrationDebug(Rect childRect, Rect targetRect, bool isCorrect)
    {
        if (!logDebugInfo || Time.unscaledTime - lastDebugLogTime < 1f)
            return;

        lastDebugLogTime = Time.unscaledTime;

        Debug.Log(
            "BodyPositionCalibrationManager: " +
            $"child center={childRect.center}, child size={childRect.size}, " +
            $"target center={targetRect.center}, target size={targetRect.size}, " +
            $"correct={isCorrect}"
        );
    }

    private void SetHiddenObjectsActive(bool isActive)
    {
        // Used for gameplay-only visuals such as baskets during calibration.
        // The camera preview and tracking providers must remain active.
        if (hideDuringCalibration == null)
            return;

        foreach (GameObject obj in hideDuringCalibration)
        {
            if (obj != null)
                obj.SetActive(isActive);
        }
    }

    private void MonitorRuntimeDistance()
    {
        // Do not interrupt the countdown or level-complete UI.
        if (countdownRunning || runtimeMonitoringPaused)
            return;

        if (!TryGetChildBodyRect(out Rect childBodyRect))
        {
            // If full-body anchors are missing after gameplay starts, ask for a
            // short correction instead of restarting the whole level.
            if (monitorBodyAnchorsAfterCalibration)
                AccumulateRuntimeRecalibration(GetRuntimeAnchorInstruction());
            else
                distanceOutOfRangeTimer = 0f;

            return;
        }

        if (monitorBodyAnchorsAfterCalibration && !monitorDistanceAfterCalibration)
        {
            // Anchor visibility is good and distance monitoring is disabled,
            // so runtime position is acceptable.
            distanceOutOfRangeTimer = 0f;
            return;
        }

        if (!monitorDistanceAfterCalibration)
            return;

        if (calibratedBodyHeight <= 0f)
            calibratedBodyHeight = childBodyRect.height;

        // Optional distance check: compare current body height to calibrated height.
        float minHeight = calibratedBodyHeight * runtimeMinBodyHeightRatio;
        float maxHeight = calibratedBodyHeight * runtimeMaxBodyHeightRatio;

        LogRuntimeDebug(childBodyRect.height, minHeight, maxHeight);

        if (childBodyRect.height > maxHeight)
        {
            AccumulateRuntimeRecalibration(CalibrationInstruction.MoveBack);
            return;
        }

        if (childBodyRect.height < minHeight)
        {
            AccumulateRuntimeRecalibration(CalibrationInstruction.MoveCloser);
            return;
        }

        distanceOutOfRangeTimer = 0f;
    }

    private void AccumulateRuntimeRecalibration(CalibrationInstruction instruction)
    {
        // Require the problem to persist for a short grace time so we do not
        // interrupt gameplay because of one noisy pose frame.
        distanceOutOfRangeTimer += Time.deltaTime;

        if (distanceOutOfRangeTimer < distanceOutOfRangeGraceTime)
            return;

        distanceOutOfRangeTimer = 0f;
        TriggerRecalibration(instruction);
    }

    private void TriggerRecalibration(CalibrationInstruction instruction)
    {
        // During gameplay this is a correction overlay, not a full game restart.
        if (hasStartedGame)
        {
            FindObjectOfType<GameManager>()?.PauseForRecalibration();
            runtimeCorrectionActive = true;
        }

        calibrationCompleted = false;
        countdownRunning = false;
        holdTimer = 0f;
        GetYoloPoseProvider()?.ResetMainPlayerLock();

        if (progressFill != null)
            progressFill.fillAmount = 0f;

        if (calibrationPanel != null)
            calibrationPanel.SetActive(true);

        SetHiddenObjectsActive(false);
        SetInstruction(instruction);
    }

    private void CompleteRuntimeCorrection()
    {
        // The child corrected their position while the level was paused.
        // Resume the same level rather than starting from countdown again.
        GetYoloPoseProvider()?.RequestMainPlayerLock();
        StopInstructionNarration();

        calibrationCompleted = true;
        runtimeCorrectionActive = false;
        distanceOutOfRangeTimer = 0f;

        if (calibrationPanel != null)
            calibrationPanel.SetActive(false);

        SetHiddenObjectsActive(true);
        FindObjectOfType<GameManager>()?.ResumeAfterRecalibration();
    }

    private YoloBodyPoseProvider GetYoloPoseProvider()
    {
        if (poseProvider is YoloBodyPoseProvider yoloProvider)
            return yoloProvider;

        return FindObjectOfType<YoloBodyPoseProvider>();
    }

    private CalibrationInstruction GetRuntimeAnchorInstruction()
    {
        // TryGetLatestPose is intentionally loose. Even if calibration anchors
        // are not all confident, it can still tell us which body parts are missing.
        if (poseProvider == null || !poseProvider.TryGetLatestPose(out BodyPoseLandmarks pose))
            return CalibrationInstruction.StandWhereVisible;

        bool noseVisible = IsAnchorVisible(pose.Nose);
        bool shouldersVisible = IsAnchorVisible(pose.LeftShoulder) && IsAnchorVisible(pose.RightShoulder);
        bool hipsVisible = IsAnchorVisible(pose.LeftHip) && IsAnchorVisible(pose.RightHip);
        bool anklesVisible = IsAnchorVisible(pose.LeftAnkle) && IsAnchorVisible(pose.RightAnkle);

        if (!anklesVisible)
            return CalibrationInstruction.MoveBackFeetVisible;

        if (!hipsVisible)
            return CalibrationInstruction.MoveBackBodyVisible;

        if (!shouldersVisible || !noseVisible)
            return CalibrationInstruction.ShowHeadAndShoulders;

        return CalibrationInstruction.StandWhereVisible;
    }

    private bool IsAnchorVisible(BodyLandmark landmark)
    {
        // Runtime correction uses a local threshold so it can be tuned separately
        // from the stricter calibration settings if needed later.
        const float minimumConfidence = 0.5f;
        return landmark.Visibility >= minimumConfidence && landmark.Presence >= minimumConfidence;
    }

    private void LogRuntimeDebug(float bodyHeight, float minHeight, float maxHeight)
    {
        if (!logDebugInfo || Time.unscaledTime - lastDebugLogTime < 1f)
            return;

        lastDebugLogTime = Time.unscaledTime;
        Debug.Log(
            "BodyPositionCalibrationManager runtime: " +
            $"bodyHeight={bodyHeight:F1}, calibratedHeight={calibratedBodyHeight:F1}, " +
            $"min={minHeight:F1}, max={maxHeight:F1}, outOfRangeTimer={distanceOutOfRangeTimer:F2}"
        );
    }

    public void SetRuntimeMonitoringPaused(bool isPaused)
    {
        // GameManager uses this while level-complete UI is open so calibration
        // does not pop over the result screen.
        runtimeMonitoringPaused = isPaused;
        distanceOutOfRangeTimer = 0f;
    }

    public void HideCalibrationPanel()
    {
        // Used by GameManager when another UI, such as level complete, should own the screen.
        StopInstructionNarration();

        if (calibrationPanel != null)
            calibrationPanel.SetActive(false);

        SetHiddenObjectsActive(true);
    }
}
