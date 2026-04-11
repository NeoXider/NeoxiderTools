using Neo.Reactive;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Neo.Rpg.UI
{
    /// <summary>
    ///     Universal UI connector for an RpgCombatant or RpgStatsManager HP bar.
    /// </summary>
    [NeoDoc("Rpg/UI/RpgHpBarUI.md")]
    [AddComponentMenu("Neoxider/RPG/UI/" + nameof(RpgHpBarUI))]
    public sealed class RpgHpBarUI : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Target (RpgCombatant or RpgStatsManager).")]
        [SerializeField] private Component target;

        [Header("UI Bindings")]
        [Tooltip("Optional Canvas Slider to control.")]
        public Slider hpSlider;

        [Tooltip("Optional Canvas Image to control via fillAmount.")]
        public Image hpFillImage;

        [Tooltip("Optional Canvas Text for HP value representation.")]
        public Text hpText;

        [Header("Settings")]
        [Tooltip("Format for the HP text (e.g. '{0} / {1}' or '{2:0}%'). {0}=Current, {1}=Max, {2}=Percent.")]
        [SerializeField] private string textFormat = "{0} / {1}";

        [Header("Events (NoCode / TMP)")]
        [SerializeField] private UnityEvent<float> onHpPercentChanged = new();
        [SerializeField] private UnityEvent<string> onHpTextChanged = new();

        public UnityEvent<float> OnHpPercentChanged => onHpPercentChanged;
        public UnityEvent<string> OnHpTextChanged => onHpTextChanged;

        private RpgCombatant _combatant;
        private RpgStatsManager _statsManager;
        private ReactivePropertyFloat _boundProperty;

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
                _boundProperty.RemoveListener(OnHpChanged);
            }
        }

        public void Bind(Component receiverComponent)
        {
            if (_boundProperty != null)
            {
                _boundProperty.RemoveListener(OnHpChanged);
                _boundProperty = null;
            }

            _combatant = receiverComponent as RpgCombatant;
            _statsManager = receiverComponent as RpgStatsManager;
            target = receiverComponent;

            if (_combatant != null)
            {
                _boundProperty = _combatant.HpPercentState;
                _boundProperty.AddListener(OnHpChanged);
                OnHpChanged(_boundProperty.Value);
            }
            else if (_statsManager != null)
            {
                _boundProperty = _statsManager.HpPercentState;
                _boundProperty.AddListener(OnHpChanged);
                OnHpChanged(_boundProperty.Value);
            }
            else
            {
                Debug.LogWarning($"[RpgHpBarUI] Target '{receiverComponent?.name}' is neither RpgCombatant nor RpgStatsManager.", this);
            }
        }

        private void OnHpChanged(float hpPercent)
        {
            if (hpSlider != null) hpSlider.value = hpPercent;
            if (hpFillImage != null) hpFillImage.fillAmount = hpPercent;

            onHpPercentChanged?.Invoke(hpPercent);

            float currentHp = 0f, maxHp = 0f;
            if (_combatant != null) { currentHp = _combatant.CurrentHp; maxHp = _combatant.MaxHp; }
            else if (_statsManager != null) { currentHp = _statsManager.CurrentHp; maxHp = _statsManager.MaxHp; }

            string text = string.Format(textFormat, Mathf.RoundToInt(currentHp), Mathf.RoundToInt(maxHp), Mathf.RoundToInt(hpPercent * 100f));

            if (hpText != null) hpText.text = text;
            onHpTextChanged?.Invoke(text);
        }
    }
}
