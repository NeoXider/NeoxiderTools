using System;
using UnityEngine;
using UnityEngine.Events;

namespace Neo
{
    namespace Bonus
    {
        [AddComponentMenu("Neoxider/" + "Bonus/" + nameof(TimeReward))]
        public class TimeReward : MonoBehaviour
        {
            [SerializeField] private int _secondsToWaitForReward = 60 * 60; //1 hours
            [SerializeField] private bool _startTakeReward = false;
            [SerializeField] private string _lastRewardTimeStr;
            [SerializeField, Min(0)] private float _updateTime = 1;
            [SerializeField] private string _addKey = "Bonus1";
            [SerializeField] private const string _lastRewardTimeKey = "LastRewardTime";

            public float timeLeft;

            public UnityEvent<float> OnTimeUpdated = new UnityEvent<float>();
            public UnityEvent OnRewardClaimed = new UnityEvent();
            public UnityEvent OnRewardAvailable = new UnityEvent();

            private bool canTakeReward = false;

            private void Start()
            {
                InvokeRepeating(nameof(GetTime), 0, _updateTime);

                if(_startTakeReward)
                {
                    TakeReward();
                }
            }

            private void GetTime()
            {
                timeLeft = GetSecondsUntilReward();
                OnTimeUpdated?.Invoke(timeLeft);

                if (timeLeft == 0 && !canTakeReward)
                {
                    OnRewardAvailable?.Invoke();
                    canTakeReward = true;
                }

            }

            public static string FormatTime(int seconds)
            {
                TimeSpan time = TimeSpan.FromSeconds(seconds);
                return time.ToString(@"hh\:mm\:ss");
            }

            public float GetSecondsUntilReward()
            {
                _lastRewardTimeStr = PlayerPrefs.GetString(_lastRewardTimeKey + _addKey, string.Empty);

                if (!string.IsNullOrEmpty(_lastRewardTimeStr))
                {
                    DateTime lastRewardTime;

                    if (DateTime.TryParse(_lastRewardTimeStr, out lastRewardTime))
                    {
                        DateTime currentTime = DateTime.UtcNow;
                        TimeSpan timeSinceLastReward = currentTime - lastRewardTime;
                        float secondsPassed = (float)timeSinceLastReward.TotalSeconds;
                        float secondsUntilReward = _secondsToWaitForReward - secondsPassed;

                        return secondsUntilReward > 0 ? secondsUntilReward : 0;
                    }
                }

                return 0;
            }

            public bool TakeReward()
            {
                if (CanTakeReward())
                {
                    SaveCurrentTimeAsLastRewardTime();
                    OnTimeUpdated?.Invoke(GetSecondsUntilReward());
                    return true;
                }

                return false;
            }

            public void Take()
            {
                TakeReward();
            }

            public bool CanTakeReward()
            {
                return GetSecondsUntilReward() == 0;
            }

            private void SaveCurrentTimeAsLastRewardTime()
            {
                canTakeReward = false;
                Debug.Log(nameof(SaveCurrentTimeAsLastRewardTime) + " " + _addKey);
                OnRewardClaimed?.Invoke();
                PlayerPrefs.SetString(_lastRewardTimeKey + _addKey, DateTime.UtcNow.ToString());
            }
        }
    }
}