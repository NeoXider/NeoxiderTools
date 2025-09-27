using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Neo.Extensions
{
    /// <summary>
    ///     Extension methods for procedurally arranging collections of Transforms.
    /// </summary>
    public static class LayoutExtensions
    {
        #region Single Element Arrangements

        /// <summary>
        ///     Positions and rotates a single transform on a circular path.
        /// </summary>
        /// <param name="element">The transform to position.</param>
        /// <param name="center">The center of the circle.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="index">The zero-based index of the element in the arrangement.</param>
        /// <param name="totalCount">The total number of elements in the arrangement.</param>
        /// <param name="rotationOffset">An optional offset in degrees to rotate the entire arrangement.</param>
        public static void ArrangeInCircle(this Transform element, Vector3 center, float radius, int index,
            int totalCount, float rotationOffset = 0)
        {
            var point = LayoutUtils.GetCircle(totalCount, center, radius, rotationOffset).ElementAtOrDefault(index);
            if (element != null) element.position = point;
        }

        #endregion

        /// <summary>
        ///     A helper method to apply a list of positions to a list of transforms.
        /// </summary>
        private static void ApplyPositions(IEnumerable<Transform> elements, IEnumerable<Vector3> points)
        {
            if (elements == null || points == null) return;

            var elementList = elements.ToList();
            var pointList = points.ToList();

            var count = Mathf.Min(elementList.Count, pointList.Count);

            for (var i = 0; i < count; i++)
                if (elementList[i] != null)
                    elementList[i].position = pointList[i];
        }

        #region Collection Arrangements

        /// <summary>
        ///     Arranges a collection of transforms in a line.
        /// </summary>
        public static void ArrangeInLine(this IEnumerable<Transform> elements, Vector3 origin, Vector3 direction,
            float spacing)
        {
            var points = LayoutUtils.GetLine(elements.Count(), origin, direction, spacing);
            ApplyPositions(elements, points);
        }

        /// <summary>
        ///     Arranges a collection of transforms in a 2D grid on the XY plane.
        /// </summary>
        public static void ArrangeInGrid(this IEnumerable<Transform> elements, Vector3 origin, int columns,
            float spacingX, float spacingY)
        {
            var points = LayoutUtils.GetGrid(elements.Count(), origin, columns, spacingX, spacingY);
            ApplyPositions(elements, points);
        }

        /// <summary>
        ///     Arranges a collection of transforms in a 2D circle on the XY plane.
        /// </summary>
        public static void ArrangeInCircle(this IEnumerable<Transform> elements, Vector3 center, float radius,
            float rotationOffset = 0)
        {
            var points = LayoutUtils.GetCircle(elements.Count(), center, radius, rotationOffset);
            ApplyPositions(elements, points);
        }

        /// <summary>
        ///     Arranges a collection of transforms in a circle around this pivot transform.
        /// </summary>
        public static void ArrangeInCircle(this Transform pivot, IEnumerable<Transform> elements, float radius,
            float rotationOffset = 0)
        {
            elements.ArrangeInCircle(pivot.position, radius, rotationOffset);
        }

        #endregion

        #region 3D Layouts

        /// <summary>
        ///     Arranges a collection of transforms in a grid on a custom 3D plane.
        /// </summary>
        public static void ArrangeInGrid3D(this IEnumerable<Transform> elements, Vector3 origin, int columns,
            Vector3 right, Vector3 up, float spacingX, float spacingY)
        {
            var points = LayoutUtils.GetGrid3D(elements.Count(), origin, columns, right, up, spacingX, spacingY);
            ApplyPositions(elements, points);
        }

        /// <summary>
        ///     Arranges a collection of transforms in a circle on a custom 3D plane.
        /// </summary>
        public static void ArrangeInCircle3D(this IEnumerable<Transform> elements, Vector3 center, Vector3 normal,
            float radius, float rotationOffset = 0)
        {
            var points = LayoutUtils.GetCircle3D(elements.Count(), center, normal, radius, rotationOffset);
            ApplyPositions(elements, points);
        }

        /// <summary>
        ///     Arranges a collection of transforms on the surface of a sphere.
        /// </summary>
        public static void ArrangeOnSphereSurface(this IEnumerable<Transform> elements, Vector3 center, float radius)
        {
            var points = LayoutUtils.GetSphereSurface(elements.Count(), center, radius);
            ApplyPositions(elements, points);
        }

        #endregion

        #region Complex Layouts

        /// <summary>
        ///     Arranges a collection of transforms in a 2D spiral.
        /// </summary>
        public static void ArrangeInSpiral(this IEnumerable<Transform> elements, Vector3 center, float radiusStep,
            float angleStep)
        {
            var points = LayoutUtils.GetSpiral(elements.Count(), center, radiusStep, angleStep);
            ApplyPositions(elements, points);
        }

        /// <summary>
        ///     Arranges a collection of transforms along a sine wave.
        /// </summary>
        public static void ArrangeOnSineWave(this IEnumerable<Transform> elements, Vector3 origin, Vector3 direction,
            float amplitude, float frequency, float spacing)
        {
            var points = LayoutUtils.GetSineWave(elements.Count(), origin, direction, amplitude, frequency, spacing);
            ApplyPositions(elements, points);
        }

        #endregion
    }
}