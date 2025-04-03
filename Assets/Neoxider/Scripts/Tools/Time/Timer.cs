//v.1.0.1
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Neo
{
    /// <summary>
    /// Interface for objects that need to subscribe to timer events
    /// </summary>
    public interface ITimerSubscriber
    {
        void OnTimerStart();
        void OnTimerEnd();
        void OnTimerUpdate(float remainingTime, float progress);
    }

    /// <summary>
    /// Async timer implementation with events and looping support
    /// </summary>
    public class Timer
    {
        ///by   Neoxider
        /// <summary>
        /// </summary>
        ///<example>
        ///<code>
        /// <![CDATA[
        ///     NewTimer timer = new NewTimer(1, 0.05f);
        ///     timer.OnTimerStart  += ()            => Debug.Log("Timer started");
        ///     timer.OnTimerUpdate += remainingTime => Debug.Log("Remaining time: " + remainingTime);
        ///     timer.OnTimerEnd    += ()            => Debug.Log("Timer ended");
        /// ]]>
        ///</code>
        ///</example>
        public event Action OnTimerStart;                    // Called when timer starts
        public event Action OnTimerEnd;                      // Called when timer completes
        public event Action<float, float> OnTimerUpdate;     // Called on each update with (remainingTime, progress)

        /// <summary>
        /// Gets the current running state
        /// </summary>
        public bool IsRunning => isRunning;

        /// <summary>
        /// Gets whether the timer is in looping mode
        /// </summary>
        public bool IsLooping => isLooping;

        /// <summary>
        /// Gets whether the timer uses unscaled time
        /// </summary>
        public bool UseUnscaledTime => useUnscaledTime;

        private float duration;                              // Total duration in seconds
        private float updateInterval;                        // How often the timer updates in seconds
        private bool isRunning;                             // Current running state
        private bool isLooping;                             // Whether timer should loop after completion
        private bool useUnscaledTime;                       // Whether to use unscaled time
        private float lastUpdateTime;                       // Time since last update
        private CancellationTokenSource cancellationTokenSource;

        /// <summary>
        /// Creates a new timer instance
        /// </summary>
        /// <param name="duration">Duration in seconds</param>
        /// <param name="updateInterval">Update frequency in seconds</param>
        /// <param name="looping">Whether timer should automatically restart</param>
        /// <param name="useUnscaledTime">Whether to ignore timeScale</param>
        public Timer(float duration, float updateInterval = 0.05f, bool looping = false, bool useUnscaledTime = false)
        {
            this.duration = duration;
            this.updateInterval = updateInterval;
            this.isLooping = looping;
            this.useUnscaledTime = useUnscaledTime;
            this.lastUpdateTime = 0f;
        }

        /// <summary>
        /// Resets timer with new parameters
        /// </summary>
        public void Reset(float newDuration, float newUpdateInterval = 0.05f, bool? looping = null, bool? useUnscaledTime = null)
        {
            Stop();
            duration = newDuration;
            updateInterval = newUpdateInterval;
            if (looping.HasValue) isLooping = looping.Value;
            if (useUnscaledTime.HasValue) this.useUnscaledTime = useUnscaledTime.Value;
            lastUpdateTime = 0f;
        }

        /// <summary>
        /// Starts or resumes the timer
        /// </summary>
        public async Task Start()
        {
            if (isRunning) return;

            isRunning = true;
            OnTimerStart?.Invoke();

            cancellationTokenSource = new CancellationTokenSource();
            try
            {
                do
                {
                    await RunTimerCycle(cancellationTokenSource.Token);
                    if (!isLooping) OnTimerEnd?.Invoke();
                } 
                while (isLooping && isRunning);
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
        /// Stops the timer
        /// </summary>
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
            lastUpdateTime = 0f;
        }

        /// <summary>
        /// Restarts the timer from the beginning
        /// </summary>
        public async Task Restart()
        {
            Stop();
            await Start();
        }

        /// <summary>
        /// Sets whether the timer should loop
        /// </summary>
        public void SetLooping(bool looping)
        {
            isLooping = looping;
        }

        /// <summary>
        /// Sets whether the timer should use unscaled time
        /// </summary>
        public void SetUnscaledTime(bool useUnscaledTime)
        {
            this.useUnscaledTime = useUnscaledTime;
        }

        private async Task RunTimerCycle(CancellationToken cancellationToken)
        {
            float remainingTime = duration;
            lastUpdateTime = 0f;

            while (remainingTime > 0 && !cancellationToken.IsCancellationRequested)
            {
                float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                lastUpdateTime += deltaTime;

                if (lastUpdateTime >= updateInterval)
                {
                    remainingTime -= lastUpdateTime;
                    float progress = 1f - (remainingTime / duration); // Progress from 0 to 1
                    OnTimerUpdate?.Invoke(remainingTime, progress);
                    lastUpdateTime = 0f;
                }

                await Task.Yield();
            }
        }
    }
}
