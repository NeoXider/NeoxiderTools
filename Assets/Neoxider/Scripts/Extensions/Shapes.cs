using UnityEngine;

namespace Neo.Extensions
{
    /// <summary>
    ///     Defines a 2D Circle with a center and radius.
    /// </summary>
    public struct Circle
    {
        /// <summary>
        ///     Circle center in 2D space.
        /// </summary>
        public Vector2 center;

        /// <summary>
        ///     Circle radius.
        /// </summary>
        public float radius;

        /// <summary>
        ///     Creates a circle with specified center and radius.
        /// </summary>
        public Circle(Vector2 center, float radius)
        {
            this.center = center;
            this.radius = radius;
        }
    }

    /// <summary>
    ///     Defines a 3D Sphere with a center and radius.
    /// </summary>
    public struct Sphere
    {
        /// <summary>
        ///     Sphere center in 3D space.
        /// </summary>
        public Vector3 center;

        /// <summary>
        ///     Sphere radius.
        /// </summary>
        public float radius;

        /// <summary>
        ///     Creates a sphere with specified center and radius.
        /// </summary>
        public Sphere(Vector3 center, float radius)
        {
            this.center = center;
            this.radius = radius;
        }
    }
}