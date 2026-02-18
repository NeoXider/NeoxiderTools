using Neo.Save;
using Neo.Shop;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Режим значения счётчика: целое (Int) или дробное (Float).
    /// </summary>
    public enum CounterValueMode
    {
        Int = 0,
        Float = 1
    }

    /// <summary>
    ///     Источник значения, передаваемого в OnSend при вызове Send() без аргумента.
    /// </summary>
    public enum CounterSendPayload
    {
        /// <summary>В OnSend передаётся текущее значение счётчика.</summary>
        Counter = 0,

        /// <summary>В OnSend передаётся текущий счёт ScoreManager.</summary>
        Score = 1,

        /// <summary>В OnSend передаётся текущее значение Money.</summary>
        Money = 2
    }

    /// <summary>
    ///     Универсальный счётчик: хранит число (int или float), Add/Subtract/Multiply/Divide/Set, Send с событием.
    ///     События по типу: OnValueChangedInt / OnValueChangedFloat, OnSendInt / OnSendFloat (в зависимости от режима).
    ///     Опционально сохраняет значение через SaveProvider по ключу (по умолчанию выключено).
    /// </summary>
    [NeoDoc("Tools/Components/Counter.md")]
    [AddComponentMenu("Neoxider/Tools/" + nameof(Counter))]
    public class Counter : MonoBehaviour
    {
        [SerializeField] [Tooltip("Mode: integer (Int) or float (Float).")]
        private CounterValueMode _valueMode = CounterValueMode.Int;

        [SerializeField] [Tooltip("Current counter value (default 1).")]
        private float _value = 1f;

        [SerializeField] [Tooltip("Value to pass to OnSend when calling Send() with no argument.")]
        private CounterSendPayload _sendPayload = CounterSendPayload.Counter;

        [Header("Сохранение")]
        [SerializeField]
        [Tooltip("Enable saving value on change (via SaveProvider). Off by default.")]
        private bool _saveEnabled;

        [SerializeField]
        [Tooltip("Save key (unique per counter). Used with SaveProvider, as in Money.")]
        private string _saveKey = "Counter";

        [Space]
        [Header("События по типу (вызывается одно в зависимости от режима)")]
        [Tooltip("Invoked when value changes in Int mode. Passes new integer value.")]
        public UnityEvent<int> OnValueChangedInt = new();

        [Tooltip("Invoked when value changes in Float mode. Passes new value.")]
        public UnityEvent<float> OnValueChangedFloat = new();

        [Space]
        [Tooltip("Invoked on Send() in Int mode. Passes integer (Payload or argument).")]
        public UnityEvent<int> OnSendInt = new();

        [Tooltip("Invoked on Send() in Float mode. Passes value (Payload or argument).")]
        public UnityEvent<float> OnSendFloat = new();

        [Space]
        [Tooltip(
            "Вызывается при любом изменении значения. Передаётся новое значение (float). Для типизированных подписок используйте OnValueChangedInt / OnValueChangedFloat.")]
        public UnityEvent<float> OnValueChanged = new();

        [Tooltip(
            "Вызывается при Send(). Передаётся значение (float). Для типизированных подписок используйте OnSendInt / OnSendFloat.")]
        public UnityEvent<float> OnSend = new();

        /// <summary>Текущее значение счётчика (целое в режиме Int).</summary>
        public int ValueInt => _valueMode == CounterValueMode.Int ? Mathf.RoundToInt(_value) : (int)_value;

        /// <summary>Текущее значение счётчика как float.</summary>
        public float ValueFloat => _value;

        private void Start()
        {
            if (_saveEnabled && !string.IsNullOrEmpty(_saveKey))
            {
                Load();
            }
        }

        /// <summary>Увеличивает счётчик на <paramref name="amount" />.</summary>
        [Button]
        public void Add(int amount)
        {
            ApplyDelta(amount);
        }

        [Button]
        public void Add(float amount)
        {
            ApplyDelta(amount);
        }

        /// <summary>Уменьшает счётчик на <paramref name="amount" />.</summary>
        [Button]
        public void Subtract(int amount)
        {
            ApplyDelta(-amount);
        }

        [Button]
        public void Subtract(float amount)
        {
            ApplyDelta(-amount);
        }

        /// <summary>Умножает счётчик на <paramref name="factor" />.</summary>
        [Button]
        public void Multiply(int factor)
        {
            ApplyFactor(factor);
        }

        [Button]
        public void Multiply(float factor)
        {
            ApplyFactor(factor);
        }

        /// <summary>Делит счётчик на <paramref name="divisor" />. При делении на 0 значение не меняется.</summary>
        [Button]
        public void Divide(int divisor)
        {
            if (divisor == 0)
            {
                return;
            }

            ApplyFactor(1f / divisor);
        }

        [Button]
        public void Divide(float divisor)
        {
            if (Mathf.Approximately(divisor, 0f))
            {
                return;
            }

            ApplyFactor(1f / divisor);
        }

        /// <summary>Устанавливает значение счётчика.</summary>
        [Button]
        public void Set(int value)
        {
            SetValue(_valueMode == CounterValueMode.Int ? value : (float)value);
        }

        [Button]
        public void Set(float value)
        {
            SetValue(_valueMode == CounterValueMode.Int ? Mathf.RoundToInt(value) : value);
        }

        /// <summary>Отправляет событие OnSend с значением по Send Payload. Счётчик не меняется.</summary>
        [Button]
        public void Send()
        {
            float payload = GetSendPayloadValue();
            InvokeSend(payload);
        }

        /// <summary>Отправляет событие OnSend с указанным числом. Счётчик не изменяется.</summary>
        [Button]
        public void Send(float valueToSend)
        {
            InvokeSend(valueToSend);
        }

        [Button]
        public void Send(int valueToSend)
        {
            InvokeSend(valueToSend);
        }

        private void Load()
        {
            _value = SaveProvider.GetFloat(_saveKey, _value);
        }

        private void SaveValue()
        {
            if (!_saveEnabled || string.IsNullOrEmpty(_saveKey))
            {
                return;
            }

            SaveProvider.SetFloat(_saveKey, _value);
        }

        private void InvokeSend(float payload)
        {
            OnSend?.Invoke(payload);
            if (_valueMode == CounterValueMode.Int)
            {
                OnSendInt?.Invoke(Mathf.RoundToInt(payload));
            }
            else
            {
                OnSendFloat?.Invoke(payload);
            }
        }

        private void ApplyDelta(float delta)
        {
            float next = _value + delta;
            if (_valueMode == CounterValueMode.Int)
            {
                next = Mathf.RoundToInt(next);
            }

            SetValue(next);
        }

        private void ApplyFactor(float factor)
        {
            float next = _value * factor;
            if (_valueMode == CounterValueMode.Int)
            {
                next = Mathf.RoundToInt(next);
            }

            SetValue(next);
        }

        private void SetValue(float newValue)
        {
            if (_valueMode == CounterValueMode.Int)
            {
                newValue = Mathf.RoundToInt(newValue);
            }

            if (Mathf.Approximately(_value, newValue))
            {
                return;
            }

            _value = newValue;
            OnValueChanged?.Invoke(_value);
            if (_valueMode == CounterValueMode.Int)
            {
                OnValueChangedInt?.Invoke(ValueInt);
            }
            else
            {
                OnValueChangedFloat?.Invoke(_value);
            }

            SaveValue();
        }

        private float GetSendPayloadValue()
        {
            switch (_sendPayload)
            {
                case CounterSendPayload.Counter:
                    return _value;
                case CounterSendPayload.Score:
                    return ScoreManager.I != null ? ScoreManager.I.Score : 0f;
                case CounterSendPayload.Money:
                    return Money.I != null ? Money.I.money : 0f;
                default:
                    return _value;
            }
        }
    }
}
