using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Neo
{
    namespace Bonus
    {
        [AddComponentMenu("Neoxider/" + "Bonus/" + nameof(TimeReward))]
        public class TimeReward : MonoBehaviour
        {
            private const string _lastRewardTimeKey = "LastRewardTime";
            [FormerlySerializedAs("_secondsToWaitForReward")] [SerializeField] public float secondsToWaitForReward = 60 * 60; //1 hours
            [FormerlySerializedAs("_startTakeReward")] [SerializeField] public bool startTakeReward;
            [FormerlySerializedAs("_lastRewardTimeStr")] [SerializeField] public string lastRewardTimeStr;
            [FormerlySerializedAs("_updateTime")] [SerializeField] [Min(0)] public float updateTime = 1;
            [SerializeField] private string _addKey = "Bonus1";

            public float timeLeft;

            public UnityEvent<float> OnTimeUpdated = new();
            public UnityEvent OnRewardClaimed = new();
            public UnityEvent OnRewardAvailable = new();

            private bool canTakeReward;

            private void Start()
            {
                InvokeRepeating(nameof(GetTime), 0, updateTime);

                if (startTakeReward) TakeReward();
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
                var time = TimeSpan.FromSeconds(seconds);
                return time.ToString(@"hh\:mm\:ss");
            }

            public float GetSecondsUntilReward()
            {
                lastRewardTimeStr = PlayerPrefs.GetString(_lastRewardTimeKey + _addKey, string.Empty);

                if (!string.IsNullOrEmpty(lastRewardTimeStr))
                {
                    DateTime lastRewardTime;

                    if (DateTime.TryParse(lastRewardTimeStr, out lastRewardTime))
                    {
                        var currentTime = DateTime.UtcNow;
                        var timeSinceLastReward = currentTime - lastRewardTime;
                        var secondsPassed = (float)timeSinceLastReward.TotalSeconds;
                        var secondsUntilReward = secondsToWaitForReward - secondsPassed;

                        return secondsUntilReward > 0 ? secondsUntilReward : 0;
                    }
                }

                return 0;
            }

#if ODIN_INSPECTOR
            [Sirenix.OdinInspector.Button]
#else
        [Button]
#endif
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