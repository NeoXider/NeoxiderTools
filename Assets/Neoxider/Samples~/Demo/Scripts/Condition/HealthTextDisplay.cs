using Neo.Tools;
using TMPro;
using UnityEngine;

namespace Neo.Demo.Condition
{
    /// <summary>
    ///     Отображает HP в TMP_Text, подписываясь на Health.Hp.OnChanged.
    ///     Формат: "HP: {current} / {max}"
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    [AddComponentMenu("Neoxider/Demo/Condition/HealthTextDisplay")]
    public class HealthTextDisplay : MonoBehaviour
    {
        [SerializeField] private Health _health;

        private TMP_Text _text;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _health.Hp.OnChanged.AddListener(UpdateText);
                UpdateText(_health.HpValue);
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.Hp.OnChanged.RemoveListener(UpdateText);
            }
        }

        private void UpdateText(int hp)
        {
            if (_text != null && _health != null)
            {
                _text.text = $"HP: {hp} / {_health.MaxHp}";
            }
        }
    }
}
