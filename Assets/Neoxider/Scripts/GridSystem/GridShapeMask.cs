using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.GridSystem
{
    /// <summary>
    ///     Defines how mask coordinates are interpreted.
    /// </summary>
    public enum GridShapeMaskMode
    {
        DisabledCells,
        EnabledCells
    }

    /// <summary>
    ///     Scriptable shape asset used to enable/disable selected grid cells.
    /// </summary>
    [CreateAssetMenu(
        fileName = "GridShapeMask",
        menuName = "Neoxider/GridSystem/Grid Shape Mask")]
    [NeoDoc("GridSystem/GridShapeMask.md")]
    public class GridShapeMask : ScriptableObject
    {
        [Tooltip("EnabledCells: only listed cells stay enabled (on a Custom grid they are the whole board). " +
                 "DisabledCells: listed cells are removed from the base shape.")]
        [SerializeField] private GridShapeMaskMode _mode = GridShapeMaskMode.EnabledCells;

        [Tooltip("Logical cell coordinates the mode applies to.")]
        [SerializeField] private List<Vector3Int> _cells = new();

        [NonSerialized] private HashSet<Vector3Int> _cache;

        /// <summary>
        ///     Mask interpretation mode. Changing it takes effect on the next field generation or
        ///     <see cref="FieldGenerator.ApplyShapeMask" />.
        /// </summary>
        /// <remarks>
        ///     Setting it on a project asset edits the asset itself (the change outlives Play Mode in the Editor and
        ///     affects every board that shares it). Change runtime shapes on a copy from <see cref="Create" /> or
        ///     <c>Object.Instantiate(asset)</c>.
        /// </remarks>
        public GridShapeMaskMode Mode
        {
            get => _mode;
            set => _mode = value;
        }

        /// <summary>
        ///     Raw mask coordinates list.
        /// </summary>
        public IReadOnlyList<Vector3Int> Cells => _cells;

        private void OnValidate()
        {
            _cache = null;
        }

        /// <summary>
        ///     Creates a runtime mask instance (not saved as an asset) for code-built boards.
        /// </summary>
        /// <param name="mode">Mask interpretation mode.</param>
        /// <param name="cells">Logical cell coordinates; null creates an empty mask.</param>
        public static GridShapeMask Create(GridShapeMaskMode mode, IEnumerable<Vector3Int> cells)
        {
            GridShapeMask mask = CreateInstance<GridShapeMask>();
            mask._mode = mode;
            mask.SetCells(cells);
            return mask;
        }

        /// <summary>
        ///     Replaces the mask coordinates. Takes effect on the next field generation or
        ///     <see cref="FieldGenerator.ApplyShapeMask" />.
        /// </summary>
        /// <remarks>
        ///     Called on a project asset this edits the asset itself, like the <see cref="Mode" /> setter; use a copy
        ///     for runtime-only shapes.
        /// </remarks>
        /// <param name="cells">Logical cell coordinates; null clears the mask.</param>
        public void SetCells(IEnumerable<Vector3Int> cells)
        {
            _cells ??= new List<Vector3Int>();
            _cells.Clear();
            if (cells != null)
            {
                _cells.AddRange(cells);
            }

            _cache = null;
        }

        /// <summary>
        ///     Returns true when <paramref name="pos" /> is listed in the mask.
        /// </summary>
        public bool Contains(Vector3Int pos)
        {
            EnsureCache();
            return _cache.Contains(pos);
        }

        /// <summary>
        ///     Evaluates whether a cell should be enabled after applying this mask.
        /// </summary>
        /// <param name="baseEnabled">Enabled value produced by base shape.</param>
        /// <param name="pos">Cell position.</param>
        /// <param name="gridType">Current grid type.</param>
        /// <returns>Final enabled state for the cell.</returns>
        public bool EvaluateCell(bool baseEnabled, Vector3Int pos, GridType gridType)
        {
            bool contains = Contains(pos);

            if (_mode == GridShapeMaskMode.DisabledCells)
            {
                return baseEnabled && !contains;
            }

            return gridType == GridType.Custom ? contains : baseEnabled && contains;
        }

        private void EnsureCache()
        {
            if (_cache != null)
            {
                return;
            }

            _cache = _cells != null ? new HashSet<Vector3Int>(_cells) : new HashSet<Vector3Int>();
        }
    }
}
