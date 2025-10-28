using UnityEngine;
using UnityEngine.Events;
using Neo.Animations;

namespace Neo.Animations
{
    /// <summary>
    /// Универсальный аниматор для цветов.
    /// Предоставляет простой способ анимации цвета с различными типами анимации.
    /// </summary>
    public class ColorAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        [Tooltip("Тип анимации")]
        public AnimationType animationType = AnimationType.PerlinNoise;

        [Header("Color Settings")]
        [Tooltip("Начальный цвет")]
        public Color startColor = Color.white;
        
        [Tooltip("Конечный цвет")]
        public Color endColor = Color.red;
        
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
        [Tooltip("Вызывается при изменении цвета")]
        public UnityEvent<Color> OnColorChanged;

        [Tooltip("Вызывается при запуске анимации")]
        public UnityEvent OnAnimationStarted;

        [Tooltip("Вызывается при остановке анимации")]
        public UnityEvent OnAnimationStopped;

        [Tooltip("Вызывается при паузе анимации")]
        public UnityEvent OnAnimationPaused;

        /// <summary>
        /// Текущий анимированный цвет (только для чтения)
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
        /// Начальный цвет (для изменения извне)
        /// </summary>
        public Color StartColor 
        { 
            get => startColor; 
            set => startColor = value; 
        }

        /// <summary>
        /// Конечный цвет (для изменения извне)
        /// </summary>
        public Color EndColor 
        { 
            get => endColor; 
            set => endColor = value; 
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
        private Color lastColor;

        void Start()
        {
            animationTime = UnityEngine.Random.Range(0f, 1000f);
            randomOffset = new Vector2(UnityEngine.Random.Range(-1000f, 1000f),
                                       UnityEngine.Random.Range(-1000f, 1000f));

            CurrentColor = startColor;
            lastColor = startColor;

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

            // Получаем новый цвет
            Color newColor = AnimationUtils.GetAnimatedColor(
                animationType,
                startColor, endColor,
                animationTime, animationSpeed,
                customCurve);

            CurrentColor = newColor;

            // Вызываем событие если цвет изменился
            if (ColorDistance(newColor, lastColor) > 0.001f)
            {
                OnColorChanged?.Invoke(newColor);
                lastColor = newColor;
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
    }
}
