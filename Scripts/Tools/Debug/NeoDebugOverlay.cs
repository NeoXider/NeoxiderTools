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
        private Func<KeyCode, bool> _getKeyDown;

        private GameObject _root;
        private RectTransform _panelRect;
        private Image _panelImage;
        private TextMeshProUGUI _text;
        private readonly StringBuilder _sb = new StringBuilder(256);

        private bool _managerReflectionReady;
        private PropertyInfo _amHasInstanceProp;
        private PropertyInfo _amInstanceProp;
        private PropertyInfo _amMusicProp;
        private MethodInfo _amIsRandomMethod;
        private MethodInfo _amGetClipMethod;
        private PropertyInfo _saveHasInstanceProp;
        private PropertyInfo _saveIsLoadProp;
        private Func<bool> _amHasInstance;
        private Func<bool> _saveHasInstance;
        private Func<bool> _saveIsLoad;
        private UnityEngine.Object _amIsRandomTarget;
        private Func<bool> _amIsRandomReader;
        private Func<UnityEngine.Object> _amInstance;
        private UnityEngine.Object _amReadersTarget;
        private Func<AudioSource> _amMusicReader;
        private Func<AudioClip> _amClipReader;
        private AudioClip _cachedClip;
        private string _cachedClipName;

        private Scene _cachedScene;
        private string _cachedSceneName;

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
            if (GetToggleKeyDown())
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

        private bool GetToggleKeyDown()
        {
            if (_getKeyDown == null)
            {
                // WHY: Reuse the package input adapter without making the debug assembly depend on Input.
                Type compatType = ResolveType("Neo.Tools.KeyInputCompat");
                MethodInfo method = compatType?.GetMethod("GetKeyDown", new[] { typeof(KeyCode) });
                _getKeyDown = method != null
                    ? (Func<KeyCode, bool>)Delegate.CreateDelegate(typeof(Func<KeyCode, bool>), method)
                    : Input.GetKeyDown;
            }

            try
            {
                return _getKeyDown(_toggleKey);
            }
            catch (InvalidOperationException)
            {
                // WHY: A stripped-out Input module must not stop the readout in Input System-only projects.
                return false;
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
                // WHY: Scene.name marshals a new string on every call; refresh it only when the active scene changes.
                if (_cachedSceneName == null || scene != _cachedScene)
                {
                    _cachedScene = scene;
                    _cachedSceneName = scene.name;
                }

                sb.Append("Scene  ").Append(_cachedSceneName).Append("  [#");
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

            // WHY: Reading diagnostics must not resolve inactive managers or trigger lazy singleton creation.
            bool hasAm = _amHasInstance != null && _amHasInstance();
            UnityEngine.Object amObj = hasAm ? ReadAmInstance() : null;

            if (amObj != null)
            {
                BindAmReaders(amObj);
                AudioSource music = ReadAmMusic(amObj);
                bool musicPlaying = music != null && music.isPlaying;
                bool randomMusic  = ReadAmIsRandomMusic(amObj);
                string clipName = GetClipName(ReadAmClip(amObj));

                sb.Append("AM  music=").Append(musicPlaying ? "playing" : "stopped")
                  .Append("  random=").Append(randomMusic ? "on" : "off")
                  .Append("  clip=").Append(clipName).Append('\n');
            }
            else
            {
                sb.Append("AM  —\n");
            }

            // WHY: SaveManager state read via reflection to avoid an assembly dependency.
            bool hasSave = _saveHasInstance != null && _saveHasInstance();
            if (hasSave)
            {
                bool loaded = _saveIsLoad != null && _saveIsLoad();
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
                _amHasInstanceProp = amType.GetProperty("HasInstance",
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
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

            // WHY: typed delegates read the flags without boxing a bool on every throttled refresh.
            _amHasInstance = CreateStaticBoolReader(_amHasInstanceProp);
            _saveHasInstance = CreateStaticBoolReader(_saveHasInstanceProp);
            _saveIsLoad = CreateStaticBoolReader(_saveIsLoadProp);
            _amInstance = CreateStaticObjectReader(_amInstanceProp);
        }

        private static Func<UnityEngine.Object> CreateStaticObjectReader(PropertyInfo property)
        {
            MethodInfo getter = property != null && typeof(UnityEngine.Object).IsAssignableFrom(property.PropertyType)
                ? property.GetGetMethod()
                : null;
            if (getter == null || !getter.IsStatic)
            {
                return null;
            }

            // WHY: return-type covariance binds AM.I (an AM) to Func<Object> without the reflection invoke path.
            return (Func<UnityEngine.Object>)Delegate.CreateDelegate(typeof(Func<UnityEngine.Object>), getter, false);
        }

        private UnityEngine.Object ReadAmInstance()
        {
            if (_amInstance != null)
            {
                return _amInstance();
            }

            return _amInstanceProp != null ? _amInstanceProp.GetValue(null) as UnityEngine.Object : null;
        }

        /// <summary>Binds the music and clip readers to <paramref name="am" /> once per manager instance.</summary>
        private void BindAmReaders(UnityEngine.Object am)
        {
            if (ReferenceEquals(am, _amReadersTarget))
            {
                return;
            }

            _amReadersTarget = am;
            MethodInfo musicGetter = _amMusicProp != null && _amMusicProp.PropertyType == typeof(AudioSource)
                ? _amMusicProp.GetGetMethod()
                : null;
            _amMusicReader = musicGetter != null
                ? (Func<AudioSource>)Delegate.CreateDelegate(typeof(Func<AudioSource>), am, musicGetter, false)
                : null;
            _amClipReader = _amGetClipMethod != null && _amGetClipMethod.ReturnType == typeof(AudioClip)
                ? (Func<AudioClip>)Delegate.CreateDelegate(typeof(Func<AudioClip>), am, _amGetClipMethod, false)
                : null;
        }

        private AudioSource ReadAmMusic(UnityEngine.Object am)
        {
            if (_amMusicReader != null)
            {
                return _amMusicReader();
            }

            return _amMusicProp != null ? _amMusicProp.GetValue(am) as AudioSource : null;
        }

        private AudioClip ReadAmClip(UnityEngine.Object am)
        {
            if (_amClipReader != null)
            {
                return _amClipReader();
            }

            return _amGetClipMethod != null ? _amGetClipMethod.Invoke(am, null) as AudioClip : null;
        }

        /// <summary>
        ///     Returns the clip name, reading <c>Object.name</c> (a new native string per call) only when the
        ///     playing clip changes.
        /// </summary>
        private string GetClipName(AudioClip clip)
        {
            if (clip == null)
            {
                _cachedClip = null;
                _cachedClipName = null;
                return "—";
            }

            if (!ReferenceEquals(clip, _cachedClip) || _cachedClipName == null)
            {
                _cachedClip = clip;
                _cachedClipName = clip.name;
            }

            return _cachedClipName;
        }

        private static Func<bool> CreateStaticBoolReader(PropertyInfo property)
        {
            MethodInfo getter = property != null && property.PropertyType == typeof(bool)
                ? property.GetGetMethod()
                : null;
            if (getter == null || !getter.IsStatic)
            {
                return null;
            }

            Func<bool> reader =
                (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), getter, false);
            return reader ?? (() => (bool)property.GetValue(null));
        }

        private bool ReadAmIsRandomMusic(UnityEngine.Object am)
        {
            if (_amIsRandomMethod == null || _amIsRandomMethod.ReturnType != typeof(bool))
            {
                return false;
            }

            if (!ReferenceEquals(am, _amIsRandomTarget))
            {
                _amIsRandomTarget = am;
                _amIsRandomReader =
                    (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), am, _amIsRandomMethod, false);
            }

            return _amIsRandomReader != null
                ? _amIsRandomReader()
                : (bool)_amIsRandomMethod.Invoke(am, null);
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
