using System.Collections.Generic;
using UnityEngine;

namespace Neo.GridSystem.Merge
{
    /// <summary>
    ///     Describes one resolved grid merge group.
    /// </summary>
    public sealed class GridMergeGroupResult
    {
        public List<FieldCell> Cells { get; } = new();
        public List<Vector3Int> Positions { get; } = new();
        public List<FieldCell> ClearedCells { get; } = new();
        public FieldCell SeedCell;
        public FieldCell ResultCell;
        public int SourceContentId;
        public int ResultContentId;
    }
}
