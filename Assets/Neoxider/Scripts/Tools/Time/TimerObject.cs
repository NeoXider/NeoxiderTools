using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#if UNITY_TEXTMESHPRO
using TMPro;
#endif

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Neo
{
    /// <summary>
    ///     MonoBehaviour-based timer with Unity events support and automatic UI updates
    /// </summary>
    [AddComponentMenu("Neo/" + "Tools/" + nameof(TimerObject))]
    public class TimerObject : MonoBehaviour
    {
        [Header("Timer Settings")] [Tooltip("Total duration of the timer in seconds")]
        public float duration = 1f;

        [Tooltip("Update frequency in seconds")] [Min(0.015f)]
        public float updateInterval = 0.015f;

        [Tooltip("If true, time counts up; if false, counts down")]
        public bool countUp = true;

        [Tooltip("Use unscaled time (ignores time scale)")]
        public bool useUnscaledTime;

        [Tooltip("Автоматически ставить на паузу при Time.timeScale == 0")]
        public bool pauseOnTimeScaleZero = true;

        [Tooltip("Automatically restart when complete")]
        public bool looping;

        [Header("Initial State")] [Tooltip("Start timer automatically on enable")]
        public bool autoStart = true;

        [Tooltip("Initial time value (0 = start from beginning, 1 = start from end)")] [Range(0f, 1f)]
        public float initialProgress;

        [Tooltip("Current active state of the timer")]
        public bool isActive;

        [Tooltip("Current time value of the timer")] [SerializeField]
        private float currentTime;

        [Header("UI Auto Update (No Code Required)")]
        [Tooltip("Image component to automatically update fillAmount (0-1). Auto-finds if on same GameObject")]
        public Image progressImage;

#if UNITY_TEXTMESHPRO
        [Tooltip("TextMeshPro component to automatically display time. Auto-finds if on same GameObject")]
        public TMP_Text timeText;
#endif

        [Tooltip("Format for time display: {0} = time, {1} = progress, {2} = percent")]
        public string timeFormat = "{0:F1}s";

        [Tooltip("If true, progress image fills from left to right. If false, fills from right to left")]
        public bool fillImageNormal = true;

        [Header("Auto Actions (No Code Required)")] [Tooltip("Enable automatic actions when timer completes")]
        public bool enableAutoActions;

        [Tooltip("GameObjects to activate when timer completes")]
        public GameObject[] activateOnComplete;

        [Tooltip("GameObjects to deactivate when timer completes")]
        public GameObject[] deactivateOnComplete;

        [Tooltip("Enable automatic restart of other TimerObjects when this timer completes")]
        public bool autoRestartOtherTimers;

        [Tooltip("Other TimerObjects to restart when this timer completes")]
        public TimerObject[] timersToRestart;

        [Header("Visual Feedback")] [Tooltip("Enable visual feedback (scale animation) when timer starts")]
        public bool enableStartAnimation;

        [Tooltip("Scale multiplier for start animation")] [Range(0.5f, 2f)]
        public float startAnimationScale = 1.2f;

        [Tooltip("Duration of start animation in seconds")] [Min(0.01f)]
        public float startAnimationDuration = 0.2f;

        [Header("Progress Milestones")] [Tooltip("Enable milestone events at specific progress percentages")]
        public bool enableMilestones;

        [Tooltip("Progress percentages (0-1) to trigger milestone events")] [Range(0f, 1f)]
        public float[] milestonePercentages = { 0.25f, 0.5f, 0.75f };

        [Tooltip("Called when timer reaches a milestone percentage")]
        public UnityEvent<float> OnMilestoneReached;

        [Header("Events")] [Tooltip("Called when timer starts")]
        public UnityEvent OnTimerStarted;

        [Tooltip("Called when timer is paused")]
        public UnityEvent OnTimerPaused;

        [Tooltip("Called when timer is resumed")]
        public UnityEvent OnTimerResumed;

        [Tooltip("Called when timer stops")] public UnityEvent OnTimerStopped;

        [Tooltip("Called with current time value on each update")]
        public UnityEvent<float> OnTimeChanged;

        [Tooltip("Called with progress (0-1) on each update")]
        public UnityEvent<float> OnProgressChanged;

        [Tooltip("Called with progress percent (0-100) on each update")]
        public UnityEvent<int> OnProgressPercentChanged;

        [Tooltip("Called when timer completes. Also called on each loop completion if looping is enabled")]
        public UnityEvent OnTimerCompleted;

        private float timeSinceLastUpdate;
        private bool[] milestoneReached;
        private float lastProgress = -1f;
        private Vector3 originalScale;

        /// <summary>
        ///     Gets whether timer is currently running
        /// </summary>
        public bool IsRunning => isActive;

        /// <summary>
        ///     Gets whether timer has completed
        /// </summary>
        public bool IsCompleted => currentTime >= duration;

        /// <summary>
        ///     Gets current progress (0-1)
        /// </summary>
        public float Progress => GetProgress();

        /// <summary>
        ///     Gets current time value
        /// </summary>
        public float CurrentTime => GetCurrentTime();

        private void OnValidate()
        {
            if (duration < 0)
            {
                duration = 0;
            }

            if (updateInterval < 0.015f)
            {
                updateInterval = 0.015f;
            }

            if (initialProgress < 0)
            {
                initialProgress = 0;
            }

            if (initialProgress > 1)
            {
                initialProgress = 1;
            }

            // Автоматически находим компоненты, если не заданы
            if (progressImage == null)
            {
                progressImage = GetComponent<Image>();
            }

#if UNITY_TEXTMESHPRO
            if (timeText == null)
                timeText = GetComponent<TMP_Text>();
#endif

            // Валидация milestones
            if (milestonePercentages != null)
            {
                for (int i = 0; i < milestonePercentages.Length; i++)
                {
                    milestonePercentages[i] = Mathf.Clamp01(milestonePercentages[i]);
                }
            }

            // Валидация визуальных настроек
            if (startAnimationScale < 0.5f)
            {
                startAnimationScale = 0.5f;
            }

            if (startAnimationScale > 2f)
            {
                startAnimationScale = 2f;
            }

            if (startAnimationDuration < 0.01f)
            {
                startAnimationDuration = 0.01f;
            }
        }

        private void Awake()
        {
            originalScale = transform.localScale;

            // Инициализация milestones
            if (enableMilestones && milestonePercentages != null && milestonePercentages.Length > 0)
            {
                milestoneReached = new bool[milestonePercentages.Length];
            }

            // Установка начального времени
            if (initialProgress > 0)
            {
                currentTime = countUp ? initialProgress * duration : (1f - initialProgress) * duration;
            }
        }

        /// <summary>
        ///     Resets timer to initial state
        /// </summary>
        public void Reset()
        {
            currentTime = initialProgress > 0
                ? countUp ? initialProgress * duration : (1f - initialProgress) * duration
                : 0f;

            // Сброс milestones
            if (milestoneReached != null)
            {
                for (int i = 0; i < milestoneReached.Length; i++)
                {
                    milestoneReached[i] = false;
                }
            }

            lastProgress = -1f;
            InvokeEvents();
            UpdateUI();
        }

        private void Start()
        {
            if (autoStart)
            {
                Play();
            }
            else
            {
                UpdateUI(); // Обновляем UI даже если не запускаем автоматически
            }
        }

        private void Update()
        {
            if (!isActive)
            {
                return;
            }

            if (pauseOnTimeScaleZero && !useUnscaledTime && Time.timeScale == 0f)
            {
                return;
            }

            float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            timeSinceLastUpdate += deltaTime;

            if (timeSinceLastUpdate >= updateInterval)
            {
                UpdateTimer(timeSinceLastUpdate);
                timeSinceLastUpdate = 0f;
            }
        }

        private void UpdateTimer(float deltaTime)
        {
            if (!isActive)
            {
                return;
            }

            currentTime += deltaTime;

            if (currentTime >= duration)
            {
                currentTime = duration;
                InvokeEvents();
                OnTimerCompleted?.Invoke();

                if (looping)
                {
                    currentTime = 0f;
                    isActive = true;
                    timeSinceLastUpdate = 0f;

                    // Сброс milestones при looping
                    if (milestoneReached != null)
                    {
                        for (int i = 0; i < milestoneReached.Length; i++)
                        {
                            milestoneReached[i] = false;
                        }
                    }

                    lastProgress = -1f;

                    OnTimerStarted?.Invoke();
                    HandleAutoActions();
                }
                else
                {
                    isActive = false;
                    HandleAutoActions();
                }
            }
            else
            {
                InvokeEvents();
            }
        }

        private void InvokeEvents()
        {
            float progress;
            float timeValue;

            if (countUp)
            {
                timeValue = currentTime;
                progress = duration > 0 ? currentTime / duration : 0f;
            }
            else
            {
                timeValue = duration - currentTime;
                progress = duration > 0 ? 1f - currentTime / duration : 0f;
            }

            progress = Mathf.Clamp01(progress);

            // Вызываем события только если прогресс изменился
            if (Mathf.Abs(progress - lastProgress) > 0.001f)
            {
                OnTimeChanged?.Invoke(timeValue);
                OnProgressChanged?.Invoke(progress);
                OnProgressPercentChanged?.Invoke(Mathf.RoundToInt(progress * 100f));
                lastProgress = progress;

                // Проверка milestones
                if (enableMilestones && milestonePercentages != null && milestoneReached != null)
                {
                    for (int i = 0; i < milestonePercentages.Length; i++)
                    {
                        if (!milestoneReached[i] && progress >= milestonePercentages[i])
                        {
                            milestoneReached[i] = true;
                            OnMilestoneReached?.Invoke(milestonePercentages[i]);
                        }
                    }
                }
            }

            // Обновляем UI автоматически
            UpdateUI(progress, timeValue);
        }

        private void UpdateUI(float progress = -1f, float timeValue = -1f)
        {
            if (progress < 0)
            {
                progress = GetProgress();
                timeValue = GetCurrentTime();
            }

            // Обновление Image fillAmount
            if (progressImage != null)
            {
                float fillAmount = fillImageNormal ? progress : 1f - progress;
                progressImage.fillAmount = fillAmount;
            }

            // Обновление текста времени
#if UNITY_TEXTMESHPRO
            if (timeText != null)
            {
                string formattedText = string.Format(timeFormat, timeValue, progress, progress * 100f);
                timeText.text = formattedText;
            }
#endif
        }

        /// <summary>
        ///     Starts or restarts timer with optional new parameters
        /// </summary>
        /// <param name="newDuration">New duration in seconds. If negative, keeps current duration</param>
        /// <param name="newUpdateInterval">New update interval in seconds. If negative, keeps current interval</param>
        public void StartTimer(float newDuration = -1f, float newUpdateInterval = -1f)
        {
            if (newDuration >= 0)
            {
                duration = newDuration;
            }

            if (newUpdateInterval >= 0)
            {
                updateInterval = newUpdateInterval;
            }

            Reset();
            Play();
        }

        /// <summary>
        ///     Starts or resumes the timer
        /// </summary>
#if ODIN_INSPECTOR
            [Button]
#else
        [ButtonAttribute]
#endif
        public void Play()
        {
            currentTime = 0f;
            isActive = true;
            timeSinceLastUpdate = 0f;

            OnTimerStarted?.Invoke();

            // Визуальная анимация при старте
            if (enableStartAnimation && Application.isPlaying)
            {
                PlayStartAnimation();
            }

            InvokeEvents();
        }

        private void PlayStartAnimation()
        {
#if DG_TWEENING
            transform.localScale = originalScale;
            transform.DOScale(originalScale * startAnimationScale, startAnimationDuration * 0.5f)
                .SetEase(DG.Tweening.Ease.OutQuad)
                .OnComplete(() =>
                {
                    transform.DOScale(originalScale, startAnimationDuration * 0.5f)
                        .SetEase(DG.Tweening.Ease.InQuad);
                });
#else
            // Fallback без DOTween
            StartCoroutine(StartAnimationCoroutine());
#endif
        }

        private IEnumerator StartAnimationCoroutine()
        {
            float elapsed = 0f;
            float halfDuration = startAnimationDuration * 0.5f;

            // Scale up
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                float scale = Mathf.Lerp(1f, startAnimationScale, t);
                transform.localScale = originalScale * scale;
                yield return null;
            }

            elapsed = 0f;

            // Scale down
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                float scale = Mathf.Lerp(startAnimationScale, 1f, t);
                transform.localScale = originalScale * scale;
                yield return null;
            }

            transform.localScale = originalScale;
        }

        private void HandleAutoActions()
        {
            if (!enableAutoActions)
            {
                return;
            }

            // Активация объектов
            if (activateOnComplete != null)
            {
                foreach (GameObject obj in activateOnComplete)
                {
                    if (obj != null)
                    {
                        obj.SetActive(true);
                    }
                }
            }

            // Деактивация объектов
            if (deactivateOnComplete != null)
            {
                foreach (GameObject obj in deactivateOnComplete)
                {
                    if (obj != null)
                    {
                        obj.SetActive(false);
                    }
                }
            }

            // Перезапуск других таймеров
            if (autoRestartOtherTimers && timersToRestart != null)
            {
                foreach (TimerObject timer in timersToRestart)
                {
                    if (timer != null)
                    {
                        timer.Play();
                    }
                }
            }
        }

        /// <summary>
        ///     Pauses or resumes the timer
        /// </summary>
        /// <param name="paused">True to pause, false to resume</param>
#if ODIN_INSPECTOR
            [Button]
#else
        [ButtonAttribute]
#endif
        public void Pause(bool paused = true)
        {
            if (isActive == !paused)
            {
                return;
            }

            isActive = !paused;
            if (paused)
            {
                OnTimerPaused?.Invoke();
            }
            else
            {
                OnTimerResumed?.Invoke();
            }
        }

        /// <summary>
        ///     Toggles between paused and running states
        /// </summary>
        public void TogglePause()
        {
            Pause(!isActive);
        }

        /// <summary>
        ///     Stops and resets the timer
        /// </summary>
#if ODIN_INSPECTOR
            [Button]
#else
        [ButtonAttribute]
#endif
        public void Stop()
        {
            if (!isActive)
            {
                return;
            }

            isActive = false;
            OnTimerStopped?.Invoke();
            Reset();
        }

        /// <summary>
        ///     Resumes the timer from pause
        /// </summary>
#if ODIN_INSPECTOR
            [Button]
#else
        [ButtonAttribute]
#endif
        public void Resume()
        {
            Pause(false);
        }

        /// <summary>
        ///     Sets the current time value
        /// </summary>
        /// <param name="time">New time value in seconds</param>
        public void SetTime(float time)
        {
            currentTime = Mathf.Clamp(time, 0f, duration);
            lastProgress = -1f; // Сбрасываем для принудительного обновления
            InvokeEvents();
        }

        /// <summary>
        ///     Sets the current progress (0-1)
        /// </summary>
        /// <param name="progress">Progress value from 0 to 1</param>
        public void SetProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            if (countUp)
            {
                currentTime = progress * duration;
            }
            else
            {
                currentTime = (1f - progress) * duration;
            }

            lastProgress = -1f;
            InvokeEvents();
        }

        /// <summary>
        ///     Adds time to the timer
        /// </summary>
        /// <param name="seconds">Seconds to add (can be negative)</param>
        public void AddTime(float seconds)
        {
            currentTime = Mathf.Clamp(currentTime + seconds, 0f, duration);
            lastProgress = -1f;
            InvokeEvents();
        }

        /// <summary>
        ///     Gets current progress (0-1)
        /// </summary>
        /// <returns>Progress value from 0 to 1</returns>
        public float GetProgress()
        {
            if (duration <= 0)
            {
                return 0f;
            }

            return countUp ? currentTime / duration : 1f - currentTime / duration;
        }

        /// <summary>
        ///     Gets current time value
        /// </summary>
        /// <returns>Current time in seconds</returns>
        public float GetCurrentTime()
        {
            return countUp ? currentTime : duration - currentTime;
        }

        /// <summary>
        ///     Gets remaining time
        /// </summary>
        /// <returns>Remaining time in seconds</returns>
        public float GetRemainingTime()
        {
            return countUp ? duration - currentTime : currentTime;
        }
    }
}