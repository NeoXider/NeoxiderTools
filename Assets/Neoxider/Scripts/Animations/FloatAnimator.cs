using UnityEngine;
using UnityEngine.Events;

namespace Neo.Animations
{
    /// <summary>
    ///     Универсальный аниматор для float значений.
    ///     Предоставляет простой способ анимации любого числового значения с различными типами анимации.
    /// </summary>
    [AddComponentMenu("Neo/" + "Animations/" + nameof(FloatAnimator))]
    public class FloatAnimator : MonoBehaviour
    {
        [Header("Animation Settings")] [Tooltip("Animation type")]
        public AnimationType animationType = AnimationType.PerlinNoise;

        [Header("Value Settings")] [Tooltip("Min value")]
        public float minValue;

        [Tooltip("Max value")] public float maxValue = 1f;

        [Tooltip("Animation speed (0 = disabled)")] [Range(0f, 30f)]
        public float animationSpeed = 1.0f;

        [Header("Noise Settings")] [Tooltip("Noise scale for PerlinNoise")] [Range(0.1f, 20f)]
        public float noiseScale = 1f;

        [Tooltip("Use 2D noise instead of 1D")]
        public bool use2DNoise = true;

        [Tooltip("Additional noise offset")]
        public Vector2 noiseOffset;

        [Header("Custom Curve")] [Tooltip("Custom curve for CustomCurve type")]
        public AnimationCurve customCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Control")] [Tooltip("Auto-start animation on Start")]
        public bool playOnStart = true;

        [Tooltip("Invoked when value changes")]
        public UnityEvent<float> OnValueChanged;

        [Tooltip("Invoked when animation starts")]
        public UnityEvent OnAnimationStarted;

        [Tooltip("Invoked when animation stops")]
        public UnityEvent OnAnimationStopped;

        [Tooltip("Invoked when animation is paused")]
        public UnityEvent OnAnimationPaused;

        private float animationTime;
        private float lastValue;
        private Vector2 randomOffset;

        /// <summary>
        ///     Текущее анимированное значение (только для чтения)
        /// </summary>
        public float CurrentValue { get; private set; }

        /// <summary>
        ///     Проигрывается ли анимация
        /// </summary>
        public bool IsPlaying { get; private set; }

        /// <summary>
        ///     Находится ли анимация на паузе
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        ///     Минимальное значение (для изменения извне)
        /// </summary>
        public float MinValue
        {
            get => minValue;
            set => minValue = value;
        }

        /// <summary>
        ///     Максимальное значение (для изменения извне)
        /// </summary>
        public float MaxValue
        {
            get => maxValue;
            set => maxValue = value;
        }

        /// <summary>
        ///     Скорость анимации (для изменения извне)
        /// </summary>
        public float AnimationSpeed
        {
            get => animationSpeed;
            set => animationSpeed = value;
        }

        /// <summary>
        ///     Тип анимации (для изменения извне)
        /// </summary>
        public AnimationType AnimationType
        {
            get => animationType;
            set => animationType = value;
        }

        private void Start()
        {
            animationTime = Random.Range(0f, 1000f);
            randomOffset = new Vector2(Random.Range(-1000f, 1000f),
                Random.Range(-1000f, 1000f));

            if (playOnStart)
            {
                Play();
            }
        }

        private void Update()
        {
            if (!IsPlaying || IsPaused)
            {
                return;
            }

            animationTime += Time.deltaTime;

            // Получаем новое значение
            float newValue = AnimationUtils.GetTargetValue(
                animationType,
                minValue, maxValue,
                animationTime, animationSpeed,
                use2DNoise, randomOffset, noiseOffset, noiseScale,
                customCurve);

            CurrentValue = newValue;

            // Вызываем событие если значение изменилось
            if (Mathf.Abs(newValue - lastValue) > 0.001f)
            {
                OnValueChanged?.Invoke(newValue);
                lastValue = newValue;
            }
        }

        private void OnValidate()
        {
            if (maxValue < minValue)
            {
                maxValue = minValue;
            }
        }

        /// <summary>
        ///     Запустить анимацию
        /// </summary>
        public void Play()
        {
            IsPlaying = true;
            IsPaused = false;
            OnAnimationStarted?.Invoke();
        }

        /// <summary>
        ///     Остановить анимацию
        /// </summary>
        public void Stop()
        {
            IsPlaying = false;
            IsPaused = false;
            OnAnimationStopped?.Invoke();
        }

        /// <summary>
        ///     Поставить анимацию на паузу
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
        ///     Снять с паузы
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
        ///     Сбросить время анимации
        /// </summary>
        public void ResetTime()
        {
            animationTime = 0f;
        }

        /// <summary>
        ///     Установить случайное начальное время
        /// </summary>
        public void RandomizeTime()
        {
            animationTime = Random.Range(0f, 1000f);
        }
    }
}