using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Neo.Extensions
{
    /// <summary>
    ///     Utility methods for common Unity UI operations.
    /// </summary>
    public static class UIUtils
    {
        private static readonly List<RaycastResult> SharedResults = new();
        private static PointerEventData _sharedPointer;
        private static EventSystem _sharedPointerOwner;
        private static bool _sharedQueryActive;
        private static bool _releaseHookRegistered;

        // WHY: with domain reload disabled the shared pointer would keep the previous session's EventSystem alive.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            SharedResults.Clear();
            _sharedPointer = null;
            _sharedPointerOwner = null;
            _sharedQueryActive = false;
            SceneManager.sceneUnloaded -= ReleaseDestroyedSharedPointer;
            _releaseHookRegistered = false;
        }

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
        ///     The buffer is also cleared when there is no active EventSystem. The query reuses one shared
        ///     PointerEventData, so with a retained buffer it adds no allocation of its own to the EventSystem raycast.
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

            if (_sharedQueryActive)
            {
                // WHY: a raycaster that queries again from inside RaycastAll must not reset the outer shared pointer.
                PointerEventData nestedPointer = new(eventSystem);
                nestedPointer.position = screenPosition;
                eventSystem.RaycastAll(nestedPointer, results);
                return results.Count;
            }

            PointerEventData pointer = RentSharedPointer(eventSystem, screenPosition);
            _sharedQueryActive = true;
            try
            {
                eventSystem.RaycastAll(pointer, results);
                return results.Count;
            }
            finally
            {
                _sharedQueryActive = false;
            }
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

            return IsPointerOverUI(Input.mousePosition);
        }

        /// <summary>
        ///     Checks whether any UI element is hit at an explicit screen position, without reading an input backend.
        ///     After the first call it adds no allocation of its own: a shared pointer and result buffer are reused
        ///     and cleared after every query, so no hit keeps a destroyed object referenced. What the EventSystem
        ///     raycast itself allocates (Unity's raycasters may sort hits when the pointer is over UI) is not covered.
        /// </summary>
        /// <param name="screenPosition">Screen position in pixels (for example a touch or a virtual cursor).</param>
        /// <returns>True when at least one raycaster reports a hit; false without an active EventSystem.</returns>
        public static bool IsPointerOverUI(Vector2 screenPosition)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            if (_sharedQueryActive)
            {
                // WHY: a raycaster that queries again from inside RaycastAll must not clear the outer shared buffer.
                return GetUIElementsAtPosition(screenPosition, new List<RaycastResult>()) > 0;
            }

            PointerEventData pointer = RentSharedPointer(eventSystem, screenPosition);
            _sharedQueryActive = true;
            try
            {
                eventSystem.RaycastAll(pointer, SharedResults);
                return SharedResults.Count > 0;
            }
            finally
            {
                SharedResults.Clear();
                _sharedQueryActive = false;
            }
        }

        private static PointerEventData RentSharedPointer(EventSystem eventSystem, Vector2 screenPosition)
        {
            if (_sharedPointer == null || _sharedPointerOwner != eventSystem)
            {
                _sharedPointer = new PointerEventData(eventSystem);
                _sharedPointerOwner = eventSystem;
                RegisterReleaseHook();
            }
            else
            {
                _sharedPointer.Reset();
            }

            _sharedPointer.position = screenPosition;
            return _sharedPointer;
        }

        private static void RegisterReleaseHook()
        {
            if (_releaseHookRegistered)
            {
                return;
            }

            // WHY: the shared pointer references its EventSystem. Only a later query with another EventSystem used to
            // replace it, so a scene that made the last query kept its destroyed EventSystem (and the managed objects
            // it references) reachable for the rest of the session.
            SceneManager.sceneUnloaded += ReleaseDestroyedSharedPointer;
            _releaseHookRegistered = true;
        }

        private static void ReleaseDestroyedSharedPointer(Scene unloadedScene)
        {
            // WHY: Unity's null check: true once the owner is destroyed, while a surviving (DontDestroyOnLoad)
            // EventSystem keeps its warm pointer.
            if (_sharedPointer != null && _sharedPointerOwner == null && !_sharedQueryActive)
            {
                _sharedPointer = null;
                _sharedPointerOwner = null;
            }
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

            RectTransform canvasRect = canvas.transform as RectTransform;
            Vector2 screenPoint = camera.WorldToScreenPoint(worldPoint);
            // WHY: an explicit Unity null check; '??' would keep a destroyed worldCamera instead of falling back.
            Camera canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera != null
                    ? canvas.worldCamera
                    : camera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint,
                canvasCamera, out Vector2 localPoint);
            return localPoint;
        }
    }
}
