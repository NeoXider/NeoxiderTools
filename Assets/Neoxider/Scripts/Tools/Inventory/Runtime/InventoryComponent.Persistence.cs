using System.Collections.Generic;
using Neo.Save;

namespace Neo.Tools
{
    public sealed partial class InventoryComponent
    {
        [Button]
        /// <summary>Serializes and writes to SaveManager using <see cref="SaveId" />.</summary>
        public void Save()
        {
            if (string.IsNullOrEmpty(_saveKey) || _storage == null)
            {
                return;
            }

            InventorySaveData data = new()
            {
                Version = 1,
                InitialStateMerged = _loadMode == InventoryLoadMode.MergeSaveWithInitial ||
                                     _loadMode == InventoryLoadMode.UseSaveIfExists,
                StorageMode = (int)_storageMode,
                SlotCapacity = _slotCount,
                Slots = _storageMode == InventoryStorageMode.SlotGrid && _storage is ISlottedInventory sc
                    ? sc.CreateSlotSnapshot()
                    : new List<InventorySlotState>(),
                Records = _storageMode != InventoryStorageMode.SlotGrid
                    ? _storage.CreateRecordSnapshot()
                    : new List<InventoryItemRecord>()
            };
            string json = UnityEngine.JsonUtility.ToJson(data);
            SaveProvider.SetString(_saveKey, json);
        }

        [Button]
        /// <summary>Restores state via SaveProvider; clears inventory first and respects LoadMode.</summary>
        public void Load()
        {
            if (string.IsNullOrEmpty(_saveKey) || _isLoading)
            {
                return;
            }

            _isLoading = true;
            try
            {
                OnBeforeLoad?.Invoke();
                EnsureRuntimeInitialized();

                if (_storage != null)
                {
                    _storage.Clear();
                }

                bool hasSavedData = TryReadSaveData(out InventorySaveData data);

                if (_loadMode == InventoryLoadMode.InitialOnlyIgnoreSave)
                {
                    ApplyInitialState();
                }
                else if (!hasSavedData)
                {
                    ApplyInitialState();
                }
                else
                {
                    if (_loadMode == InventoryLoadMode.MergeSaveWithInitial && !data.InitialStateMerged)
                    {
                        ApplyInitialState();
                    }

                    ImportSaveData(data);
                }

                if (_applyInitialIfResultEmpty && IsEmpty)
                {
                    ApplyInitialState();
                }

                OnLoaded?.Invoke();
                if (_invokeEventsOnLoad)
                {
                    OnInventoryChanged?.Invoke();
                }
            }
            finally
            {
                _isLoading = false;
            }
        }

        private bool TryReadSaveData(out InventorySaveData data)
        {
            string json = SaveProvider.GetString(_saveKey, "");
            if (string.IsNullOrEmpty(json))
            {
                data = null;
                return false;
            }

            try
            {
                data = UnityEngine.JsonUtility.FromJson<InventorySaveData>(json);
                return data != null;
            }
            catch
            {
                data = null;
                return false;
            }
        }

        private bool HasAnySaveContent()
        {
            if (string.IsNullOrEmpty(_saveKey) || !TryReadSaveData(out InventorySaveData data))
            {
                return false;
            }

            return (data.Slots != null && data.Slots.Count > 0) ||
                   (data.Records != null && data.Records.Count > 0);
        }

        private void ImportSaveData(InventorySaveData data)
        {
            _storageMode = (InventoryStorageMode)data.StorageMode;
            _slotCount = data.SlotCapacity;
            EnsureStorage();

            if (_storageMode == InventoryStorageMode.SlotGrid && _storage is ISlottedInventory slotted &&
                data.Slots != null && data.Slots.Count > 0)
            {
                ImportSlots(data.Slots, slotted);
                return;
            }

            if (data.Records != null && data.Records.Count > 0)
            {
                ImportSlotRecords(data.Records, _storage);
            }

            if (data.Entries != null && data.Entries.Count > 0)
            {
                ImportEntries(data.Entries, _storage);
            }

            if (data.Instances != null && data.Instances.Count > 0)
            {
                ImportInstances(data.Instances, _storage);
            }
        }

        private void ApplyInitialState()
        {
            if (_storage == null)
            {
                return;
            }

            if (_initialEntries != null && _initialEntries.Count > 0)
            {
                ImportEntries(_initialEntries, _storage);
            }

            if (_initialStateData != null && _initialStateData.Entries != null)
            {
                for (int i = 0; i < _initialStateData.Entries.Count; i++)
                {
                    InventoryEntry e = _initialStateData.Entries[i];
                    if (e != null && CanUseItemId(e.ItemId))
                    {
                        _storage.Add(e.ItemId, e.Count);
                    }
                }
            }
        }

        private void ImportSlotRecords(List<InventoryItemRecord> list, IInventoryStorage storage)
        {
            if (list == null || list.Count == 0 || storage == null)
            {
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                InventoryItemRecord r = list[i];
                if (r == null || r.EffectiveItemId < 0 || r.EffectiveCount <= 0 || !CanUseItemId(r.EffectiveItemId))
                {
                    continue;
                }

                if (r.IsInstance && r.Instance != null)
                {
                    storage.AddInstance(r.Instance.Clone());
                }
                else
                {
                    storage.Add(r.EffectiveItemId, r.Count);
                }
            }
        }

        private void ImportSlots(List<InventorySlotState> list, ISlottedInventory slotted)
        {
            if (list == null || list.Count == 0 || slotted == null)
            {
                return;
            }

            int cap = slotted.SlotCapacity;
            for (int i = 0; i < list.Count && i < cap; i++)
            {
                InventorySlotState s = list[i];
                if (s == null || s.IsEmpty || !CanUseItemId(s.EffectiveItemId))
                {
                    continue;
                }

                slotted.SetSlot(i, s.Clone());
            }
        }

        private void ImportInstances(List<InventoryItemInstance> list, IInventoryStorage storage)
        {
            if (list == null || list.Count == 0 || storage == null)
            {
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                InventoryItemInstance inst = list[i];
                if (inst != null && CanUseItemId(inst.ItemId))
                {
                    storage.AddInstance(inst.Clone());
                }
            }
        }

        private void ImportEntries(List<InventoryEntry> list, IInventoryStorage storage)
        {
            if (list == null || list.Count == 0 || storage == null)
            {
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                InventoryEntry e = list[i];
                if (e != null && CanUseItemId(e.ItemId))
                {
                    storage.Add(e.ItemId, e.Count);
                }
            }
        }
    }
}
