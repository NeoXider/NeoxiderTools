using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Complete, non-allocating sphere overlaps shared by the physics emitters.
    /// </summary>
    internal static class PhysicsOverlap
    {
        /// <summary>
        ///     Runs <see cref="Physics.OverlapSphereNonAlloc(Vector3, float, Collider[], int)" /> into
        ///     <paramref name="buffer" /> and doubles the buffer until the result is no longer saturated, so no collider
        ///     in range is silently dropped. The buffer is reused (and only ever grows) across calls.
        /// </summary>
        /// <param name="center">Sphere center in world space.</param>
        /// <param name="radius">Sphere radius.</param>
        /// <param name="layerMask">Layers to query.</param>
        /// <param name="buffer">Reusable result buffer; replaced by a larger one when a query fills it.</param>
        /// <param name="initialCapacity">Minimum buffer size when the buffer is created or too small.</param>
        /// <returns>Number of valid entries at the start of <paramref name="buffer" />.</returns>
        internal static int OverlapSphereComplete(Vector3 center, float radius, int layerMask, ref Collider[] buffer,
            int initialCapacity)
        {
            int capacity = Mathf.Max(1, initialCapacity);
            if (buffer == null || buffer.Length < capacity)
            {
                buffer = new Collider[capacity];
            }

            int count = Physics.OverlapSphereNonAlloc(center, radius, buffer, layerMask);
            while (count == buffer.Length)
            {
                buffer = new Collider[checked(buffer.Length * 2)];
                count = Physics.OverlapSphereNonAlloc(center, radius, buffer, layerMask);
            }

            return count;
        }
    }
}
