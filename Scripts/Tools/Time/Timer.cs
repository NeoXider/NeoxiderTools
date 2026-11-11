using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
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
    ///     Async timer implementation with events and looping support using UniTask
    /// </summary>
    /// <example>
    ///     <code>
    /// // Create a timer that runs for 5 seconds, updates every 0.1 seconds
    /// Timer timer = new Timer(5f, 0.1f);
    /// 
    /// // Subscribe to events
    /// timer.OnTimerStart.AddListener(() => global::NeoDiagnostics.Log("Timer started"));
    /// timer.OnTimerEnd.AddListener(() => global::NeoDiagnostics.Log("Timer ended"));
    /// timer.OnTimerUpdate.AddListener((time, progress) => {
    ///     global::NeoDiagnostics.Log($"Time left: {time:F1}s, Progress: {progress:P0}");
    /// });
    /// 
    /// // Start the timer (UniTask)
    /// await timer.StartAsync();
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
    /// 
    /// // Dispose resources when done
    /// timer.Dispose();
    /// </code>
    /// </example>
    [Serializable]
    public class Timer : IDisposable
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

        [SerializeField] private bool carryLoopOvershoot;
        private CancellationTokenSource cancellationTokenSource;
        private float lastUpdateTime;

        // WHY: Time the last frame of a cycle ran past zero. With CarryLoopOvershoot the next looping cycle is that much
        // shorter, so the long-run period stays exact instead of gaining up to one frame per cycle.
        private float cycleOvershoot;

        [NonSerialized] private IFrameClock frameClock;

        // WHY: Identifies the current run; a cancelled cycle observes the cancellation one frame
        // late and must not clobber the state of a newer run started in the meantime.
        private int runGeneration;

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
        ///     Frame time source read once per frame by a running timer. Defaults to
        ///     <see cref="UnityFrameClock.Instance" />; assign a <see cref="ManualFrameClock" /> for deterministic
        ///     tests, replays or simulations. Assigning <c>null</c> restores the default.
        /// </summary>
        public IFrameClock FrameClock
        {
            get => frameClock ?? UnityFrameClock.Instance;
            set => frameClock = value;
        }

        /// <summary>
        ///     Looping only: when true, each new cycle starts with the time by which the previous cycle's last frame
        ///     overshot zero, keeping the long-run period exact. Whole cycles skipped by one long frame are dropped
        ///     (one completion, phase kept), so a new cycle never starts already complete. Default false keeps the
        ///     legacy restart at full duration.
        /// </summary>
        public bool CarryLoopOvershoot
        {
            get => carryLoopOvershoot;
            set => carryLoopOvershoot = value;
        }

        /// <summary>
        ///     Gets the current remaining time in seconds
        /// </summary>
        public float RemainingTime { get; private set; }

        /// <summary>
        ///     Gets the current progress of the timer (0 to 1)
        /// </summary>
        public float Progress => duration > 0 ? 1f - RemainingTime / duration : 0f;

        /// <summary>
        ///     Gets or sets the total duration of the timer in seconds
        /// </summary>
        public float Duration
        {
            get => duration;
            set
            {
                if (value < 0)
                {
                    value = 0;
                }

                float difference = value - duration;
                duration = value;
                if (isRunning)
                {
                    RemainingTime += difference;
                }

                RemainingTime = Mathf.Clamp(RemainingTime, 0f, duration);
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
                if (value < 0)
                {
                    value = 0;
                }

                updateInterval = value;
            }
        }

        /// <summary>
        ///     Disposes resources used by the timer
        /// </summary>
        public void Dispose()
        {
            Stop();
            cancellationTokenSource?.Dispose();
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
            if (looping.HasValue)
            {
                IsLooping = looping.Value;
            }

            if (useUnscaledTime.HasValue)
            {
                UseUnscaledTime = useUnscaledTime.Value;
            }

            lastUpdateTime = 0f;
            cycleOvershoot = 0f;
            RemainingTime = Duration;
        }

        /// <summary>
        ///     Starts the timer (synchronous version - fire and forget).
        ///     Can be called from non-async code. For async usage, use StartAsync().
        /// </summary>
        [Button]
        public void Start()
        {
            StartAsync().Forget();
        }

        /// <summary>
        ///     Alias for <see cref="Start" />. Starts the timer (fire and forget).
        /// </summary>
        public void Play()
        {
            Start();
        }

        /// <summary>
        ///     Sets the remaining time in seconds. Clamped to valid range.
        /// </summary>
        /// <param name="seconds">Remaining seconds.</param>
        public void SetRemainingTime(float seconds)
        {
            RemainingTime = Mathf.Clamp(seconds, 0f, duration);
        }

        /// <summary>
        ///     Sets the timer progress (0-1). 0 = start, 1 = end.
        /// </summary>
        /// <param name="progress">Progress value.</param>
        public void SetProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            RemainingTime = duration > 0f ? duration * (1f - progress) : 0f;
        }

        /// <summary>
        ///     Starts the timer (async version). Use <see cref="Resume" /> to resume a paused run.
        /// </summary>
        /// <param name="cancellationToken">Optional external cancellation token</param>
        public async UniTask StartAsync(CancellationToken cancellationToken = default)
        {
            if (isRunning || cancellationToken.IsCancellationRequested)
            {
                return;
            }

            CancellationTokenSource runCancellation = cancellationToken.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                : new CancellationTokenSource();
            CancellationToken runToken = runCancellation.Token;
            cancellationTokenSource = runCancellation;
            isRunning = true;
            isPaused = false;
            RemainingTime = Duration;
            lastUpdateTime = 0f;
            cycleOvershoot = 0f;
            int generation = ++runGeneration;

            try
            {
                OnTimerStart.Invoke();
                runToken.ThrowIfCancellationRequested();
                do
                {
                    bool cycleNeedsYield = !(RemainingTime > 0f);
                    await RunTimerCycle(runToken);
                    runToken.ThrowIfCancellationRequested();
                    if (generation != runGeneration || !isRunning)
                    {
                        break;
                    }

                    if (!IsLooping)
                    {
                        // Completion handlers may start a new run immediately.
                        isRunning = false;
                        isPaused = false;
                    }

                    OnTimerEnd.Invoke();
                    if (generation != runGeneration || !isRunning || !IsLooping)
                    {
                        break;
                    }

                    RemainingTime = carryLoopOvershoot ? GetNextCycleRemaining() : Duration;
                    cycleOvershoot = 0f;
                    lastUpdateTime = 0f;
                    // Non-empty cycles already yield on their final update; empty cycles must also yield.
                    if (cycleNeedsYield)
                    {
                        await UniTask.Yield(runToken);
                    }
                } while (generation == runGeneration && isRunning);
            }
            catch (OperationCanceledException)
            {
                // WHY: Timer was cancelled (expected, not an error).
            }
            finally
            {
                // WHY: Only the current run may reset the flag; after Stop()+Start() (or Restart())
                // in one frame the stale task's finally would otherwise mark the new run as stopped,
                // making it unstoppable and allowing concurrent cycles.
                if (generation == runGeneration)
                {
                    isRunning = false;
                    isPaused = false;
                    cancellationTokenSource = null;
                }

                runCancellation.Dispose();
            }
        }

        /// <summary>
        ///     Stops the timer
        /// </summary>
        [Button]
        public void Stop()
        {
            if (!isRunning)
            {
                return;
            }

            CancellationTokenSource runCancellation = cancellationTokenSource;
            cancellationTokenSource = null;
            isRunning = false;
            isPaused = false;
            lastUpdateTime = 0f;
            cycleOvershoot = 0f;
            RemainingTime = Duration;
            // The owning StartAsync finally disposes the source after its pending await observes cancellation.
            runCancellation?.Cancel();
        }

        /// <summary>
        ///     Pauses the timer
        /// </summary>
        [Button]
        public void Pause()
        {
            if (!isRunning || isPaused)
            {
                return;
            }

            isPaused = true;
            OnTimerPause.Invoke();
        }

        /// <summary>
        ///     Resumes the timer from pause
        /// </summary>
        [Button]
        public void Resume()
        {
            if (!isRunning || !isPaused)
            {
                return;
            }

            isPaused = false;
            OnTimerResume.Invoke();
        }

        /// <summary>
        ///     Restarts the timer from the beginning
        /// </summary>
        /// <param name="cancellationToken">Optional external cancellation token</param>
        public async UniTask Restart(CancellationToken cancellationToken = default)
        {
            Stop();
            await StartAsync(cancellationToken);
        }

        /// <summary>
        ///     Adds time to the current timer duration
        /// </summary>
        /// <param name="seconds">Seconds to add (can be negative)</param>
        public void AddTime(float seconds)
        {
            float remainingTime = RemainingTime + seconds;
            Duration = Mathf.Max(0f, Duration + seconds);
            RemainingTime = Mathf.Clamp(remainingTime, 0f, Duration);
        }

        // WHY: Only the part of the overshoot inside one cycle is carried. A remainder of a whole cycle or more would
        // start the next cycle at zero and fire a second OnTimerEnd in the same frame.
        private float GetNextCycleRemaining()
        {
            float cycleLength = Duration;
            if (!(cycleOvershoot > 0f) || !(cycleLength > 0f))
            {
                return cycleLength;
            }

            float remaining = cycleLength - cycleOvershoot % cycleLength;
            return remaining > 0f ? remaining : cycleLength;
        }

        private async UniTask RunTimerCycle(CancellationToken cancellationToken)
        {
            while (RemainingTime > 0 && !cancellationToken.IsCancellationRequested)
            {
                if (isPaused)
                {
                    await UniTask.WaitWhile(() => isPaused, cancellationToken: cancellationToken);
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                }

                float deltaTime = FrameClock.GetDeltaTime(UseUnscaledTime);
                if (deltaTime > 0f && !float.IsNaN(deltaTime) && !float.IsInfinity(deltaTime))
                {
                    lastUpdateTime += deltaTime;
                }

                if (lastUpdateTime >= UpdateInterval)
                {
                    float remainingBefore = RemainingTime;
                    RemainingTime -= lastUpdateTime;
                    cycleOvershoot = remainingBefore > 0f && RemainingTime < 0f ? -RemainingTime : 0f;
                    RemainingTime = Mathf.Max(0f, RemainingTime);

                    float progress = Duration > 0 ? 1f - RemainingTime / Duration : 1f;
                    lastUpdateTime = 0f;
                    OnTimerUpdate.Invoke(RemainingTime, progress);
                }

                // WHY: Use UniTask.Yield for better Unity integration
                await UniTask.Yield(cancellationToken);
            }
        }
    }
}
