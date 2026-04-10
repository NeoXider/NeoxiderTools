using System.Collections.Generic;
using UnityEngine;

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

            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IInventoryItemState)
                {
                    return true;
                }
            }

            return false;
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

            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is not IInventoryItemState state)
                {
                    continue;
                }

                string key = string.IsNullOrWhiteSpace(state.InventoryStateKey)
                    ? behaviours[i].GetType().FullName
                    : state.InventoryStateKey;
                instance.ComponentStates.Add(new InventoryItemComponentState(key, state.CaptureInventoryState()));
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

            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is not IInventoryItemState state)
                {
                    continue;
                }

                string key = string.IsNullOrWhiteSpace(state.InventoryStateKey)
                    ? behaviours[i].GetType().FullName
                    : state.InventoryStateKey;
                if (byKey.TryGetValue(key, out InventoryItemComponentState componentState))
                {
                    state.RestoreInventoryState(componentState.Json);
                }
            }
        }
    }
}
