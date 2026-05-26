using UnityEngine;

namespace Neo.GridSystem.SlidingMerge
{
    public enum SlidingMergeDirection
    {
        Left,
        Right,
        Down,
        Up,
        Backward,
        Forward
    }

    public static class SlidingMergeDirectionExtensions
    {
        public static Vector3Int ToVector(this SlidingMergeDirection direction)
        {
            switch (direction)
            {
                case SlidingMergeDirection.Left:
                    return Vector3Int.left;
                case SlidingMergeDirection.Right:
                    return Vector3Int.right;
                case SlidingMergeDirection.Down:
                    return Vector3Int.down;
                case SlidingMergeDirection.Up:
                    return Vector3Int.up;
                case SlidingMergeDirection.Backward:
                    return new Vector3Int(0, 0, -1);
                case SlidingMergeDirection.Forward:
                    return new Vector3Int(0, 0, 1);
                default:
                    return Vector3Int.zero;
            }
        }
    }
}
