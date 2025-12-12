using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if ODIN_INSPECTOR
#endif

namespace Neo
{
    namespace Tools
    {
        /// <summary>
        ///     A component that manages selection between multiple GameObjects, with support for different selection modes.
        ///     Useful for UI elements, inventory systems, or any scenario requiring sequential selection.
        /// </summary>
        [AddComponentMenu("Neo/" + "Tools/" + nameof(Selector))]
        public class Selector : MonoBehaviour
        {
            #region Private Methods

            /// <summary>
            ///     Updates the active state of items based on current selection
            /// </summary>
            private void UpdateSelection()
            {
                int total = Count;
                if (total == 0)
                {
                    OnSelectionChanged?.Invoke(_currentIndex);
                    return;
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

                GameObject[] items = _items;
                if (items != null && items.Length > 0)
                {
                    if (effectiveIndex < 0)
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
                                    // Игнорируем ошибки при деактивации (объект может быть уничтожен)
                                }
                            }
                        }
                    }
                    else
                    {
                        if (_fillMode)
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
                                            // Игнорируем ошибки при изменении состояния (объект может быть уничтожен)
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            for (int i = 0; i < items.Length; i++)
                            {
                                GameObject item = items[i];
                                if (item != null)
                                {
                                    bool shouldBeActive = i == effectiveIndex;
                                    if (item.activeSelf != shouldBeActive)
                                    {
                                        try
                                        {
                                            item.SetActive(shouldBeActive);
                                        }
                                        catch (Exception)
                                        {
                                            // Игнорируем ошибки при изменении состояния (объект может быть уничтожен)
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                OnSelectionChanged?.Invoke(_currentIndex);
            }

            #endregion

            #region Serialized Fields

            [Tooltip("Sets the initial index when appearing")]
            public bool startOnAwake;

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

            [Tooltip("When enabled, automatically update items array when child objects are added or removed")]
            [SerializeField]
            private bool _autoUpdateFromChildren;

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

            [Header("Debug")] [Tooltip("Current selection index")] [SerializeField]
            private int _currentIndex;

            [Tooltip("Update selection in editor when values change")] [SerializeField]
            private bool _changeDebug = true;

            private int _startIndex;

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
            public UnityEvent<int> OnSelectionChanged;

            /// <summary>
            ///     Invoked when reaching the end of the items array (only if loop is disabled)
            /// </summary>
            public UnityEvent OnFinished;

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

            #endregion

            #region Unity Methods

            private void Awake()
            {
                _startIndex = _currentIndex;
            }

            private void Start()
            {
                TryInitializeFromChildren();

                if (Count > 0)
                {
                    UpdateSelection();
                }
            }

            private void OnEnable()
            {
                TryInitializeFromChildren();

                if (startOnAwake && Count > 0)
                {
                    Set(_startIndex);
                }
            }

            private void OnValidate()
            {
                if (_items == null)
                {
                    Debug.LogWarning("Selector: Items array is null");
                    return;
                }

                if (_setChild)
                {
                    _setChild = false;
                    RefreshItemsFromChildren();
                }

                if (_changeDebug && _items != null && Application.isPlaying)
                {
                    UpdateSelection();
                }
            }

            private void OnTransformChildrenChanged()
            {
                if (_autoUpdateFromChildren && Application.isPlaying)
                {
                    RefreshItemsFromChildren();
                }
            }

            #endregion

            #region Public Methods

            /// <summary>
            ///     Moves to the next item in the selection
            /// </summary>
#if ODIN_INSPECTOR
            [Button]
#else
            [ButtonAttribute]
#endif
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
#if ODIN_INSPECTOR
            [Button]
#else
            [ButtonAttribute]
#endif
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
#if ODIN_INSPECTOR
            [Button]
#else
            [ButtonAttribute]
#endif
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

                UpdateSelection();
            }

            /// <summary>
            ///     Sets the selection to the first item
            /// </summary>
#if ODIN_INSPECTOR
            [Button]
#else
            [ButtonAttribute]
#endif
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
#if ODIN_INSPECTOR
            [Button]
#else
            [ButtonAttribute]
#endif
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
            ///     Resets the selection to the start index
            /// </summary>
            public void Reset()
            {
                (int min, int max) = GetCurrentBounds();
                _currentIndex = min;
                UpdateSelection();
            }

            /// <summary>
            ///     Toggles the active state of a specific index (with offset)
            /// </summary>
            /// <param name="index">Index to toggle</param>
            /// <param name="state">Optional state to set (true to enable, false to disable, null to toggle)</param>
#if ODIN_INSPECTOR
            [Button]
#else
            [ButtonAttribute]
#endif
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
                }
            }

            /// <summary>
            ///     Refreshes the items array from child GameObjects
            /// </summary>
#if ODIN_INSPECTOR
            [Button]
#else
            [ButtonAttribute]
#endif
            public void RefreshItems()
            {
                RefreshItemsFromChildren();
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
            ///     Tries to initialize items from children if list is empty and auto-update is enabled
            /// </summary>
            private void TryInitializeFromChildren()
            {
                if (_autoUpdateFromChildren && (_items == null || _items.Length == 0) && _count <= 0)
                {
                    RefreshItemsFromChildren();
                }
            }

            /// <summary>
            ///     Refreshes the items array from child GameObjects
            /// </summary>
            private void RefreshItemsFromChildren()
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

                int total = Count;
                if (total > 0 && _currentIndex >= total)
                {
                    _currentIndex = total - 1;
                }

                if (Application.isPlaying && total > 0)
                {
                    UpdateSelection();
                }
            }

            #endregion
        }
    }
}