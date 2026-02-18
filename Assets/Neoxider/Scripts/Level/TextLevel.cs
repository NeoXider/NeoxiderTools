using Neo.Extensions;
using Neo.Tools;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Level
{
    [CreateFromMenu("Neoxider/Level/TextLevel")]
    [AddComponentMenu("Neoxider/Level/" + nameof(TextLevel))]
    public class TextLevel : SetText
    {
        private enum LevelDisplayMode
        {
            Current = 0,
            Max = 1
        }

        [SerializeField] private LevelDisplayMode _displayMode = LevelDisplayMode.Current;
        [SerializeField] [HideInInspector] private bool _best;
        [SerializeField] private int _displayOffset = 1;

        private UnityEvent<int> _event;

        private void OnEnable()
        {
            IndexOffset = _displayOffset;
            this.WaitWhile(() => LevelManager.I == null, Init);
        }

        private void OnValidate()
        {
            if (_best)
            {
                _displayMode = LevelDisplayMode.Max;
            }
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
            LevelManager lm = LevelManager.I;
            if (lm == null)
            {
                return;
            }

            if (_displayMode == LevelDisplayMode.Max || _best)
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
