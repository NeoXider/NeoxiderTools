using System;
using System.Collections.Generic;
using Neo.Save;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
#if MIRROR
using Mirror;
using Neo.Network;
#endif

namespace Neo.Tools
{
    /// <summary>
    ///     What part of Selector state to save via SaveProvider.
    /// </summary>
    [Flags]
    public enum SelectorSaveMode
    {
        /// <summary>Save current index (Value).</summary>
        Index = 1,

        /// <summary>Save excluded indices (ExcludeIndex/IncludeIndex/IncludeAllIndices).</summary>
        ExcludedIndices = 2
    }

    /// <summary>
    ///     A component that manages selection between multiple GameObjects, with support for different selection modes.
    ///     Useful for UI elements, inventory systems, or any scenario requiring sequential selection.
    /// </summary>
    [NeoDoc("Tools/View/Selector.md")]
    [CreateFromMenu("Neoxider/Tools/View/Selector")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(Selector))]
#if MIRROR
    public class Selector : NetworkBehaviour
#else
    public class Selector : MonoBehaviour
#endif
    {
        [Header("Networking")]
        [Tooltip("If enabled, changing the selection locally is replicated to all clients.")]
        [SerializeField]
        public bool isNetworked = false;
        #region Private Methods

        /// <summary>
        ///     Updates the active state of items based on current selection.
        /// </summary>
        /// <param name="deactivateNonSelected">
        ///     If true (default), only the current selection is on (plus fill-mode rules). If false, only activates matching
        ///     items and does not turn others off — used by <see cref="SetRandom(bool)"/> when additive random is requested.
        ///     Fill mode and empty effective index always use exclusive deactivation.
        /// </param>
        private void UpdateSelection(bool deactivateNonSelected = true)
        {
#if MIRROR
            if (isNetworked && (NeoNetworkState.IsClient || NeoNetworkState.IsServer))
            {
                if (NeoNetworkState.IsClient && !NeoNetworkState.IsServer)
                {
                    CmdSyncState(_currentIndex, deactivateNonSelected);
                    return;
                }
            }
#endif

            ApplyUpdateSelection(deactivateNonSelected);

#if MIRROR
            if (isNetworked && NeoNetworkState.IsServer)
            {
                RpcSyncState(_currentIndex, deactivateNonSelected);
            }
#endif
        }

        private void ApplyUpdateSelection(bool deactivateNonSelected)
        {
            int total = Count;
            if (total == 0)
            {
                OnSelectionChanged?.Invoke(_currentIndex);
                NotifyCountActiveChangedIfNeeded();
                return;
            }

            (int min, int max) = GetCurrentBounds();
            bool virtualOnly = _items == null || _items.Length == 0;
            if (virtualOnly)
            {
                if (_currentIndex < min)
                {
                    _currentIndex = min;
                }
                else if (_currentIndex > max)
                {
                    _currentIndex = max;
                }
            }

            int minEff = _allowEmptyEffectiveIndex ? -1 : 0;
            int effectiveIndex = _currentIndex + _indexOffset;
            if (effectiveIndex < minEff)
            {
                effectiveIndex = minEff;
            }
            else if (effectiveIndex >= total)
            {
                effectiveIndex = total - 1;
            }

            bool exclusiveApply = deactivateNonSelected || _fillMode || effectiveIndex < 0;

            GameObject[] items = _items;
            if (items != null && items.Length > 0)
            {
                if (_notifySelectorItemsOnly)
                {
                    bool forceBootstrap = !_selectorItemBootstrapNotifyDone && _controlGameObjectActive;
                    for (int i = 0; i < items.Length; i++)
                    {
                        GameObject item = items[i];
                        if (item == null)
                        {
                            continue;
                        }

                        bool shouldBeActive = effectiveIndex >= 0 &&
                                              (_fillMode ? i <= effectiveIndex : i == effectiveIndex);
                        SelectorItem si = item.GetComponent<SelectorItem>();
                        if (si != null)
                        {
                            si.Index = i;
                            if (_controlGameObjectActive)
                            {
                                if (exclusiveApply)
                                {
                                    si.SetActive(shouldBeActive, forceBootstrap);
                                }
                                else if (shouldBeActive)
                                {
                                    si.SetActive(true, forceBootstrap);
                                }
                            }
                        }
                        else if (_controlGameObjectActive)
                        {
                            if (exclusiveApply && item.activeSelf != shouldBeActive)
                            {
                                try
                                {
                                    item.SetActive(shouldBeActive);
                                }
                                catch (Exception) { }
                            }
                            else if (!exclusiveApply && shouldBeActive && !item.activeSelf)
                            {
                                try
                                {
                                    item.SetActive(true);
                                }
                                catch (Exception) { }
                            }
                        }
                    }

                    if (_controlGameObjectActive)
                    {
                        _selectorItemBootstrapNotifyDone = true;
                    }
                }
                else if (effectiveIndex < 0 && _controlGameObjectActive)
                {
                    for (int i = 0; i < items.Length; i++)
                    {
                        GameObject item = items[i];
                        if (item != null && item.activeSelf)
                        {
                            try
                            {
                                item.SetActive(false);
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }
                else
                {
                    if (_controlGameObjectActive && _fillMode)
                    {
                        for (int i = 0; i < items.Length; i++)
                        {
                            GameObject item = items[i];
                            if (item != null)
                            {
                                bool shouldBeActive = i <= effectiveIndex;
                                if (item.activeSelf != shouldBeActive)
                                {
                                    try
                                    {
                                        item.SetActive(shouldBeActive);
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }
                            }
                        }
                    }
                    else if (_controlGameObjectActive)
                    {
                        for (int i = 0; i < items.Length; i++)
                        {
                            GameObject item = items[i];
                            if (item != null)
                            {
                                bool shouldBeActive = i == effectiveIndex;
                                if (exclusiveApply)
                                {
                                    if (item.activeSelf != shouldBeActive)
                                    {
                                        try
                                        {
                                            item.SetActive(shouldBeActive);
                                        }
                                        catch (Exception)
                                        {
                                        }
                                    }
                                }
                                else if (shouldBeActive && !item.activeSelf)
                                {
                                    try
                                    {
                                        item.SetActive(true);
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }
                            }
                        }
                    }
                }
            }

            OnSelectionChanged?.Invoke(_currentIndex);
            OnSelectionChangedGameObject?.Invoke(GetSelectedItem());

            SaveState();

            SyncIncludedIndicesInspector();

            NotifyCountActiveChangedIfNeeded();
        }

#if MIRROR
        [Command(requiresAuthority = false)]
        private void CmdSyncState(int newIndex, bool deactivateNonSelected)
        {
            _currentIndex = newIndex;
            ApplyUpdateSelection(deactivateNonSelected);
            RpcSyncState(newIndex, deactivateNonSelected);
        }

        [ClientRpc(includeOwner = true)]
        private void RpcSyncState(int newIndex, bool deactivateNonSelected)
        {
            if (isServerOnly) return;
            _currentIndex = newIndex;
            ApplyUpdateSelection(deactivateNonSelected);
        }

        [Command(requiresAuthority = false)]
        private void CmdSetRandom(bool deactivateOthers)
        {
            ExecuteSetRandom(deactivateOthers);
        }
#endif

        /// <summary>
        ///     Logical active count when there are no item GameObjects to inspect (virtual <see cref="Count"/> or empty items).
        /// </summary>
        private int ComputeLogicalCountActive()
        {
            int total = Count;
            if (total == 0)
            {
                return 0;
            }

            int minEff = _allowEmptyEffectiveIndex ? -1 : 0;
            int effectiveIndex = _currentIndex + _indexOffset;
            if (effectiveIndex < minEff)
            {
                return 0;
            }

            if (effectiveIndex >= total)
            {
                return _fillMode ? total : 1;
            }

            return _fillMode ? effectiveIndex + 1 : 1;
        }

        /// <summary>
        ///     Counts items that are on: <see cref="SelectorItem.ActiveValue"/> when Notify Selector Items Only and a
        ///     SelectorItem is present; otherwise <see cref="GameObject.activeSelf"/>.
        /// </summary>
        private int CountActiveItemsInArray()
        {
            GameObject[] items = _items;
            if (items == null || items.Length == 0)
            {
                return 0;
            }

            int n = 0;
            for (int i = 0; i < items.Length; i++)
            {
                GameObject item = items[i];
                if (item == null)
                {
                    continue;
                }

                if (_notifySelectorItemsOnly)
                {
                    SelectorItem si = item.GetComponent<SelectorItem>();
                    if (si != null)
                    {
                        if (si.ActiveValue)
                        {
                            n++;
                        }
                    }
                    else if (item.activeSelf)
                    {
                        n++;
                    }
                }
                else if (item.activeSelf)
                {
                    n++;
                }
            }

            return n;
        }

        private int ComputeCountActive()
        {
            if (_count > 0)
            {
                return ComputeLogicalCountActive();
            }

            if (HasItems)
            {
                return CountActiveItemsInArray();
            }

            return ComputeLogicalCountActive();
        }

        private void NotifyCountActiveChangedIfNeeded()
        {
            int c = ComputeCountActive();
            if (c == _lastCountActiveNotified)
            {
                return;
            }

            _lastCountActiveNotified = c;
            OnCountActiveChanged?.Invoke(c);
        }

        #endregion

        #region Serialized Fields

        [Tooltip(
            "When enabled: OnEnable calls Set(start index), Start syncs children, and child auto-sync may call UpdateSelection after refresh. When disabled: items still sync from children, but UpdateSelection is not run automatically until you call Set/Next/SetRandom or RefreshItems().")]
        public bool startOnAwake = true;

        [Header("Count Mode")]
        [Tooltip("If set > 0 and items array is empty, selector will work with this count as virtual items")]
        [SerializeField]
        private int _count = -1;

        [Header("Items")] [Tooltip("Array of GameObjects to be selected between")] [SerializeField]
        private GameObject[] _items;

        [Header("Auto Setup")]
        [Tooltip("When enabled, automatically populate items array with child GameObjects")]
        [SerializeField]
        private bool _setChild;

        [Tooltip(
            "When enabled, automatically keep items array in sync with child objects (auto-populate + auto-update). True by default — selector reacts to OnTransformChildrenChanged.")]
        [SerializeField]
        private bool _autoUpdateFromChildren = true;

        [Header("Random Selection")] [Tooltip("Enables random selection features (e.g. SetRandom)")] [SerializeField]
        private bool _useRandomSelection;

        [Tooltip(
            "When enabled and random selection is enabled, Next() and Previous() will use SetRandom() instead of stepping by +/- 1")]
        [FormerlySerializedAs("_randomChangeOnStep")]
        [SerializeField]
        private bool _useNextPreviousAsRandom;

        [Tooltip(
            "When true, parameterless SetRandom() only activates the picked item and leaves other items in their current active state. Set/Next/Previous still deactivate non-selected items. You can also call SetRandom(false) from code regardless of this flag.")]
        [SerializeField]
        private bool _keepOthersActiveOnRandom;

        [Header("Unique Selection (no repeats until reset)")]
        [Tooltip(
            "When enabled (and random is used), each index is selected at most once until ResetUnique() or cycle completes. Off by default.")]
        [SerializeField]
        private bool _uniqueSelectionMode;

        [Tooltip(
            "When unique mode is on and all indices have been used once: if true, auto-clear and start a new cycle; if false, next random does nothing until ResetUnique().")]
        [SerializeField]
        private bool _resetUniqueWhenCycleComplete = true;

        [Header("Selection Settings")]
        [Tooltip("Whether to loop back to the beginning when reaching the end")]
        [SerializeField]
        private bool _loop = true;

        [Tooltip("Allow effective index -1 (nothing selected). Useful for skins/empty state")] [SerializeField]
        private bool _allowEmptyEffectiveIndex;

        [Header("Fill Settings")]
        [Tooltip("If enabled, all items up to and including current index will be active")]
        [SerializeField]
        private bool _fillMode;

        [Tooltip("Offset applied to the current index for selection")] [SerializeField]
        private int _indexOffset;

        [Header("Notify SelectorItem Only")]
        [Tooltip(
            "When enabled, Selector does not call GameObject.SetActive; it finds SelectorItem on each element and calls SetActive on it. Off by default.")]
        [SerializeField]
        private bool _notifySelectorItemsOnly;

        [Tooltip(
            "When true (default), Selector applies selection to items: GameObject.SetActive for entries without SelectorItem; SelectorItem.SetActive when Notify Selector Items Only is on (SelectorItem does not toggle GameObject active by itself). When false, no SetActive calls at all — only index/events.")]
        [SerializeField]
        private bool _controlGameObjectActive = true;

        [Header("Save")] [Tooltip("Enable saving selector state via SaveProvider. Off by default.")] [SerializeField]
        private bool _saveEnabled;

        [Tooltip("Base save key for Selector. Suffixes _Index and _Excluded are appended automatically.")]
        [SerializeField]
        private string _saveKey = "Selector";

        [Tooltip("Which parts of state to save: current index, excluded indices, or both.")] [SerializeField]
        private SelectorSaveMode _saveMode = SelectorSaveMode.Index | SelectorSaveMode.ExcludedIndices;

        [Header("Debug")] [Tooltip("Current selection index")] [SerializeField]
        private int _currentIndex;

        [Tooltip("Update selection in editor when values change")] [SerializeField]
        private bool _changeDebug = true;

        private int _startIndex;
        private int _lastCountActiveNotified = int.MinValue;
        private HashSet<int> _usedIndicesForUnique;
        private HashSet<int> _excludedIndices;

        /// <summary>
        /// After the first <see cref="ApplyUpdateSelection"/> pass over real items in <see cref="_notifySelectorItemsOnly"/>
        /// mode, we stop forcing <see cref="SelectorItem.SetActive(bool,bool)"/> notifications (avoids duplicate events).
        /// </summary>
        private bool _selectorItemBootstrapNotifyDone;

        [Header("Inspector / Debug Lists (Runtime)")]
        [Tooltip(
            "Snapshot of excluded pool indices for Random (ExcludeIndex/IncludeIndex/IncludeAllIndices). " +
            "In the Editor you can pre-fill this list: on play start Selector builds a HashSet from it. " +
            "In Play Mode it updates automatically.")]
        [SerializeField]
        private List<int> _excludedIndicesInspector = new();

        [Tooltip("Snapshot of indices already used in unique-selection mode. Read-only (debug).")] [SerializeField]
        private List<int> _usedIndicesForUniqueInspector = new();

        [Tooltip(
            "Snapshot of included pool indices for Random (all indices within current bounds except excluded). " +
            "Shown in the Inspector for convenience. Stays in sync with excluded set and index bounds.")]
        [SerializeField]
        private List<int> _includedIndicesInspector = new();

        /// <summary>
        ///     Returns the number of selectable items (GameObjects or virtual count)
        ///     Priority: If _count > 0 (virtual mode), returns _count. Otherwise returns _items.Length if available.
        /// </summary>
        public int Count
        {
            get
            {
                if (_count > 0)
                {
                    return _count;
                }

                if (_items != null && _items.Length > 0)
                {
                    return _items.Length;
                }

                return 0;
            }
            set
            {
                _count = value;
                if (Count > 0)
                {
                    UpdateSelection();
                }
            }
        }

        /// <summary>
        ///     Returns true if selector is working with GameObjects
        /// </summary>
        public bool HasItems => _items != null && _items.Length > 0;

        #endregion

        #region Events

        /// <summary>
        ///     Invoked when the selection changes, providing the new index
        /// </summary>
        public UnityEvent<int> OnSelectionChanged = new();

        /// <summary>
        ///     Invoked when reaching the end of the items array (only if loop is disabled)
        /// </summary>
        public UnityEvent OnFinished = new();

        /// <summary>
        ///     Invoked when unique mode is on and all indices have been selected once (cycle complete). If auto-reset is enabled,
        ///     the set is cleared right after.
        /// </summary>
        public UnityEvent OnUniqueCycleComplete = new();

        /// <summary>
        ///     Invoked when ResetUnique() is called (unique mode tracking is cleared).
        /// </summary>
        public UnityEvent OnUniqueReset = new();

        /// <summary>
        ///     Invoked when selection changes, passing the newly selected GameObject (or null if none).
        /// </summary>
        public UnityEvent<GameObject> OnSelectionChangedGameObject = new();

        /// <summary>
        ///     Invoked when the number of active items changes (see <see cref="CountActive"/>). With real item objects, this
        ///     reflects actual <see cref="GameObject.activeSelf"/> / <see cref="SelectorItem.ActiveValue"/> counts; with virtual
        ///     <see cref="Count"/> only, the previous logical formula is used.
        /// </summary>
        public UnityEvent<int> OnCountActiveChanged = new();

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the array of selectable items
        /// </summary>
        public GameObject[] Items => _items;

        /// <summary>
        ///     Gets the current selection index
        /// </summary>
        public int Value
        {
            get => _currentIndex;
            set => Set(value);
        }

        /// <summary>
        ///     Gets or sets the fill mode
        /// </summary>
        public bool FillMode
        {
            get => _fillMode;
            set
            {
                _fillMode = value;
                UpdateSelection();
            }
        }

        /// <summary>
        ///     Gets or sets the index offset
        /// </summary>
        public int IndexOffset
        {
            get => _indexOffset;
            set
            {
                _indexOffset = value;
                UpdateSelection();
            }
        }

        /// <summary>
        ///     Gets whether the selector has reached the end of the items array
        /// </summary>
        public bool IsAtEnd
        {
            get
            {
                (int min, int max) = GetCurrentBounds();
                return _currentIndex >= max;
            }
        }

        /// <summary>
        ///     Gets whether the selector is at the beginning of the items array
        /// </summary>
        public bool IsAtStart
        {
            get
            {
                (int min, int max) = GetCurrentBounds();
                return _currentIndex <= min;
            }
        }

        /// <summary>
        ///     Gets the current index with offset applied
        /// </summary>
        public int IndexWithOffset => _currentIndex + _indexOffset;

        /// <summary>
        ///     Gets whether unique selection mode is enabled (no index repeats until reset).
        /// </summary>
        public bool UniqueSelectionMode => _uniqueSelectionMode;

        /// <summary>
        ///     In unique mode, gets how many indices have not been used yet in the current cycle. 0 when cycle is complete or when
        ///     unique mode is off.
        /// </summary>
        public int UniqueRemainingCount
        {
            get
            {
                if (!_uniqueSelectionMode || _usedIndicesForUnique == null)
                {
                    return 0;
                }

                (int min, int max) = GetCurrentBounds();
                int range = max - min + 1;
                if (range <= 0)
                {
                    return 0;
                }

                int used = 0;
                for (int i = min; i <= max; i++)
                {
                    if (_usedIndicesForUnique.Contains(i))
                    {
                        used++;
                    }
                }

                return range - used;
            }
        }

        /// <summary>
        ///     Gets the currently selected GameObject
        /// </summary>
        public GameObject Item
        {
            get
            {
                if (!HasItems)
                {
                    return null;
                }

                int effectiveIndex = Value + _indexOffset;
                if (effectiveIndex < 0 || effectiveIndex >= _items.Length)
                {
                    return null;
                }

                return _items[effectiveIndex];
            }
        }

        /// <summary>
        ///     Gets how many items are currently on: with a populated <see cref="Items"/> array and <c>_count &lt;= 0</c>, counts
        ///     <see cref="GameObject.activeSelf"/> (or <see cref="SelectorItem.ActiveValue"/> when Notify Selector Items Only and
        ///     a SelectorItem is present). With virtual <see cref="Count"/> only (<c>_count &gt; 0</c>) or no items, returns the
        ///     logical count (0/1 or fill prefix length). Subscribe to <see cref="OnCountActiveChanged"/> when the value changes.
        /// </summary>
        public int CountActive => ComputeCountActive();

        /// <summary>
        ///     When true, Selector notifies SelectorItem components instead of calling GameObject.SetActive.
        /// </summary>
        public bool NotifySelectorItemsOnly
        {
            get => _notifySelectorItemsOnly;
            set => _notifySelectorItemsOnly = value;
        }

        /// <summary>
        ///     Number of indices currently excluded from the random pool.
        /// </summary>
        public int ExcludedCount => _excludedIndices?.Count ?? 0;

        /// <summary>
        ///     Returns true if the given index is excluded from the random pool.
        /// </summary>
        public bool IsExcluded(int index)
        {
            return _excludedIndices != null && _excludedIndices.Contains(index);
        }

        /// <summary>
        ///     Snapshot of excluded pool indices for Random (Inspector display and use from other scripts).
        /// </summary>
        public IReadOnlyList<int> ExcludedIndices => _excludedIndicesInspector;

        /// <summary>
        ///     Snapshot of indices already used in unique-selection mode (debug).
        /// </summary>
        public IReadOnlyList<int> UsedIndicesForUnique => _usedIndicesForUniqueInspector;

        /// <summary>
        ///     Snapshot of included Random pool indices (all indices in current bounds except excluded).
        /// </summary>
        public IReadOnlyList<int> IncludedIndices => _includedIndicesInspector;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            _startIndex = _currentIndex;
        }

        private void Start()
        {
            TryInitializeFromChildren();

            if (_saveEnabled && !string.IsNullOrEmpty(_saveKey))
            {
                LoadState();
            }

            if (!_saveEnabled || (_saveMode & SelectorSaveMode.ExcludedIndices) == 0)
            {
                ApplyExcludedIndicesFromInspector();
            }

            SyncExcludedIndicesInspector();
            SyncUsedIndicesForUniqueInspector();

            if (startOnAwake && Count > 0)
            {
                UpdateSelection();
            }
        }

        private void OnEnable()
        {
            TryInitializeFromChildren(startOnAwake);

            if (startOnAwake && Count > 0)
            {
                Set(_startIndex);
            }
        }

        private void OnDisable()
        {
            _selectorItemBootstrapNotifyDone = false;
        }

#if MIRROR
        protected override void OnValidate()
        {
            if (isNetworked)
            {
                base.OnValidate();
            }
#else
        private void OnValidate()
        {
#endif
            _items ??= Array.Empty<GameObject>();

            if (_setChild)
            {
                _setChild = false;
                RefreshItemsFromChildren(startOnAwake);
            }
            else if (_autoUpdateFromChildren && _count <= 0)
            {
                RefreshItemsFromChildren(startOnAwake);
            }

            if (_useNextPreviousAsRandom)
            {
                _useRandomSelection = true;
            }

            if (_changeDebug && _items != null && Application.isPlaying && startOnAwake)
            {
                UpdateSelection();
            }

            if (!Application.isPlaying)
            {
                ApplyExcludedIndicesFromInspector();
                SyncExcludedIndicesInspector();
            }
        }

        #endregion

        #region Save Helpers

        private void SaveState()
        {
            if (!_saveEnabled || string.IsNullOrEmpty(_saveKey))
            {
                return;
            }

            string keyBase = _saveKey;

            if ((_saveMode & SelectorSaveMode.Index) != 0)
            {
                SaveProvider.SetInt(keyBase + "_Index", _currentIndex);
            }

            if ((_saveMode & SelectorSaveMode.ExcludedIndices) != 0)
            {
                string excludedKey = keyBase + "_Excluded";
                if (_excludedIndices == null || _excludedIndices.Count == 0)
                {
                    if (SaveProvider.HasKey(excludedKey))
                    {
                        SaveProvider.DeleteKey(excludedKey);
                    }
                }
                else
                {
                    string data = string.Join(",", _excludedIndices);
                    SaveProvider.SetString(excludedKey, data);
                }
            }
        }

        private void LoadState()
        {
            if (!_saveEnabled || string.IsNullOrEmpty(_saveKey))
            {
                return;
            }

            string keyBase = _saveKey;

            if ((_saveMode & SelectorSaveMode.Index) != 0)
            {
                string indexKey = keyBase + "_Index";
                if (SaveProvider.HasKey(indexKey))
                {
                    _currentIndex = SaveProvider.GetInt(indexKey, _currentIndex);
                }
            }

            if ((_saveMode & SelectorSaveMode.ExcludedIndices) != 0)
            {
                string excludedKey = keyBase + "_Excluded";
                _excludedIndices ??= new HashSet<int>();
                _excludedIndices.Clear();

                if (SaveProvider.HasKey(excludedKey))
                {
                    string data = SaveProvider.GetString(excludedKey, string.Empty);
                    if (!string.IsNullOrEmpty(data))
                    {
                        string[] parts = data.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < parts.Length; i++)
                        {
                            if (int.TryParse(parts[i].Trim(), out int idx))
                            {
                                _excludedIndices.Add(idx);
                            }
                        }
                    }
                }
            }

            SyncExcludedIndicesInspector();
            SyncUsedIndicesForUniqueInspector();
        }

        private void SyncExcludedIndicesInspector()
        {
            _excludedIndicesInspector ??= new List<int>();
            _excludedIndicesInspector.Clear();

            if (_excludedIndices == null)
            {
                return;
            }

            foreach (int idx in _excludedIndices)
            {
                _excludedIndicesInspector.Add(idx);
            }

            _excludedIndicesInspector.Sort();

            SyncIncludedIndicesInspector();
        }

        private void SyncUsedIndicesForUniqueInspector()
        {
            _usedIndicesForUniqueInspector ??= new List<int>();
            _usedIndicesForUniqueInspector.Clear();

            if (_usedIndicesForUnique == null)
            {
                return;
            }

            foreach (int idx in _usedIndicesForUnique)
            {
                _usedIndicesForUniqueInspector.Add(idx);
            }

            _usedIndicesForUniqueInspector.Sort();
        }

        private void SyncIncludedIndicesInspector()
        {
            _includedIndicesInspector ??= new List<int>();
            _includedIndicesInspector.Clear();

            int total = Count;
            if (total <= 0)
            {
                return;
            }

            (int min, int max) = GetCurrentBounds();
            if (min > max)
            {
                return;
            }

            for (int i = min; i <= max; i++)
            {
                if (_excludedIndices != null && _excludedIndices.Contains(i))
                {
                    continue;
                }

                _includedIndicesInspector.Add(i);
            }

            _includedIndicesInspector.Sort();
        }

        private void ApplyExcludedIndicesFromInspector()
        {
            if (_excludedIndicesInspector == null)
            {
                return;
            }

            _excludedIndices ??= new HashSet<int>();
            _excludedIndices.Clear();

            for (int i = 0; i < _excludedIndicesInspector.Count; i++)
            {
                _excludedIndices.Add(_excludedIndicesInspector[i]);
            }
        }

        private void OnTransformChildrenChanged()
        {
            if (_autoUpdateFromChildren && _count <= 0)
            {
                RefreshItemsFromChildren(startOnAwake);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Moves to the next item in the selection
        /// </summary>
        [Button]
        public void Next()
        {
            TryInitializeFromChildren();

            int total = Count;
            if (total == 0)
            {
                Debug.LogWarning(
                    "Selector: Cannot move to next - no items available (items array is null/empty or count is 0)");
                return;
            }

            if (_useRandomSelection && _useNextPreviousAsRandom)
            {
                SetRandom();
                return;
            }

            _currentIndex++;

            (int min, int max) = GetCurrentBounds();
            if (_currentIndex > max)
            {
                if (_loop)
                {
                    _currentIndex = min;
                }
                else
                {
                    _currentIndex = max;
                }

                OnFinished?.Invoke();
            }

            UpdateSelection();
        }

        /// <summary>
        ///     Moves to the previous item in the selection
        /// </summary>
        [Button]
        public void Previous()
        {
            TryInitializeFromChildren();

            int total = Count;
            if (total == 0)
            {
                Debug.LogWarning(
                    "Selector: Cannot move to previous - no items available (items array is null/empty or count is 0)");
                return;
            }

            if (_useRandomSelection && _useNextPreviousAsRandom)
            {
                SetRandom();
                return;
            }

            _currentIndex--;

            (int min, int max) = GetCurrentBounds();
            if (_currentIndex < min)
            {
                if (_loop)
                {
                    _currentIndex = max;
                }
                else
                {
                    _currentIndex = min;
                }
            }

            UpdateSelection();
        }

        /// <summary>
        ///     Gets the current selection index
        /// </summary>
        /// <returns>The current index</returns>
        public int GetCurrentIndex()
        {
            return _currentIndex;
        }

        /// <summary>
        ///     Gets the total number of items
        /// </summary>
        /// <returns>The number of items</returns>
        public int GetCount()
        {
            return Count;
        }

        /// <summary>
        ///     Sets the current selection index
        /// </summary>
        /// <param name="index">The index to set</param>
        [Button]
        public void Set(int index)
        {
            TryInitializeFromChildren();

            int total = Count;
            if (total == 0)
            {
                Debug.LogWarning(
                    "Selector: Cannot set selection - no items available (items array is null/empty or count is 0)");
                return;
            }

            (int min, int max) = GetCurrentBounds();
            if (_loop)
            {
                int range = max - min + 1;
                if (range <= 0)
                {
                    _currentIndex = min;
                }
                else
                {
                    _currentIndex = ((index - min) % range + range) % range + min;
                }
            }
            else
            {
                _currentIndex = Mathf.Clamp(index, min, max);
            }

            MarkIndexUsedInUniqueMode(_currentIndex);
            UpdateSelection();
        }

        /// <summary>
        ///     Sets the selection to a random value within the current valid bounds.
        ///     In unique mode, picks only from indices not yet used in the current cycle; when all are used, invokes
        ///     OnUniqueCycleComplete and optionally starts a new cycle.
        ///     Uses <see cref="_keepOthersActiveOnRandom"/>: when that flag is false (default), non-selected items are turned off.
        /// </summary>
        [Button]
        public void SetRandom()
        {
            SetRandom(!_keepOthersActiveOnRandom);
        }

        /// <summary>
        ///     Sets the selection to a random index (same rules as parameterless <see cref="SetRandom()"/>).
        /// </summary>
        /// <param name="deactivateOthers">
        ///     If true, only the picked item stays active (classic behaviour). If false, the picked item is activated and other
        ///     items keep their current active state.
        /// </param>
        public void SetRandom(bool deactivateOthers)
        {
            TryInitializeFromChildren();

            if (!_useRandomSelection)
            {
                Debug.LogWarning("Selector: Random selection is disabled (_useRandomSelection = false)");
                return;
            }

#if MIRROR
            if (isNetworked && (NeoNetworkState.IsClient || NeoNetworkState.IsServer))
            {
                if (NeoNetworkState.IsClient && !NeoNetworkState.IsServer)
                {
                    CmdSetRandom(deactivateOthers);
                    return;
                }
            }
#endif

            ExecuteSetRandom(deactivateOthers);
        }

        private void ExecuteSetRandom(bool deactivateOthers)
        {
            int total = Count;
            if (total == 0)
            {
                Debug.LogWarning(
                    "Selector: Cannot set random selection - no items available (items array is null/empty or count is 0)");
                return;
            }

            (int min, int max) = GetCurrentBounds();
            if (min > max)
            {
                return;
            }

            bool IsExcludedIndex(int idx)
            {
                return _excludedIndices != null && _excludedIndices.Contains(idx);
            }

            int range = max - min + 1;
            List<int> availableNonExcluded = new(range);
            for (int i = min; i <= max; i++)
            {
                if (!IsExcludedIndex(i))
                {
                    availableNonExcluded.Add(i);
                }
            }

            if (availableNonExcluded.Count == 0)
            {
                return;
            }

            if (range <= 1 || availableNonExcluded.Count == 1)
            {
                int only = availableNonExcluded[0];
                _currentIndex = only;
                if (_uniqueSelectionMode)
                {
                    MarkIndexUsedInUniqueMode(only);
                }

                UpdateSelection(deactivateOthers);
                return;
            }

            if (_uniqueSelectionMode)
            {
                SetRandomUnique(min, max, range, IsExcludedIndex, deactivateOthers);
                return;
            }

            int pick = Random.Range(0, availableNonExcluded.Count);
            int newIndex = availableNonExcluded[pick];
            if (newIndex == _currentIndex && availableNonExcluded.Count > 1)
            {
                int next = (pick + 1) % availableNonExcluded.Count;
                newIndex = availableNonExcluded[next];
            }

            _currentIndex = newIndex;
            UpdateSelection(deactivateOthers);
        }

        private void SetRandomUnique(int min, int max, int range, Func<int, bool> isExcluded, bool deactivateOthers)
        {
            _usedIndicesForUnique ??= new HashSet<int>();

            List<int> available = new(range);
            for (int i = min; i <= max; i++)
            {
                if (!_usedIndicesForUnique.Contains(i) && (isExcluded == null || !isExcluded(i)))
                {
                    available.Add(i);
                }
            }

            if (available.Count == 0)
            {
                OnUniqueCycleComplete?.Invoke();
                if (_resetUniqueWhenCycleComplete)
                {
                    _usedIndicesForUnique.Clear();
                    SyncUsedIndicesForUniqueInspector();
                    for (int i = min; i <= max; i++)
                    {
                        if (isExcluded == null || !isExcluded(i))
                        {
                            available.Add(i);
                        }
                    }
                }
                else
                {
                    return;
                }
            }

            int chosen = available[Random.Range(0, available.Count)];
            _usedIndicesForUnique.Add(chosen);
            SyncUsedIndicesForUniqueInspector();
            _currentIndex = chosen;
            UpdateSelection(deactivateOthers);
        }

        private void MarkIndexUsedInUniqueMode(int index)
        {
            if (!_uniqueSelectionMode)
            {
                return;
            }

            _usedIndicesForUnique ??= new HashSet<int>();
            _usedIndicesForUnique.Add(index);
            SyncUsedIndicesForUniqueInspector();
        }

        /// <summary>
        ///     Sets the selection to the first item
        /// </summary>
        [Button]
        public void SetFirst()
        {
            TryInitializeFromChildren();

            int total = Count;
            if (total == 0)
            {
                Debug.LogWarning(
                    "Selector: Cannot set to first - no items available (items array is null/empty or count is 0)");
                return;
            }

            (int min, int max) = GetCurrentBounds();
            _currentIndex = min;
            UpdateSelection();
        }

        /// <summary>
        ///     Sets the selection to the last item
        /// </summary>
        [Button]
        public void SetLast()
        {
            TryInitializeFromChildren();

            int total = Count;
            if (total == 0)
            {
                Debug.LogWarning(
                    "Selector: Cannot set to last - no items available (items array is null/empty or count is 0)");
                return;
            }

            _currentIndex = total - 1;
            UpdateSelection();
        }

        /// <summary>
        ///     Toggles between fill mode and normal mode
        /// </summary>
        public void ToggleFillMode()
        {
            _fillMode = !_fillMode;
            UpdateSelection();
        }

        /// <summary>
        ///     Gets the currently selected GameObject
        /// </summary>
        /// <returns>The selected GameObject or null if none is selected (including when effectiveIndex == -1)</returns>
        public GameObject GetSelectedItem()
        {
            if (!HasItems)
            {
                return null;
            }

            int total = Count;
            if (total == 0)
            {
                return null;
            }

            int idx = _currentIndex + _indexOffset;
            if (idx < 0 || idx >= _items.Length)
            {
                return null;
            }

            return _items[idx];
        }

        /// <summary>
        ///     Checks if a specific index is valid
        /// </summary>
        /// <param name="index">The index to check</param>
        /// <returns>True if the index is valid, false otherwise</returns>
        public bool IsValidIndex(int index)
        {
            int total = Count;
            if (total <= 0)
            {
                return false;
            }

            (int min, int max) = GetCurrentBounds();
            return index >= min && index <= max;
        }

        /// <summary>
        ///     Resets the selection to the start index (first valid index).
        /// </summary>
        public void Reset()
        {
            (int min, int max) = GetCurrentBounds();
            _currentIndex = min;
            UpdateSelection();
        }

        /// <summary>
        ///     Clears the unique-selection tracking so indices can repeat again. Use when unique mode is on and you want the next
        ///     random (or manual Set) to be allowed to repeat. Invokes OnUniqueReset.
        /// </summary>
        [Button("Reset Unique")]
        public void ResetUnique()
        {
            if (_usedIndicesForUnique != null && _usedIndicesForUnique.Count > 0)
            {
                _usedIndicesForUnique.Clear();
                SyncUsedIndicesForUniqueInspector();
                OnUniqueReset?.Invoke();
            }
        }

        /// <summary>
        ///     Resets selection to first index and clears unique-mode tracking (so next random can repeat).
        /// </summary>
        [Button("Reset All")]
        public void ResetAll()
        {
            Reset();
            ResetUnique();
        }

        /// <summary>
        ///     Excludes the given index from the random pool (e.g. when item is "resolved"). Excluded indices are not chosen by
        ///     SetRandom until IncludeIndex or IncludeAllIndices is called.
        /// </summary>
        /// <param name="index">Index to exclude.</param>
        public void ExcludeIndex(int index)
        {
            _excludedIndices ??= new HashSet<int>();
            _excludedIndices.Add(index);
            SyncExcludedIndicesInspector();
        }

        /// <summary>
        ///     Includes the given index back into the random pool.
        /// </summary>
        /// <param name="index">Index to include.</param>
        public void IncludeIndex(int index)
        {
            _excludedIndices?.Remove(index);
            SyncExcludedIndicesInspector();
        }

        /// <summary>
        ///     Clears all excluded indices so the full pool is available for SetRandom again.
        /// </summary>
        public void IncludeAllIndices()
        {
            _excludedIndices?.Clear();
            SyncExcludedIndicesInspector();
        }

        /// <summary>
        ///     Sets the excluded index list in one call (convenient when driven from other scripts).
        /// </summary>
        /// <param name="indices">Indices to exclude from the Random pool.</param>
        public void SetExcludedIndices(IEnumerable<int> indices)
        {
            _excludedIndices = indices != null ? new HashSet<int>(indices) : new HashSet<int>();
            SyncExcludedIndicesInspector();
            SyncIncludedIndicesInspector();
        }

        /// <summary>
        ///     Toggles the active state of a specific index (with offset)
        /// </summary>
        /// <param name="index">Index to toggle</param>
        /// <param name="state">Optional state to set (true to enable, false to disable, null to toggle)</param>
        [Button]
        public void ToggleIndex(int index, bool? state = null)
        {
            if (!HasItems)
            {
                Debug.LogWarning("Selector: No GameObjects to toggle");
                return;
            }

            int effectiveIndex = index + _indexOffset;
            if (effectiveIndex < 0 || effectiveIndex >= _items.Length)
            {
                Debug.LogWarning($"Selector: Index {index} with offset {_indexOffset} is out of bounds");
                return;
            }

            if (_items[effectiveIndex] != null)
            {
                _items[effectiveIndex].SetActive(state ?? !_items[effectiveIndex].activeSelf);
                NotifyCountActiveChangedIfNeeded();
            }
        }

        /// <summary>
        ///     Refreshes the items array from child GameObjects
        /// </summary>
        [Button]
        public void RefreshItems()
        {
            RefreshItemsFromChildren(true);
        }

        /// <summary>
        ///     Adds a GameObject to the items array
        /// </summary>
        /// <param name="item">The GameObject to add</param>
        public void AddItem(GameObject item)
        {
            if (item == null)
            {
                Debug.LogWarning("Selector: Cannot add null GameObject to items");
                return;
            }

            if (_count > 0)
            {
                Debug.LogWarning("Selector: Cannot add items when using count mode (_count > 0)");
                return;
            }

            List<GameObject> itemsList = _items != null ? new List<GameObject>(_items) : new List<GameObject>();

            if (!itemsList.Contains(item))
            {
                itemsList.Add(item);
                _items = itemsList.ToArray();

                if (Count > 0)
                {
                    UpdateSelection();
                }
            }
        }

        /// <summary>
        ///     Removes a GameObject from the items array
        /// </summary>
        /// <param name="item">The GameObject to remove</param>
        public void RemoveItem(GameObject item)
        {
            if (item == null || _items == null)
            {
                return;
            }

            List<GameObject> itemsList = new(_items);

            if (itemsList.Remove(item))
            {
                _items = itemsList.ToArray();

                int total = Count;
                if (_currentIndex >= total && total > 0)
                {
                    _currentIndex = total - 1;
                }

                if (Count > 0)
                {
                    UpdateSelection();
                }
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        ///     Gets the current valid bounds for the selection index
        /// </summary>
        /// <returns>A tuple containing the minimum and maximum valid indices</returns>
        private (int min, int max) GetCurrentBounds()
        {
            int total = Count;
            if (total <= 0)
            {
                return (0, 0);
            }

            int effMin = _allowEmptyEffectiveIndex ? -1 : 0;
            int min = effMin - _indexOffset;
            int max = total - 1 - _indexOffset;
            return (min, max);
        }

        /// <summary>
        ///     Tries to initialize/sync items from children if auto-update is enabled
        /// </summary>
        private void TryInitializeFromChildren(bool forceSync = false)
        {
            if (!_autoUpdateFromChildren)
            {
                return;
            }

            // Count > 0 means "virtual items" (no GameObjects). If Items is empty but there are real children,
            // child sync was skipped and SelectorItem / events never run — reset Count so children drive Items.
            if (_count > 0 && (_items == null || _items.Length == 0) && HasChildObjectsForItemsSync())
            {
                Debug.LogWarning(
                    $"[Selector] '{name}': Count > 0 with empty Items skips syncing from children. " +
                    "Resetting Count to -1 and building Items from children. " +
                    "Use Count > 0 only for virtual slots with no child objects.",
                    this);
                _count = -1;
            }

            if (_count > 0)
            {
                return;
            }

            if (forceSync)
            {
                RefreshItemsFromChildren(startOnAwake);
                return;
            }

            if (_items == null || _items.Length == 0 || !IsItemsSyncedWithChildren())
            {
                RefreshItemsFromChildren(startOnAwake);
            }
        }

        private bool HasChildObjectsForItemsSync()
        {
            if (transform == null)
            {
                return false;
            }

            foreach (Transform child in transform)
            {
                if (child != null && child.gameObject != null && child.gameObject != gameObject)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsItemsSyncedWithChildren()
        {
            if (transform == null)
            {
                return true;
            }

            if (_items == null)
            {
                return false;
            }

            int index = 0;
            foreach (Transform child in transform)
            {
                if (child == null || child.gameObject == null || child.gameObject == gameObject)
                {
                    continue;
                }

                if (index >= _items.Length)
                {
                    return false;
                }

                if (_items[index] != child.gameObject)
                {
                    return false;
                }

                index++;
            }

            return index == _items.Length;
        }

        /// <summary>
        ///     Refreshes the items array from child GameObjects
        /// </summary>
        /// <param name="applySelection">
        ///     When true (e.g. <see cref="startOnAwake"/> or explicit <see cref="RefreshItems"/>), calls
        ///     <see cref="UpdateSelection"/> after sync. When false, only rebuilds <see cref="_items"/> and indices — no active-state
        ///     propagation until a public API calls <see cref="UpdateSelection"/> (e.g. <see cref="Set"/>).
        /// </param>
        private void RefreshItemsFromChildren(bool applySelection)
        {
            if (transform == null)
            {
                return;
            }

            List<GameObject> childs = new();

            foreach (Transform child in transform)
            {
                if (child != null && child.gameObject != null && child.gameObject != gameObject)
                {
                    childs.Add(child.gameObject);
                }
            }

            _items = childs.ToArray();

            for (int i = 0; i < _items.Length; i++)
            {
                if (_items[i] != null && _items[i].TryGetComponent(out SelectorItem si))
                {
                    si.Index = i;
                }
            }

            int total = Count;
            if (total > 0 && _currentIndex >= total)
            {
                _currentIndex = total - 1;
            }

            if (Application.isPlaying && total > 0 && applySelection)
            {
                UpdateSelection();
            }
        }

        #endregion
    }
}

