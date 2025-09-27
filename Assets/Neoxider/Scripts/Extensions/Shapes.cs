using UnityEngine;

namespace Neo.Extensions
{
    /// <summary>
    ///     Defines a 2D Circle with a center and radius.
    /// </summary>
    public struct Circle
    {
        public Vector2 center;
        public float radius;

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
        public Vector3 center;
        public float radius;

        public Sphere(Vector3 center, float radius)
        {
            this.center = center;
            this.radius = radius;
        }
    }
}