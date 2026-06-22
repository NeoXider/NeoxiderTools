using UnityEngine;
using UnityEngine.SceneManagement;
using Neo.Audio;

namespace Neo.Tools
{
    /// <summary>
    ///     Drop-in on-screen debug panel rendered with IMGUI.
    ///     Shows FPS, frame time, active scene, time scale, and known manager states (AM, SaveManager).
    ///     Toggle visibility with <see cref="_toggleKey"/> (default F3). No scene or prefab dependencies.
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

            System.Text.StringBuilder sb = new System.Text.StringBuilder(256);

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

        private void AppendManagerInfo(System.Text.StringBuilder sb)
        {
            sb.AppendLine("── Managers ──");

            // AM (Audio Manager)
            AM am = AM.I;
            if (am != null)
            {
                bool musicPlaying = am.Music != null && am.Music.isPlaying;
                bool randomMusic  = am.IsRandomMusicEnabled();
                AudioClip currentClip = am.GetCurrentMusicClip();
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

            // SaveManager
            if (Neo.Save.SaveManager.HasInstance)
            {
                sb.AppendLine(string.Format("SaveManager  loaded={0}", Neo.Save.SaveManager.IsLoad ? "yes" : "no"));
            }
            else
            {
                sb.AppendLine("SaveManager  —");
            }
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
