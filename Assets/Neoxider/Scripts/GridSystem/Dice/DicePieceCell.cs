using System;
using UnityEngine;

namespace Neo.GridSystem.Dice
{
    /// <summary>
    ///     One local cell inside a dice placement piece.
    /// </summary>
    [Serializable]
    public struct DicePieceCell
    {
        public Vector3Int Offset;
        public int Value;

        public DicePieceCell(Vector3Int offset, int value)
        {
            Offset = offset;
            Value = value;
        }
    }
}
