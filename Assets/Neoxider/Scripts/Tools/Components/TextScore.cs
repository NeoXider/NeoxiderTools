using System;
using Neo.Extensions;
using Neo.Reactive;
using UnityEngine;

namespace Neo.Tools
{
    [NeoDoc("Tools/Components/TextScore.md")]
    [CreateFromMenu("Neoxider/Tools/Components/TextScore")]
    [AddComponentMenu("Neoxider/Tools/" + nameof(TextScore))]
    public class TextScore : SetText
    {
        [SerializeField] private ScoreDisplayMode _displayMode = ScoreDisplayMode.Current;
        [SerializeField] [HideInInspector] private bool _best;

        [SerializeField]
        [Tooltip("Score source. If unset, uses ScoreManager.I. Set explicitly when multiple ScoreManagers exist.")]
        private ScoreManager _scoreSource;

        private IDisposable _scoreSubscription;
        private CoroutineExtensions.CoroutineHandle _waitForScore;
        private bool _hasStarted;

        private void Start()
        {
            _hasStarted = true;
            if (GetScoreManager() == null)
            {
                _waitForScore = this.WaitWhile(() => GetScoreManager() == null, Init);
                return;
            }

            Init();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_hasStarted)
            {
                Start();
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _waitForScore?.Stop();
            _waitForScore = null;
            _scoreSubscription?.Dispose();
            _scoreSubscription = null;
        }

        private void OnValidate()
        {
            if (_best)
            {
                _displayMode = ScoreDisplayMode.Best;
            }
        }

        private ScoreManager GetScoreManager()
        {
            return _scoreSource != null ? _scoreSource : ScoreManager.I;
        }

        private void Init()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            ScoreManager sm = GetScoreManager();
            if (sm == null)
            {
                return;
            }

            _scoreSubscription?.Dispose();
            ReactivePropertyInt scoreProperty = _displayMode == ScoreDisplayMode.Best || _best
                ? sm.BestScore
                : sm.Score;
            _scoreSubscription = scoreProperty.Subscribe(Set);
            Set(scoreProperty.CurrentValue);
        }

        private enum ScoreDisplayMode
        {
            Current = 0,
            Best = 1
        }
    }
}
