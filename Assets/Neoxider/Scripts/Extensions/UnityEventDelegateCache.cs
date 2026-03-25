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

        /// <summary>Number of cached delegates.</summary>
        public int Count => _delegates.Count;

        /// <summary>Returns the delegate at index for RemoveListener.</summary>
        public UnityAction this[int index] => _delegates[index];

        /// <summary>Adds a delegate to the cache; pass it to AddListener, then remove via UnsubscribeAt.</summary>
        public void Add(UnityAction action)
        {
            _delegates.Add(action);
        }

        /// <summary>Subscribes a delegate by index and stores it in the cache (grows cache if needed).</summary>
        /// <param name="index">Slot index (0..N-1).</param>
        /// <param name="evt">Event (e.g. button.onClick).</param>
        /// <param name="action">Delegate (e.g. () => Handler(index)).</param>
        public void SubscribeAt(int index, UnityEvent evt, UnityAction action)
        {
            while (_delegates.Count <= index)
            {
                _delegates.Add(null);
            }

            _delegates[index] = action;
            evt?.AddListener(action);
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
            }
        }

        /// <summary>Clears the cache. Does not remove listeners from events — call UnsubscribeAt for each index before Clear.</summary>
        public void Clear()
        {
            _delegates.Clear();
        }
    }
}
