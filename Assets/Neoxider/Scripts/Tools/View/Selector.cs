using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Neo
{
    namespace Tools
    {
        /// <summary>
        /// A component that manages selection between multiple GameObjects, with support for different selection modes.
        /// Useful for UI elements, inventory systems, or any scenario requiring sequential selection.
        /// </summary>
        [AddComponentMenu("Neoxider/" + "Tools/" + nameof(Selector))]
        public class Selector : MonoBehaviour
        {
            #region Serialized Fields

            [Tooltip("Sets the initial index when appearing")]
            public bool startOnAwake = false;

            [Header("Count Mode")]
            [Tooltip("If set > 0 and items array is empty, selector will work with this count as virtual items")]
            [SerializeField]
            private int _count = -1; // -1 means disabled, >0 enables count mode

            [Header("Items")] [Tooltip("Array of GameObjects to be selected between")] [SerializeField]
            private GameObject[] _items;

            [Header("Auto Setup")]
            [Tooltip("When enabled, automatically populate items array with child GameObjects")]
            [SerializeField]
            private bool _setChild;

            [Header("Selection Settings")]
            [Tooltip("Whether to loop back to the beginning when reaching the end")]
            [SerializeField]
            private bool _loop = true;

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

            private int _startIndex = 0;


            /// <summary>
            /// Returns the number of selectable items (GameObjects or virtual count)
            /// </summary>
            public int Count
            {
                get
                {
                    if (_items != null && _items.Length > 0 && _count == 0)
                        return _items.Length;
                    return _count > 0 ? _count : 0;
                }
                set
                {
                    _count = value;
                    UpdateSelection();
                }
            }

            /// <summary>
            /// Returns true if selector is working with GameObjects
            /// </summary>
            public bool HasItems => _items != null && _items.Length > 0;

            #endregion

            #region Events

            /// <summary>
            /// Invoked when the selection changes, providing the new index
            /// </summary>
            public UnityEvent<int> OnSelectionChanged;

            /// <summary>
            /// Invoked when reaching the end of the items array (only if loop is disabled)
            /// </summary>
            public UnityEvent OnFinished;

            #endregion

            #region Properties

            /// <summary>
            /// Gets the array of selectable items
            /// </summary>
            public GameObject[] Items => _items;

            /// <summary>
            /// Gets the current selection index
            /// </summary>
            public int Value
            {
                get => _currentIndex;
                set => Set(value);
            }

            /// <summary>
            /// Gets or sets the fill mode
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
            /// Gets or sets the index offset
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
            /// Gets whether the selector has reached the end of the items array
            /// </summary>
            public bool IsAtEnd => _currentIndex >= Count - 1;

            /// <summary>
            /// Gets whether the selector is at the beginning of the items array
            /// </summary>
            public bool IsAtStart => _currentIndex <= 0;

            public int IndexWithOffset => _currentIndex + _indexOffset;

            public GameObject Item
            {
                get
                {
                    if (Value >= 0 && Value < Count)
                        return _items[Value];
                    return null;
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
                // Ensure selection is applied at start
                UpdateSelection();
            }

            private void OnEnable()
            {
                if (startOnAwake) Set(_startIndex);
            }

            private void OnValidate()
            {
                // Validate items array
                if (_items == null)
                {
                    Debug.LogWarning("Selector: Items array is null");
                    return;
                }

                // Auto-populate items from children if requested
                if (_setChild)
                {
                    _setChild = false;
                    var childs = new List<GameObject>();

                    foreach (Transform child in transform)
                        if (child.gameObject != gameObject)
                            childs.Add(child.gameObject);

                    _items = childs.ToArray();

                    // Log the number of items found
                    if (_items.Length > 0)
                        Debug.Log($"Selector: Auto-populated {_items.Length} items from children");
                    else
                        Debug.LogWarning("Selector: No child items found to populate");
                }

                // Update selection in editor if debug mode is enabled
                if (_changeDebug && _items != null)
                    UpdateSelection();
            }

            #endregion

            #region Private Methods

            /// <summary>
            /// Updates the active state of items based on current selection
            /// </summary>
            private void UpdateSelection()
            {
                var total = Count;
                if (total == 0)
                {
                    Debug.LogWarning("Selector: No items to select");
                    return;
                }

                var effectiveIndex = _currentIndex + _indexOffset;
                if (effectiveIndex < -1)
                    effectiveIndex = -1;
                else if (effectiveIndex >= total)
                    effectiveIndex = total - 1;

                if (HasItems)
                {
                    // Update GameObject active state based on selection
                    if (_fillMode)
                    {
                        for (var i = 0; i < _items.Length; i++)
                            if (_items[i] != null)
                                _items[i].SetActive(i <= effectiveIndex);
                    }
                    else
                    {
                        for (var i = 0; i < _items.Length; i++)
                            if (_items[i] != null)
                                _items[i].SetActive(i == effectiveIndex);
                    }
                }

                // In count-only mode, just invoke the event
                OnSelectionChanged?.Invoke(_currentIndex);
            }

            #endregion

            #region Public Methods

            /// <summary>
            /// Moves to the next item in the selection
            /// </summary>
#if ODIN_INSPECTOR
            [Sirenix.OdinInspector.Button]
#else
        [Button]
#endif
            public void Next()
            {
                var total = Count;
                if (total == 0)
                {
                    Debug.LogWarning("Selector: No items to select");
                    return;
                }

                _currentIndex++;

                // Handle reaching the end of the array or count
                if (_currentIndex >= total)
                {
                    if (_loop)
                        _currentIndex = 0;
                    else
                        _currentIndex = total - 1;

                    // Notify that we've reached the end
                    OnFinished?.Invoke();
                }

                UpdateSelection();
            }

            /// <summary>
            /// Moves to the previous item in the selection
            /// </summary>
#if ODIN_INSPECTOR
            [Sirenix.OdinInspector.Button]
#else
        [Button]
#endif
            public void Previous()
            {
                var total = Count;
                if (total == 0)
                {
                    Debug.LogWarning("Selector: No items to select");
                    return;
                }

                _currentIndex--;

                // Handle reaching the beginning
                if (_currentIndex < 0)
                {
                    if (_loop)
                        _currentIndex = total - 1;
                    else
                        _currentIndex = 0;
                }

                UpdateSelection();
            }

            /// <summary>
            /// Gets the current selection index
            /// </summary>
            /// <returns>The current index</returns>
            public int GetCurrentIndex()
            {
                return _currentIndex;
            }

            /// <summary>
            /// Gets the total number of items
            /// </summary>
            /// <returns>The number of items</returns>
            public int GetCount()
            {
                return Count;
            }

            /// <summary>
            /// Sets the current selection index
            /// </summary>
            /// <param name="index">The index to set</param>
#if ODIN_INSPECTOR
            [Sirenix.OdinInspector.Button]
#else
        [Button]
#endif
            public void Set(int index)
            {
                var total = Count;
                if (total == 0)
                {
                    Debug.LogWarning("Selector: No items to select");
                    return;
                }

                if (_loop)
                    _currentIndex = (index % total + total) % total;
                else
                    _currentIndex = Mathf.Clamp(index, 0, total - 1);
                UpdateSelection();
            }

            /// <summary>
            /// Sets the selection to the last item
            /// </summary>
#if ODIN_INSPECTOR
            [Sirenix.OdinInspector.Button]
#else
        [Button]
#endif
            public void SetLast()
            {
                var total = Count;
                if (total == 0)
                {
                    Debug.LogWarning("Selector: No items to select");
                    return;
                }

                _currentIndex = total - 1;
                UpdateSelection();
            }

            /// <summary>
            /// Sets the selection to the first item
            /// </summary>
#if ODIN_INSPECTOR
            [Sirenix.OdinInspector.Button]
#else
        [Button]
#endif
            public void SetFirst()
            {
                var total = Count;
                if (total == 0)
                {
                    Debug.LogWarning("Selector: No items to select");
                    return;
                }

                _currentIndex = 0;
                UpdateSelection();
            }

            /// <summary>
            /// Toggles between fill mode and normal mode
            /// </summary>
            public void ToggleFillMode()
            {
                _fillMode = !_fillMode;
                UpdateSelection();
            }

            /// <summary>
            /// Gets the currently selected GameObject
            /// </summary>
            /// <returns>The selected GameObject or null if none is selected</returns>
            public GameObject GetSelectedItem()
            {
                if (!HasItems || _currentIndex < 0 || _currentIndex >= _items.Length)
                    return null;
                var idx = _currentIndex + _indexOffset;
                if (idx < 0 || idx >= _items.Length || _items[idx] == null)
                    return null;
                return _items[idx];
            }

            /// <summary>
            /// Checks if a specific index is valid
            /// </summary>
            /// <param name="index">The index to check</param>
            /// <returns>True if the index is valid, false otherwise</returns>
            public bool IsValidIndex(int index)
            {
                var total = Count;
                return total > 0 && index >= 0 && index < total;
            }

            /// <summary>
            /// Resets the selection to the start index
            /// </summary>
            public void Reset()
            {
                _currentIndex = 0;
                UpdateSelection();
            }

            /// <summary>
            /// Toggles the active state of a specific index (with offset)
            /// </summary>
            /// <param name="index">Index to toggle</param>
            /// <param name="state">Optional state to set (true to enable, false to disable, null to toggle)</param>
#if ODIN_INSPECTOR
            [Sirenix.OdinInspector.Button]
#else
        [Button]
#endif
            public void ToggleIndex(int index, bool? state = null)
            {
                if (!HasItems)
                {
                    Debug.LogWarning("Selector: No GameObjects to toggle");
                    return;
                }

                var effectiveIndex = index + _indexOffset;
                if (effectiveIndex < 0 || effectiveIndex >= _items.Length)
                {
                    Debug.LogWarning($"Selector: Index {index} with offset {_indexOffset} is out of bounds");
                    return;
                }

                if (_items[effectiveIndex] != null)
                    _items[effectiveIndex].SetActive(state ?? !_items[effectiveIndex].activeSelf);
            }

            #endregion
        }
    }
}