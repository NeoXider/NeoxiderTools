using System;
using System.Globalization;
using Neo.Animations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Samples
{
    /// <summary>
    ///     Controller of the authored <b>Neo.Animations</b> demo (AnimatorsDemo.unity). The scene, saved by its
    ///     DemoSceneKit builder, holds a disc whose scale follows a <see cref="Vector3Animator" />, a disc tinted by a
    ///     <see cref="ColorAnimator" />, a square moved by a <see cref="TransformAnimator" /> (rotate, bob, scale pulse,
    ///     impulse shake) and a card bar filled by a <see cref="FloatAnimator" />. The value animators reach their
    ///     targets through persistent UnityEvents (OnVectorChanged, OnColorChanged, Value.OnChanged), so the targets
    ///     move without this component. The card's Play / Pause / Resume / Stop buttons, the shared
    ///     <see cref="AnimationType" /> dropdown, the speed slider and the transform channel toggles call the public
    ///     methods below, which forward to the real animator API and log every call. It builds nothing at runtime.
    /// </summary>
    [AddComponentMenu("")]
    public sealed class AnimatorsDemoController : MonoBehaviour
    {
        [Header("Animators")]
        [Tooltip("Fills the card bar through Value.OnChanged.")]
        [SerializeField] private FloatAnimator _float;

        [Tooltip("Tints the colour disc through OnColorChanged.")]
        [SerializeField] private ColorAnimator _color;

        [Tooltip("Scales the scale disc through OnVectorChanged.")]
        [SerializeField] private Vector3Animator _vector;

        [Tooltip("Rotates, bobs and pulses the square.")]
        [SerializeField] private TransformAnimator _transform;

        [Header("Controls synced on start")]
        [SerializeField] private TMP_Dropdown _typeDropdown;
        [SerializeField] private Slider _speedSlider;
        [SerializeField] private Toggle _rotateToggle;
        [SerializeField] private Toggle _bobToggle;
        [SerializeField] private Toggle _pulseToggle;

        [Header("Feed")]
        [SerializeField] private DemoLogFeed _log;

        private bool _warnedUnbuilt;

        private enum Playback
        {
            Play,
            Pause,
            Resume,
            Stop
        }

        /// <summary>True when the authored scene references are assigned (the scene builder ran).</summary>
        public bool IsSceneBuilt => _float != null && _color != null && _vector != null && _transform != null &&
                                    _log != null;

        public FloatAnimator FloatAnimator => _float;
        public ColorAnimator ColorAnimator => _color;
        public Vector3Animator Vector3Animator => _vector;
        public TransformAnimator TransformAnimator => _transform;

        /// <summary>The animation type the three value animators share (read from the float animator).</summary>
        public AnimationType CurrentType => _float != null ? _float.AnimationType : AnimationType.PerlinNoise;

        private void Start()
        {
            if (!IsSceneBuilt)
            {
                WarnUnbuilt();
                return;
            }

            SyncControls();
            Log("Four animators on real targets; the value animators reach them through UnityEvents");
        }

        /// <summary>Button target: <c>Play()</c> on every animator.</summary>
        public void PlayAll()
        {
            ForEach(Playback.Play);
            Log("Play() x4");
        }

        /// <summary>Button target: <c>Pause()</c> on every animator; the targets hold their current value.</summary>
        public void PauseAll()
        {
            ForEach(Playback.Pause);
            Log("Pause() x4 - values hold, the clocks stop");
        }

        /// <summary>Button target: <c>Resume()</c> on every animator (only paused ones continue).</summary>
        public void ResumeAll()
        {
            ForEach(Playback.Resume);
            Log("Resume() x4");
        }

        /// <summary>
        ///     Button target: <c>Stop()</c> on every animator. The value animators freeze their output;
        ///     <see cref="Neo.Animations.TransformAnimator.Stop" /> restores the captured base pose.
        /// </summary>
        public void StopAll()
        {
            ForEach(Playback.Stop);
            Log("Stop() x4 - value animators freeze, TransformAnimator returns to its base pose");
        }

        /// <summary>Dropdown target: the shared <see cref="AnimationType" /> by its enum value.</summary>
        public void SetAnimationType(int type)
        {
            if (!Enum.IsDefined(typeof(AnimationType), type))
            {
                Log("Ignored unknown AnimationType " + type);
                return;
            }

            ApplyType((AnimationType)type);
        }

        /// <summary>Slider target: <c>AnimationSpeed</c> of the three value animators.</summary>
        public void SetSpeed(float speed)
        {
            float value = Mathf.Max(0f, speed);
            if (_float != null)
            {
                _float.AnimationSpeed = value;
            }

            if (_color != null)
            {
                _color.AnimationSpeed = value;
            }

            if (_vector != null)
            {
                _vector.AnimationSpeed = value;
            }

            Log("AnimationSpeed = " + value.ToString("0.0", CultureInfo.InvariantCulture));
        }

        /// <summary>Toggle target: the rotate channel of <see cref="Neo.Animations.TransformAnimator.Settings" />.</summary>
        public void SetRotateChannel(bool enabled)
        {
            TransformAnimationSettings settings = Settings();
            if (settings != null)
            {
                settings.RotateEnabled = enabled;
                Log("TransformAnimator rotate channel " + OnOff(enabled));
            }
        }

        /// <summary>Toggle target: the float (bob) channel of <see cref="Neo.Animations.TransformAnimator.Settings" />.</summary>
        public void SetBobChannel(bool enabled)
        {
            TransformAnimationSettings settings = Settings();
            if (settings != null)
            {
                settings.FloatEnabled = enabled;
                Log("TransformAnimator float (bob) channel " + OnOff(enabled));
            }
        }

        /// <summary>Toggle target: the scale pulse channel of <see cref="Neo.Animations.TransformAnimator.Settings" />.</summary>
        public void SetPulseChannel(bool enabled)
        {
            TransformAnimationSettings settings = Settings();
            if (settings != null)
            {
                settings.ScaleEnabled = enabled;
                Log("TransformAnimator scale pulse channel " + OnOff(enabled));
            }
        }

        private void ForEach(Playback playback)
        {
            if (_float != null)
            {
                Apply(playback, _float.Play, _float.Pause, _float.Resume, _float.Stop);
            }

            if (_color != null)
            {
                Apply(playback, _color.Play, _color.Pause, _color.Resume, _color.Stop);
            }

            if (_vector != null)
            {
                Apply(playback, _vector.Play, _vector.Pause, _vector.Resume, _vector.Stop);
            }

            if (_transform != null)
            {
                Apply(playback, _transform.Play, _transform.Pause, _transform.Resume, _transform.Stop);
            }
        }

        private static void Apply(Playback playback, Action play, Action pause, Action resume, Action stop)
        {
            switch (playback)
            {
                case Playback.Play:
                    play();
                    break;
                case Playback.Pause:
                    pause();
                    break;
                case Playback.Resume:
                    resume();
                    break;
                default:
                    stop();
                    break;
            }
        }

        private void ApplyType(AnimationType type)
        {
            if (_float != null)
            {
                _float.AnimationType = type;
            }

            if (_color != null)
            {
                _color.AnimationType = type;
            }

            if (_vector != null)
            {
                _vector.AnimationType = type;
            }

            Log("AnimationType = " + type + " (Float, Color and Vector3 animators)");
        }

        private void SyncControls()
        {
            if (_typeDropdown != null)
            {
                _typeDropdown.SetValueWithoutNotify((int)CurrentType);
            }

            if (_speedSlider != null && _float != null)
            {
                _speedSlider.SetValueWithoutNotify(_float.AnimationSpeed);
            }

            TransformAnimationSettings settings = _transform != null ? _transform.Settings : null;
            if (settings == null)
            {
                return;
            }

            if (_rotateToggle != null)
            {
                _rotateToggle.SetIsOnWithoutNotify(settings.RotateEnabled);
            }

            if (_bobToggle != null)
            {
                _bobToggle.SetIsOnWithoutNotify(settings.FloatEnabled);
            }

            if (_pulseToggle != null)
            {
                _pulseToggle.SetIsOnWithoutNotify(settings.ScaleEnabled);
            }
        }

        private TransformAnimationSettings Settings()
        {
            if (_transform == null)
            {
                WarnUnbuilt();
                return null;
            }

            return _transform.Settings;
        }

        private static string OnOff(bool enabled)
        {
            return enabled ? "on" : "off";
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
                "[AnimatorsDemoController] The scene is not authored. Open Scenes/Animations/AnimatorsDemo.unity or " +
                "run Neoxider/Demo/Rebuild Active Demo Scene.", this);
        }
    }
}
