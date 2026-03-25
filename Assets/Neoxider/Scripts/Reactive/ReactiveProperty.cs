using System;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Reactive
{
    [Serializable]
    public class UnityEventFloat : UnityEvent<float>
    {
    }

    [Serializable]
    public class UnityEventInt : UnityEvent<int>
    {
    }

    [Serializable]
    public class UnityEventBool : UnityEvent<bool>
    {
    }

    /// <summary>
    ///     Reactive variable (float): value + UnityEvent. R3-style API.
    /// </summary>
    [Serializable]
    public class ReactivePropertyFloat
    {
        [SerializeField] private float _value;
        [SerializeField] private UnityEventFloat _onChanged = new();

        public ReactivePropertyFloat()
        {
        }

        public ReactivePropertyFloat(float initialValue)
        {
            _value = initialValue;
        }

        /// <summary>Current value (read-only).</summary>
        public float CurrentValue => _value;

        /// <summary>Value; setter invokes OnChanged.</summary>
        public float Value
        {
            get => _value;
            set
            {
                _value = value;
                _onChanged?.Invoke(_value);
            }
        }

        /// <summary>Change subscription (code and Inspector). AddListener / RemoveListener / RemoveAllListeners.</summary>
        public UnityEventFloat OnChanged => _onChanged;

        /// <summary>Subscribe to changes (wrapper for OnChanged.AddListener).</summary>
        public void AddListener(UnityAction<float> call)
        {
            _onChanged?.AddListener(call);
        }

        /// <summary>Unsubscribe (wrapper for OnChanged.RemoveListener).</summary>
        public void RemoveListener(UnityAction<float> call)
        {
            _onChanged?.RemoveListener(call);
        }

        /// <summary>Remove all listeners (wrapper for OnChanged.RemoveAllListeners).</summary>
        public void RemoveAllListeners()
        {
            _onChanged?.RemoveAllListeners();
        }

        /// <summary>Set value and notify subscribers.</summary>
        public void OnNext(float value)
        {
            Value = value;
        }

        /// <summary>Set value without invoking OnChanged (e.g. on load).</summary>
        public void SetValueWithoutNotify(float value)
        {
            _value = value;
        }

        /// <summary>Invoke OnChanged with the current value.</summary>
        public void ForceNotify()
        {
            _onChanged?.Invoke(_value);
        }
    }

    /// <summary>
    ///     Reactive variable (int): value + UnityEvent. R3-style API.
    /// </summary>
    [Serializable]
    public class ReactivePropertyInt
    {
        [SerializeField] private int _value;
        [SerializeField] private UnityEventInt _onChanged = new();

        public ReactivePropertyInt()
        {
        }

        public ReactivePropertyInt(int initialValue)
        {
            _value = initialValue;
        }

        /// <summary>Current value (read-only).</summary>
        public int CurrentValue => _value;

        /// <summary>Value; setter invokes OnChanged.</summary>
        public int Value
        {
            get => _value;
            set
            {
                _value = value;
                _onChanged?.Invoke(_value);
            }
        }

        /// <summary>Change subscription (code and Inspector). AddListener / RemoveListener / RemoveAllListeners.</summary>
        public UnityEventInt OnChanged => _onChanged;

        /// <summary>Subscribe to changes (wrapper for OnChanged.AddListener).</summary>
        public void AddListener(UnityAction<int> call)
        {
            _onChanged?.AddListener(call);
        }

        /// <summary>Unsubscribe (wrapper for OnChanged.RemoveListener).</summary>
        public void RemoveListener(UnityAction<int> call)
        {
            _onChanged?.RemoveListener(call);
        }

        /// <summary>Remove all listeners (wrapper for OnChanged.RemoveAllListeners).</summary>
        public void RemoveAllListeners()
        {
            _onChanged?.RemoveAllListeners();
        }

        /// <summary>Set value and notify subscribers.</summary>
        public void OnNext(int value)
        {
            Value = value;
        }

        /// <summary>Set value without invoking OnChanged (e.g. on load).</summary>
        public void SetValueWithoutNotify(int value)
        {
            _value = value;
        }

        /// <summary>Invoke OnChanged with the current value.</summary>
        public void ForceNotify()
        {
            _onChanged?.Invoke(_value);
        }
    }

    /// <summary>
    ///     Reactive variable (bool): value + UnityEvent. R3-style API. Defaults to false.
    /// </summary>
    [Serializable]
    public class ReactivePropertyBool
    {
        [SerializeField] private bool _value;
        [SerializeField] private UnityEventBool _onChanged = new();

        public ReactivePropertyBool()
        {
        }

        public ReactivePropertyBool(bool initialValue)
        {
            _value = initialValue;
        }

        /// <summary>Current value (read-only).</summary>
        public bool CurrentValue => _value;

        /// <summary>Value; setter invokes OnChanged.</summary>
        public bool Value
        {
            get => _value;
            set
            {
                _value = value;
                _onChanged?.Invoke(_value);
            }
        }

        /// <summary>Change subscription (code and Inspector). AddListener / RemoveListener / RemoveAllListeners.</summary>
        public UnityEventBool OnChanged => _onChanged;

        /// <summary>Subscribe to changes (wrapper for OnChanged.AddListener).</summary>
        public void AddListener(UnityAction<bool> call)
        {
            _onChanged?.AddListener(call);
        }

        /// <summary>Unsubscribe (wrapper for OnChanged.RemoveListener).</summary>
        public void RemoveListener(UnityAction<bool> call)
        {
            _onChanged?.RemoveListener(call);
        }

        /// <summary>Remove all listeners (wrapper for OnChanged.RemoveAllListeners).</summary>
        public void RemoveAllListeners()
        {
            _onChanged?.RemoveAllListeners();
        }

        /// <summary>Set value and notify subscribers.</summary>
        public void OnNext(bool value)
        {
            Value = value;
        }

        /// <summary>Set value without invoking OnChanged (e.g. on load).</summary>
        public void SetValueWithoutNotify(bool value)
        {
            _value = value;
        }

        /// <summary>Invoke OnChanged with the current value.</summary>
        public void ForceNotify()
        {
            _onChanged?.Invoke(_value);
        }
    }
}
