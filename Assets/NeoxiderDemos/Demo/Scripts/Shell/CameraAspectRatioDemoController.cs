using System;
using System.Globalization;
using Neo.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Samples
{
    /// <summary>
    ///     Controller of the authored <b>CameraAspectRatio</b> demo scene. Everything the demo shows (the world-space
    ///     design frame, the control card, the live readout, the device preview and the info card) is saved in
    ///     CameraAspectRatio.unity. Buttons call the public methods below through
    ///     persistent UnityEvents; the reference-size slider writes
    ///     <see cref="CameraAspectRatioScaler.ReferenceOrthographicSize" /> directly (the NoCode path) and then
    ///     calls <see cref="OnReferenceSizeChanged" />. This component only forwards to the real
    ///     <see cref="CameraAspectRatioScaler" /> API, lays out the design frame to match it and mirrors the camera
    ///     into the readout. It builds no UI at runtime.
    /// </summary>
    [AddComponentMenu("Neoxider/Demos/Camera Aspect Ratio Demo")]
    public sealed class CameraAspectRatioDemoController : MonoBehaviour
    {
        /// <summary>Design preset index: 1080×1920, reference size 9.6.</summary>
        public const int PortraitPreset = 0;

        /// <summary>Design preset index: 1920×1080, reference size 5.4.</summary>
        public const int LandscapePreset = 1;

        /// <summary>Design preset index: 1080×1080, reference size 5.4.</summary>
        public const int SquarePreset = 2;

        /// <summary>Output index of the screen (no render texture).</summary>
        public const int ScreenOutput = 0;

        private const float EdgeThicknessPerHeight = 0.00625f;
        private const float CornerSizePerHeight = 0.06f;
        private const float CenterSizePerHeight = 0.05f;
        private const float LabelFontPerHeight = 0.25f;
        private const float LabelInsetPerHeight = 0.045f;
        private const float LabelWidthShare = 0.8f;
        private const float PlatePadding = 0.2f;
        private const float CropTolerance = 0.02f;
        private const string GoodColor = "#43D9A3";
        private const string DangerColor = "#FF5C7A";

        private static readonly Vector2[] DesignResolutions =
        {
            new(1080f, 1920f),
            new(1920f, 1080f),
            new(1080f, 1080f)
        };

        private static readonly float[] DesignReferenceSizes = { 9.6f, 5.4f, 5.4f };
        private static readonly string[] DesignNames = { "Portrait", "Landscape", "Square" };

        private static readonly string[] ModeHints =
        {
            "Fit Width · the design's full width always fits; top and bottom are cropped or extended.",
            "Fit Height · the design's full height always fits; the sides are cropped or extended.",
            "Fit Both · the whole design frame stays visible; Camera.rect adds bars (a render texture expands instead).",
            "Manual · the scaler leaves the size alone and resets Camera.rect to the full screen."
        };

        [Header("Module")]
        [Tooltip("The scaler this demo drives (on 'Main Camera').")]
        [SerializeField] private CameraAspectRatioScaler _scaler;

        [Tooltip("Camera of the scaler; its targetTexture switches between the screen and the device previews.")]
        [SerializeField] private Camera _camera;

        [Header("Design frame (world)")]
        [SerializeField] private SpriteRenderer _frameFill;

        [Tooltip("Top, bottom, left, right edge (sliced sprites).")]
        [SerializeField] private SpriteRenderer[] _frameEdges = Array.Empty<SpriteRenderer>();

        [Tooltip("Top-left, top-right, bottom-left, bottom-right corner markers.")]
        [SerializeField] private Transform[] _frameCorners = Array.Empty<Transform>();

        [SerializeField] private Transform _frameCenter;
        [SerializeField] private TMP_Text _frameTitle;
        [SerializeField] private TMP_Text _frameCaption;

        [Tooltip("Light plates behind the title and caption, so they read on any background art.")]
        [SerializeField] private SpriteRenderer _frameTitlePlate;

        [SerializeField] private SpriteRenderer _frameCaptionPlate;

        [Header("Outputs")]
        [Tooltip("Button labels of the device outputs, in the order of Device Textures.")]
        [SerializeField] private string[] _deviceLabels = Array.Empty<string>();

        [Tooltip("Render texture assets the camera renders into for each device output.")]
        [SerializeField] private RenderTexture[] _deviceTextures = Array.Empty<RenderTexture>();

        [SerializeField] private GameObject _previewRoot;
        [SerializeField] private RawImage _previewImage;
        [SerializeField] private AspectRatioFitter _previewFitter;
        [SerializeField] private TMP_Text _previewCaption;

        [Header("Controls synced on start")]
        [SerializeField] private Slider _referenceSlider;
        [SerializeField] private TMP_Text _modeHint;

        [Header("Live readout")]
        [SerializeField] private TMP_Text _outputValue;
        [SerializeField] private TMP_Text _aspectValue;
        [SerializeField] private TMP_Text _sizeValue;
        [SerializeField] private TMP_Text _viewportValue;
        [SerializeField] private TMP_Text _visibleValue;
        [SerializeField] private TMP_Text _frameValue;

        [Header("Feed")]
        [SerializeField] private DemoLogFeed _log;

        private int _output;
        private bool _warnedUnbuilt;
        private bool _readoutValid;
        private int _lastWidth;
        private int _lastHeight;
        private RenderTexture _lastTarget;
        private bool _lastOrthographic;
        private float _lastSize;
        private float _lastFov;
        private Rect _lastRect;
        private float _lastReference;
        private Vector2 _lastDesign;
        private CameraAspectRatioScaler.ScaleMode _lastMode;

        /// <summary>True when the authored scene references are assigned.</summary>
        public bool IsSceneBuilt => _scaler != null && _camera != null && _frameFill != null && _log != null &&
                                    _outputValue != null && _referenceSlider != null;

        /// <summary>The driven scaler.</summary>
        public CameraAspectRatioScaler Scaler => _scaler;

        /// <summary>Current output: <see cref="ScreenOutput" /> or 1 + the device index.</summary>
        public int CurrentOutput => _output;

        /// <summary>Number of device outputs (render textures) next to the screen.</summary>
        public int DeviceCount => Mathf.Min(_deviceLabels != null ? _deviceLabels.Length : 0,
            _deviceTextures != null ? _deviceTextures.Length : 0);

        private void Start()
        {
            if (!IsSceneBuilt)
            {
                WarnUnbuilt();
            }

            if (_scaler == null && _camera != null)
            {
                _scaler = _camera.GetComponent<CameraAspectRatioScaler>();
            }

            if (_camera == null && _scaler != null)
            {
                _camera = _scaler.GetComponent<Camera>();
            }

            if (_scaler == null || _camera == null)
            {
                return;
            }

            ApplyOutput(ScreenOutput, false);
            SyncControls();
            LayoutDesignFrame();
            RefreshReadout(true);
            Log("CameraAspectRatioScaler live on '" + _camera.name + "' - pick a mode, a design or an output");
        }

        private void OnDisable()
        {
            if (_camera != null && _output != ScreenOutput)
            {
                _camera.targetTexture = null;
            }

            _output = ScreenOutput;
            if (_previewRoot != null)
            {
                _previewRoot.SetActive(false);
            }
        }

        private void LateUpdate()
        {
            RefreshReadout(false);
        }

        /// <summary>Button target: sets <see cref="CameraAspectRatioScaler.Mode" /> by its enum value and refreshes.</summary>
        public void SetScaleMode(int mode)
        {
            if (_scaler == null || !Enum.IsDefined(typeof(CameraAspectRatioScaler.ScaleMode), mode))
            {
                Log("Ignored unknown scale mode " + mode);
                return;
            }

            float before = CameraSize();
            CameraAspectRatioScaler.ScaleMode value = (CameraAspectRatioScaler.ScaleMode)mode;
            _scaler.Mode = value;
            _scaler.Refresh();
            UpdateModeHint();
            Log("scaler.Mode = " + value + " · size " + Format(before, "0.00") + " -> " + Format(CameraSize(), "0.00"));
        }

        /// <summary>
        ///     Button target: applies a design preset (<see cref="PortraitPreset" />, <see cref="LandscapePreset" />,
        ///     <see cref="SquarePreset" />) through <see cref="CameraAspectRatioScaler.TargetResolution" /> and
        ///     <see cref="CameraAspectRatioScaler.ReferenceOrthographicSize" />.
        /// </summary>
        public void SetDesignPreset(int preset)
        {
            if (_scaler == null || preset < 0 || preset >= DesignResolutions.Length)
            {
                Log("Ignored unknown design preset " + preset);
                return;
            }

            Vector2 resolution = DesignResolutions[preset];
            float reference = DesignReferenceSizes[preset];
            _scaler.TargetResolution = resolution;
            _scaler.ReferenceOrthographicSize = reference;
            _scaler.Refresh();
            SyncControls();
            LayoutDesignFrame();
            Log(DesignNames[preset] + " · scaler.TargetResolution = " + Resolution(resolution) +
                " · ReferenceOrthographicSize = " + Format(reference, "0.0"));
        }

        /// <summary>
        ///     Slider listener after the NoCode setter <see cref="CameraAspectRatioScaler.ReferenceOrthographicSize" />:
        ///     applies the value immediately and resizes the design frame to the new reference.
        /// </summary>
        public void OnReferenceSizeChanged(float value)
        {
            if (_scaler == null)
            {
                return;
            }

            if (!Mathf.Approximately(_scaler.ReferenceOrthographicSize, value))
            {
                _scaler.ReferenceOrthographicSize = value;
            }

            _scaler.Refresh();
            LayoutDesignFrame();
        }

        /// <summary>
        ///     Button target: <see cref="ScreenOutput" /> renders to the screen (follow the Game view size); 1..N
        ///     render the same camera into the device render texture and show it in the preview.
        /// </summary>
        public void SetOutput(int output)
        {
            ApplyOutput(output, true);
        }

        /// <summary>Button target: Portrait design, Fit Both, the screen output.</summary>
        public void ResetDemo()
        {
            if (_scaler == null)
            {
                return;
            }

            ApplyOutput(ScreenOutput, false);
            _scaler.Mode = CameraAspectRatioScaler.ScaleMode.FitBoth;
            _scaler.TargetResolution = DesignResolutions[PortraitPreset];
            _scaler.ReferenceOrthographicSize = DesignReferenceSizes[PortraitPreset];
            _scaler.Refresh();
            SyncControls();
            LayoutDesignFrame();
            Log("Reset · Portrait 1080×1920, Fit Both, reference 9.6, Screen");
        }

        /// <summary>
        ///     Sizes the world-space design frame to the scaler's reference composition: height = 2 ×
        ///     ReferenceOrthographicSize, width = height × the TargetResolution aspect. Call it
        ///     once so the saved frame matches the authored scaler.
        /// </summary>
        public void LayoutDesignFrame()
        {
            if (_scaler == null)
            {
                return;
            }

            Vector2 design = _scaler.TargetResolution;
            float height = 2f * _scaler.ReferenceOrthographicSize;
            float width = height * DesignAspect(design);
            float thickness = height * EdgeThicknessPerHeight;
            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;

            if (_frameFill != null)
            {
                _frameFill.transform.localPosition = Vector3.zero;
                _frameFill.size = new Vector2(width, height);
            }

            SetEdge(0, new Vector3(0f, halfHeight, 0f), new Vector2(width + thickness, thickness));
            SetEdge(1, new Vector3(0f, -halfHeight, 0f), new Vector2(width + thickness, thickness));
            SetEdge(2, new Vector3(-halfWidth, 0f, 0f), new Vector2(thickness, height + thickness));
            SetEdge(3, new Vector3(halfWidth, 0f, 0f), new Vector2(thickness, height + thickness));

            float cornerSize = height * CornerSizePerHeight;
            SetCorner(0, new Vector3(-halfWidth, halfHeight, 0f), cornerSize);
            SetCorner(1, new Vector3(halfWidth, halfHeight, 0f), cornerSize);
            SetCorner(2, new Vector3(-halfWidth, -halfHeight, 0f), cornerSize);
            SetCorner(3, new Vector3(halfWidth, -halfHeight, 0f), cornerSize);

            if (_frameCenter != null)
            {
                _frameCenter.localPosition = Vector3.zero;
                _frameCenter.localScale = Vector3.one * (height * CenterSizePerHeight);
            }

            float inset = height * LabelInsetPerHeight;
            PlaceLabel(_frameTitle, _frameTitlePlate, "DESIGN " + Resolution(design) + " · " + DesignName(design),
                halfHeight - inset, width, height);
            PlaceLabel(_frameCaption, _frameCaptionPlate, "Reference size " +
                                                          Format(_scaler.ReferenceOrthographicSize, "0.0") + " -> " +
                                                          Format(width, "0.0") + " × " + Format(height, "0.0") + " units",
                -halfHeight + inset, width, height);
        }

        private void ApplyOutput(int output, bool log)
        {
            if (output < 0 || output > DeviceCount)
            {
                Log("Ignored unknown output " + output);
                return;
            }

            RenderTexture texture = output == ScreenOutput ? null : _deviceTextures[output - 1];
            string label = output == ScreenOutput ? "Screen" : _deviceLabels[output - 1];
            if (output != ScreenOutput && texture == null)
            {
                Log(label + " has no render texture; re-import the scene from the Demo download");
                return;
            }

            _output = output;
            if (_camera != null)
            {
                _camera.targetTexture = texture;
            }

            if (_previewRoot != null)
            {
                _previewRoot.SetActive(texture != null);
            }

            if (texture != null)
            {
                if (_previewImage != null)
                {
                    _previewImage.texture = texture;
                }

                if (_previewFitter != null)
                {
                    _previewFitter.aspectRatio = texture.width / (float)Mathf.Max(1, texture.height);
                }

                if (_previewCaption != null)
                {
                    _previewCaption.text = label + " · Camera.targetTexture " + texture.width + "×" + texture.height;
                }
            }

            if (_scaler != null)
            {
                _scaler.Refresh();
            }

            if (!log)
            {
                return;
            }

            Log(texture == null
                ? "camera.targetTexture = null -> Screen " + Screen.width + "×" + Screen.height + " · size " +
                  Format(CameraSize(), "0.00")
                : "camera.targetTexture = " + label + " (" + texture.width + "×" + texture.height + ") · size " +
                  Format(CameraSize(), "0.00"));
        }

        private void SyncControls()
        {
            if (_referenceSlider != null && _scaler != null)
            {
                _referenceSlider.SetValueWithoutNotify(_scaler.ReferenceOrthographicSize);
            }

            UpdateModeHint();
        }

        private void UpdateModeHint()
        {
            if (_modeHint == null || _scaler == null)
            {
                return;
            }

            int mode = (int)_scaler.Mode;
            _modeHint.text = mode >= 0 && mode < ModeHints.Length ? ModeHints[mode] : _scaler.Mode.ToString();
        }

        private void RefreshReadout(bool force)
        {
            if (_camera == null || _scaler == null)
            {
                return;
            }

            RenderTexture target = _camera.targetTexture;
            int width = target != null ? target.width : Screen.width;
            int height = target != null ? target.height : Screen.height;
            bool orthographic = _camera.orthographic;
            float size = _camera.orthographicSize;
            float fov = _camera.fieldOfView;
            Rect rect = _camera.rect;
            float reference = _scaler.ReferenceOrthographicSize;
            Vector2 design = _scaler.TargetResolution;
            CameraAspectRatioScaler.ScaleMode mode = _scaler.Mode;

            // WHY: runs every frame; texts (and their strings) are rebuilt only when a measured value changed.
            if (!force && _readoutValid && width == _lastWidth && height == _lastHeight && target == _lastTarget &&
                orthographic == _lastOrthographic && size == _lastSize && fov == _lastFov && rect == _lastRect &&
                reference == _lastReference && design == _lastDesign && mode == _lastMode)
            {
                return;
            }

            _readoutValid = true;
            _lastWidth = width;
            _lastHeight = height;
            _lastTarget = target;
            _lastOrthographic = orthographic;
            _lastSize = size;
            _lastFov = fov;
            _lastRect = rect;
            _lastReference = reference;
            _lastDesign = design;
            _lastMode = mode;

            float outputAspect = width / (float)Mathf.Max(1, height);
            float designAspect = DesignAspect(design);
            SetText(_outputValue, (target != null ? OutputLabel() + " " : "Screen ") + width + "×" + height);
            SetText(_aspectValue, Format(outputAspect, "0.00") + " / " + Format(designAspect, "0.00"));
            SetText(_viewportValue, DescribeRect(rect));

            if (!orthographic)
            {
                SetText(_sizeValue, "FOV " + Format(fov, "0.0") + "° (ref " +
                                    Format(_scaler.ReferenceFieldOfView, "0.0") + "°)");
                SetText(_visibleValue, "perspective");
                SetText(_frameValue, "-");
                return;
            }

            SetText(_sizeValue, Format(size, "0.00") + " (ref " + Format(reference, "0.0") + ")");
            float viewAspect = width * rect.width / Mathf.Max(1f, height * rect.height);
            float visibleHeight = 2f * size;
            float visibleWidth = visibleHeight * viewAspect;
            SetText(_visibleValue, Format(visibleWidth, "0.0") + " × " + Format(visibleHeight, "0.0") + " units");

            float frameHeight = 2f * reference;
            float frameWidth = frameHeight * designAspect;
            SetText(_frameValue, DescribeFrame(frameWidth - visibleWidth, frameHeight - visibleHeight));
        }

        private static string DescribeFrame(float cropWidth, float cropHeight)
        {
            bool sides = cropWidth > CropTolerance;
            bool topBottom = cropHeight > CropTolerance;
            if (!sides && !topBottom)
            {
                bool extra = cropWidth < -CropTolerance || cropHeight < -CropTolerance;
                return "<color=" + GoodColor + ">all visible</color>" + (extra
                    ? " + " + Format(Mathf.Max(-cropWidth, -cropHeight), "0.0") + " u extra"
                    : string.Empty);
            }

            string cropped = "<color=" + DangerColor + ">cropped</color>";
            if (sides && topBottom)
            {
                return cropped + " sides " + Format(cropWidth, "0.0") + " · top/bottom " + Format(cropHeight, "0.0");
            }

            return sides
                ? cropped + " sides " + Format(cropWidth, "0.0") + " u"
                : cropped + " top/bottom " + Format(cropHeight, "0.0") + " u";
        }

        private static string DescribeRect(Rect rect)
        {
            if (Mathf.Approximately(rect.width, 1f) && Mathf.Approximately(rect.height, 1f))
            {
                return "full (0, 0, 1, 1)";
            }

            return rect.width < 1f
                ? "pillarbox · w " + Format(rect.width, "0.00") + " at x " + Format(rect.x, "0.00")
                : "letterbox · h " + Format(rect.height, "0.00") + " at y " + Format(rect.y, "0.00");
        }

        private string OutputLabel()
        {
            int device = _output - 1;
            return device >= 0 && device < DeviceCount ? _deviceLabels[device] : "Render texture";
        }

        private void SetEdge(int index, Vector3 position, Vector2 size)
        {
            if (_frameEdges == null || index >= _frameEdges.Length || _frameEdges[index] == null)
            {
                return;
            }

            SpriteRenderer edge = _frameEdges[index];
            edge.transform.localPosition = position;
            edge.size = size;
        }

        private void SetCorner(int index, Vector3 position, float size)
        {
            if (_frameCorners == null || index >= _frameCorners.Length || _frameCorners[index] == null)
            {
                return;
            }

            Transform corner = _frameCorners[index];
            corner.localPosition = position;
            corner.localScale = Vector3.one * size;
        }

        private static void PlaceLabel(TMP_Text label, SpriteRenderer plate, string text, float y, float width,
            float height)
        {
            float labelWidth = width * LabelWidthShare;
            float labelHeight = height * LabelInsetPerHeight * 1.5f;
            if (label != null)
            {
                float fontSize = height * LabelFontPerHeight;
                label.text = text;
                label.enableAutoSizing = true;
                label.fontSizeMax = fontSize;
                label.fontSizeMin = fontSize * 0.4f;
                label.fontSize = fontSize;
                label.rectTransform.localPosition = new Vector3(0f, y, 0f);
                label.rectTransform.sizeDelta = new Vector2(labelWidth, labelHeight);
            }

            if (plate != null)
            {
                plate.transform.localPosition = label != null ? Vector3.zero : new Vector3(0f, y, 0f);
                plate.size = new Vector2(labelWidth + PlatePadding, labelHeight);
            }
        }

        private float CameraSize()
        {
            return _camera != null ? _camera.orthographicSize : 0f;
        }

        private static float DesignAspect(Vector2 design)
        {
            return Mathf.Max(1f, design.x) / Mathf.Max(1f, design.y);
        }

        private static string DesignName(Vector2 design)
        {
            if (Mathf.Approximately(design.x, design.y))
            {
                return "SQUARE";
            }

            return design.x > design.y ? "LANDSCAPE" : "PORTRAIT";
        }

        private static string Resolution(Vector2 resolution)
        {
            return Format(resolution.x, "0") + "×" + Format(resolution.y, "0");
        }

        private static string Format(float value, string format)
        {
            return value.ToString(format, CultureInfo.InvariantCulture);
        }

        private static void SetText(TMP_Text label, string text)
        {
            if (label != null)
            {
                label.text = text;
            }
        }

        private void Log(string message)
        {
            if (_log != null)
            {
                _log.Log(message);
            }
        }

        private void WarnUnbuilt()
        {
            if (_warnedUnbuilt)
            {
                return;
            }

            _warnedUnbuilt = true;
            global::Neo.Demo.SampleDiagnostics.Warn(
                "[CameraAspectRatioDemoController] The scene is not authored. Re-import the scene from the Demo download.",
                this);
        }
    }
}
