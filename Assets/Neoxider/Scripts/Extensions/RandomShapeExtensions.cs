using UnityEngine;

namespace Neo.Extensions
{
    /// <summary>
    ///     Extension methods for generating random points within or on various shapes.
    /// </summary>
    public static class RandomShapeExtensions
    {
        #region Bounds

        /// <summary>
        ///     Gets a random point within the given bounds.
        /// </summary>
        public static Vector3 RandomPointInBounds(this Bounds bounds)
        {
            return new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                Random.Range(bounds.min.z, bounds.max.z)
            );
        }

        /// <summary>
        ///     Gets a random point on the surface of the given bounds.
        /// </summary>
        public static Vector3 RandomPointOnBounds(this Bounds bounds)
        {
            var point = bounds.RandomPointInBounds();
            var axis = Random.Range(0, 3);
            var side = Random.Range(0, 2) == 0 ? bounds.min[axis] : bounds.max[axis];
            point[axis] = side;
            return point;
        }

        #endregion

        #region Circle

        /// <summary>
        ///     Gets a random point within a 2D circle.
        /// </summary>
        public static Vector2 RandomPointInCircle(this Circle circle)
        {
            // Using sqrt for uniform distribution
            var angle = Random.Range(0, 2 * Mathf.PI);
            var r = circle.radius * Mathf.Sqrt(Random.value);
            return circle.center + new Vector2(r * Mathf.Cos(angle), r * Mathf.Sin(angle));
        }

        /// <summary>
        ///     Gets a random point on the circumference of a 2D circle.
        /// </summary>
        public static Vector2 RandomPointOnCircle(this Circle circle)
        {
            var angle = Random.Range(0, 2 * Mathf.PI);
            return circle.center + new Vector2(circle.radius * Mathf.Cos(angle), circle.radius * Mathf.Sin(angle));
        }

        #endregion

        #region Sphere

        /// <summary>
        ///     Gets a random point within a sphere.
        /// </summary>
        public static Vector3 RandomPointInSphere(this Sphere sphere)
        {
            return sphere.center + Random.insideUnitSphere * sphere.radius;
        }

        /// <summary>
        ///     Gets a random point on the surface of a sphere.
        /// </summary>
        public static Vector3 RandomPointOnSphere(this Sphere sphere)
        {
            return sphere.center + Random.onUnitSphere * sphere.radius;
        }

        #endregion

        #region Collider Extensions

        /// <summary>
        ///     Gets a random point within the bounds of a 3D collider.
        /// </summary>
        public static Vector3 RandomPointInBounds(this Collider collider)
        {
            return collider.bounds.RandomPointInBounds();
        }

        /// <summary>
        ///     Gets a random point within the bounds of a 2D collider.
        /// </summary>
        public static Vector2 RandomPointInBounds(this Collider2D collider)
        {
            return collider.bounds.RandomPointInBounds();
        }

        #endregion
    }
}