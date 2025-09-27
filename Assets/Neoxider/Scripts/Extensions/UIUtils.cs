using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Neo.Extensions
{
    /// <summary>
    ///     Utility methods for common Unity UI operations.
    /// </summary>
    public static class UIUtils
    {
        /// <summary>
        ///     Gets all UI elements hit by the current mouse position.
        /// </summary>
        /// <returns>List of raycast results for UI elements under the cursor.</returns>
        public static List<RaycastResult> GetUIElementsUnderCursor()
        {
            if (EventSystem.current == null)
                return new List<RaycastResult>();

            var pointer = new PointerEventData(EventSystem.current);
            pointer.position = Input.mousePosition;
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, results);
            return results;
        }

        /// <summary>
        ///     Checks if the mouse pointer is currently over any UI element.
        /// </summary>
        /// <returns>True if the pointer is over a UI element, false otherwise.</returns>
        public static bool IsPointerOverUI()
        {
            if (EventSystem.current == null) return false;
            return GetUIElementsUnderCursor().Count > 0;
        }

        /// <summary>
        ///     Converts a 3D world position to a 2D local position on a specific UI Canvas.
        /// </summary>
        /// <param name="canvas">The target canvas.</param>
        /// <param name="worldPoint">The world position to convert.</param>
        /// <param name="camera">The camera to use for the conversion. If null, Camera.main is used.</param>
        /// <returns>The local 2D position on the canvas.</returns>
        public static Vector2 WorldToCanvasPoint(Canvas canvas, Vector3 worldPoint, Camera camera = null)
        {
            if (camera == null) camera = Camera.main;
            if (camera == null) return Vector2.zero;

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) return camera.WorldToScreenPoint(worldPoint);

            var canvasRect = canvas.GetComponent<RectTransform>();
            Vector2 screenPoint = camera.WorldToScreenPoint(worldPoint);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint,
                canvas.worldCamera ?? camera, out var localPoint);
            return localPoint;
        }
    }
}