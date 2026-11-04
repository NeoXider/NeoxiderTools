using System;
using System.Collections;
using System.Globalization;
using Neo.Reactive;
using Neo.Save;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Random = UnityEngine.Random;


namespace Neo.Tools
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

        [Tooltip("Time speed multiplier (1 = real time, 2 = twice as fast). Applied to delta before updating.")]
        [Min(0.001f)]
        public float timeScale = 1f;

        [Tooltip("Automatically pause when Time.timeScale == 0")]
        public bool pauseOnTimeScaleZero = true;

        [Tooltip("Automatically restart when complete")]
        public bool looping;

        [Tooltip(
            "Looping only: start the next cycle with the time by which the last tick ran past the end, so the " +
            "long-run period matches elapsed time. Off keeps the legacy restart from the start value.")]
        [SerializeField]
        private bool _carryLoopOvershoot;

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

        [Tooltip(
            "Optional TextMeshPro label that displays the time through Time Format. Assign it explicitly: it is " +
            "never auto-found, so a label driven by another component (for example TimeToText) is left alone.")]
        public TMP_Text timeText;

        [Tooltip(
            "Format for time display: {0} = time, {1} = progress, {2} = percent. An invalid format falls back to " +
            "\"{0:F1}s\".")]
        public string timeFormat = DefaultTimeFormat;

        [Tooltip("If true, progress image fills from left to right. If false, fills from right to left")]
        public bool fillImageNormal = true;

        [Header("Visual Feedback")] [Tooltip("Enable visual feedback (scale animation) when timer starts")]
        public bool enableStartAnimation;

        [Tooltip("Scale multiplier for start animation")] [Range(0.5f, 2f)]
        public float startAnimationScale = 1.2f;

        [Tooltip("Duration of start animation in seconds")] [Min(0.01f)]
        public float startAnimationDuration = 0.2f;

        [Tooltip("Called when timer starts")] public UnityEvent OnTimerStarted = new();

        [Tooltip("Called when timer is paused")]
        public UnityEvent OnTimerPaused = new();

        [Tooltip("Called when timer is resumed")]
        public UnityEvent OnTimerResumed = new();

        [Tooltip("Called when timer stops")] public UnityEvent OnTimerStopped = new();

        [Tooltip("Reactive current time; subscribe via Time.OnChanged")]
        public ReactivePropertyFloat Time = new();

        /// <summary>Current timer value in seconds (for NeoCondition and reflection).</summary>
        public float TimeValue => Time.CurrentValue;

        [Tooltip("Called with progress (0-1) on each update")]
        public UnityEvent<float> OnProgressChanged = new();

        [Tooltip("Called with progress percent (0-100) on each update")]
        public UnityEvent<int> OnProgressPercentChanged = new();

        [Tooltip("Called when timer completes. Also called on each loop completion if looping is enabled")]
        public UnityEvent OnTimerCompleted = new();

        [Header("Progress Milestones")] [Tooltip("Enable milestone events at specific progress percentages")]
        public bool enableMilestones;

        [Tooltip("Progress percentages (0-1) to trigger milestone events")] [Range(0f, 1f)]
        public float[] milestonePercentages = { 0.25f, 0.5f, 0.75f };

        [Tooltip("Called when timer reaches a milestone percentage")]
        public UnityEvent<float> OnMilestoneReached = new();

        [Header("Day Time (00:00–24:00)")]
        [Tooltip(
            "When > 0, timer acts as day cycle: time wraps at this value (e.g. 86400 = 24h). DayTimeHours/Minutes/Seconds and OnNewDay/OnDayTimeChanged apply. Set to 0 to disable.")]
        [Min(0f)]
        [SerializeField]
        private float _dayLengthSeconds;

        [Tooltip(
            "Format for day time display when day length > 0. {0}=hours, {1}=minutes, {2}=seconds (e.g. \"{0:00}:{1:00}\" for HH:mm)")]
        [SerializeField]
        private string _dayTimeFormat = DefaultDayTimeFormat;

        [Tooltip("Called when day cycle wraps (midnight) — only when dayLengthSeconds > 0 and looping.")]
        public UnityEvent OnNewDay = new();

        [Tooltip("Called when day time updates (hours, minutes, seconds) — only when dayLengthSeconds > 0.")]
        public UnityEvent<float, float, float> OnDayTimeChanged = new();

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

        /// <summary>
        ///     Returns the UTC clock used by every RealTime save and restore calculation.
        ///     Override to keep derived eligibility checks and persisted deadlines in the same clock domain.
        ///     The default implementation uses the device UTC clock.
        /// </summary>
        protected virtual DateTime GetUtcNow()
        {
            return _utcClock != null ? _utcClock.UtcNow : DateTime.UtcNow;
        }

        /// <summary>
        ///     UTC source used by the default <see cref="GetUtcNow" /> for RealTime persistence. Defaults to the device
        ///     clock; assign <see cref="NeoTrustedTime.AsClock" /> for tamper-resistant offline progress or a fake
        ///     <see cref="INeoClock" /> in tests. Derived timers that override <see cref="GetUtcNow" /> ignore it.
        ///     Assign it before the GameObject is activated (<c>Awake</c> restores the save) so restore and save share
        ///     one clock domain; scene-placed timers should override <see cref="GetUtcNow" /> instead.
        /// </summary>
        public INeoClock UtcClock
        {
            get => _utcClock;
            set => _utcClock = value;
        }

        /// <summary>
        ///     Frame time source used by <see cref="Update" /> and by the <see cref="pauseOnTimeScaleZero" /> check.
        ///     Defaults to <see cref="UnityFrameClock.Instance" />; assign <see cref="ManualFrameClock" /> for
        ///     deterministic pacing. Assigning <c>null</c> restores the default.
        /// </summary>
        public IFrameClock FrameClock
        {
            get => _frameClock ?? UnityFrameClock.Instance;
            set => _frameClock = value;
        }

        private INeoClock _utcClock;
        private IFrameClock _frameClock;

        // WHY: currentTime is a float; at day-cycle magnitudes (86400 s) one ulp is ~8 ms, so adding a 16 ms frame
        // delta rounds by up to 50%. Carrying the rounding residual in double keeps long timers exact.
        private double _timeCarry;
        private TimerTextFormatter _textFormatter;
        private TMP_Text _formattedTextTarget;

        // WHY: A format that once threw is remembered, so a broken stored format costs one exception and one
        // warning instead of an exception (and a parse) on every update.
        private string _rejectedTimeFormat;
        private string _rejectedDayTimeFormat;

        private bool _loadedFromSave;
        private float timeSinceLastUpdate;
        private bool[] milestoneReached;
        private float lastProgress = -1f;
        private Vector3 originalScale;
        private int playbackGeneration;
        private Coroutine _startAnimation;

        private const string DefaultTimeFormat = "{0:F1}s";
        private const string DefaultDayTimeFormat = "{0:00}:{1:00}";

        /// <summary>
        ///     Looping only: when true, the next cycle starts with the time by which the completing tick overshot the
        ///     end, keeping the long-run period exact. Whole cycles skipped by one long tick are dropped (one completion,
        ///     phase kept), so the new cycle never starts already complete. Default false keeps the legacy restart.
        /// </summary>
        public bool CarryLoopOvershoot
        {
            get => _carryLoopOvershoot;
            set => _carryLoopOvershoot = value;
        }

        /// <summary>
        ///     Day cycle length in seconds (0 = disabled). When > 0, time wraps at this value and DayTime* / OnNewDay /
        ///     OnDayTimeChanged apply.
        /// </summary>
        public float dayLengthSeconds => _dayLengthSeconds;

        /// <summary>
        ///     Format string for day time display when dayLengthSeconds > 0. {0}=hours, {1}=minutes, {2}=seconds.
        /// </summary>
        public string dayTimeFormat => _dayTimeFormat;

        /// <summary>
        ///     Current time of day hours (0–24) when dayLengthSeconds > 0; otherwise 0.
        /// </summary>
        public float DayTimeHours => _dayLengthSeconds > 0 ? GetDayTimeSeconds() / 3600f : 0f;

        /// <summary>
        ///     Current time of day minutes (0–59) when dayLengthSeconds > 0; otherwise 0.
        /// </summary>
        public float DayTimeMinutes => _dayLengthSeconds > 0 ? GetDayTimeSeconds() % 3600f / 60f : 0f;

        /// <summary>
        ///     Current time of day seconds (0–59) when dayLengthSeconds > 0; otherwise 0.
        /// </summary>
        public float DayTimeSeconds => _dayLengthSeconds > 0 ? GetDayTimeSeconds() % 60f : 0f;

        private float GetDayTimeSeconds()
        {
            if (_dayLengthSeconds <= 0f)
            {
                return 0f;
            }

            float t = currentTime % _dayLengthSeconds;
            if (t < 0f)
            {
                t += _dayLengthSeconds;
            }

            return t;
        }

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

        /// <summary>
        ///     Validates serialized timer configuration. Overrides must call <c>base.OnValidate()</c>.
        /// </summary>
        protected virtual void OnValidate()
        {
            if (infiniteDuration)
            {
                looping = false;
                useRandomDuration = false;
                countUp = true;
                _dayLengthSeconds = 0f;
            }
            else if (_dayLengthSeconds > 0f)
            {
                infiniteDuration = false;
            }

            if (randomDurationMax < randomDurationMin)
            {
                randomDurationMax = randomDurationMin;
            }

            if (progressImage == null)
            {
                progressImage = GetComponent<Image>();
            }

            // WHY: timeText is deliberately not auto-found. The field used to be compiled out, so auto-binding it now
            // would make existing timers (and CooldownReward) overwrite labels that TimeToText or game code drive.

            if (milestonePercentages != null)
            {
                for (int i = 0; i < milestonePercentages.Length; i++)
                {
                    milestonePercentages[i] = Mathf.Clamp01(milestonePercentages[i]);
                }
            }
        }

        /// <summary>
        ///     Initializes the timer. Overrides should normally customize <see cref="Init"/> instead;
        ///     if this method is overridden, <c>base.Awake()</c> must be called.
        /// </summary>
        protected virtual void Awake()
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

            if (_dayLengthSeconds > 0f)
            {
                currentTime = currentTime % _dayLengthSeconds;
                if (currentTime < 0f)
                {
                    currentTime += _dayLengthSeconds;
                }
            }

            if (saveProgress && !string.IsNullOrEmpty(GetSaveKey()))
            {
                _loadedFromSave = LoadState();
            }

            _timeCarry = 0d;
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

        /// <summary>
        ///     Restores the loaded state or applies the configured auto-start behavior.
        ///     Overrides must call <c>base.OnEnable()</c>.
        /// </summary>
        protected virtual void OnEnable()
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

        /// <summary>
        ///     Persists timer state when saving is enabled. Overrides must call <c>base.OnDisable()</c>.
        /// </summary>
        protected virtual void OnDisable()
        {
            StopStartAnimation();
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
            float timeToSave = currentTime;
            if (_dayLengthSeconds > 0f)
            {
                timeToSave = GetDayTimeSeconds();
            }

            if (saveMode == TimerSaveMode.Seconds)
            {
                SaveProvider.SetFloat(key + "_t", timeToSave);
                SaveProvider.SetBool(key + "_a", isActive);
                return;
            }

            // Frozen seconds must also survive pause for an unbounded RealTime timer.
            if (!isActive)
            {
                SaveProvider.SetFloat(key + "_t", timeToSave);
            }

            if (infiniteDuration)
            {
                if (saveMode == TimerSaveMode.RealTime)
                {
                    DateTime startUtc = GetUtcNow().AddSeconds(-currentTime);
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

            // WHY: RealTime load used to derive isActive purely from remaining time, so a paused
            // (or never-started) timer came back running and drained in real time. Persist the
            // active flag, plus the frozen seconds for inactive timers, so pause survives restarts.
            SaveProvider.SetBool(key + "_a", isActive);

            if (countUp)
            {
                DateTime startUtc = GetUtcNow().AddSeconds(-currentTime);
                SaveProvider.SetString(key + "_rt", startUtc.ToString("o"));
            }
            else
            {
                DateTime endUtc = GetUtcNow().AddSeconds(currentTime);
                SaveProvider.SetString(key + "_rt", endUtc.ToString("o"));
            }
        }

        private bool LoadState()
        {
            string key = GetSaveKey();
            if (saveMode == TimerSaveMode.RealTime &&
                !SaveProvider.GetBool(key + "_a", true) && SaveProvider.HasKey(key + "_t"))
            {
                currentTime = SaveProvider.GetFloat(key + "_t", countUp ? 0f : duration);
                currentTime = infiniteDuration ? Mathf.Max(0f, currentTime)
                    : _dayLengthSeconds > 0f ? GetDayTimeSeconds()
                    : Mathf.Clamp(currentTime, 0f, duration);
                isActive = false;
                lastProgress = -1f;
                Time.Value = currentTime;
                return true;
            }

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

                currentTime = (float)(GetUtcNow() - startUtc).TotalSeconds;
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
                if (_dayLengthSeconds > 0f)
                {
                    currentTime = currentTime % _dayLengthSeconds;
                    if (currentTime < 0f)
                    {
                        currentTime += _dayLengthSeconds;
                    }
                }
                else if (!infiniteDuration)
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

            DateTime now = GetUtcNow();
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
            int generation = ++playbackGeneration;
            timeSinceLastUpdate = 0f;
            currentTime = infiniteDuration ? 0f : GetStartTimeForCurrentDuration();
            _timeCarry = 0d;
            Time.Value = currentTime;
            if (generation != playbackGeneration)
            {
                return;
            }

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

        /// <summary>
        ///     Unity-driven timer update. Derived timers inherit this implementation automatically;
        ///     overrides must call <c>base.Update()</c> unless they intentionally provide another clock.
        /// </summary>
        protected virtual void Update()
        {
            Tick(FrameClock.GetDeltaTime(useUnscaledTime));
        }

        /// <summary>
        ///     Advances the timer by a caller-supplied delta. This is useful for deterministic tests,
        ///     server clocks, replay systems, and projects that drive time from a custom update loop.
        ///     The configured <see cref="timeScale"/>, <see cref="updateInterval"/>, active state and
        ///     pause policy are applied exactly like the Unity-driven update.
        /// </summary>
        /// <param name="deltaTime">Non-negative elapsed seconds in the timer's selected clock domain.</param>
        public void Tick(float deltaTime)
        {
            if (!isActive || deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
            {
                return;
            }

            if (pauseOnTimeScaleZero && !useUnscaledTime && FrameClock.TimeScale == 0f)
            {
                return;
            }

            deltaTime *= Mathf.Max(0.001f, timeScale);
            timeSinceLastUpdate += deltaTime;

            if (timeSinceLastUpdate >= updateInterval)
            {
                float elapsed = timeSinceLastUpdate;
                timeSinceLastUpdate = 0f;
                UpdateTimer(elapsed);
            }
        }

        private void UpdateTimer(float deltaTime)
        {
            if (!isActive)
            {
                return;
            }

            int generation = playbackGeneration;
            if (infiniteDuration)
            {
                AdvanceCurrentTime(deltaTime);
                Time.Value = currentTime;
                if (generation != playbackGeneration)
                {
                    return;
                }

                UpdateUI(timeValue: currentTime);
                return;
            }

            AdvanceCurrentTime(countUp ? deltaTime : -deltaTime);

            if (_dayLengthSeconds > 0f && countUp && currentTime >= _dayLengthSeconds)
            {
                currentTime = currentTime % _dayLengthSeconds;
                OnNewDay?.Invoke();
                if (generation != playbackGeneration)
                {
                    return;
                }

                if (!looping)
                {
                    isActive = false;
                    InvokeEvents();
                    if (generation != playbackGeneration)
                    {
                        return;
                    }

                    OnTimerCompleted?.Invoke();
                    return;
                }
            }

            bool reachedEnd = countUp ? currentTime >= duration : currentTime <= 0f;
            if (reachedEnd)
            {
                double preciseTime = currentTime + _timeCarry;
                float overshoot = (float)(countUp ? preciseTime - duration : -preciseTime);
                currentTime = countUp ? duration : 0f;
                _timeCarry = 0d;

                if (!looping)
                {
                    // WHY: Deactivate BEFORE the completion event so a handler (e.g. CooldownReward auto-claim)
                    // can re-arm the timer with Play(); deactivating after would overwrite that restart.
                    isActive = false;
                }

                InvokeEvents();
                if (generation != playbackGeneration)
                {
                    return;
                }

                OnTimerCompleted?.Invoke();

                if (generation != playbackGeneration)
                {
                    return;
                }

                if (looping && isActive)
                {
                    ApplyRandomDurationIfNeeded();
                    currentTime = GetStartTimeForCurrentDuration();
                    if (_carryLoopOvershoot)
                    {
                        ApplyLoopOvershoot(overshoot);
                    }
                    isActive = true;
                    timeSinceLastUpdate = 0f;

                    if (milestoneReached != null)
                    {
                        for (int i = 0; i < milestoneReached.Length; i++)
                        {
                            milestoneReached[i] = false;
                        }
                    }

                    lastProgress = -1f;

                    OnTimerStarted?.Invoke();
                    if (generation != playbackGeneration)
                    {
                        return;
                    }

                    InvokeEvents();
                }
            }
            else
            {
                InvokeEvents();
            }
        }

        private void AdvanceCurrentTime(float signedDelta)
        {
            double precise = currentTime + _timeCarry + signedDelta;
            currentTime = (float)precise;
            _timeCarry = precise - currentTime;
        }

        // WHY: The tick that completes a looping cycle usually runs past the end. Carrying that remainder into the
        // next cycle keeps the long-run period exact. Whole skipped cycles are dropped (modulo), so a hitch keeps the
        // phase without a burst of completions and the new cycle never starts already complete.
        private void ApplyLoopOvershoot(float overshoot)
        {
            float start = currentTime;
            float cycleLength = countUp ? duration - start : start;
            if (!(overshoot > 0f) || !(cycleLength > 0f))
            {
                return;
            }

            float carried = overshoot % cycleLength;
            float next = countUp ? start + carried : start - carried;
            if (countUp ? next < duration : next > 0f)
            {
                currentTime = next;
            }
        }

        private void InvokeEvents()
        {
            int generation = playbackGeneration;
            if (infiniteDuration)
            {
                float timeValueInfinite = currentTime;
                Time.Value = timeValueInfinite;
                if (generation != playbackGeneration)
                {
                    return;
                }

                if (_dayLengthSeconds > 0f)
                {
                    InvokeDayTimeChanged();
                    if (generation != playbackGeneration)
                    {
                        return;
                    }
                }

                UpdateUI(timeValue: timeValueInfinite);
                return;
            }

            float timeValue = currentTime;
            float progress = duration > 0
                ? countUp ? currentTime / duration : 1f - currentTime / duration
                : 0f;

            progress = Mathf.Clamp01(progress);

            Time.Value = timeValue;
            if (generation != playbackGeneration)
            {
                return;
            }

            OnProgressChanged?.Invoke(progress);
            if (generation != playbackGeneration)
            {
                return;
            }

            OnProgressPercentChanged?.Invoke(Mathf.RoundToInt(progress * 100f));
            if (generation != playbackGeneration)
            {
                return;
            }

            lastProgress = progress;

            if (enableMilestones && milestonePercentages != null)
            {
                if (milestoneReached == null || milestoneReached.Length != milestonePercentages.Length)
                {
                    milestoneReached = new bool[milestonePercentages.Length];
                }

                float[] percentages = milestonePercentages;
                bool[] reached = milestoneReached;
                for (int i = 0; i < percentages.Length; i++)
                {
                    if (!reached[i] && progress >= percentages[i])
                    {
                        reached[i] = true;
                        OnMilestoneReached?.Invoke(percentages[i]);
                        if (generation != playbackGeneration)
                        {
                            return;
                        }
                    }
                }
            }

            if (_dayLengthSeconds > 0f)
            {
                InvokeDayTimeChanged();
                if (generation != playbackGeneration)
                {
                    return;
                }
            }

            UpdateUI(progress, timeValue);
        }

        private void InvokeDayTimeChanged()
        {
            float h = DayTimeHours;
            float m = DayTimeMinutes;
            float s = DayTimeSeconds;
            OnDayTimeChanged?.Invoke(h, m, s);
        }

        private void UpdateUI(float progress = -1f, float timeValue = -1f)
        {
            int generation = playbackGeneration;
            if (infiniteDuration)
            {
                if (timeText != null)
                {
                    float t = timeValue >= 0 ? timeValue : currentTime;
                    if (_dayLengthSeconds > 0f)
                    {
                        float secs = t % _dayLengthSeconds;
                        if (secs < 0f)
                        {
                            secs += _dayLengthSeconds;
                        }

                        WriteDayTimeText(secs);
                    }
                    else
                    {
                        WriteTimeText(t, 0f, 0f);
                    }
                }

                return;
            }

            if (progress < 0)
            {
                progress = GetProgress();
                timeValue = GetCurrentTime();
            }

            if (progressImage != null)
            {
                float fillAmount = fillImageNormal ? progress : 1f - progress;
                progressImage.fillAmount = fillAmount;
                if (generation != playbackGeneration)
                {
                    return;
                }
            }

            if (timeText != null)
            {
                if (_dayLengthSeconds > 0f)
                {
                    WriteDayTimeText(GetDayTimeSeconds());
                }
                else
                {
                    WriteTimeText(timeValue, progress, progress * 100f);
                }
            }
        }

        private void WriteDayTimeText(float secondsOfDay)
        {
            int h = (int)(secondsOfDay / 3600f) % 24;
            int m = (int)(secondsOfDay % 3600f / 60f);
            int sec = (int)(secondsOfDay % 60f);
            TimerTextFormatter formatter = PrepareTextFormatter();
            string format = _dayTimeFormat;
            if (format != null && format == _rejectedDayTimeFormat)
            {
                format = DefaultDayTimeFormat;
            }

            bool changed;
            try
            {
                changed = formatter.Format(format, h, m, sec);
            }
            catch (FormatException)
            {
                _rejectedDayTimeFormat = format;
                WarnInvalidFormat("Day Time Format", format, DefaultDayTimeFormat);
                changed = formatter.Format(DefaultDayTimeFormat, h, m, sec);
            }

            if (changed)
            {
                timeText.SetText(formatter.Buffer, 0, formatter.Length);
            }
        }

        private void WriteTimeText(float timeValue, float progress, float percent)
        {
            TimerTextFormatter formatter = PrepareTextFormatter();
            string format = timeFormat;
            if (format != null && format == _rejectedTimeFormat)
            {
                format = DefaultTimeFormat;
            }

            bool changed;
            try
            {
                changed = formatter.Format(format, timeValue, progress, percent);
            }
            catch (FormatException)
            {
                _rejectedTimeFormat = format;
                WarnInvalidFormat("Time Format", format, DefaultTimeFormat);
                changed = formatter.Format(DefaultTimeFormat, timeValue, progress, percent);
            }

            if (changed)
            {
                timeText.SetText(formatter.Buffer, 0, formatter.Length);
            }
        }

        private void WarnInvalidFormat(string fieldName, string format, string fallback)
        {
            if (!NeoDiagnostics.RuntimeWarningsEnabled)
            {
                return;
            }

            NeoDiagnostics.LogWarning(
                $"[TimerObject] {name}: {fieldName} \"{format}\" is not a valid composite format for this timer; " +
                $"showing \"{fallback}\" instead.", this);
        }

        // WHY: Formatting into a reused buffer and pushing only changed text avoids a boxed, freshly allocated
        // string (and a TMP mesh rebuild) on every timer update.
        private TimerTextFormatter PrepareTextFormatter()
        {
            _textFormatter ??= new TimerTextFormatter();
            if (!ReferenceEquals(_formattedTextTarget, timeText))
            {
                _formattedTextTarget = timeText;
                _textFormatter.Invalidate();
            }

            return _textFormatter;
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

            int resetGeneration = unchecked(playbackGeneration + 1);
            Reset();
            if (resetGeneration != playbackGeneration)
            {
                return;
            }

            Play();
        }

        /// <summary>
        ///     Starts or restarts the timer. Use <see cref="Resume" /> to continue from pause.
        /// </summary>
        [Button]
        public void Play()
        {
            int generation = ++playbackGeneration;
            ApplyRandomDurationIfNeeded();
            currentTime = infiniteDuration ? 0f : GetStartTimeForCurrentDuration();
            _timeCarry = 0d;

            if (milestoneReached != null)
            {
                Array.Clear(milestoneReached, 0, milestoneReached.Length);
            }

            lastProgress = -1f;
            isActive = true;
            timeSinceLastUpdate = 0f;

            OnTimerStarted?.Invoke();
            if (generation != playbackGeneration)
            {
                return;
            }

            if (enableStartAnimation && Application.isPlaying && isActiveAndEnabled)
            {
                PlayStartAnimation();
            }

            InvokeEvents();
        }

        // WHY: The pulse is a plain coroutine on the timer's own frame clock and time domain. The former DOTween branch
        // sat behind a define that no asmdef sets, so it never compiled; the coroutine is the only implementation.
        private void PlayStartAnimation()
        {
            StopStartAnimation();
            _startAnimation = StartCoroutine(StartAnimationCoroutine());
        }

        // WHY: A restart or disable in the middle of the pulse used to leave the scale wherever the stopped (or a
        // second, competing) animation had put it. Only the timer's own animation is stopped; others are untouched.
        private void StopStartAnimation()
        {
            if (_startAnimation == null)
            {
                return;
            }

            StopCoroutine(_startAnimation);
            _startAnimation = null;
            transform.localScale = originalScale;
        }

        private IEnumerator StartAnimationCoroutine()
        {
            float elapsed = 0f;
            float halfDuration = startAnimationDuration * 0.5f;

            while (elapsed < halfDuration)
            {
                elapsed += FrameClock.GetDeltaTime(useUnscaledTime);
                float t = elapsed / halfDuration;
                float scale = Mathf.Lerp(1f, startAnimationScale, t);
                transform.localScale = originalScale * scale;
                yield return null;
            }

            elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed += FrameClock.GetDeltaTime(useUnscaledTime);
                float t = elapsed / halfDuration;
                float scale = Mathf.Lerp(startAnimationScale, 1f, t);
                transform.localScale = originalScale * scale;
                yield return null;
            }

            transform.localScale = originalScale;
            _startAnimation = null;
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
            Pause(isActive);
        }

        /// <summary>
        ///     Stops and resets the timer
        /// </summary>
        [Button]
        public void Stop()
        {
            if (!isActive && currentTime == (infiniteDuration ? 0f : GetStartTimeForCurrentDuration()) &&
                timeSinceLastUpdate == 0f)
            {
                return;
            }

            int generation = ++playbackGeneration;
            isActive = false;
            OnTimerStopped?.Invoke();
            if (generation != playbackGeneration)
            {
                return;
            }

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
            playbackGeneration++;
            _timeCarry = 0d;
            if (infiniteDuration)
            {
                currentTime = Mathf.Max(0f, time);
            }
            else if (_dayLengthSeconds > 0f)
            {
                currentTime = time % _dayLengthSeconds;
                if (currentTime < 0f)
                {
                    currentTime += _dayLengthSeconds;
                }
            }
            else
            {
                currentTime = Mathf.Clamp(time, 0f, duration);
            }

            lastProgress = -1f; // WHY: Force UI refresh
            InvokeEvents();
        }

        /// <summary>
        ///     Sets the current progress (0–1). Does not change <see cref="duration" />; use <see cref="SetDuration" /> or
        ///     <see cref="StartTimer" /> to change total length.
        /// </summary>
        /// <param name="progress">Progress value from 0 to 1</param>
        public void SetProgress(float progress)
        {
            playbackGeneration++;
            _timeCarry = 0d;
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
            SetTime(currentTime + seconds);
        }

        /// <summary>
        ///     Sets the timer duration and preserves the current progress ratio (same as
        ///     <see cref="SetDuration(float, bool)" /> with <c>keepProgress == true</c>).
        /// </summary>
        /// <param name="newDuration">New duration in seconds.</param>
        public void SetDuration(float newDuration)
        {
            SetDuration(newDuration, true);
        }

        /// <summary>
        ///     Sets the timer duration. Optionally preserves progress ratio.
        /// </summary>
        /// <param name="newDuration">New duration in seconds.</param>
        /// <param name="keepProgress">If true, scales current time to preserve progress; otherwise clamps current time.</param>
        public void SetDuration(float newDuration, bool keepProgress)
        {
            playbackGeneration++;
            _timeCarry = 0d;
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
                if (!infiniteDuration)
                {
                    currentTime = Mathf.Clamp(currentTime, 0f, duration);
                }
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
