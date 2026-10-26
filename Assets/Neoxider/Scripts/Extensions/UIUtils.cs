using System;
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
            {
                return new List<RaycastResult>();
            }

            return GetUIElementsAtPosition(Input.mousePosition);
        }

        /// <summary>
        ///     Gets the raycast results at an explicit screen position without reading an input backend.
        /// </summary>
        public static List<RaycastResult> GetUIElementsAtPosition(Vector2 screenPosition)
        {
            List<RaycastResult> results = new();
            GetUIElementsAtPosition(screenPosition, results);
            return results;
        }

        /// <summary>
        ///     Clears and fills a caller-owned result buffer at an explicit screen position.
        ///     The buffer is also cleared when there is no active EventSystem. PointerEventData is per-call.
        /// </summary>
        /// <returns>The number of results written to the buffer.</returns>
        /// <exception cref="ArgumentNullException">The result buffer is null.</exception>
        public static int GetUIElementsAtPosition(Vector2 screenPosition, List<RaycastResult> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            results.Clear();
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return 0;
            }

            PointerEventData pointer = new(eventSystem);
            pointer.position = screenPosition;
            eventSystem.RaycastAll(pointer, results);
            return results.Count;
        }

        /// <summary>
        ///     Checks if the mouse pointer is currently over any UI element.
        /// </summary>
        /// <returns>True if the pointer is over a UI element, false otherwise.</returns>
        public static bool IsPointerOverUI()
        {
            if (EventSystem.current == null)
            {
                return false;
            }

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
            if (canvas == null)
            {
                return Vector2.zero;
            }

            if (camera == null)
            {
                camera = Camera.main;
            }

            if (camera == null)
            {
                return Vector2.zero;
            }

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            Vector2 screenPoint = camera.WorldToScreenPoint(worldPoint);
            Camera canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera ?? camera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint,
                canvasCamera, out Vector2 localPoint);
            return localPoint;
        }
    }
}
