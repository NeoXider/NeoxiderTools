using Neo.Reactive;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Neo.Tools
{
    /// <summary>
    ///     Универсальный компонент механики уклонения/рывка с перезарядкой.
    ///     Управляет длительностью действия, cooldown, событиями и реактивными полями для UI.
    /// </summary>
    /// <remarks>
    ///     Подходит для даша, ролла, блока и любых способностей с «временем действия + перезарядка».
    ///     Cooldown можно запускать сразу с началом уклонения или после его завершения.
    /// </remarks>
    [NeoDoc("Tools/Components/AttackSystem/Evade.md")]
    [CreateFromMenu("Neoxider/Tools/Components/Evade")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(Evade))]
    public class Evade : MonoBehaviour
    {
        #region Settings

        [Header("Evade")]
        [Tooltip("Длительность действия уклонения в секундах (неуязвимость, анимация и т.д.).")]
        [Min(0.01f)]
        public float evadeDuration = 1f;

        [Header("Cooldown")]
        [Tooltip("Время перезарядки способности в секундах.")]
        [Min(0.01f)]
        [FormerlySerializedAs("reloadTime")]
        public float cooldownDuration = 2f;

        [Tooltip("Если true, перезарядка стартует в момент начала уклонения; иначе — после его завершения.")]
        [FormerlySerializedAs("reloadImmediately")]
        public bool cooldownStartsWithEvade = true;

        [Tooltip("Использовать unscaled time для cooldown (не зависит от Time.timeScale, удобно при паузе).")]
        public bool useUnscaledTimeForCooldown;

        [Tooltip("Интервал обновления прогресса перезарядки в секундах (влияет на частоту ReloadProgress/RemainingCooldownTime).")]
        [Min(0.015f)]
        public float cooldownUpdateInterval = 0.05f;

        #endregion

        #region Events

        [Header("Evade Events")]
        [Tooltip("Вызывается в момент начала уклонения.")]
        public UnityEvent OnEvadeStarted;

        [Tooltip("Вызывается по окончании действия уклонения.")]
        public UnityEvent OnEvadeCompleted;

        [Header("Cooldown Events")]
        [Tooltip("Вызывается при старте перезарядки.")]
        [FormerlySerializedAs("OnReloadStarted")]
        public UnityEvent OnCooldownStarted;

        [Tooltip("Вызывается, когда перезарядка завершена и способность снова доступна.")]
        [FormerlySerializedAs("OnReloadCompleted")]
        public UnityEvent OnCooldownCompleted;

        #endregion

        #region Reactive & Condition

        [Header("Reactive (UI / NeoCondition)")]
        [Tooltip("Прогресс перезарядки 0–1; подписка через ReloadProgress.OnChanged.")]
        public ReactivePropertyFloat ReloadProgress = new();

        [Tooltip("Оставшееся время перезарядки в секундах; подписка через RemainingCooldownTime.OnChanged.")]
        public ReactivePropertyFloat RemainingCooldownTime = new();

        /// <summary>Прогресс перезарядки 0–1 (для NeoCondition и рефлексии).</summary>
        public float ReloadProgressValue => ReloadProgress.CurrentValue;

        /// <summary>Оставшееся время перезарядки в секундах (для NeoCondition и рефлексии).</summary>
        public float RemainingCooldownTimeValue => RemainingCooldownTime.CurrentValue;

        #endregion

        #region State

        /// <summary>Идёт ли в данный момент действие уклонения.</summary>
        public bool IsEvading { get; private set; }

        /// <summary>Идёт ли перезарядка (способность недоступна).</summary>
        public bool IsOnCooldown => _cooldownTimer != null && _cooldownTimer.IsRunning;

        /// <summary>Прогресс перезарядки 0–1 (удобный геттер).</summary>
        public float CooldownProgress => _cooldownTimer != null ? _cooldownTimer.Progress : 0f;

        /// <summary>Можно ли сейчас выполнить уклонение.</summary>
        public bool CanEvade => !IsOnCooldown && !IsEvading;

        #endregion

        #region Internal

        private Timer _cooldownTimer;
        private bool _cooldownCompletedInvoked;

        private void Awake()
        {
            _cooldownTimer = new Timer(cooldownDuration, cooldownUpdateInterval, looping: false, useUnscaledTimeForCooldown);
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
            if (_cooldownTimer == null) return;
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
        ///     Пытается начать уклонение. Выполняется только если способность не на перезарядке и не в процессе уклонения.
        /// </summary>
        /// <returns>true, если уклонение начато; иначе false.</returns>
        [Button]
        public bool TryStartEvade()
        {
            if (!CanEvade)
                return false;

            IsEvading = true;
            OnEvadeStarted?.Invoke();

            if (cooldownStartsWithEvade)
                _cooldownTimer?.Start();

            Invoke(nameof(CompleteEvade), evadeDuration);
            return true;
        }

        /// <summary>
        ///     Начинает уклонение, если возможно. Удобная обёртка без возврата значения.
        /// </summary>
        public void StartEvade()
        {
            TryStartEvade();
        }

        /// <summary>
        ///     Сбрасывает перезарядку — способность снова доступна. Не прерывает текущее уклонение.
        /// </summary>
        [Button]
        public void ResetCooldown()
        {
            _cooldownTimer?.Stop();
            RemainingCooldownTime.Value = 0f;
            ReloadProgress.Value = 1f;
        }

        /// <summary>
        ///     Устанавливает длительность перезарядки в секундах. Не влияет на уже идущий cooldown до следующего старта.
        /// </summary>
        /// <param name="seconds">Новая длительность в секундах (минимум 0.01).</param>
        public void SetCooldownDuration(float seconds)
        {
            cooldownDuration = Mathf.Max(0.01f, seconds);
            if (_cooldownTimer != null)
                _cooldownTimer.Duration = cooldownDuration;
        }

        /// <summary>
        ///     Устанавливает длительность действия уклонения в секундах.
        /// </summary>
        /// <param name="seconds">Новая длительность в секундах (минимум 0.01).</param>
        public void SetEvadeDuration(float seconds)
        {
            evadeDuration = Mathf.Max(0.01f, seconds);
        }

        /// <summary>
        ///     Возвращает оставшееся время перезарядки в секундах (0, если перезарядка не идёт).
        /// </summary>
        public float GetRemainingCooldown()
        {
            return _cooldownTimer != null && _cooldownTimer.IsRunning ? _cooldownTimer.RemainingTime : 0f;
        }

        #endregion
    }
}
