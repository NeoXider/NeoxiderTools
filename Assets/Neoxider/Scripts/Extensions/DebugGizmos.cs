using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Neo.Extensions
{
    /// <summary>
    ///     A utility class for drawing common debug gizmos in the Unity editor.
    ///     Call these methods from MonoBehaviour's OnDrawGizmos or OnDrawGizmosSelected.
    /// </summary>
    public static class DebugGizmos
    {
        /// <summary>
        ///     Draws the outline of a Bounds object.
        /// </summary>
        /// <param name="bounds">The bounds to draw.</param>
        /// <param name="changeColor">
        ///     If true, the gizmo color will be temporarily changed. If false, uses the current
        ///     Gizmos.color.
        /// </param>
        /// <param name="color">The color to use for the gizmo. Defaults to red if not specified.</param>
        public static void DrawBounds(Bounds bounds, bool changeColor = true, Color? color = null)
        {
            if (!changeColor)
            {
                Gizmos.DrawWireCube(bounds.center, bounds.size);
                return;
            }

            var originalColor = Gizmos.color;
            Gizmos.color = color ?? Color.red;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
            Gizmos.color = originalColor;
        }

        /// <summary>
        ///     Draws a sphere at an average position.
        /// </summary>
        /// <param name="center">The center position to draw the sphere at.</param>
        /// <param name="changeColor">
        ///     If true, the gizmo color will be temporarily changed. If false, uses the current
        ///     Gizmos.color.
        /// </param>
        /// <param name="color">The color of the sphere. Defaults to red if not specified.</param>
        /// <param name="radius">The radius of the sphere.</param>
        public static void DrawAveragePosition(Vector3 center, bool changeColor = true, Color? color = null,
            float radius = 0.5f)
        {
            if (!changeColor)
            {
                Gizmos.DrawWireSphere(center, radius);
                return;
            }

            var originalColor = Gizmos.color;
            Gizmos.color = color ?? Color.red;
            Gizmos.DrawWireSphere(center, radius);
            Gizmos.color = originalColor;
        }

        /// <summary>
        ///     Draws a line from a starting point to the position of the closest GameObject.
        /// </summary>
        /// <param name="from">The starting point of the line.</param>
        /// <param name="closest">The target GameObject. If null, nothing is drawn.</param>
        /// <param name="changeColor">
        ///     If true, the gizmo color will be temporarily changed. If false, uses the current
        ///     Gizmos.color.
        /// </param>
        /// <param name="color">The color of the line. Defaults to red if not specified.</param>
        public static void DrawLineToClosest(Vector3 from, GameObject closest, bool changeColor = true,
            Color? color = null)
        {
            if (closest == null) return;

            if (!changeColor)
            {
                Gizmos.DrawLine(from, closest.transform.position);
                return;
            }

            var originalColor = Gizmos.color;
            Gizmos.color = color ?? Color.red;
            Gizmos.DrawLine(from, closest.transform.position);
            Gizmos.color = originalColor;
        }

        /// <summary>
        ///     Draws lines from a starting point to a collection of target GameObjects.
        /// </summary>
        /// <param name="from">The starting point for the lines.</param>
        /// <param name="targets">The collection of GameObjects to draw lines to.</param>
        /// <param name="changeColor">
        ///     If true, the gizmo color will be temporarily changed. If false, uses the current
        ///     Gizmos.color.
        /// </param>
        /// <param name="color">The color of the lines. Defaults to red if not specified.</param>
        public static void DrawConnections(Vector3 from, IEnumerable<GameObject> targets, bool changeColor = true,
            Color? color = null)
        {
            if (targets == null) return;

            if (!changeColor)
            {
                foreach (var target in targets.Where(t => t != null)) Gizmos.DrawLine(from, target.transform.position);
                return;
            }

            var originalColor = Gizmos.color;
            Gizmos.color = color ?? Color.red;

            foreach (var target in targets.Where(t => t != null)) Gizmos.DrawLine(from, target.transform.position);

            Gizmos.color = originalColor;
        }
    }
}