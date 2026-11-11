using System;
using System.Collections.Generic;
using Neo.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Samples
{
    /// <summary>
    ///     Controller of the authored <b>Follow</b> demo (Scenes/Tools/Move/Follow.unity). The scene, its followers,
    ///     the MouseMover2D on the Target and every UI row are saved by the FollowBuilder; the speed slider, the
    ///     deadzone toggle and the Teleport button call <see cref="Follow" /> directly through persistent
    ///     UnityEvents (the NoCode path). This component only adds the glue a scene cannot express: an idle orbit
    ///     for the Target, the smoothing dropdown (enum argument), drop/restore of the target, a layout reset and the
    ///     live status rows. It builds no UI at runtime.
    /// </summary>
    [AddComponentMenu("Neoxider/Demos/Follow Demo")]
    public sealed class FollowDemoController : MonoBehaviour
    {
        /// <summary>Dropdown option 0 restores the modes authored on each follower.</summary>
        public const int SceneDefaultsOption = 0;

        [Header("Scene")]
        [SerializeField] private Transform _target;

        [Tooltip("Mouse driver of the Target; while the button is held it owns the Target and the orbit pauses.")]
        [SerializeField] private MouseMover2D _targetMover;

        [SerializeField] private Follow[] _followers = Array.Empty<Follow>();

        [Tooltip("Position smooth mode authored on each follower (parallel to Followers), restored by option 0.")]
        [SerializeField] private int[] _defaultSmoothModes = Array.Empty<int>();

        [Tooltip("Live status label of each follower (parallel to Followers).")]
        [SerializeField] private TMP_Text[] _statusLabels = Array.Empty<TMP_Text>();

        [Header("Idle orbit")]
        [SerializeField] private bool _autoOrbit = true;
        [SerializeField] private Vector2 _orbitCenter = new(-0.4f, 0.5f);
        [SerializeField] private Vector2 _orbitRadius = new(3.4f, 1.9f);

        [Tooltip("Angular speed of the figure-eight path in radians per second.")]
        [SerializeField] [Min(0.01f)] private float _orbitSpeed = 0.55f;

        [Tooltip("Units per second the Target moves back onto the path after the mouse released it.")]
        [SerializeField] [Min(0.1f)] private float _returnSpeed = 6f;

        [SerializeField] private Toggle _autoOrbitToggle;

        [Header("Feed")]
        [SerializeField] private DemoLogFeed _log;

        [SerializeField] [Min(0.02f)] private float _statusInterval = 0.1f;

        private readonly List<Vector3> _startPositions = new();
        private readonly List<Quaternion> _startRotations = new();
        private Vector3 _targetStart;
        private float _phase;
        private float _nextStatus;
        private bool _warnedUnbuilt;

        /// <summary>True when the authored scene references are assigned.</summary>
        public bool IsSceneBuilt => _target != null && _followers.Length > 0 && _log != null;

        public Transform Target => _target;
        public IReadOnlyList<Follow> Followers => _followers;
        public int FollowerCount => _followers.Length;
        public bool AutoOrbit => _autoOrbit;

        private void Start()
        {
            if (!IsSceneBuilt)
            {
                WarnUnbuilt();
            }

            RecordLayout();
            if (_autoOrbitToggle != null)
            {
                _autoOrbitToggle.SetIsOnWithoutNotify(_autoOrbit);
            }

            RefreshStatus();
            Log("Follow live: " + _followers.Length + " followers chase the Target");
        }

        private void Update()
        {
            MoveTargetAlongOrbit();
            if (Time.unscaledTime >= _nextStatus)
            {
                _nextStatus = Time.unscaledTime + _statusInterval;
                RefreshStatus();
            }
        }

        /// <summary>Toggle target: the Target follows a figure-eight path while the mouse does not drive it.</summary>
        public void SetAutoOrbit(bool enabled)
        {
            _autoOrbit = enabled;
            Log(enabled ? "Idle orbit on" : "Idle orbit off: hold the mouse to move the Target");
        }

        /// <summary>
        ///     Dropdown target: option 0 restores each follower's authored mode, option N applies
        ///     <see cref="Follow.SmoothMode" /> (N - 1) to every follower that follows position.
        /// </summary>
        public void SetPositionSmoothMode(int option)
        {
            int changed = 0;
            for (int i = 0; i < _followers.Length; i++)
            {
                Follow follower = _followers[i];
                if (follower == null || !follower.GetFollowPosition())
                {
                    continue;
                }

                int mode = option == SceneDefaultsOption
                    ? (i < _defaultSmoothModes.Length ? _defaultSmoothModes[i] : (int)Follow.SmoothMode.MoveTowards)
                    : option - 1;
                if (!Enum.IsDefined(typeof(Follow.SmoothMode), mode))
                {
                    continue;
                }

                follower.SetPositionSmoothMode((Follow.SmoothMode)mode);
                changed++;
            }

            string label = option == SceneDefaultsOption
                ? "scene defaults"
                : Enum.IsDefined(typeof(Follow.SmoothMode), option - 1)
                    ? ((Follow.SmoothMode)(option - 1)).ToString()
                    : option.ToString();
            Log("Follow.SetPositionSmoothMode(" + label + ") on " + changed + " followers");
        }

        /// <summary>Button target: clears every follower's target; each one raises onTargetLost next frame.</summary>
        public void DropTarget()
        {
            foreach (Follow follower in _followers)
            {
                if (follower != null)
                {
                    follower.SetTarget(null);
                }
            }

            Log("Follow.SetTarget(null) on every follower");
        }

        /// <summary>Button target: gives every follower the Target again.</summary>
        public void RestoreTarget()
        {
            foreach (Follow follower in _followers)
            {
                if (follower != null)
                {
                    follower.SetTarget(_target);
                }
            }

            Log("Follow.SetTarget(Target) on every follower");
        }

        /// <summary>Button target: puts the followers and the Target back where the scene started.</summary>
        public void ResetLayout()
        {
            int count = Mathf.Min(_followers.Length, _startPositions.Count);
            for (int i = 0; i < count; i++)
            {
                if (_followers[i] != null)
                {
                    _followers[i].transform.SetPositionAndRotation(_startPositions[i], _startRotations[i]);
                }
            }

            if (_target != null)
            {
                _target.position = _targetStart;
            }

            Log("Layout reset to the authored positions");
        }

        private void RecordLayout()
        {
            _startPositions.Clear();
            _startRotations.Clear();
            foreach (Follow follower in _followers)
            {
                Transform node = follower != null ? follower.transform : null;
                _startPositions.Add(node != null ? node.position : Vector3.zero);
                _startRotations.Add(node != null ? node.rotation : Quaternion.identity);
            }

            if (_target != null)
            {
                _targetStart = _target.position;
            }
        }

        private void MoveTargetAlongOrbit()
        {
            if (!_autoOrbit || _target == null || IsMouseDrivingTarget())
            {
                return;
            }

            _phase += _orbitSpeed * Time.deltaTime;
            Vector3 current = _target.position;
            Vector3 goal = new(
                _orbitCenter.x + _orbitRadius.x * Mathf.Sin(_phase),
                _orbitCenter.y + _orbitRadius.y * Mathf.Sin(2f * _phase),
                current.z);
            _target.position = Vector3.MoveTowards(current, goal, _returnSpeed * Time.deltaTime);
        }

        private bool IsMouseDrivingTarget()
        {
            return _targetMover != null && _targetMover.isActiveAndEnabled &&
                   MouseInputCompat.TryGetButton(0, out bool held) && held;
        }

        private void RefreshStatus()
        {
            int count = Mathf.Min(_followers.Length, _statusLabels.Length);
            for (int i = 0; i < count; i++)
            {
                Follow follower = _followers[i];
                TMP_Text label = _statusLabels[i];
                if (follower == null || label == null)
                {
                    continue;
                }

                if (follower.GetTarget() == null)
                {
                    label.SetText("no target");
                }
                else if (follower.IsFollowing())
                {
                    label.SetText("{0:1} u · moves", follower.GetDistanceToTarget());
                }
                else
                {
                    label.SetText("{0:1} u · aims", follower.GetDistanceToTarget());
                }
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
                "[FollowDemoController] The scene UI is not authored. Re-import the scene from the Demo download.",
                this);
        }
    }
}
