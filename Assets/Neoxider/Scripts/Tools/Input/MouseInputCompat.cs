using System;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Compatibility between legacy and new mouse input. Reads sample the backend at the moment of the call and
    ///     do not allocate once warm. On touch-only devices both backends report the primary touch as the left
    ///     button and pointer: the legacy one through <c>Input.simulateMouseWithTouches</c>, the Input System one
    ///     through <see cref="OptionalInputSystemAdapter.SimulateMouseWithTouches" />.
    /// </summary>
    public static class MouseInputCompat
    {
        public static bool TryGetPosition(out Vector3 position)
        {
            if (InputBackendCompat.UsesLegacyInput)
            {
                try
                {
                    position = Input.mousePosition;
                    return true;
                }
                catch (InvalidOperationException)
                {
                    InputBackendCompat.MarkLegacyUnavailable();
                }
            }

            return OptionalInputSystemAdapter.TryGetMousePosition(out position);
        }

        public static bool TryGetButton(int buttonIndex, out bool isPressed)
        {
            if (InputBackendCompat.UsesLegacyInput)
            {
                try
                {
                    isPressed = Input.GetMouseButton(buttonIndex);
                    return true;
                }
                catch (InvalidOperationException)
                {
                    InputBackendCompat.MarkLegacyUnavailable();
                }
            }

            return OptionalInputSystemAdapter.TryGetMouseButtonState(buttonIndex, "isPressed", out isPressed);
        }

        public static bool TryGetButtonDown(int buttonIndex, out bool wasPressedThisFrame)
        {
            if (InputBackendCompat.UsesLegacyInput)
            {
                try
                {
                    wasPressedThisFrame = Input.GetMouseButtonDown(buttonIndex);
                    return true;
                }
                catch (InvalidOperationException)
                {
                    InputBackendCompat.MarkLegacyUnavailable();
                }
            }

            return OptionalInputSystemAdapter.TryGetMouseButtonState(buttonIndex, "wasPressedThisFrame",
                out wasPressedThisFrame);
        }

        public static bool TryGetButtonUp(int buttonIndex, out bool wasReleasedThisFrame)
        {
            if (InputBackendCompat.UsesLegacyInput)
            {
                try
                {
                    wasReleasedThisFrame = Input.GetMouseButtonUp(buttonIndex);
                    return true;
                }
                catch (InvalidOperationException)
                {
                    InputBackendCompat.MarkLegacyUnavailable();
                }
            }

            return OptionalInputSystemAdapter.TryGetMouseButtonState(buttonIndex, "wasReleasedThisFrame",
                out wasReleasedThisFrame);
        }

        /// <summary>
        ///     Mouse wheel movement this frame; y is the vertical wheel. Legacy reads <c>Input.mouseScrollDelta</c>,
        ///     the Input System reads <c>Mouse.scroll</c>. Both report about 1 per notch by default (the Input System
        ///     only with its default Scroll Delta Behavior); the exact step is platform-dependent, so scale it by your
        ///     own sensitivity. Returns false without a mouse.
        /// </summary>
        public static bool TryGetScrollDelta(out Vector2 scrollDelta)
        {
            if (InputBackendCompat.UsesLegacyInput)
            {
                try
                {
                    scrollDelta = Input.mouseScrollDelta;
                    return true;
                }
                catch (InvalidOperationException)
                {
                    InputBackendCompat.MarkLegacyUnavailable();
                }
            }

            return OptionalInputSystemAdapter.TryGetMouseScroll(out scrollDelta);
        }

        /// <summary>
        ///     Pointer movement this frame in pixels, from the same pointer as <see cref="TryGetPosition" />. Legacy
        ///     reads <c>Input.mousePositionDelta</c>, which Unity documents for a locked cursor too; the Input System
        ///     reads <c>Mouse.delta</c>, or the primary touch without a mouse. It accumulates per frame, so do not
        ///     multiply it by <c>Time.deltaTime</c>. Returns false without a pointer.
        /// </summary>
        public static bool TryGetPointerDelta(out Vector2 pointerDelta)
        {
            if (InputBackendCompat.UsesLegacyInput)
            {
                try
                {
                    // WHY: no version switch is needed; the property exists since Unity 2023.2, below the package's
                    // 6000.0 minimum.
                    pointerDelta = Input.mousePositionDelta;
                    return true;
                }
                catch (InvalidOperationException)
                {
                    InputBackendCompat.MarkLegacyUnavailable();
                }
            }

            return OptionalInputSystemAdapter.TryGetMouseDelta(out pointerDelta);
        }
    }
}
