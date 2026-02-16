using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.GridSystem
{
    /// <summary>
    /// Defines how mask coordinates are interpreted.
    /// </summary>
    public enum GridShapeMaskMode
    {
        DisabledCells,
        EnabledCells
    }

    /// <summary>
    /// Scriptable shape asset used to enable/disable selected grid cells.
    /// </summary>
    [CreateAssetMenu(
        fileName = "GridShapeMask",
        menuName = "Neo/GridSystem/Grid Shape Mask")]
    public class GridShapeMask : ScriptableObject
    {
        [SerializeField] private GridShapeMaskMode _mode = GridShapeMaskMode.EnabledCells;
        [SerializeField] private List<Vector3Int> _cells = new();

        [NonSerialized] private HashSet<Vector3Int> _cache;

        /// <summary>
        /// Mask interpretation mode.
        /// </summary>
        public GridShapeMaskMode Mode => _mode;
        /// <summary>
        /// Raw mask coordinates list.
        /// </summary>
        public IReadOnlyList<Vector3Int> Cells => _cells;

        /// <summary>
        /// Evaluates whether a cell should be enabled after applying this mask.
        /// </summary>
        /// <param name="baseEnabled">Enabled value produced by base shape.</param>
        /// <param name="pos">Cell position.</param>
        /// <param name="gridType">Current grid type.</param>
        /// <returns>Final enabled state for the cell.</returns>
        public bool EvaluateCell(bool baseEnabled, Vector3Int pos, GridType gridType)
        {
            EnsureCache();
            bool contains = _cache.Contains(pos);

            if (_mode == GridShapeMaskMode.DisabledCells)
            {
                return baseEnabled && !contains;
            }

            return gridType == GridType.Custom ? contains : baseEnabled && contains;
        }

        private void OnValidate()
        {
            _cache = null;
        }

        private void EnsureCache()
        {
            if (_cache != null)
            {
                return;
            }

            _cache = new HashSet<Vector3Int>(_cells);
        }
    }
}
