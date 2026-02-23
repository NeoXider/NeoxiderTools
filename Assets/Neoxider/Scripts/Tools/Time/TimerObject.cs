using System;
using System.Collections;
using System.Globalization;
using Neo.Reactive;
using Neo.Save;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Random = UnityEngine.Random;
#if UNITY_TEXTMESHPRO
using TMPro;
#endif


namespace Neo
{
    /// <summary>How to persist timer state: by current seconds or by real end/start time (UTC).</summary>
    public enum TimerSaveMode
    {
        /// <summary>Save current time value; on load continue from that value.</summary>
        Seconds,

        /// <summary>Save target time (UTC); on load remaining time is recalculated from now.</summary>
        RealTime
    }

    /// <summary>
    ///     MonoBehaviour-based timer with Unity events support and automatic UI updates
    /// </summary>
    [NeoDoc("Tools/Time/TimerObject.md")]
    [CreateFromMenu("Neoxider/Tools/Time/TimerObject")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(TimerObject))]
    public class TimerObject : MonoBehaviour
    {
        [Header("Timer Settings")] [Tooltip("Total duration of the timer in seconds")] [Min(0f)]
        public float duration = 1f;

        [Tooltip("Update frequency in seconds")] [Min(0.015f)]
        public float updateInterval = 0.015f;

        [Tooltip("If true, time counts up; if false, counts down")]
        public bool countUp = true;

        [Tooltip("Use unscaled time (ignores time scale)")]
        public bool useUnscaledTime;

        [Tooltip("Automatically pause when Time.timeScale == 0")]
        public bool pauseOnTimeScaleZero = true;

        [Tooltip("Automatically restart when complete")]
        public bool looping;

        [Tooltip(
            "Infinite time (no max). Time only increases; progress is not updated. Disables looping/random in OnValidate.")]
        public bool infiniteDuration;

        [Header("Random Duration")]
        [Tooltip(
            "If enabled, duration is randomized in [min,max] on Play/StartTimer. If looping is enabled, it randomizes each cycle.")]
        public bool useRandomDuration;

        [Tooltip("Minimum random duration (seconds)")] [Min(0f)]
        public float randomDurationMin = 1f;

        [Tooltip("Maximum random duration (seconds)")] [Min(0f)]
        public float randomDurationMax = 2f;

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

        [Header("Visual Feedback")] [Tooltip("Enable visual feedback (scale animation) when timer starts")]
        public bool enableStartAnimation;

        [Tooltip("Scale multiplier for start animation")] [Range(0.5f, 2f)]
        public float startAnimationScale = 1.2f;

        [Tooltip("Duration of start animation in seconds")] [Min(0.01f)]
        public float startAnimationDuration = 0.2f;

        [Tooltip("Called when timer starts")] public UnityEvent OnTimerStarted;

        [Tooltip("Called when timer is paused")]
        public UnityEvent OnTimerPaused;

        [Tooltip("Called when timer is resumed")]
        public UnityEvent OnTimerResumed;

        [Tooltip("Called when timer stops")] public UnityEvent OnTimerStopped;

        [Tooltip("Reactive current time; subscribe via Time.OnChanged")]
        public ReactivePropertyFloat Time = new();

        /// <summary>Текущее значение таймера в секундах (для NeoCondition и рефлексии).</summary>
        public float TimeValue => Time.CurrentValue;

        [Tooltip("Called with progress (0-1) on each update")]
        public UnityEvent<float> OnProgressChanged;

        [Tooltip("Called with progress percent (0-100) on each update")]
        public UnityEvent<int> OnProgressPercentChanged;

        [Tooltip("Called when timer completes. Also called on each loop completion if looping is enabled")]
        public UnityEvent OnTimerCompleted;

        [Header("Progress Milestones")] [Tooltip("Enable milestone events at specific progress percentages")]
        public bool enableMilestones;

        [Tooltip("Progress percentages (0-1) to trigger milestone events")] [Range(0f, 1f)]
        public float[] milestonePercentages = { 0.25f, 0.5f, 0.75f };

        [Tooltip("Called when timer reaches a milestone percentage")]
        public UnityEvent<float> OnMilestoneReached;

        [Header("Save")]
        [Tooltip("Save and restore timer state (current time and running state). Disabled by default.")]
        [SerializeField]
        protected bool saveProgress;

        [Tooltip(
            "Seconds: save current time value; on load continue from that value. RealTime: save target time (UTC); on load remaining time is recalculated (e.g. re-enter after 1 min → countdown continues from real end time).")]
        [SerializeField]
        protected TimerSaveMode saveMode = TimerSaveMode.Seconds;

        [Tooltip("Unique key for SaveProvider. Required when Save Progress is enabled.")] [SerializeField]
        private string saveKey = "TimerObject";

        /// <summary>
        ///     Returns the save key used for persistence. Override in derived classes to use a custom key.
        /// </summary>
        protected virtual string GetSaveKey()
        {
            return saveKey;
        }

        private bool _loadedFromSave;
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
        public bool IsCompleted => !infiniteDuration && (countUp ? currentTime >= duration : currentTime <= 0f);

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
            if (infiniteDuration)
            {
                looping = false;
                useRandomDuration = false;
                countUp = true;
            }

            if (randomDurationMax < randomDurationMin)
            {
                randomDurationMax = randomDurationMin;
            }

            if (progressImage == null)
            {
                progressImage = GetComponent<Image>();
            }

#if UNITY_TEXTMESHPRO
            if (timeText == null)
                timeText = GetComponent<TMP_Text>();
#endif

            if (milestonePercentages != null)
            {
                for (int i = 0; i < milestonePercentages.Length; i++)
                {
                    milestonePercentages[i] = Mathf.Clamp01(milestonePercentages[i]);
                }
            }
        }

        private void Awake()
        {
            Init();
        }

        protected virtual void Init()
        {
            originalScale = transform.localScale;

            if (enableMilestones && milestonePercentages != null && milestonePercentages.Length > 0)
            {
                milestoneReached = new bool[milestonePercentages.Length];
            }

            if (initialProgress > 0)
            {
                currentTime = countUp ? initialProgress * duration : (1f - initialProgress) * duration;
            }

            if (saveProgress && !string.IsNullOrEmpty(GetSaveKey()))
            {
                _loadedFromSave = LoadState();
            }

            Time.Value = GetCurrentTime();
        }

        private void ApplyRandomDurationIfNeeded()
        {
            if (!useRandomDuration || infiniteDuration || !Application.isPlaying)
            {
                return;
            }

            float min = Mathf.Max(0f, randomDurationMin);
            float max = Mathf.Max(min, randomDurationMax);
            duration = Random.Range(min, max);
        }

        private float GetStartTimeForCurrentDuration()
        {
            if (initialProgress <= 0f)
            {
                return countUp ? 0f : duration;
            }

            return countUp ? initialProgress * duration : (1f - initialProgress) * duration;
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (_loadedFromSave)
            {
                _loadedFromSave = false;
                InvokeEvents();
                UpdateUI();
            }
            else if (autoStart)
            {
                Play();
            }
            else
            {
                UpdateUI();
            }
        }

        private void OnDisable()
        {
            if (saveProgress && !string.IsNullOrEmpty(GetSaveKey()))
            {
                SaveState();
            }
        }

        /// <summary>
        ///     Saves current timer state to SaveProvider. Override or call from derived classes when restarting cooldown.
        /// </summary>
        protected virtual void SaveState()
        {
            string key = GetSaveKey();
            if (saveMode == TimerSaveMode.Seconds)
            {
                SaveProvider.SetFloat(key + "_t", currentTime);
                SaveProvider.SetBool(key + "_a", isActive);
                return;
            }

            if (infiniteDuration)
            {
                if (saveMode == TimerSaveMode.RealTime)
                {
                    DateTime startUtc = DateTime.UtcNow.AddSeconds(-currentTime);
                    SaveProvider.SetString(key + "_rt", startUtc.ToString("o"));
                    SaveProvider.SetBool(key + "_a", isActive);
                }
                else
                {
                    SaveProvider.SetFloat(key + "_t", currentTime);
                    SaveProvider.SetBool(key + "_a", isActive);
                }

                return;
            }

            if (countUp)
            {
                DateTime startUtc = DateTime.UtcNow.AddSeconds(-currentTime);
                SaveProvider.SetString(key + "_rt", startUtc.ToString("o"));
            }
            else
            {
                DateTime endUtc = DateTime.UtcNow.AddSeconds(currentTime);
                SaveProvider.SetString(key + "_rt", endUtc.ToString("o"));
            }
        }

        private bool LoadState()
        {
            string key = GetSaveKey();
            if (infiniteDuration && saveMode == TimerSaveMode.RealTime)
            {
                if (!SaveProvider.HasKey(key + "_rt"))
                {
                    return false;
                }

                string raw = SaveProvider.GetString(key + "_rt", null);
                if (string.IsNullOrEmpty(raw) ||
                    !DateTime.TryParse(raw, null, DateTimeStyles.RoundtripKind, out DateTime startUtc))
                {
                    return false;
                }

                currentTime = (float)(DateTime.UtcNow - startUtc).TotalSeconds;
                currentTime = Mathf.Max(0f, currentTime);
                isActive = SaveProvider.GetBool(key + "_a", true);
                lastProgress = -1f;
                return true;
            }

            if (saveMode == TimerSaveMode.Seconds || infiniteDuration)
            {
                if (!SaveProvider.HasKey(key + "_t"))
                {
                    return false;
                }

                currentTime = SaveProvider.GetFloat(key + "_t", countUp ? 0f : duration);
                if (!infiniteDuration)
                {
                    currentTime = Mathf.Clamp(currentTime, 0f, duration);
                }

                isActive = SaveProvider.GetBool(key + "_a");
                lastProgress = -1f;
                Time.Value = currentTime;
                return true;
            }

            if (!SaveProvider.HasKey(key + "_rt"))
            {
                return false;
            }

            string rawRt = SaveProvider.GetString(key + "_rt", null);
            if (string.IsNullOrEmpty(rawRt) ||
                !DateTime.TryParse(rawRt, null, DateTimeStyles.RoundtripKind, out DateTime savedUtc))
            {
                return false;
            }

            DateTime now = DateTime.UtcNow;
            if (countUp)
            {
                currentTime = (float)(now - savedUtc).TotalSeconds;
                currentTime = Mathf.Clamp(currentTime, 0f, duration);
                isActive = currentTime < duration;
            }
            else
            {
                currentTime = (float)(savedUtc - now).TotalSeconds;
                currentTime = Mathf.Clamp(currentTime, 0f, duration);
                isActive = currentTime > 0f;
            }

            lastProgress = -1f;
            Time.Value = currentTime;
            return true;
        }

        /// <summary>
        ///     Resets timer to initial state
        /// </summary>
        public void Reset()
        {
            currentTime = infiniteDuration
                ? 0f
                : initialProgress > 0
                    ? countUp ? initialProgress * duration : (1f - initialProgress) * duration
                    : 0f;
            Time.Value = currentTime;

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
        }

        private void Update()
        {
            if (!isActive)
            {
                return;
            }

            if (pauseOnTimeScaleZero && !useUnscaledTime && UnityEngine.Time.timeScale == 0f)
            {
                return;
            }

            float deltaTime = useUnscaledTime ? UnityEngine.Time.unscaledDeltaTime : UnityEngine.Time.deltaTime;
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

            if (infiniteDuration)
            {
                currentTime += deltaTime;
                Time.Value = currentTime;
                UpdateUI(timeValue: currentTime);
                return;
            }

            currentTime += countUp ? deltaTime : -deltaTime;

            bool reachedEnd = countUp ? currentTime >= duration : currentTime <= 0f;
            if (reachedEnd)
            {
                currentTime = countUp ? duration : 0f;
                InvokeEvents();
                OnTimerCompleted?.Invoke();

                if (looping)
                {
                    ApplyRandomDurationIfNeeded();
                    currentTime = GetStartTimeForCurrentDuration();
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
                    InvokeEvents();
                }
                else
                {
                    isActive = false;
                }
            }
            else
            {
                InvokeEvents();
            }
        }

        private void InvokeEvents()
        {
            if (infiniteDuration)
            {
                float timeValueInfinite = currentTime;
                Time.Value = timeValueInfinite;
                UpdateUI(timeValue: timeValueInfinite);
                return;
            }

            float timeValue = currentTime;
            float progress = duration > 0
                ? countUp ? currentTime / duration : 1f - currentTime / duration
                : 0f;

            progress = Mathf.Clamp01(progress);

            Time.Value = timeValue;
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

            // Обновляем UI автоматически
            UpdateUI(progress, timeValue);
        }

        private void UpdateUI(float progress = -1f, float timeValue = -1f)
        {
            if (infiniteDuration)
            {
#if UNITY_TEXTMESHPRO
                if (timeText != null)
                {
                    float t = timeValue >= 0 ? timeValue : currentTime;
                    timeText.text = string.Format(timeFormat, t, 0f, 0f);
                }
#endif
                return;
            }

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
        [Button]
        public void Play()
        {
            ApplyRandomDurationIfNeeded();
            currentTime = infiniteDuration ? 0f : GetStartTimeForCurrentDuration();

            lastProgress = -1f;
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
                elapsed += UnityEngine.Time.deltaTime;
                float t = elapsed / halfDuration;
                float scale = Mathf.Lerp(1f, startAnimationScale, t);
                transform.localScale = originalScale * scale;
                yield return null;
            }

            elapsed = 0f;

            // Scale down
            while (elapsed < halfDuration)
            {
                elapsed += UnityEngine.Time.deltaTime;
                float t = elapsed / halfDuration;
                float scale = Mathf.Lerp(startAnimationScale, 1f, t);
                transform.localScale = originalScale * scale;
                yield return null;
            }

            transform.localScale = originalScale;
        }

        /// <summary>
        ///     Pauses or resumes the timer
        /// </summary>
        /// <param name="paused">True to pause, false to resume</param>
        [Button]
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
        [Button]
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
        [Button]
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
        ///     Sets the timer duration. Optionally preserves progress ratio.
        /// </summary>
        /// <param name="newDuration">New duration in seconds.</param>
        /// <param name="keepProgress">If true, scales current time to preserve progress; otherwise clamps current time.</param>
        public void SetDuration(float newDuration, bool keepProgress = true)
        {
            if (newDuration < 0f)
            {
                newDuration = 0f;
            }

            if (keepProgress && duration > 0f && !infiniteDuration)
            {
                float ratio = countUp ? currentTime / duration : 1f - currentTime / duration;
                ratio = Mathf.Clamp01(ratio);
                duration = newDuration;
                currentTime = countUp ? ratio * duration : (1f - ratio) * duration;
            }
            else
            {
                duration = newDuration;
                currentTime = Mathf.Clamp(currentTime, 0f, duration);
            }

            lastProgress = -1f;
            InvokeEvents();
        }

        /// <summary>
        ///     Gets current progress (0-1)
        /// </summary>
        /// <returns>Progress value from 0 to 1</returns>
        public float GetProgress()
        {
            if (infiniteDuration)
            {
                return 0f;
            }

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
            return currentTime;
        }

        /// <summary>
        ///     Gets remaining time
        /// </summary>
        /// <returns>Remaining time in seconds</returns>
        public float GetRemainingTime()
        {
            if (infiniteDuration)
            {
                return 0f;
            }

            return countUp ? duration - currentTime : currentTime;
        }
    }
}