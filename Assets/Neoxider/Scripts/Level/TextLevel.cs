using Neo.Extensions;
using Neo.Tools;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Level
{
    [AddComponentMenu("Neo/Level/" + nameof(TextLevel))]
    public class TextLevel : SetText
    {
        [SerializeField] private bool _best;
        [SerializeField] private int _displayOffset = 1;

        private UnityEvent<int> _event;

        private void OnEnable()
        {
            IndexOffset = _displayOffset;
            this.WaitWhile(() => LevelManager.I == null, Init);
        }

        private void OnDisable()
        {
            if (_event != null)
            {
                _event.RemoveListener(Set);
                _event = null;
            }
        }

        private void Init()
        {
            var lm = LevelManager.I;
            if (lm == null)
            {
                return;
            }

            if (_best)
            {
                Set(lm.MaxLevel);
                _event = lm.OnChangeMaxLevel;
            }
            else
            {
                Set(lm.CurrentLevel);
                _event = lm.OnChangeLevel;
            }

            _event?.AddListener(Set);
        }
    }
}


