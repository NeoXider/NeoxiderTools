using System;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Neo.Tools
{
    /// <summary>
    ///     Drop-in on-screen debug panel rendered with IMGUI.
    ///     Shows FPS, frame time, active scene, time scale, and known manager states (AM, SaveManager).
    ///     Manager state is read via reflection so this overlay carries no assembly dependency on the
    ///     Audio / Save modules (avoids circular asmdef references). Toggle visibility with
    ///     <see cref="_toggleKey"/> (default F3). No scene or prefab dependencies.
    /// </summary>
    [AddComponentMenu("Neoxider/Tools/Debug/" + nameof(NeoDebugOverlay))]
    public class NeoDebugOverlay : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

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

        [Tooltip("Opacity of the dark background box (0 = transparent, 1 = opaque).")]
        [SerializeField] [Range(0f, 1f)] private float _backgroundAlpha = 0.6f;

        // ── FPS smoothing (no per-frame allocation) ───────────────────────────

        private const float SmoothFactor = 0.1f;   // exponential moving average weight
        private const float MinDeltaTime = 1e-5f;

        private float _smoothFps;
        private float _smoothMs;

        // ── Visibility ────────────────────────────────────────────────────────

        private bool _visible;

        // ── IMGUI style (created once) ────────────────────────────────────────

        private GUIStyle _boxStyle;
        private GUIStyle _labelStyle;
        private bool _stylesReady;

        // ── Manager reflection cache (resolved once, no hard asmdef deps) ──────

        private bool _managerReflectionReady;
        private PropertyInfo _amInstanceProp;
        private PropertyInfo _amMusicProp;
        private MethodInfo _amIsRandomMethod;
        private MethodInfo _amGetClipMethod;
        private PropertyInfo _saveHasInstanceProp;
        private PropertyInfo _saveIsLoadProp;

        // ── Unity messages ────────────────────────────────────────────────────

        private void Awake()
        {
            _visible = _startVisible;
        }

        private void Update()
        {
            // Toggle
            if (Input.GetKeyDown(_toggleKey))
            {
                _visible = !_visible;
            }

            // Smooth FPS — no string building here
            float dt = Time.unscaledDeltaTime;
            if (dt >= MinDeltaTime)
            {
                float instantFps = 1f / dt;
                float instantMs  = dt * 1000f;

                if (_smoothFps <= 0f)
                {
                    // First frame: seed the smoother
                    _smoothFps = instantFps;
                    _smoothMs  = instantMs;
                }
                else
                {
                    _smoothFps = Mathf.Lerp(_smoothFps, instantFps, SmoothFactor);
                    _smoothMs  = Mathf.Lerp(_smoothMs,  instantMs,  SmoothFactor);
                }
            }
        }

        private void OnGUI()
        {
            if (!_visible)
            {
                return;
            }

            EnsureStyles();

            // ── Build content string (OnGUI allocations are fine) ──────────

            StringBuilder sb = new StringBuilder(256);

            if (_showFps)
            {
                sb.AppendLine(string.Format("FPS  {0:F1}   ({1:F2} ms)", _smoothFps, _smoothMs));
            }

            if (_showScene)
            {
                Scene scene = SceneManager.GetActiveScene();
                sb.AppendLine(string.Format("Scene  {0}  [#{1}]", scene.name, scene.buildIndex));
                sb.AppendLine(string.Format("TimeScale  {0:F2}", Time.timeScale));
            }

            if (_showManagers)
            {
                AppendManagerInfo(sb);
            }

            // ── Render ─────────────────────────────────────────────────────

            string content = sb.ToString();

            // Measure the text to size the box
            Vector2 textSize = _labelStyle.CalcSize(new GUIContent(content));
            float padding = 10f;
            Rect boxRect = new Rect(8f, 8f, textSize.x + padding * 2f, textSize.y + padding * 2f);

            GUI.Box(boxRect, GUIContent.none, _boxStyle);

            Rect labelRect = new Rect(boxRect.x + padding, boxRect.y + padding,
                                      textSize.x, textSize.y);
            GUI.Label(labelRect, content, _labelStyle);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void AppendManagerInfo(StringBuilder sb)
        {
            EnsureManagerReflection();

            sb.AppendLine("── Managers ──");

            // AM (Audio Manager) — resolved via reflection; instance read through its static "I".
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

                sb.AppendLine(string.Format("AM  music={0}  random={1}  clip={2}",
                    musicPlaying ? "playing" : "stopped",
                    randomMusic  ? "on"      : "off",
                    clipName));
            }
            else
            {
                sb.AppendLine("AM  —");
            }

            // SaveManager — static HasInstance / IsLoad read via reflection.
            bool hasSave = _saveHasInstanceProp != null && (bool)_saveHasInstanceProp.GetValue(null);
            if (hasSave)
            {
                bool loaded = _saveIsLoadProp != null && (bool)_saveIsLoadProp.GetValue(null);
                sb.AppendLine(string.Format("SaveManager  loaded={0}", loaded ? "yes" : "no"));
            }
            else
            {
                sb.AppendLine("SaveManager  —");
            }
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

        private void EnsureStyles()
        {
            if (_stylesReady)
            {
                return;
            }

            _stylesReady = true;

            // Background box
            Texture2D bgTex = new Texture2D(1, 1);
            bgTex.SetPixel(0, 0, new Color(0f, 0f, 0f, _backgroundAlpha));
            bgTex.Apply();

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = bgTex }
            };

            // Label
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = _fontSize,
                alignment = TextAnchor.UpperLeft,
                richText  = false,
                wordWrap  = false,
                normal    = { textColor = Color.white }
            };
        }
    }
}
