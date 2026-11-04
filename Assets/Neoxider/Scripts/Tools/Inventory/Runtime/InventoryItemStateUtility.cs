using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Neo.Tools
{
    /// <summary>
    ///     Static helper: walks a prefab hierarchy, calls <see cref="IInventoryItemState.CaptureInventoryState" /> or <see cref="IInventoryItemState.RestoreInventoryState" /> on every implementation.
    /// </summary>
    /// <remarks>
    ///     Used by pickup/drop flows so you do not call capture/restore manually per component. Implement state on
    ///     <see cref="InventoryItemStateBehaviour" /> (or <see cref="IInventoryItemState" />); this class aggregates payloads into <see cref="InventoryItemInstance.ComponentStates" />.
    /// </remarks>
    public static class InventoryItemStateUtility
    {
        /// <summary>True if any child <see cref="MonoBehaviour" /> implements <see cref="IInventoryItemState" />.</summary>
        public static bool HasState(GameObject root)
        {
            if (root == null)
            {
                return false;
            }

            List<IInventoryItemState> states = ListPool<IInventoryItemState>.Get();
            try
            {
                root.GetComponentsInChildren(true, states);
                return states.Count > 0;
            }
            finally
            {
                ListPool<IInventoryItemState>.Release(states);
            }
        }

        /// <summary>
        ///     Builds an <see cref="InventoryItemInstance" /> and fills <see cref="InventoryItemInstance.ComponentStates" /> from behaviours under <paramref name="root" />.
        /// </summary>
        public static InventoryItemInstance CaptureInstance(GameObject root, int itemId, int count = 1)
        {
            InventoryItemInstance instance = new(itemId, count);
            if (root == null)
            {
                return instance;
            }

            // WHY: Pooled list instead of a MonoBehaviour[] per call: pickups capture on every collect. A rented list
            // also stays valid if a capture callback re-enters this utility.
            List<IInventoryItemState> states = ListPool<IInventoryItemState>.Get();
            try
            {
                root.GetComponentsInChildren(true, states);
                for (int i = 0; i < states.Count; i++)
                {
                    IInventoryItemState state = states[i];
                    instance.ComponentStates.Add(new InventoryItemComponentState(ResolveKey(state),
                        state.CaptureInventoryState()));
                }
            }
            finally
            {
                ListPool<IInventoryItemState>.Release(states);
            }

            return instance;
        }

        /// <summary>
        ///     Calls <see cref="IInventoryItemState.RestoreInventoryState" /> on matching behaviours under <paramref name="root" />.
        /// </summary>
        public static void RestoreInstance(GameObject root, InventoryItemInstance instance)
        {
            if (root == null || instance == null || instance.ComponentStates == null ||
                instance.ComponentStates.Count <= 0)
            {
                return;
            }

            Dictionary<string, InventoryItemComponentState> byKey = new();
            for (int i = 0; i < instance.ComponentStates.Count; i++)
            {
                InventoryItemComponentState state = instance.ComponentStates[i];
                if (state == null || string.IsNullOrWhiteSpace(state.Key))
                {
                    continue;
                }

                byKey[state.Key] = state;
            }

            List<IInventoryItemState> states = ListPool<IInventoryItemState>.Get();
            try
            {
                root.GetComponentsInChildren(true, states);
                for (int i = 0; i < states.Count; i++)
                {
                    IInventoryItemState state = states[i];
                    if (byKey.TryGetValue(ResolveKey(state), out InventoryItemComponentState componentState))
                    {
                        state.RestoreInventoryState(componentState.Json);
                    }
                }
            }
            finally
            {
                ListPool<IInventoryItemState>.Release(states);
            }
        }

        private static string ResolveKey(IInventoryItemState state)
        {
            string key = state.InventoryStateKey;
            return string.IsNullOrWhiteSpace(key) ? state.GetType().FullName : key;
        }
    }
}
