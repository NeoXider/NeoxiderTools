using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace Neo.Extensions
{
    /// <summary>
    ///     Delegate cache for correct UnityEvent unsubscription: keeps the same delegate instances
    ///     passed to AddListener so RemoveListener uses the same reference.
    ///     Used for dynamic subscriptions by index (buttons, list items).
    /// </summary>
    public class UnityEventDelegateCache
    {
        private readonly List<UnityAction> _delegates = new();
        private readonly List<UnityEvent> _events = new();

        /// <summary>Number of cached delegates.</summary>
        public int Count => _delegates.Count;

        /// <summary>Returns the delegate at index for RemoveListener.</summary>
        public UnityAction this[int index] => _delegates[index];

        /// <summary>Adds a delegate to the cache; pass it to AddListener, then remove via UnsubscribeAt.</summary>
        public void Add(UnityAction action)
        {
            _delegates.Add(action);
            _events.Add(null);
        }

        /// <summary>Replaces the subscription at an index, detaching its previous event before storing the new one.</summary>
        /// <param name="index">Slot index (0..N-1).</param>
        /// <param name="evt">Event (e.g. button.onClick).</param>
        /// <param name="action">Delegate (e.g. () => Handler(index)).</param>
        public void SubscribeAt(int index, UnityEvent evt, UnityAction action)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            while (_delegates.Count <= index)
            {
                _delegates.Add(null);
                _events.Add(null);
            }

            UnityAction previous = _delegates[index];
            if (previous != null)
            {
                _events[index]?.RemoveListener(previous);
            }

            _delegates[index] = action;
            _events[index] = evt;
            if (action != null)
            {
                evt?.AddListener(action);
            }
        }

        /// <summary>Unsubscribes the delegate stored at the given index.</summary>
        public void UnsubscribeAt(int index, UnityEvent evt)
        {
            if (evt == null || index < 0 || index >= _delegates.Count)
            {
                return;
            }

            UnityAction a = _delegates[index];
            if (a != null)
            {
                evt.RemoveListener(a);
                if (_events[index] == null || ReferenceEquals(_events[index], evt))
                {
                    // Keep a tracked subscription recoverable if the caller supplied a different event.
                    _delegates[index] = null;
                    _events[index] = null;
                }
            }
        }

        /// <summary>Clears the cache. Does not remove listeners from events — call UnsubscribeAt for each index before Clear.</summary>
        public void Clear()
        {
            _delegates.Clear();
            _events.Clear();
        }
    }
}
