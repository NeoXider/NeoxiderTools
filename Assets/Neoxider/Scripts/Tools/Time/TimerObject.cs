
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

using UnityEngine;
using UnityEngine.Events;

namespace Neo
{
    /// <summary>
    ///     MonoBehaviour-based timer with Unity events support
    /// </summary>
    [AddComponentMenu("Neoxider/Tools/" + nameof(TimerObject))]
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

        [Tooltip("Automatically restart when complete")]
        public bool looping;

        [Header("Initial State")] [Tooltip("Start timer automatically on enable")]
        public bool autoStart = true;

        [Tooltip("Current active state of the timer")]
        public bool isActive;

        [Tooltip("Current time value of the timer")] [SerializeField]
        private float currentTime;

        [Header("Events")] [Tooltip("Called when timer starts")]
        public UnityEvent OnTimerStarted;

        [Tooltip("Called when timer is paused")]
        public UnityEvent OnTimerPaused;

        [Tooltip("Called with current time value on each update")]
        public UnityEvent<float> OnTimeChanged;

        [Tooltip("Called with progress (0-1) on each update")]
        public UnityEvent<float> OnProgressChanged;

        [Tooltip("Called when timer completes")]
        public UnityEvent OnTimerCompleted;

        private float timeSinceLastUpdate;

        /// <summary>
        ///     Gets whether timer is currently running
        /// </summary>
        public bool IsRunning => isActive;

        /// <summary>
        ///     Gets whether timer has completed
        /// </summary>
        public bool IsCompleted => currentTime >= duration;

        /// <summary>
        ///     Resets timer to initial state
        /// </summary>
        public void Reset()
        {
            currentTime = 0f;
            InvokeEvents();
        }

        private void Start()
        {
            if (autoStart) Play();
        }

        private void Update()
        {
            if (!isActive) return;

            var deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            timeSinceLastUpdate += deltaTime;

            if (timeSinceLastUpdate >= updateInterval)
            {
                UpdateTimer(timeSinceLastUpdate);
                timeSinceLastUpdate = 0f;
            }
        }

        private void UpdateTimer(float deltaTime)
        {
            if (!isActive) return;

            currentTime += deltaTime;

            if (currentTime >= duration)
            {
                currentTime = duration;
                InvokeEvents();
                OnTimerCompleted?.Invoke();

                if (looping)
                    Play();
                else
                    isActive = false;
            }
            else
            {
                InvokeEvents();
            }
        }

        private void InvokeEvents()
        {
            if (countUp)
            {
                OnTimeChanged?.Invoke(currentTime);
                OnProgressChanged?.Invoke(currentTime / duration);
            }
            else
            {
                var remainingTime = duration - currentTime;
                OnTimeChanged?.Invoke(remainingTime);
                OnProgressChanged?.Invoke(1f - currentTime / duration);
            }
        }

        /// <summary>
        ///     Starts or restarts timer with optional new parameters
        /// </summary>
        /// <param name="newDuration">New duration in seconds. If negative, keeps current duration</param>
        /// <param name="newUpdateInterval">New update interval in seconds. If negative, keeps current interval</param>
        public void StartTimer(float newDuration = -1f, float newUpdateInterval = -1f)
        {
            if (newDuration >= 0) duration = newDuration;
            if (newUpdateInterval >= 0) updateInterval = newUpdateInterval;

            Reset();
            Play();
        }

        /// <summary>
        ///     Starts or resumes the timer
        /// </summary>
        [Button]
        public void Play()
        {
            currentTime = 0f;
            isActive = true;
            timeSinceLastUpdate = 0f;

            OnTimerStarted?.Invoke();
            InvokeEvents();
        }

        /// <summary>
        ///     Pauses or resumes the timer
        /// </summary>
        /// <param name="paused">True to pause, false to resume</param>
        [Button]
        public void Pause(bool paused = true)
        {
            if (isActive == !paused) return;

            isActive = !paused;
            if (paused)
                OnTimerPaused?.Invoke();
            else
                OnTimerStarted?.Invoke();
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
            isActive = false;
            Reset();
        }

        /// <summary>
        ///     Sets the current time value
        /// </summary>
        /// <param name="time">New time value in seconds</param>
        public void SetTime(float time)
        {
            currentTime = Mathf.Clamp(time, 0f, duration);
            InvokeEvents();
        }

        /// <summary>
        ///     Gets current progress (0-1)
        /// </summary>
        /// <returns>Progress value from 0 to 1</returns>
        public float GetProgress()
        {
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
    }
}