using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Demo.Editor
{
    /// <summary>One captured (or failed) scene of a <see cref="DemoSceneCapture" /> run.</summary>
    public sealed class DemoCaptureResult
    {
        internal DemoCaptureResult(string scenePath)
        {
            ScenePath = scenePath;
        }

        public string ScenePath { get; }

        /// <summary>Written PNG (null when the capture failed).</summary>
        public string ImagePath { get; internal set; }

        public string Error { get; internal set; }
        public List<string> Warnings { get; } = new();
        public bool Success => Error == null;
    }

    /// <summary>Outcome of a capture run over several scenes.</summary>
    public sealed class DemoCaptureReport
    {
        public List<DemoCaptureResult> Scenes { get; } = new();

        /// <summary>Errors not tied to one scene (bad arguments, unmatched filters, no graphics device).</summary>
        public List<string> Errors { get; } = new();

        public bool Succeeded => Errors.Count == 0 && Scenes.TrueForAll(scene => scene.Success);

        public string Summary()
        {
            StringBuilder builder = new();
            int captured = Scenes.FindAll(scene => scene.Success).Count;
            builder.Append(DemoSceneRunner.LogPrefix).Append("Capture ").Append(Succeeded ? "SUCCESS" : "FAILED")
                .Append(": ").Append(captured).Append('/').Append(Scenes.Count).Append(" scenes captured");
            foreach (string error in Errors)
            {
                builder.Append("\n error: ").Append(error);
            }

            foreach (DemoCaptureResult scene in Scenes)
            {
                builder.Append("\n ").Append(scene.Success ? "OK   " : "FAIL ").Append(scene.ScenePath);
                builder.Append(scene.Success ? " -> " + scene.ImagePath : " | " + scene.Error);
                foreach (string warning in scene.Warnings)
                {
                    builder.Append("\n   warning: ").Append(warning);
                }
            }

            return builder.ToString();
        }
    }

    /// <summary>
    ///     Renders what the Game view shows for a demo scene WITHOUT entering Play Mode: every enabled scene camera
    ///     in depth order into one render texture, then the Screen Space Overlay canvases, which are pointed at a
    ///     temporary capture camera for the shot (CanvasScaler factors are computed for the shot size) and restored
    ///     afterwards. The scene is never saved. Needs a graphics device: run batch captures without -nographics.
    ///     Built-in render pipeline first; URP renders through Camera.Render as well.
    /// </summary>
    public static class DemoSceneCapture
    {
        public const int DefaultWidth = 1920;
        public const int DefaultHeight = 1080;

        private const string MenuRoot = "Neoxider/Demo/";
        private const string LastFolderKey = "Neo.Demo.Editor.DemoSceneCapture.Folder";
        private const int UiLayer = 5;
        private const float UiPlaneDistance = 1f;

        [MenuItem(MenuRoot + "Capture Active Scene (Game View PNG)...", false, 330)]
        private static void CaptureActiveFromMenu()
        {
            Scene scene = SceneManager.GetActiveScene();
            string name = string.IsNullOrEmpty(scene.name) ? "Untitled" : scene.name;
            string folder = EditorPrefs.GetString(LastFolderKey, Path.GetFullPath(Path.Combine(Application.dataPath, "..")));
            string path = EditorUtility.SaveFilePanel("Capture demo scene", folder, name + ".png", "png");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            EditorPrefs.SetString(LastFolderKey, Path.GetDirectoryName(path));
            if (TryCapture(scene, path, DefaultWidth, DefaultHeight, out string error, out List<string> warnings))
            {
                Debug.Log(DemoSceneRunner.LogPrefix + "Captured " + scene.path + " -> " + path +
                          (warnings.Count > 0 ? "\n warning: " + string.Join("\n warning: ", warnings) : string.Empty));
                EditorUtility.RevealInFinder(path);
            }
            else
            {
                Debug.LogError(DemoSceneRunner.LogPrefix + "Capture of " + scene.path + " failed: " + error);
            }
        }

        [MenuItem(MenuRoot + "Capture Active Scene (Game View PNG)...", true)]
        private static bool CanCaptureActive()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode && HasGraphicsDevice;
        }

        [MenuItem(MenuRoot + "Capture All Demo Scenes (Game View PNGs)...", false, 331)]
        private static void CaptureAllFromMenu()
        {
            string folder = EditorPrefs.GetString(LastFolderKey, Path.GetFullPath(Path.Combine(Application.dataPath, "..")));
            string chosen = EditorUtility.SaveFolderPanel("Folder for demo scene captures", folder, string.Empty);
            if (string.IsNullOrEmpty(chosen))
            {
                return;
            }

            EditorPrefs.SetString(LastFolderKey, chosen);
            DemoCaptureReport report = CaptureScenes(null, chosen, DefaultWidth, DefaultHeight);
            EditorUtility.DisplayDialog("Neoxider demo captures",
                report.Succeeded
                    ? report.Scenes.Count + " scene(s) captured to " + chosen + "."
                    : "Some captures failed. See the Console.", "OK");
            if (report.Scenes.Count > 0)
            {
                EditorUtility.RevealInFinder(chosen);
            }
        }

        [MenuItem(MenuRoot + "Capture All Demo Scenes (Game View PNGs)...", true)]
        private static bool CanCaptureAll()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode && HasGraphicsDevice;
        }

        /// <summary>False under -nographics (the null graphics device cannot render).</summary>
        public static bool HasGraphicsDevice => SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;

        /// <summary>
        ///     Opens every registered builder scene (or the scenes matching <paramref name="sceneFilter" />, which may
        ///     also name Demo / NeoxiderPages scenes without a builder), captures it to
        ///     &lt;<paramref name="directory" />&gt;/&lt;SceneName&gt;.png and never saves it. Interactive runs ask to
        ///     save modified scenes first and restore the open scenes afterwards.
        /// </summary>
        public static DemoCaptureReport CaptureScenes(IReadOnlyCollection<string> sceneFilter, string directory, int width,
            int height)
        {
            DemoCaptureReport report = new();
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                report.Errors.Add("Leave Play Mode before capturing demo scenes.");
                return Finish(report);
            }

            if (!HasGraphicsDevice)
            {
                report.Errors.Add("No graphics device (the editor runs with -nographics); captures need a GPU context.");
                return Finish(report);
            }

            if (string.IsNullOrWhiteSpace(directory))
            {
                report.Errors.Add("No output folder (" + DemoSceneBatch.ShotsArgument + " <dir>).");
                return Finish(report);
            }

            List<string> scenes = DemoSceneRunner.ResolveScenePaths(sceneFilter, report.Errors);
            if (scenes.Count == 0)
            {
                report.Errors.Add("No scenes to capture.");
                return Finish(report);
            }

            Directory.CreateDirectory(directory);
            SceneSetup[] setup = null;
            if (!Application.isBatchMode)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    report.Errors.Add("Cancelled: open scenes have unsaved changes.");
                    return Finish(report);
                }

                setup = EditorSceneManager.GetSceneManagerSetup();
            }

            HashSet<string> usedNames = new(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (string scenePath in scenes)
                {
                    DemoCaptureResult result = new(scenePath);
                    report.Scenes.Add(result);
                    string fileName = UniqueName(Path.GetFileNameWithoutExtension(scenePath), usedNames) + ".png";
                    try
                    {
                        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                        string target = Path.Combine(directory, fileName);
                        if (TryCapture(scene, target, width, height, out string error, out List<string> warnings))
                        {
                            result.ImagePath = target;
                        }
                        else
                        {
                            result.Error = error;
                        }

                        result.Warnings.AddRange(warnings);
                    }
                    catch (Exception exception)
                    {
                        result.Error = exception.GetType().Name + ": " + exception.Message;
                        Debug.LogException(exception);
                    }
                }
            }
            finally
            {
                DemoSceneRunner.RestoreSetup(setup);
            }

            return Finish(report);
        }

        /// <summary>Renders <paramref name="scene" /> and writes a PNG; false with <paramref name="error" /> on failure.</summary>
        public static bool TryCapture(Scene scene, string pngPath, int width, int height, out string error,
            out List<string> warnings)
        {
            warnings = new List<string>();
            Texture2D shot = null;
            try
            {
                shot = Render(scene, width, height, warnings);
                string folder = Path.GetDirectoryName(Path.GetFullPath(pngPath));
                if (!string.IsNullOrEmpty(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                File.WriteAllBytes(pngPath, shot.EncodeToPNG());
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.GetType().Name + ": " + exception.Message;
                return false;
            }
            finally
            {
                if (shot != null)
                {
                    Object.DestroyImmediate(shot);
                }
            }
        }

        /// <summary>
        ///     Renders the Game view of an open scene into a new RGB texture (the caller destroys it). Overlay canvases
        ///     are switched to a capture camera only for the render and restored afterwards.
        /// </summary>
        /// <param name="warnings">Optional list that receives notes such as "no camera".</param>
        public static Texture2D Render(Scene scene, int width, int height, List<string> warnings = null)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new ArgumentException("Render needs a valid, loaded scene.", nameof(scene));
            }

            if (!HasGraphicsDevice)
            {
                throw new InvalidOperationException("No graphics device (-nographics); captures need a GPU context.");
            }

            if (width < 16 || height < 16 || width > 8192 || height > 8192)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "Capture size must be 16..8192 px per side, got " +
                                                                     width + "x" + height + ".");
            }

            List<Camera> textureCameras = new();
            List<Camera> screenCameras = new();
            List<Canvas> overlays = new();
            Collect(scene, textureCameras, screenCameras, overlays);
            if (screenCameras.Count == 0 && overlays.Count == 0)
            {
                warnings?.Add("The scene has no enabled camera or screen-space canvas; the capture is black.");
            }
            else if (screenCameras.Count == 0)
            {
                warnings?.Add("The scene has no enabled camera; only the screen-space UI is drawn (Game view shows " +
                              "'No cameras rendering').");
            }

            RenderTextureDescriptor descriptor = new(width, height, RenderTextureFormat.ARGB32, 24)
            {
                sRGB = QualitySettings.activeColorSpace == ColorSpace.Linear,
                msaaSamples = 1
            };
            RenderTexture target = RenderTexture.GetTemporary(descriptor);
            RenderTexture previousActive = RenderTexture.active;
            GameObject uiHost = null;
            List<CanvasState> states = new();
            try
            {
                Camera uiCamera = null;
                if (overlays.Count > 0)
                {
                    uiHost = new GameObject("__DemoCaptureCamera") { hideFlags = HideFlags.HideAndDontSave };
                    uiHost.layer = UiLayer;
                    // WHY: far from any demo world, and the clip range only reaches the canvas plane.
                    uiHost.transform.SetPositionAndRotation(new Vector3(0f, -100000f, -100000f), Quaternion.identity);
                    uiCamera = uiHost.AddComponent<Camera>();
                    uiCamera.enabled = false;
                    uiCamera.orthographic = true;
                    uiCamera.orthographicSize = 1f;
                    uiCamera.nearClipPlane = 0.01f;
                    uiCamera.farClipPlane = UiPlaneDistance * 2f;
                    uiCamera.cullingMask = ~0;
                    uiCamera.useOcclusionCulling = false;
                    uiCamera.allowHDR = false;
                    uiCamera.allowMSAA = false;
                    uiCamera.backgroundColor = Color.black;
                    uiCamera.clearFlags = screenCameras.Count > 0 ? CameraClearFlags.Depth : CameraClearFlags.SolidColor;
                    uiCamera.targetTexture = target;
                    foreach (Canvas canvas in overlays)
                    {
                        states.Add(CanvasState.Capture(canvas, uiCamera, width, height));
                    }
                }

                RefreshUi(scene);
                if (uiCamera != null)
                {
                    // WHY: the first render sizes the switched canvases; layouts and texts are rebuilt for that size.
                    uiCamera.Render();
                    RefreshUi(scene);
                }

                foreach (Camera camera in textureCameras)
                {
                    camera.Render();
                }

                RenderTexture.active = target;
                GL.Clear(true, true, Color.black);
                RenderTexture.active = previousActive;
                foreach (Camera camera in screenCameras)
                {
                    RenderTexture own = camera.targetTexture;
                    camera.targetTexture = target;
                    try
                    {
                        camera.Render();
                    }
                    finally
                    {
                        camera.targetTexture = own;
                    }
                }

                if (uiCamera != null)
                {
                    uiCamera.Render();
                }

                RenderTexture.active = target;
                Texture2D shot = new(width, height, TextureFormat.RGB24, false, false);
                shot.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
                shot.Apply(false, false);
                return shot;
            }
            finally
            {
                RenderTexture.active = previousActive;
                for (int i = states.Count - 1; i >= 0; i--)
                {
                    states[i].Restore();
                }

                if (states.Count > 0)
                {
                    Canvas.ForceUpdateCanvases();
                }

                RenderTexture.ReleaseTemporary(target);
                if (uiHost != null)
                {
                    Object.DestroyImmediate(uiHost);
                }
            }
        }

        private static void Collect(Scene scene, List<Camera> textureCameras, List<Camera> screenCameras,
            List<Canvas> overlays)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Camera camera in root.GetComponentsInChildren<Camera>(false))
                {
                    if (!camera.enabled || !camera.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    if (camera.targetTexture != null)
                    {
                        textureCameras.Add(camera);
                    }
                    else if (camera.targetDisplay == 0)
                    {
                        screenCameras.Add(camera);
                    }
                }

                foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(false))
                {
                    bool overlay = canvas.renderMode == RenderMode.ScreenSpaceOverlay ||
                                   (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null);
                    if (overlay && canvas.enabled && canvas.isRootCanvas && canvas.gameObject.activeInHierarchy &&
                        canvas.targetDisplay == 0)
                    {
                        overlays.Add(canvas);
                    }
                }
            }

            // WHY: List.Sort is unstable; the index keeps hierarchy order for cameras of equal depth, like the Game view.
            List<Camera> ordered = new(screenCameras);
            screenCameras.Sort((a, b) =>
            {
                int byDepth = a.depth.CompareTo(b.depth);
                return byDepth != 0 ? byDepth : ordered.IndexOf(a).CompareTo(ordered.IndexOf(b));
            });
        }

        private static void RefreshUi(Scene scene)
        {
            Canvas.ForceUpdateCanvases();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(false))
                {
                    if (text.isActiveAndEnabled)
                    {
                        text.ForceMeshUpdate(false, true);
                    }
                }
            }

            Canvas.ForceUpdateCanvases();
        }

        private static string UniqueName(string name, HashSet<string> used)
        {
            string candidate = string.IsNullOrEmpty(name) ? "Scene" : name;
            for (int i = 2; !used.Add(candidate); i++)
            {
                candidate = name + "_" + i.ToString(CultureInfo.InvariantCulture);
            }

            return candidate;
        }

        private static DemoCaptureReport Finish(DemoCaptureReport report)
        {
            if (report.Succeeded)
            {
                Debug.Log(report.Summary());
            }
            else
            {
                Debug.LogError(report.Summary());
            }

            return report;
        }

        /// <summary>Render settings of one overlay canvas, switched to the capture camera and restored afterwards.</summary>
        private sealed class CanvasState
        {
            private Canvas _canvas;
            private RenderMode _renderMode;
            private Camera _worldCamera;
            private float _planeDistance;
            private CanvasScaler _scaler;
            private CanvasScaler.ScaleMode _scaleMode;
            private float _scaleFactor;

            public static CanvasState Capture(Canvas canvas, Camera camera, int width, int height)
            {
                CanvasState state = new()
                {
                    _canvas = canvas,
                    _renderMode = canvas.renderMode,
                    _worldCamera = canvas.worldCamera,
                    _planeDistance = canvas.planeDistance,
                    _scaler = canvas.GetComponent<CanvasScaler>()
                };

                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = UiPlaneDistance;
                if (state._scaler != null && state._scaler.enabled &&
                    state._scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
                {
                    // WHY: the scaler reads the display size, which in batch mode is not the shot size.
                    state._scaleMode = state._scaler.uiScaleMode;
                    state._scaleFactor = state._scaler.scaleFactor;
                    float factor = ScaleFactor(state._scaler, width, height);
                    state._scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                    state._scaler.scaleFactor = factor;
                    Rehandle(state._scaler);
                }
                else
                {
                    state._scaler = null;
                }

                return state;
            }

            public void Restore()
            {
                if (_canvas == null)
                {
                    return;
                }

                _canvas.renderMode = _renderMode;
                _canvas.worldCamera = _worldCamera;
                _canvas.planeDistance = _planeDistance;
                if (_scaler != null)
                {
                    _scaler.uiScaleMode = _scaleMode;
                    _scaler.scaleFactor = _scaleFactor;
                    Rehandle(_scaler);
                }
            }

            private static void Rehandle(CanvasScaler scaler)
            {
                // WHY: OnEnable runs CanvasScaler.Handle, which pushes the scale factor into the canvas now.
                scaler.enabled = false;
                scaler.enabled = true;
            }

            private static float ScaleFactor(CanvasScaler scaler, int width, int height)
            {
                Vector2 reference = scaler.referenceResolution;
                if (reference.x <= 0f || reference.y <= 0f)
                {
                    return 1f;
                }

                float widthRatio = width / reference.x;
                float heightRatio = height / reference.y;
                switch (scaler.screenMatchMode)
                {
                    case CanvasScaler.ScreenMatchMode.Expand:
                        return Mathf.Min(widthRatio, heightRatio);
                    case CanvasScaler.ScreenMatchMode.Shrink:
                        return Mathf.Max(widthRatio, heightRatio);
                    default:
                        float logWidth = Mathf.Log(widthRatio, 2f);
                        float logHeight = Mathf.Log(heightRatio, 2f);
                        return Mathf.Pow(2f, Mathf.Lerp(logWidth, logHeight, scaler.matchWidthOrHeight));
                }
            }
        }
    }
}
