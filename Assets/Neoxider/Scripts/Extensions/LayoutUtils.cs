using System.Collections.Generic;
using UnityEngine;

namespace Neo.Extensions
{
    /// <summary>
    ///     A utility class for calculating position layouts for groups of objects.
    ///     These methods return collections of Vector3 points.
    /// </summary>
    public static class LayoutUtils
    {
        /// <summary>
        ///     Calculates points arranged in a line.
        /// </summary>
        /// <param name="count">The number of points to generate.</param>
        /// <param name="origin">The starting point of the line.</param>
        /// <param name="direction">The direction of the line.</param>
        /// <param name="spacing">The distance between each point.</param>
        /// <returns>A collection of points representing the line.</returns>
        public static IEnumerable<Vector3> GetLine(int count, Vector3 origin, Vector3 direction, float spacing)
        {
            for (var i = 0; i < count; i++) yield return origin + direction.normalized * (i * spacing);
        }

        /// <summary>
        ///     Calculates points arranged in a grid.
        /// </summary>
        /// <param name="count">The total number of points to generate.</param>
        /// <param name="origin">The starting point of the grid (e.g., top-left corner).</param>
        /// <param name="columns">The number of columns in the grid.</param>
        /// <param name="spacingX">The horizontal spacing between points.</param>
        /// <param name="spacingY">The vertical spacing between points.</param>
        /// <returns>A collection of points representing the grid.</returns>
        public static IEnumerable<Vector3> GetGrid(int count, Vector3 origin, int columns, float spacingX,
            float spacingY)
        {
            if (columns <= 0) columns = 1;

            for (var i = 0; i < count; i++)
            {
                var row = i / columns;
                var col = i % columns;
                yield return origin + new Vector3(col * spacingX, -row * spacingY, 0);
            }
        }

        /// <summary>
        ///     Calculates points arranged in a circle.
        /// </summary>
        /// <param name="count">The number of points to generate.</param>
        /// <param name="center">The center of the circle.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="rotationOffset">An optional offset in degrees to rotate the entire arrangement.</param>
        /// <returns>A collection of points representing the circle.</returns>
        public static IEnumerable<Vector3> GetCircle(int count, Vector3 center, float radius, float rotationOffset = 0)
        {
            if (count <= 0) yield break;

            var angleStep = 360f / count;
            for (var i = 0; i < count; i++)
            {
                var angle = i * angleStep + rotationOffset;
                var rad = angle * Mathf.Deg2Rad;
                yield return center + new Vector3(radius * Mathf.Cos(rad), radius * Mathf.Sin(rad), 0);
            }
        }

        #region 3D Layouts

        /// <summary>
        ///     Calculates points for a grid on a custom 3D plane.
        /// </summary>
        /// <param name="right">The vector defining the grid's horizontal direction.</param>
        /// <param name="up">The vector defining the grid's vertical direction.</param>
        public static IEnumerable<Vector3> GetGrid3D(int count, Vector3 origin, int columns, Vector3 right, Vector3 up,
            float spacingX, float spacingY)
        {
            if (columns <= 0) columns = 1;

            for (var i = 0; i < count; i++)
            {
                var row = i / columns;
                var col = i % columns;
                yield return origin + right.normalized * col * spacingX + up.normalized * row * spacingY;
            }
        }

        /// <summary>
        ///     Calculates points for a circle on a custom 3D plane defined by its normal.
        /// </summary>
        /// <param name="normal">The vector perpendicular to the circle's plane.</param>
        public static IEnumerable<Vector3> GetCircle3D(int count, Vector3 center, Vector3 normal, float radius,
            float rotationOffset = 0)
        {
            if (count <= 0) yield break;

            var rotation = Quaternion.FromToRotation(Vector3.forward, normal);
            var angleStep = 360f / count;

            for (var i = 0; i < count; i++)
            {
                var angle = i * angleStep + rotationOffset;
                var rad = angle * Mathf.Deg2Rad;
                var point = new Vector3(radius * Mathf.Cos(rad), radius * Mathf.Sin(rad), 0);
                yield return center + rotation * point;
            }
        }

        /// <summary>
        ///     Calculates points distributed on the surface of a sphere.
        /// </summary>
        /// <remarks>
        ///     Uses a simple spherical coordinate mapping. For more uniform distributions, advanced algorithms like Fibonacci
        ///     lattice are needed.
        /// </remarks>
        public static IEnumerable<Vector3> GetSphereSurface(int count, Vector3 center, float radius)
        {
            if (count <= 0) yield break;

            for (var i = 0; i < count; i++) yield return center + Random.onUnitSphere * radius;
        }

        #endregion

        #region Complex Layouts

        /// <summary>
        ///     Calculates points arranged in a 2D spiral.
        /// </summary>
        /// <param name="radiusStep">How much the radius grows with each full 360-degree turn.</param>
        /// <param name="angleStep">The angular separation between each point in degrees.</param>
        public static IEnumerable<Vector3> GetSpiral(int count, Vector3 center, float radiusStep, float angleStep)
        {
            float currentRadius = 0;
            float currentAngle = 0;

            for (var i = 0; i < count; i++)
            {
                var rad = currentAngle * Mathf.Deg2Rad;
                var point = new Vector3(currentRadius * Mathf.Cos(rad), currentRadius * Mathf.Sin(rad), 0);
                yield return center + point;

                currentAngle += angleStep;
                currentRadius = currentAngle / 360f * radiusStep;
            }
        }

        /// <summary>
        ///     Calculates points arranged along a sine wave.
        /// </summary>
        /// <param name="direction">The primary axis of the wave.</param>
        /// <param name="amplitude">The height of the wave.</param>
        /// <param name="frequency">How many wave crests appear over the distance of the line.</param>
        /// <param name="spacing">The distance between points along the primary axis.</param>
        public static IEnumerable<Vector3> GetSineWave(int count, Vector3 origin, Vector3 direction, float amplitude,
            float frequency, float spacing)
        {
            var up = Vector3.Cross(direction, Vector3.forward).normalized;
            if (up == Vector3.zero) up = Vector3.up; // Handle case where direction is aligned with forward

            for (var i = 0; i < count; i++)
            {
                var distance = i * spacing;
                var y = amplitude * Mathf.Sin(distance * frequency);
                yield return origin + direction.normalized * distance + up * y;
            }
        }

        #endregion
    }
}