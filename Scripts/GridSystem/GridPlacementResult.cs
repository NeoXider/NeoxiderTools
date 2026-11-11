using System.Collections.Generic;
using UnityEngine;

namespace Neo.GridSystem
{
    /// <summary>
    ///     Result of writing a footprint into FieldGenerator cells.
    /// </summary>
    public sealed class GridPlacementResult
    {
        public bool Placed;
        public string FailureReason;
        public List<FieldCell> Cells = new();
        public List<Vector3Int> Positions = new();
    }
}
