using UnityEngine;

namespace Neo.Extensions
{
    public static class ScreenExtensions
    {
        /// <summary>
        ///     Checks if a point is on screen.
        /// </summary>
        /// <param name="position">Position to check.</param>
        /// <param name="camera">Camera to check with (if null, uses Camera.main).</param>
        /// <returns>True if the point is on screen.</returns>
        public static bool IsOnScreen(this Vector3 position, Camera camera = null)
        {
            camera = camera ?? Camera.main;
            if (camera == null) return false;

            var viewportPoint = camera.WorldToViewportPoint(position);
            return viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
                   viewportPoint.y >= 0 && viewportPoint.y <= 1 &&
                   viewportPoint.z >= 0;
        }

        /// <summary>
        ///     Checks if a point is out of screen bounds.
        /// </summary>
        /// <param name="position">Position to check.</param>
        /// <param name="camera">Camera to check with (if null, uses Camera.main).</param>
        /// <returns>True if the point is out of screen bounds.</returns>
        public static bool IsOutOfScreen(this Vector3 position, Camera camera = null)
        {
            return !position.IsOnScreen(camera);
        }

        /// <summary>
        ///     Checks if a point is out of screen bounds on a specific side.
        /// </summary>
        /// <param name="position">Position to check.</param>
        /// <param name="side">Screen side to check.</param>
        /// <param name="camera">Camera to check with (if null, uses Camera.main).</param>
        /// <returns>True if the point is out of bounds on the specified side.</returns>
        public static bool IsOutOfScreenSide(this Vector3 position, ScreenEdge side, Camera camera = null)
        {
            camera = camera ?? Camera.main;
            if (camera == null) return false;

            var viewportPoint = camera.WorldToViewportPoint(position);

            return side switch
            {
                ScreenEdge.Left => viewportPoint.x < 0,
                ScreenEdge.Right => viewportPoint.x > 1,
                ScreenEdge.Bottom => viewportPoint.y < 0,
                ScreenEdge.Top => viewportPoint.y > 1,
                ScreenEdge.Front => viewportPoint.z < 0,
                ScreenEdge.Back => viewportPoint.z > camera.farClipPlane,
                _ => false
            };
        }

        /// <summary>
        ///     Gets the closest point on screen edge to the specified position.
        /// </summary>
        /// <param name="position">Source position.</param>
        /// <param name="camera">Camera to check with (if null, uses Camera.main).</param>
        /// <returns>Closest point on screen edge.</returns>
        public static Vector3 GetClosestScreenEdgePoint(this Vector3 position, Camera camera = null)
        {
            camera = camera ?? Camera.main;
            if (camera == null) return position;

            var viewportPoint = camera.WorldToViewportPoint(position);

            if (position.IsOnScreen(camera)) return position;

            viewportPoint.x = Mathf.Clamp01(viewportPoint.x);
            viewportPoint.y = Mathf.Clamp01(viewportPoint.y);
            viewportPoint.z = Mathf.Clamp(viewportPoint.z, 0, camera.farClipPlane);

            return camera.ViewportToWorldPoint(viewportPoint);
        }

        /// <summary>
        ///     Gets a world position at a specified edge of the screen.
        /// </summary>
        /// <param name="camera">The camera to use.</param>
        /// <param name="edge">The screen edge to get the position from.</param>
        /// <param name="offset">An optional offset from the edge point.</param>
        /// <param name="depth">The distance from the camera's near clip plane.</param>
        /// <returns>The calculated world position.</returns>
        public static Vector3 GetWorldPositionAtScreenEdge(this Camera camera,
            ScreenEdge edge,
            Vector2 offset = default,
            float depth = 0f)
        {
            if (camera == null) camera = Camera.main;
            if (camera == null)
            {
                Debug.LogError("No camera available!");
                return Vector3.zero;
            }

            var screenPosition = GetEdgePosition(edge) + offset;
            return camera.ScreenPointToWorldPosition(screenPosition, depth);
        }

        /// <summary>
        ///     Gets the world-space bounds of the camera's viewport at a specific distance.
        /// </summary>
        /// <param name="camera">The camera to get the bounds from.</param>
        /// <param name="distance">The distance from the camera to calculate the bounds at.</param>
        /// <returns>A Bounds object representing the viewable area in world space.</returns>
        public static Bounds GetWorldScreenBounds(this Camera camera, float distance)
        {
            if (camera == null) camera = Camera.main;
            if (camera == null) return new Bounds(Vector3.zero, Vector3.zero);

            var lowerLeft = camera.ViewportToWorldPoint(new Vector3(0, 0, distance));
            var upperRight = camera.ViewportToWorldPoint(new Vector3(1, 1, distance));

            var bounds = new Bounds();
            bounds.SetMinMax(lowerLeft, upperRight);
            return bounds;
        }

        private static Vector2 GetEdgePosition(ScreenEdge edge)
        {
            return edge switch
            {
                ScreenEdge.Left => new Vector2(0, Screen.height * 0.5f),
                ScreenEdge.Right => new Vector2(Screen.width, Screen.height * 0.5f),
                ScreenEdge.Top => new Vector2(Screen.width * 0.5f, Screen.height),
                ScreenEdge.Bottom => new Vector2(Screen.width * 0.5f, 0),
                ScreenEdge.TopLeft => new Vector2(0, Screen.height),
                ScreenEdge.TopRight => new Vector2(Screen.width, Screen.height),
                ScreenEdge.BottomLeft => Vector2.zero,
                ScreenEdge.BottomRight => new Vector2(Screen.width, 0),
                ScreenEdge.Center => new Vector2(Screen.width * 0.5f, Screen.height * 0.5f),
                _ => Vector2.zero
            };
        }

        private static Vector3 ScreenPointToWorldPosition(this Camera camera, Vector2 screenPoint, float depth)
        {
            return camera.ScreenToWorldPoint(new Vector3(
                screenPoint.x,
                screenPoint.y,
                camera.orthographic ? depth : camera.nearClipPlane + depth
            ));
        }
    }
}