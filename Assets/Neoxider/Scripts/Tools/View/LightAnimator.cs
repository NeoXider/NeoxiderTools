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

        private ILightAccessor _light;
        private float animationTime;
        private Color lastColor;
        private float lastIntensity;
        private Color originalColor;
        private float originalIntensity;
        private Vector2 randomOffset;

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

            animationTime = Random.Range(0f, 1000f);
            randomOffset = new Vector2(Random.Range(-1000f, 1000f),
                Random.Range(-1000f, 1000f));

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
            if (_light == null || !IsPlaying || IsPaused)
            {
                return;
            }

            animationTime += Time.deltaTime;

            // ---------- TARGET INTENSITY ----------
            float targetIntensity = AnimationUtils.GetTargetValue(
                animationType,
                minIntensity, maxIntensity,
                animationTime, animationSpeed,
                use2DNoise, randomOffset, noiseOffset, noiseScale,
                customCurve);

            // ---------- COLOR BLEND FACTOR ----------
            float colorBlendFactor = AnimationUtils.GetColorBlendFactor(
                animationTime, animationSpeed, colorBlendSpeed);

            // ---------- APPLY RESULT ----------
            AnimationUtils.ApplyToLight(_light,
                targetIntensity,
                originalColor,
                changeColor,
                targetColor,
                colorBlendFactor);

            if (Mathf.Abs(_light.Intensity - lastIntensity) > 0.001f)
            {
                Intensity.Value = _light.Intensity;
                lastIntensity = _light.Intensity;
            }

            CurrentColor = _light.Color;

            if (changeColor && ColorDistance(_light.Color, lastColor) > 0.001f)
            {
                OnColorChanged?.Invoke(_light.Color);
                lastColor = _light.Color;
            }

            if (enableDebugging)
            {
                float dbgNoise = animationType == AnimationType.PerlinNoise
                    ? AnimationUtils.GetPerlinNoiseValue(animationTime, animationSpeed,
                        randomOffset, noiseOffset, noiseScale, use2DNoise)
                    : 0f;
                Debug.Log(
                    $"[{gameObject.name}] Impl: {_light.ImplName}, Intensity: {_light.Intensity:F2}, Time: {animationTime:F2}, Speed: {animationSpeed:F2}, Noise: {dbgNoise:F2}");
            }
        }

        private void OnDisable()
        {
            if (_light != null)
            {
                _light.Intensity = originalIntensity;
                _light.Color = originalColor;
            }
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
        public void Play()
        {
            IsPlaying = true;
            IsPaused = false;
            OnAnimationStarted?.Invoke();
        }

        /// <summary>
        ///     Stops the animation.
        /// </summary>
        public void Stop()
        {
            IsPlaying = false;
            IsPaused = false;
            OnAnimationStopped?.Invoke();
        }

        /// <summary>
        ///     Pauses the animation.
        /// </summary>
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
            if (_light != null)
            {
                _light.Intensity = originalIntensity;
                _light.Color = originalColor;
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

        private void InitializeLight()
        {
            // 1) Try Light2D via reflection
            var light2DType = Type.GetType(
                "UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime",
                false);

            if (light2DType != null)
            {
                Component comp = GetComponent(light2DType);
                if (comp != null)
                {
                    PropertyInfo pIntensity =
                        light2DType.GetProperty("intensity", BindingFlags.Instance | BindingFlags.Public);
                    PropertyInfo pColor = light2DType.GetProperty("color", BindingFlags.Instance | BindingFlags.Public);

                    if (pIntensity != null && pIntensity.CanRead && pIntensity.CanWrite &&
                        pColor != null && pColor.CanRead && pColor.CanWrite)
                    {
                        _light = new URPLight2DAccessor(comp, pIntensity, pColor);
                    }
                }
            }

            // 2) If no Light2D, use standard Light
            if (_light == null)
            {
                Light l = GetComponent<Light>();
                if (l != null)
                {
                    _light = new UnityLightAccessor(l);
                }
            }

            if (_light != null)
            {
                originalIntensity = _light.Intensity;
                originalColor = _light.Color;

                if (enableDebugging)
                {
                    Debug.Log($"[{gameObject.name}] Using {_light.ImplName}");
                }
            }
            else
            {
                Debug.LogError("No Light or Light2D component found on this GameObject!", this);
                enabled = false;
            }
        }

        /// <summary>
        ///     Computes distance between two colors.
        /// </summary>
        private float ColorDistance(Color a, Color b)
        {
            return Mathf.Sqrt(
                Mathf.Pow(a.r - b.r, 2) +
                Mathf.Pow(a.g - b.g, 2) +
                Mathf.Pow(a.b - b.b, 2) +
                Mathf.Pow(a.a - b.a, 2)
            );
        }

        // ---------- ILightAccessor implementation ----------
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
            private readonly Func<Component, Color> _getColor;
            private readonly Func<Component, float> _getIntensity;
            private readonly Component _light2D;
            private readonly Action<Component, Color> _setColor;
            private readonly Action<Component, float> _setIntensity;

            public URPLight2DAccessor(Component comp,
                PropertyInfo pIntensity,
                PropertyInfo pColor)
            {
                _light2D = comp;

                MethodInfo getI = pIntensity.GetGetMethod();
                MethodInfo setI = pIntensity.GetSetMethod();
                MethodInfo getC = pColor.GetGetMethod();
                MethodInfo setC = pColor.GetSetMethod();

                _getIntensity = (Func<Component, float>)Delegate.CreateDelegate(
                    typeof(Func<Component, float>), null, getI);
                _setIntensity = (Action<Component, float>)Delegate.CreateDelegate(
                    typeof(Action<Component, float>), null, setI);
                _getColor = (Func<Component, Color>)Delegate.CreateDelegate(
                    typeof(Func<Component, Color>), null, getC);
                _setColor = (Action<Component, Color>)Delegate.CreateDelegate(
                    typeof(Action<Component, Color>), null, setC);
            }

            public float Intensity
            {
                get => _getIntensity(_light2D);
                set => _setIntensity(_light2D, value);
            }

            public Color Color
            {
                get => _getColor(_light2D);
                set => _setColor(_light2D, value);
            }

            public string ImplName => "Light2D";
        }
    }
}
