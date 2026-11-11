using Neo.Extensions;
using Neo.Tools;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Level
{
    [NeoDoc("Level/TextLevel.md")]
    [CreateFromMenu("Neoxider/Level/TextLevel")]
    [AddComponentMenu("Neoxider/Level/" + nameof(TextLevel))]
    public class TextLevel : SetText
    {
        [SerializeField] private LevelDisplayMode _displayMode = LevelDisplayMode.Current;
        [SerializeField] [HideInInspector] private bool _best;
        [SerializeField] private int _displayOffset = 1;

        [SerializeField]
        [Tooltip("Level source. If not set, LevelManager.I. Set when multiple LevelManagers are in the scene.")]
        private LevelManager _levelSource;

        private UnityEvent<int> _event;
        private bool _started;
        private CoroutineExtensions.CoroutineHandle _waitForManager;

        private void Start()
        {
            _started = true;
            IndexOffset = _displayOffset;
            Init();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            // WHY: OnDisable clears _event, so after Start the re-enable must re-subscribe
            // (checking _event != null here could never be true and text froze).
            if (_started && _event == null)
            {
                Init();
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _waitForManager?.Stop();
            _waitForManager = null;
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

        private LevelManager GetLevelManager()
        {
            return _levelSource != null ? _levelSource : LevelManager.I;
        }

        private void Init()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            LevelManager lm = GetLevelManager();
            if (lm == null)
            {
                if (_waitForManager == null)
                {
                    _waitForManager = this.WaitWhile(() => GetLevelManager() == null, () =>
                    {
                        _waitForManager = null;
                        Init();
                    });
                }

                return;
            }

            _waitForManager?.Stop();
            _waitForManager = null;
            // WHY: Init can run twice (OnEnable + deferred WaitWhile); drop the old listener first.
            if (_event != null)
            {
                _event.RemoveListener(Set);
                _event = null;
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
