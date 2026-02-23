using Neo.Reactive;
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
    [CreateFromMenu("Neoxider/Tools/Components/Counter")]
    [AddComponentMenu("Neoxider/Tools/" + nameof(Counter))]
    public class Counter : MonoBehaviour
    {
        [SerializeField] [Tooltip("Mode: integer (Int) or float (Float).")]
        private CounterValueMode _valueMode = CounterValueMode.Int;

        [Tooltip("Current counter value. Initial value used when save is disabled.")]
        public ReactivePropertyFloat Value = new();

        [SerializeField] [Tooltip("Value to pass to OnSend when calling Send() with no argument.")]
        private CounterSendPayload _sendPayload = CounterSendPayload.Counter;

        [Header("Save")]
        [SerializeField]
        [Tooltip("Enable saving value on change (via SaveProvider). Off by default.")]
        private bool _saveEnabled;

        [SerializeField] [Tooltip("Save key (unique per counter). Used with SaveProvider, as in Money.")]
        private string _saveKey = "Counter";

        [SerializeField]
        [Tooltip("При загрузке значения в Start вызывать OnValueChanged* (чтобы UI и подписчики применили загруженное значение). По умолчанию вкл.")]
        private bool _invokeEventsOnLoad = true;

        [Space]
        [Header("Events by type (one fired depending on mode)")]
        [Tooltip("Invoked when value changes in Int mode. Passes new integer value.")]
        public UnityEvent<int> OnValueChangedInt = new();

        [Tooltip("Invoked when value changes in Float mode. Passes new value.")]
        public UnityEvent<float> OnValueChangedFloat = new();

        [Space] [Tooltip("Invoked on Send() in Int mode. Passes integer (Payload or argument).")]
        public UnityEvent<int> OnSendInt = new();

        [Tooltip("Invoked on Send() in Float mode. Passes value (Payload or argument).")]
        public UnityEvent<float> OnSendFloat = new();

        [Tooltip(
            "Вызывается при Send(). Передаётся значение (float). Для типизированных подписок используйте OnSendInt / OnSendFloat.")]
        public UnityEvent<float> OnSend = new();

        /// <summary>Текущее значение счётчика (целое в режиме Int).</summary>
        public int ValueInt => _valueMode == CounterValueMode.Int ? Mathf.RoundToInt(Value.CurrentValue) : (int)Value.CurrentValue;

        /// <summary>Текущее значение счётчика как float.</summary>
        public float ValueFloat => Value.CurrentValue;

        private void Start()
        {
            if (_saveEnabled && !string.IsNullOrEmpty(_saveKey))
            {
                Load();
                if (_invokeEventsOnLoad)
                {
                    InvokeValueChanged();
                }
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
            Value.SetValueWithoutNotify(SaveProvider.GetFloat(_saveKey, Value.CurrentValue));
        }

        private void SaveValue()
        {
            if (!_saveEnabled || string.IsNullOrEmpty(_saveKey))
            {
                return;
            }

            SaveProvider.SetFloat(_saveKey, Value.CurrentValue);
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
            float next = Value.CurrentValue + delta;
            if (_valueMode == CounterValueMode.Int)
            {
                next = Mathf.RoundToInt(next);
            }

            SetValue(next);
        }

        private void ApplyFactor(float factor)
        {
            float next = Value.CurrentValue * factor;
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

            if (Mathf.Approximately(Value.CurrentValue, newValue))
            {
                return;
            }

            Value.Value = newValue;
            if (_valueMode == CounterValueMode.Int)
            {
                OnValueChangedInt?.Invoke(ValueInt);
            }
            else
            {
                OnValueChangedFloat?.Invoke(Value.CurrentValue);
            }

            SaveValue();
        }

        private void InvokeValueChanged()
        {
            Value.ForceNotify();
            if (_valueMode == CounterValueMode.Int)
            {
                OnValueChangedInt?.Invoke(ValueInt);
            }
            else
            {
                OnValueChangedFloat?.Invoke(Value.CurrentValue);
            }
        }

        private float GetSendPayloadValue()
        {
            switch (_sendPayload)
            {
                case CounterSendPayload.Counter:
                    return Value.CurrentValue;
                case CounterSendPayload.Score:
                    return ScoreManager.I != null ? (float)ScoreManager.I.ScoreValue : 0f;
                case CounterSendPayload.Money:
                    return Money.I != null ? Money.I.money : 0f;
                default:
                    return Value.CurrentValue;
            }
        }
    }
}