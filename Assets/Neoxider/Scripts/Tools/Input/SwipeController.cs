//v.1.0.4
using UnityEngine;
using UnityEngine.Events;


namespace Neo
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
    /// SwipeController is responsible for detecting swipe gestures on both touch devices and PCs.
    /// It can differentiate between swipes in all four cardinal directions and trigger events accordingly.
    /// </summary>
    ///     ///<example>
    ///<code>
    /// <![CDATA[
    ///public void SubscribeToSwipe(SwipeData swipeData) => Debug.Log(swipeData.Direction);
    /// ]]>
    ///</code>
    ///</example>
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(SwipeController))]
    public class SwipeController : MonoBehaviour
    {
        /// <summary>
        /// If true, swipes are only detected after the user has released the touch or mouse button. This can be useful
        /// for games or applications where swipes are meant to be deliberate actions rather than continuous inputs.
        /// </summary>
        [SerializeField]
        public bool detectSwipeOnlyAfterRelease = false;

        /// <summary>
        /// The minimum distance (in screen pixels) that the user must move their finger or the mouse for a movement
        /// to be considered a swipe. This helps differentiate between swipes and taps or clicks.
        /// </summary>
        [SerializeField]
        private float minDistanceForSwipeX = 20f;
        [SerializeField]
        private float minDistanceForSwipeY = 20f;
        public bool ignoreLastSwipe = false;

        private Vector2 fingerUpPosition;
        private Vector2 fingerDownPosition;

        private bool swipeCompleted = true;
        private SwipeDirection lastSwipeDirection;
        private static SwipeController instance;

        public UnityEvent<SwipeData> OnSwipe = new UnityEvent<SwipeData>();

        private bool swipeDetected = false;

        private void Awake()
        {
            instance = this;
        }

        public static SwipeController Instance
        {
            get
            {
                if (instance == null)
                {

                }
                return instance;
            }
        }

        private void Update()
        {
            if (Input.touchSupported && Input.touchCount > 0)
            {
                HandleTouch();
            }
            else
            {
                HandleMouse();
            }
        }

        public static bool GetSwipeDirection(out SwipeDirection direction)
        {
            direction = Instance.lastSwipeDirection;
            if (Instance.swipeDetected)
            {
                Instance.swipeDetected = false; 
                return true;
            }
            return false;
        }

        private void HandleTouch()
        {
            foreach (Touch touch in Input.touches)
            {
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        fingerDownPosition = touch.position;
                        fingerUpPosition = touch.position;
                        break;

                    case TouchPhase.Moved:
                        if (!detectSwipeOnlyAfterRelease)
                        {
                            fingerUpPosition = touch.position;
                            DetectSwipe();
                        }
                        break;

                    case TouchPhase.Ended:
                        fingerUpPosition = touch.position;
                        DetectSwipe();
                        swipeCompleted = true;
                        break;
                }
            }
        }

        private void HandleMouse()
        {
            if (Input.GetMouseButtonDown(0))
            {
                fingerUpPosition = Input.mousePosition;
                fingerDownPosition = Input.mousePosition;
            }

            if (!detectSwipeOnlyAfterRelease && Input.GetMouseButton(0))
            {
                fingerUpPosition = Input.mousePosition;
                DetectSwipe();
            }

            if (Input.GetMouseButtonUp(0))
            {
                fingerUpPosition = Input.mousePosition;
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
                    var direction = fingerUpPosition.y - fingerDownPosition.y > 0 ? SwipeDirection.Up : SwipeDirection.Down;
                    if (swipeCompleted || lastSwipeDirection != direction || ignoreLastSwipe)
                    {
                        swipeCompleted = false;
                        lastSwipeDirection = direction;
                        SendSwipe(direction);
                    }
                }
                else
                {
                    var direction = fingerUpPosition.x - fingerDownPosition.x > 0 ? SwipeDirection.Right : SwipeDirection.Left;
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

        void SendSwipe(SwipeDirection direction)
        {
            SwipeData swipeData = new SwipeData()
            {
                Direction = direction,
                StartPosition = fingerUpPosition,
                EndPosition = fingerDownPosition
            };

            swipeDetected = true;
            OnSwipe?.Invoke(swipeData);
        }

        private bool IsVerticalSwipe()
        {
            return VerticalMovementDistance() > HorizontalMovementDistance();
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
