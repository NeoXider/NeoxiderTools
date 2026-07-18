using System;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Neo.Tools
{
    /// <summary>
    ///     Drop-in on-screen debug panel rendered with a runtime-built uGUI canvas and TextMeshPro.
    ///     Shows FPS, frame time, active scene, time scale, and known manager states (AM, SaveManager).
    ///     Manager state is read via reflection so this overlay carries no assembly dependency on the
    ///     Audio / Save modules (avoids circular asmdef references). Toggle visibility with
    ///     <see cref="_toggleKey"/> (default F3). No scene or prefab dependencies.
    /// </summary>
    [NeoDoc("Tools/Debug/NeoDebugOverlay.md")]
    [CreateFromMenu("Neoxider/Tools/Debug/NeoDebugOverlay")]
    [AddComponentMenu("Neoxider/Tools/Debug/" + nameof(NeoDebugOverlay))]
    public class NeoDebugOverlay : MonoBehaviour
    {
        [Header("Toggle")]
        [Tooltip("Key that shows / hides the overlay at runtime.")]
        [SerializeField] private KeyCode _toggleKey = KeyCode.F3;

        [Tooltip("Whether the overlay is visible when Play starts.")]
        [SerializeField] private bool _startVisible = true;

        [Header("Sections")]
        [SerializeField] private bool _showFps      = true;
        [SerializeField] private bool _showScene    = true;
        [SerializeField] private bool _showManagers = true;

        [Header("Style")]
        [Tooltip("Font size used in the overlay.")]
        [SerializeField] [Range(10, 32)] private int _fontSize = 14;

        [Tooltip("Opacity of the dark background panel (0 = transparent, 1 = opaque).")]
        [SerializeField] [Range(0f, 1f)] private float _backgroundAlpha = 0.6f;

        [Tooltip("Screen corner the panel is anchored to.")]
        [SerializeField] private Corner _anchorCorner = Corner.TopLeft;

        [Tooltip("Optional TMP font override. When empty the TMP default font is used " +
                 "(the readout is forced to fixed-width glyph advance either way).")]
        [SerializeField] private TMP_FontAsset _font;

        [Header("Update")]
        [Tooltip("How often the readout text is rebuilt, in seconds. " +
                 "FPS smoothing still runs every frame; only the string is throttled.")]
        [SerializeField] [Range(0.05f, 2f)] private float _updateInterval = 0.25f;

        private const float SmoothFactor = 0.1f;   // WHY: exponential moving average weight
        private const float MinDeltaTime = 1e-5f;
        private const float ScreenMargin = 8f;
        private const int   PanelPadding = 10;
        private const int   SortOrder    = 32000;  // WHY: near the top of the canvas sorting range
        private const float OutlineWidth = 0.2f;   // WHY: TMP outline for readability on any background

        /// <summary>Forces fixed glyph advance (monospace feel) and literal text after it.</summary>
        private const string MonoPrefix = "<mspace=0.6em><noparse>";

        private float _smoothFps;
        private float _smoothMs;

        private bool _visible;
        private float _nextTextUpdate;

        private GameObject _root;
        private RectTransform _panelRect;
        private Image _panelImage;
        private TextMeshProUGUI _text;
        private readonly StringBuilder _sb = new StringBuilder(256);

        private bool _managerReflectionReady;
        private PropertyInfo _amInstanceProp;
        private PropertyInfo _amMusicProp;
        private MethodInfo _amIsRandomMethod;
        private MethodInfo _amGetClipMethod;
        private PropertyInfo _saveHasInstanceProp;
        private PropertyInfo _saveIsLoadProp;

        private void Awake()
        {
            _visible = _startVisible;
            BuildUi();
            _root.SetActive(false); // WHY: OnEnable applies the real state
        }

        private void OnEnable()
        {
            if (_root != null)
            {
                _root.SetActive(_visible);
                _nextTextUpdate = 0f; // WHY: repaint on the next Update
            }
        }

        private void OnDisable()
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_root != null)
            {
                Destroy(_root);
                _root = null;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(_toggleKey))
            {
                _visible = !_visible;

                if (_root != null)
                {
                    _root.SetActive(_visible);
                }

                _nextTextUpdate = 0f;
            }

            // WHY: Smooth FPS — no string building here
            float dt = Time.unscaledDeltaTime;
            if (dt >= MinDeltaTime)
            {
                float instantFps = 1f / dt;
                float instantMs  = dt * 1000f;

                if (_smoothFps <= 0f)
                {
                    // WHY: First frame: seed the smoother
                    _smoothFps = instantFps;
                    _smoothMs  = instantMs;
                }
                else
                {
                    _smoothFps = Mathf.Lerp(_smoothFps, instantFps, SmoothFactor);
                    _smoothMs  = Mathf.Lerp(_smoothMs,  instantMs,  SmoothFactor);
                }
            }

            // WHY: Throttled readout rebuild (the only place strings are touched)
            if (_visible && _root != null && Time.unscaledTime >= _nextTextUpdate)
            {
                _nextTextUpdate = Time.unscaledTime + Mathf.Max(0.05f, _updateInterval);
                RebuildText();
            }
        }

        private void OnValidate()
        {
            if (_root != null)
            {
                ApplyStyle();
            }
        }

        private void BuildUi()
        {
            if (_root != null)
            {
                return;
            }

            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer < 0)
            {
                uiLayer = 0;
            }

            _root = new GameObject("[NeoDebugOverlay]");
            _root.layer = uiLayer;
            _root.hideFlags = HideFlags.DontSave;
            DontDestroyOnLoad(_root);

            Canvas canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortOrder;

            CanvasScaler scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            GameObject panelGo = new GameObject("Panel");
            panelGo.layer = uiLayer;
            panelGo.transform.SetParent(_root.transform, false);

            _panelImage = panelGo.AddComponent<Image>();
            _panelImage.raycastTarget = false;

            VerticalLayoutGroup layout = panelGo.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(PanelPadding, PanelPadding, PanelPadding, PanelPadding);
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = panelGo.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _panelRect = (RectTransform)panelGo.transform;

            GameObject textGo = new GameObject("Readout");
            textGo.layer = uiLayer;
            textGo.transform.SetParent(panelGo.transform, false);

            _text = textGo.AddComponent<TextMeshProUGUI>();
            _text.raycastTarget = false;
            _text.richText = true; // WHY: needed for the <mspace> monospace prefix
            _text.textWrappingMode = TextWrappingModes.NoWrap;
            _text.alignment = TextAlignmentOptions.TopLeft;

            ApplyStyle();
        }

        private void ApplyStyle()
        {
            if (_panelImage != null)
            {
                _panelImage.color = new Color(0f, 0f, 0f, _backgroundAlpha);
            }

            if (_text != null)
            {
                if (_font != null)
                {
                    _text.font = _font;
                }

                _text.fontSize = _fontSize;
                _text.color = Color.white;
                _text.outlineWidth = OutlineWidth;
                _text.outlineColor = new Color32(0, 0, 0, 255);
            }

            if (_panelRect != null)
            {
                Vector2 anchor = GetCornerAnchor(_anchorCorner);
                _panelRect.anchorMin = anchor;
                _panelRect.anchorMax = anchor;
                _panelRect.pivot = anchor;
                _panelRect.anchoredPosition = new Vector2(
                    anchor.x < 0.5f ? ScreenMargin : -ScreenMargin,
                    anchor.y < 0.5f ? ScreenMargin : -ScreenMargin);
            }
        }

        private static Vector2 GetCornerAnchor(Corner corner)
        {
            switch (corner)
            {
                case Corner.TopRight:    return new Vector2(1f, 1f);
                case Corner.BottomLeft:  return new Vector2(0f, 0f);
                case Corner.BottomRight: return new Vector2(1f, 0f);
                default:                 return new Vector2(0f, 1f);
            }
        }

        private void RebuildText()
        {
            StringBuilder sb = _sb;
            sb.Length = 0;
            sb.Append(MonoPrefix);

            if (_showFps)
            {
                sb.Append("FPS  ");
                AppendFloat(sb, _smoothFps, 1);
                sb.Append("   (");
                AppendFloat(sb, _smoothMs, 2);
                sb.Append(" ms)\n");
            }

            if (_showScene)
            {
                Scene scene = SceneManager.GetActiveScene();
                sb.Append("Scene  ").Append(scene.name).Append("  [#");
                AppendInt(sb, scene.buildIndex);
                sb.Append("]\n");
                sb.Append("TimeScale  ");
                AppendFloat(sb, Time.timeScale, 2);
                sb.Append('\n');
            }

            if (_showManagers)
            {
                AppendManagerInfo(sb);
            }

            // WHY: Trim the trailing newline so the panel does not reserve an empty line.
            if (sb.Length > 0 && sb[sb.Length - 1] == '\n')
            {
                sb.Length -= 1;
            }

            _text.SetText(sb);
        }

        private void AppendManagerInfo(StringBuilder sb)
        {
            EnsureManagerReflection();

            sb.Append("— Managers —\n");

            // WHY: AM (Audio Manager) resolved via reflection; instance read through its static "I".
            UnityEngine.Object amObj = _amInstanceProp != null
                ? _amInstanceProp.GetValue(null) as UnityEngine.Object
                : null;

            if (amObj != null)
            {
                object am = amObj;
                AudioSource music = _amMusicProp != null ? _amMusicProp.GetValue(am) as AudioSource : null;
                bool musicPlaying = music != null && music.isPlaying;
                bool randomMusic  = _amIsRandomMethod != null && (bool)_amIsRandomMethod.Invoke(am, null);
                AudioClip currentClip = _amGetClipMethod != null ? _amGetClipMethod.Invoke(am, null) as AudioClip : null;
                string clipName = currentClip != null ? currentClip.name : "—";

                sb.Append("AM  music=").Append(musicPlaying ? "playing" : "stopped")
                  .Append("  random=").Append(randomMusic ? "on" : "off")
                  .Append("  clip=").Append(clipName).Append('\n');
            }
            else
            {
                sb.Append("AM  —\n");
            }

            // WHY: SaveManager state read via reflection to avoid an assembly dependency.
            bool hasSave = _saveHasInstanceProp != null && (bool)_saveHasInstanceProp.GetValue(null);
            if (hasSave)
            {
                bool loaded = _saveIsLoadProp != null && (bool)_saveIsLoadProp.GetValue(null);
                sb.Append("SaveManager  loaded=").Append(loaded ? "yes" : "no").Append('\n');
            }
            else
            {
                sb.Append("SaveManager  —\n");
            }
        }

        /// <summary>Appends <paramref name="value"/> with fixed decimals (F-style), no boxing / ToString.</summary>
        private static void AppendFloat(StringBuilder sb, float value, int decimals)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                sb.Append('0');
                return;
            }

            if (value < 0f)
            {
                sb.Append('-');
                value = -value;
            }

            long scale = 1;
            for (int i = 0; i < decimals; i++)
            {
                scale *= 10;
            }

            long scaled = (long)Math.Round(value * (double)scale);
            AppendDigits(sb, scaled / scale);

            if (decimals > 0)
            {
                sb.Append('.');
                long frac = scaled % scale;
                for (long div = scale / 10; div > 0; div /= 10)
                {
                    sb.Append((char)('0' + (int)(frac / div % 10)));
                }
            }
        }

        private static void AppendInt(StringBuilder sb, int value)
        {
            if (value < 0)
            {
                sb.Append('-');
                AppendDigits(sb, -(long)value);
            }
            else
            {
                AppendDigits(sb, value);
            }
        }

        private static void AppendDigits(StringBuilder sb, long value)
        {
            if (value >= 10)
            {
                AppendDigits(sb, value / 10);
            }

            sb.Append((char)('0' + (int)(value % 10)));
        }

        private void EnsureManagerReflection()
        {
            if (_managerReflectionReady)
            {
                return;
            }

            _managerReflectionReady = true;

            Type amType = ResolveType("Neo.Audio.AM");
            if (amType != null)
            {
                _amInstanceProp = amType.GetProperty("I",
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                _amMusicProp = amType.GetProperty("Music", BindingFlags.Public | BindingFlags.Instance);
                _amIsRandomMethod = amType.GetMethod("IsRandomMusicEnabled",
                    BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                _amGetClipMethod = amType.GetMethod("GetCurrentMusicClip",
                    BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
            }

            Type saveType = ResolveType("Neo.Save.SaveManager");
            if (saveType != null)
            {
                _saveHasInstanceProp = saveType.GetProperty("HasInstance",
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                _saveIsLoadProp = saveType.GetProperty("IsLoad",
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            }
        }

        private static Type ResolveType(string fullName)
        {
            Type t = Type.GetType(fullName);
            if (t != null)
            {
                return t;
            }

            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                t = asm.GetType(fullName);
                if (t != null)
                {
                    return t;
                }
            }

            return null;
        }

        /// <summary>Screen corner the panel is anchored to.</summary>
        private enum Corner
        {
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }
    }
}
