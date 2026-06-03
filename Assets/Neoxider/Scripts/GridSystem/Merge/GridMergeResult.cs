using System.Collections.Generic;
using UnityEngine;

namespace Neo.GridSystem.Merge
{
    /// <summary>
    ///     FieldGenerator-friendly merge result with cells and positions.
    /// </summary>
    public sealed class GridMergeResult
    {
        public List<GridMergeGroupResult> Groups { get; } = new();
        public List<FieldCell> ChangedCells { get; } = new();
        public List<Vector3Int> ChangedPositions { get; } = new();

        /// <summary>
        ///     True when a cascade was stopped by the safety limit (see <see cref="GridMergeRequest.MaxCascadeIterations" />).
        /// </summary>
        public bool CascadeLimitReached { get; set; }

        public bool HasChanges => Groups.Count > 0;
    }
}
