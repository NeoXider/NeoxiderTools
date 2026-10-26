using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     JSON-serializable snapshot of one <see cref="InventoryComponent" /> (entries, instances, and optional slot grid).
    /// </summary>
    [Serializable]
    public sealed class InventorySaveData : ISerializationCallbackReceiver
    {
        /// <summary>Format version for migration in <see cref="InventoryComponent" />.</summary>
        public int Version = 1;

        /// <summary>True if the initial state entries have already been merged into this save.</summary>
        public bool InitialStateMerged;

        /// <summary>Stored <see cref="InventoryStorageMode" /> as int.</summary>
        public int StorageMode;

        /// <summary>Slot count when grid data is present.</summary>
        public int SlotCapacity;

        /// <summary>Aggregated stack entries (non-instance items).</summary>
        public List<InventoryEntry> Entries = new();

        /// <summary>Instance-based items with per-item state payload.</summary>
        public List<InventoryItemInstance> Instances = new();

        /// <summary>Snapshot of items when not using slot grid.</summary>
        public List<InventoryItemRecord> Records = new();

        /// <summary>Per-slot state when using slot grid persistence.</summary>
        public List<InventorySlotState> Slots = new();

        /// <summary>Writes the existing snapshot format without materializing domain-default optional instances.</summary>
        public string ToJson(bool prettyPrint = false)
        {
            WireSaveData wire = new()
            {
                Version = Version,
                InitialStateMerged = InitialStateMerged,
                StorageMode = StorageMode,
                SlotCapacity = SlotCapacity,
                Entries = Entries,
                Instances = Instances?.ConvertAll(EncodeInstance),
                Records = Records?.ConvertAll(row => row == null ? null : new WireRow
                {
                    ItemId = row.ItemId, Count = row.Count, Instance = EncodeInstance(row.Instance)
                }),
                Slots = Slots?.ConvertAll(row => row == null ? null : new WireSlot
                {
                    ItemId = row.ItemId, Count = row.Count, Instance = EncodeInstance(row.Instance)
                })
            };
            return JsonUtility.ToJson(wire, prettyPrint);
        }

        /// <summary>
        ///     Reads compatible snapshot JSON with row id defaults and zero-default optional instances. Invalid JSON follows
        ///     <see cref="JsonUtility.FromJson{T}(string)" /> error behavior; admission remains the storage's responsibility.
        /// </summary>
        public static InventorySaveData FromJson(string json)
        {
            WireSaveData wire = JsonUtility.FromJson<WireSaveData>(json);
            if (wire == null)
            {
                return null;
            }

            return new InventorySaveData
            {
                Version = wire.Version,
                InitialStateMerged = wire.InitialStateMerged,
                StorageMode = wire.StorageMode,
                SlotCapacity = wire.SlotCapacity,
                Entries = wire.Entries ?? new List<InventoryEntry>(),
                Instances = wire.Instances?.ConvertAll(DecodeInstance) ?? new List<InventoryItemInstance>(),
                Records = wire.Records?.ConvertAll(row => row == null ? null : new InventoryItemRecord
                {
                    ItemId = row.ItemId, Count = row.Count, Instance = DecodeOptionalInstance(row.Instance)
                }) ?? new List<InventoryItemRecord>(),
                Slots = wire.Slots?.ConvertAll(row => row == null ? null : new InventorySlotState
                {
                    ItemId = row.ItemId, Count = row.Count, Instance = DecodeOptionalInstance(row.Instance)
                }) ?? new List<InventorySlotState>()
            };
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // Retain the narrow legacy callback for direct JsonUtility consumers. It cannot resolve
            // Count=1 domain defaults; use ToJson/FromJson for the nullable snapshot contract.
            if (Slots != null)
            {
                for (int i = 0; i < Slots.Count; i++)
                {
                    InventorySlotState slot = Slots[i];
                    if (slot != null && IsEmptyInlineInstance(slot.Instance))
                    {
                        slot.Instance = null;
                    }
                }
            }

            if (Records != null)
            {
                for (int i = 0; i < Records.Count; i++)
                {
                    InventoryItemRecord record = Records[i];
                    if (record != null && IsEmptyInlineInstance(record.Instance))
                    {
                        record.Instance = null;
                    }
                }
            }
        }

        private static bool IsEmptyInlineInstance(InventoryItemInstance instance)
        {
            return instance != null && string.IsNullOrEmpty(instance.InstanceId) && instance.ItemId == 0 &&
                   instance.Count == 0 && instance.StateVersion == 0 &&
                   (instance.ComponentStates == null || instance.ComponentStates.Count == 0);
        }

        private static WireInstance EncodeInstance(InventoryItemInstance instance)
        {
            return instance == null ? null : new WireInstance
            {
                InstanceId = instance.InstanceId,
                ItemId = instance.ItemId,
                Count = instance.Count,
                StateVersion = instance.StateVersion,
                ComponentStates = instance.ComponentStates
            };
        }

        private static InventoryItemInstance DecodeInstance(WireInstance instance)
        {
            // Do not clone here: Clone generates missing ids and normalizes counts before admission.
            return instance == null ? null : new InventoryItemInstance
            {
                InstanceId = instance.InstanceId,
                ItemId = instance.ItemId,
                Count = instance.Count,
                StateVersion = instance.StateVersion,
                ComponentStates = instance.ComponentStates ?? new List<InventoryItemComponentState>()
            };
        }

        private static InventoryItemInstance DecodeOptionalInstance(WireInstance instance)
        {
            if (instance == null || (string.IsNullOrEmpty(instance.InstanceId) && instance.ItemId == 0 &&
                instance.Count == 0 && instance.StateVersion == 0 &&
                (instance.ComponentStates == null || instance.ComponentStates.Count == 0)))
            {
                return null;
            }

            // In particular, id0/keyless Count1 is real data, and invalid data retains instance precedence.
            return DecodeInstance(instance);
        }

        // Keep the existing row id defaults separate: records use 0, slots use the empty sentinel -1.
        // Unity may materialize inline nulls; only the optional instance's wire defaults are all-zero.
        [Serializable]
        private sealed class WireSaveData
        {
            public int Version;
            public bool InitialStateMerged;
            public int StorageMode;
            public int SlotCapacity;
            public List<InventoryEntry> Entries;
            public List<WireInstance> Instances;
            public List<WireRow> Records;
            public List<WireSlot> Slots;
        }

        [Serializable]
        private sealed class WireRow
        {
            public int ItemId;
            public int Count;
            public WireInstance Instance;
        }

        [Serializable]
        private sealed class WireSlot
        {
            public int ItemId = -1;
            public int Count;
            public WireInstance Instance;
        }

        [Serializable]
        private sealed class WireInstance
        {
            public string InstanceId;
            public int ItemId;
            public int Count;
            public int StateVersion;
            public List<InventoryItemComponentState> ComponentStates;
        }
    }
}
