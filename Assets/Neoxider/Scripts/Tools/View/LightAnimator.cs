using UnityEngine;
using UnityEngine.Events;
using Neo.Animations;
using System;

namespace Neo.Tools.View
{
    /// <summary>
    /// Компонент для анимации источников света (Light и Light2D).
    /// Поддерживает различные типы анимации интенсивности и цвета.
    /// </summary>
    public class LightAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        [Tooltip("Тип анимации")]
        public AnimationType animationType = AnimationType.PerlinNoise;

        [Header("Intensity Settings")]
        [Tooltip("Минимальная интенсивность")]
        [Range(0f, 100)] 
        public float minIntensity = 0.5f;
        
        [Tooltip("Максимальная интенсивность")]
        [Range(0f, 200f)] 
        public float maxIntensity = 1.5f;
        
        [Tooltip("Скорость анимации (0 = анимация отключена)")]
        [Range(0f, 30f)] 
        public float animationSpeed = 1.0f;

        [Header("Noise Settings")]
        [Tooltip("Масштаб шума для PerlinNoise")]
        [Range(0.1f, 20f)] 
        public float noiseScale = 1f;
        
        [Tooltip("Использовать 2D шум вместо 1D")]
        public bool use2DNoise = true;
        
        [Tooltip("Дополнительное смещение шума")]
        public Vector2 noiseOffset;

        [Header("Color Settings")]
        [Tooltip("Изменять ли цвет света")]
        public bool changeColor = false;
        
        [Tooltip("Целевой цвет")]
        public Color targetColor = Color.white;
        
        [Tooltip("Скорость смешивания цветов")]
        [Range(0f, 1f)] 
        public float colorBlendSpeed = 1f;

        [Header("Custom Curve")]
        [Tooltip("Пользовательская кривая для CustomCurve типа")]
        public AnimationCurve customCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Control")]
        [Tooltip("Автоматически запускать анимацию при старте")]
        public bool playOnStart = true;

        [Header("Debug Settings")]
        [Tooltip("Включить отладочные сообщения")]
        public bool enableDebugging = false;

        [Header("Events")]
        [Tooltip("Вызывается при изменении интенсивности")]
        public UnityEvent<float> OnIntensityChanged;

        [Tooltip("Вызывается при изменении цвета")]
        public UnityEvent<Color> OnColorChanged;

        [Tooltip("Вызывается при запуске анимации")]
        public UnityEvent OnAnimationStarted;

        [Tooltip("Вызывается при остановке анимации")]
        public UnityEvent OnAnimationStopped;

        [Tooltip("Вызывается при паузе анимации")]
        public UnityEvent OnAnimationPaused;

        /// <summary>
        /// Текущая интенсивность света (только для чтения)
        /// </summary>
        public float CurrentIntensity { get; private set; }

        /// <summary>
        /// Текущий цвет света (только для чтения)
        /// </summary>
        public Color CurrentColor { get; private set; }

        /// <summary>
        /// Проигрывается ли анимация
        /// </summary>
        public bool IsPlaying { get; private set; }

        /// <summary>
        /// Находится ли анимация на паузе
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        /// Минимальная интенсивность (для изменения извне)
        /// </summary>
        public float MinIntensity 
        { 
            get => minIntensity; 
            set => minIntensity = value; 
        }

        /// <summary>
        /// Максимальная интенсивность (для изменения извне)
        /// </summary>
        public float MaxIntensity 
        { 
            get => maxIntensity; 
            set => maxIntensity = value; 
        }

        /// <summary>
        /// Скорость анимации (для изменения извне)
        /// </summary>
        public float AnimationSpeed 
        { 
            get => animationSpeed; 
            set => animationSpeed = value; 
        }

        /// <summary>
        /// Тип анимации (для изменения извне)
        /// </summary>
        public AnimationType AnimationType 
        { 
            get => animationType; 
            set => animationType = value; 
        }

        /// <summary>
        /// Целевой цвет (для изменения извне)
        /// </summary>
        public Color TargetColor 
        { 
            get => targetColor; 
            set => targetColor = value; 
        }

        private ILightAccessor _light;
        private float originalIntensity;
        private Color originalColor;
        private float animationTime;
        private Vector2 randomOffset;
        private float lastIntensity;
        private Color lastColor;

        void Awake()
        {
            InitializeLight();
        }

        void Start()
        {
            if (_light == null) return;

            animationTime = UnityEngine.Random.Range(0f, 1000f);
            randomOffset = new Vector2(UnityEngine.Random.Range(-1000f, 1000f),
                                       UnityEngine.Random.Range(-1000f, 1000f));

            CurrentIntensity = originalIntensity;
            CurrentColor = originalColor;
            lastIntensity = originalIntensity;
            lastColor = originalColor;

            if (playOnStart)
            {
                Play();
            }
        }

        void Update()
        {
            if (_light == null || !IsPlaying || IsPaused)
                return;

            animationTime += Time.deltaTime;

            // ---------- ВЫЧИСЛЕНИЕ ЦЕЛЬНОЙ ИНТЕНСИВНОСТИ ----------
            float targetIntensity = AnimationUtils.GetTargetValue(
                animationType,
                minIntensity, maxIntensity,
                animationTime, animationSpeed,
                use2DNoise, randomOffset, noiseOffset, noiseScale,
                customCurve);

            // ---------- ФАКТОР СМЕШАНИЯ ЦВЕТА ----------
            float colorBlendFactor = AnimationUtils.GetColorBlendFactor(
                animationTime, animationSpeed, colorBlendSpeed);

            // ---------- ПРИЛОЖЕНИЕ РЕЗУЛЬТАТОВ ----------
            AnimationUtils.ApplyToLight(_light,
                                        targetIntensity,
                                        originalColor,
                                        changeColor,
                                        targetColor,
                                        colorBlendFactor);

            CurrentIntensity = _light.Intensity;
            CurrentColor = _light.Color;

            // Вызываем события при изменении значений
            if (Mathf.Abs(_light.Intensity - lastIntensity) > 0.001f)
            {
                OnIntensityChanged?.Invoke(_light.Intensity);
                lastIntensity = _light.Intensity;
            }

            if (changeColor && ColorDistance(_light.Color, lastColor) > 0.001f)
            {
                OnColorChanged?.Invoke(_light.Color);
                lastColor = _light.Color;
            }

            if (enableDebugging)
            {
                float dbgNoise = (animationType == AnimationType.PerlinNoise) ?
                    AnimationUtils.GetPerlinNoiseValue(animationTime, animationSpeed,
                                                       randomOffset, noiseOffset, noiseScale, use2DNoise) : 0f;
                Debug.Log($"[{gameObject.name}] Impl: {_light.ImplName}, Intensity: {_light.Intensity:F2}, Time: {animationTime:F2}, Speed: {animationSpeed:F2}, Noise: {dbgNoise:F2}");
            }
        }

        /// <summary>
        /// Запустить анимацию
        /// </summary>
        public void Play()
        {
            IsPlaying = true;
            IsPaused = false;
            OnAnimationStarted?.Invoke();
        }

        /// <summary>
        /// Остановить анимацию
        /// </summary>
        public void Stop()
        {
            IsPlaying = false;
            IsPaused = false;
            OnAnimationStopped?.Invoke();
        }

        /// <summary>
        /// Поставить анимацию на паузу
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
        /// Снять с паузы
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
        /// Сбросить к исходным значениям света
        /// </summary>
        public void ResetToOriginal()
        {
            if (_light != null)
            {
                _light.Intensity = originalIntensity;
                _light.Color = originalColor;
                CurrentIntensity = originalIntensity;
                CurrentColor = originalColor;
            }
        }

        /// <summary>
        /// Сбросить время анимации
        /// </summary>
        public void ResetTime()
        {
            animationTime = 0f;
        }

        /// <summary>
        /// Установить случайное начальное время
        /// </summary>
        public void RandomizeTime()
        {
            animationTime = UnityEngine.Random.Range(0f, 1000f);
        }

        private void InitializeLight()
        {
            // 1) Попробуем Light2D через reflection
            Type light2DType = System.Type.GetType(
                "UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime",
                throwOnError: false);

            if (light2DType != null)
            {
                var comp = GetComponent(light2DType);
                if (comp != null)
                {
                    var pIntensity = light2DType.GetProperty("intensity", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                    var pColor     = light2DType.GetProperty("color", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

                    if (pIntensity != null && pIntensity.CanRead && pIntensity.CanWrite &&
                        pColor != null && pColor.CanRead && pColor.CanWrite)
                    {
                        _light = new URPLight2DAccessor(comp, pIntensity, pColor);
                    }
                }
            }

            // 2) Если Light2D не найден — обычный Light
            if (_light == null)
            {
                var l = GetComponent<Light>();
                if (l != null)
                    _light = new UnityLightAccessor(l);
            }

            if (_light != null)
            {
                originalIntensity = _light.Intensity;
                originalColor = _light.Color;

                if (enableDebugging)
                    Debug.Log($"[{gameObject.name}] Using {_light.ImplName}");
            }
            else
            {
                Debug.LogError("No Light or Light2D component found on this GameObject!", this);
                enabled = false;
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
                maxIntensity = minIntensity;
        }

        /// <summary>
        /// Вычисляет расстояние между двумя цветами
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

        // ---------- ILightAccessor реализация ----------
        private sealed class UnityLightAccessor : ILightAccessor
        {
            private readonly Light _light;
            public UnityLightAccessor(Light l) => _light = l;
            public float Intensity { get => _light.intensity; set => _light.intensity = value; }
            public Color Color { get => _light.color; set => _light.color = value; }
            public string ImplName => "Light";
        }

        private sealed class URPLight2DAccessor : ILightAccessor
        {
            private readonly Component _light2D;
            private readonly System.Func<Component, float> _getIntensity;
            private readonly System.Action<Component, float> _setIntensity;
            private readonly System.Func<Component, Color> _getColor;
            private readonly System.Action<Component, Color> _setColor;

            public URPLight2DAccessor(Component comp,
                                      System.Reflection.PropertyInfo pIntensity,
                                      System.Reflection.PropertyInfo pColor)
            {
                _light2D = comp;

                var getI = pIntensity.GetGetMethod();
                var setI = pIntensity.GetSetMethod();
                var getC = pColor.GetGetMethod();
                var setC = pColor.GetSetMethod();

                _getIntensity = (System.Func<Component, float>)System.Delegate.CreateDelegate(
                    typeof(System.Func<Component, float>), null, getI);
                _setIntensity = (System.Action<Component, float>)System.Delegate.CreateDelegate(
                    typeof(System.Action<Component, float>), null, setI);
                _getColor     = (System.Func<Component, Color>)System.Delegate.CreateDelegate(
                    typeof(System.Func<Component, Color>), null, getC);
                _setColor     = (System.Action<Component, Color>)System.Delegate.CreateDelegate(
                    typeof(System.Action<Component, Color>), null, setC);
            }

            public float Intensity { get => _getIntensity(_light2D); set => _setIntensity(_light2D, value); }
            public Color Color { get => _getColor(_light2D); set => _setColor(_light2D, value); }
            public string ImplName => "Light2D";
        }
    }
}