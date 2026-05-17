using Neo.Reactive;
using Neo.Rpg.Components;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Neo.Rpg.UI
{
    /// <summary>
    ///     UI connector for the level of an <see cref="RpgCharacter"/>.
    /// </summary>
    [NeoDoc("Rpg/UI/RpgLevelTextUI.md")]
    [AddComponentMenu("Neoxider/RPG/UI/" + nameof(RpgLevelTextUI))]
    public sealed class RpgLevelTextUI : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Character to display. If empty, the component searches its parents on Start.")]
        [SerializeField] private RpgCharacter _character;

        [Header("UI Bindings")]
        public Text levelText;

        [Header("Settings")]
        [Tooltip("Format for the Level text. {0}=Current Level.")]
        [SerializeField] private string textFormat = "Lv.{0}";

        [Header("Events")]
        [SerializeField] private UnityEvent<int> _onLevelChanged = new();
        [SerializeField] private UnityEvent<string> _onLevelTextChanged = new();

        public UnityEvent<int> OnLevelChanged => _onLevelChanged;
        public UnityEvent<string> OnLevelTextChanged => _onLevelTextChanged;

        private ReactivePropertyInt _boundProperty;

        public RpgCharacter Character { get => _character; set { Unbind(); _character = value; Bind(); } }

        private void Start()
        {
            if (_character == null) _character = GetComponentInParent<RpgCharacter>();
            Bind();
        }

        private void OnDestroy() => Unbind();

        private void Bind()
        {
            if (_character == null) return;
            _boundProperty = _character.LevelState;
            _boundProperty.AddListener(OnLevelChangedInternal);
            OnLevelChangedInternal(_boundProperty.CurrentValue);
        }

        private void Unbind()
        {
            if (_boundProperty == null) return;
            _boundProperty.RemoveListener(OnLevelChangedInternal);
            _boundProperty = null;
        }

        private void OnLevelChangedInternal(int levelVal)
        {
            _onLevelChanged?.Invoke(levelVal);
            string text = string.Format(textFormat, levelVal);
            if (levelText != null) levelText.text = text;
            _onLevelTextChanged?.Invoke(text);
        }
    }
}
