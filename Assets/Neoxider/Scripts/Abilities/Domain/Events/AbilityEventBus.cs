using System;
using System.Collections.Generic;

namespace Neo.Abilities
{
    /// <summary>
    ///     Typed gameplay event bus of one <see cref="AbilitySystem" />. Subscribe per event id,
    ///     to every event, or to events whose Target matches a unit. Modifier reactions are system-owned.
    ///     Publish is re-entrancy safe (snapshot iteration) but not thread-safe by design.
    /// </summary>
    public sealed class AbilityEventBus
    {
        private readonly Dictionary<string, List<Action<AbilityEventArgs>>> _handlers =
            new Dictionary<string, List<Action<AbilityEventArgs>>>(StringComparer.OrdinalIgnoreCase);

        private readonly List<ObserverRegistration> _anyHandlers = new List<ObserverRegistration>();

        private readonly Dictionary<UnitId, List<ObserverRegistration>> _targetHandlers =
            new Dictionary<UnitId, List<ObserverRegistration>>();

        private readonly Action<AbilityEventArgs> _internalHandler;
        private ulong _nextObserverOrder;

        /// <summary>Number of nonempty global/Target observer arrays captured over this bus's lifetime.</summary>
        public long ObserverSnapshotCount { get; private set; }

        /// <summary>Total entries copied into those arrays; excludes event-id-specific handler snapshots.</summary>
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

            // Specific callbacks affect this batch; internal reactions cannot alter the captured batch.
            // Only global and matching-Target registrations are copied, in their common registration order.
            ObserverRegistration[] observerSnapshot = CaptureObservers(args.Target);
            _internalHandler?.Invoke(args);

            if (observerSnapshot != null)
            {
                for (int i = 0; i < observerSnapshot.Length; i++)
                {
                    ObserverInvocationCount++;
                    observerSnapshot[i].Handler(args);
                }
            }
        }

        /// <summary>Clears user subscriptions, retaining the internal reaction route and lifetime counters.</summary>
        public void Clear()
        {
            _handlers.Clear();
            _anyHandlers.Clear();
            _targetHandlers.Clear();
        }

        private ObserverRegistration[] CaptureObservers(UnitId target)
        {
            _targetHandlers.TryGetValue(target, out List<ObserverRegistration> matching);
            int matchingCount = matching?.Count ?? 0;
            int count = _anyHandlers.Count + matchingCount;
            if (count == 0)
            {
                return null;
            }

            ObserverRegistration[] snapshot = new ObserverRegistration[count];
            int globalIndex = 0;
            int matchingIndex = 0;
            for (int i = 0; i < count; i++)
            {
                if (matchingIndex >= matchingCount || globalIndex < _anyHandlers.Count &&
                    _anyHandlers[globalIndex].Order < matching[matchingIndex].Order)
                {
                    snapshot[i] = _anyHandlers[globalIndex++];
                }
                else
                {
                    snapshot[i] = matching[matchingIndex++];
                }
            }

            ObserverSnapshotCount++;
            ObserverSnapshotEntryCount += count;
            return snapshot;
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
