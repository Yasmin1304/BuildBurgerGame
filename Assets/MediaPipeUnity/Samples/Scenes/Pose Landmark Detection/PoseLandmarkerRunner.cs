// Copyright (c) 2023 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Collections;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mediapipe.Unity.Sample.PoseLandmarkDetection
{
  public class PoseLandmarkerRunner : VisionTaskApiRunner<PoseLandmarker>
  {
    [SerializeField] private PoseLandmarkerResultAnnotationController _poseLandmarkerResultAnnotationController;
    [SerializeField] private MonoBehaviour bodyPoseProvider;
    [SerializeField] private bool drawPoseAnnotations = false;

    private Experimental.TextureFramePool _textureFramePool;
    private readonly object _bodyPoseResultLock = new object();
    private PoseLandmarkerResult _bodyPoseResult;
    private bool _hasBodyPoseResult;
    private bool _shouldClearBodyPose;

    public readonly PoseLandmarkDetectionConfig config = new PoseLandmarkDetectionConfig();

    public override void Stop()
    {
      base.Stop();
      _textureFramePool?.Dispose();
      _textureFramePool = null;
      QueueClearBodyPose();
    }

    private void Update()
    {
      FlushBodyPoseProvider();
    }

    protected override IEnumerator Run()
    {
      Debug.Log($"Delegate = {config.Delegate}");
      Debug.Log($"Image Read Mode = {config.ImageReadMode}");
      Debug.Log($"Model = {config.ModelName}");
      Debug.Log($"Running Mode = {config.RunningMode}");
      Debug.Log($"NumPoses = {config.NumPoses}");
      Debug.Log($"MinPoseDetectionConfidence = {config.MinPoseDetectionConfidence}");
      Debug.Log($"MinPosePresenceConfidence = {config.MinPosePresenceConfidence}");
      Debug.Log($"MinTrackingConfidence = {config.MinTrackingConfidence}");
      Debug.Log($"OutputSegmentationMasks = {config.OutputSegmentationMasks}");

      yield return AssetLoader.PrepareAssetAsync(config.ModelPath);

      var options = config.GetPoseLandmarkerOptions(config.RunningMode == Tasks.Vision.Core.RunningMode.LIVE_STREAM ? OnPoseLandmarkDetectionOutput : null);
      taskApi = PoseLandmarker.CreateFromOptions(options, GpuManager.GpuResources);
      var imageSource = ImageSourceProvider.ImageSource;

      yield return imageSource.Play();

      if (!imageSource.isPrepared)
      {
        Logger.LogError(TAG, "Failed to start ImageSource, exiting...");
        yield break;
      }

      // Use RGBA32 as the input format.
      // TODO: When using GpuBuffer, MediaPipe assumes that the input format is BGRA, so maybe the following code needs to be fixed.
      _textureFramePool = new Experimental.TextureFramePool(imageSource.textureWidth, imageSource.textureHeight, TextureFormat.RGBA32, 10);

      // NOTE: The screen will be resized later, keeping the aspect ratio.
      screen.Initialize(imageSource);

      if (drawPoseAnnotations && _poseLandmarkerResultAnnotationController != null)
      {
        SetupAnnotationController(_poseLandmarkerResultAnnotationController, imageSource);
        _poseLandmarkerResultAnnotationController.InitScreen(imageSource.textureWidth, imageSource.textureHeight);
      }

      var transformationOptions = imageSource.GetTransformationOptions();
      var flipHorizontally = transformationOptions.flipHorizontally;
      var flipVertically = transformationOptions.flipVertically;

      // Always setting rotationDegrees to 0 to avoid the issue that the detection becomes unstable when the input image is rotated.
      // https://github.com/homuler/MediaPipeUnityPlugin/issues/1196
      var imageProcessingOptions = new Tasks.Vision.Core.ImageProcessingOptions(rotationDegrees: 0);

      AsyncGPUReadbackRequest req = default;
      var waitUntilReqDone = new WaitUntil(() => req.done);
      var waitForEndOfFrame = new WaitForEndOfFrame();
      var result = PoseLandmarkerResult.Alloc(options.numPoses, options.outputSegmentationMasks);

      // NOTE: we can share the GL context of the render thread with MediaPipe (for now, only on Android)
      var canUseGpuImage = SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3 && GpuManager.GpuResources != null;
      using var glContext = canUseGpuImage ? GpuManager.GetGlContext() : null;

      while (true)
      {
        if (isPaused)
        {
          yield return new WaitWhile(() => isPaused);
        }

        if (!_textureFramePool.TryGetTextureFrame(out var textureFrame))
        {
          yield return new WaitForEndOfFrame();
          continue;
        }

        // Build the input Image
        Image image;
        switch (config.ImageReadMode)
        {
          case ImageReadMode.GPU:
            if (!canUseGpuImage)
            {
              throw new System.Exception("ImageReadMode.GPU is not supported");
            }
            textureFrame.ReadTextureOnGPU(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
            image = textureFrame.BuildGPUImage(glContext);
            // TODO: Currently we wait here for one frame to make sure the texture is fully copied to the TextureFrame before sending it to MediaPipe.
            // This usually works but is not guaranteed. Find a proper way to do this. See: https://github.com/homuler/MediaPipeUnityPlugin/pull/1311
            yield return waitForEndOfFrame;
            break;
          case ImageReadMode.CPU:
            yield return waitForEndOfFrame;
            textureFrame.ReadTextureOnCPU(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
            image = textureFrame.BuildCPUImage();
            textureFrame.Release();
            break;
          case ImageReadMode.CPUAsync:
          default:
            req = textureFrame.ReadTextureAsync(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
            yield return waitUntilReqDone;

            if (req.hasError)
            {
              Debug.LogWarning($"Failed to read texture from the image source");
              continue;
            }
            image = textureFrame.BuildCPUImage();
            textureFrame.Release();
            break;
        }

        switch (taskApi.runningMode)
        {
          case Tasks.Vision.Core.RunningMode.IMAGE:
            if (taskApi.TryDetect(image, imageProcessingOptions, ref result))
            {
              QueueBodyPose(result);
              DrawPoseNow(result);
            }
            else
            {
              QueueClearBodyPose();
              DrawPoseNow(default);
            }
            DisposeAllMasks(result);
            break;
          case Tasks.Vision.Core.RunningMode.VIDEO:
            if (taskApi.TryDetectForVideo(image, GetCurrentTimestampMillisec(), imageProcessingOptions, ref result))
            {
              QueueBodyPose(result);
              DrawPoseNow(result);
            }
            else
            {
              QueueClearBodyPose();
              DrawPoseNow(default);
            }
            DisposeAllMasks(result);
            break;
          case Tasks.Vision.Core.RunningMode.LIVE_STREAM:
            taskApi.DetectAsync(image, GetCurrentTimestampMillisec(), imageProcessingOptions);
            break;
        }
      }
    }

    private void OnPoseLandmarkDetectionOutput(PoseLandmarkerResult result, Image image, long timestamp)
    {
      QueueBodyPose(result);
      DrawPoseLater(result);
      DisposeAllMasks(result);
    }

    private void DrawPoseNow(PoseLandmarkerResult result)
    {
      if (drawPoseAnnotations && _poseLandmarkerResultAnnotationController != null)
      {
        _poseLandmarkerResultAnnotationController.DrawNow(result);
      }
    }

    private void DrawPoseLater(PoseLandmarkerResult result)
    {
      if (drawPoseAnnotations && _poseLandmarkerResultAnnotationController != null)
      {
        _poseLandmarkerResultAnnotationController.DrawLater(result);
      }
    }

    private void QueueBodyPose(PoseLandmarkerResult result)
    {
      lock (_bodyPoseResultLock)
      {
        result.CloneTo(ref _bodyPoseResult);
        _hasBodyPoseResult = true;
        _shouldClearBodyPose = false;
      }
    }

    private void QueueClearBodyPose()
    {
      lock (_bodyPoseResultLock)
      {
        _hasBodyPoseResult = false;
        _shouldClearBodyPose = true;
      }
    }

    private void FlushBodyPoseProvider()
    {
      if (bodyPoseProvider == null)
      {
        return;
      }

      var hasResult = false;
      var shouldClear = false;
      var result = default(PoseLandmarkerResult);

      lock (_bodyPoseResultLock)
      {
        if (_hasBodyPoseResult)
        {
          _bodyPoseResult.CloneTo(ref result);
          hasResult = true;
          _hasBodyPoseResult = false;
        }
        else if (_shouldClearBodyPose)
        {
          shouldClear = true;
          _shouldClearBodyPose = false;
        }
      }

      if (hasResult)
      {
        bodyPoseProvider.SendMessage("SetPose", result, SendMessageOptions.DontRequireReceiver);
      }
      else if (shouldClear)
      {
        bodyPoseProvider.SendMessage("ClearPose", SendMessageOptions.DontRequireReceiver);
      }
    }

    private void DisposeAllMasks(PoseLandmarkerResult result)
    {
      if (result.segmentationMasks != null)
      {
        foreach (var mask in result.segmentationMasks)
        {
          mask.Dispose();
        }
      }
    }
  }
}
