using System;

namespace Neo.Tools
{
    /// <summary>
    ///     One JSON payload stored inside <see cref="InventoryItemInstance.ComponentStates" />.
    /// </summary>
    [Serializable]
    public sealed class InventoryItemComponentState
    {
        /// <summary>Matches <see cref="IInventoryItemState.InventoryStateKey" /> on the world prefab.</summary>
        public string Key;

        /// <summary>Opaque JSON from <see cref="IInventoryItemState.CaptureInventoryState" />.</summary>
        public string Json;

        public InventoryItemComponentState()
        {
        }

        public InventoryItemComponentState(string key, string json)
        {
            Key = key;
            Json = json;
        }

        /// <summary>Shallow copy of key and json.</summary>
        public InventoryItemComponentState Clone()
        {
            return new InventoryItemComponentState(Key, Json);
        }
    }
}
