using Neo.Reactive;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Neo.Rpg.UI
{
    /// <summary>
    ///     Universal UI connector for an RpgCombatant or RpgStatsManager Level representation.
    /// </summary>
    [NeoDoc("Rpg/UI/RpgLevelTextUI.md")]
    [AddComponentMenu("Neoxider/RPG/UI/" + nameof(RpgLevelTextUI))]
    public sealed class RpgLevelTextUI : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Target (RpgCombatant or RpgStatsManager).")]
        [SerializeField] private Component target;

        [Header("UI Bindings")]
        [Tooltip("Optional Canvas Text for Level value representation.")]
        public Text levelText;

        [Header("Settings")]
        [Tooltip("Format for the Level text (e.g. 'Lv.{0}'). {0}=Current Level.")]
        [SerializeField] private string textFormat = "Lv.{0}";

        [Header("Events (NoCode / TMP)")]
        [SerializeField] private UnityEvent<int> onLevelChanged = new();
        [SerializeField] private UnityEvent<string> onLevelTextChanged = new();

        public UnityEvent<int> OnLevelChanged => onLevelChanged;
        public UnityEvent<string> OnLevelTextChanged => onLevelTextChanged;

        private RpgCombatant _combatant;
        private RpgStatsManager _statsManager;
        private ReactivePropertyInt _boundProperty;

        private void Start()
        {
            if (target != null)
            {
                Bind(target);
            }
        }

        private void OnDestroy()
        {
            if (_boundProperty != null)
            {
                _boundProperty.RemoveListener(OnLevelChangedInternal);
            }
        }

        public void Bind(Component receiverComponent)
        {
            if (_boundProperty != null)
            {
                _boundProperty.RemoveListener(OnLevelChangedInternal);
                _boundProperty = null;
            }

            _combatant = receiverComponent as RpgCombatant;
            _statsManager = receiverComponent as RpgStatsManager;
            target = receiverComponent;

            if (_combatant != null)
            {
                _boundProperty = _combatant.LevelState;
                _boundProperty.AddListener(OnLevelChangedInternal);
                OnLevelChangedInternal(_boundProperty.Value);
            }
            else if (_statsManager != null)
            {
                _boundProperty = _statsManager.LevelState;
                _boundProperty.AddListener(OnLevelChangedInternal);
                OnLevelChangedInternal(_boundProperty.Value);
            }
            else
            {
                Debug.LogWarning($"[RpgLevelTextUI] Target '{receiverComponent?.name}' is neither RpgCombatant nor RpgStatsManager.", this);
            }
        }

        private void OnLevelChangedInternal(int levelVal)
        {
            onLevelChanged?.Invoke(levelVal);

            string text = string.Format(textFormat, levelVal);

            if (levelText != null)
            {
                levelText.text = text;
            }

            onLevelTextChanged?.Invoke(text);
        }
    }
}
