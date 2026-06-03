using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.GridSystem.Dice
{
    /// <summary>
    ///     Describes a one-cell or two-cell dice piece before placement.
    /// </summary>
    [Serializable]
    public sealed class DicePiece
    {
        private readonly List<DicePieceCell> _cells = new();

        public IReadOnlyList<DicePieceCell> Cells => _cells;
        public bool IsPair => _cells.Count == 2;

        public DicePiece(IEnumerable<DicePieceCell> cells)
        {
            if (cells != null)
            {
                _cells.AddRange(cells);
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

        public DicePiece RotateClockwise()
        {
            if (!IsPair)
            {
                return Clone();
            }

            var rotated = new List<DicePieceCell>(_cells.Count);
            foreach (DicePieceCell cell in _cells)
            {
                Vector3Int offset = cell.Offset;
                rotated.Add(new DicePieceCell(new Vector3Int(offset.y, -offset.x, offset.z), cell.Value));
            }

            return new DicePiece(rotated);
        }

        public DicePiece RotateCounterClockwise()
        {
            if (!IsPair)
            {
                return Clone();
            }

            var rotated = new List<DicePieceCell>(_cells.Count);
            foreach (DicePieceCell cell in _cells)
            {
                Vector3Int offset = cell.Offset;
                rotated.Add(new DicePieceCell(new Vector3Int(-offset.y, offset.x, offset.z), cell.Value));
            }

            return new DicePiece(rotated);
        }

        public DicePiece Clone()
        {
            return new DicePiece(_cells);
        }
    }
}
