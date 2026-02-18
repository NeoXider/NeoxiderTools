using UnityEngine;
using UnityEngine.Events;

namespace Neo.Animations
{
    /// <summary>
    ///     Универсальный аниматор для Vector3 значений.
    ///     Предоставляет простой способ анимации позиции, масштаба, поворота и других Vector3 параметров.
    /// </summary>
    [NeoDoc("Animations/Vector3Animator.md")]
    [AddComponentMenu("Neoxider/" + "Animations/" + nameof(Vector3Animator))]
    public class Vector3Animator : MonoBehaviour
    {
        [Header("Animation Settings")] [Tooltip("Animation type")]
        public AnimationType animationType = AnimationType.PerlinNoise;

        [Header("Vector Settings")] [Tooltip("Start vector")]
        public Vector3 startVector = Vector3.zero;

        [Tooltip("End vector")] public Vector3 endVector = Vector3.one;

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

        [Tooltip("Invoked when vector changes")]
        public UnityEvent<Vector3> OnVectorChanged;

        [Tooltip("Invoked when animation starts")]
        public UnityEvent OnAnimationStarted;

        [Tooltip("Invoked when animation stops")]
        public UnityEvent OnAnimationStopped;

        [Tooltip("Invoked when animation is paused")]
        public UnityEvent OnAnimationPaused;

        private float animationTime;
        private Vector3 lastVector;
        private Vector2 randomOffset;

        /// <summary>
        ///     Текущий анимированный вектор (только для чтения)
        /// </summary>
        public Vector3 CurrentVector { get; private set; }

        /// <summary>
        ///     Проигрывается ли анимация
        /// </summary>
        public bool IsPlaying { get; private set; }

        /// <summary>
        ///     Находится ли анимация на паузе
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        ///     Начальный вектор (для изменения извне)
        /// </summary>
        public Vector3 StartVector
        {
            get => startVector;
            set => startVector = value;
        }

        /// <summary>
        ///     Конечный вектор (для изменения извне)
        /// </summary>
        public Vector3 EndVector
        {
            get => endVector;
            set => endVector = value;
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

            CurrentVector = startVector;
            lastVector = startVector;

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
