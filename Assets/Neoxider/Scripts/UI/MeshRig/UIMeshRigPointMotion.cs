using UnityEngine;

namespace Neo.UI
{
    /// <summary>
    /// Procedurally animates a mesh-rig point without writing its Transform, so Unity
    /// Animator and authored poses remain independent and safely composable.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIMeshRigPoint))]
    [AddComponentMenu("Neoxider/UI/Mesh Rig Point Motion")]
    [NeoDoc("UI/UIMeshRig.md")]
    public sealed class UIMeshRigPointMotion : MonoBehaviour
    {
        [Header("Animation Preset")]
        [Tooltip("Choosing a preset applies it immediately and starts a live Edit Mode preview.")]
        [SerializeField] private UIMeshRigMotionPreset _preset = UIMeshRigMotionPreset.Custom;

        [Tooltip("Editable position, rotation and scale curves. Used as-is by the Custom preset.")]
        [SerializeField] private UIMeshRigMotionProfile _profile = new UIMeshRigMotionProfile();

        [Header("Timing & Playback")]
        [Tooltip("Starts the motion as soon as the component is enabled.")]
        [SerializeField] private bool _playOnEnable = true;

        [Tooltip("Ignores Time.timeScale, so UI keeps moving while the game is paused.")]
        [SerializeField] private bool _useUnscaledTime = true;

        [Tooltip("Multiplies playback speed.")]
        [Min(0f)] [SerializeField] private float _speed = 1f;

        [Tooltip("Manual cycle offset. Wave already adds phase from each point's position.")]
        [Range(0f, 1f)] [SerializeField] private float _phase;

        [Tooltip("Same seed and point position always produce the same smooth Noise motion.")]
        [SerializeField] private int _seed;

        // WHY: never serialized. As a [SerializeField] the Start Preview button dirtied the scene and the
        // preview survived save, domain reload and Play Mode — a transient editor visualisation must not
        // become project data.
        [System.NonSerialized] private bool _previewInEditMode;

        private UIMeshRigPoint _point;
        private bool _isPlaying;
        private bool _isPaused;
        private float _currentTime;
        private float _lastRealtime;
        private bool _hasAppliedPose;
        private UIMeshRigProceduralPose _currentPose = UIMeshRigProceduralPose.Identity;

#if UNITY_EDITOR
        internal static event System.Action<UIMeshRigPointMotion> EditModePreviewStateChanged;
#endif

        public UIMeshRigMotionPreset Preset => _preset;
        public UIMeshRigMotionProfile Profile => _profile;
        public bool IsPlaying => _isPlaying;
        public bool IsPaused => _isPaused;
        public float CurrentTime => _currentTime;
        public UIMeshRigProceduralPose CurrentPose => _currentPose;

        public bool PlayOnEnable
        {
            get => _playOnEnable;
            set => _playOnEnable = value;
        }

        public bool UseUnscaledTime
        {
            get => _useUnscaledTime;
            set => _useUnscaledTime = value;
        }

        /// <summary>
        /// Transient Edit Mode preview switch. Not serialized: it is reset by domain reload, Play Mode and
        /// scene reload, never saved into a scene or prefab, and turning it off restores the point exactly
        /// as it was. The preview only ever writes the transient procedural pose, never the point Transform.
        /// </summary>
        public bool PreviewInEditMode
        {
            get => _previewInEditMode;
            set
            {
                _previewInEditMode = value;
                if (!Application.isPlaying)
                {
                    if (value)
                    {
                        Restart();
                    }
                    else
                    {
                        Stop();
                    }

#if UNITY_EDITOR
                    EditModePreviewStateChanged?.Invoke(this);
#endif
                }
            }
        }

        public float Speed
        {
            get => _speed;
            set => _speed = Mathf.Max(0f, value);
        }

        public float Phase
        {
            get => _phase;
            set
            {
                _phase = Mathf.Repeat(value, 1f);
                EvaluateAt(_currentTime);
            }
        }

        public int Seed
        {
            get => _seed;
            set
            {
                _seed = value;
                EvaluateAt(_currentTime);
            }
        }

        private void Reset()
        {
            ResolvePoint();
            ApplyPreset(UIMeshRigMotionPreset.Breathe);
        }

        private void OnEnable()
        {
            ResolvePoint();
            _lastRealtime = Time.realtimeSinceStartup;
            if ((Application.isPlaying && _playOnEnable) || (!Application.isPlaying && _previewInEditMode))
            {
                Restart();
            }
            else
            {
                ClearPose();
            }
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && _previewInEditMode)
            {
                _previewInEditMode = false;
                EditModePreviewStateChanged?.Invoke(this);
            }
#endif
            _isPlaying = false;
            _isPaused = false;
            ClearPose();
        }

        private void OnValidate()
        {
            _speed = Mathf.Max(0f, _speed);
            _phase = Mathf.Repeat(_phase, 1f);
            if (_profile == null)
            {
                _profile = new UIMeshRigMotionProfile();
            }

            _profile.Duration = Mathf.Max(0.01f, _profile.Duration);
            ResolvePoint();
            if (isActiveAndEnabled && (!Application.isPlaying && _previewInEditMode))
            {
                EvaluateAt(_currentTime);
            }
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            float deltaTime = GetDeltaTime();
            if (!_isPlaying || _isPaused)
            {
                return;
            }

            _currentTime += Mathf.Max(0f, deltaTime);
            EvaluateAt(_currentTime);
        }

        public void ApplyPreset(UIMeshRigMotionPreset preset)
        {
            _preset = preset;
            if (preset == UIMeshRigMotionPreset.Custom)
            {
                EvaluateAt(_currentTime);
                return;
            }

            UIMeshRigMotionProfile presetProfile = UIMeshRigMotionPresets.Create(preset);
            if (_profile == null)
            {
                _profile = new UIMeshRigMotionProfile();
            }

            _profile.CopyFrom(presetProfile);
            EvaluateAt(_currentTime);
        }

        public void Play()
        {
            ResolvePoint();
            _isPlaying = true;
            _isPaused = false;
            _lastRealtime = Time.realtimeSinceStartup;
            EvaluateAt(_currentTime);
        }

        public void Stop()
        {
            _isPlaying = false;
            _isPaused = false;
            _currentTime = 0f;
            ClearPose();
        }

        public void Pause()
        {
            if (_isPlaying)
            {
                _isPaused = true;
            }
        }

        public void Resume()
        {
            if (!_isPlaying)
            {
                Play();
                return;
            }

            _isPaused = false;
            _lastRealtime = Time.realtimeSinceStartup;
        }

        public void Restart()
        {
            _currentTime = 0f;
            _isPlaying = true;
            _isPaused = false;
            _lastRealtime = Time.realtimeSinceStartup;
            EvaluateAt(_currentTime);
        }

        public void SetTime(float timeSeconds)
        {
            _currentTime = Mathf.Max(0f, timeSeconds);
            EvaluateAt(_currentTime);
        }

        public UIMeshRigProceduralPose EvaluateAt(float timeSeconds)
        {
            ResolvePoint();
            _currentPose = UIMeshRigMotionEvaluator.Evaluate(
                _profile,
                Mathf.Max(0f, timeSeconds),
                _speed,
                _phase,
                _point != null ? _point.RestCenterNormalized : Vector2.zero,
                _seed);

            if (_point != null)
            {
                _point.SetProceduralPose(
                    _currentPose.Position,
                    _currentPose.RotationDegrees,
                    _currentPose.Scale);
                _hasAppliedPose = true;
            }

            return _currentPose;
        }

        private float GetDeltaTime()
        {
            if (Application.isPlaying)
            {
                return _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            }

            float now = Time.realtimeSinceStartup;
            float delta = now - _lastRealtime;
            _lastRealtime = now;
            return Mathf.Clamp(delta, 0f, 0.1f);
        }

        private void ResolvePoint()
        {
            if (_point == null)
            {
                _point = GetComponent<UIMeshRigPoint>();
            }
        }

        private void ClearPose()
        {
            ResolvePoint();
            _currentPose = UIMeshRigProceduralPose.Identity;
            if (_point != null && _hasAppliedPose)
            {
                _point.ClearProceduralPose();
            }

            _hasAppliedPose = false;
        }
    }
}
