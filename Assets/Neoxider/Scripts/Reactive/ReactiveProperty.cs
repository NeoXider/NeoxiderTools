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

        /// <summary>
        ///     Code subscriptions (via <see cref="AddListener"/>). Invoked directly so notifications work in Edit Mode;
        ///     <see cref="UnityEvent{T}.Invoke"/> can skip runtime listeners outside Play Mode.
        /// </summary>
        [NonSerialized]
        private List<UnityAction<T>> _codeListeners;

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
                NotifySubscribers();
            }
        }

        /// <summary>Inspector / serialized UnityEvent subscribers.</summary>
        public TEvent OnChanged => _onChanged;

        /// <summary>Subscribe from code (not serialized). Prefer over <see cref="OnChanged"/>.AddListener in tooling/tests.</summary>
        public void AddListener(UnityAction<T> call)
        {
            if (call == null)
            {
                return;
            }

            _codeListeners ??= new List<UnityAction<T>>();
            for (int i = 0; i < _codeListeners.Count; i++)
            {
                if (_codeListeners[i] == call)
                {
                    return;
                }
            }

            _codeListeners.Add(call);
        }

        /// <summary>Unsubscribe (matches listeners from <see cref="AddListener"/> or <see cref="OnChanged"/>).</summary>
        public void RemoveListener(UnityAction<T> call)
        {
            _codeListeners?.Remove(call);
            _onChanged?.RemoveListener(call);
        }

        /// <summary>Clear code listeners and UnityEvent subscribers.</summary>
        public void RemoveAllListeners()
        {
            _codeListeners?.Clear();
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

        /// <summary>Invoke subscribers with the current value.</summary>
        public void ForceNotify()
        {
            NotifySubscribers();
        }

        private void NotifySubscribers()
        {
            if (_codeListeners is { Count: > 0 })
            {
                // Snapshot: prevents ConcurrentModification if a listener calls Add/RemoveListener
                int count = _codeListeners.Count;
                for (int i = 0; i < count; i++)
                {
                    // Guard: list may have shrunk if a listener removed itself
                    if (i >= _codeListeners.Count) break;
                    UnityAction<T> listener = _codeListeners[i];
                    try
                    {
                        listener?.Invoke(_value);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
            }

            try
            {
                _onChanged?.Invoke(_value);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
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
