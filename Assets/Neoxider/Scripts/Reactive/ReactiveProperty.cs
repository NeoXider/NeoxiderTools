using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

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
    ///     Notification guarantees:
    ///     every listener registered when a notification starts is invoked exactly once with the value captured at that
    ///     moment, even if listeners are added, removed, or set the value again from inside the callback;
    ///     a listener that throws is logged and skipped without affecting the remaining listeners;
    ///     a subscription whose target is gone (destroyed <see cref="Object" />, or collected under
    ///     <see cref="ReactiveListenerMode.Weak" />) is reported once and dropped instead of throwing.
    /// </summary>
    [Serializable]
    public abstract class ReactivePropertyBase<T, TEvent> : IReactiveProperty<T> where TEvent : UnityEvent<T>, new()
    {
        [SerializeField] protected T _value;
        [SerializeField] protected TEvent _onChanged = new();

        /// <summary>
        ///     Code subscriptions (via <see cref="AddListener(UnityAction{T})" />). Invoked directly so notifications work
        ///     in Edit Mode; <see cref="UnityEvent{T}.Invoke" /> can skip runtime listeners outside Play Mode.
        /// </summary>
        [NonSerialized] private List<Subscription> _listeners;

        /// <summary>Reusable snapshot buffer for <see cref="NotifySubscribers" /> (no per-notify allocation).</summary>
        [NonSerialized] private Subscription[] _notifyBuffer;

        /// <summary>Re-entrancy depth. The shared buffer belongs to the outermost notification only.</summary>
        [NonSerialized] private int _notifyDepth;

        /// <summary>Set while a notification found a dead subscription that must be compacted away afterwards.</summary>
        [NonSerialized] private bool _hasDeadListeners;

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

        /// <summary>Number of live code subscriptions. Dead subscriptions are dropped before counting.</summary>
        public int ListenerCount
        {
            get
            {
                CompactDeadListeners();
                return _listeners?.Count ?? 0;
            }
        }

        /// <summary>Subscribe from code (not serialized). Prefer over <see cref="OnChanged" />.AddListener in tooling/tests.</summary>
        public void AddListener(UnityAction<T> call)
        {
            AddListener(call, ReactiveListenerMode.Strong);
        }

        /// <summary>Subscribe from code with an explicit lifetime mode.</summary>
        public void AddListener(UnityAction<T> call, ReactiveListenerMode mode)
        {
            AddSubscription(call, mode);
        }

        /// <summary>
        ///     Subscribe weakly: the property does not keep the delegate target alive, so a forgotten
        ///     <see cref="RemoveListener" /> cannot leak it. Read <see cref="ReactiveListenerMode.Weak" /> first.
        /// </summary>
        public void AddWeakListener(UnityAction<T> call)
        {
            AddListener(call, ReactiveListenerMode.Weak);
        }

        /// <summary>Subscribe and get a handle that unsubscribes on <see cref="IDisposable.Dispose" />.</summary>
        public IDisposable Subscribe(UnityAction<T> onNext, bool invokeImmediately = false)
        {
            return SubscribeInternal(onNext, ReactiveListenerMode.Strong, invokeImmediately);
        }

        /// <summary>Weak counterpart of <see cref="Subscribe" />.</summary>
        public IDisposable SubscribeWeak(UnityAction<T> onNext, bool invokeImmediately = false)
        {
            return SubscribeInternal(onNext, ReactiveListenerMode.Weak, invokeImmediately);
        }

        /// <summary>
        ///     Allocation-free weak subscription: <paramref name="handler" /> must be a static delegate (a lambda that
        ///     captures nothing) receiving <paramref name="target" /> back. Unlike <see cref="SubscribeWeak(UnityAction{T},bool)" />
        ///     this needs no reflection, so it is safe on every IL2CPP/AOT platform.
        ///     Usage: <c>hp.SubscribeWeak(this, static (self, value) =&gt; self.OnHpChanged(value));</c>
        /// </summary>
        public IDisposable SubscribeWeak<TTarget>(TTarget target, Action<TTarget, T> handler,
            bool invokeImmediately = false) where TTarget : class
        {
            if (target == null || handler == null)
            {
                return EmptyDisposable.Instance;
            }

            Subscription subscription = Subscription.CreateWeak(
                new WeakReference(target),
                handler.Method,
                (boxedTarget, value) => handler((TTarget)boxedTarget, value));

            Register(subscription);
            if (invokeImmediately)
            {
                handler(target, _value);
            }

            return new SubscriptionHandle(this, subscription);
        }

        /// <summary>Unsubscribe (matches listeners from <see cref="AddListener(UnityAction{T})" /> or <see cref="OnChanged" />).</summary>
        public void RemoveListener(UnityAction<T> call)
        {
            if (call != null && _listeners != null)
            {
                for (int i = _listeners.Count - 1; i >= 0; i--)
                {
                    if (_listeners[i].Matches(call))
                    {
                        _listeners.RemoveAt(i);
                    }
                }
            }

            _onChanged?.RemoveListener(call);
        }

        /// <summary>Clear code listeners and UnityEvent subscribers.</summary>
        public void RemoveAllListeners()
        {
            _listeners?.Clear();
            _hasDeadListeners = false;
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

        private IDisposable SubscribeInternal(UnityAction<T> onNext, ReactiveListenerMode mode, bool invokeImmediately)
        {
            Subscription subscription = AddSubscription(onNext, mode);
            if (subscription == null)
            {
                return EmptyDisposable.Instance;
            }

            if (invokeImmediately)
            {
                InvokeSafe(subscription, _value);
            }

            return new SubscriptionHandle(this, subscription);
        }

        private Subscription AddSubscription(UnityAction<T> call, ReactiveListenerMode mode)
        {
            if (call == null)
            {
                return null;
            }

            _listeners ??= new List<Subscription>();
            for (int i = 0; i < _listeners.Count; i++)
            {
                if (_listeners[i].Matches(call))
                {
                    return _listeners[i];
                }
            }

            Subscription subscription = CreateSubscription(call, mode);
            _listeners.Add(subscription);
            return subscription;
        }

        private static Subscription CreateSubscription(UnityAction<T> call, ReactiveListenerMode mode)
        {
            if (mode != ReactiveListenerMode.Weak || call.Target == null)
            {
                // WHY: a static delegate has no target to keep alive, so weak mode would only add overhead.
                return Subscription.CreateStrong(call);
            }

            Action<object, T> invoker = ReactiveWeakInvokerCache<T>.Get(call.Method);
            if (invoker == null)
            {
                // WHY: open-delegate creation can fail on AOT platforms. Degrade to a strong subscription
                // (still pruned when the target is a destroyed Object) instead of dropping the listener.
                NeoDiagnostics.LogWarningThrottled(
                    "Neo.Reactive.WeakUnavailable." + DescribeMethod(call.Method),
                    $"[ReactiveProperty] Weak subscription for '{DescribeMethod(call.Method)}' is not supported on this " +
                    "platform; falling back to a strong subscription. Remove the listener explicitly.");
                return Subscription.CreateStrong(call);
            }

            return Subscription.CreateWeak(new WeakReference(call.Target), call.Method, invoker);
        }

        private void Register(Subscription subscription)
        {
            _listeners ??= new List<Subscription>();
            _listeners.Add(subscription);
        }

        internal void RemoveSubscription(Subscription subscription)
        {
            if (subscription != null)
            {
                _listeners?.Remove(subscription);
            }
        }

        private void NotifySubscribers()
        {
            // WHY: the value is captured up front so every listener of this notification sees the same value,
            // even when one of them assigns Value again and starts a nested notification.
            T value = _value;
            int count = _listeners?.Count ?? 0;
            if (count > 0)
            {
                // WHY: real snapshot - listeners added/removed during notification do not shift indices, so every
                // listener registered at notify time is invoked exactly once. The shared buffer belongs to the
                // outermost notification; nested ones allocate, which keeps re-entrancy correct without a pool.
                bool useSharedBuffer = _notifyDepth == 0;
                Subscription[] buffer;
                if (useSharedBuffer)
                {
                    if (_notifyBuffer == null || _notifyBuffer.Length < count)
                    {
                        _notifyBuffer = new Subscription[Mathf.NextPowerOfTwo(count)];
                    }

                    buffer = _notifyBuffer;
                }
                else
                {
                    buffer = new Subscription[count];
                }

                _listeners.CopyTo(buffer, 0);
                _notifyDepth++;
                try
                {
                    for (int i = 0; i < count; i++)
                    {
                        InvokeSafe(buffer[i], value);
                    }
                }
                finally
                {
                    _notifyDepth--;
                    // WHY: clear the shared buffer so it never keeps a removed subscriber alive until the next notify.
                    Array.Clear(buffer, 0, count);
                }
            }

            try
            {
                _onChanged?.Invoke(value);
            }
            catch (Exception ex)
            {
                NeoDiagnostics.LogException(ex);
            }

            if (_notifyDepth == 0)
            {
                CompactDeadListeners();
            }
        }

        private void InvokeSafe(Subscription subscription, T value)
        {
            if (subscription == null)
            {
                return;
            }

            switch (subscription.GetState())
            {
                case ReactiveSubscriptionState.Alive:
                    break;

                case ReactiveSubscriptionState.Collected:
                    // WHY: a weak subscriber that was collected is the expected end of its life - drop it quietly.
                    _hasDeadListeners = true;
                    return;

                default:
                    _hasDeadListeners = true;
                    NeoDiagnostics.LogWarningThrottled(
                        "Neo.Reactive.DestroyedListener." + subscription.Describe(),
                        $"[ReactiveProperty] Listener '{subscription.Describe()}' belongs to a destroyed object and was " +
                        "unsubscribed automatically. Call RemoveListener in OnDestroy/OnDisable, or subscribe with " +
                        "ReactiveListenerMode.Weak.");
                    return;
            }

            try
            {
                subscription.Invoke(value);
            }
            catch (Exception ex)
            {
                // WHY: one broken listener must not stop the remaining ones or the gameplay code that set the value.
                NeoDiagnostics.LogException(ex);
            }
        }

        private void CompactDeadListeners()
        {
            if (!_hasDeadListeners || _listeners == null)
            {
                return;
            }

            _hasDeadListeners = false;
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                if (_listeners[i].GetState() != ReactiveSubscriptionState.Alive)
                {
                    _listeners.RemoveAt(i);
                }
            }
        }

        private static string DescribeMethod(MethodInfo method)
        {
            if (method == null)
            {
                return "<unknown>";
            }

            string owner = method.DeclaringType != null ? method.DeclaringType.Name : "<unknown>";
            return owner + "." + method.Name;
        }

        /// <summary>One code subscription: either a normal delegate or a weak target plus an open-instance invoker.</summary>
        internal sealed class Subscription
        {
            private UnityAction<T> _strong;
            private Action<object, T> _weakInvoker;
            private MethodInfo _method;
            private WeakReference _weakTarget;

            public static Subscription CreateStrong(UnityAction<T> call)
            {
                return new Subscription { _strong = call, _method = call.Method };
            }

            public static Subscription CreateWeak(WeakReference target, MethodInfo method, Action<object, T> invoker)
            {
                return new Subscription { _weakTarget = target, _method = method, _weakInvoker = invoker };
            }

            public ReactiveSubscriptionState GetState()
            {
                if (_weakTarget != null)
                {
                    object target = _weakTarget.Target;
                    if (target == null)
                    {
                        return ReactiveSubscriptionState.Collected;
                    }

                    return IsDestroyedUnityObject(target) ? ReactiveSubscriptionState.Destroyed : ReactiveSubscriptionState.Alive;
                }

                if (_strong == null)
                {
                    return ReactiveSubscriptionState.Collected;
                }

                return IsDestroyedUnityObject(_strong.Target) ? ReactiveSubscriptionState.Destroyed : ReactiveSubscriptionState.Alive;
            }

            public void Invoke(T value)
            {
                if (_weakInvoker != null)
                {
                    object target = _weakTarget?.Target;
                    if (target != null)
                    {
                        _weakInvoker(target, value);
                    }

                    return;
                }

                _strong?.Invoke(value);
            }

            public bool Matches(UnityAction<T> call)
            {
                if (call == null)
                {
                    return false;
                }

                if (_strong != null)
                {
                    return _strong == call;
                }

                return _method == call.Method && ReferenceEquals(_weakTarget?.Target, call.Target);
            }

            public string Describe()
            {
                return DescribeMethod(_method);
            }

            private static bool IsDestroyedUnityObject(object target)
            {
                // WHY: a destroyed Object compares equal to null through Unity's overloaded operator while the managed
                // shell is still referenced, which is exactly the "forgot to unsubscribe in OnDestroy" leak.
                return target is Object unityObject && unityObject == null;
            }
        }

        private sealed class SubscriptionHandle : IDisposable
        {
            private ReactivePropertyBase<T, TEvent> _owner;
            private Subscription _subscription;

            public SubscriptionHandle(ReactivePropertyBase<T, TEvent> owner, Subscription subscription)
            {
                _owner = owner;
                _subscription = subscription;
            }

            public void Dispose()
            {
                _owner?.RemoveSubscription(_subscription);
                _owner = null;
                _subscription = null;
            }
        }

        private sealed class EmptyDisposable : IDisposable
        {
            public static readonly EmptyDisposable Instance = new();

            public void Dispose()
            {
            }
        }
    }

    /// <summary>
    ///     Code-first generic reactive variable for any C# value type or reference type.
    ///     For Inspector/UnityEvent serialization, prefer the concrete wrappers such as
    ///     <see cref="ReactivePropertyFloat" />, <see cref="ReactivePropertyInt" />, or
    ///     <see cref="ReactivePropertyBool" />.
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
