using System;
using System.Collections.Generic;
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
    ///     Generic base for reactive properties. R3-style API.
    /// </summary>
    [Serializable]
    public abstract class ReactivePropertyBase<T, TEvent> where TEvent : UnityEvent<T>, new()
    {
        [SerializeField] protected T _value;
        [SerializeField] protected TEvent _onChanged = new();

        protected ReactivePropertyBase()
        {
        }

        protected ReactivePropertyBase(T initialValue)
        {
            _value = initialValue;
        }

        /// <summary>Current value (read-only).</summary>
        public T CurrentValue => _value;

        /// <summary>Value; setter invokes OnChanged.</summary>
        public virtual T Value
        {
            get => _value;
            set
            {
                if (EqualityComparer<T>.Default.Equals(_value, value))
                {
                    return;
                }

                _value = value;
                _onChanged?.Invoke(_value);
            }
        }

        /// <summary>Change subscription (code and Inspector). AddListener / RemoveListener / RemoveAllListeners.</summary>
        public TEvent OnChanged => _onChanged;

        /// <summary>Subscribe to changes (wrapper for OnChanged.AddListener).</summary>
        public void AddListener(UnityAction<T> call)
        {
            _onChanged?.AddListener(call);
        }

        /// <summary>Unsubscribe (wrapper for OnChanged.RemoveListener).</summary>
        public void RemoveListener(UnityAction<T> call)
        {
            _onChanged?.RemoveListener(call);
        }

        /// <summary>Remove all listeners (wrapper for OnChanged.RemoveAllListeners).</summary>
        public void RemoveAllListeners()
        {
            _onChanged?.RemoveAllListeners();
        }

        /// <summary>Set value and notify subscribers.</summary>
        public void OnNext(T value)
        {
            Value = value;
        }

        /// <summary>Set value without invoking OnChanged (e.g. on load).</summary>
        public void SetValueWithoutNotify(T value)
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
    ///     Reactive variable (float): value + UnityEvent. R3-style API.
    /// </summary>
    [Serializable]
    public class ReactivePropertyFloat : ReactivePropertyBase<float, UnityEventFloat>
    {
        public ReactivePropertyFloat() { }
        public ReactivePropertyFloat(float initialValue) : base(initialValue) { }
    }

    /// <summary>
    ///     Reactive variable (int): value + UnityEvent. R3-style API.
    /// </summary>
    [Serializable]
    public class ReactivePropertyInt : ReactivePropertyBase<int, UnityEventInt>
    {
        public ReactivePropertyInt() { }
        public ReactivePropertyInt(int initialValue) : base(initialValue) { }
    }

    /// <summary>
    ///     Reactive variable (bool): value + UnityEvent. R3-style API. Defaults to false.
    /// </summary>
    [Serializable]
    public class ReactivePropertyBool : ReactivePropertyBase<bool, UnityEventBool>
    {
        public ReactivePropertyBool() { }
        public ReactivePropertyBool(bool initialValue) : base(initialValue) { }
    }
}
