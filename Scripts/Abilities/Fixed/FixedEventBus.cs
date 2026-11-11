using System.Collections.Generic;

namespace Neo.Abilities.Fixed
{
    /// <summary>One receipt of the fixed core; the same fields as the float AbilityEventArgs with a raw amount.</summary>
    public readonly struct FixedEventArgs
    {
        public readonly string EventId;
        public readonly int Target;
        public readonly int Source;
        public readonly int AmountRaw;
        public readonly string AbilityId;
        public readonly string ModifierId;
        public readonly string DamageType;
        public readonly uint CastId;
        public readonly string Tag;

        public FixedEventArgs(string eventId, int target, int source, int amountRaw = 0, string abilityId = null,
            string modifierId = null, string damageType = null, uint castId = 0u, string tag = null)
        {
            EventId = eventId;
            Target = target;
            Source = source;
            AmountRaw = amountRaw;
            AbilityId = abilityId;
            ModifierId = modifierId;
            DamageType = damageType;
            CastId = castId;
            Tag = tag;
        }
    }

    /// <summary>Handler of fixed receipts; the arguments are passed by reference and never boxed.</summary>
    public delegate void FixedEventHandler(in FixedEventArgs args);

    internal interface IFixedEventInterceptor
    {
        void React(in FixedEventArgs args);
    }

    /// <summary>
    ///     Synchronous receipt stream of a fixed system. Order per publish: handlers of that event id (subscription
    ///     order), then modifier reactions, then handlers of every event. Nested publishes keep their own snapshot,
    ///     so handlers may publish again; publishing allocates nothing.
    /// </summary>
    public sealed class FixedEventBus
    {
        private readonly Dictionary<string, List<FixedEventHandler>> _handlers =
            new Dictionary<string, List<FixedEventHandler>>(FixedIdComparer.Instance);

        private readonly List<FixedEventHandler> _anyHandlers = new List<FixedEventHandler>();
        private readonly List<List<FixedEventHandler>> _snapshots = new List<List<FixedEventHandler>>(4);
        private readonly IFixedEventInterceptor _interceptor;
        private int _depth;

        public FixedEventBus()
        {
        }

        internal FixedEventBus(IFixedEventInterceptor interceptor)
        {
            _interceptor = interceptor;
        }

        /// <summary>True when a handler is subscribed to this event id (optional receipts are published only then).</summary>
        public bool HasHandlers(string eventId)
        {
            return _handlers.Count > 0 && !string.IsNullOrEmpty(eventId) &&
                   _handlers.TryGetValue(eventId, out List<FixedEventHandler> list) && list.Count > 0;
        }

        public void Subscribe(string eventId, FixedEventHandler handler)
        {
            if (string.IsNullOrEmpty(eventId) || handler == null)
            {
                return;
            }

            if (!_handlers.TryGetValue(eventId, out List<FixedEventHandler> list))
            {
                list = new List<FixedEventHandler>(2);
                _handlers[eventId] = list;
            }

            list.Add(handler);
        }

        public void Unsubscribe(string eventId, FixedEventHandler handler)
        {
            if (!string.IsNullOrEmpty(eventId) && handler != null &&
                _handlers.TryGetValue(eventId, out List<FixedEventHandler> list))
            {
                list.Remove(handler);
            }
        }

        /// <summary>Receives every published event after the reactions.</summary>
        public void SubscribeAny(FixedEventHandler handler)
        {
            if (handler != null)
            {
                _anyHandlers.Add(handler);
            }
        }

        public void UnsubscribeAny(FixedEventHandler handler)
        {
            _anyHandlers.Remove(handler);
        }

        public void Clear()
        {
            _handlers.Clear();
            _anyHandlers.Clear();
        }

        public void Publish(in FixedEventArgs args)
        {
            if (args.EventId == null)
            {
                return;
            }

            while (_snapshots.Count <= _depth)
            {
                _snapshots.Add(new List<FixedEventHandler>(8));
            }

            List<FixedEventHandler> snapshot = _snapshots[_depth];
            _depth++;
            try
            {
                if (_handlers.Count > 0 && _handlers.TryGetValue(args.EventId, out List<FixedEventHandler> list) &&
                    list.Count > 0)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        snapshot.Add(list[i]);
                    }

                    for (int i = 0; i < snapshot.Count; i++)
                    {
                        snapshot[i](in args);
                    }

                    snapshot.Clear();
                }

                _interceptor?.React(in args);
                if (_anyHandlers.Count > 0)
                {
                    for (int i = 0; i < _anyHandlers.Count; i++)
                    {
                        snapshot.Add(_anyHandlers[i]);
                    }

                    for (int i = 0; i < snapshot.Count; i++)
                    {
                        snapshot[i](in args);
                    }
                }
            }
            finally
            {
                snapshot.Clear();
                _depth--;
            }
        }
    }
}
