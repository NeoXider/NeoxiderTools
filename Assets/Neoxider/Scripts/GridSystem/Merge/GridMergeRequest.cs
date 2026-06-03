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

        /// <summary>
        ///     When true (default) the resolver raises <c>FieldGenerator.OnCellStateChanged</c> for every cell it
        ///     mutates. Set to false when the caller wants to update extra state (e.g. occupancy) and raise a single,
        ///     consistent notification itself.
        /// </summary>
        public bool NotifyOnContentChanged = true;

        /// <summary>
        ///     Safety limit for chained cascades from a single seed. Forwarded to the generic merge core.
        /// </summary>
        public int MaxCascadeIterations = 128;

        /// <summary>
        ///     Builds the most common grid merge: connect equal content along the field movement rule and replace the
        ///     group with <c>content + step</c> at the seed cell, cascading from the result.
        /// </summary>
        /// <param name="seeds">Cells to start resolving from. Pass null to scan the whole board.</param>
        /// <param name="emptyContentId">Content id that represents an empty cell.</param>
        /// <param name="minGroupSize">Minimum connected equal cells required to merge.</param>
        /// <param name="step">Amount added to the source content to produce the merged content.</param>
        /// <param name="requireWalkable">Whether merges must stay on walkable cells.</param>
        public static GridMergeRequest Increment(
            IEnumerable<Vector3Int> seeds,
            int emptyContentId,
            int minGroupSize = 3,
            int step = 1,
            bool requireWalkable = true)
        {
            return new GridMergeRequest
            {
                Seeds = seeds,
                EmptyContentId = emptyContentId,
                MinGroupSize = minGroupSize,
                RequireEnabled = true,
                RequireWalkable = requireWalkable,
                IgnoreOccupied = true,
                Mutate = true,
                CascadeMode = Neo.Merge.MergeCascadeMode.FromResult,
                IsEmptyContent = value => value == emptyContentId,
                GetMergedContent = (value, count) => value + step,
                SelectResultCell = (group, seed) => seed
            };
        }
    }
}
