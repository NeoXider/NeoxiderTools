using Neo.Reactive;
using Neo.Save;
using Neo.Shop;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>Counter value mode: integer (Int) or float (Float).</summary>
    public enum CounterValueMode
    {
        Int = 0,
        Float = 1
    }

    /// <summary>Source of value passed to OnSend when Send() is called with no argument.</summary>
    public enum CounterSendPayload
    {
        /// <summary>OnSend receives current counter value.</summary>
        Counter = 0,

        /// <summary>OnSend receives current ScoreManager score.</summary>
        Score = 1,

        /// <summary>OnSend receives current Money value.</summary>
        Money = 2
    }

    /// <summary>
    ///     Universal counter: holds a number (int or float), Add/Subtract/Multiply/Divide/Set, Send with events. Events
    ///     by type: OnValueChangedInt/Float, OnSendInt/OnSendFloat. Optionally saves via SaveProvider (off by default).
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

        [Header("Repeat Event")]
        [SerializeField]
        [Tooltip("Invoke Repeat event N times when counter value changes (N = current counter value, clamped to >= 0).")]
        private bool _invokeRepeatEventOnValueChanged;

        [SerializeField]
        [Tooltip("Invoke Repeat event N times when Send() is called (N = current counter value, clamped to >= 0).")]
        private bool _invokeRepeatEventOnSend;

        [Header("Save")] [SerializeField] [Tooltip("Enable saving value on change (via SaveProvider). Off by default.")]
        private bool _saveEnabled;

        [SerializeField] [Tooltip("Save key (unique per counter). Used with SaveProvider, as in Money.")]
        private string _saveKey = "Counter";

        [SerializeField]
        [Tooltip(
            "When loading value in Start, invoke OnValueChanged* so UI and subscribers apply loaded value. On by default.")]
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

        [Tooltip("Invoked on Send(); passes value as float. For typed subscriptions use OnSendInt / OnSendFloat.")]
        public UnityEvent<float> OnSend = new();

        [Tooltip("Invoked N times (N = current counter value) when enabled by Repeat Event settings.")]
        public UnityEvent OnRepeatByCounterValue = new();

        /// <summary>Current counter value as int (rounded in Float mode).</summary>
        public int ValueInt => _valueMode == CounterValueMode.Int
            ? Mathf.RoundToInt(Value.CurrentValue)
            : (int)Value.CurrentValue;

        /// <summary>Current counter value as float.</summary>
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

        /// <summary>Adds amount to the counter.</summary>
        /// <param name="amount">Value to add.</param>
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

        /// <summary>Subtracts amount from the counter.</summary>
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

        /// <summary>Multiplies the counter by factor.</summary>
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

        /// <summary>Divides the counter by divisor. No change if divisor is 0.</summary>
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

        /// <summary>Sets the counter value.</summary>
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

        /// <summary>Invokes OnSend with value from Send Payload. Counter value is not changed.</summary>
        [Button]
        public void Send()
        {
            float payload = GetSendPayloadValue();
            InvokeSend(payload);
        }

        /// <summary>Invokes OnSend with the given value. Counter value is not changed.</summary>
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

            if (_invokeRepeatEventOnSend)
            {
                InvokeRepeatByCounterValue();
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

            if (_invokeRepeatEventOnValueChanged)
            {
                InvokeRepeatByCounterValue();
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
                    return ScoreManager.I != null ? ScoreManager.I.ScoreValue : 0f;
                case CounterSendPayload.Money:
                    return Money.I != null ? Money.I.money : 0f;
                default:
                    return Value.CurrentValue;
            }
        }

        private void InvokeRepeatByCounterValue()
        {
            int repeatCount = Mathf.Max(0, Mathf.RoundToInt(Value.CurrentValue));
            for (int i = 0; i < repeatCount; i++)
            {
                OnRepeatByCounterValue?.Invoke();
            }
        }
    }
}
