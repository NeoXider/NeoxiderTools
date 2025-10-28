using UnityEngine;
using UnityEngine.Events;
using Neo.Animations;

namespace Neo.Animations
{
    /// <summary>
    /// Универсальный аниматор для float значений.
    /// Предоставляет простой способ анимации любого числового значения с различными типами анимации.
    /// </summary>
    public class FloatAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        [Tooltip("Тип анимации")]
        public AnimationType animationType = AnimationType.PerlinNoise;

        [Header("Value Settings")]
        [Tooltip("Минимальное значение")]
        public float minValue = 0f;
        
        [Tooltip("Максимальное значение")]
        public float maxValue = 1f;
        
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

        [Header("Custom Curve")]
        [Tooltip("Пользовательская кривая для CustomCurve типа")]
        public AnimationCurve customCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Control")]
        [Tooltip("Автоматически запускать анимацию при старте")]
        public bool playOnStart = true;

        [Header("Events")]
        [Tooltip("Вызывается при изменении значения")]
        public UnityEvent<float> OnValueChanged;

        [Tooltip("Вызывается при запуске анимации")]
        public UnityEvent OnAnimationStarted;

        [Tooltip("Вызывается при остановке анимации")]
        public UnityEvent OnAnimationStopped;

        [Tooltip("Вызывается при паузе анимации")]
        public UnityEvent OnAnimationPaused;

        /// <summary>
        /// Текущее анимированное значение (только для чтения)
        /// </summary>
        public float CurrentValue { get; private set; }

        /// <summary>
        /// Проигрывается ли анимация
        /// </summary>
        public bool IsPlaying { get; private set; }

        /// <summary>
        /// Находится ли анимация на паузе
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        /// Минимальное значение (для изменения извне)
        /// </summary>
        public float MinValue 
        { 
            get => minValue; 
            set => minValue = value; 
        }

        /// <summary>
        /// Максимальное значение (для изменения извне)
        /// </summary>
        public float MaxValue 
        { 
            get => maxValue; 
            set => maxValue = value; 
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

        private float animationTime;
        private Vector2 randomOffset;
        private float lastValue;

        void Start()
        {
            animationTime = UnityEngine.Random.Range(0f, 1000f);
            randomOffset = new Vector2(UnityEngine.Random.Range(-1000f, 1000f),
                                       UnityEngine.Random.Range(-1000f, 1000f));

            if (playOnStart)
            {
                Play();
            }
        }

        void Update()
        {
            if (!IsPlaying || IsPaused)
                return;

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

        private void OnValidate()
        {
            if (maxValue < minValue) 
                maxValue = minValue;
        }
    }
}
