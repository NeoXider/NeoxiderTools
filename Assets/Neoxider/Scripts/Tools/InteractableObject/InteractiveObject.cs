using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Neo
{
    namespace Tools
    {
        /// <summary>
        ///     Universal interactive object component with mouse, keyboard, and distance-based interaction support.
        /// </summary>
        [AddComponentMenu("Neo/" + "Tools/" + nameof(InteractiveObject))]
        public class InteractiveObject : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
        {
            public enum MouseButton
            {
                Left = 0,
                Right = 1,
                Middle = 2
            }

            private static bool physicsRaycasterEnsured3D;
            private static bool physicsRaycasterEnsured2D;
            private static bool eventSystemChecked;

            [Header("Event System")] [SerializeField]
            private bool _autoCheckEventSystem = true;

            public bool interactable = true;

            [Header("Interaction Settings")] [Tooltip("Enable mouse interaction (hover and click).")] [SerializeField]
            private bool useMouseInteraction = true;

            [Tooltip("Enable keyboard interaction.")] [SerializeField]
            private bool useKeyboardInteraction = true;

            [Header("Distance Control")] [Tooltip("Maximum interaction distance (0 = unlimited).")] [SerializeField]
            private float interactionDistance = 2f;

            [Tooltip("Reference point for distance check (player/camera). Uses main camera if not set.")]
            [SerializeField]
            private Transform distanceCheckPoint;

            [Tooltip(
                "Check for obstacles (walls) between object and check point. Uses raycast to detect blocking colliders.")]
            [SerializeField]
            private bool checkObstacles = true;

            [Tooltip("Layers that block interaction (used when checkObstacles is enabled).")] [SerializeField]
            private LayerMask obstacleLayers = -1;

            [Header("Down/Up — Mouse Binding")] [SerializeField]
            private MouseButton downUpMouseButton = MouseButton.Left;

            [Header("Down/Up — Keyboard Binding")] [SerializeField]
            private KeyCode keyboardKey = KeyCode.E;

            [Space] [Header("Down/Up Events")] public UnityEvent onInteractDown;

            public UnityEvent onInteractUp;

            [Header("Hover Events")] [Space] public UnityEvent onHoverEnter;

            public UnityEvent onHoverExit;

            [Header("Click Events")] [SerializeField]
            private float doubleClickThreshold = 0.3f;

            public UnityEvent onClick;
            public UnityEvent onDoubleClick;
            public UnityEvent onRightClick;
            public UnityEvent onMiddleClick;

            [Header("Distance Events")] public UnityEvent onEnterRange;

            public UnityEvent onExitRange;

            private float clickTime;
            private bool keyHeldPrev;
            private bool mouseHeldPrev;
            private bool wasInRange;

            private void Awake()
            {
                CheckEventSystemOnce();
                TryEnsureNeededRaycasterOnce();

                if (distanceCheckPoint == null && Camera.main != null)
                {
                    distanceCheckPoint = Camera.main.transform;
                }
            }

            private void Update()
            {
                if (!interactable)
                {
                    return;
                }

                bool inRange = IsInRange();

                if (inRange && !wasInRange)
                {
                    onEnterRange?.Invoke();
                }
                else if (!inRange && wasInRange)
                {
                    onExitRange?.Invoke();
                }

                wasInRange = inRange;

                if (!inRange && interactionDistance > 0f)
                {
                    return;
                }

                if (useMouseInteraction)
                {
                    UpdateMouseInput();
                }

                if (useKeyboardInteraction)
                {
                    UpdateKeyboardInput();
                }
            }

            private void OnDrawGizmosSelected()
            {
                if (interactionDistance <= 0f)
                {
                    return;
                }

                Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
                Gizmos.DrawWireSphere(transform.position, interactionDistance);
            }

            public void OnPointerClick(PointerEventData eventData)
            {
                if (!interactable || !useMouseInteraction)
                {
                    return;
                }

                if (interactionDistance > 0f && !IsInRange())
                {
                    return;
                }

                if (eventData.button == PointerEventData.InputButton.Left)
                {
                    if (doubleClickThreshold > 0f && Time.time - clickTime < doubleClickThreshold)
                    {
                        onDoubleClick.Invoke();
                    }
                    else
                    {
                        onClick.Invoke();
                    }

                    clickTime = Time.time;
                }
                else if (eventData.button == PointerEventData.InputButton.Right)
                {
                    onRightClick.Invoke();
                }
                else if (eventData.button == PointerEventData.InputButton.Middle)
                {
                    onMiddleClick.Invoke();
                }
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                if (interactable && useMouseInteraction)
                {
                    IsHovered = true;
                    onHoverEnter.Invoke();
                }
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                if (interactable && useMouseInteraction)
                {
                    IsHovered = false;
                    onHoverExit.Invoke();
                }
            }

            private void UpdateMouseInput()
            {
                int mouseIndex = (int)downUpMouseButton;
                bool mouseHeld = Input.GetMouseButton(mouseIndex);

                if (mouseHeld && !mouseHeldPrev)
                {
                    if (IsHovered)
                    {
                        onInteractDown?.Invoke();
                    }
                }
                else if (!mouseHeld && mouseHeldPrev)
                {
                    onInteractUp?.Invoke();
                }

                mouseHeldPrev = mouseHeld;
            }

            private void UpdateKeyboardInput()
            {
                bool keyDown = Input.GetKeyDown(keyboardKey);
                bool keyUp = Input.GetKeyUp(keyboardKey);

                if (keyDown)
                {
                    bool inRange = IsInRange();
                    if (inRange || interactionDistance <= 0f)
                    {
                        onInteractDown?.Invoke();
                    }
                }

                if (keyUp)
                {
                    onInteractUp?.Invoke();
                }
            }

            private bool IsInRange()
            {
                if (interactionDistance <= 0f && !checkObstacles)
                {
                    return true;
                }

                if (distanceCheckPoint == null)
                {
                    return true;
                }

                Vector3 checkPointPos = distanceCheckPoint.position;
                Vector3 objectPos = transform.position;
                float distanceSqr = (objectPos - checkPointPos).sqrMagnitude;

                if (interactionDistance > 0f && distanceSqr > interactionDistance * interactionDistance)
                {
                    return false;
                }

                if (checkObstacles)
                {
                    Vector3 direction = objectPos - checkPointPos;
                    float distance = Mathf.Sqrt(distanceSqr);

                    if (distance < 0.01f)
                    {
                        return true;
                    }

                    Vector3 directionNormalized = direction.normalized;
                    float checkDistance = distance - 0.1f;

                    if (checkDistance <= 0f)
                    {
                        return true;
                    }

                    bool has3DCollider = TryGetComponent(out Collider selfCollider3D);
                    bool has2DCollider = TryGetComponent(out Collider2D selfCollider2D);

                    if (has3DCollider)
                    {
                        RaycastHit hit;
                        if (Physics.Raycast(checkPointPos, directionNormalized, out hit, checkDistance, obstacleLayers))
                        {
                            if (hit.collider != selfCollider3D)
                            {
                                return false;
                            }
                        }
                    }
                    else if (has2DCollider)
                    {
                        Vector2 origin2D = new(checkPointPos.x, checkPointPos.y);
                        Vector2 direction2D = new(directionNormalized.x, directionNormalized.y);
                        RaycastHit2D hit = Physics2D.Raycast(origin2D, direction2D, checkDistance, obstacleLayers);
                        if (hit.collider != null && hit.collider != selfCollider2D)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (Physics.Raycast(checkPointPos, directionNormalized, checkDistance, obstacleLayers))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            private void CheckEventSystemOnce()
            {
                if (!_autoCheckEventSystem || eventSystemChecked)
                {
                    return;
                }

                if (EventSystem.current == null && FindObjectOfType<EventSystem>() == null)
                {
                    Debug.LogWarning("InteractiveObject: EventSystem not found in scene");
                }

                eventSystemChecked = true;
            }

            private void TryEnsureNeededRaycasterOnce()
            {
                Camera cam = Camera.main ?? FindFirstObjectByType<Camera>();
                if (cam == null)
                {
                    Debug.LogError(
                        $"[InteractiveObject] No camera found on {gameObject.name}. Component will be disabled.", this);
                    enabled = false;
                    return;
                }

                bool isUI = GetComponentInParent<Canvas>() != null && TryGetComponent<RectTransform>(out _);
                if (isUI)
                {
                    return;
                }

                if (TryGetComponent<Collider2D>(out _) && !physicsRaycasterEnsured2D)
                {
                    if (cam.GetComponent<Physics2DRaycaster>() == null)
                    {
                        cam.gameObject.AddComponent<Physics2DRaycaster>();
                    }

                    physicsRaycasterEnsured2D = true;
                }

                if (TryGetComponent<Collider>(out _) && !physicsRaycasterEnsured3D)
                {
                    if (cam.GetComponent<PhysicsRaycaster>() == null)
                    {
                        cam.gameObject.AddComponent<PhysicsRaycaster>();
                    }

                    physicsRaycasterEnsured3D = true;
                }
            }

            #region === Public API ===

            /// <summary>
            ///     Interaction distance (0 = unlimited).
            /// </summary>
            public float InteractionDistance
            {
                get => interactionDistance;
                set => interactionDistance = Mathf.Max(0f, value);
            }

            /// <summary>
            ///     Reference point for distance checks.
            /// </summary>
            public Transform DistanceCheckPoint
            {
                get => distanceCheckPoint;
                set => distanceCheckPoint = value;
            }

            /// <summary>
            ///     Enable or disable mouse interaction.
            /// </summary>
            public bool UseMouseInteraction
            {
                get => useMouseInteraction;
                set => useMouseInteraction = value;
            }

            /// <summary>
            ///     Enable or disable keyboard interaction.
            /// </summary>
            public bool UseKeyboardInteraction
            {
                get => useKeyboardInteraction;
                set => useKeyboardInteraction = value;
            }

            /// <summary>
            ///     Returns true if object is currently in interaction range.
            /// </summary>
            public bool IsInInteractionRange => IsInRange();

            /// <summary>
            ///     Current distance to check point.
            /// </summary>
            public float DistanceToCheckPoint
            {
                get
                {
                    if (distanceCheckPoint == null)
                    {
                        return 0f;
                    }

                    return Vector3.Distance(transform.position, distanceCheckPoint.position);
                }
            }

            /// <summary>
            ///     Returns true if object is currently hovered.
            /// </summary>
            public bool IsHovered { get; private set; }

            #endregion
        }
    }
}