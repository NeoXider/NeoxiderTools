using System;
using System.Collections.Generic;

namespace Neo.Abilities
{
    /// <summary>
    ///     Typed gameplay event bus of one <see cref="AbilitySystem" />. Subscribe per event id,
    ///     to every event, or to events whose Target matches a unit. Modifier reactions are system-owned.
    ///     Publish is re-entrancy safe (snapshot iteration) but not thread-safe by design. Once the
    ///     snapshot buffers are warm, Publish allocates nothing.
    /// </summary>
    public sealed class AbilityEventBus
    {
        private readonly Dictionary<string, List<Action<AbilityEventArgs>>> _handlers =
            new Dictionary<string, List<Action<AbilityEventArgs>>>(AbilityIdComparer.Instance);

        private readonly List<ObserverRegistration> _anyHandlers = new List<ObserverRegistration>();

        private readonly Dictionary<UnitId, List<ObserverRegistration>> _targetHandlers =
            new Dictionary<UnitId, List<ObserverRegistration>>();

        // WHY: handlers publish nested events synchronously, so every nesting level owns its own
        // snapshot buffer; a single shared buffer would be overwritten while an outer batch iterates.
        private readonly List<List<Action<AbilityEventArgs>>> _handlerSnapshots =
            new List<List<Action<AbilityEventArgs>>>(4);

        private readonly List<List<ObserverRegistration>> _observerSnapshots =
            new List<List<ObserverRegistration>>(4);

        private readonly Action<AbilityEventArgs> _internalHandler;
        private ulong _nextObserverOrder;
        private int _publishDepth;

        /// <summary>Number of nonempty global/Target observer snapshots captured over this bus's lifetime.</summary>
        public long ObserverSnapshotCount { get; private set; }

        /// <summary>Total entries copied into those snapshots; excludes event-id-specific handler snapshots.</summary>
        public long ObserverSnapshotEntryCount { get; private set; }

        /// <summary>Global/Target observer calls attempted, including nested publications and throwing calls.</summary>
        public long ObserverInvocationCount { get; private set; }

        public AbilityEventBus()
        {
        }

        internal AbilityEventBus(Action<AbilityEventArgs> internalHandler)
        {
            _internalHandler = internalHandler;
        }

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
                _anyHandlers.Add(new ObserverRegistration(handler, _nextObserverOrder++));
            }
        }

        public void UnsubscribeAny(Action<AbilityEventArgs> handler)
        {
            RemoveObserver(_anyHandlers, handler);
        }

        /// <summary>
        ///     Subscribes to every event whose Target equals the supplied id, including None when requested.
        ///     Matching Target and global observers run in their common registration order.
        /// </summary>
        public void SubscribeTarget(UnitId target, Action<AbilityEventArgs> handler)
        {
            if (handler == null)
            {
                return;
            }

            if (!_targetHandlers.TryGetValue(target, out List<ObserverRegistration> list))
            {
                list = new List<ObserverRegistration>();
                _targetHandlers[target] = list;
            }

            list.Add(new ObserverRegistration(handler, _nextObserverOrder++));
        }

        /// <summary>Removes the first matching registration for this Target; captured batches stay intact.</summary>
        public void UnsubscribeTarget(UnitId target, Action<AbilityEventArgs> handler)
        {
            if (_targetHandlers.TryGetValue(target, out List<ObserverRegistration> list))
            {
                RemoveObserver(list, handler);
                if (list.Count == 0)
                {
                    _targetHandlers.Remove(target);
                }
            }
        }

        /// <summary>
        ///     Delivers <paramref name="args" /> to event-id handlers, then the system's modifier reactions,
        ///     then global and matching-Target observers. Each group iterates a snapshot, so subscription
        ///     changes made by a handler apply to later publications. Events without an id are ignored.
        /// </summary>
        public void Publish(in AbilityEventArgs args)
        {
            if (args.EventId == null)
            {
                return;
            }

            int depth = _publishDepth++;
            List<Action<AbilityEventArgs>> handlerSnapshot = RentSnapshot(_handlerSnapshots, depth);
            List<ObserverRegistration> observerSnapshot = RentSnapshot(_observerSnapshots, depth);
            try
            {
                if (_handlers.TryGetValue(args.EventId, out List<Action<AbilityEventArgs>> list) && list.Count > 0)
                {
                    // WHY: List.AddRange copies through a temporary array on some runtimes; index copy is alloc-free.
                    for (int i = 0; i < list.Count; i++)
                    {
                        handlerSnapshot.Add(list[i]);
                    }

                    for (int i = 0; i < handlerSnapshot.Count; i++)
                    {
                        handlerSnapshot[i](args);
                    }
                }

                // WHY: specific callbacks affect this batch; internal reactions cannot alter the captured batch.
                // Only global and matching-Target registrations are copied, in their common registration order.
                CaptureObservers(args.Target, observerSnapshot);
                _internalHandler?.Invoke(args);

                for (int i = 0; i < observerSnapshot.Count; i++)
                {
                    ObserverInvocationCount++;
                    observerSnapshot[i].Handler(args);
                }
            }
            finally
            {
                // WHY: cleared buffers must not keep unsubscribed handlers (and their targets) alive.
                handlerSnapshot.Clear();
                observerSnapshot.Clear();
                _publishDepth--;
            }
        }

        /// <summary>Clears user subscriptions, retaining the internal reaction route and lifetime counters.</summary>
        public void Clear()
        {
            _handlers.Clear();
            _anyHandlers.Clear();
            _targetHandlers.Clear();
        }

        private void CaptureObservers(UnitId target, List<ObserverRegistration> snapshot)
        {
            _targetHandlers.TryGetValue(target, out List<ObserverRegistration> matching);
            int matchingCount = matching?.Count ?? 0;
            int count = _anyHandlers.Count + matchingCount;
            if (count == 0)
            {
                return;
            }

            int globalIndex = 0;
            int matchingIndex = 0;
            for (int i = 0; i < count; i++)
            {
                if (matchingIndex >= matchingCount || globalIndex < _anyHandlers.Count &&
                    _anyHandlers[globalIndex].Order < matching[matchingIndex].Order)
                {
                    snapshot.Add(_anyHandlers[globalIndex++]);
                }
                else
                {
                    snapshot.Add(matching[matchingIndex++]);
                }
            }

            ObserverSnapshotCount++;
            ObserverSnapshotEntryCount += count;
        }

        private static List<T> RentSnapshot<T>(List<List<T>> pool, int depth)
        {
            while (pool.Count <= depth)
            {
                pool.Add(new List<T>(8));
            }

            return pool[depth];
        }

        private static void RemoveObserver(List<ObserverRegistration> list, Action<AbilityEventArgs> handler)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Handler == handler)
                {
                    list.RemoveAt(i);
                    return;
                }
            }
        }

        private sealed class ObserverRegistration
        {
            public readonly Action<AbilityEventArgs> Handler;
            public readonly ulong Order;

            public ObserverRegistration(Action<AbilityEventArgs> handler, ulong order)
            {
                Handler = handler;
                Order = order;
            }
        }
    }
}
