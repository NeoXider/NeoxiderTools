using System;
using Neo.Reactive;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Neo.Tools
{
    /// <summary>
    ///     Generic dodge/dash mechanic with cooldown.
    ///     Controls action duration, cooldown, events, and reactive fields for UI.
    /// </summary>
    /// <remarks>
    ///     Suitable for dash, roll, block, and any ability with action time + cooldown.
    ///     Cooldown can start with the evade or after it ends.
    /// </remarks>
    [LegacyComponent("Neo.Rpg.RpgEvadeController")]
    [Obsolete("Use Neo.Rpg.RpgEvadeController for new RPG evade/invulnerability flow.")]
    [NeoDoc("Tools/Components/AttackSystem/Evade.md")]
    [CreateFromMenu("Neoxider/Tools/Components/Evade")]
    [AddComponentMenu("Neoxider/Tools/Components/Evade (Legacy)")]
    public class Evade : MonoBehaviour
    {
        #region Settings

        [Header("Evade")]
        [Tooltip("Evade action duration in seconds (invulnerability window, animation, etc.).")]
        [Min(0.01f)]
        public float evadeDuration = 1f;

        [Header("Cooldown")]
        [Tooltip("Ability cooldown duration in seconds.")]
        [Min(0.01f)]
        [FormerlySerializedAs("reloadTime")]
        public float cooldownDuration = 2f;

        [Tooltip("If true, cooldown starts when evade begins; otherwise after evade ends.")]
        [FormerlySerializedAs("reloadImmediately")]
        public bool cooldownStartsWithEvade = true;

        [Tooltip("Use unscaled time for cooldown (ignores Time.timeScale; useful when paused).")]
        public bool useUnscaledTimeForCooldown;

        [Tooltip(
            "Cooldown progress update interval in seconds (affects ReloadProgress/RemainingCooldownTime update rate).")]
        [Min(0.015f)]
        public float cooldownUpdateInterval = 0.05f;

        #endregion

        #region Events

        [Header("Evade Events")] [Tooltip("Invoked when evade starts.")]
        public UnityEvent OnEvadeStarted;

        [Tooltip("Invoked when evade action ends.")]
        public UnityEvent OnEvadeCompleted;

        [Header("Cooldown Events")]
        [Tooltip("Invoked when cooldown starts.")]
        [FormerlySerializedAs("OnReloadStarted")]
        public UnityEvent OnCooldownStarted;

        [Tooltip("Invoked when cooldown ends and the ability is ready again.")]
        [FormerlySerializedAs("OnReloadCompleted")]
        public UnityEvent OnCooldownCompleted;

        #endregion

        #region Reactive & Condition

        [Header("Reactive (UI / NeoCondition)")]
        [Tooltip("Cooldown progress 0–1; subscribe via ReloadProgress.OnChanged.")]
        public ReactivePropertyFloat ReloadProgress = new();

        [Tooltip("Remaining cooldown in seconds; subscribe via RemainingCooldownTime.OnChanged.")]
        public ReactivePropertyFloat RemainingCooldownTime = new();

        /// <summary>Cooldown progress 0–1 (for NeoCondition and reflection).</summary>
        public float ReloadProgressValue => ReloadProgress.CurrentValue;

        /// <summary>Remaining cooldown in seconds (for NeoCondition and reflection).</summary>
        public float RemainingCooldownTimeValue => RemainingCooldownTime.CurrentValue;

        #endregion

        #region State

        /// <summary>Whether an evade is currently active.</summary>
        public bool IsEvading { get; private set; }

        /// <summary>Whether cooldown is running (ability unavailable).</summary>
        public bool IsOnCooldown => _cooldownTimer != null && _cooldownTimer.IsRunning;

        /// <summary>Cooldown progress 0–1 (convenience getter).</summary>
        public float CooldownProgress => _cooldownTimer != null ? _cooldownTimer.Progress : 0f;

        /// <summary>Whether evade can be used now.</summary>
        public bool CanEvade => !IsOnCooldown && !IsEvading;

        #endregion

        #region Internal

        private Timer _cooldownTimer;
        private bool _cooldownCompletedInvoked;

        private void Awake()
        {
            _cooldownTimer = new Timer(cooldownDuration, cooldownUpdateInterval, false, useUnscaledTimeForCooldown);
            _cooldownTimer.OnTimerStart.AddListener(OnCooldownTimerStart);
            _cooldownTimer.OnTimerEnd.AddListener(OnCooldownTimerEnd);
            _cooldownTimer.OnTimerUpdate.AddListener(OnCooldownTimerUpdate);
            SyncReactiveFromTimer();
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(CompleteEvade));
        }

        private void OnDestroy()
        {
            if (_cooldownTimer != null)
            {
                _cooldownTimer.Stop();
                _cooldownTimer.Dispose();
                _cooldownTimer = null;
            }
        }

        private void OnCooldownTimerStart()
        {
            _cooldownCompletedInvoked = false;
            OnCooldownStarted?.Invoke();
        }

        private void OnCooldownTimerEnd()
        {
            if (!_cooldownCompletedInvoked)
            {
                _cooldownCompletedInvoked = true;
                RemainingCooldownTime.Value = 0f;
                ReloadProgress.Value = 1f;
                OnCooldownCompleted?.Invoke();
            }
        }

        private void OnCooldownTimerUpdate(float remaining, float progress)
        {
            RemainingCooldownTime.Value = remaining;
            ReloadProgress.Value = progress;
        }

        private void SyncReactiveFromTimer()
        {
            if (_cooldownTimer == null)
            {
                return;
            }

            if (_cooldownTimer.IsRunning)
            {
                RemainingCooldownTime.Value = _cooldownTimer.RemainingTime;
                ReloadProgress.Value = _cooldownTimer.Progress;
            }
            else
            {
                RemainingCooldownTime.Value = 0f;
                ReloadProgress.Value = 1f;
            }
        }

        private void CompleteEvade()
        {
            IsEvading = false;
            OnEvadeCompleted?.Invoke();

            if (!cooldownStartsWithEvade)
            {
                _cooldownTimer?.Start();
            }
        }

        #endregion

        #region Public API

        /// <summary>
        ///     Tries to start evade. Succeeds only if not on cooldown and not already evading.
        /// </summary>
        /// <returns>True if evade started; otherwise false.</returns>
        [Button]
        public bool TryStartEvade()
        {
            if (!CanEvade)
            {
                return false;
            }

            IsEvading = true;
            OnEvadeStarted?.Invoke();

            if (cooldownStartsWithEvade)
            {
                _cooldownTimer?.Start();
            }

            Invoke(nameof(CompleteEvade), evadeDuration);
            return true;
        }

        /// <summary>
        ///     Starts evade if possible. Convenience wrapper with no return value.
        /// </summary>
        public void StartEvade()
        {
            TryStartEvade();
        }

        /// <summary>
        ///     Clears cooldown so the ability is ready again. Does not cancel an active evade.
        /// </summary>
        [Button]
        public void ResetCooldown()
        {
            _cooldownTimer?.Stop();
            RemainingCooldownTime.Value = 0f;
            ReloadProgress.Value = 1f;
        }

        /// <summary>
        ///     Sets cooldown duration in seconds. Does not affect a cooldown already in progress until the next start.
        /// </summary>
        /// <param name="seconds">New duration in seconds (minimum 0.01).</param>
        public void SetCooldownDuration(float seconds)
        {
            cooldownDuration = Mathf.Max(0.01f, seconds);
            if (_cooldownTimer != null)
            {
                _cooldownTimer.Duration = cooldownDuration;
            }
        }

        /// <summary>
        ///     Sets evade action duration in seconds.
        /// </summary>
        /// <param name="seconds">New duration in seconds (minimum 0.01).</param>
        public void SetEvadeDuration(float seconds)
        {
            evadeDuration = Mathf.Max(0.01f, seconds);
        }

        /// <summary>
        ///     Returns remaining cooldown in seconds (0 if not cooling down).
        /// </summary>
        public float GetRemainingCooldown()
        {
            return _cooldownTimer != null && _cooldownTimer.IsRunning ? _cooldownTimer.RemainingTime : 0f;
        }

        #endregion
    }
}
