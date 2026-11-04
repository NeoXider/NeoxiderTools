using System;
using System.Reflection;
using Neo.Animations;
using Neo.Reactive;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace Neo.Tools.View
{
    /// <summary>
    ///     Animates light sources (Light and Light2D).
    ///     Supports multiple intensity and color animation types.
    /// </summary>
    [NeoDoc("Tools/View/LightAnimator.md")]
    [CreateFromMenu("Neoxider/Tools/View/LightAnimator")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(LightAnimator))]
    public class LightAnimator : MonoBehaviour
    {
        [Header("Animation Settings")] [Tooltip("Animation type")]
        public AnimationType animationType = AnimationType.PerlinNoise;

        [Header("Intensity Settings")] [Tooltip("Min intensity")] [Range(0f, 100)]
        public float minIntensity = 0.5f;

        [Tooltip("Max intensity")] [Range(0f, 200f)]
        public float maxIntensity = 1.5f;

        [Tooltip("Animation speed (0 = disabled)")] [Range(0f, 30f)]
        public float animationSpeed = 1.0f;

        [Header("Noise Settings")] [Tooltip("Noise scale for PerlinNoise")] [Range(0.1f, 20f)]
        public float noiseScale = 1f;

        [Tooltip("Use 2D noise instead of 1D")]
        public bool use2DNoise = true;

        [Tooltip("Additional noise offset")] public Vector2 noiseOffset;

        [Header("Color Settings")] [Tooltip("Whether to change light color")]
        public bool changeColor;

        [Tooltip("Target color")] public Color targetColor = Color.white;

        [Tooltip("Color blend speed")] [Range(0f, 1f)]
        public float colorBlendSpeed = 1f;

        [Header("Custom Curve")] [Tooltip("Custom curve for CustomCurve type")]
        public AnimationCurve customCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Control")] [Tooltip("Auto-start animation on Start")]
        public bool playOnStart = true;

        [Tooltip("Randomize the clock and noise offset in Start so several lights do not flicker in sync. " +
                 "Off = deterministic start at time 0, or at the time set by ResetTime before Start.")]
        [SerializeField] private bool _randomizeStartTime = true;

        [Tooltip("Advance on unscaled time (ignores Time.timeScale), e.g. for lights in a pause menu.")]
        [SerializeField] private bool _useUnscaledTime;

        [Header("Debug Settings")] [Tooltip("Enable debug messages")]
        public bool enableDebugging;

        [Tooltip("Reactive intensity; subscribe via Intensity.OnChanged")]
        public ReactivePropertyFloat Intensity = new();

        [Tooltip("Invoked when color changes")]
        public UnityEvent<Color> OnColorChanged;

        [Tooltip("Invoked when animation starts")]
        public UnityEvent OnAnimationStarted;

        [Tooltip("Invoked when animation stops")]
        public UnityEvent OnAnimationStopped;

        [Tooltip("Invoked when animation is paused")]
        public UnityEvent OnAnimationPaused;

        // WHY cached once per domain: the Light2D type and its properties are looked up by reflection, and every
        // LightAnimator in a scene used to repeat the two Type.GetType scans in its own Awake.
        private static bool _light2DResolved;
        private static Type _light2DType;
        private static PropertyInfo _light2DIntensity;
        private static PropertyInfo _light2DColor;

        private ILightAccessor _light;
        private Component _lightComponent;
        private float animationTime;
        private Color lastColor;
        private float lastIntensity;
        private Color originalColor;
        private float originalIntensity;
        private Vector2 randomOffset;
        private bool _disabled;

        /// <summary>Current intensity (for NeoCondition and reflection).</summary>
        public float IntensityValue => Intensity.CurrentValue;

        /// <summary>
        ///     Current light intensity (read-only).
        /// </summary>
        public float CurrentIntensity => Intensity.CurrentValue;

        /// <summary>
        ///     Current light color (read-only).
        /// </summary>
        public Color CurrentColor { get; private set; }

        /// <summary>
        ///     Whether the animation is playing.
        /// </summary>
        public bool IsPlaying { get; private set; }

        /// <summary>
        ///     Whether the animation is paused.
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        ///     When true, <c>Update</c> does nothing and the owner advances the animation through <see cref="Tick" />.
        ///     Not serialized.
        /// </summary>
        public bool ManualUpdate { get; set; }

        /// <summary>Whether <c>Start</c> randomizes the clock and noise offset (the legacy default).</summary>
        public bool RandomizeStartTime
        {
            get => _randomizeStartTime;
            set => _randomizeStartTime = value;
        }

        /// <summary>Whether <c>Update</c> advances on <see cref="Time.unscaledDeltaTime" />.</summary>
        public bool UseUnscaledTime
        {
            get => _useUnscaledTime;
            set => _useUnscaledTime = value;
        }

        /// <summary>The animated Light or Light2D component, or null when none was found or it was destroyed.</summary>
        public Component LightComponent => _lightComponent;

        /// <summary>
        ///     Minimum intensity (mutable from outside).
        /// </summary>
        public float MinIntensity
        {
            get => minIntensity;
            set => minIntensity = value;
        }

        /// <summary>
        ///     Maximum intensity (mutable from outside).
        /// </summary>
        public float MaxIntensity
        {
            get => maxIntensity;
            set => maxIntensity = value;
        }

        /// <summary>
        ///     Animation speed (mutable from outside).
        /// </summary>
        public float AnimationSpeed
        {
            get => animationSpeed;
            set => animationSpeed = value;
        }

        /// <summary>
        ///     Animation type (mutable from outside).
        /// </summary>
        public AnimationType AnimationType
        {
            get => animationType;
            set => animationType = value;
        }

        /// <summary>
        ///     Target color (mutable from outside).
        /// </summary>
        public Color TargetColor
        {
            get => targetColor;
            set => targetColor = value;
        }

        private void Awake()
        {
            InitializeLight();
        }

        private void Start()
        {
            if (_light == null)
            {
                return;
            }

            if (_randomizeStartTime)
            {
                animationTime = Random.Range(0f, 1000f);
                randomOffset = new Vector2(Random.Range(-1000f, 1000f),
                    Random.Range(-1000f, 1000f));
            }

            Intensity.Value = originalIntensity;
            CurrentColor = originalColor;
            lastIntensity = originalIntensity;
            lastColor = originalColor;

            if (playOnStart)
            {
                Play();
            }
        }

        private void Update()
        {
            if (ManualUpdate)
            {
                return;
            }

            Tick(_useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
        }

        /// <summary>
        ///     Advances the clock by <paramref name="deltaTime" /> seconds and applies intensity (and color when
        ///     <see cref="changeColor" /> is on) to the light. Does nothing while stopped, paused or disabled (a
        ///     disabled animator keeps the baseline its <c>OnDisable</c> restored). If the light component was
        ///     destroyed, the animator stops instead of throwing every frame.
        /// </summary>
        public void Tick(float deltaTime)
        {
            // WHY the disable flag: OnDisable restores the light's baseline, and a manual clock that kept ticking
            // a disabled animator overwrote it at once, so the disabled light went on flickering.
            if (_light == null || _disabled || !IsPlaying || IsPaused)
            {
                return;
            }

            if (_lightComponent == null)
            {
                // WHY: the Light/Light2D was destroyed under the animator; every accessor call would throw a
                // MissingReferenceException on each following frame.
                _light = null;
                IsPlaying = false;
                IsPaused = false;
                return;
            }

            animationTime += AnimationUtils.SanitizeDeltaTime(deltaTime);

            float targetIntensity = AnimationUtils.GetTargetValue(
                animationType,
                minIntensity, maxIntensity,
                animationTime, animationSpeed,
                use2DNoise, randomOffset, noiseOffset, noiseScale,
                customCurve);

            float colorBlendFactor = AnimationUtils.GetColorBlendFactor(
                animationTime, animationSpeed, colorBlendSpeed);

            AnimationUtils.ApplyToLight(_light,
                targetIntensity,
                originalColor,
                changeColor,
                targetColor,
                colorBlendFactor);

            // WHY one readback each: the light may clamp what it accepts, and every accessor call is a native
            // (or reflected) call, so the values are read once instead of once per use.
            float appliedIntensity = _light.Intensity;
            Color appliedColor = _light.Color;

            if (Mathf.Abs(appliedIntensity - lastIntensity) > AnimationUtils.ChangeThreshold)
            {
                Intensity.Value = appliedIntensity;
                lastIntensity = appliedIntensity;
            }

            CurrentColor = appliedColor;

            if (changeColor &&
                AnimationUtils.ColorDistanceSquared(appliedColor, lastColor) > AnimationUtils.ChangeThresholdSquared)
            {
                OnColorChanged?.Invoke(appliedColor);
                lastColor = appliedColor;
            }

            if (enableDebugging)
            {
                float dbgNoise = animationType == AnimationType.PerlinNoise
                    ? AnimationUtils.GetPerlinNoiseValue(animationTime, animationSpeed,
                        randomOffset, noiseOffset, noiseScale, use2DNoise)
                    : 0f;
                NeoDiagnostics.Log(
                    $"[{gameObject.name}] Impl: {_light.ImplName}, Intensity: {appliedIntensity:F2}, Time: {animationTime:F2}, Speed: {animationSpeed:F2}, Noise: {dbgNoise:F2}");
            }
        }

        private void OnEnable()
        {
            _disabled = false;
        }

        private void OnDisable()
        {
            _disabled = true;
            ResetToOriginal();
        }

        private void OnValidate()
        {
            if (maxIntensity < minIntensity)
            {
                maxIntensity = minIntensity;
            }
        }

        /// <summary>
        ///     Starts the animation.
        /// </summary>
        [Button(PlayModeOnly = true)]
        public void Play()
        {
            IsPlaying = true;
            IsPaused = false;
            OnAnimationStarted?.Invoke();
        }

        /// <summary>
        ///     Stops the animation.
        /// </summary>
        [Button(PlayModeOnly = true)]
        public void Stop()
        {
            IsPlaying = false;
            IsPaused = false;
            OnAnimationStopped?.Invoke();
        }

        /// <summary>
        ///     Pauses the animation.
        /// </summary>
        [Button(PlayModeOnly = true)]
        public void Pause()
        {
            if (IsPlaying)
            {
                IsPaused = true;
                OnAnimationPaused?.Invoke();
            }
        }

        /// <summary>
        ///     Resumes from pause.
        /// </summary>
        [Button(PlayModeOnly = true)]
        public void Resume()
        {
            if (IsPaused)
            {
                IsPaused = false;
                OnAnimationStarted?.Invoke();
            }
        }

        /// <summary>
        ///     Resets light to original values.
        /// </summary>
        public void ResetToOriginal()
        {
            if (_light != null && _lightComponent != null)
            {
                _light.Intensity = originalIntensity;
                _light.Color = originalColor;
                lastIntensity = originalIntensity;
                lastColor = originalColor;
                Intensity.Value = originalIntensity;
                CurrentColor = originalColor;
            }
        }

        /// <summary>
        ///     Resets animation time.
        /// </summary>
        public void ResetTime()
        {
            animationTime = 0f;
        }

        /// <summary>
        ///     Sets a random start time.
        /// </summary>
        public void RandomizeTime()
        {
            animationTime = Random.Range(0f, 1000f);
        }

        private static void ResolveLight2DType()
        {
            if (_light2DResolved)
            {
                return;
            }

            _light2DResolved = true;
            // WHY two lookups: Unity 6 URP separates the 2D runtime; older URP keeps Light2D in the main
            // runtime assembly.
            Type light2DType = Type.GetType(
                "UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.2D.Runtime",
                false) ?? Type.GetType(
                "UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime",
                false);
            if (light2DType == null)
            {
                return;
            }

            PropertyInfo pIntensity = light2DType.GetProperty("intensity", BindingFlags.Instance | BindingFlags.Public);
            PropertyInfo pColor = light2DType.GetProperty("color", BindingFlags.Instance | BindingFlags.Public);
            if (pIntensity != null && pIntensity.CanRead && pIntensity.CanWrite &&
                pColor != null && pColor.CanRead && pColor.CanWrite)
            {
                _light2DType = light2DType;
                _light2DIntensity = pIntensity;
                _light2DColor = pColor;
            }
        }

        private void InitializeLight()
        {
            _light = null;
            _lightComponent = null;
            ResolveLight2DType();
            if (_light2DType != null)
            {
                Component comp = GetComponent(_light2DType);
                if (comp != null)
                {
                    _light = new URPLight2DAccessor(comp, _light2DIntensity, _light2DColor);
                    _lightComponent = comp;
                }
            }

            if (_light == null)
            {
                Light l = GetComponent<Light>();
                if (l != null)
                {
                    _light = new UnityLightAccessor(l);
                    _lightComponent = l;
                }
            }

            if (_light != null)
            {
                originalIntensity = _light.Intensity;
                originalColor = _light.Color;

                if (enableDebugging)
                {
                    NeoDiagnostics.Log($"[{gameObject.name}] Using {_light.ImplName}");
                }
            }
            else
            {
                NeoDiagnostics.LogError("No Light or Light2D component found on this GameObject!", this);
                enabled = false;
            }
        }

        private sealed class UnityLightAccessor : ILightAccessor
        {
            private readonly Light _light;

            public UnityLightAccessor(Light l)
            {
                _light = l;
            }

            public float Intensity
            {
                get => _light.intensity;
                set => _light.intensity = value;
            }

            public Color Color
            {
                get => _light.color;
                set => _light.color = value;
            }

            public string ImplName => "Light";
        }

        private sealed class URPLight2DAccessor : ILightAccessor
        {
            private readonly Func<Color> _getColor;
            private readonly Func<float> _getIntensity;
            private readonly Action<Color> _setColor;
            private readonly Action<float> _setIntensity;

            public URPLight2DAccessor(Component comp,
                PropertyInfo pIntensity,
                PropertyInfo pColor)
            {
                MethodInfo getI = pIntensity.GetGetMethod();
                MethodInfo setI = pIntensity.GetSetMethod();
                MethodInfo getC = pColor.GetGetMethod();
                MethodInfo setC = pColor.GetSetMethod();

                _getIntensity = (Func<float>)Delegate.CreateDelegate(typeof(Func<float>), comp, getI);
                _setIntensity = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), comp, setI);
                _getColor = (Func<Color>)Delegate.CreateDelegate(typeof(Func<Color>), comp, getC);
                _setColor = (Action<Color>)Delegate.CreateDelegate(typeof(Action<Color>), comp, setC);
            }

            public float Intensity
            {
                get => _getIntensity();
                set => _setIntensity(value);
            }

            public Color Color
            {
                get => _getColor();
                set => _setColor(value);
            }

            public string ImplName => "Light2D";
        }
    }
}
