using System;
using R3;

namespace Neo.Runtime.Features.Score.Model
{
    /// <summary>
    /// Score model with reactive properties for tracking game score.
    /// </summary>
    public class ScoreModel : IDisposable
    {
        /// <summary>
        /// Current score value
        /// </summary>
        public ReactiveProperty<int> Current { get; }

        /// <summary>
        /// Maximum score value
        /// </summary>
        public ReactiveProperty<int> Max { get; }

        /// <summary>
        /// Percentage of current score from maximum (0-1)
        /// </summary>
        public ReactiveProperty<float> Percent { get; }

        /// <summary>
        /// Event that occurs when score reaches maximum
        /// </summary>
        public Observable<Unit> OnReachedMax { get; }

        /// <summary>
        /// Constructor for score model
        /// </summary>
        /// <param name="maxScore">Maximum score points</param>
        /// <param name="startScore">Initial score points (default 0)</param>
        public ScoreModel(int maxScore, int startScore = 0)
        {
            if (maxScore <= 0)
            {
                maxScore = 1;
            }

            int init = startScore <= 0 || startScore > maxScore ? 0 : startScore;

            Current = new BindableReactiveProperty<int>(init);
            Max = new BindableReactiveProperty<int>(maxScore);
            Percent = new BindableReactiveProperty<float>();

            Current.AsObservable().Subscribe(_ => Recalc());
            Max.AsObservable().Subscribe(_ => Recalc());

            OnReachedMax = Current.AsObservable()
                .Where(s => s >= Max.Value)
                .Select(_ => Unit.Default);

            void Recalc()
            {
                Percent.Value = Max.Value > 0 ? (float)Current.Value / Max.Value : 0f;
            }
        }

        /// <summary>
        /// Add points to current score
        /// </summary>
        /// <param name="amount">Points to add</param>
        public void Add(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            Current.Value = Math.Min(Max.Value, Current.Value + amount);
        }

        /// <summary>
        /// Remove points from current score
        /// </summary>
        /// <param name="amount">Points to remove</param>
        public void Subtract(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            Current.Value = Math.Max(0, Current.Value - amount);
        }

        /// <summary>
        /// Reset score to zero
        /// </summary>
        public void Reset()
        {
            Current.Value = 0;
        }

        /// <summary>
        /// Set new maximum score value
        /// </summary>
        /// <param name="newMax">New maximum value</param>
        public void SetMax(int newMax)
        {
            if (newMax <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(newMax));
            }

            Max.Value = newMax;
            if (Current.Value > Max.Value)
            {
                Current.Value = Max.Value;
            }
        }

        /// <summary>
        /// Set specific score amount
        /// </summary>
        /// <param name="score">Score amount to set</param>
        public void SetScore(int score)
        {
            if (score < 0)
            {
                score = 0;
            }

            Current.Value = Math.Min(Max.Value, score);
        }

        /// <summary>
        /// Dispose model resources
        /// </summary>
        public void Dispose()
        {
            Current.Dispose();
            Max.Dispose();
            Percent.Dispose();
        }
    }
}