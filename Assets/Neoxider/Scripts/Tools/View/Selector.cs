using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

using UnityEngine;
using UnityEngine.Events;

namespace Neo
{
    namespace Tools
    {
        /// <summary>
        ///     A component that manages selection between multiple GameObjects, with support for different selection modes.
        ///     Useful for UI elements, inventory systems, or any scenario requiring sequential selection.
        /// </summary>
        [AddComponentMenu("Neoxider/" + "Tools/" + nameof(Selector))]
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
                    Debug.LogWarning("Selector: No items to select");
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

                if (HasItems)
                {
                    // Update GameObject active state based on selection
                    if (_fillMode)
                    {
                        for (int i = 0; i < _items.Length; i++)
                        {
                            if (_items[i] != null)
                            {
                                _items[i].SetActive(i <= effectiveIndex);
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < _items.Length; i++)
                        {
                            if (_items[i] != null)
                            {
                                _items[i].SetActive(i == effectiveIndex);
                            }
                        }
                    }
                }

                // In count-only mode, just invoke the event
                OnSelectionChanged?.Invoke(_currentIndex);
            }

            #endregion

            #region Serialized Fields

            [Tooltip("Sets the initial index when appearing")]
            public bool startOnAwake;

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

            [Tooltip("Allow effective index -1 (nothing selected). Useful for skins/empty state")] [SerializeField]
            private bool _allowEmptyEffectiveIndex = false;

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
            /// </summary>
            public int Count
            {
                get
                {
                    // If explicit virtual count is set (>0), use it (count mode)
                    if (_count > 0)
                    {
                        return _count;
                    }

                    // Otherwise, fall back to items length when items provided
                    if (_items != null && _items.Length > 0)
                    {
                        return _items.Length;
                    }

                    return 0;
                }
                set
                {
                    _count = value;
                    UpdateSelection();
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

            public int IndexWithOffset => _currentIndex + _indexOffset;

            public GameObject Item
            {
                get
                {
                    if (Value >= 0 && Value < Count)
                    {
                        return _items[Value];
                    }

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
                if (startOnAwake)
                {
                    Set(_startIndex);
                }
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
                    List<GameObject> childs = new();

                    foreach (Transform child in transform)
                    {
                        if (child.gameObject != gameObject)
                        {
                            childs.Add(child.gameObject);
                        }
                    }

                    _items = childs.ToArray();

                    // Log the number of items found
                    if (_items.Length > 0)
                    {
                        Debug.Log($"Selector: Auto-populated {_items.Length} items from children");
                    }
                    else
                    {
                        Debug.LogWarning("Selector: No child items found to populate");
                    }
                }

                // Update selection in editor if debug mode is enabled
                if (_changeDebug && _items != null)
                {
                    UpdateSelection();
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
                int total = Count;
                if (total == 0)
                {
                    Debug.LogWarning("Selector: No items to select");
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
                int total = Count;
                if (total == 0)
                {
                    Debug.LogWarning("Selector: No items to select");
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
                int total = Count;
                if (total == 0)
                {
                    Debug.LogWarning("Selector: No items to select");
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
            ///     Sets the selection to the last item
            /// </summary>
            [Button]
            public void SetLast()
            {
                int total = Count;
                if (total == 0)
                {
                    Debug.LogWarning("Selector: No items to select");
                    return;
                }

                _currentIndex = total - 1;
                UpdateSelection();
            }

            /// <summary>
            ///     Sets the selection to the first item
            /// </summary>
            [Button]
            public void SetFirst()
            {
                int total = Count;
                if (total == 0)
                {
                    Debug.LogWarning("Selector: No items to select");
                    return;
                }

                _currentIndex = 0;
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
            /// <returns>The selected GameObject or null if none is selected</returns>
            public GameObject GetSelectedItem()
            {
                if (!HasItems)
                {
                    return null;
                }

                if (_items.Length == 0)
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
                return total > 0 && index >= 0 && index < total;
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
                }
            }

            #endregion

            #region Helpers

            private (int min, int max) GetCurrentBounds()
            {
                int total = Count;
                int effMin = _allowEmptyEffectiveIndex ? -1 : 0;
                int min = effMin - _indexOffset;
                int max = total - 1 - _indexOffset;
                return (min, max);
            }

            #endregion
        }
    }
}