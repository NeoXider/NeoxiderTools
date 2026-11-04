using System;
using System.Collections.Generic;
using Neo.Save;

namespace Neo.Tools
{
    public sealed partial class InventoryComponent
    {
        private int _autoSaveDeferDepth;
        private bool _autoSavePending;
        private int _autoSaveScopeSerial;

        // WHY: serial of the scope open at each depth (index = depth - 1). A depth alone cannot tell a stale scope
        // from a later one opened at the same depth, so a repeated dispose could close the newer scope early.
        private int[] _autoSaveScopeSerials;

        /// <summary>True while at least one <see cref="DeferAutoSave" /> scope is open.</summary>
        public bool IsAutoSaveDeferred => _autoSaveDeferDepth > 0;

        /// <summary>
        ///     Holds back Auto Save until the returned scope is disposed, so a burst of mutations (a loot bundle, a
        ///     shop grant, a scripted refill) writes the inventory JSON once instead of once per change. Events still
        ///     fire per mutation. Scopes nest; disposing the outermost one calls <see cref="Save" /> when a deferred
        ///     mutation changed the contents. Dispose each scope once, ideally with a <c>using</c> block; a repeated or
        ///     stale dispose is ignored, even after newer scopes were opened.
        /// </summary>
        /// <example>
        ///     <code>
        ///     using (inventory.DeferAutoSave())
        ///     {
        ///         inventory.AddItemByIdAmount(1, 10);
        ///         inventory.AddItemByIdAmount(2, 3);
        ///     }
        ///     </code>
        /// </example>
        public AutoSaveScope DeferAutoSave()
        {
            int depth = _autoSaveDeferDepth + 1;
            if (_autoSaveScopeSerials == null)
            {
                _autoSaveScopeSerials = new int[4];
            }
            else if (_autoSaveScopeSerials.Length < depth)
            {
                Array.Resize(ref _autoSaveScopeSerials, _autoSaveScopeSerials.Length * 2);
            }

            _autoSaveScopeSerial = unchecked(_autoSaveScopeSerial + 1);
            _autoSaveScopeSerials[depth - 1] = _autoSaveScopeSerial;
            _autoSaveDeferDepth = depth;
            return new AutoSaveScope(this, depth, _autoSaveScopeSerial);
        }

        private void EndAutoSaveDeferral(int depth, int serial)
        {
            // WHY: a repeated, stale or out-of-order dispose closes its own level (and any inner ones) only while
            // that exact scope is still open.
            if (depth <= 0 || _autoSaveDeferDepth < depth || _autoSaveScopeSerials[depth - 1] != serial)
            {
                return;
            }

            _autoSaveDeferDepth = depth - 1;
            if (_autoSaveDeferDepth == 0 && _autoSavePending)
            {
                Save();
            }
        }

        /// <summary>Scope returned by <see cref="DeferAutoSave" />; disposing it ends that deferral level.</summary>
        public readonly struct AutoSaveScope : IDisposable
        {
            private readonly InventoryComponent _owner;
            private readonly int _depth;
            private readonly int _serial;

            internal AutoSaveScope(InventoryComponent owner, int depth, int serial)
            {
                _owner = owner;
                _depth = depth;
                _serial = serial;
            }

            /// <summary>Ends this deferral level; the outermost level writes the deferred save.</summary>
            public void Dispose()
            {
                // WHY: reference check, not Unity null: mutations made before a destroy still deserve their save.
                if (!ReferenceEquals(_owner, null))
                {
                    _owner.EndAutoSaveDeferral(_depth, _serial);
                }
            }
        }

        /// <summary>Serializes and writes to SaveProvider using <see cref="SaveKey" />.</summary>
        [Button(PlayModeOnly = true)]
        public void Save()
        {
            _autoSavePending = false;
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
                    ? CaptureSlotsForSave(sc)
                    : new List<InventorySlotState>(),
                Records = _storageMode != InventoryStorageMode.SlotGrid
                    ? CaptureRecordsForSave()
                    : new List<InventoryItemRecord>()
            };
            string json = data.ToJson();
            SaveProvider.SetString(_saveKey, json);
            OnSaved?.Invoke();
        }

        // WHY: Save serializes these rows before it returns and never exposes them, so the built-in storages lend
        // their live rows instead of deep clones; with autosave this runs after every mutation.
        private static List<InventorySlotState> CaptureSlotsForSave(ISlottedInventory slotted)
        {
            if (slotted is not SlotGridInventory grid)
            {
                return slotted.CreateSlotSnapshot();
            }

            List<InventorySlotState> slots = new(grid.SlotCapacity);
            for (int i = 0; i < grid.SlotCapacity; i++)
            {
                slots.Add(grid.PeekSlot(i));
            }

            return slots;
        }

        private List<InventoryItemRecord> CaptureRecordsForSave()
        {
            if (_storage is not AggregatedInventory aggregated)
            {
                return _storage.CreateRecordSnapshot();
            }

            List<InventoryItemRecord> records = new();
            aggregated.AppendLiveRecords(records);
            return records;
        }

        /// <summary>Restores state via SaveProvider; clears inventory first and respects LoadMode.</summary>
        [Button(PlayModeOnly = true)]
        public void Load()
        {
            if (_isLoading)
            {
                return;
            }

            _isLoading = true;
            try
            {
                OnBeforeLoad?.Invoke();
                EnsureRuntimeInitialized();

                InventorySaveData data = null;
                bool hasSavedData = _loadMode != InventoryLoadMode.InitialOnlyIgnoreSave &&
                                    TryReadSaveData(out data);
                if (hasSavedData)
                {
                    RestoreSavedLayout(data);
                }

                if (_storage != null)
                {
                    _storage.Clear();
                }

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
            data = null;
            if (string.IsNullOrEmpty(_saveKey))
            {
                return false;
            }

            string json = SaveProvider.GetString(_saveKey, "");
            if (string.IsNullOrEmpty(json))
            {
                data = null;
                return false;
            }

            try
            {
                data = InventorySaveData.FromJson(json);
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

        private void RestoreSavedLayout(InventorySaveData data)
        {
            bool hasSlots = data.Slots != null && data.Slots.Count > 0;
            bool hasRecords = data.Records != null && data.Records.Count > 0;
            bool hasLegacyContent = (data.Entries != null && data.Entries.Count > 0) ||
                                    (data.Instances != null && data.Instances.Count > 0);
            if ((!hasSlots && !hasRecords && (hasLegacyContent || data.Version < 1)) ||
                (data.StorageMode != (int)InventoryStorageMode.Aggregated &&
                 data.StorageMode != (int)InventoryStorageMode.SlotGrid))
            {
                return;
            }

            _storageMode = (InventoryStorageMode)data.StorageMode;
            _slotCount = hasSlots ? data.Slots.Count : System.Math.Max(0, data.SlotCapacity);
            _storage = _storageMode == InventoryStorageMode.SlotGrid
                ? new SlotGridInventory(_slotCount)
                : new AggregatedInventory();
            ApplyConstraints();
        }

        private void ImportSaveData(InventorySaveData data)
        {
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
                if (s == null || s.IsEmpty || !CanApplySlotState(i, s))
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
