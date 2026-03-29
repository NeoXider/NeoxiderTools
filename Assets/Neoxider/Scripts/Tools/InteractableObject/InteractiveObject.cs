using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Neo.Tools
{
    /// <summary>
    ///     Universal interactive object component with mouse, keyboard, and distance-based interaction support.
    /// </summary>
    [NeoDoc("Tools/InteractableObject/InteractiveObject.md")]
    [CreateFromMenu("Neoxider/Tools/Interact/InteractiveObject",
        "Prefabs/Tools/Interact/Interactive Sphere.prefab")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(InteractiveObject))]
    public class InteractiveObject : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public enum KeyboardInteractionMode
        {
            ViewOrMouse,
            DistanceOnly
        }

        public enum MouseButton
        {
            Left = 0,
            Right = 1,
            Middle = 2
        }

        [Header("Event System")] [SerializeField]
        private bool _autoCheckEventSystem = true;

        [Tooltip("Create EventSystem automatically if it is missing in scene.")] [SerializeField]
        private bool _autoCreateEventSystemIfMissing = true;

        public bool interactable = true;

        [Header("Interaction Settings")]
        [Tooltip("Enable hover detection (cursor over collider).")]
        [SerializeField]
        private bool useHoverDetection = true;

        [Tooltip("Enable mouse click/down/up interaction (hover detection can be enabled separately).")]
        [SerializeField]
        private bool useMouseInteraction = true;

        [Tooltip("Enable keyboard interaction.")] [SerializeField]
        private bool useKeyboardInteraction = true;

        [Tooltip("ViewOrMouse: keyboard requires looking at object. DistanceOnly: only distance check.")]
        [SerializeField]
        private KeyboardInteractionMode keyboardInteractionMode = KeyboardInteractionMode.ViewOrMouse;

        [Tooltip("Require looking at object when interacting with keyboard.")] [SerializeField]
        private bool requireViewForKeyboardInteraction = true;

        [Tooltip("Require direct line of sight from check point to object.")] [SerializeField]
        private bool requireDirectLookRay = true;

        [Tooltip("Include trigger colliders in look ray checks.")] [SerializeField]
        private bool includeTriggerCollidersInLookRay = true;

        [Tooltip("Include trigger colliders in mouse hover raycast. Enable for objects with Trigger Collider.")]
        [SerializeField]
        private bool includeTriggerCollidersInMouseRaycast = true;

        [Header("Target Colliders")]
        [Tooltip("Optional explicit 3D collider used for hover, click, and view checks. If not set, uses Collider on this GameObject only.")]
        [SerializeField]
        private Collider targetCollider3D;

        [Tooltip("Optional explicit 2D collider used for hover, click, and view checks. If not set, uses Collider2D on this GameObject only.")]
        [SerializeField]
        private Collider2D targetCollider2D;

        [Header("Distance Control")] [Tooltip("Maximum interaction distance (0 = unlimited).")] [SerializeField]
        private float interactionDistance = 3f;

        [Tooltip("Reference point for distance check (player/camera). Uses main camera if not set.")] [SerializeField]
        private Transform distanceCheckPoint;

        [Tooltip("Reference point for look direction checks. Uses main camera if not set.")] [SerializeField]
        private Transform viewCheckPoint;

        [Tooltip(
            "Check for obstacles (walls) between object and check point. When enabled, also requires the mouse ray to hit this object before any non-trigger collider (line-of-sight for hover/click). When disabled, distance checks skip obstacles and the mouse ray accepts a hit on this object even if solid geometry is closer.")]
        [SerializeField]
        private bool checkObstacles = true;

        [Tooltip("Layers that block interaction (used when checkObstacles is enabled).")] [SerializeField]
        private LayerMask obstacleLayers = -1;

        [Tooltip("Include trigger colliders in obstacle ray checks.")]
        [SerializeField]
        private bool includeTriggerCollidersInObstacleCheck;

        [Tooltip("Ignore colliders from distance check point hierarchy (e.g. player capsule/camera rig).")]
        [SerializeField]
        private bool ignoreDistancePointHierarchyColliders = true;

        [Header("Down/Up — Mouse Binding")] [SerializeField]
        private MouseButton downUpMouseButton = MouseButton.Left;

        [Header("Down/Up — Keyboard Binding")] [SerializeField]
        private KeyCode keyboardKey = KeyCode.E;

        [Space] [Header("Down/Up Events")] public UnityEvent onInteractDown;

        public UnityEvent onInteractUp;

        [Header("Hover Events")] [Space] public UnityEvent onHoverEnter;

        public UnityEvent onHoverExit;

        [Tooltip("Invoked on hover state change. Passes true on enter, false on exit.")]
        public UnityEvent<bool> onHoverChanged;

        [Header("Click Events")] [SerializeField]
        private float doubleClickThreshold = 0.3f;

        public UnityEvent onClick;
        public UnityEvent onDoubleClick;
        public UnityEvent onRightClick;
        public UnityEvent onMiddleClick;

        [Header("Distance Events")] public UnityEvent onEnterRange;

        public UnityEvent onExitRange;

        [Header("Debug")] [SerializeField] private bool drawInteractionRayForOneSecond;
        [SerializeField] private float interactionRayDrawDuration = 1f;
        private readonly RaycastHit2D[] lookHits2D = new RaycastHit2D[16];
        private readonly RaycastHit[] lookHits3D = new RaycastHit[16];
        private readonly bool[] mouseButtonsHeldPrev = new bool[3];
        private readonly bool[] mouseButtonsPressedOnObject = new bool[3];

        private Camera cachedCamera;
        private Collider2D cachedCollider2D;
        private Collider cachedCollider3D;
        private float clickTime;
        private Vector3 currentMouseHitPoint;
        private bool hasCurrentMouseHit;
        private bool keyHeldPrev;
        private Color lastDebugRayColor = Color.cyan;
        private Vector3 lastDebugRayEnd;
        private Vector3 lastDebugRayStart;
        private float lastDebugRayUntilTime;
        private PointerEventData.InputButton lastProcessedClickButton;
        private int lastProcessedClickFrame = -1;
        private bool wasHoveredByRaycast;
        private bool wasInRange;

        private void Awake()
        {
            InteractiveObjectSceneSetup.EnsureEventSystem(this, _autoCheckEventSystem, _autoCreateEventSystemIfMissing);

            RefreshCachedReferences();

            bool hasCollider3D = cachedCollider3D != null;
            bool hasCollider2D = cachedCollider2D != null;
            if (!InteractiveObjectSceneSetup.TryEnsureRaycasters(this, hasCollider3D, hasCollider2D))
            {
                enabled = false;
                return;
            }

            if (distanceCheckPoint == null && Camera.main != null)
            {
                distanceCheckPoint = Camera.main.transform;
            }

            if (viewCheckPoint == null)
            {
                viewCheckPoint = distanceCheckPoint != null ? distanceCheckPoint :
                    Camera.main != null ? Camera.main.transform : null;
            }
        }

        private void Update()
        {
            if (!interactable)
            {
                return;
            }

            if (useHoverDetection)
            {
                UpdateMouseHoverRaycast();
            }

            if (useMouseInteraction)
            {
                UpdateMouseInput();
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

            if (!inRange && interactionDistance > 0f && !useKeyboardInteraction)
            {
                return;
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

            if (drawInteractionRayForOneSecond && lastDebugRayUntilTime > 0f)
            {
                Gizmos.color = lastDebugRayColor;
                Gizmos.DrawLine(lastDebugRayStart, lastDebugRayEnd);
                Gizmos.DrawWireSphere(lastDebugRayEnd, 0.05f);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!interactable || !useMouseInteraction)
            {
                return;
            }

            if (!hasCurrentMouseHit)
            {
                return;
            }

            if (!CanMouseInteractAtPoint(currentMouseHitPoint))
            {
                return;
            }

            ProcessClick(eventData.button);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (interactable && useHoverDetection)
            {
                Vector3 rangePoint = eventData.pointerCurrentRaycast.isValid
                    ? eventData.pointerCurrentRaycast.worldPosition
                    : GetInteractionTargetPosition();
                bool inRange = interactionDistance > 0f ? IsInRange(rangePoint) : true;
                if (interactionDistance > 0f && !inRange)
                {
                    return;
                }

                IsHovered = true;
                onHoverEnter?.Invoke();
                onHoverChanged?.Invoke(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (interactable && useHoverDetection)
            {
                IsHovered = false;
                onHoverExit?.Invoke();
                onHoverChanged?.Invoke(false);
            }
        }

        private void UpdateMouseInput()
        {
            bool canUseCurrentMouseTarget = hasCurrentMouseHit && CanMouseInteractAtPoint(currentMouseHitPoint);

            for (int buttonIndex = 0; buttonIndex < mouseButtonsHeldPrev.Length; buttonIndex++)
            {
                if (!MouseInputCompat.TryGetButton(buttonIndex, out bool mouseHeld))
                {
                    mouseButtonsHeldPrev[buttonIndex] = false;
                    mouseButtonsPressedOnObject[buttonIndex] = false;
                    continue;
                }

                bool wasHeld = mouseButtonsHeldPrev[buttonIndex];
                bool startedOnObject = IsHovered && canUseCurrentMouseTarget;

                if (mouseHeld && !wasHeld)
                {
                    mouseButtonsPressedOnObject[buttonIndex] = startedOnObject;
                    if (buttonIndex == (int)downUpMouseButton && startedOnObject)
                    {
                        onInteractDown?.Invoke();
                    }
                }
                else if (!mouseHeld && wasHeld)
                {
                    if (buttonIndex == (int)downUpMouseButton && canUseCurrentMouseTarget)
                    {
                        onInteractUp?.Invoke();
                    }

                    if (mouseButtonsPressedOnObject[buttonIndex] && IsHovered && canUseCurrentMouseTarget)
                    {
                        ProcessClick((PointerEventData.InputButton)buttonIndex);
                    }

                    mouseButtonsPressedOnObject[buttonIndex] = false;
                }

                mouseButtonsHeldPrev[buttonIndex] = mouseHeld;
            }
        }

        private void UpdateMouseHoverRaycast()
        {
            RefreshCachedReferences();
            Camera cam = cachedCamera;
            if (cam == null)
            {
                hasCurrentMouseHit = false;
                return;
            }

            if (!HasEnabledInteractionCollider())
            {
                hasCurrentMouseHit = false;
                if (wasHoveredByRaycast)
                {
                    wasHoveredByRaycast = false;
                    if (IsHovered)
                    {
                        OnHoverExitRaycast();
                    }
                }

                return;
            }

            if (!TryGetCurrentMouseTargetHit(cam, out Vector3 hitPoint))
            {
                hasCurrentMouseHit = false;
                if (wasHoveredByRaycast && IsHovered)
                {
                    wasHoveredByRaycast = false;
                    OnHoverExitRaycast();
                }

                return;
            }

            currentMouseHitPoint = hitPoint;
            hasCurrentMouseHit = true;

            bool isHoveredNow = CanMouseInteractAtPoint(hitPoint);

            if (isHoveredNow && !wasHoveredByRaycast)
            {
                if (!IsHovered)
                {
                    OnHoverEnterRaycast();
                }

                if (IsHovered)
                {
                    wasHoveredByRaycast = true;
                }
            }
            else if (!isHoveredNow && wasHoveredByRaycast)
            {
                wasHoveredByRaycast = false;
                if (IsHovered)
                {
                    OnHoverExitRaycast();
                }
            }
            else if (IsHovered && !isHoveredNow)
            {
                OnHoverExitRaycast();
            }
        }

        private void OnHoverEnterRaycast()
        {
            if (!interactable || !useHoverDetection)
            {
                return;
            }

            // Match CanMouseInteractAtPoint: use the actual ray hit for distance/obstacle checks, not only collider center.
            Vector3 rangePoint = hasCurrentMouseHit ? currentMouseHitPoint : GetInteractionTargetPosition();
            bool inRange = interactionDistance > 0f ? IsInRange(rangePoint) : true;
            if (interactionDistance > 0f && !inRange)
            {
                return;
            }

            IsHovered = true;
            onHoverEnter?.Invoke();
            onHoverChanged?.Invoke(true);
        }

        private void OnHoverExitRaycast()
        {
            if (!interactable || !useHoverDetection)
            {
                return;
            }

            IsHovered = false;
            onHoverExit?.Invoke();
            onHoverChanged?.Invoke(false);
        }

        private void UpdateKeyboardInput()
        {
            bool keyDown = IsKeyboardActionDown();
            bool keyUp = IsKeyboardActionUp();

            if (!keyDown && !keyUp)
            {
                return;
            }

            bool inRange = interactionDistance > 0f ? IsInRange() : true;
            bool inView = IsInViewForKeyboardInteraction();
            bool canInteract = (inRange || interactionDistance <= 0f) && inView;

            if (keyDown && canInteract)
            {
                onInteractDown?.Invoke();
            }

            if (keyUp && canInteract)
            {
                onInteractUp?.Invoke();
            }
        }

        private bool IsInRange()
        {
            return IsInRange(GetInteractionTargetPosition());
        }

        private bool IsInRange(Vector3 targetPos)
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
            float distanceSqr = (targetPos - checkPointPos).sqrMagnitude;

            if (interactionDistance > 0f && distanceSqr > interactionDistance * interactionDistance)
            {
                return false;
            }

            if (checkObstacles)
            {
                Vector3 direction = targetPos - checkPointPos;
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

                RefreshCachedReferences();
                bool has3DCollider = cachedCollider3D != null && cachedCollider3D.enabled;
                bool has2DCollider = cachedCollider2D != null && cachedCollider2D.enabled;

                QueryTriggerInteraction obstacleTriggerMode = includeTriggerCollidersInObstacleCheck
                    ? QueryTriggerInteraction.Collide
                    : QueryTriggerInteraction.Ignore;

                if (has3DCollider)
                {
                    int hitCount = Physics.RaycastNonAlloc(checkPointPos, directionNormalized, lookHits3D,
                        checkDistance,
                        obstacleLayers, obstacleTriggerMode);
                    Collider nearestCollider = null;
                    float nearestDistance = float.MaxValue;
                    for (int i = 0; i < hitCount; i++)
                    {
                        Collider hitCollider = lookHits3D[i].collider;
                        if (hitCollider == null || ShouldIgnoreHitCollider(hitCollider))
                        {
                            continue;
                        }

                        if (lookHits3D[i].distance < nearestDistance)
                        {
                            nearestDistance = lookHits3D[i].distance;
                            nearestCollider = hitCollider;
                        }
                    }

                    if (nearestCollider != null && !IsTargetHierarchyCollider(nearestCollider))
                    {
                        return false;
                    }
                }
                else if (has2DCollider)
                {
                    Vector2 origin2D = new(checkPointPos.x, checkPointPos.y);
                    Vector2 direction2D = new(directionNormalized.x, directionNormalized.y);
                    ContactFilter2D filter = new()
                    {
                        useLayerMask = true,
                        layerMask = obstacleLayers,
                        useTriggers = includeTriggerCollidersInObstacleCheck
                    };
                    int hitCount2D = Physics2D.Raycast(origin2D, direction2D, filter, lookHits2D, checkDistance);
                    Collider2D nearestCollider2D = null;
                    float nearestDistance2D = float.MaxValue;
                    for (int i = 0; i < hitCount2D; i++)
                    {
                        Collider2D hitCollider = lookHits2D[i].collider;
                        if (hitCollider == null || ShouldIgnoreHitCollider(hitCollider))
                        {
                            continue;
                        }

                        if (lookHits2D[i].distance < nearestDistance2D)
                        {
                            nearestDistance2D = lookHits2D[i].distance;
                            nearestCollider2D = hitCollider;
                        }
                    }

                    if (nearestCollider2D != null && !IsTargetHierarchyCollider(nearestCollider2D))
                    {
                        return false;
                    }
                }
                else
                {
                    int hitCount = Physics.RaycastNonAlloc(checkPointPos, directionNormalized, lookHits3D,
                        checkDistance,
                        obstacleLayers, obstacleTriggerMode);
                    Collider nearestCollider = null;
                    float nearestDistance = float.MaxValue;
                    for (int i = 0; i < hitCount; i++)
                    {
                        Collider hitCollider = lookHits3D[i].collider;
                        if (hitCollider == null || ShouldIgnoreHitCollider(hitCollider))
                        {
                            continue;
                        }

                        if (lookHits3D[i].distance < nearestDistance)
                        {
                            nearestDistance = lookHits3D[i].distance;
                            nearestCollider = hitCollider;
                        }
                    }

                    if (nearestCollider != null && !IsTargetHierarchyCollider(nearestCollider))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool IsInViewForKeyboardInteraction()
        {
            if (keyboardInteractionMode == KeyboardInteractionMode.DistanceOnly)
            {
                return true;
            }

            if (!requireViewForKeyboardInteraction)
            {
                return true;
            }

            if (distanceCheckPoint == null)
            {
                return true;
            }

            Transform lookSource = ResolveLookSource();
            if (lookSource == null)
            {
                return true;
            }

            Vector3 origin = lookSource.position;
            Vector3 target = GetInteractionTargetPosition();
            Vector3 toTarget = target - origin;
            float distance = toTarget.magnitude;
            if (distance <= 0.001f)
            {
                return true;
            }

            if (!requireDirectLookRay || !checkObstacles)
            {
                CacheDebugRay(origin, target, Color.cyan);
                return true;
            }

            RefreshCachedReferences();

            if (cachedCollider3D != null && cachedCollider3D.enabled)
            {
                Vector3 forward = lookSource.forward.normalized;
                float maxRayDistance = interactionDistance > 0f ? interactionDistance + 0.05f : distance + 2f;
                QueryTriggerInteraction triggerMode = includeTriggerCollidersInLookRay
                    ? QueryTriggerInteraction.Collide
                    : QueryTriggerInteraction.Ignore;
                int hitCount = Physics.RaycastNonAlloc(origin, forward, lookHits3D, maxRayDistance,
                    ~0, triggerMode);
                float nearestDistance = float.MaxValue;
                Collider nearestCollider = null;
                Vector3 nearestPoint = origin + forward * maxRayDistance;
                for (int i = 0; i < hitCount; i++)
                {
                    Collider hitCollider = lookHits3D[i].collider;
                    if (hitCollider == null)
                    {
                        continue;
                    }

                    if (ShouldIgnoreHitCollider(hitCollider))
                    {
                        continue;
                    }

                    float hitDistance = lookHits3D[i].distance;
                    if (hitDistance < nearestDistance)
                    {
                        nearestDistance = hitDistance;
                        nearestCollider = hitCollider;
                        nearestPoint = lookHits3D[i].point;
                    }
                }

                bool hasTargetHit = nearestCollider != null && IsTargetHierarchyCollider(nearestCollider);
                Vector3 debugEnd = hasTargetHit
                    ? nearestCollider.ClosestPoint(origin)
                    : nearestPoint;
                CacheDebugRay(origin, debugEnd,
                    hasTargetHit ? Color.green : Color.red);
                return hasTargetHit;
            }

            if (cachedCollider2D != null && cachedCollider2D.enabled)
            {
                Vector2 origin2D = new(origin.x, origin.y);
                Vector2 dir2D = new Vector2(toTarget.x, toTarget.y).normalized;
                int hitCount2D = Physics2D.RaycastNonAlloc(origin2D, dir2D, lookHits2D, distance + 0.05f, ~0);
                float nearestDistance2D = float.MaxValue;
                Collider2D nearestCollider2D = null;
                for (int i = 0; i < hitCount2D; i++)
                {
                    Collider2D hitCollider = lookHits2D[i].collider;
                    if (hitCollider == null)
                    {
                        continue;
                    }

                    if (ShouldIgnoreHitCollider(hitCollider))
                    {
                        continue;
                    }

                    float hitDistance = lookHits2D[i].distance;
                    if (hitDistance < nearestDistance2D)
                    {
                        nearestDistance2D = hitDistance;
                        nearestCollider2D = hitCollider;
                    }
                }

                bool hasTargetHit = nearestCollider2D != null && nearestCollider2D.transform.IsChildOf(transform);
                Vector3 debugEnd = hasTargetHit ? nearestCollider2D.bounds.center : target;
                CacheDebugRay(origin, debugEnd, hasTargetHit ? Color.green : Color.red);
                return hasTargetHit;
            }

            CacheDebugRay(origin, target, Color.cyan);
            return true;
        }

        private Vector3 GetInteractionTargetPosition()
        {
            RefreshCachedReferences();
            if (cachedCollider3D != null)
            {
                return cachedCollider3D.bounds.center;
            }

            if (cachedCollider2D != null)
            {
                return cachedCollider2D.bounds.center;
            }

            return transform.position;
        }

        private bool CanMouseInteractAtPoint(Vector3 interactionPoint)
        {
            return (interactionDistance <= 0f && !checkObstacles) || IsInRange(interactionPoint);
        }

        private void ProcessClick(PointerEventData.InputButton button)
        {
            if (lastProcessedClickFrame == Time.frameCount && lastProcessedClickButton == button)
            {
                return;
            }

            lastProcessedClickFrame = Time.frameCount;
            lastProcessedClickButton = button;

            if (button == PointerEventData.InputButton.Left)
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
                return;
            }

            if (button == PointerEventData.InputButton.Right)
            {
                onRightClick.Invoke();
                return;
            }

            if (button == PointerEventData.InputButton.Middle)
            {
                onMiddleClick.Invoke();
            }
        }

        private bool HasEnabledInteractionCollider()
        {
            return (cachedCollider3D != null && cachedCollider3D.enabled) ||
                   (cachedCollider2D != null && cachedCollider2D.enabled);
        }

        private void RefreshCachedReferences()
        {
            if (cachedCamera == null)
            {
                cachedCamera = Camera.main ?? FindFirstObjectByType<Camera>();
            }

            cachedCollider3D = targetCollider3D != null
                ? targetCollider3D
                : GetComponent<Collider>() ?? GetComponentInChildren<Collider>(true);
            cachedCollider2D = targetCollider2D != null
                ? targetCollider2D
                : GetComponent<Collider2D>() ?? GetComponentInChildren<Collider2D>(true);
        }

        private bool TryGetCurrentMouseTargetHit(Camera cam, out Vector3 hitPoint)
        {
            hitPoint = Vector3.zero;

            if (cam == null || !MouseInputCompat.TryGetPosition(out Vector3 mousePos))
            {
                return false;
            }

            Ray ray = cam.ScreenPointToRay(mousePos);

            if (cachedCollider3D != null && cachedCollider3D.enabled)
            {
                QueryTriggerInteraction triggerInteraction = includeTriggerCollidersInMouseRaycast
                    ? QueryTriggerInteraction.Collide
                    : QueryTriggerInteraction.Ignore;
                int hitCount = Physics.RaycastNonAlloc(ray, lookHits3D, float.MaxValue, ~0, triggerInteraction);
                float nearestTargetDistance = float.MaxValue;
                float nearestBlockingDistance = float.MaxValue;
                bool hasTargetHit = false;

                for (int i = 0; i < hitCount; i++)
                {
                    Collider hitCollider = lookHits3D[i].collider;
                    if (hitCollider == null || ShouldIgnoreHitCollider(hitCollider))
                    {
                        continue;
                    }

                    float hitDistance = lookHits3D[i].distance;
                    if (IsTargetHierarchyCollider(hitCollider))
                    {
                        if (hitDistance < nearestTargetDistance)
                        {
                            nearestTargetDistance = hitDistance;
                            hitPoint = lookHits3D[i].point;
                            hasTargetHit = true;
                        }
                    }
                    else if (!hitCollider.isTrigger && hitDistance < nearestBlockingDistance)
                    {
                        nearestBlockingDistance = hitDistance;
                    }
                }

                if (!checkObstacles)
                {
                    return hasTargetHit;
                }

                return hasTargetHit && nearestTargetDistance <= nearestBlockingDistance;
            }

            if (cachedCollider2D != null && cachedCollider2D.enabled)
            {
                int hitCount2D = Physics2D.GetRayIntersectionNonAlloc(ray, lookHits2D, float.MaxValue, ~0);
                float nearestTargetDistance2D = float.MaxValue;
                float nearestBlockingDistance2D = float.MaxValue;
                bool hasTargetHit2D = false;

                for (int i = 0; i < hitCount2D; i++)
                {
                    Collider2D hitCollider = lookHits2D[i].collider;
                    if (hitCollider == null || ShouldIgnoreHitCollider(hitCollider))
                    {
                        continue;
                    }

                    float hitDistance = lookHits2D[i].distance;
                    if (IsTargetHierarchyCollider(hitCollider))
                    {
                        if (hitDistance < nearestTargetDistance2D)
                        {
                            nearestTargetDistance2D = hitDistance;
                            hitPoint = lookHits2D[i].point;
                            hasTargetHit2D = true;
                        }
                    }
                    else if (!hitCollider.isTrigger && hitDistance < nearestBlockingDistance2D)
                    {
                        nearestBlockingDistance2D = hitDistance;
                    }
                }

                if (!checkObstacles)
                {
                    return hasTargetHit2D;
                }

                return hasTargetHit2D && nearestTargetDistance2D <= nearestBlockingDistance2D;
            }

            return false;
        }

        private bool ShouldIgnoreHitCollider(Component hitCollider)
        {
            if (hitCollider == null || !ignoreDistancePointHierarchyColliders)
            {
                return false;
            }

            Transform hitTransform = hitCollider.transform;
            return IsSameHierarchy(hitTransform, distanceCheckPoint) ||
                   IsSameHierarchy(hitTransform, viewCheckPoint);
        }

        private bool IsTargetHierarchyCollider(Component hitCollider)
        {
            if (hitCollider == null)
            {
                return false;
            }

            if (hitCollider is Collider hitCollider3D)
            {
                if (targetCollider3D != null)
                {
                    return hitCollider3D == targetCollider3D;
                }

                Transform t = hitCollider3D.transform;
                return t == transform || t.IsChildOf(transform);
            }

            if (hitCollider is Collider2D hitCollider2D)
            {
                if (targetCollider2D != null)
                {
                    return hitCollider2D == targetCollider2D;
                }

                Transform t2 = hitCollider2D.transform;
                return t2 == transform || t2.IsChildOf(transform);
            }

            return false;
        }

        private Transform ResolveLookSource()
        {
            if (viewCheckPoint != null)
            {
                return viewCheckPoint;
            }

            if (Camera.main != null)
            {
                return Camera.main.transform;
            }

            return distanceCheckPoint;
        }

        private static bool IsSameHierarchy(Transform a, Transform b)
        {
            if (a == null || b == null)
            {
                return false;
            }

            return a == b || a.IsChildOf(b) || b.IsChildOf(a) || a.root == b.root;
        }

        private void CacheDebugRay(Vector3 start, Vector3 end, Color color)
        {
            if (!drawInteractionRayForOneSecond)
            {
                return;
            }

            lastDebugRayStart = start;
            lastDebugRayEnd = end;
            lastDebugRayColor = color;
            lastDebugRayUntilTime = float.PositiveInfinity;
        }


        private bool IsKeyboardActionDown()
        {
            return KeyInputCompat.GetKeyDown(keyboardKey);
        }

        private bool IsKeyboardActionUp()
        {
            return KeyInputCompat.GetKeyUp(keyboardKey);
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
        ///     Enable or disable mouse click/down/up interaction.
        /// </summary>
        public bool UseMouseInteraction
        {
            get => useMouseInteraction;
            set => useMouseInteraction = value;
        }

        /// <summary>
        ///     Enable or disable hover detection (cursor over collider).
        /// </summary>
        public bool UseHoverDetection
        {
            get => useHoverDetection;
            set => useHoverDetection = value;
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
