using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Events;

namespace Neo.Reactive
{
    /// <summary>Liveness of a single reactive subscription.</summary>
    internal enum ReactiveSubscriptionState
    {
        Alive = 0,

        /// <summary>Weak target was garbage collected - the expected end of a weak subscription.</summary>
        Collected = 1,

        /// <summary>Target is a <see cref="UnityEngine.Object" /> that was destroyed without unsubscribing.</summary>
        Destroyed = 2
    }

    /// <summary>
    ///     Caches one open-instance invoker per listener method so weak dispatch stays a plain delegate call
    ///     instead of <see cref="MethodBase.Invoke(object,object[])" />.
    /// </summary>
    internal static class ReactiveWeakInvokerCache<TValue>
    {
        private static readonly Dictionary<MethodInfo, Action<object, TValue>> Invokers = new();
        private static readonly object Gate = new();

        public static Action<object, TValue> Get(MethodInfo method)
        {
            if (method == null || method.IsStatic)
            {
                return null;
            }

            lock (Gate)
            {
                if (Invokers.TryGetValue(method, out Action<object, TValue> cached))
                {
                    return cached;
                }

                Action<object, TValue> invoker = Build(method);
                // WHY: negative results are cached too - a failed build must not retry reflection on every subscribe.
                Invokers[method] = invoker;
                return invoker;
            }
        }

        internal static void ClearForTests()
        {
            lock (Gate)
            {
                Invokers.Clear();
            }
        }

        private static Action<object, TValue> Build(MethodInfo method)
        {
            Type declaringType = method.DeclaringType;
            if (declaringType == null || declaringType.IsValueType)
            {
                return null;
            }

            try
            {
                Type factory = typeof(ReactiveOpenInvokerFactory<,>).MakeGenericType(declaringType, typeof(TValue));
                MethodInfo create = factory.GetMethod("Create", BindingFlags.Public | BindingFlags.Static);
                return create?.Invoke(null, new object[] { method }) as Action<object, TValue>;
            }
            catch (Exception)
            {
                // WHY: AOT platforms can refuse a generic instantiation that was never compiled. The caller degrades
                // to a strong subscription instead of losing the listener.
                return null;
            }
        }
    }

    /// <summary>
    ///     Builds an <c>Action&lt;object, TValue&gt;</c> that calls an instance method without capturing its target,
    ///     which is what lets a weak subscription stay collectable.
    /// </summary>
    internal static class ReactiveOpenInvokerFactory<TTarget, TValue> where TTarget : class
    {
        public static Action<object, TValue> Create(MethodInfo method)
        {
            Delegate open = Delegate.CreateDelegate(typeof(Action<TTarget, TValue>), null, method, false);
            if (open is Action<TTarget, TValue> typed)
            {
                return (target, value) => typed((TTarget)target, value);
            }

            return null;
        }
    }

    /// <summary>
    ///     How a reactive property holds a code subscription.
    /// </summary>
    public enum ReactiveListenerMode
    {
        /// <summary>
        ///     Default. The property keeps a normal reference to the delegate, so the subscriber stays alive as long as
        ///     the property does. Subscriptions whose target is an already destroyed <see cref="UnityEngine.Object" />
        ///     are still dropped automatically - a destroyed object can never be a valid listener.
        /// </summary>
        Strong = 0,

        /// <summary>
        ///     The property keeps only a weak reference to the delegate target, so a forgotten
        ///     <see cref="IReactiveProperty{T}.RemoveListener" /> cannot leak the subscriber. The subscription drops
        ///     itself once the target is collected.
        ///     WHY: only pass delegates whose target you keep alive yourself (a method group on a field/component).
        ///     A closure that captures locals has no other owner and may be collected immediately.
        /// </summary>
        Weak = 1
    }

    /// <summary>
    ///     Read side of a reactive property: current value plus subscription management.
    ///     Accept this instead of a concrete <c>ReactiveProperty*</c> when a type only observes a value.
    /// </summary>
    public interface IReadOnlyReactiveProperty<T>
    {
        /// <summary>Current value without notifying anyone.</summary>
        T CurrentValue { get; }

        /// <summary>Number of live code subscriptions (dead ones are pruned lazily).</summary>
        int ListenerCount { get; }

        /// <summary>Subscribe and get a handle that unsubscribes on <see cref="IDisposable.Dispose" />.</summary>
        IDisposable Subscribe(UnityAction<T> onNext, bool invokeImmediately = false);

        /// <summary>Subscribe weakly: the subscription drops itself once the delegate target is collected.</summary>
        IDisposable SubscribeWeak(UnityAction<T> onNext, bool invokeImmediately = false);

        /// <summary>Subscribe from code. Prefer <see cref="Subscribe" /> when you can hold the returned handle.</summary>
        void AddListener(UnityAction<T> call);

        /// <summary>Unsubscribe a delegate added through <see cref="AddListener" /> or the serialized event.</summary>
        void RemoveListener(UnityAction<T> call);
    }

    /// <summary>
    ///     Read/write side of a reactive property.
    /// </summary>
    public interface IReactiveProperty<T> : IReadOnlyReactiveProperty<T>
    {
        /// <summary>Value; the setter notifies subscribers when the value actually changes.</summary>
        T Value { get; set; }

        /// <summary>Set the value without notifying anyone (for example while loading a save).</summary>
        void SetValueWithoutNotify(T value);

        /// <summary>Notify every subscriber with the current value, even if it did not change.</summary>
        void ForceNotify();
    }
}
