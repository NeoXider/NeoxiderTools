using Neo.Extensions;
using UnityEngine;

namespace Neo.Tools
{
    [AddComponentMenu("Neo/Tools/" + nameof(TextScore))]
    public class TextScore : SetText
    {
        [SerializeField] private bool _best;

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

            if (_best)
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

            if (_best)
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