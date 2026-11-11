using System;
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
            Vector3 point = LayoutUtils.GetCircle(totalCount, center, radius, rotationOffset).ElementAtOrDefault(index);
            if (element != null)
            {
                element.position = point;
            }
        }

        #endregion

        /// <summary>
        ///     Materializes the source once so counting and positioning use the same transforms.
        /// </summary>
        private static void ApplyPositions(IEnumerable<Transform> elements, Func<int, IEnumerable<Vector3>> createPoints)
        {
            if (elements == null)
            {
                return;
            }

            List<Transform> elementList = elements.ToList();
            List<Vector3> pointList = createPoints(elementList.Count).ToList();

            int count = Mathf.Min(elementList.Count, pointList.Count);

            for (int i = 0; i < count; i++)
            {
                if (elementList[i] != null)
                {
                    elementList[i].position = pointList[i];
                }
            }
        }

        #region Collection Arrangements

        /// <summary>
        ///     Arranges a collection of transforms in a line.
        /// </summary>
        public static void ArrangeInLine(this IEnumerable<Transform> elements, Vector3 origin, Vector3 direction,
            float spacing)
        {
            ApplyPositions(elements, count => LayoutUtils.GetLine(count, origin, direction, spacing));
        }

        /// <summary>
        ///     Arranges a collection of transforms in a 2D grid on the XY plane.
        /// </summary>
        public static void ArrangeInGrid(this IEnumerable<Transform> elements, Vector3 origin, int columns,
            float spacingX, float spacingY)
        {
            ApplyPositions(elements, count => LayoutUtils.GetGrid(count, origin, columns, spacingX, spacingY));
        }

        /// <summary>
        ///     Arranges a collection of transforms in a 2D circle on the XY plane.
        /// </summary>
        public static void ArrangeInCircle(this IEnumerable<Transform> elements, Vector3 center, float radius,
            float rotationOffset = 0)
        {
            ApplyPositions(elements, count => LayoutUtils.GetCircle(count, center, radius, rotationOffset));
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
            ApplyPositions(elements,
                count => LayoutUtils.GetGrid3D(count, origin, columns, right, up, spacingX, spacingY));
        }

        /// <summary>
        ///     Arranges a collection of transforms in a circle on a custom 3D plane.
        /// </summary>
        public static void ArrangeInCircle3D(this IEnumerable<Transform> elements, Vector3 center, Vector3 normal,
            float radius, float rotationOffset = 0)
        {
            ApplyPositions(elements, count => LayoutUtils.GetCircle3D(count, center, normal, radius, rotationOffset));
        }

        /// <summary>
        ///     Arranges a collection of transforms on the surface of a sphere.
        /// </summary>
        public static void ArrangeOnSphereSurface(this IEnumerable<Transform> elements, Vector3 center, float radius)
        {
            ApplyPositions(elements, count => LayoutUtils.GetSphereSurface(count, center, radius));
        }

        #endregion

        #region Complex Layouts

        /// <summary>
        ///     Arranges a collection of transforms in a 2D spiral.
        /// </summary>
        public static void ArrangeInSpiral(this IEnumerable<Transform> elements, Vector3 center, float radiusStep,
            float angleStep)
        {
            ApplyPositions(elements, count => LayoutUtils.GetSpiral(count, center, radiusStep, angleStep));
        }

        /// <summary>
        ///     Arranges a collection of transforms along a sine wave.
        /// </summary>
        public static void ArrangeOnSineWave(this IEnumerable<Transform> elements, Vector3 origin, Vector3 direction,
            float amplitude, float frequency, float spacing)
        {
            ApplyPositions(elements,
                count => LayoutUtils.GetSineWave(count, origin, direction, amplitude, frequency, spacing));
        }

        #endregion
    }
}
