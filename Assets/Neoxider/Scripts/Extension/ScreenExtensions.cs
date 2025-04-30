using UnityEngine;

public enum ScreenEdge
{
    Left,
    Right,
    Top,
    Bottom,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    Center,

    Front,
    Back
}

namespace Neo
{
    public static class ScreenExtensions
    {
        /// <summary>
        /// Checks if a point is on screen
        /// </summary>
        /// <param name="position">Position to check</param>
        /// <param name="camera">Camera to check with (if null, uses Camera.main)</param>
        /// <returns>True if the point is on screen</returns>
        public static bool IsOnScreen(this Vector3 position, Camera camera = null)
        {
            camera = camera ?? Camera.main;
            if (camera == null) return false;

            Vector3 viewportPoint = camera.WorldToViewportPoint(position);
            return viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
                   viewportPoint.y >= 0 && viewportPoint.y <= 1 &&
                   viewportPoint.z >= 0;
        }

        /// <summary>
        /// Checks if a point is out of screen bounds
        /// </summary>
        /// <param name="position">Position to check</param>
        /// <param name="camera">Camera to check with (if null, uses Camera.main)</param>
        /// <returns>True if the point is out of screen bounds</returns>
        public static bool IsOutOfScreen(this Vector3 position, Camera camera = null)
        {
            return !position.IsOnScreen(camera);
        }

        /// <summary>
        /// Checks if a point is out of screen bounds on a specific side
        /// </summary>
        /// <param name="position">Position to check</param>
        /// <param name="side">Screen side to check</param>
        /// <param name="camera">Camera to check with (if null, uses Camera.main)</param>
        /// <returns>True if the point is out of bounds on the specified side</returns>
        public static bool IsOutOfScreenSide(this Vector3 position, ScreenEdge side, Camera camera = null)
        {
            camera = camera ?? Camera.main;
            if (camera == null) return false;

            Vector3 viewportPoint = camera.WorldToViewportPoint(position);

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
        /// Gets the closest point on screen edge to the specified position
        /// </summary>
        /// <param name="position">Source position</param>
        /// <param name="camera">Camera to check with (if null, uses Camera.main)</param>
        /// <returns>Closest point on screen edge</returns>
        public static Vector3 GetClosestScreenEdgePoint(this Vector3 position, Camera camera = null)
        {
            camera = camera ?? Camera.main;
            if (camera == null) return position;

            Vector3 viewportPoint = camera.WorldToViewportPoint(position);

            // If point is already on screen, return it
            if (position.IsOnScreen(camera))
            {
                return position;
            }

            // Find the closest point on screen edge
            viewportPoint.x = Mathf.Clamp01(viewportPoint.x);
            viewportPoint.y = Mathf.Clamp01(viewportPoint.y);
            viewportPoint.z = Mathf.Clamp(viewportPoint.z, 0, camera.farClipPlane);

            return camera.ViewportToWorldPoint(viewportPoint);
        }

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

            Vector2 screenPosition = GetEdgePosition(edge) + offset;
            return camera.ScreenPointToWorldPosition(screenPosition, depth);
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