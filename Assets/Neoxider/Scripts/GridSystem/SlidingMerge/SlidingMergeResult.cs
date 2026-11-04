using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.GridSystem.SlidingMerge
{
    /// <summary>
    ///     One tile movement produced by a slide. Views animate a tile from <see cref="From" /> to <see cref="To" />; for a
    ///     merge, the consumed tile travels from <see cref="MergedFrom" /> to the same target.
    /// </summary>
    [Serializable]
    public sealed class SlidingMergeStep
    {
        /// <summary>Original position of the tile that ends at <see cref="To" />.</summary>
        public Vector3Int From;

        /// <summary>Target cell of the move or merge.</summary>
        public Vector3Int To;

        /// <summary>Value of the tile that started at <see cref="From" />.</summary>
        public int Value;

        /// <summary>True when two tiles merged into <see cref="To" />.</summary>
        public bool IsMerge;

        /// <summary>Value written to <see cref="To" /> (the merged value for merges, otherwise <see cref="Value" />).</summary>
        public int ResultValue;

        /// <summary>Original position of the second (consumed) tile of a merge. Equals <see cref="From" /> when not a merge.</summary>
        public Vector3Int MergedFrom;

        /// <summary>Value of the consumed tile of a merge. Equals <see cref="Value" /> when not a merge.</summary>
        public int MergedValue;
    }

    /// <summary>
    ///     Result of one slide (or slide preview) over a <see cref="FieldGenerator" />.
    /// </summary>
    [Serializable]
    public sealed class SlidingMergeResult
    {
        /// <summary>True when at least one cell value changed (or would change for a preview).</summary>
        public bool Changed;

        /// <summary>Number of tiles that moved or merged.</summary>
        public int MoveCount;

        /// <summary>Number of merges.</summary>
        public int MergeCount;

        /// <summary>Sum of merged values, the classic 2048 score increment.</summary>
        public int ScoreDelta;

        /// <summary>Per-tile movements in line order; empty when nothing moved.</summary>
        public List<SlidingMergeStep> Steps = new();
    }
}
