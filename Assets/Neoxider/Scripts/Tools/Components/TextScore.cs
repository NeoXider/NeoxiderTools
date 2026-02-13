using Neo.Extensions;
using UnityEngine;

namespace Neo.Tools
{
    [AddComponentMenu("Neo/Tools/" + nameof(TextScore))]
    public class TextScore : SetText
    {
        private enum ScoreDisplayMode
        {
            Current = 0,
            Best = 1
        }

        [SerializeField] private ScoreDisplayMode _displayMode = ScoreDisplayMode.Current;
        [SerializeField] [HideInInspector] private bool _best;

        private void OnEnable()
        {
            this.WaitWhile(() => ScoreManager.I == null, Init);
        }

        private void OnValidate()
        {
            if (_best)
            {
                _displayMode = ScoreDisplayMode.Best;
            }
        }

        private void OnDisable()
        {
            if (ScoreManager.I == null)
            {
                return;
            }

            if (_displayMode == ScoreDisplayMode.Best || _best)
            {
                ScoreManager.I.OnBestValueChange.RemoveListener(Set);
            }
            else
            {
                ScoreManager.I.OnValueChange.RemoveListener(Set);
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
                Set(sm.BestScore);
                sm.OnBestValueChange.AddListener(Set);
            }
            else
            {
                Set(sm.Score);
                sm.OnValueChange.AddListener(Set);
            }
        }
    }
}