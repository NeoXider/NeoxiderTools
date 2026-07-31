using System;
using System.Collections.Generic;

namespace Neo.Abilities
{
    /// <summary>
    ///     Typed gameplay event bus of one <see cref="AbilitySystem" />. Handlers subscribe globally
    ///     per event id; modifier event reactions are driven by the system itself.
    ///     Publish is re-entrancy safe (snapshot iteration) but not thread-safe by design.
    /// </summary>
    public sealed class AbilityEventBus
    {
        private readonly Dictionary<string, List<Action<AbilityEventArgs>>> _handlers =
            new Dictionary<string, List<Action<AbilityEventArgs>>>(StringComparer.OrdinalIgnoreCase);

        private readonly List<Action<AbilityEventArgs>> _anyHandlers = new List<Action<AbilityEventArgs>>();

        public void Subscribe(string eventId, Action<AbilityEventArgs> handler)
        {
            if (string.IsNullOrEmpty(eventId) || handler == null)
            {
                return;
            }

            if (!_handlers.TryGetValue(eventId, out List<Action<AbilityEventArgs>> list))
            {
                list = new List<Action<AbilityEventArgs>>();
                _handlers[eventId] = list;
            }

            list.Add(handler);
        }

        public void Unsubscribe(string eventId, Action<AbilityEventArgs> handler)
        {
            if (string.IsNullOrEmpty(eventId) || handler == null)
            {
                return;
            }

            if (_handlers.TryGetValue(eventId, out List<Action<AbilityEventArgs>> list))
            {
                list.Remove(handler);
            }
        }

        /// <summary>Subscribe to every event (receipt streams, logging, network replication).</summary>
        public void SubscribeAny(Action<AbilityEventArgs> handler)
        {
            if (handler != null)
            {
                _anyHandlers.Add(handler);
            }
        }

        public void UnsubscribeAny(Action<AbilityEventArgs> handler)
        {
            _anyHandlers.Remove(handler);
        }

        public void Publish(in AbilityEventArgs args)
        {
            if (_handlers.TryGetValue(args.EventId, out List<Action<AbilityEventArgs>> list) && list.Count > 0)
            {
                Action<AbilityEventArgs>[] snapshot = list.ToArray();
                for (int i = 0; i < snapshot.Length; i++)
                {
                    snapshot[i](args);
                }
            }

            if (_anyHandlers.Count > 0)
            {
                Action<AbilityEventArgs>[] snapshot = _anyHandlers.ToArray();
                for (int i = 0; i < snapshot.Length; i++)
                {
                    snapshot[i](args);
                }
            }
        }

        public void Clear()
        {
            _handlers.Clear();
            _anyHandlers.Clear();
        }
    }
}
