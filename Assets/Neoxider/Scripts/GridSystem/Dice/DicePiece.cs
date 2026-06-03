using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.GridSystem.Dice
{
    /// <summary>
    ///     Describes a multi-cell dice piece (single, pair or larger footprint) before placement.
    /// </summary>
    [Serializable]
    public sealed class DicePiece
    {
        [SerializeField] private List<DicePieceCell> _cells = new();

        public IReadOnlyList<DicePieceCell> Cells => MutableCells;
        public int CellCount => MutableCells.Count;
        public bool IsPair => MutableCells.Count == 2;

        private List<DicePieceCell> MutableCells
        {
            get
            {
                if (_cells == null)
                {
                    _cells = new List<DicePieceCell>();
                }

                return _cells;
            }
        }

        public DicePiece(IEnumerable<DicePieceCell> cells)
        {
            if (cells != null)
            {
                MutableCells.AddRange(cells);
            }
        }

        public static DicePiece Single(int value)
        {
            return new DicePiece(new[]
            {
                new DicePieceCell(Vector3Int.zero, value)
            });
        }

        public static DicePiece Pair(int firstValue, int secondValue)
        {
            return new DicePiece(new[]
            {
                new DicePieceCell(Vector3Int.zero, firstValue),
                new DicePieceCell(Vector3Int.right, secondValue)
            });
        }

        /// <summary>Rotates the footprint 90° clockwise around the anchor. Works for any cell count.</summary>
        public DicePiece RotateClockwise()
        {
            return Rotate(offset => new Vector3Int(offset.y, -offset.x, offset.z));
        }

        /// <summary>Rotates the footprint 90° counter-clockwise around the anchor. Works for any cell count.</summary>
        public DicePiece RotateCounterClockwise()
        {
            return Rotate(offset => new Vector3Int(-offset.y, offset.x, offset.z));
        }

        private DicePiece Rotate(Func<Vector3Int, Vector3Int> rotateOffset)
        {
            if (CellCount < 2)
            {
                return Clone();
            }

            IReadOnlyList<DicePieceCell> cells = Cells;
            var rotated = new List<DicePieceCell>(cells.Count);
            foreach (DicePieceCell cell in cells)
            {
                rotated.Add(new DicePieceCell(rotateOffset(cell.Offset), cell.Value));
            }

            return new DicePiece(rotated);
        }

        public DicePiece Clone()
        {
            return new DicePiece(Cells);
        }
    }
}
