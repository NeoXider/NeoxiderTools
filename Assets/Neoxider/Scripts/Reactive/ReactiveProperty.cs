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
        [NonSerialized] private List<UnityAction<T>> _codeListeners;

        /// <summary>Reusable snapshot buffer for <see cref="NotifySubscribers"/> (no per-notify allocation).</summary>
        [NonSerialized] private UnityAction<T>[] _notifyBuffer;

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
                // Real snapshot into a reusable buffer: listeners added/removed during notification
                // do not shift indices, so every listener registered at notify time is invoked exactly
                // once (a live-list iteration would skip the next listener when an earlier one is removed).
                int count = _codeListeners.Count;
                if (_notifyBuffer == null || _notifyBuffer.Length < count)
                {
                    _notifyBuffer = new UnityAction<T>[Mathf.NextPowerOfTwo(count)];
                }

                _codeListeners.CopyTo(_notifyBuffer, 0);
                for (int i = 0; i < count; i++)
                {
                    UnityAction<T> listener = _notifyBuffer[i];
                    _notifyBuffer[i] = null;
                    try
                    {
                        listener?.Invoke(_value);
                    }
                    catch (Exception ex)
                    {
                        NeoDiagnostics.LogException(ex);
                    }
                }
            }

            try
            {
                _onChanged?.Invoke(_value);
            }
            catch (Exception ex)
            {
                NeoDiagnostics.LogException(ex);
            }
        }
    }

    /// <summary>
    ///     Code-first generic reactive variable for any C# value type or reference type.
    ///     For Inspector/UnityEvent serialization, prefer the concrete wrappers such as
    ///     <see cref="ReactivePropertyFloat"/>, <see cref="ReactivePropertyInt"/>, or
    ///     <see cref="ReactivePropertyBool"/>.
    /// </summary>
    [Serializable]
    public class ReactiveProperty<T> : ReactivePropertyBase<T, UnityEvent<T>>
    {
        public ReactiveProperty() { }
        public ReactiveProperty(T initialValue) : base(initialValue) { }
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
