using System;
using Neo.Reactive;
using UnityEngine;

namespace Neo.Network
{
    /// <summary>
    ///     Bridge between Mirror's [SyncVar(hook)] pattern and <see cref="ReactivePropertyBase{T,TEvent}"/>.
    ///     <para>
    ///         In multiplayer, the owning <c>NetworkBehaviour</c> declares a <c>[SyncVar(hook = nameof(OnXChanged))]</c>
    ///         field.  The hook method calls <see cref="SetFromNetwork"/> to propagate the server-authoritative
    ///         value into the local <see cref="ReactivePropertyBase{T,TEvent}"/>, triggering UI and
    ///         inspector bindings without an extra subscription layer.
    ///     </para>
    ///     <para>
    ///         In solo mode this class is unused — <see cref="ReactivePropertyBase{T,TEvent}"/> works
    ///         unchanged as it always has.
    ///     </para>
    /// </summary>
    /// <example>
    ///     <code>
    ///     // Inside a NetworkBehaviour (with MIRROR define):
    ///     [SyncVar(hook = nameof(OnHpChanged))]
    ///     private float _syncHp = 100f;
    ///
    ///     public ReactivePropertyFloat HpState = new(100f);
    ///
    ///     // SyncVar hook — called on every client when value changes
    ///     private void OnHpChanged(float oldValue, float newValue)
    ///     {
    ///         NetworkReactivePropertyBridge.SetFromNetwork(HpState, newValue);
    ///     }
    ///
    ///     // Server-side mutation:
    ///     [Server]
    ///     public void ServerSetHp(float value)
    ///     {
    ///         _syncHp = value;                        // triggers SyncVar → hook → reactive
    ///         HpState.SetValueWithoutNotify(value);   // local update on the server itself
    ///         HpState.ForceNotify();
    ///     }
    ///     </code>
    /// </example>
    public static class NetworkReactivePropertyBridge
    {
        /// <summary>
        ///     Pushes a network-received value into a <see cref="ReactivePropertyFloat"/>.
        /// </summary>
        public static void SetFromNetwork(ReactivePropertyFloat property, float newValue)
        {
            if (property == null) return;
            property.SetValueWithoutNotify(newValue);
            property.ForceNotify();
        }

        /// <summary>
        ///     Pushes a network-received value into a <see cref="ReactivePropertyInt"/>.
        /// </summary>
        public static void SetFromNetwork(ReactivePropertyInt property, int newValue)
        {
            if (property == null) return;
            property.SetValueWithoutNotify(newValue);
            property.ForceNotify();
        }

        /// <summary>
        ///     Pushes a network-received value into a <see cref="ReactivePropertyBool"/>.
        /// </summary>
        public static void SetFromNetwork(ReactivePropertyBool property, bool newValue)
        {
            if (property == null) return;
            property.SetValueWithoutNotify(newValue);
            property.ForceNotify();
        }
    }
}
