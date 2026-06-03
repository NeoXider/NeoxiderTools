using System.Collections.Generic;
using Neo.GridSystem.Merge;
using UnityEngine;

namespace Neo.GridSystem.Dice
{
    /// <summary>
    ///     Result of a dice placement and its follow-up merges.
    /// </summary>
    public sealed class DicePlacementResult
    {
        public bool Placed;
        public List<Vector3Int> PlacedPositions = new();
        public GridMergeResult MergeResult = new();
    }
}
