using System;
using Neo.Runtime.Features.Score.Model;
using Neo.Runtime.Features.Score.View;
using R3;
using VContainer;
using VContainer.Unity;

namespace Neo.Runtime.Features.Score.Presenter
{
    /// <summary>
    /// Presenter for score system that handles communication between model and view.
    /// </summary>
    public class ScorePresenter : IStartable, IDisposable
    {
        [Inject] private readonly ScoreModel _score;
        [Inject] private readonly IScoreView _view;

        private readonly CompositeDisposable _disp = new();

        public void Start()
        {
            _score.Current.AsObservable().Subscribe(OnScoreChanged).AddTo(_disp);
            _score.Max.AsObservable().Subscribe(OnMaxChanged).AddTo(_disp);
            _score.Percent.AsObservable().Subscribe(OnPercentChanged).AddTo(_disp);

            _score.OnReachedMax.Subscribe(_ => _view.ShowGoalReached(true)).AddTo(_disp);
        }

        /// <summary>
        /// Handle score change event
        /// </summary>
        /// <param name="current">Current score value</param>
        private void OnScoreChanged(int current)
        {
            _view.UpdateScore(current, _score.Max.Value);
        }

        /// <summary>
        /// Handle maximum score change event
        /// </summary>
        /// <param name="max">New maximum score value</param>
        private void OnMaxChanged(int max)
        {
            _view.UpdateMaxScore(max);
        }

        /// <summary>
        /// Handle score percentage change event
        /// </summary>
        /// <param name="p">Score percentage</param>
        private void OnPercentChanged(float p)
        {
            _view.UpdateScorePercentage(p);
            _view.UpdateScorePercent100(p * 100f);
        }

        /// <summary>
        /// Dispose presenter resources
        /// </summary>
        public void Dispose()
        {
            _disp.Dispose();
        }
    }
}