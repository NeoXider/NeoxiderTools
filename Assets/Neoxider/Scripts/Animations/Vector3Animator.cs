using UnityEngine;
using UnityEngine.Events;
using Neo.Animations;

namespace Neo.Animations
{
    /// <summary>
    /// Универсальный аниматор для Vector3 значений.
    /// Предоставляет простой способ анимации позиции, масштаба, поворота и других Vector3 параметров.
    /// </summary>
    public class Vector3Animator : MonoBehaviour
    {
        [Header("Animation Settings")]
        [Tooltip("Тип анимации")]
        public AnimationType animationType = AnimationType.PerlinNoise;

        [Header("Vector Settings")]
        [Tooltip("Начальный вектор")]
        public Vector3 startVector = Vector3.zero;
        
        [Tooltip("Конечный вектор")]
        public Vector3 endVector = Vector3.one;
        
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
        [Tooltip("Вызывается при изменении вектора")]
        public UnityEvent<Vector3> OnVectorChanged;

        [Tooltip("Вызывается при запуске анимации")]
        public UnityEvent OnAnimationStarted;

        [Tooltip("Вызывается при остановке анимации")]
        public UnityEvent OnAnimationStopped;

        [Tooltip("Вызывается при паузе анимации")]
        public UnityEvent OnAnimationPaused;

        /// <summary>
        /// Текущий анимированный вектор (только для чтения)
        /// </summary>
        public Vector3 CurrentVector { get; private set; }

        /// <summary>
        /// Проигрывается ли анимация
        /// </summary>
        public bool IsPlaying { get; private set; }

        /// <summary>
        /// Находится ли анимация на паузе
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        /// Начальный вектор (для изменения извне)
        /// </summary>
        public Vector3 StartVector 
        { 
            get => startVector; 
            set => startVector = value; 
        }

        /// <summary>
        /// Конечный вектор (для изменения извне)
        /// </summary>
        public Vector3 EndVector 
        { 
            get => endVector; 
            set => endVector = value; 
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
        private Vector3 lastVector;

        void Start()
        {
            animationTime = UnityEngine.Random.Range(0f, 1000f);
            randomOffset = new Vector2(UnityEngine.Random.Range(-1000f, 1000f),
                                       UnityEngine.Random.Range(-1000f, 1000f));

            CurrentVector = startVector;
            lastVector = startVector;

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

            // Получаем новый вектор
            Vector3 newVector = AnimationUtils.GetAnimatedVector3(
                animationType,
                startVector, endVector,
                animationTime, animationSpeed,
                customCurve);

            CurrentVector = newVector;

            // Вызываем событие если вектор изменился
            if (Vector3.Distance(newVector, lastVector) > 0.001f)
            {
                OnVectorChanged?.Invoke(newVector);
                lastVector = newVector;
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
    }
}
