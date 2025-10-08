//v.1.0.4

using System;
using System.Threading;
using System.Threading.Tasks;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

using UnityEngine;
using UnityEngine.Events;

namespace Neo
{
    /// <summary>
    ///     Interface for objects that need to subscribe to timer events
    /// </summary>
    public interface ITimerSubscriber
    {
        void TimerStart();
        void TimerEnd();
        void TimerUpdate(float remainingTime, float progress);
    }

    /// <summary>
    ///     Async timer implementation with events and looping support
    /// </summary>
    /// <example>
    ///     <code>
    /// // Create a timer that runs for 5 seconds, updates every 0.1 seconds
    /// Timer timer = new Timer(5f, 0.1f);
    /// 
    /// // Subscribe to events
    /// timer.OnTimerStart.AddListener(() => Debug.Log("Timer started"));
    /// timer.OnTimerEnd.AddListener(() => Debug.Log("Timer ended"));
    /// timer.OnTimerUpdate.AddListener((time, progress) => {
    ///     Debug.Log($"Time left: {time:F1}s, Progress: {progress:P0}");
    /// });
    /// 
    /// // Start the timer
    /// await timer.Start();
    /// 
    /// // Pause and resume
    /// timer.Pause();
    /// timer.Resume();
    /// 
    /// // Add or subtract time
    /// timer.AddTime(2f); // Add 2 seconds
    /// timer.AddTime(-1f); // Subtract 1 second
    /// 
    /// // Stop the timer
    /// timer.Stop();
    /// </code>
    /// </example>
    [Serializable]
    public class Timer
    {
        /// <summary>
        ///     Event triggered when the timer starts
        /// </summary>
        public UnityEvent OnTimerStart = new();

        /// <summary>
        ///     Event triggered when the timer ends
        /// </summary>
        public UnityEvent OnTimerEnd = new();

        /// <summary>
        ///     Event triggered on each update with remaining time and progress
        /// </summary>
        public UnityEvent<float, float> OnTimerUpdate = new();

        /// <summary>
        ///     Event triggered when the timer is paused
        /// </summary>
        public UnityEvent OnTimerPause = new();

        /// <summary>
        ///     Event triggered when the timer is resumed
        /// </summary>
        public UnityEvent OnTimerResume = new();

        [SerializeField] private float duration;

        [SerializeField] private float updateInterval;

        [SerializeField] private bool isRunning;

        [SerializeField] private bool isLooping;

        [SerializeField] private bool useUnscaledTime;

        [SerializeField] private bool isPaused;
        private CancellationTokenSource cancellationTokenSource;
        private float lastUpdateTime;

        /// <summary>
        ///     Creates a new timer instance
        /// </summary>
        /// <param name="duration">Duration in seconds</param>
        /// <param name="updateInterval">Update frequency in seconds</param>
        /// <param name="looping">Whether timer should automatically restart</param>
        /// <param name="useUnscaledTime">Whether to ignore timeScale</param>
        public Timer(float duration, float updateInterval = 0.05f, bool looping = false, bool useUnscaledTime = false)
        {
            Duration = duration;
            UpdateInterval = updateInterval;
            IsLooping = looping;
            UseUnscaledTime = useUnscaledTime;
            lastUpdateTime = 0f;
            RemainingTime = Duration;
        }

        /// <summary>
        ///     Gets the current running state of the timer
        /// </summary>
        public bool IsRunning => isRunning;

        /// <summary>
        ///     Gets or sets whether the timer is in looping mode
        /// </summary>
        public bool IsLooping
        {
            get => isLooping;
            set => isLooping = value;
        }

        /// <summary>
        ///     Gets or sets whether the timer uses unscaled time
        /// </summary>
        public bool UseUnscaledTime
        {
            get => useUnscaledTime;
            set => useUnscaledTime = value;
        }

        /// <summary>
        ///     Gets whether the timer is currently paused
        /// </summary>
        public bool IsPaused => isPaused;

        /// <summary>
        ///     Gets the current remaining time in seconds
        /// </summary>
        public float RemainingTime { get; private set; }

        /// <summary>
        ///     Gets the current progress of the timer (0 to 1)
        /// </summary>
        public float Progress => 1f - RemainingTime / duration;

        /// <summary>
        ///     Gets or sets the total duration of the timer in seconds
        /// </summary>
        public float Duration
        {
            get => duration;
            set
            {
                if (value < 0) value = 0;
                var difference = value - duration;
                duration = value;
                if (isRunning) RemainingTime += difference;
            }
        }

        /// <summary>
        ///     Gets or sets the update interval in seconds
        /// </summary>
        public float UpdateInterval
        {
            get => updateInterval;
            set
            {
                if (value < 0) value = 0;
                updateInterval = value;
            }
        }

        /// <summary>
        ///     Resets timer with new parameters
        /// </summary>
        public void Reset(float newDuration, float newUpdateInterval = 0.05f, bool? looping = null,
            bool? useUnscaledTime = null)
        {
            Stop();
            Duration = newDuration;
            UpdateInterval = newUpdateInterval;
            if (looping.HasValue) IsLooping = looping.Value;
            if (useUnscaledTime.HasValue) UseUnscaledTime = useUnscaledTime.Value;
            lastUpdateTime = 0f;
            RemainingTime = Duration;
        }

        /// <summary>
        ///     Starts or resumes the timer
        /// </summary>
        [Button]
        public async Task Start()
        {
            if (isRunning) return;

            isRunning = true;
            isPaused = false;
            OnTimerStart.Invoke();

            cancellationTokenSource = new CancellationTokenSource();
            try
            {
                do
                {
                    await RunTimerCycle(cancellationTokenSource.Token);
                    if (!IsLooping) OnTimerEnd.Invoke();
                } while (IsLooping && isRunning);
            }
            catch (OperationCanceledException)
            {
                // Timer was cancelled
            }
            finally
            {
                isRunning = false;
            }
        }

        /// <summary>
        ///     Stops the timer
        /// </summary>
        [Button]
        public void Stop()
        {
            if (!isRunning) return;

            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
            }

            isRunning = false;
            isPaused = false;
            lastUpdateTime = 0f;
            RemainingTime = Duration;
        }

        /// <summary>
        ///     Pauses the timer
        /// </summary>
        [Button]
        public void Pause()
        {
            if (!isRunning || isPaused) return;

            isPaused = true;
            OnTimerPause.Invoke();
        }

        /// <summary>
        ///     Resumes the timer from pause
        /// </summary>
        [Button]
        public void Resume()
        {
            if (!isRunning || !isPaused) return;

            isPaused = false;
            OnTimerResume.Invoke();
        }

        /// <summary>
        ///     Restarts the timer from the beginning
        /// </summary>
        public async Task Restart()
        {
            Stop();
            await Start();
        }

        /// <summary>
        ///     Adds time to the current timer duration
        /// </summary>
        /// <param name="seconds">Seconds to add (can be negative)</param>
        public void AddTime(float seconds)
        {
            RemainingTime = Mathf.Max(0, RemainingTime + seconds);
            Duration = Mathf.Max(0, Duration + seconds);
        }

        private async Task RunTimerCycle(CancellationToken cancellationToken)
        {
            RemainingTime = Duration;
            lastUpdateTime = 0f;

            while (RemainingTime > 0 && !cancellationToken.IsCancellationRequested)
            {
                if (!isPaused)
                {
                    var deltaTime = UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                    lastUpdateTime += deltaTime;

                    if (lastUpdateTime >= UpdateInterval)
                    {
                        RemainingTime -= lastUpdateTime;
                        var progress = 1f - RemainingTime / Duration;
                        OnTimerUpdate.Invoke(RemainingTime, progress);
                        lastUpdateTime = 0f;
                    }
                }

                await Task.Yield();
            }
        }
    }
}