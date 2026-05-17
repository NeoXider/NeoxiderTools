using Neo.Reactive;
using Neo.Rpg.Components;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Neo.Rpg.UI
{
    /// <summary>
    ///     UI connector for the HP percentage of an <see cref="RpgCharacter"/>.
    ///     Slider / Image fillAmount / Text get updated whenever the character's HP changes.
    /// </summary>
    [NeoDoc("Rpg/UI/RpgHpBarUI.md")]
    [AddComponentMenu("Neoxider/RPG/UI/" + nameof(RpgHpBarUI))]
    public sealed class RpgHpBarUI : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Character to display. If empty, the component searches its parents on Start.")]
        [SerializeField] private RpgCharacter _character;

        [Header("UI Bindings")]
        [Tooltip("Optional Canvas Slider to control.")]
        public Slider hpSlider;

        [Tooltip("Optional Canvas Image to control via fillAmount.")]
        public Image hpFillImage;

        [Tooltip("Optional Canvas Text for HP value representation.")]
        public Text hpText;

        [Header("Settings")]
        [Tooltip("Format for the HP text. {0}=Current, {1}=Max, {2}=Percent.")]
        [SerializeField] private string textFormat = "{0} / {1}";

        [Header("Events")]
        [SerializeField] private UnityEvent<float> _onHpPercentChanged = new();
        [SerializeField] private UnityEvent<string> _onHpTextChanged = new();

        public UnityEvent<float> OnHpPercentChanged => _onHpPercentChanged;
        public UnityEvent<string> OnHpTextChanged => _onHpTextChanged;

        private ReactivePropertyFloat _boundProperty;

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
            _boundProperty = _character.HpPercentState;
            if (_boundProperty == null) return;
            _boundProperty.AddListener(OnHpChanged);
            OnHpChanged(_boundProperty.CurrentValue);
        }

        private void Unbind()
        {
            if (_boundProperty == null) return;
            _boundProperty.RemoveListener(OnHpChanged);
            _boundProperty = null;
        }

        private void OnHpChanged(float hpPercent)
        {
            if (hpSlider != null) hpSlider.value = hpPercent;
            if (hpFillImage != null) hpFillImage.fillAmount = hpPercent;

            _onHpPercentChanged?.Invoke(hpPercent);

            float current = _character != null ? _character.HpValue : 0f;
            float max = _character != null ? _character.MaxHpValue : 0f;

            string text = string.Format(textFormat,
                Mathf.RoundToInt(current),
                Mathf.RoundToInt(max),
                Mathf.RoundToInt(hpPercent * 100f));

            if (hpText != null) hpText.text = text;
            _onHpTextChanged?.Invoke(text);
        }
    }
}
