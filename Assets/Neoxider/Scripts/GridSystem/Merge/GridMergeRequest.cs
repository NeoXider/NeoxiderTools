using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.GridSystem.Merge
{
    /// <summary>
    ///     Grid-facing merge request for FieldGenerator cell content.
    /// </summary>
    public sealed class GridMergeRequest
    {
        public IEnumerable<Vector3Int> Seeds;
        public IEnumerable<Vector3Int> Directions;
        public int EmptyContentId;
        public int MinGroupSize = 3;
        public bool RequireEnabled = true;
        public bool RequireWalkable = true;
        public bool IgnoreOccupied = true;
        public bool Mutate = true;
        public Neo.Merge.MergeCascadeMode CascadeMode = Neo.Merge.MergeCascadeMode.FromResult;
        public Func<FieldCell, bool> CustomCellFilter;
        public Func<int, bool> IsEmptyContent;
        public Func<int, int, bool> AreContentEqual;
        public Func<IReadOnlyList<FieldCell>, FieldCell, FieldCell> SelectResultCell;
        public Func<int, int, int> GetMergedContent;
    }
}
