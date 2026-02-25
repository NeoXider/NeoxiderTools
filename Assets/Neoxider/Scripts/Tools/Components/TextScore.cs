using Neo.Extensions;
using UnityEngine;

namespace Neo.Tools
{
    [CreateFromMenu("Neoxider/Tools/Components/TextScore")]
    [AddComponentMenu("Neoxider/Tools/" + nameof(TextScore))]
    public class TextScore : SetText
    {
        [SerializeField] private ScoreDisplayMode _displayMode = ScoreDisplayMode.Current;
        [SerializeField] [HideInInspector] private bool _best;
        [SerializeField]
        [Tooltip("Источник счёта. Если не задан — ScoreManager.I. Задайте при нескольких ScoreManager в сцене.")]
        private ScoreManager _scoreSource;

        private ScoreManager _scoreManager;

        private ScoreManager GetScoreManager()
        {
            return _scoreSource != null ? _scoreSource : ScoreManager.I;
        }

        private void Start()
        {
            if (GetScoreManager() == null)
            {
                this.WaitWhile(() => GetScoreManager() == null, Init);
                return;
            }
            Init();
        }

        private void OnEnable()
        {
            if (_scoreManager != null)
                Init();
        }

        private void OnDisable()
        {
            if (_scoreManager == null)
            {
                return;
            }

            if (_displayMode == ScoreDisplayMode.Best || _best)
            {
                _scoreManager.BestScore.OnChanged.RemoveListener(Set);
            }
            else
            {
                _scoreManager.Score.OnChanged.RemoveListener(Set);
            }
        }

        private void OnValidate()
        {
            if (_best)
            {
                _displayMode = ScoreDisplayMode.Best;
            }
        }

        private void Init()
        {
            ScoreManager sm = GetScoreManager();
            if (sm == null)
            {
                return;
            }

            _scoreManager = sm;

            if (_displayMode == ScoreDisplayMode.Best || _best)
            {
                sm.BestScore.OnChanged.RemoveListener(Set);
                Set(sm.BestScoreValue);
                sm.BestScore.OnChanged.AddListener(Set);
            }
            else
            {
                sm.Score.OnChanged.RemoveListener(Set);
                Set(sm.ScoreValue);
                sm.Score.OnChanged.AddListener(Set);
            }
        }

        private enum ScoreDisplayMode
        {
            Current = 0,
            Best = 1
        }
    }
}