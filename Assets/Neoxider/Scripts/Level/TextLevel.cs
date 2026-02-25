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
        [SerializeField] private LevelDisplayMode _displayMode = LevelDisplayMode.Current;
        [SerializeField] [HideInInspector] private bool _best;
        [SerializeField] private int _displayOffset = 1;
        [SerializeField]
        [Tooltip("Источник уровня. Если не задан — LevelManager.I. Задайте при нескольких LevelManager в сцене.")]
        private LevelManager _levelSource;

        private UnityEvent<int> _event;

        private LevelManager GetLevelManager()
        {
            return _levelSource != null ? _levelSource : LevelManager.I;
        }

        private void Start()
        {
            IndexOffset = _displayOffset;
            if (GetLevelManager() == null)
            {
                this.WaitWhile(() => GetLevelManager() == null, Init);
                return;
            }
            Init();
        }

        private void OnEnable()
        {
            if (_event != null)
                Init();
        }

        private void OnDisable()
        {
            if (_event != null)
            {
                _event.RemoveListener(Set);
                _event = null;
            }
        }

        private void OnValidate()
        {
            if (_best)
            {
                _displayMode = LevelDisplayMode.Max;
            }
        }

        private void Init()
        {
            LevelManager lm = GetLevelManager();
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

        private enum LevelDisplayMode
        {
            Current = 0,
            Max = 1
        }
    }
}