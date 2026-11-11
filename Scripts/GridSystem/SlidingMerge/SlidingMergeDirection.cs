using UnityEngine;

namespace Neo.GridSystem.SlidingMerge
{
    /// <summary>
    ///     Slide direction in logical grid space. <see cref="Backward" /> / <see cref="Forward" /> move along the depth
    ///     axis of 3D boards.
    /// </summary>
    public enum SlidingMergeDirection
    {
        Left,
        Right,
        Down,
        Up,
        Backward,
        Forward
    }

    /// <summary>
    ///     Conversion helpers for <see cref="SlidingMergeDirection" />.
    /// </summary>
    public static class SlidingMergeDirectionExtensions
    {
        /// <summary>
        ///     Returns the unit grid offset of the direction (for example <c>Left</c> = <c>(-1, 0, 0)</c>).
        /// </summary>
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
