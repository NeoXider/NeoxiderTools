using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A component that manages selection between multiple GameObjects, with support for different selection modes.
/// Useful for UI elements, inventory systems, or any scenario requiring sequential selection.
/// </summary>
[AddComponentMenu("UI/" + nameof(UISelector))]
public class UISelector : MonoBehaviour
{
    #region Serialized Fields

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
    public bool IsAtEnd => _currentIndex >= _items.Length - 1;

    /// <summary>
    /// Gets whether the selector is at the beginning of the items array
    /// </summary>
    public bool IsAtStart => _currentIndex <= 0;

    public int IndexWithOffset => _currentIndex + _indexOffset;

    #endregion

    #region Unity Methods

    private void Awake()
    {
    }

    private void Start()
    {
        // Ensure selection is applied at start
        UpdateSelection();
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
        if (_items == null || _items.Length == 0)
        {
            Debug.LogWarning("Selector: Cannot update selection - items array is null or empty");
            return;
        }

        // Calculate the effective index with offset
        var effectiveIndex = _currentIndex + _indexOffset;

        // Ensure effective index is within bounds
        if (effectiveIndex < -1)
            effectiveIndex = -1;
        else if (effectiveIndex >= _items.Length)
            effectiveIndex = _items.Length - 1;

        if (_fillMode)
        {
            // In fill mode, activate all items up to and including the current index
            for (var i = 0; i < _items.Length; i++)
                if (_items[i] != null)
                    _items[i].SetActive(i <= effectiveIndex);
        }
        else
        {
            // In normal mode, activate only the current item
            for (var i = 0; i < _items.Length; i++)
                if (_items[i] != null)
                    _items[i].SetActive(i == effectiveIndex);
        }

        // Notify listeners of selection change
        OnSelectionChanged?.Invoke(_currentIndex);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Moves to the next item in the selection
    /// </summary>
    public void Next()
    {
        if (_items == null || _items.Length == 0)
        {
            Debug.LogWarning("Selector: Cannot move to next - items array is null or empty");
            return;
        }

        _currentIndex++;

        // Handle reaching the end of the array
        if (_currentIndex >= _items.Length)
        {
            if (_loop)
                _currentIndex = 0;
            else
                _currentIndex = _items.Length - 1;

            // Notify that we've reached the end
            OnFinished?.Invoke();
        }

        UpdateSelection();
    }

    /// <summary>
    /// Moves to the previous item in the selection
    /// </summary>
    public void Previous()
    {
        if (_items == null || _items.Length == 0)
        {
            Debug.LogWarning("Selector: Cannot move to previous - items array is null or empty");
            return;
        }

        _currentIndex--;

        // Handle reaching the beginning of the array
        if (_currentIndex < 0)
        {
            if (_loop)
                _currentIndex = _items.Length - 1;
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
        return _items != null ? _items.Length : 0;
    }

    /// <summary>
    /// Sets the current selection index
    /// </summary>
    /// <param name="index">The index to set</param>
    public void Set(int index)
    {
        if (_items == null || _items.Length == 0)
        {
            Debug.LogWarning("Selector: Cannot set index - items array is null or empty");
            return;
        }

        _currentIndex = index;
        UpdateSelection();
    }

    /// <summary>
    /// Sets the selection to the last item
    /// </summary>
    public void SetLast()
    {
        if (_items == null || _items.Length == 0)
        {
            Debug.LogWarning("Selector: Cannot set to last - items array is null or empty");
            return;
        }

        _currentIndex = _items.Length - 1;
        UpdateSelection();
    }

    /// <summary>
    /// Sets the selection to the first item
    /// </summary>
    public void SetFirst()
    {
        if (_items == null || _items.Length == 0)
        {
            Debug.LogWarning("Selector: Cannot set to first - items array is null or empty");
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
        if (_items == null || _items.Length == 0 || _currentIndex < 0 || _currentIndex >= _items.Length)
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
        return _items != null && _items.Length > 0 && index >= 0 && index < _items.Length;
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
    public void ToggleIndex(int index, bool? state = null)
    {
        if (_items == null || _items.Length == 0)
        {
            Debug.LogWarning("Selector: Cannot toggle index - items array is null or empty");
            return;
        }

        // Calculate effective index with offset
        var effectiveIndex = index + _indexOffset;

        // Ensure effective index is within bounds
        if (effectiveIndex < 0 || effectiveIndex >= _items.Length)
        {
            Debug.LogWarning($"Selector: Index {index} with offset {_indexOffset} is out of bounds");
            return;
        }

        // Toggle or set specific state
        if (_items[effectiveIndex] != null)
            _items[effectiveIndex].SetActive(state ?? !_items[effectiveIndex].activeSelf);
    }

    #endregion
}