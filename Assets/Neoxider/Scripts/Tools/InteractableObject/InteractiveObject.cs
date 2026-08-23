using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using Neo.Network;
#if MIRROR
using Mirror;
#endif

namespace Neo.Tools
{
    /// <summary>
    ///     Universal interactive object component with mouse, keyboard, and distance-based interaction support.
    /// </summary>
    [NeoDoc("Tools/InteractableObject/InteractiveObject.md")]
    [CreateFromMenu("Neoxider/Tools/Interact/InteractiveObject",
        "Prefabs/Tools/Interact/Interactive Sphere.prefab")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(InteractiveObject))]
    public class InteractiveObject : NeoNetworkComponent, IInteractiveTarget,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
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

        [Tooltip(
            "Who may trigger this interaction over the network. Default None lets NoCode scene objects work without ownership.")]
        [SerializeField]
        [FormerlySerializedAs("requireAuthority")]
        private NetworkAuthorityMode authorityMode = NetworkAuthorityMode.None;

        public bool interactable = true;

        [Header("Interaction Settings")] [Tooltip("Enable hover detection (cursor over collider).")] [SerializeField]
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

        [Tooltip(
            "For keyboard: require a forward ray from the view source to hit this object's collider. Whether the ray is required at all is not tied to checkObstacles; whether obstacles along that ray block the aim is - exactly like the mouse ray.")]
        [SerializeField]
        private bool requireDirectLookRay = true;

        [Tooltip("Include trigger colliders in look ray checks.")] [SerializeField]
        private bool includeTriggerCollidersInLookRay = true;

        [Tooltip(
            "Include trigger colliders in the mouse hover raycast. Enable for objects with a Trigger Collider, " +
            "otherwise the ray cannot see this object at all. Applies to 3D and 2D alike: 3D excludes triggers " +
            "inside the query, 2D filters them out of the result because Physics2D has no per-query trigger " +
            "option. In 2D the global Physics2D.queriesHitTriggers still applies first — this flag can only " +
            "narrow that result, never widen it.")]
        [SerializeField]
        private bool includeTriggerCollidersInMouseRaycast = true;

        [Tooltip(
            "Per-object override: use screen center ray. Prefer adding InteractionRayProvider to the camera instead.")]
        [SerializeField]
        private bool useScreenCenterRay;

        [Header("Target Colliders")]
        [Tooltip(
            "Optional explicit 3D collider used for hover, click, and view checks. If not set, uses Collider on this GameObject only.")]
        [SerializeField]
        private Collider targetCollider3D;

        [Tooltip(
            "Optional explicit 2D collider used for hover, click, and view checks. If not set, uses Collider2D on this GameObject only.")]
        [SerializeField]
        private Collider2D targetCollider2D;

        [Header("Distance Control")] [Tooltip("Maximum interaction distance (0 = unlimited).")] [SerializeField]
        private float interactionDistance = 3f;

        [Tooltip("Reference point for distance check (player/camera). Uses main camera if not set.")] [SerializeField]
        private Transform distanceCheckPoint;

        [Tooltip("Reference point for look direction checks. Uses main camera if not set.")] [SerializeField]
        private Transform viewCheckPoint;

        [Tooltip(
            "Check for obstacles (walls) between object and check point. When enabled, also requires the mouse ray and the keyboard look ray to hit this object before any non-trigger collider (line-of-sight for hover, click and key). When disabled, distance checks skip obstacles and both rays accept a hit on this object even if solid geometry is closer. Foreign trigger volumes never block either ray.")]
        [SerializeField]
        private bool checkObstacles = true;

        [Tooltip("Layers that block interaction (used when checkObstacles is enabled).")] [SerializeField]
        private LayerMask obstacleLayers = -1;

        [Tooltip("Include trigger colliders in obstacle ray checks.")] [SerializeField]
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

        [Header("Debug")]
        [Tooltip(
            "Draw a debug ray every frame (always visible while selected or in play mode). Color changes based on state: gray=no target, cyan=in range, yellow=hovered, green=interacting.")]
        [SerializeField]
        private bool drawDebugRay;

        [Tooltip("Legacy: draw ray briefly on interaction only.")] [SerializeField]
        private bool drawInteractionRayForOneSecond;

        [SerializeField] private float interactionRayDrawDuration = 1f;
        private readonly RaycastHit2D[] lookHits2D = new RaycastHit2D[16];
        private readonly RaycastHit[] lookHits3D = new RaycastHit[16];
        private readonly InteractionRayHit[] interactionHits = new InteractionRayHit[16];
        private readonly bool[] mouseButtonsHeldPrev = new bool[3];
        private readonly bool[] mouseButtonsPressedOnObject = new bool[3];

        private Camera cachedCamera;
        private Collider2D cachedCollider2D;
        private Collider cachedCollider3D;
        private float clickTime;
        private bool hasClickTime;
        private Vector3 currentMouseHitPoint;
        private bool hasCurrentMouseHit;
        private bool keyHeldPrev;
        private Color lastDebugRayColor = Color.cyan;
        private Vector3 lastDebugRayEnd;
        private Vector3 lastDebugRayStart;
        private float lastDebugRayUntilTime;
        private bool isInteractingThisFrame;
        private PointerEventData.InputButton lastProcessedClickButton;
        private int lastProcessedClickFrame = -1;
        private bool wasHoveredByRaycast;
        private bool wasInRange;

#if MIRROR
        protected override void OnValidate()
        {
            if (isNetworked)
            {
                base.OnValidate();
            }
        }
#endif

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

            // Preserve the scene-authoring default: checkpoints auto-bind only to a tagged main camera.
            Camera interactionCamera = InteractionCameraResolver.Resolve(null, false);
            if (distanceCheckPoint == null && interactionCamera != null)
            {
                distanceCheckPoint = interactionCamera.transform;
            }

            if (viewCheckPoint == null)
            {
                viewCheckPoint = distanceCheckPoint != null ? distanceCheckPoint :
                    interactionCamera != null ? interactionCamera.transform : null;
            }
        }

        private void Update()
        {
            isInteractingThisFrame = false;

            if (!interactable)
            {
                return;
            }

            // WHY: One raycast feeds both hover state and click/press handling, so it must run when
            // either feature is enabled - and exactly once per frame when both are.
            if (useHoverDetection || useMouseInteraction)
            {
                UpdateMouseTargetRaycast();
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
                UpdatePersistentDebugRay();
                return;
            }

            if (useKeyboardInteraction)
            {
                UpdateKeyboardInput();
            }

            UpdatePersistentDebugRay();
        }

        private void OnDrawGizmosSelected()
        {
            if (interactionDistance > 0f)
            {
                Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
                Gizmos.DrawWireSphere(transform.position, interactionDistance);
            }

            bool showLegacy = drawInteractionRayForOneSecond && Time.realtimeSinceStartup <= lastDebugRayUntilTime;
            if (drawDebugRay || showLegacy)
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

            ProcessClickEvent(eventData.button);
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

            // WHY: IsHovered only exists while hover detection is on; with it off the raycast hit alone
            // decides whether the pointer is on this object, so clicks keep working independently.
            bool pointerOnTarget = canUseCurrentMouseTarget && (!useHoverDetection || IsHovered);

            for (int buttonIndex = 0; buttonIndex < mouseButtonsHeldPrev.Length; buttonIndex++)
            {
                if (!MouseInputCompat.TryGetButton(buttonIndex, out bool mouseHeld))
                {
                    mouseButtonsHeldPrev[buttonIndex] = false;
                    mouseButtonsPressedOnObject[buttonIndex] = false;
                    continue;
                }

                bool wasHeld = mouseButtonsHeldPrev[buttonIndex];

                if (mouseHeld && !wasHeld)
                {
                    mouseButtonsPressedOnObject[buttonIndex] = pointerOnTarget;
                    if (buttonIndex == (int)downUpMouseButton && pointerOnTarget)
                    {
                        isInteractingThisFrame = true;
                        TriggerInteractDown();
                    }
                }
                else if (!mouseHeld && wasHeld)
                {
                    if (buttonIndex == (int)downUpMouseButton && canUseCurrentMouseTarget)
                    {
                        TriggerInteractUp();
                    }

                    if (mouseButtonsPressedOnObject[buttonIndex] && pointerOnTarget)
                    {
                        ProcessClickEvent((PointerEventData.InputButton)buttonIndex);
                    }

                    mouseButtonsPressedOnObject[buttonIndex] = false;
                }

                mouseButtonsHeldPrev[buttonIndex] = mouseHeld;
            }
        }

        /// <summary>
        ///     Resolves the current mouse (or screen-center) target hit for click and press handling,
        ///     and drives hover enter/exit only while <see cref="useHoverDetection" /> is enabled.
        /// </summary>
        private void UpdateMouseTargetRaycast()
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

            // WHY: Match CanMouseInteractAtPoint: use the actual ray hit for distance/obstacle checks, not only collider center.
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
                isInteractingThisFrame = true;
                TriggerInteractDown();
            }

            if (keyUp && canInteract)
            {
                TriggerInteractUp();
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
            if (!InteractionQueryMath.IsWithinRange(checkPointPos, targetPos, interactionDistance))
            {
                return false;
            }

            if (checkObstacles)
            {
                Vector3 direction = targetPos - checkPointPos;
                float distance = direction.magnitude;

                if (distance < 0.01f)
                {
                    return true;
                }

                Vector3 directionNormalized = direction.normalized;
                float checkDistance = InteractionQueryMath.GetObstacleCheckDistance(distance);

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
                    int candidateCount = BuildInteractionHits3D(hitCount, true);
                    if (InteractionQueryMath.TryGetNearestHit(interactionHits, candidateCount,
                            out InteractionRayHit nearestHit) && !nearestHit.IsTarget)
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
                    int candidateCount2D = BuildInteractionHits2D(hitCount2D, true, true);
                    if (InteractionQueryMath.TryGetNearestHit(interactionHits, candidateCount2D,
                            out InteractionRayHit nearestHit2D) && !nearestHit2D.IsTarget)
                    {
                        return false;
                    }
                }
                else
                {
                    int hitCount = Physics.RaycastNonAlloc(checkPointPos, directionNormalized, lookHits3D,
                        checkDistance,
                        obstacleLayers, obstacleTriggerMode);
                    int candidateCount = BuildInteractionHits3D(hitCount, true);
                    if (InteractionQueryMath.TryGetNearestHit(interactionHits, candidateCount,
                            out InteractionRayHit nearestHit) && !nearestHit.IsTarget)
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

            Transform lookSource = ResolveLookSource();
            if (lookSource == null)
            {
                return false;
            }

            Vector3 origin = lookSource.position;
            Vector3 target = GetInteractionTargetPosition();
            Vector3 toTarget = target - origin;
            float distance = toTarget.magnitude;
            if (distance <= 0.001f)
            {
                return true;
            }

            // WHY: Without a look ray — just "in zone" (and other mode flags). Previously the ray was disabled together with
            // checkObstacles, which caused the key to trigger at a single distance when obstacles were off.
            if (!requireDirectLookRay)
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
                // WHY: The look ray answers the same question as the mouse ray - "is this object the one I am
                // aiming at" - so it must read the same serialized settings. Both arguments used to be a
                // hardcoded true: foreign trigger volumes counted as blockers and line-of-sight was demanded
                // even with checkObstacles off. A pickup standing inside a door's trigger volume was therefore
                // clickable with the mouse while E silently did nothing.
                int candidateCount = BuildInteractionHits3D(hitCount, false);
                bool hasTargetHit = InteractionQueryMath.TrySelectTarget(interactionHits, candidateCount,
                    checkObstacles, out InteractionRayHit targetHit);
                Vector3 debugEnd = hasTargetHit ? targetHit.Point : origin + forward * maxRayDistance;
                if (!hasTargetHit && InteractionQueryMath.TryGetNearestHit(interactionHits, candidateCount,
                        out InteractionRayHit nearestHit))
                {
                    debugEnd = nearestHit.Point;
                }

                CacheDebugRay(origin, debugEnd,
                    hasTargetHit ? Color.green : Color.red);
                return hasTargetHit;
            }

            if (cachedCollider2D != null && cachedCollider2D.enabled)
            {
                Vector2 origin2D = new(origin.x, origin.y);
                Vector3 fwd = lookSource.forward;
                Vector2 dir2D = new Vector2(fwd.x, fwd.y);
                if (dir2D.sqrMagnitude < 1e-6f)
                {
                    dir2D = new Vector2(toTarget.x, toTarget.y);
                }

                dir2D.Normalize();
                float maxRay2D = interactionDistance > 0f ? interactionDistance + 0.05f : distance + 2f;
                int hitCount2D = Physics2D.RaycastNonAlloc(origin2D, dir2D, lookHits2D, maxRay2D, ~0);
                // WHY: Same serialized settings as the 3D branch above and as the mouse ray.
                int candidateCount2D = BuildInteractionHits2D(hitCount2D, false,
                    includeTriggerCollidersInLookRay);
                bool hasTargetHit = InteractionQueryMath.TrySelectTarget(interactionHits, candidateCount2D,
                    checkObstacles, out InteractionRayHit targetHit);
                Vector3 debugEnd = hasTargetHit ? targetHit.Point : origin + (Vector3)(dir2D * maxRay2D);
                if (!hasTargetHit && InteractionQueryMath.TryGetNearestHit(interactionHits, candidateCount2D,
                        out InteractionRayHit nearestHit))
                {
                    debugEnd = nearestHit.Point;
                }

                CacheDebugRay(origin, debugEnd, hasTargetHit ? Color.green : Color.red);
                return hasTargetHit;
            }

            // WHY: A look ray is required, but there is no valid collider — cannot confirm the aim.
            CacheDebugRay(origin, target, Color.red);
            return false;
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

        private void ProcessClickEvent(PointerEventData.InputButton button)
        {
            if (lastProcessedClickFrame == Time.frameCount && lastProcessedClickButton == button)
            {
                return;
            }

            lastProcessedClickFrame = Time.frameCount;
            lastProcessedClickButton = button;

            if (button == PointerEventData.InputButton.Left)
            {
                bool isDouble = hasClickTime && doubleClickThreshold > 0f &&
                                Time.time - clickTime < doubleClickThreshold;
                TriggerClick(button, isDouble);
                clickTime = Time.time;
                hasClickTime = true;
            }
            else
            {
                TriggerClick(button, false);
            }
        }

        /// <summary>
        ///     Triggers the interact-down event through the same local or Mirror network path as configured input.
        /// </summary>
        [Button("Test Interact Down", PlayModeOnly = true)]
        public void InteractDown()
        {
            if (!interactable)
            {
                return;
            }

            isInteractingThisFrame = true;
            TriggerInteractDown();
        }

        /// <summary>
        ///     Triggers the interact-up event through the same local or Mirror network path as configured input.
        /// </summary>
        [Button("Test Interact Up", PlayModeOnly = true)]
        public void InteractUp()
        {
            if (!interactable)
            {
                return;
            }

            TriggerInteractUp();
        }

        /// <summary>
        ///     Triggers a click through the same local or Mirror network path as configured input.
        /// </summary>
        /// <param name="button">Mouse button event to invoke.</param>
        /// <param name="isDouble">Invokes the double-click event for the left button.</param>
        [Button("Test Click", PlayModeOnly = true)]
        public void Click(MouseButton button = MouseButton.Left, bool isDouble = false)
        {
            if (!interactable)
            {
                return;
            }

            TriggerClick((PointerEventData.InputButton)button, isDouble);
        }

        private void TriggerInteractDown()
        {
#if MIRROR
            if (isNetworked && NeoNetworkState.IsNetworkActive)
            {
                if (NeoNetworkState.IsServer)
                {
                    bool skipHostLocalRpc = NeoNetworkState.IsClient;
                    onInteractDown?.Invoke();
                    RpcInteractDown(skipHostLocalRpc);
                }
                else if (NeoNetworkState.IsClient)
                {
                    CmdInteractDown();
                }

                return;
            }
#endif
            onInteractDown?.Invoke();
        }

        private void TriggerInteractUp()
        {
#if MIRROR
            if (isNetworked && NeoNetworkState.IsNetworkActive)
            {
                if (NeoNetworkState.IsServer)
                {
                    bool skipHostLocalRpc = NeoNetworkState.IsClient;
                    onInteractUp?.Invoke();
                    RpcInteractUp(skipHostLocalRpc);
                }
                else if (NeoNetworkState.IsClient)
                {
                    CmdInteractUp();
                }

                return;
            }
#endif
            onInteractUp?.Invoke();
        }

        private void TriggerClick(PointerEventData.InputButton button, bool isDouble = false)
        {
#if MIRROR
            if (isNetworked && NeoNetworkState.IsNetworkActive)
            {
                if (NeoNetworkState.IsServer)
                {
                    bool skipHostLocalRpc = NeoNetworkState.IsClient;
                    InvokeClick(button, isDouble);
                    RpcClick((int)button, isDouble, skipHostLocalRpc);
                }
                else if (NeoNetworkState.IsClient)
                {
                    CmdClick((int)button, isDouble);
                }

                return;
            }
#endif
            InvokeClick(button, isDouble);
        }

        private void InvokeClick(PointerEventData.InputButton button, bool isDouble)
        {
            if (button == PointerEventData.InputButton.Left)
            {
                if (isDouble)
                {
                    onDoubleClick?.Invoke();
                }
                else
                {
                    onClick?.Invoke();
                }
            }
            else if (button == PointerEventData.InputButton.Right)
            {
                onRightClick?.Invoke();
            }
            else if (button == PointerEventData.InputButton.Middle)
            {
                onMiddleClick?.Invoke();
            }
        }

#if MIRROR
        private bool AuthorizedSender(NetworkConnectionToClient sender)
        {
            return NeoNetworkState.IsAuthorized(gameObject, sender, authorityMode);
        }

        [Command(requiresAuthority = false)]
        private void CmdInteractDown(NetworkConnectionToClient sender = null)
        {
            if (RateLimitCheck())
            {
                return;
            }

            if (!AuthorizedSender(sender))
            {
                return;
            }

            if (isServerOnly)
            {
                onInteractDown?.Invoke();
            }

            RpcInteractDown(false);
        }

        [ClientRpc(includeOwner = true)]
        private void RpcInteractDown(bool skipHostLocal)
        {
            if (skipHostLocal && NeoNetworkState.IsHost)
            {
                return;
            }

            onInteractDown?.Invoke();
        }

        [Command(requiresAuthority = false)]
        private void CmdInteractUp(NetworkConnectionToClient sender = null)
        {
            if (RateLimitCheck())
            {
                return;
            }

            if (!AuthorizedSender(sender))
            {
                return;
            }

            if (isServerOnly)
            {
                onInteractUp?.Invoke();
            }

            RpcInteractUp(false);
        }

        [ClientRpc(includeOwner = true)]
        private void RpcInteractUp(bool skipHostLocal)
        {
            if (skipHostLocal && NeoNetworkState.IsHost)
            {
                return;
            }

            onInteractUp?.Invoke();
        }

        [Command(requiresAuthority = false)]
        private void CmdClick(int buttonInt, bool isDouble, NetworkConnectionToClient sender = null)
        {
            if (RateLimitCheck())
            {
                return;
            }

            if (!AuthorizedSender(sender))
            {
                return;
            }

            if (isServerOnly)
            {
                InvokeClick((PointerEventData.InputButton)buttonInt, isDouble);
            }

            RpcClick(buttonInt, isDouble, false);
        }

        [ClientRpc(includeOwner = true)]
        private void RpcClick(int buttonInt, bool isDouble, bool skipHostLocal)
        {
            if (skipHostLocal && NeoNetworkState.IsHost)
            {
                return;
            }

            InvokeClick((PointerEventData.InputButton)buttonInt, isDouble);
        }
#endif

        private bool HasEnabledInteractionCollider()
        {
            return (cachedCollider3D != null && cachedCollider3D.enabled) ||
                   (cachedCollider2D != null && cachedCollider2D.enabled);
        }

        // WHY: Guard: colliders are resolved once in Awake (or on explicit invalidation) to avoid
        // per-frame GetComponent calls. Camera uses the existing null-check guard already present here.
        private bool _collidersResolved;

        private void RefreshCachedReferences()
        {
            ResolveCamera();

            if (_collidersResolved)
            {
                return;
            }

            cachedCollider3D = targetCollider3D != null
                ? targetCollider3D
                : GetComponent<Collider>() ?? GetComponentInChildren<Collider>(true);
            cachedCollider2D = targetCollider2D != null
                ? targetCollider2D
                : GetComponent<Collider2D>() ?? GetComponentInChildren<Collider2D>(true);
            _collidersResolved = true;
        }

        /// <summary>
        ///     Forces collider references to be re-resolved on the next call to
        ///     <see cref="RefreshCachedReferences"/>. Call this if a collider is added,
        ///     removed, or replaced on the GameObject at runtime.
        /// </summary>
        [Button("Invalidate Colliders", PlayModeOnly = true)]
        public void InvalidateCachedColliders()
        {
            _collidersResolved = false;
        }

        private bool TryGetCurrentMouseTargetHit(Camera cam, out Vector3 hitPoint)
        {
            hitPoint = Vector3.zero;

            if (cam == null)
            {
                return false;
            }

            Ray ray;
            InteractionRayProvider provider = InteractionRayProvider.FindOnMainCamera();
            bool useCenterRay = useScreenCenterRay || (provider != null && provider.UseScreenCenterForHover);

            if (useCenterRay)
            {
                ray = cam.ScreenPointToRay(new Vector3(cam.pixelWidth * 0.5f, cam.pixelHeight * 0.5f, 0f));
            }
            else
            {
                if (!MouseInputCompat.TryGetPosition(out Vector3 mousePos))
                {
                    return false;
                }

                // WHY: Guard against the new InputSystem sentinel (inf,-inf) and other NaN/out-of-frustum
                // values that surface in headless / PlayMode-test sessions without a real mouse device.
                // Without this guard Camera.ScreenPointToRay logs "Screen position out of view frustum"
                // and breaks every test that has an InteractiveObject in the scene.
                if (!float.IsFinite(mousePos.x) || !float.IsFinite(mousePos.y) ||
                    mousePos.x < 0f || mousePos.y < 0f ||
                    mousePos.x > cam.pixelWidth || mousePos.y > cam.pixelHeight)
                {
                    return false;
                }

                ray = cam.ScreenPointToRay(mousePos);
            }

            if (cachedCollider3D != null && cachedCollider3D.enabled)
            {
                QueryTriggerInteraction triggerInteraction = includeTriggerCollidersInMouseRaycast
                    ? QueryTriggerInteraction.Collide
                    : QueryTriggerInteraction.Ignore;
                int hitCount = Physics.RaycastNonAlloc(ray, lookHits3D, float.MaxValue, ~0, triggerInteraction);
                int candidateCount = BuildInteractionHits3D(hitCount, false);
                bool hasTargetHit = InteractionQueryMath.TrySelectTarget(interactionHits, candidateCount,
                    checkObstacles, out InteractionRayHit targetHit);
                hitPoint = targetHit.Point;
                return hasTargetHit;
            }

            if (cachedCollider2D != null && cachedCollider2D.enabled)
            {
                // WHY: 2D honours includeTriggerCollidersInMouseRaycast by filtering hits, not the query.
                // Physics2D.GetRayIntersectionNonAlloc takes no QueryTriggerInteraction — it obeys the global
                // Physics2D.queriesHitTriggers — so the 3D trick of excluding triggers inside the query is not
                // available here. BuildInteractionHits2D drops them afterwards and reaches the same result.
                // The literal true that used to stand here made the checkbox a silent no-op for 2D objects.
                int hitCount2D = Physics2D.GetRayIntersectionNonAlloc(ray, lookHits2D, float.MaxValue, ~0);
                int candidateCount2D = BuildInteractionHits2D(hitCount2D, false,
                    includeTriggerCollidersInMouseRaycast);
                bool hasTargetHit2D = InteractionQueryMath.TrySelectTarget(interactionHits, candidateCount2D,
                    checkObstacles, out InteractionRayHit targetHit2D);
                hitPoint = targetHit2D.Point;
                return hasTargetHit2D;
            }

            return false;
        }

        private int BuildInteractionHits3D(int hitCount, bool foreignTriggersBlock)
        {
            int candidateCount = 0;
            int validCount = Mathf.Min(hitCount, lookHits3D.Length);
            for (int i = 0; i < validCount; i++)
            {
                RaycastHit physicsHit = lookHits3D[i];
                Collider hitCollider = physicsHit.collider;
                if (hitCollider == null || ShouldIgnoreHitCollider(hitCollider))
                {
                    continue;
                }

                bool isTarget = IsTargetHierarchyCollider(hitCollider);
                bool blocksInteraction = !isTarget && (foreignTriggersBlock || !hitCollider.isTrigger);
                interactionHits[candidateCount] = new InteractionRayHit(physicsHit.distance, physicsHit.point,
                    isTarget, blocksInteraction);
                candidateCount++;
            }

            return candidateCount;
        }

        private int BuildInteractionHits2D(int hitCount, bool foreignTriggersBlock, bool includeTriggers)
        {
            int candidateCount = 0;
            int validCount = Mathf.Min(hitCount, lookHits2D.Length);
            for (int i = 0; i < validCount; i++)
            {
                RaycastHit2D physicsHit = lookHits2D[i];
                Collider2D hitCollider = physicsHit.collider;
                if (hitCollider == null || ShouldIgnoreHitCollider(hitCollider) ||
                    (!includeTriggers && hitCollider.isTrigger))
                {
                    continue;
                }

                bool isTarget = IsTargetHierarchyCollider(hitCollider);
                bool blocksInteraction = !isTarget && (foreignTriggersBlock || !hitCollider.isTrigger);
                interactionHits[candidateCount] = new InteractionRayHit(physicsHit.distance, physicsHit.point,
                    isTarget, blocksInteraction);
                candidateCount++;
            }

            return candidateCount;
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

            Camera interactionCamera = ResolveCamera();
            if (interactionCamera != null)
            {
                return interactionCamera.transform;
            }

            return distanceCheckPoint;
        }

        private Camera ResolveCamera()
        {
            cachedCamera = InteractionCameraResolver.Resolve(cachedCamera);
            return cachedCamera;
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
            if (!drawInteractionRayForOneSecond && !drawDebugRay)
            {
                return;
            }

            lastDebugRayStart = start;
            lastDebugRayEnd = end;
            lastDebugRayColor = color;
            lastDebugRayUntilTime = drawDebugRay
                ? float.PositiveInfinity
                : Time.realtimeSinceStartup + Mathf.Max(0f, interactionRayDrawDuration);
        }

        /// <summary>
        ///     Draws a persistent debug ray every frame.
        ///     Colors: gray = no target, cyan = target in range, yellow = hovered, green = interacting.
        /// </summary>
        private void UpdatePersistentDebugRay()
        {
            if (!drawDebugRay)
            {
                return;
            }

            Transform source = ResolveLookSource();
            if (source == null)
            {
                return;
            }

            Vector3 origin = source.position;
            Vector3 target = GetInteractionTargetPosition();
            Vector3 end = target;

            Color rayColor;
            if (isInteractingThisFrame)
            {
                rayColor = Color.green;
            }
            else if (IsHovered)
            {
                rayColor = Color.yellow;
            }
            else if (wasInRange)
            {
                rayColor = Color.cyan;
            }
            else
            {
                rayColor = Color.gray;
            }

            lastDebugRayStart = origin;
            lastDebugRayEnd = end;
            lastDebugRayColor = rayColor;

            Debug.DrawLine(origin, end, rayColor);
        }


        private bool IsKeyboardActionDown()
        {
            return KeyInputCompat.GetKeyDown(keyboardKey);
        }

        private bool IsKeyboardActionUp()
        {
            return KeyInputCompat.GetKeyUp(keyboardKey);
        }

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
        ///     When enabled, interaction events are replicated through Mirror while a network session is active.
        ///     When disabled, or when no network session is active, events run locally.
        /// </summary>
        public bool IsNetworked
        {
            get => isNetworked;
            set => isNetworked = value;
        }

        /// <summary>
        ///     Manual NoCode authority policy. Defaults to None so scene objects work without ownership.
        /// </summary>
        public NetworkAuthorityMode AuthorityMode
        {
            get => authorityMode;
            set => authorityMode = value;
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
        ///     Whether this target currently accepts commands through <see cref="IInteractiveTarget" />.
        /// </summary>
        public bool IsInteractable => interactable;

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

        /// <summary>
        ///     When true, hover and click raycasts fire from the center of the screen
        ///     instead of the mouse/touch position. Ideal for mobile FPS/TPS games
        ///     where a fixed crosshair is displayed at the screen center
        ///     and the player swipes to rotate the camera.
        ///     <para>
        ///         Prefer adding <see cref="InteractionRayProvider" /> to the camera
        ///         for global control instead of setting this per object.
        ///     </para>
        /// </summary>
        public bool UseScreenCenterRay
        {
            get => useScreenCenterRay;
            set => useScreenCenterRay = value;
        }
    }
}
