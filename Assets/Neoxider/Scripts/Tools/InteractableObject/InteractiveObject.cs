using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System;
using System.Reflection;

namespace Neo
{
    namespace Tools
    {
        /// <summary>
        ///     Universal interactive object component with mouse, keyboard, and distance-based interaction support.
        /// </summary>
        [NeoDoc("Tools/InteractableObject/InteractiveObject.md")]
        [CreateFromMenu("Neoxider/Tools/InteractiveObject", "Prefabs/Tools/Interact/Interactive Sphere.prefab")]
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

            [Tooltip("ViewOrMouse: keyboard requires looking at object. DistanceOnly: only distance check.")]
            [SerializeField]
            private KeyboardInteractionMode keyboardInteractionMode = KeyboardInteractionMode.ViewOrMouse;

            [Tooltip("Require looking at object when interacting with keyboard.")] [SerializeField]
            private bool requireViewForKeyboardInteraction = true;

            [Tooltip("Minimum dot product for look check. Higher value means narrower interaction cone.")]
            [Range(-1f, 1f)]
            [SerializeField]
            private float minLookDot = 0.55f;

            [Tooltip("Require direct line of sight from check point to object.")] [SerializeField]
            private bool requireDirectLookRay = true;

            [Tooltip("Include trigger colliders in look ray checks.")] [SerializeField]
            private bool includeTriggerCollidersInLookRay = true;

            [Tooltip("Include trigger colliders in mouse hover raycast. Enable for objects with Trigger Collider.")] [SerializeField]
            private bool includeTriggerCollidersInMouseRaycast = true;

            [Header("Distance Control")] [Tooltip("Maximum interaction distance (0 = unlimited).")] [SerializeField]
            private float interactionDistance = 3f;

            [Tooltip("Reference point for distance check (player/camera). Uses main camera if not set.")]
            [SerializeField]
            private Transform distanceCheckPoint;

            [Tooltip("Reference point for look direction checks. Uses main camera if not set.")] [SerializeField]
            private Transform viewCheckPoint;

            [Tooltip(
                "Check for obstacles (walls) between object and check point. Uses raycast to detect blocking colliders.")]
            [SerializeField]
            private bool checkObstacles = true;

            [Tooltip("Layers that block interaction (used when checkObstacles is enabled).")] [SerializeField]
            private LayerMask obstacleLayers = -1;

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

            private float clickTime;
            private bool keyHeldPrev;
            private bool mouseHeldPrev;
            private bool wasHoveredByRaycast;
            private bool wasInRange;
            private readonly RaycastHit[] lookHits3D = new RaycastHit[8];
            private readonly RaycastHit2D[] lookHits2D = new RaycastHit2D[8];
            private Vector3 lastDebugRayStart;
            private Vector3 lastDebugRayEnd;
            private Color lastDebugRayColor = Color.cyan;
            private float lastDebugRayUntilTime;

            private static readonly Type KeyboardType =
                Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem");

            private static readonly Type MouseType =
                Type.GetType("UnityEngine.InputSystem.Mouse, Unity.InputSystem");

            private void Awake()
            {
                CheckEventSystemOnce();
                TryEnsureNeededRaycasterOnce();

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
                    UpdateMouseHoverRaycast();
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

                if (drawInteractionRayForOneSecond && Time.realtimeSinceStartup <= lastDebugRayUntilTime)
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
                    bool inRange = interactionDistance > 0f ? IsInRange() : true;

                    if (interactionDistance > 0f && !inRange)
                    {
                        return;
                    }

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
                if (!TryGetMouseButton(mouseIndex, out bool mouseHeld))
                {
                    mouseHeldPrev = false;
                    return;
                }

                if (mouseHeld && !mouseHeldPrev)
                {
                    if (IsHovered)
                    {
                        bool inRange = IsInRange();
                        if (inRange || interactionDistance <= 0f)
                        {
                            onInteractDown?.Invoke();
                        }
                    }
                }
                else if (!mouseHeld && mouseHeldPrev)
                {
                    bool inRange = IsInRange();
                    if (inRange || interactionDistance <= 0f)
                    {
                        onInteractUp?.Invoke();
                    }
                }

                mouseHeldPrev = mouseHeld;
            }

            private void UpdateMouseHoverRaycast()
            {
                Camera cam = Camera.main ?? FindFirstObjectByType<Camera>();
                if (cam == null)
                {
                    return;
                }

                Collider collider = GetComponent<Collider>();
                if (collider == null || !collider.enabled)
                {
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

                bool inRange = interactionDistance > 0f ? IsInRange() : true;

                if (!TryGetMousePosition(out Vector3 mousePos))
                {
                    return;
                }

                Ray ray = cam.ScreenPointToRay(mousePos);
                RaycastHit hit;
                QueryTriggerInteraction triggerInteraction = includeTriggerCollidersInMouseRaycast ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;
                bool raycastHit = Physics.Raycast(ray, out hit, float.MaxValue, ~0, triggerInteraction) && hit.collider == collider;
                bool isHoveredNow = raycastHit && inRange;

                if (isHoveredNow && !wasHoveredByRaycast)
                {
                    wasHoveredByRaycast = true;
                    if (!IsHovered)
                    {
                        OnHoverEnterRaycast();
                    }
                }
                else if ((!isHoveredNow || !inRange) && wasHoveredByRaycast)
                {
                    wasHoveredByRaycast = false;
                    if (IsHovered)
                    {
                        OnHoverExitRaycast();
                    }
                }
                else if (IsHovered && !inRange && interactionDistance > 0f)
                {
                    OnHoverExitRaycast();
                }
            }

            private void OnHoverEnterRaycast()
            {
                if (!interactable || !useMouseInteraction)
                {
                    return;
                }

                bool inRange = interactionDistance > 0f ? IsInRange() : true;
                if (interactionDistance > 0f && !inRange)
                {
                    return;
                }

                IsHovered = true;
                onHoverEnter?.Invoke();
            }

            private void OnHoverExitRaycast()
            {
                if (!interactable || !useMouseInteraction)
                {
                    return;
                }

                IsHovered = false;
                onHoverExit?.Invoke();
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
                if (interactionDistance <= 0f && !checkObstacles)
                {
                    return true;
                }

                if (distanceCheckPoint == null)
                {
                    return true;
                }

                Vector3 checkPointPos = distanceCheckPoint.position;
                Vector3 targetPos = GetInteractionTargetPosition();
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

                    bool has3DCollider = TryGetComponent(out Collider selfCollider3D);
                    bool has2DCollider = TryGetComponent(out Collider2D selfCollider2D);

                    if (has3DCollider)
                    {
                        int hitCount = Physics.RaycastNonAlloc(checkPointPos, directionNormalized, lookHits3D,
                            checkDistance,
                            obstacleLayers, QueryTriggerInteraction.Ignore);
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
                        int hitCount2D = Physics2D.RaycastNonAlloc(origin2D, direction2D, lookHits2D, checkDistance,
                            obstacleLayers);
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
                            obstacleLayers, QueryTriggerInteraction.Ignore);
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

                if (keyboardInteractionMode == KeyboardInteractionMode.ViewOrMouse &&
                    (IsHovered || wasHoveredByRaycast))
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

                Vector3 forward = lookSource.forward.normalized;
                float dot = Vector3.Dot(forward, toTarget / distance);
                if (dot < minLookDot)
                {
                    CacheDebugRay(origin, target, Color.yellow);
                    return false;
                }

                if (!requireDirectLookRay)
                {
                    CacheDebugRay(origin, target, Color.cyan);
                    return true;
                }

                if (TryGetComponent(out Collider _))
                {
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

                if (TryGetComponent(out Collider2D _))
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
                if (TryGetComponent(out Collider col3D))
                {
                    return col3D.bounds.center;
                }

                if (TryGetComponent(out Collider2D col2D))
                {
                    return col2D.bounds.center;
                }

                return transform.position;
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
                return hitCollider != null && hitCollider.transform.IsChildOf(transform);
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
                lastDebugRayUntilTime = Time.realtimeSinceStartup + Mathf.Max(0.05f, interactionRayDrawDuration);
            }


            private bool IsKeyboardActionDown()
            {
                try
                {
                    if (Input.GetKeyDown(keyboardKey)) return true;
                }
                catch (InvalidOperationException) { }

                return ReadNewInputKeyState(keyboardKey, "wasPressedThisFrame");
            }

            private bool IsKeyboardActionUp()
            {
                try
                {
                    if (Input.GetKeyUp(keyboardKey)) return true;
                }
                catch (InvalidOperationException) { }

                return ReadNewInputKeyState(keyboardKey, "wasReleasedThisFrame");
            }

            private static bool ReadNewInputKeyState(KeyCode keyCode, string statePropertyName)
            {
                if (KeyboardType == null)
                {
                    return false;
                }

                string keyProperty = GetInputSystemKeyPropertyName(keyCode);
                if (string.IsNullOrEmpty(keyProperty))
                {
                    return false;
                }

                PropertyInfo currentProperty =
                    KeyboardType.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
                object keyboard = currentProperty?.GetValue(null);
                if (keyboard == null)
                {
                    return false;
                }

                PropertyInfo keyControlProperty =
                    KeyboardType.GetProperty(keyProperty, BindingFlags.Public | BindingFlags.Instance);
                object keyControl = keyControlProperty?.GetValue(keyboard);
                if (keyControl == null)
                {
                    return false;
                }

                PropertyInfo stateProperty = keyControl.GetType()
                    .GetProperty(statePropertyName, BindingFlags.Public | BindingFlags.Instance);
                return stateProperty != null && stateProperty.PropertyType == typeof(bool) &&
                       (bool)stateProperty.GetValue(keyControl);
            }

            private static string GetInputSystemKeyPropertyName(KeyCode keyCode)
            {
                if (keyCode >= KeyCode.A && keyCode <= KeyCode.Z)
                {
                    return char.ToLowerInvariant((char)('a' + (keyCode - KeyCode.A))) + "Key";
                }

                if (keyCode >= KeyCode.Alpha0 && keyCode <= KeyCode.Alpha9)
                {
                    int digit = keyCode - KeyCode.Alpha0;
                    return "digit" + digit + "Key";
                }

                switch (keyCode)
                {
                    case KeyCode.Space:
                        return "spaceKey";
                    case KeyCode.Return:
                    case KeyCode.KeypadEnter:
                        return "enterKey";
                    case KeyCode.Escape:
                        return "escapeKey";
                    case KeyCode.Tab:
                        return "tabKey";
                    case KeyCode.Backspace:
                        return "backspaceKey";
                    case KeyCode.LeftShift:
                        return "leftShiftKey";
                    case KeyCode.RightShift:
                        return "rightShiftKey";
                    case KeyCode.LeftControl:
                        return "leftCtrlKey";
                    case KeyCode.RightControl:
                        return "rightCtrlKey";
                    case KeyCode.LeftAlt:
                        return "leftAltKey";
                    case KeyCode.RightAlt:
                        return "rightAltKey";
                    case KeyCode.UpArrow:
                        return "upArrowKey";
                    case KeyCode.DownArrow:
                        return "downArrowKey";
                    case KeyCode.LeftArrow:
                        return "leftArrowKey";
                    case KeyCode.RightArrow:
                        return "rightArrowKey";
                    default:
                        return null;
                }
            }

            private static bool TryGetMousePosition(out Vector3 position)
            {
                try
                {
                    position = Input.mousePosition;
                    return true;
                }
                catch (InvalidOperationException)
                {
                    if (TryGetMousePositionFromNewInputSystem(out position))
                    {
                        return true;
                    }

                    position = Vector3.zero;
                    return false;
                }
            }

            private static bool TryGetMousePositionFromNewInputSystem(out Vector3 position)
            {
                position = Vector3.zero;
                if (MouseType == null) return false;

                PropertyInfo currentProperty = MouseType.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
                object mouse = currentProperty?.GetValue(null);
                if (mouse == null) return false;

                PropertyInfo positionProperty = MouseType.GetProperty("position", BindingFlags.Public | BindingFlags.Instance);
                object positionControl = positionProperty?.GetValue(mouse);
                if (positionControl == null) return false;

                MethodInfo readValueMethod = positionControl.GetType().GetMethod("ReadValue", Type.EmptyTypes);
                if (readValueMethod == null) return false;

                object value = readValueMethod.Invoke(positionControl, null);
                if (value is Vector2 v2)
                {
                    position = new Vector3(v2.x, v2.y, 0f);
                    return true;
                }

                return false;
            }

            private static bool TryGetMouseButton(int buttonIndex, out bool isPressed)
            {
                try
                {
                    isPressed = Input.GetMouseButton(buttonIndex);
                    return true;
                }
                catch (InvalidOperationException)
                {
                    if (TryGetMouseButtonFromNewInputSystem(buttonIndex, out isPressed))
                    {
                        return true;
                    }

                    isPressed = false;
                    return false;
                }
            }

            private static bool TryGetMouseButtonFromNewInputSystem(int buttonIndex, out bool isPressed)
            {
                isPressed = false;
                if (MouseType == null) return false;

                PropertyInfo currentProperty = MouseType.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
                object mouse = currentProperty?.GetValue(null);
                if (mouse == null) return false;

                string buttonName = buttonIndex switch
                {
                    0 => "leftButton",
                    1 => "rightButton",
                    2 => "middleButton",
                    _ => null
                };
                if (buttonName == null) return false;

                PropertyInfo buttonProperty = MouseType.GetProperty(buttonName, BindingFlags.Public | BindingFlags.Instance);
                object buttonControl = buttonProperty?.GetValue(mouse);
                if (buttonControl == null) return false;

                PropertyInfo isPressedProperty = buttonControl.GetType().GetProperty("isPressed", BindingFlags.Public | BindingFlags.Instance);
                if (isPressedProperty?.PropertyType != typeof(bool)) return false;

                isPressed = (bool)isPressedProperty.GetValue(buttonControl);
                return true;
            }

            private void CheckEventSystemOnce()
            {
                if (!_autoCheckEventSystem || eventSystemChecked)
                {
                    return;
                }

                bool eventSystemExists = EventSystem.current != null || FindObjectOfType<EventSystem>() != null;
                EventSystem eventSystem = EventSystem.current ?? FindObjectOfType<EventSystem>();
                bool hasInputModule = eventSystem != null && eventSystem.currentInputModule != null;

                if (!eventSystemExists)
                {
                    Debug.LogWarning("InteractiveObject: EventSystem not found in scene");
                }
                else if (!hasInputModule)
                {
                    Debug.LogWarning(
                        "InteractiveObject: EventSystem found but no InputModule. Adding StandaloneInputModule.", this);
                    if (eventSystem != null)
                    {
                        eventSystem.gameObject.AddComponent<StandaloneInputModule>();
                    }
                }

                eventSystemChecked = true;
            }

            private void TryEnsureNeededRaycasterOnce()
            {
                Camera cam = Camera.main ?? FindFirstObjectByType<Camera>();
                bool hasCollider3DCheck = TryGetComponent<Collider>(out _);
                bool hasCollider2DCheck = TryGetComponent<Collider2D>(out _);

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

                if (hasCollider2DCheck && !physicsRaycasterEnsured2D)
                {
                    if (cam.GetComponent<Physics2DRaycaster>() == null)
                    {
                        cam.gameObject.AddComponent<Physics2DRaycaster>();
                    }

                    physicsRaycasterEnsured2D = true;
                }

                if (hasCollider3DCheck && !physicsRaycasterEnsured3D)
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
