using System.Collections.Generic;
using UnityEngine;

namespace Neo.GridSystem.Merge
{
    /// <summary>
    ///     Describes one resolved grid merge group.
    /// </summary>
    public sealed class GridMergeGroupResult
    {
        public List<FieldCell> Cells = new();
        public List<Vector3Int> Positions = new();
        public List<FieldCell> ClearedCells = new();
        public FieldCell SeedCell;
        public FieldCell ResultCell;
        public int SourceContentId;
        public int ResultContentId;
    }
}
