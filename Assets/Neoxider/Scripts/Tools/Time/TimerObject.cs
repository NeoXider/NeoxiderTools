using UnityEngine;
using UnityEngine.Events;

namespace Neo
{
    /// <summary>
    /// MonoBehaviour-based timer with Unity events support
    /// </summary>
    [AddComponentMenu("Neoxider/Tools/" + nameof(TimerObject))]
    public class TimerObject : MonoBehaviour
    {
        [Header("Timer Settings")]
        [Tooltip("Duration in seconds")]
        public float duration = 1f;                         // Total duration of the timer

        [Tooltip("How often the timer updates")]
        [Min(0.015f)]
        public float updateInterval = 0.015f;               // Update frequency in seconds

        [Tooltip("If true, time counts up; if false, counts down")]
        public bool countUp = true;                         // Direction of counting

        [Tooltip("Use unscaled time (ignores time scale)")]
        public bool useUnscaledTime = false;               // Whether to ignore time scale

        [Tooltip("Automatically restart when complete")]
        public bool looping = false;                       // Whether to loop the timer

        [Header("Initial State")]
        [Tooltip("Start timer automatically")]
        public bool autoStart = true;                      // Whether to start on Enable

        [Tooltip("Current active state")]
        public bool isActive = false;                      // Whether timer is running

        [Tooltip("Current time value")]
        [SerializeField] 
        private float currentTime = 0f;                    // Current timer value

        [Header("Events")]
        public UnityEvent OnTimerStarted;                 // Called when timer starts
        public UnityEvent OnTimerPaused;                  // Called when timer is paused
        public UnityEvent<float> OnTimeChanged;           // Called with current time value
        public UnityEvent<float> OnProgressChanged;       // Called with progress (0-1)
        public UnityEvent OnTimerCompleted;               // Called when timer completes

        private float timeSinceLastUpdate;                // Time accumulator for updates

        private void Start()
        {
            if (autoStart) Play();
        }

        private void Update()
        {
            if (!isActive) return;

            // Use appropriate time delta based on settings
            float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            timeSinceLastUpdate += deltaTime;

            // Check if it's time to update
            if (timeSinceLastUpdate >= updateInterval)
            {
                UpdateTimer(timeSinceLastUpdate);
                timeSinceLastUpdate = 0f;
            }
        }

        private void UpdateTimer(float deltaTime)
        {
            if (!isActive) return;

            // Update current time
            currentTime += deltaTime;

            // Check for completion
            if (currentTime >= duration)
            {
                currentTime = duration;
                InvokeEvents();
                OnTimerCompleted?.Invoke();

                if (looping)
                {
                    Play(); // Restart if looping
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
            if (countUp)
            {
                OnTimeChanged?.Invoke(currentTime);
                OnProgressChanged?.Invoke(currentTime / duration);
            }
            else
            {
                float remainingTime = duration - currentTime;
                OnTimeChanged?.Invoke(remainingTime);
                OnProgressChanged?.Invoke(1f - (currentTime / duration));
            }
        }

        /// <summary>
        /// Starts or restarts timer with optional new parameters
        /// </summary>
        public void StartTimer(float newDuration = -1f, float newUpdateInterval = -1f)
        {
            // Update parameters if provided
            if (newDuration >= 0) duration = newDuration;
            if (newUpdateInterval >= 0) updateInterval = newUpdateInterval;

            Reset();
            Play();
        }

        /// <summary>
        /// Starts or resumes the timer
        /// </summary>
        public void Play()
        {
            currentTime = 0f;
            isActive = true;
            timeSinceLastUpdate = 0f;
            
            OnTimerStarted?.Invoke();
            InvokeEvents(); // Immediate update for UI responsiveness
        }

        /// <summary>
        /// Pauses or resumes the timer
        /// </summary>
        public void Pause(bool paused = true)
        {
            if (isActive == !paused) return; // No state change
            
            isActive = !paused;
            if (paused)
            {
                OnTimerPaused?.Invoke();
            }
            else
            {
                OnTimerStarted?.Invoke();
            }
        }

        /// <summary>
        /// Toggles between paused and running states
        /// </summary>
        public void TogglePause() => Pause(!isActive);

        /// <summary>
        /// Stops and resets the timer
        /// </summary>
        public void Stop()
        {
            isActive = false;
            Reset();
        }

        /// <summary>
        /// Resets timer to initial state
        /// </summary>
        public void Reset()
        {
            currentTime = 0f;
            InvokeEvents();
        }

        /// <summary>
        /// Sets the current time value
        /// </summary>
        public void SetTime(float time)
        {
            currentTime = Mathf.Clamp(time, 0f, duration);
            InvokeEvents();
        }

        /// <summary>
        /// Gets current progress (0-1)
        /// </summary>
        public float GetProgress() => countUp ? currentTime / duration : 1f - (currentTime / duration);

        /// <summary>
        /// Gets current time value
        /// </summary>
        public float GetCurrentTime() => countUp ? currentTime : duration - currentTime;

        /// <summary>
        /// Gets whether timer is currently running
        /// </summary>
        public bool IsRunning => isActive;

        /// <summary>
        /// Gets whether timer has completed
        /// </summary>
        public bool IsCompleted => currentTime >= duration;
    }
}