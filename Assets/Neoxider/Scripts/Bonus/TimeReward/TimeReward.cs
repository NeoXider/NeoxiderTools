using System;
using System.Collections;
using Neo.Extensions;
using Neo.Save;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Neo
{
    namespace Bonus
    {
        /// <summary>
        /// Provides time-based reward logic with persistent cooldown and timer control API.
        /// </summary>
        /// <remarks>Consider using CooldownReward for new implementations (inherits TimerObject).</remarks>
        [Obsolete("Use CooldownReward (inherits TimerObject) for new code. This component remains functional but is deprecated.")]
        [NeoDoc("Bonus/TimeReward/TimeReward.md")]
        [AddComponentMenu("Neo/" + "Bonus/" + nameof(TimeReward))]
        public class TimeReward : MonoBehaviour
        {
            private const string LastRewardTimeKeyPrefix = "LastRewardTime";
            private const float MinUpdateInterval = 0.02f;

            [Header("Settings")]
            [FormerlySerializedAs("_secondsToWaitForReward")]
            [SerializeField]
            [Min(0)]
            /// <summary>
            /// Reward cooldown duration in seconds.
            /// </summary>
            public float secondsToWaitForReward = 60f * 60f;

            [FormerlySerializedAs("_updateTime")]
            [SerializeField]
            [Min(0)]
            /// <summary>
            /// Timer update interval in seconds.
            /// </summary>
            public float updateTime = 0.2f;

            [SerializeField] private bool _rewardAvailableOnStart = false;

            [SerializeField] [Tooltip("-1 = take all accumulated; 1 = one per take; N = cap at N per take")]
            private int _maxRewardsPerTake = 1;

            [SerializeField] private string _addKey = "Bonus1";

            [FormerlySerializedAs("_startTakeReward")]
            [SerializeField]
            /// <summary>
            /// If enabled, attempts to claim reward on start.
            /// </summary>
            public bool startTakeReward;

            [SerializeField] private bool _startTimerOnStart = true;
            [SerializeField] private bool _saveTimeOnTakeReward = true;
            [SerializeField] private bool _saveTimeOnStartWhenSaveOnTakeDisabled = true;
            [SerializeField] private TimeFormat _displayTimeFormat = TimeFormat.HoursMinutesSeconds;
            [SerializeField] private string _displaySeparator = ":";

            [Header("Debug")]
            [FormerlySerializedAs("_lastRewardTimeStr")]
            [SerializeField]
            /// <summary>
            /// Raw saved UTC time string.
            /// </summary>
            public string lastRewardTimeStr;

            /// <summary>
            /// Remaining time until the reward becomes available.
            /// </summary>
            public float timeLeft;

            /// <summary>
            /// Invoked on each timer update with remaining seconds.
            /// </summary>
            public UnityEvent<float> OnTimeUpdated = new();

            /// <summary>
            /// Invoked when reward claim succeeds (once per claim when multiple are given).
            /// </summary>
            public UnityEvent OnRewardClaimed = new();

            /// <summary>
            /// Invoked once per take with the number of claims given.
            /// </summary>
            public UnityEvent<int> OnRewardsClaimed = new();

            /// <summary>
            /// Invoked once when reward becomes available.
            /// </summary>
            public UnityEvent OnRewardAvailable = new();

            /// <summary>
            /// Invoked when timer starts.
            /// </summary>
            public UnityEvent OnTimerStarted = new();

            /// <summary>
            /// Invoked when timer stops.
            /// </summary>
            public UnityEvent OnTimerStopped = new();

            /// <summary>
            /// Invoked when timer is paused.
            /// </summary>
            public UnityEvent OnTimerPaused = new();

            /// <summary>
            /// Invoked when timer is resumed.
            /// </summary>
            public UnityEvent OnTimerResumed = new();

            private Coroutine _timerRoutine;
            private bool _isTimerRunning;
            private bool _isTimerPaused;
            private bool _waitingForManualStart;
            private bool canTakeReward;

            /// <summary>
            /// Gets a value indicating whether timer loop is active.
            /// </summary>
            public bool IsTimerRunning => _isTimerRunning;

            /// <summary>
            /// Gets a value indicating whether timer is paused.
            /// </summary>
            public bool IsTimerPaused => _isTimerPaused;

            /// <summary>
            /// Gets a value indicating whether reward can be claimed right now.
            /// </summary>
            public bool IsRewardAvailable => canTakeReward && !_waitingForManualStart;

            /// <summary>
            /// Gets the full save key used for reward timestamp.
            /// </summary>
            public string RewardTimeKey => BuildRewardTimeKey();

            /// <summary>
            /// Gets or sets whether successful claim immediately starts cooldown persistence.
            /// </summary>
            public bool SaveTimeOnTakeReward
            {
                get => _saveTimeOnTakeReward;
                set => _saveTimeOnTakeReward = value;
            }

            private void Start()
            {
                if (!_rewardAvailableOnStart && !TryGetLastRewardTimeUtc(out _))
                {
                    SaveCurrentTimeAsLastRewardTime();
                }

                if (startTakeReward)
                {
                    TakeReward();
                }

                if (_startTimerOnStart)
                {
                    StartTime();
                    return;
                }

                RefreshTimeState();
            }

            private void OnDisable()
            {
                StopTimeInternal(false);
            }

            private void OnValidate()
            {
                secondsToWaitForReward = Mathf.Max(0f, secondsToWaitForReward);
                updateTime = Mathf.Max(0f, updateTime);
                _addKey = string.IsNullOrWhiteSpace(_addKey) ? "Bonus1" : _addKey.Trim();
                _displaySeparator = string.IsNullOrEmpty(_displaySeparator) ? ":" : _displaySeparator;
            }

            /// <summary>
            /// Formats seconds to hh:mm:ss.
            /// </summary>
            /// <param name="seconds">Duration in seconds.</param>
            /// <returns>Formatted duration string.</returns>
            public static string FormatTime(int seconds)
            {
                return FormatTime((float)seconds, TimeFormat.HoursMinutesSeconds, ":", false);
            }

            /// <summary>
            /// Formats seconds to hh:mm:ss.
            /// </summary>
            /// <param name="seconds">Duration in seconds.</param>
            /// <returns>Formatted duration string.</returns>
            public static string FormatTime(float seconds)
            {
                return FormatTime(seconds, TimeFormat.HoursMinutesSeconds, ":", false);
            }

            /// <summary>
            /// Formats seconds using a configurable time format.
            /// </summary>
            /// <param name="seconds">Duration in seconds.</param>
            /// <param name="format">Output time format.</param>
            /// <param name="separator">Separator between time parts.</param>
            /// <param name="trimLeadingZeros">Whether to trim leading zeros in the first token.</param>
            /// <returns>Formatted duration string.</returns>
            public static string FormatTime(float seconds, TimeFormat format, string separator = ":", bool trimLeadingZeros = false)
            {
                return Mathf.Max(0f, seconds).FormatTime(format, separator, trimLeadingZeros);
            }

            /// <summary>
            /// Returns remaining seconds before the next reward becomes available (for the first claim in queue).
            /// </summary>
            /// <returns>Remaining cooldown in seconds.</returns>
            public float GetSecondsUntilReward()
            {
                if (secondsToWaitForReward <= 0f)
                {
                    return 0f;
                }

                lastRewardTimeStr = SaveProvider.GetString(BuildRewardTimeKey(), string.Empty);
                if (!lastRewardTimeStr.TryParseUtcRoundTrip(out DateTime lastRewardTimeUtc))
                {
                    if (_rewardAvailableOnStart)
                    {
                        return 0f;
                    }

                    SaveCurrentTimeAsLastRewardTime();
                    return secondsToWaitForReward;
                }

                float secondsPassed = lastRewardTimeUtc.GetSecondsSinceUtc(DateTime.UtcNow);
                float secondsUntilReward = secondsToWaitForReward - secondsPassed;
                return Mathf.Max(0f, secondsUntilReward);
            }

            /// <summary>
            /// Returns how many rewards can be claimed right now (capped by max rewards per take).
            /// </summary>
            public int GetClaimableCount()
            {
                if (secondsToWaitForReward <= 0f)
                {
                    return 0;
                }

                if (!TryGetLastRewardTimeUtc(out DateTime lastUtc))
                {
                    return _rewardAvailableOnStart ? 1 : 0;
                }

                DateTime now = DateTime.UtcNow;
                int accumulated = GetAccumulatedClaimCount(lastUtc, secondsToWaitForReward, now);
                return CapToMaxPerTake(accumulated, _maxRewardsPerTake);
            }

            private static int GetAccumulatedClaimCount(DateTime lastClaimUtc, float cooldownSeconds, DateTime nowUtc)
            {
                if (cooldownSeconds <= 0f) return 0;
                double elapsed = (nowUtc - lastClaimUtc).TotalSeconds;
                return elapsed < 0 ? 0 : (int)(elapsed / cooldownSeconds);
            }

            private static int CapToMaxPerTake(int accumulated, int maxPerTake)
            {
                if (accumulated <= 0) return 0;
                if (maxPerTake < 0) return accumulated;
                return Math.Min(accumulated, maxPerTake);
            }

            private static DateTime AdvanceLastClaimTime(DateTime lastClaimUtc, int claimsGiven, float cooldownSeconds)
            {
                if (claimsGiven <= 0 || cooldownSeconds <= 0f) return lastClaimUtc;
                return lastClaimUtc.AddSeconds(claimsGiven * cooldownSeconds);
            }

            /// <summary>
            /// Returns remaining time using the component output format settings.
            /// </summary>
            /// <param name="trimLeadingZeros">Whether to trim leading zeros in the first token.</param>
            /// <returns>Formatted remaining time string.</returns>
            public string GetFormattedTimeLeft(bool trimLeadingZeros = false)
            {
                return FormatTime(timeLeft, _displayTimeFormat, _displaySeparator, trimLeadingZeros);
            }

            /// <summary>
            /// Attempts to claim reward(s). Gives up to GetClaimableCount() (capped by max per take).
            /// </summary>
            /// <returns>True when at least one claim succeeds.</returns>
            [Button]
            public bool TakeReward()
            {
                int count = GetClaimableCount();
                if (count < 1)
                {
                    return false;
                }

                for (int i = 0; i < count; i++)
                {
                    OnRewardClaimed?.Invoke();
                }

                OnRewardsClaimed?.Invoke(count);

                if (_saveTimeOnTakeReward)
                {
                    if (TryGetLastRewardTimeUtc(out DateTime lastUtc))
                    {
                        DateTime newLast = AdvanceLastClaimTime(lastUtc, count, secondsToWaitForReward);
                        SaveLastRewardTime(newLast);
                    }
                    else
                    {
                        SaveCurrentTimeAsLastRewardTime();
                    }

                    _waitingForManualStart = false;
                    RefreshTimeState();
                    return true;
                }

                _waitingForManualStart = true;
                canTakeReward = false;
                timeLeft = 0f;
                OnTimeUpdated?.Invoke(timeLeft);
                return true;
            }

            /// <summary>
            /// Shortcut for reward claim, useful for UnityEvent bindings.
            /// </summary>
            public void Take()
            {
                TakeReward();
            }

            /// <summary>
            /// Checks whether at least one reward can be claimed right now.
            /// </summary>
            /// <returns>True when at least one reward is available.</returns>
            public bool CanTakeReward()
            {
                if (_waitingForManualStart)
                {
                    return false;
                }

                return GetClaimableCount() >= 1;
            }

            /// <summary>
            /// Starts timer updates. If save-on-claim is disabled, optionally starts cooldown from current UTC time.
            /// </summary>
            [Button]
            public void StartTime()
            {
                if (!_saveTimeOnTakeReward && _saveTimeOnStartWhenSaveOnTakeDisabled)
                {
                    SaveCurrentTimeAsLastRewardTime();
                }

                _waitingForManualStart = false;

                if (_isTimerRunning)
                {
                    if (_isTimerPaused)
                    {
                        _isTimerPaused = false;
                        OnTimerResumed?.Invoke();
                    }

                    RefreshTimeState();
                    return;
                }

                _isTimerRunning = true;
                _isTimerPaused = false;
                _timerRoutine = StartCoroutine(TimerRoutine());
                OnTimerStarted?.Invoke();
                RefreshTimeState();
            }

            /// <summary>
            /// Stops timer updates.
            /// </summary>
            [Button]
            public void StopTime()
            {
                StopTimeInternal(true);
            }

            /// <summary>
            /// Pauses timer updates without resetting state.
            /// </summary>
            [Button]
            public void PauseTime()
            {
                if (!_isTimerRunning || _isTimerPaused)
                {
                    return;
                }

                _isTimerPaused = true;
                OnTimerPaused?.Invoke();
            }

            /// <summary>
            /// Resumes timer updates after pause.
            /// </summary>
            [Button]
            public void ResumeTime()
            {
                if (!_isTimerRunning || !_isTimerPaused)
                {
                    return;
                }

                _isTimerPaused = false;
                OnTimerResumed?.Invoke();
                RefreshTimeState();
            }

            /// <summary>
            /// Restarts cooldown from current UTC time and refreshes timer state.
            /// </summary>
            [Button]
            public void RestartTime()
            {
                SaveCurrentTimeAsLastRewardTime();
                _waitingForManualStart = false;

                if (!_isTimerRunning)
                {
                    StartTime();
                    return;
                }

                bool wasPaused = _isTimerPaused;
                _isTimerPaused = false;
                if (wasPaused)
                {
                    OnTimerResumed?.Invoke();
                }

                RefreshTimeState();
            }

            /// <summary>
            /// Clears saved cooldown and makes reward available immediately.
            /// </summary>
            [Button]
            public void SetRewardAvailableNow()
            {
                SaveProvider.DeleteKey(BuildRewardTimeKey());
                lastRewardTimeStr = string.Empty;
                _waitingForManualStart = false;
                RefreshTimeState();
            }

            /// <summary>
            /// Forces immediate timer refresh and event dispatch.
            /// </summary>
            [Button]
            public void RefreshTimeState()
            {
                if (_waitingForManualStart)
                {
                    canTakeReward = false;
                    timeLeft = 0f;
                    OnTimeUpdated?.Invoke(timeLeft);
                    return;
                }

                timeLeft = GetSecondsUntilReward();
                OnTimeUpdated?.Invoke(timeLeft);

                bool rewardAvailable = GetClaimableCount() >= 1;
                if (rewardAvailable && !canTakeReward)
                {
                    canTakeReward = true;
                    OnRewardAvailable?.Invoke();
                }
                else if (!rewardAvailable)
                {
                    canTakeReward = false;
                }
            }

            /// <summary>
            /// Changes additional save key suffix.
            /// </summary>
            /// <param name="addKey">Key suffix for reward timer slot.</param>
            /// <param name="refreshAfterChange">If true, recalculates timer right away.</param>
            public void SetAdditionalKey(string addKey, bool refreshAfterChange = true)
            {
                _addKey = string.IsNullOrWhiteSpace(addKey) ? "Bonus1" : addKey.Trim();

                if (refreshAfterChange)
                {
                    _waitingForManualStart = false;
                    RefreshTimeState();
                }
            }

            /// <summary>
            /// Tries to read last reward timestamp from persistent storage.
            /// </summary>
            /// <param name="lastRewardUtc">Parsed UTC timestamp.</param>
            /// <returns>True when timestamp exists and is valid.</returns>
            public bool TryGetLastRewardTimeUtc(out DateTime lastRewardUtc)
            {
                string raw = SaveProvider.GetString(BuildRewardTimeKey(), string.Empty);
                return raw.TryParseUtcRoundTrip(out lastRewardUtc);
            }

            /// <summary>
            /// Gets elapsed seconds since the last reward claim.
            /// </summary>
            /// <returns>Elapsed seconds or zero when timestamp is missing.</returns>
            public float GetElapsedSinceLastReward()
            {
                return TryGetLastRewardTimeUtc(out DateTime lastRewardUtc)
                    ? Mathf.Max(0f, lastRewardUtc.GetSecondsSinceUtc(DateTime.UtcNow))
                    : 0f;
            }

            private IEnumerator TimerRoutine()
            {
                while (_isTimerRunning)
                {
                    yield return new WaitForSecondsRealtime(GetSafeUpdateInterval());

                    if (_isTimerRunning && !_isTimerPaused)
                    {
                        RefreshTimeState();
                    }
                }
            }

            private void StopTimeInternal(bool invokeEvent)
            {
                if (_timerRoutine != null)
                {
                    StopCoroutine(_timerRoutine);
                    _timerRoutine = null;
                }

                bool wasRunning = _isTimerRunning;
                _isTimerRunning = false;
                _isTimerPaused = false;

                if (invokeEvent && wasRunning)
                {
                    OnTimerStopped?.Invoke();
                }
            }

            private string BuildRewardTimeKey()
            {
                return LastRewardTimeKeyPrefix + _addKey;
            }

            private float GetSafeUpdateInterval()
            {
                return Mathf.Max(MinUpdateInterval, updateTime);
            }

            private void SaveCurrentTimeAsLastRewardTime()
            {
                canTakeReward = false;
                lastRewardTimeStr = DateTime.UtcNow.ToRoundTripUtcString();
                SaveProvider.SetString(BuildRewardTimeKey(), lastRewardTimeStr);
            }

            private void SaveLastRewardTime(DateTime utc)
            {
                lastRewardTimeStr = utc.ToRoundTripUtcString();
                SaveProvider.SetString(BuildRewardTimeKey(), lastRewardTimeStr);
            }
        }
    }
}