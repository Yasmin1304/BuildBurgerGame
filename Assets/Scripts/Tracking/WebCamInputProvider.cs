using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Owns the webcam connection shared by YOLO tracking components.
/// Keep only one active instance in the scene.
/// </summary>
public sealed class WebCamInputProvider : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private string preferredDeviceName;
    [SerializeField] private bool preferFrontFacingCamera = true;
    [SerializeField] private int requestedWidth = 1280;
    [SerializeField] private int requestedHeight = 720;
    [SerializeField] private int requestedFrameRate = 30;
    [SerializeField] private bool playOnStart = true;

    [Header("Optional Preview")]
    [SerializeField] private RawImage previewImage;
    [SerializeField] private bool mirrorHorizontally = true;

    private WebCamTexture webCamTexture;
    private Coroutine startRoutine;

    public WebCamTexture Texture => webCamTexture;
    public Texture CurrentTexture => webCamTexture;
    public bool IsReady =>
        webCamTexture != null &&
        webCamTexture.isPlaying &&
        webCamTexture.width > 16 &&
        webCamTexture.height > 16;
    public bool DidUpdateThisFrame => IsReady && webCamTexture.didUpdateThisFrame;
    public int TextureWidth => IsReady ? webCamTexture.width : 0;
    public int TextureHeight => IsReady ? webCamTexture.height : 0;
    public int VideoRotationAngle => webCamTexture != null ? webCamTexture.videoRotationAngle : 0;
    public bool VideoVerticallyMirrored =>
        webCamTexture != null && webCamTexture.videoVerticallyMirrored;
    public bool MirrorHorizontally => mirrorHorizontally;

    /// <summary>
    /// Returns the normalized area occupied by the full camera frame when it is
    /// fitted inside a square YOLO input without cropping or distortion.
    /// </summary>
    public Rect GetLetterboxInferenceRect()
    {
        float width = Mathf.Max(1f, TextureWidth);
        float height = Mathf.Max(1f, TextureHeight);
        float aspect = width / height;

        return aspect >= 1f
            ? new Rect(0f, (1f - 1f / aspect) * 0.5f, 1f, 1f / aspect)
            : new Rect((1f - aspect) * 0.5f, 0f, aspect, 1f);
    }

    public void DrawLetterboxedFrame(RenderTexture destination)
    {
        if (destination == null || CurrentTexture == null)
            return;

        Rect contentRect = GetLetterboxInferenceRect();
        Rect pixelRect = new Rect(
            contentRect.x * destination.width,
            contentRect.y * destination.height,
            contentRect.width * destination.width,
            contentRect.height * destination.height
        );
        Rect sourceRect = new Rect(0f, 0f, 1f, 1f);

        if (mirrorHorizontally)
        {
            sourceRect.x = 1f;
            sourceRect.width = -1f;
        }

        if (VideoVerticallyMirrored)
        {
            sourceRect.y = 1f;
            sourceRect.height = -1f;
        }

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = destination;
        GL.Clear(true, true, Color.black);
        GL.PushMatrix();
        GL.LoadPixelMatrix(0f, destination.width, destination.height, 0f);
        Graphics.DrawTexture(pixelRect, CurrentTexture, sourceRect, 0, 0, 0, 0);
        GL.PopMatrix();
        RenderTexture.active = previous;
    }

    public static Vector2 LetterboxInferenceToPreview(
        Vector2 inferencePoint,
        Rect contentRect)
    {
        return new Vector2(
            (inferencePoint.x - contentRect.x) / contentRect.width,
            (inferencePoint.y - contentRect.y) / contentRect.height
        );
    }

    /// <summary>
    /// Converts normalized camera coordinates into screen pixels using the
    /// preview's actual fitted rectangle, including any letterboxing.
    /// </summary>
    public Vector2 PreviewNormalizedToScreenPoint(Vector2 normalizedPoint)
    {
        if (previewImage == null)
        {
            return new Vector2(
                normalizedPoint.x * Screen.width,
                (1f - normalizedPoint.y) * Screen.height
            );
        }

        RectTransform previewRect = previewImage.rectTransform;
        Canvas canvas = previewImage.canvas;
        Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        Vector3[] corners = new Vector3[4];
        previewRect.GetWorldCorners(corners);
        Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[0]);
        Vector2 topRight = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[2]);

        return new Vector2(
            Mathf.Lerp(bottomLeft.x, topRight.x, normalizedPoint.x),
            Mathf.Lerp(bottomLeft.y, topRight.y, 1f - normalizedPoint.y)
        );
    }

    private void Awake()
    {
        if (previewImage == null)
            previewImage = FindCameraPreview();

        Debug.Log(
            $"WebCamInputProvider: Awake. playOnStart={playOnStart}, " +
            $"previewFound={previewImage != null}."
        );
    }

    private void Start()
    {
        if (playOnStart)
            StartCamera();
    }

    public void StartCamera()
    {
        if (startRoutine != null || IsReady)
            return;

        startRoutine = StartCoroutine(StartCameraRoutine());
    }

    public void StopCamera()
    {
        if (startRoutine != null)
        {
            StopCoroutine(startRoutine);
            startRoutine = null;
        }

        if (webCamTexture != null)
        {
            if (webCamTexture.isPlaying)
                webCamTexture.Stop();

            Destroy(webCamTexture);
            webCamTexture = null;
        }

        if (previewImage != null)
            previewImage.texture = null;
    }

    private IEnumerator StartCameraRoutine()
    {
        Debug.Log(
            $"WebCamInputProvider: Requesting camera. " +
            $"Devices reported={WebCamTexture.devices.Length}."
        );

        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);

        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            Debug.LogError("WebCamInputProvider: Webcam permission was not granted.");
            startRoutine = null;
            yield break;
        }

        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            Debug.LogError("WebCamInputProvider: No webcam device was found.");
            startRoutine = null;
            yield break;
        }

        WebCamDevice selectedDevice = SelectDevice(devices);
        Debug.Log(
            $"WebCamInputProvider: Selected '{selectedDevice.name}', " +
            $"frontFacing={selectedDevice.isFrontFacing}."
        );
        webCamTexture = new WebCamTexture(
            selectedDevice.name,
            requestedWidth,
            requestedHeight,
            requestedFrameRate
        );
        webCamTexture.Play();

        float timeoutAt = Time.realtimeSinceStartup + 10f;
        while (!IsReady && Time.realtimeSinceStartup < timeoutAt)
            yield return null;

        if (!IsReady)
        {
            Debug.LogError($"WebCamInputProvider: Timed out starting '{selectedDevice.name}'.");
            startRoutine = null;
            StopCamera();
            yield break;
        }

        ApplyPreviewSettings();
        Debug.Log(
            $"WebCamInputProvider: Started '{selectedDevice.name}' " +
            $"at {webCamTexture.width}x{webCamTexture.height}."
        );
        startRoutine = null;
    }

    private WebCamDevice SelectDevice(WebCamDevice[] devices)
    {
        if (!string.IsNullOrWhiteSpace(preferredDeviceName))
        {
            foreach (WebCamDevice device in devices)
            {
                if (device.name == preferredDeviceName)
                    return device;
            }

            Debug.LogWarning(
                $"WebCamInputProvider: Preferred device '{preferredDeviceName}' was not found."
            );
        }

        foreach (WebCamDevice device in devices)
        {
            if (device.isFrontFacing == preferFrontFacingCamera)
                return device;
        }

        return devices[0];
    }

    private RawImage FindCameraPreview()
    {
        RawImage[] rawImages = FindObjectsOfType<RawImage>(true);
        foreach (RawImage rawImage in rawImages)
        {
            if (rawImage.gameObject.name == "Camera Preview")
                return rawImage;
        }

        Debug.LogWarning("WebCamInputProvider: Could not find the 'Camera Preview' RawImage.");
        return null;
    }

    private void ApplyPreviewSettings()
    {
        if (previewImage == null)
            return;

        previewImage.gameObject.SetActive(true);
        previewImage.enabled = true;
        previewImage.color = Color.white;
        previewImage.texture = webCamTexture;
        previewImage.rectTransform.sizeDelta =
            new Vector2(webCamTexture.width, webCamTexture.height);

        Rect uvRect = new Rect(0f, 0f, 1f, 1f);
        if (mirrorHorizontally)
        {
            uvRect.x = 1f;
            uvRect.width = -1f;
        }

        if (VideoVerticallyMirrored)
        {
            uvRect.y = 1f;
            uvRect.height = -1f;
        }

        previewImage.uvRect = uvRect;
        previewImage.rectTransform.localEulerAngles =
            new Vector3(0f, 0f, -VideoRotationAngle);
    }

    private void OnDisable()
    {
        StopCamera();
    }

    private void OnDestroy()
    {
        StopCamera();
    }
}
