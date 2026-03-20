using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Base contract for movement components.
    /// </summary>
    public interface IMover
    {
        /// <summary>Whether the object is currently moving.</summary>
        bool IsMoving { get; }

        /// <summary>Applies a world-space position delta (units per frame).</summary>
        void MoveDelta(Vector2 delta);

        /// <summary>Moves towards a world-space target point.</summary>
        void MoveToPoint(Vector2 worldTarget);
    }
}
