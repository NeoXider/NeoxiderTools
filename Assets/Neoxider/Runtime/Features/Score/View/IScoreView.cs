namespace Neo.Runtime.Features.Score.View
{
    /// <summary>
    /// Interface for score view that defines UI update methods.
    /// </summary>
    public interface IScoreView
    {
        /// <summary>
        /// Update score values in UI
        /// </summary>
        /// <param name="current">Current score value</param>
        /// <param name="max">Maximum score value</param>
        void UpdateScore(int current, int max);

        /// <summary>
        /// Update maximum score value in UI
        /// </summary>
        /// <param name="max">Maximum score value</param>
        void UpdateMaxScore(int max);

        /// <summary>
        /// Update score percentage in UI
        /// </summary>
        /// <param name="percentage">Score percentage value</param>
        void UpdateScorePercentage(float percentage);

        /// <summary>
        /// Update score percent 100 in UI
        /// </summary>
        /// <param name="percent100">Score percent 100 value</param>
        void UpdateScorePercent100(float percent100);

        /// <summary>
        /// Show goal reached state in UI
        /// </summary>
        /// <param name="reached">Goal reached status</param>
        void ShowGoalReached(bool reached);

        /// <summary>
        /// Update score delta in UI
        /// </summary>
        /// <param name="delta">Score delta value</param>
        void UpdateScoreDelta(int delta);
    }
}