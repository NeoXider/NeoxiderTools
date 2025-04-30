using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    /// Component that handles evade mechanics with cooldown system
    /// </summary>
    [AddComponentMenu("Neoxider/Tools/EvadeManager")]
    public class Evade : MonoBehaviour
    {
        [Header("Evade Settings")]
        [Tooltip("Duration of the evade action in seconds")]
        [SerializeField] private float evadeDuration = 1f;

        [Tooltip("Cooldown time between evades in seconds")]
        [SerializeField] private float reloadTime = 2f;

        [Tooltip("If true, cooldown starts immediately with evade")]
        [SerializeField] private bool reloadImmediately = true;

        [Header("Events")]
        [Tooltip("Called when evade action starts")]
        public UnityEvent OnEvadeStarted;

        [Tooltip("Called with evade progress (0-1)")]
        public UnityEvent<float> OnEvadeProgress;

        [Tooltip("Called when evade action completes")]
        public UnityEvent OnEvadeCompleted;

        [Tooltip("Called when cooldown starts")]
        public UnityEvent OnReloadStarted;

        [Tooltip("Called when cooldown completes")]
        public UnityEvent OnReloadCompleted;

        private Timer reloadTimer;
        private bool isEvading;

        /// <summary>
        /// Gets whether evade action is currently active
        /// </summary>
        public bool IsEvading => isEvading;

        /// <summary>
        /// Gets whether evade is on cooldown
        /// </summary>
        public bool IsOnCooldown => reloadTimer.IsRunning;

        /// <summary>
        /// Gets current cooldown progress (0-1)
        /// </summary>
        public float CooldownProgress => reloadTimer.Progress;

        private void Awake()
        {
            InitializeTimer();
        }

        private void InitializeTimer()
        {
            reloadTimer = new Timer(reloadTime);
            reloadTimer.OnTimerStart.AddListener(OnReloadStarted.Invoke);
            reloadTimer.OnTimerEnd.AddListener(OnReloadCompleted.Invoke);
            reloadTimer.OnTimerUpdate.AddListener((_, progress) => OnEvadeProgress.Invoke(progress));
        }

        private void OnDestroy()
        {
            if (reloadTimer != null)
            {
                reloadTimer.Stop();
            }
        }

        /// <summary>
        /// Starts the evade action if not on cooldown
        /// </summary>
        [Button]
        public void StartEvade()
        {
            if (IsOnCooldown) return;

            isEvading = true;
            OnEvadeStarted.Invoke();

            if (reloadImmediately)
            {
                reloadTimer.Start();
            }

            Invoke(nameof(CompleteEvade), evadeDuration);
        }

        private void CompleteEvade()
        {
            isEvading = false;
            OnEvadeCompleted.Invoke();

            if (!reloadImmediately)
            {
                reloadTimer.Start();
            }
        }

        /// <summary>
        /// Gets whether evade can be performed
        /// </summary>
        public bool CanEvade() => !IsOnCooldown && !IsEvading;

        /// <summary>
        /// Gets remaining cooldown time in seconds
        /// </summary>
        public float GetRemainingCooldown() => reloadTimer.RemainingTime;
    }
}