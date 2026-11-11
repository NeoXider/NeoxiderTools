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

        /// <summary>Applies a position delta in the configured movement plane (world units per call).</summary>
        void MoveDelta(Vector2 delta);

        /// <summary>Moves to a point in the configured movement plane, preserving the perpendicular coordinate.</summary>
        void MoveToPoint(Vector2 worldTarget);
    }
}
