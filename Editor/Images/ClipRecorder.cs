using System;
using System.Collections.Generic;
using CookieJarTools.Cookbook.Editor.Images.Gif;
using UnityEditor;
using UnityEngine;

namespace CookieJarTools.Cookbook.Editor.Images
{
    /// <summary>
    /// Records a few seconds of the Scene view or the game camera as a GIF, for a page. Frames are
    /// taken a dozen times a second at up to 480 pixels wide; recording stops when asked, or after
    /// eight seconds, and the GIF is made then.
    /// </summary>
    public sealed class ClipRecorder
    {
        private const float FramesPerSecond = 12f;
        private const float MaxSeconds = 8f;
        private const int MaxWidth = 480;

        private readonly bool scene;
        private readonly List<Color32[]> frames = new List<Color32[]>();

        private double started;
        private double lastFrame;
        private int width;
        private int height;

        public ClipRecorder(bool scene)
        {
            this.scene = scene;
        }

        /// <summary>A frame was taken; the recording is this many seconds long.</summary>
        public event Action<float> Ticked;

        /// <summary>The GIF, once made — or null when nothing could be recorded.</summary>
        public event Action<byte[]> Finished;

        public bool Recording { get; private set; }

        public bool Start()
        {
            var camera = Camera();
            if (camera == null)
            {
                return false;
            }

            var size = scene ? new Vector2(camera.pixelWidth, camera.pixelHeight) : Handles.GetMainGameViewSize();
            var scale = Mathf.Min(1f, MaxWidth / Mathf.Max(1f, size.x));
            width = Mathf.Max(2, Mathf.RoundToInt(size.x * scale));
            height = Mathf.Max(2, Mathf.RoundToInt(size.y * scale));
            started = EditorApplication.timeSinceStartup;
            lastFrame = 0;
            Recording = true;
            EditorApplication.update += Tick;
            return true;
        }

        public void Stop()
        {
            if (!Recording)
            {
                return;
            }

            Recording = false;
            EditorApplication.update -= Tick;
            EditorUtility.DisplayProgressBar("Cookbook", "Making the clip…", 0.5f);
            try
            {
                Finished?.Invoke(frames.Count > 0 ? GifWriter.Write(frames, width, height, Mathf.RoundToInt(100f / FramesPerSecond)) : null);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                frames.Clear();
            }
        }

        private void Tick()
        {
            var now = EditorApplication.timeSinceStartup;
            if (now - lastFrame < 1.0 / FramesPerSecond)
            {
                return;
            }

            lastFrame = now;
            var camera = Camera();
            if (camera != null)
            {
                frames.Add(ViewCapture.Pixels(camera, width, height));
            }

            var seconds = (float)(now - started);
            Ticked?.Invoke(seconds);
            if (seconds >= MaxSeconds)
            {
                Stop();
            }
        }

        private Camera Camera()
        {
            if (scene)
            {
                return SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.camera : null;
            }

            return UnityEngine.Camera.main != null ? UnityEngine.Camera.main : UnityEngine.Object.FindFirstObjectByType<Camera>();
        }
    }
}
