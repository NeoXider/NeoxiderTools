using Neo.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Samples
{
    /// <summary>
    ///     Controller of the authored <b>CameraConstraint</b> demo (Scenes/Tools/CameraConstraint.unity). The
    ///     hand-authored Main Camera keeps its CameraConstraint and MouseMover2D; the CameraConstraintBuilder saves
    ///     the UI and wires the edge padding slider straight to <see cref="CameraConstraint.SetEdgePadding" /> (the
    ///     NoCode path). This component zooms the camera, toggles the clamped axes through
    ///     <see cref="CameraConstraint.SetAxisConstraint" />, jumps the camera past the edges to show the clamp and
    ///     mirrors the constraint state into labels. It builds no UI at runtime.
    /// </summary>
    [AddComponentMenu("Neoxider/Demos/Camera Constraint Demo")]
    public sealed class CameraConstraintDemoController : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private Camera _camera;
        [SerializeField] private CameraConstraint _constraint;

        [Tooltip("World extent used by the jump buttons; far outside the bounds so the clamp is visible.")]
        [SerializeField] [Min(1f)] private float _jumpDistance = 200f;

        [Header("Controls synced on start")]
        [SerializeField] private Slider _zoomSlider;
        [SerializeField] private Toggle _clampXToggle;
        [SerializeField] private Toggle _clampYToggle;

        [Header("Live values")]
        [SerializeField] private TMP_Text _positionValue;
        [SerializeField] private TMP_Text _rangeValue;
        [SerializeField] private TMP_Text _edgeValue;

        [Header("Feed")]
        [SerializeField] private DemoLogFeed _log;

        [SerializeField] [Min(0.02f)] private float _refreshInterval = 0.1f;

        private bool _clampX = true;
        private bool _clampY = true;
        private float _nextRefresh;
        private bool _warnedUnbuilt;

        /// <summary>True when the authored scene references are assigned.</summary>
        public bool IsSceneBuilt => _camera != null && _constraint != null && _log != null && _positionValue != null;

        public CameraConstraint Constraint => _constraint;

        private void Start()
        {
            if (!IsSceneBuilt)
            {
                WarnUnbuilt();
            }

            _clampX = _clampXToggle == null || _clampXToggle.isOn;
            _clampY = _clampYToggle == null || _clampYToggle.isOn;
            if (_zoomSlider != null && _camera != null)
            {
                _zoomSlider.SetValueWithoutNotify(_camera.orthographicSize);
            }

            Refresh();
            Log("CameraConstraint live: drag in the scene to move Main Camera");
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextRefresh)
            {
                return;
            }

            _nextRefresh = Time.unscaledTime + _refreshInterval;
            Refresh();
        }

        /// <summary>Slider target: orthographic size of the camera; the constraint recalculates its bounds.</summary>
        public void SetZoom(float orthographicSize)
        {
            if (_camera == null)
            {
                return;
            }

            _camera.orthographicSize = Mathf.Max(0.5f, orthographicSize);
            if (_constraint != null)
            {
                _constraint.UpdateBounds();
            }
        }

        /// <summary>Toggle target: clamp the X axis (CameraConstraint.SetAxisConstraint).</summary>
        public void SetClampX(bool enabled)
        {
            _clampX = enabled;
            ApplyAxes();
        }

        /// <summary>Toggle target: clamp the Y axis (CameraConstraint.SetAxisConstraint).</summary>
        public void SetClampY(bool enabled)
        {
            _clampY = enabled;
            ApplyAxes();
        }

        /// <summary>
        ///     Button target: moves the camera far past the bounds (1 = up, -1 = down, 0 = back to the centre); with the
        ///     axis clamped, CameraConstraint pulls it back to the edge in the same frame.
        /// </summary>
        public void JumpVertical(int direction)
        {
            if (_camera == null)
            {
                return;
            }

            Transform cameraTransform = _camera.transform;
            Vector3 position = cameraTransform.position;
            int sign = direction > 0 ? 1 : direction < 0 ? -1 : 0;
            position.x = sign == 0 ? 0f : position.x;
            position.y = sign * _jumpDistance;
            cameraTransform.position = position;
            Log(sign == 0
                ? "Camera moved to the centre"
                : "Camera moved to y = " + position.y.ToString("0") + "; LateUpdate clamps it to the edge");
        }

        private void ApplyAxes()
        {
            if (_constraint == null)
            {
                return;
            }

            _constraint.SetAxisConstraint(_clampX, _clampY, false);
            Log("CameraConstraint.SetAxisConstraint(x: " + _clampX + ", y: " + _clampY + ", z: false)");
        }

        private void Refresh()
        {
            if (_camera != null && _positionValue != null)
            {
                Vector3 position = _camera.transform.position;
                _positionValue.SetText("x {0:1}   y {1:1}", position.x, position.y);
            }

            if (_constraint == null)
            {
                return;
            }

            _constraint.GetConstraintBounds(out Vector3 min, out Vector3 max);
            if (_rangeValue != null)
            {
                _rangeValue.SetText("x {0:1}..{1:1}   y {2:1}..{3:1}", min.x, max.x, min.y, max.y);
            }

            if (_edgeValue != null)
            {
                _constraint.IsAtEdge(out bool minX, out bool maxX, out bool minY, out bool maxY);
                // WHY: when the view is wider than the bounds, CameraConstraint pins X to the centre, so both X
                // "edges" match permanently; only report X edges when there is room to move.
                bool xHasRoom = _clampX && max.x - min.x > 0.01f;
                bool yHasRoom = _clampY && max.y - min.y > 0.01f;
                string edge = yHasRoom && maxY ? "top edge"
                    : yHasRoom && minY ? "bottom edge"
                    : xHasRoom && minX ? "left edge"
                    : xHasRoom && maxX ? "right edge"
                    : _clampX && !xHasRoom ? "inside · X pinned"
                    : "inside";
                _edgeValue.SetText(edge);
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
                "[CameraConstraintDemoController] The scene UI is not authored. Re-import the scene from the Demo download.",
                this);
        }
    }
}
