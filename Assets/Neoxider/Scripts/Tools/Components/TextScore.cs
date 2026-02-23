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

        private void OnEnable()
        {
            this.WaitWhile(() => ScoreManager.I == null, Init);
        }

        private void OnDisable()
        {
            if (ScoreManager.I == null)
            {
                return;
            }

            if (_displayMode == ScoreDisplayMode.Best || _best)
            {
                ScoreManager.I.BestScore.OnChanged.RemoveListener(Set);
            }
            else
            {
                ScoreManager.I.Score.OnChanged.RemoveListener(Set);
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
            ScoreManager sm = ScoreManager.I;
            if (sm == null)
            {
                return;
            }

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