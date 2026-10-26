using System;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    public struct SwipeData
    {
        public Vector2 StartPosition;
        public Vector2 EndPosition;
        public SwipeDirection Direction;
    }

    public enum SwipeDirection
    {
        Up,
        Down,
        Left,
        Right
    }

    public interface ISwipeSubscriber
    {
        void SubscribeToSwipe(SwipeData swipeData);
    }

    /// <summary>
    ///     SwipeController is responsible for detecting swipe gestures on both touch devices and PCs.
    ///     It can differentiate between swipes in all four cardinal directions and trigger events accordingly.
    /// </summary>
    /// ///
    /// <example>
    ///     <code>
    ///  <![CDATA[
    /// public void SubscribeToSwipe(SwipeData swipeData) => global::NeoDiagnostics.Log(swipeData.Direction);
    ///  ]]>
    /// </code>
    /// </example>
    [NeoDoc("Tools/Input/SwipeController.md")]
    [CreateFromMenu("Neoxider/Tools/Input/SwipeController")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(SwipeController))]
    public class SwipeController : MonoBehaviour
    {
        private static SwipeController instance;

        /// <summary>
        ///     If true, swipes are only detected after the user has released the touch or mouse button. This can be useful
        ///     for games or applications where swipes are meant to be deliberate actions rather than continuous inputs.
        /// </summary>
        [Header("Settings")] [SerializeField] public bool detectSwipeOnlyAfterRelease;

        /// <summary>
        ///     The minimum distance (in screen pixels) that the user must move their finger or the mouse for a movement
        ///     to be considered a swipe. This helps differentiate between swipes and taps or clicks.
        /// </summary>
        [SerializeField] private float minDistanceForSwipeX = 20f;

        [SerializeField] private float minDistanceForSwipeY = 20f;
        public bool ignoreLastSwipe;

        public UnityEvent<SwipeData> OnSwipe = new();
        private Vector2 fingerDownPosition;

        private Vector2 fingerUpPosition;
        private SwipeDirection lastSwipeDirection;

        private bool swipeCompleted = true;

        private bool swipeDetected;
        private int? _activeTouchId;
        private bool _mouseGestureActive;

        public static SwipeController Instance => instance;

        private void Awake()
        {
            instance = this;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        public static void ResetStaticStateForRuntime()
        {
            instance = null;
        }

        private void Update()
        {
            if (TryHandleTouch())
            {
                return;
            }

            if (_activeTouchId.HasValue)
            {
                ResetGesture();
            }

            HandleMouse();
        }

        private void OnDisable()
        {
            ResetGesture();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                ResetGesture();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                ResetGesture();
            }
        }

        private void ResetGesture()
        {
            _activeTouchId = null;
            _mouseGestureActive = false;
            swipeCompleted = true;
            swipeDetected = false;
        }

        public static bool GetSwipeDirection(out SwipeDirection direction)
        {
            SwipeController controller = Instance;
            if (controller == null)
            {
                direction = default;
                return false;
            }

            direction = controller.lastSwipeDirection;
            if (controller.swipeDetected)
            {
                controller.swipeDetected = false;
                return true;
            }

            return false;
        }

        private bool TryHandleTouch()
        {
            int touchCount = 0;
            if (InputBackendCompat.UsesLegacyInput)
            {
                try
                {
                    touchCount = Input.touchCount;
                }
                catch (InvalidOperationException)
                {
                    InputBackendCompat.MarkLegacyUnavailable();
                }
            }

            if (!InputBackendCompat.UsesLegacyInput)
            {
                if (!OptionalInputSystemAdapter.TryGetPrimaryTouch(out int fingerId, out Vector2 position,
                        out TouchPhase phase))
                {
                    return false;
                }

                ProcessTouch(fingerId, position, phase);
                return true;
            }

            for (int i = 0; i < touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                ProcessTouch(touch.fingerId, touch.position, touch.phase);
            }

            return touchCount > 0;
        }

        internal void ProcessTouch(int fingerId, Vector2 position, TouchPhase phase)
        {
            if (phase == TouchPhase.Began)
            {
                if (_activeTouchId.HasValue)
                {
                    return;
                }

                _activeTouchId = fingerId;
                _mouseGestureActive = false;
                BeginSwipe(position);
                return;
            }

            if (_activeTouchId != fingerId)
            {
                return;
            }

            if (phase == TouchPhase.Canceled)
            {
                ResetGesture();
            }
            else if (phase == TouchPhase.Ended)
            {
                _activeTouchId = null;
                fingerUpPosition = position;
                DetectSwipe();
                swipeCompleted = true;
            }
            else if (phase == TouchPhase.Moved && !detectSwipeOnlyAfterRelease)
            {
                fingerUpPosition = position;
                DetectSwipe();
            }
        }

        private void BeginSwipe(Vector2 position)
        {
            fingerUpPosition = position;
            fingerDownPosition = position;
            swipeCompleted = true;
        }

        private void HandleMouse()
        {
            if (!MouseInputCompat.TryGetPosition(out Vector3 position) ||
                !MouseInputCompat.TryGetButtonDown(0, out bool wasPressed) ||
                !MouseInputCompat.TryGetButton(0, out bool isHeld) ||
                !MouseInputCompat.TryGetButtonUp(0, out bool wasReleased))
            {
                ResetGesture();
                return;
            }

            ProcessMouseState(position, wasPressed, isHeld, wasReleased);
        }

        internal void ProcessMouseState(Vector2 position, bool wasPressed, bool isHeld, bool wasReleased)
        {
            if (_activeTouchId.HasValue)
            {
                return;
            }

            if (wasPressed)
            {
                _mouseGestureActive = true;
                BeginSwipe(position);
            }

            if (_mouseGestureActive && !detectSwipeOnlyAfterRelease && isHeld)
            {
                fingerUpPosition = position;
                DetectSwipe();
            }

            if (_mouseGestureActive && wasReleased)
            {
                _mouseGestureActive = false;
                fingerUpPosition = position;
                DetectSwipe();
                swipeCompleted = true;
            }
        }

        private void DetectSwipe()
        {
            if (SwipeDistanceCheckMet())
            {
                if (IsVerticalSwipe())
                {
                    SwipeDirection direction = fingerUpPosition.y - fingerDownPosition.y > 0
                        ? SwipeDirection.Up
                        : SwipeDirection.Down;
                    if (swipeCompleted || lastSwipeDirection != direction || ignoreLastSwipe)
                    {
                        swipeCompleted = false;
                        lastSwipeDirection = direction;
                        SendSwipe(direction);
                    }
                }
                else
                {
                    SwipeDirection direction = fingerUpPosition.x - fingerDownPosition.x > 0
                        ? SwipeDirection.Right
                        : SwipeDirection.Left;
                    if (swipeCompleted || lastSwipeDirection != direction || ignoreLastSwipe)
                    {
                        swipeCompleted = false;
                        lastSwipeDirection = direction;
                        SendSwipe(direction);
                    }
                }

                fingerDownPosition = fingerUpPosition;
            }
        }

        private void SendSwipe(SwipeDirection direction)
        {
            // WHY: fingerDownPosition is where the gesture began, fingerUpPosition where it ended;
            // swapping them made End-Start point opposite to the actual swipe vector.
            SwipeData swipeData = new()
            {
                Direction = direction,
                StartPosition = fingerDownPosition,
                EndPosition = fingerUpPosition
            };

            swipeDetected = true;
            OnSwipe?.Invoke(swipeData);
        }

        private bool IsVerticalSwipe()
        {
            return VerticalMovementDistance() > minDistanceForSwipeY &&
                   (HorizontalMovementDistance() <= minDistanceForSwipeX ||
                    VerticalMovementDistance() > HorizontalMovementDistance());
        }

        private bool SwipeDistanceCheckMet()
        {
            return VerticalMovementDistance() > minDistanceForSwipeY
                   || HorizontalMovementDistance() > minDistanceForSwipeX;
        }

        private float VerticalMovementDistance()
        {
            return Mathf.Abs(fingerUpPosition.y - fingerDownPosition.y);
        }

        private float HorizontalMovementDistance()
        {
            return Mathf.Abs(fingerUpPosition.x - fingerDownPosition.x);
        }
    }
}
