using UnityEditor;
using UnityEngine;

namespace CookieJarTools.Cookbook.Editor.Images
{
    /// <summary>
    /// A picture of what the Scene view or the game's camera sees right now: as PNG bytes, for
    /// dropping straight into a page, or as pixels, for a clip. Null when there is no view or camera
    /// to take it from.
    /// </summary>
    public static class ViewCapture
    {
        public static byte[] Scene()
        {
            var view = SceneView.lastActiveSceneView;
            if (view == null || view.camera == null)
            {
                return null;
            }

            var camera = view.camera;
            return Png(camera, Mathf.RoundToInt(camera.pixelWidth), Mathf.RoundToInt(camera.pixelHeight));
        }

        /// <summary>The main camera at the Game view's size; any camera when none is tagged main.</summary>
        public static byte[] Game()
        {
            var camera = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                return null;
            }

            var size = Handles.GetMainGameViewSize();
            return Png(camera, Mathf.Max(1, Mathf.RoundToInt(size.x)), Mathf.Max(1, Mathf.RoundToInt(size.y)));
        }

        /// <summary>What the camera sees at the size given, top row first.</summary>
        public static Color32[] Pixels(Camera camera, int width, int height)
        {
            var picture = Render(camera, width, height);
            var bottomUp = picture.GetPixels32();
            Object.DestroyImmediate(picture);

            var topDown = new Color32[bottomUp.Length];
            for (var y = 0; y < height; y++)
            {
                System.Array.Copy(bottomUp, (height - 1 - y) * width, topDown, y * width, width);
            }

            return topDown;
        }

        private static byte[] Png(Camera camera, int width, int height)
        {
            var picture = Render(camera, width, height);
            var png = picture.EncodeToPNG();
            Object.DestroyImmediate(picture);
            return png;
        }

        private static Texture2D Render(Camera camera, int width, int height)
        {
            var target = RenderTexture.GetTemporary(new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 24) { sRGB = true });
            var previous = camera.targetTexture;
            var active = RenderTexture.active;
            camera.targetTexture = target;
            camera.Render();
            camera.targetTexture = previous;

            RenderTexture.active = target;
            var picture = new Texture2D(width, height, TextureFormat.RGB24, false);
            picture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            picture.Apply();
            RenderTexture.active = active;
            RenderTexture.ReleaseTemporary(target);
            return picture;
        }
    }
}
