using UnityEngine;
using UnityEngine.Events;

namespace Neo.Runtime.Features.Score.View
{
    /// <summary>
    /// View component for score system that handles UI updates and events.
    /// </summary>
    public class ScoreView : MonoBehaviour, IScoreView
    {
        [SerializeField] private int current;
        [SerializeField] private int max;
        [SerializeField] private float percent;
        [SerializeField] private float percent100;
        [SerializeField] private int delta;

        /// <summary>
        /// Event triggered when score changes
        /// </summary>
        public UnityEvent OnScoreChangedEvent;

        /// <summary>
        /// Event triggered when goal is reached
        /// </summary>
        public UnityEvent<bool> OnGoalReachedEvent;

        /// <summary>
        /// Event triggered when maximum score changes
        /// </summary>
        public UnityEvent<int> OnMaxScoreChangedEvent;

        /// <summary>
        /// Event triggered when score percentage changes
        /// </summary>
        public UnityEvent<float> OnScorePercentageChangedEvent;

        /// <summary>
        /// Event triggered when score percent 100 changes
        /// </summary>
        public UnityEvent<float> OnScorePercent100ChangedEvent;

        /// <summary>
        /// Event triggered when score delta changes
        /// </summary>
        public UnityEvent<int> OnScoreDeltaChangedEvent;

        public void UpdateScore(int current, int max)
        {
            this.current = current;
            this.max = max;
            percent = max > 0 ? (float)current / max : 0f;
            percent100 = percent * 100f;
            OnScoreChangedEvent?.Invoke();
        }

        public void ShowGoalReached(bool reached)
        {
            OnGoalReachedEvent?.Invoke(reached);
        }

        public void UpdateMaxScore(int max)
        {
            this.max = max;
            OnMaxScoreChangedEvent?.Invoke(max);
        }

        public void UpdateScorePercentage(float percentage)
        {
            percent = percentage;
            OnScorePercentageChangedEvent?.Invoke(percentage);
        }

        public void UpdateScorePercent100(float p100)
        {
            percent100 = p100;
            OnScorePercent100ChangedEvent?.Invoke(p100);
        }

        public void UpdateScoreDelta(int delta)
        {
            this.delta = delta;
            OnScoreDeltaChangedEvent?.Invoke(delta);
        }
    }
}