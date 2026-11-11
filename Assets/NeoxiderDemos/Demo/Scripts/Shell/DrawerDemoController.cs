using Neo.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Samples
{
    /// <summary>
    ///     Controller of the authored <b>Drawer</b> demo (Scenes/Tools/DrawExample.unity). The hand-authored Drawer,
    ///     Spawner and PoolManager stay as they were; the scene also saves the UI and wires the reactive
    ///     <see cref="Drawer.PointCount" /> / <see cref="Drawer.Distance" /> events straight into value labels, and the
    ///     Undo / Clear / Pause / Resume buttons straight into Drawer and Spawner methods (the NoCode path). This
    ///     component only sets the public Drawer fields that have no setter method (smoothing, width, auto-delete,
    ///     live collider), counts the alive lines and logs. It builds no UI at runtime.
    /// </summary>
    [AddComponentMenu("Neoxider/Demos/Drawer Demo")]
    public sealed class DrawerDemoController : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private Drawer _drawer;

        [Header("Controls synced on start")]
        [SerializeField] private Slider _smoothingSlider;
        [SerializeField] private Slider _widthSlider;
        [SerializeField] private Slider _lifetimeSlider;
        [SerializeField] private Toggle _colliderToggle;

        [Header("Live values")]
        [SerializeField] private TMP_Text _linesValue;

        [Header("Feed")]
        [SerializeField] private DemoLogFeed _log;

        [SerializeField] [Min(0.02f)] private float _refreshInterval = 0.1f;

        private float _nextRefresh;
        private int _shownLines = -1;
        private bool _warnedUnbuilt;

        /// <summary>True when the authored scene references are assigned.</summary>
        public bool IsSceneBuilt => _drawer != null && _log != null && _linesValue != null;

        public Drawer Drawer => _drawer;

        private void Start()
        {
            if (!IsSceneBuilt)
            {
                WarnUnbuilt();
            }

            SyncControls();
            RefreshLines();
            Log("Drawer live: hold the left mouse button and drag in the scene");
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextRefresh)
            {
                return;
            }

            _nextRefresh = Time.unscaledTime + _refreshInterval;
            RefreshLines();
        }

        /// <summary>Slider target: Chaikin passes applied to every following stroke (Drawer.smoothing).</summary>
        public void SetSmoothing(float passes)
        {
            if (_drawer == null)
            {
                return;
            }

            _drawer.smoothing = Mathf.Clamp(Mathf.RoundToInt(passes), 0, 10);
            Log("Drawer.smoothing = " + _drawer.smoothing + " Chaikin passes");
        }

        /// <summary>Slider target: width of the next strokes (Drawer.lineWidth).</summary>
        public void SetLineWidth(float width)
        {
            if (_drawer != null)
            {
                _drawer.lineWidth = Mathf.Max(0.01f, width);
            }
        }

        /// <summary>Slider target: seconds before a finished line is deleted; 0 keeps lines (Drawer.deleteAfterRelease).</summary>
        public void SetDeleteAfter(float seconds)
        {
            if (_drawer == null)
            {
                return;
            }

            _drawer.deleteAfterRelease = Mathf.Max(0f, Mathf.Round(seconds));
            Log(_drawer.deleteAfterRelease > 0f
                ? "Drawer.deleteAfterRelease = " + _drawer.deleteAfterRelease + " s"
                : "Drawer.deleteAfterRelease = 0: lines stay");
        }

        /// <summary>Toggle target: give new strokes a live EdgeCollider2D (Drawer.addCollider).</summary>
        public void SetLiveCollider(bool enabled)
        {
            if (_drawer == null)
            {
                return;
            }

            _drawer.addCollider = enabled;
            Log(enabled ? "Drawer.addCollider = true: balls bounce off new lines" : "Drawer.addCollider = false");
        }

        /// <summary>Drawer.OnLineCreated listener: reports the finished stroke.</summary>
        public void ReportLine(LineRenderer line)
        {
            if (line == null)
            {
                return;
            }

            Log("OnLineCreated: " + line.positionCount + " points rendered");
            RefreshLines();
        }

        private void SyncControls()
        {
            if (_drawer == null)
            {
                return;
            }

            if (_smoothingSlider != null)
            {
                _smoothingSlider.SetValueWithoutNotify(_drawer.smoothing);
            }

            if (_widthSlider != null)
            {
                _widthSlider.SetValueWithoutNotify(_drawer.lineWidth);
            }

            if (_lifetimeSlider != null)
            {
                _lifetimeSlider.SetValueWithoutNotify(_drawer.deleteAfterRelease);
            }

            if (_colliderToggle != null)
            {
                _colliderToggle.SetIsOnWithoutNotify(_drawer.addCollider);
            }
        }

        private void RefreshLines()
        {
            if (_drawer == null || _linesValue == null)
            {
                return;
            }

            int count = _drawer.lines.Count;
            if (count == _shownLines)
            {
                return;
            }

            _shownLines = count;
            _linesValue.SetText("{0}", count);
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
                "[DrawerDemoController] The scene UI is not authored. Re-import the scene from the Demo download.",
                this);
        }
    }
}
