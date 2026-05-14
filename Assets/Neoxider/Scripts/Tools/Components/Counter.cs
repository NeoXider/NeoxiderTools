using Neo.Reactive;
using Neo.Save;
using Neo.Shop;
using Neo.Network;
using UnityEngine;
using UnityEngine.Events;
#if MIRROR
using Mirror;
#endif

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
    ///     by type: OnValueChangedInt/Float, OnSendInt/OnSendFloat, OnLoadedInt/Float after save load. Optionally saves
    ///     via SaveProvider (off by default). Optional Send On Start and Load On Start toggles.
    /// </summary>
    [NeoDoc("Tools/Components/Counter.md")]
    [CreateFromMenu("Neoxider/Tools/Components/Counter")]
    [AddComponentMenu("Neoxider/Tools/" + nameof(Counter))]
    public class Counter : NeoNetworkComponent
    {
#if MIRROR
        /// <summary>Server-authoritative value, synced to late-joining clients.</summary>
        [SyncVar]
        private float _syncValue;

        private float _lastCmdTime;
        private const float CmdRateLimit = 0.05f;
#endif

        [SerializeField] [Tooltip("Mode: integer (Int) or float (Float).")]
        private CounterValueMode _valueMode = CounterValueMode.Int;

        [Tooltip("Current counter value. Initial value used when save is disabled.")]
        public ReactivePropertyFloat Value = new();

        [SerializeField] [Tooltip("Value to pass to OnSend when calling Send() with no argument.")]
        private CounterSendPayload _sendPayload = CounterSendPayload.Counter;

        [Header("Repeat Event")]
        [SerializeField]
        [Tooltip(
            "Invoke Repeat event N times when counter value changes (N = current counter value, clamped to >= 0).")]
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
            "When Save is enabled, read saved value from SaveProvider on Start. Off = keep Inspector value until LoadFromSave().")]
        private bool _loadOnStart = true;

        [SerializeField]
        [Tooltip(
            "When loading value in Start, invoke OnValueChanged* so UI and subscribers apply loaded value. On by default.")]
        private bool _invokeEventsOnLoad = true;

        [SerializeField]
        [Tooltip("After Start setup, call Send() once (uses Send Payload; counter value unchanged). Off by default.")]
        private bool _sendOnStart;

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

        [Header("Events (after load from save)")]
        [Tooltip(
            "Invoked once after Load() from SaveProvider in Int mode. Use for UI/state that only reacts to persistence load.")]
        public UnityEvent<int> OnLoadedInt = new();

        [Tooltip("Invoked once after Load() from SaveProvider in Float mode.")]
        public UnityEvent<float> OnLoadedFloat = new();

        /// <summary>Current counter value as int (rounded in Float mode).</summary>
        public int ValueInt => _valueMode == CounterValueMode.Int
            ? Mathf.RoundToInt(Value.CurrentValue)
            : (int)Value.CurrentValue;

        /// <summary>Current counter value as float.</summary>
        public float ValueFloat => Value.CurrentValue;

        private void Start()
        {
            if (_saveEnabled && !string.IsNullOrEmpty(_saveKey) && _loadOnStart)
            {
                Load();
                InvokeOnLoaded();
                if (_invokeEventsOnLoad)
                {
                    InvokeValueChanged();
                }
            }

            if (_sendOnStart)
            {
                Send();
            }
        }

        public static readonly System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Counter>> Registry = new();

        protected virtual void OnEnable()
        {
            if (!string.IsNullOrEmpty(_saveKey))
            {
                if (!Registry.TryGetValue(_saveKey, out var list))
                {
                    list = new System.Collections.Generic.List<Counter>();
                    Registry[_saveKey] = list;
                }
                if (!list.Contains(this))
                {
                    list.Add(this);
                }
            }
        }

        protected virtual void OnDisable()
        {
            if (!string.IsNullOrEmpty(_saveKey) && Registry.TryGetValue(_saveKey, out var list))
            {
                list.Remove(this);
            }
        }

#if MIRROR
        protected override void OnValidate()
        {
            if (isNetworked)
            {
                base.OnValidate();
            }
        }
#endif

        /// <summary>
        ///     Loads value from SaveProvider now and notifies subscribers (OnLoaded*, and OnValueChanged* if Invoke Events On Load).
        ///     Use when Load On Start is off or to refresh from disk after Start.
        /// </summary>
        [Button]
        public void LoadFromSave()
        {
            if (!_saveEnabled || string.IsNullOrEmpty(_saveKey))
            {
                return;
            }

            Load();
            InvokeOnLoaded();
            if (_invokeEventsOnLoad)
            {
                InvokeValueChanged();
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

#if MIRROR
            if (isNetworked && (NeoNetworkState.IsClient || NeoNetworkState.IsServer))
            {
                if (NeoNetworkState.IsClient && !NeoNetworkState.IsServer)
                {
                    CmdSetValue(newValue);
                    return;
                }
            }
#endif

            ApplyValueLocally(newValue);

#if MIRROR
            if (isNetworked && NeoNetworkState.IsServer)
            {
                _syncValue = newValue;
                RpcSetValue(newValue);
            }
#endif
        }

        private void ApplyValueLocally(float newValue)
        {
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

#if MIRROR
        [Command(requiresAuthority = false)]
        private void CmdSetValue(float newValue, NetworkConnectionToClient sender = null)
        {
            // Rate-limit: reject commands arriving faster than CmdRateLimit
            if (Time.time - _lastCmdTime < CmdRateLimit) return;
            _lastCmdTime = Time.time;

            _syncValue = newValue;
            ApplyValueLocally(newValue);
            RpcSetValue(newValue);
        }

        [ClientRpc(includeOwner = true)]
        private void RpcSetValue(float newValue)
        {
            if (isServerOnly) return;
            ApplyValueLocally(newValue);
        }

        /// <summary>
        ///     Late-join: when a new client connects, apply the server's authoritative value.
        /// </summary>
        protected override void ApplyNetworkState() => ApplyValueLocally(_syncValue);
#endif

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

        private void InvokeOnLoaded()
        {
            if (_valueMode == CounterValueMode.Int)
            {
                OnLoadedInt?.Invoke(ValueInt);
            }
            else
            {
                OnLoadedFloat?.Invoke(Value.CurrentValue);
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

