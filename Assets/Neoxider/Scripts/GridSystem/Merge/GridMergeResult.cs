using System.Collections.Generic;
using UnityEngine;

namespace Neo.GridSystem.Merge
{
    /// <summary>
    ///     FieldGenerator-friendly merge result with cells and positions.
    /// </summary>
    public sealed class GridMergeResult
    {
        public List<GridMergeGroupResult> Groups = new();
        public List<FieldCell> ChangedCells = new();
        public List<Vector3Int> ChangedPositions = new();

        public bool HasChanges => Groups.Count > 0;
    }
}
