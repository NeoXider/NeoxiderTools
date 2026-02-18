using System;
using Neo.Extensions;
using Neo.Save;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Bonus
{
    /// <summary>
    /// Time-based reward with persistent cooldown, built on TimerObject (RealTime countdown).
    /// Use this instead of the deprecated TimeReward for new code.
    /// </summary>
    [NeoDoc("Bonus/TimeReward/CooldownReward.md")]
    [AddComponentMenu("Neo/Bonus/" + nameof(CooldownReward))]
    public class CooldownReward : TimerObject
    {
        private const string LastRewardTimeKeyPrefix = "LastRewardTime";

        [Header("Reward Settings")]
        [SerializeField] [Min(0)] private float _cooldownSeconds = 60f * 60f;
        [SerializeField] [Min(0.015f)] private float _updateInterval = 0.2f;
        [SerializeField] private bool _rewardAvailableOnStart;
        [SerializeField] [Tooltip("-1 = take all accumulated; 1 = one per take; N = cap at N per take")]
        private int _maxRewardsPerTake = 1;
        [SerializeField] private string _addKey = "Bonus1";
        [SerializeField] private bool _startTakeReward;
        [SerializeField] private bool _startTimerOnStart = true;
        [SerializeField] private bool _saveTimeOnTakeReward = true;
        [SerializeField] private bool _saveTimeOnStartWhenSaveOnTakeDisabled = true;
        [SerializeField] private TimeFormat _displayTimeFormat = TimeFormat.HoursMinutesSeconds;
        [SerializeField] private string _displaySeparator = ":";

        [Header("Reward Events")]
        [SerializeField] private UnityEvent<float> _onTimeUpdated = new();
        [SerializeField] private UnityEvent _onRewardClaimed = new();
        [SerializeField] private UnityEvent<int> _onRewardsClaimed = new();
        [SerializeField] private UnityEvent _onRewardAvailable = new();

        private bool _waitingForManualStart;
        private bool _canTakeReward;

        /// <summary>Invoked on each timer update with remaining seconds.</summary>
        public UnityEvent<float> OnTimeUpdated => _onTimeUpdated;

        /// <summary>Invoked when reward claim succeeds (once per claim when multiple are given).</summary>
        public UnityEvent OnRewardClaimed => _onRewardClaimed;

        /// <summary>Invoked once per take with the number of claims given.</summary>
        public UnityEvent<int> OnRewardsClaimed => _onRewardsClaimed;

        /// <summary>Invoked once when reward becomes available.</summary>
        public UnityEvent OnRewardAvailable => _onRewardAvailable;

        /// <summary>Whether reward can be claimed right now.</summary>
        public bool IsRewardAvailable => _canTakeReward && !_waitingForManualStart;

        /// <summary>Full save key used for reward timestamp.</summary>
        public string RewardTimeKey => GetSaveKey();

        /// <summary>Whether successful claim immediately persists cooldown.</summary>
        public bool SaveTimeOnTakeReward { get => _saveTimeOnTakeReward; set => _saveTimeOnTakeReward = value; }

        protected override string GetSaveKey() => LastRewardTimeKeyPrefix + _addKey;

        protected override void Init()
        {
            base.Init();
            SyncTimerConfig();
            if (!SaveProvider.HasKey(GetSaveKey() + "_rt") && !_rewardAvailableOnStart)
                SetTime(duration);
        }

        private void Start()
        {
            if (_startTakeReward)
            {
                TakeReward();
            }

            if (_startTimerOnStart)
            {
                StartTime();
            }
            else
            {
                RefreshTimeState();
            }

            OnTimerCompleted.AddListener(OnBaseTimerCompleted);
            OnTimeChanged.AddListener(OnBaseTimeChanged);
        }

        private void OnDestroy()
        {
            OnTimerCompleted.RemoveListener(OnBaseTimerCompleted);
            OnTimeChanged.RemoveListener(OnBaseTimeChanged);
        }

        private void SyncTimerConfig()
        {
            duration = _cooldownSeconds;
            updateInterval = _updateInterval;
            countUp = false;
            saveProgress = true;
            saveMode = TimerSaveMode.RealTime;
            useUnscaledTime = true;
            pauseOnTimeScaleZero = false;
            looping = false;
            initialProgress = _rewardAvailableOnStart ? 1f : 0f;
            autoStart = _startTimerOnStart;
        }

        private void OnValidate()
        {
            _cooldownSeconds = Mathf.Max(0f, _cooldownSeconds);
            _updateInterval = Mathf.Max(0.015f, _updateInterval);
            _addKey = string.IsNullOrWhiteSpace(_addKey) ? "Bonus1" : _addKey.Trim();
            _displaySeparator = string.IsNullOrEmpty(_displaySeparator) ? ":" : _displaySeparator;
            SyncTimerConfig();
        }

        private void OnBaseTimerCompleted()
        {
            _canTakeReward = true;
            OnRewardAvailable?.Invoke();
        }

        private void OnBaseTimeChanged(float _)
        {
            _onTimeUpdated?.Invoke(GetRemainingTime());
        }

        /// <summary>Remaining seconds until the next reward is available.</summary>
        public float GetSecondsUntilReward() => GetRemainingTime();

        /// <summary>Formatted remaining time using component format settings.</summary>
        public string GetFormattedTimeLeft(bool trimLeadingZeros = false)
        {
            return GetRemainingTime().FormatTime(_displayTimeFormat, _displaySeparator, trimLeadingZeros);
        }

        /// <summary>Number of rewards that can be claimed now (capped by max per take).</summary>
        public int GetClaimableCount()
        {
            if (duration <= 0f) return 0;
            if (!TryGetEndUtcFromSave(out DateTime endUtc))
                return _rewardAvailableOnStart ? 1 : 0;
            DateTime lastRewardUtc = endUtc.AddSeconds(-duration);
            int accumulated = lastRewardUtc.GetAccumulatedClaimCount(duration, DateTime.UtcNow);
            return CooldownRewardExtensions.CapToMaxPerTake(accumulated, _maxRewardsPerTake);
        }

        /// <summary>Attempts to claim reward(s). Returns true when at least one claim succeeds.</summary>
        [Button]
        public bool TakeReward()
        {
            int count = GetClaimableCount();
            if (count < 1) return false;

            for (int i = 0; i < count; i++)
                OnRewardClaimed?.Invoke();
            OnRewardsClaimed?.Invoke(count);

            if (_saveTimeOnTakeReward)
            {
                SetTime(duration);
                SaveState();
                _waitingForManualStart = false;
                return true;
            }

            _waitingForManualStart = true;
            _canTakeReward = false;
            _onTimeUpdated?.Invoke(0f);
            return true;
        }

        /// <summary>Shortcut for UnityEvent bindings.</summary>
        public void Take() => TakeReward();

        /// <summary>Whether at least one reward can be claimed now.</summary>
        public bool CanTakeReward() => !_waitingForManualStart && GetClaimableCount() >= 1;

        /// <summary>Starts timer. If save-on-claim is disabled, optionally starts cooldown from now.</summary>
        [Button]
        public void StartTime()
        {
            if (!_saveTimeOnTakeReward && _saveTimeOnStartWhenSaveOnTakeDisabled)
            {
                SetTime(duration);
                SaveState();
            }
            _waitingForManualStart = false;
            Play();
        }

        /// <summary>Stops timer updates.</summary>
        [Button]
        public void StopTime() => Stop();

        /// <summary>Pauses timer.</summary>
        public void PauseTime() => Pause(true);

        /// <summary>Resumes timer after pause.</summary>
        public void ResumeTime() => Resume();

        /// <summary>Restarts cooldown from current time.</summary>
        public void RestartTime()
        {
            SetTime(duration);
            SaveState();
            _waitingForManualStart = false;
            if (!IsRunning) Play();
            else RefreshTimeState();
        }

        /// <summary>Clears saved cooldown and makes reward available immediately.</summary>
        [Button]
        public void SetRewardAvailableNow()
        {
            SaveProvider.DeleteKey(GetSaveKey());
            SaveProvider.DeleteKey(GetSaveKey() + "_rt");
            SaveProvider.DeleteKey(GetSaveKey() + "_a");
            SaveProvider.Save();
            _waitingForManualStart = false;
            SetTime(0f);
            _canTakeReward = true;
            RefreshTimeState();
        }

        /// <summary>Forces immediate refresh and event dispatch.</summary>
        [Button]
        public void RefreshTimeState()
        {
            if (_waitingForManualStart)
            {
                _onTimeUpdated?.Invoke(0f);
                return;
            }
            _onTimeUpdated?.Invoke(GetRemainingTime());
            bool available = GetClaimableCount() >= 1;
            if (available && !_canTakeReward)
            {
                _canTakeReward = true;
                OnRewardAvailable?.Invoke();
            }
            else if (!available)
                _canTakeReward = false;
        }

        /// <summary>Changes additional save key suffix.</summary>
        public void SetAdditionalKey(string addKey, bool refreshAfterChange = true)
        {
            _addKey = string.IsNullOrWhiteSpace(addKey) ? "Bonus1" : addKey.Trim();
            if (refreshAfterChange)
            {
                _waitingForManualStart = false;
                RefreshTimeState();
            }
        }

        /// <summary>Tries to read last reward UTC from saved end time (endUtc - duration).</summary>
        public bool TryGetLastRewardTimeUtc(out DateTime lastRewardUtc)
        {
            if (!TryGetEndUtcFromSave(out DateTime endUtc))
            {
                lastRewardUtc = default;
                return false;
            }
            lastRewardUtc = endUtc.AddSeconds(-duration);
            return true;
        }

        /// <summary>Elapsed seconds since last reward claim.</summary>
        public float GetElapsedSinceLastReward()
        {
            return TryGetLastRewardTimeUtc(out DateTime lastUtc)
                ? Mathf.Max(0f, lastUtc.GetSecondsSinceUtc(DateTime.UtcNow))
                : 0f;
        }

        private bool TryGetEndUtcFromSave(out DateTime endUtc)
        {
            endUtc = default;
            string key = GetSaveKey() + "_rt";
            if (!SaveProvider.HasKey(key))
                return false;
            string raw = SaveProvider.GetString(key, null);
            return !string.IsNullOrEmpty(raw) && raw.TryParseUtcRoundTrip(out endUtc);
        }
    }
}
