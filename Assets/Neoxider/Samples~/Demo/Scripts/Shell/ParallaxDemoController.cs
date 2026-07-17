using System.Reflection;
using Neo.Samples.Survivor;
using UnityEngine;

namespace Neo.Samples
{
    /// <summary>
    ///     Bright, self-contained demo for <b>Neo.Parallax</b>. Four procedural depth bands — far mountains, mid
    ///     hills, near hills and a foreground ridge — are built from tinted <see cref="SurvivorArt" /> discs, each
    ///     driven by a real <see cref="Neo.ParallaxLayer" /> that tiles and recycles seamlessly against the camera.
    ///     Each band has a different parallax multiplier, so when the demo auto-pans the camera left-right the
    ///     bands slide at different apparent speeds and read as depth. A slider sets the pan speed and "Reverse"
    ///     flips direction. Robust in an empty scene.
    /// </summary>
    [AddComponentMenu("Neoxider/Demos/Parallax Demo")]
    public sealed class ParallaxDemoController : MonoBehaviour
    {
        private const float PanRange = 7f;

        private NeoDemoShell.Context _shell;
        private Camera _cam;
        private float _camX;
        private float _panSpeed = 1.5f;
        private int _dir = 1;

        private void Start()
        {
            _shell = NeoDemoShell.Build("Neo.Parallax", new Color(1f, 0.60f, 0.20f));
            HideBackdrop();

            NeoDemoShell.ShowInfoCardOnce(
                "Neo.Parallax",
                "Four ParallaxLayer bands tile and recycle as the camera pans; deeper bands scroll slower.",
                "Each band has its own parallax multiplier (logged below)",
                "Speed slider drives the auto camera pan; Reverse flips it",
                "Layers are procedural discs — no imported art");

            _cam = Camera.main;

            // WHY: back-to-front — smaller multiplier = deeper = slower apparent scroll.
            BuildLayer("Far mountains", new Color(0.31f, 0.35f, 0.53f), 0.9f, 4.6f, 3.4f, 0.05f, -40);
            BuildLayer("Mid hills", new Color(0.24f, 0.46f, 0.55f), -1.5f, 3.3f, 2.5f, 0.28f, -30);
            BuildLayer("Near hills", new Color(0.30f, 0.56f, 0.43f), -2.9f, 2.5f, 2.1f, 0.52f, -20);
            BuildLayer("Foreground", new Color(0.44f, 0.37f, 0.27f), -4.3f, 1.9f, 1.7f, 0.80f, -10);

            _shell.AddSlider("Camera pan speed", 0f, 4f, _panSpeed, v => _panSpeed = v);
            _shell.AddButtonRow(("Reverse", Reverse));

            _shell.Log("Parallax multipliers: far 0.05 · mid 0.28 · near 0.52 · fg 0.80");
        }

        private void Update()
        {
            if (_cam == null)
            {
                return;
            }

            // WHY: ping-pong pan; ParallaxLayer.LateUpdate reads the camera delta and offsets each band.
            _camX += _dir * _panSpeed * Time.deltaTime;
            if (_camX > PanRange)
            {
                _camX = PanRange;
                _dir = -1;
            }
            else if (_camX < -PanRange)
            {
                _camX = -PanRange;
                _dir = 1;
            }

            Vector3 p = _cam.transform.position;
            _cam.transform.position = new Vector3(_camX, p.y, p.z);
        }

        private void Reverse()
        {
            _dir = -_dir;
            _shell.Log(_dir > 0 ? "Pan → right" : "Pan → left");
        }

        private void BuildLayer(string label, Color color, float y, float sizeX, float sizeY,
            float multiplier, int order)
        {
            // WHY: scaler parent carries the size; the ParallaxLayer child stays at scale 1 so its tiles size and
            // space by the parent's lossy scale (no double-scaling of the recycled tiles).
            var scaler = new GameObject(label);
            scaler.transform.SetParent(transform, false);
            scaler.transform.position = new Vector3(0f, y, 0f);
            scaler.transform.localScale = new Vector3(sizeX, sizeY, 1f);

            var layerGo = new GameObject(label + " Layer");
            layerGo.transform.SetParent(scaler.transform, false);
            layerGo.transform.localPosition = Vector3.zero;
            layerGo.transform.localScale = Vector3.one;

            var sr = layerGo.AddComponent<SpriteRenderer>();
            sr.sprite = SurvivorArt.Disc;
            sr.color = color;
            sr.sortingOrder = order; // copied onto every recycled tile

            // WHY: Awake → Initialise runs here and resolves Camera.main; set the multiplier after (read live each frame).
            var layer = layerGo.AddComponent<Neo.ParallaxLayer>();
            SetPrivateField(layer, "parallaxMultiplier", new Vector2(multiplier, 0f));

            _shell.Log($"{label}: ParallaxLayer ×{multiplier:0.00}");
        }

        private static void SetPrivateField(object target, string field, object value)
        {
            FieldInfo info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            info?.SetValue(target, value);
        }

        private void HideBackdrop()
        {
            // WHY: the scrolling landscape IS the background here, so fully hide the shell's opaque gradient and let
            // the camera clear color read as sky behind the world-space parallax bands.
            Transform backdrop = _shell.Canvas != null ? _shell.Canvas.transform.Find("Backdrop") : null;
            if (backdrop != null && backdrop.TryGetComponent(out UnityEngine.UI.Image img))
            {
                img.enabled = false;
            }
        }
    }
}
