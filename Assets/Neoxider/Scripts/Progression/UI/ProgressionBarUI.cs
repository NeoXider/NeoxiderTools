using Neo.Reactive;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Neo.Progression.UI
{
    /// <summary>
    ///     Universal UI connector for ProgressionManager states (Level, XP).
    /// </summary>
    [NeoDoc("Progression/UI/ProgressionBarUI.md")]
    [AddComponentMenu("Neoxider/Progression/UI/" + nameof(ProgressionBarUI))]
    public sealed class ProgressionBarUI : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("The ProgressionManager to read from. If null, uses the singleton Instance.")]
        [SerializeField] private ProgressionManager targetManager;

        [Header("UI Bindings")]
        [Tooltip("Optional Canvas Text for Level/XP representation.")]
        public Text levelText;

        [Header("Settings")]
        [Tooltip("Format for the Text (e.g. 'Level {0} - XP: {1} (To next: {2})'). {0}=Level, {1}=Total XP, {2}=XP to next level.")]
        [SerializeField] private string textFormat = "Level {0} | XP: {1} (Next: {2})";

        [Header("Events (NoCode / TMP)")]
        [SerializeField] private UnityEvent<string> onProgressionTextChanged = new();
        [SerializeField] private UnityEvent<int> onLevelChanged = new();

        public UnityEvent<string> OnProgressionTextChanged => onProgressionTextChanged;
        public UnityEvent<int> OnLevelChanged => onLevelChanged;

        private ProgressionManager _boundManager;

        private void Start()
        {
            if (targetManager == null)
            {
                targetManager = ProgressionManager.Instance;
            }

            if (targetManager != null)
            {
                Bind(targetManager);
            }
        }

        private void OnDestroy()
        {
            if (_boundManager != null)
            {
                _boundManager.LevelState.RemoveListener(OnStateUpdate);
                _boundManager.XpState.RemoveListener(OnStateUpdate);
                _boundManager.XpToNextLevelState.RemoveListener(OnStateUpdate);
            }
        }

        public void Bind(ProgressionManager manager)
        {
            if (_boundManager != null)
            {
                _boundManager.LevelState.RemoveListener(OnStateUpdate);
                _boundManager.XpState.RemoveListener(OnStateUpdate);
                _boundManager.XpToNextLevelState.RemoveListener(OnStateUpdate);
            }

            _boundManager = manager;

            if (_boundManager != null)
            {
                _boundManager.LevelState.AddListener(OnStateUpdate);
                _boundManager.XpState.AddListener(OnStateUpdate);
                _boundManager.XpToNextLevelState.AddListener(OnStateUpdate);
                OnStateUpdate(0); // Trigger update immediately
            }
        }

        private void OnStateUpdate(int _)
        {
            if (_boundManager == null) return;

            int level = _boundManager.LevelState.Value;
            int xp = _boundManager.XpState.Value;
            int xpToNext = _boundManager.XpToNextLevelState.Value;

            string text = string.Format(textFormat, level, xp, xpToNext);

            if (levelText != null)
            {
                levelText.text = text;
            }

            onLevelChanged?.Invoke(level);
            onProgressionTextChanged?.Invoke(text);
        }
    }
}
