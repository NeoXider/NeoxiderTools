using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Flexalon
{
    /// <summary> Allows a gameObject to be clicked and dragged. </summary>
    [AddComponentMenu("Flexalon/Flexalon Interactable"), HelpURL("https://www.flexalon.com/docs/interactable"), DisallowMultipleComponent]
    public class FlexalonInteractable : MonoBehaviour
    {
        [SerializeField]
        private bool _clickable = false;
        /// <summary> Determines if this object can be clicked and generate click events. </summary>
        public bool Clickable {
            get => _clickable;
            set => _clickable = value;
        }

        [SerializeField]
        private float _maxClickTime = 0.1f;
        /// <summary>
        /// With a mouse or touch input, a click is defined as a press and release.
        /// The time between press and release must be less than Max Click Time to
        /// count as a click. A drag interaction cannot start until Max Click Time is exceeded.
        /// </summary>
        public float MaxClickTime {
            get => _maxClickTime;
            set => _maxClickTime = value;
        }

        [SerializeField]
        private float _maxClickDistance = 0.1f;
        /// <summary>
        /// With a mouse or touch input, a click is defined as a press and release.
        /// The distance between press and release must be less than Max Click Distance to
        /// count as a click. Otherwise, the interaction is considered a drag.
        /// </summary>
        public float MaxClickDistance {
            get => _maxClickDistance;
            set => _maxClickDistance = value;
        }

        [SerializeField]
        private bool _draggable = false;
        /// <summary> Determines if this object can be dragged and generate drag events. </summary>
        public bool Draggable {
            get => _draggable;
            set => _draggable = value;
        }

        [SerializeField]
        private float _interpolationSpeed = 10;
        /// <summary> How quickly the object moves towards the cursor when dragged. </summary>
        public float InterpolationSpeed {
            get => _interpolationSpeed;
            set => _interpolationSpeed = value;
        }

        [SerializeField]
        private float _insertRadius = 0.5f;
        /// <summary> How close this object needs to a drag target's bounds to be inserted. </summary>
        public float InsertRadius {
            get => _insertRadius;
            set => _insertRadius = value;
        }

        /// <summary> Restricts the movement of an object during a drag. </summary>
        public enum RestrictionType
        {
            /// <summary> No restriction ensures the object can move freely. </summary>
            None,

            /// <summary> Plane restriction ensures the object moves along a plane, defined
            /// by the objects initial position and the Plane Normal property. </summary>
            Plane,

            /// <summary> Line restriction ensures the object moves along a line, defined
            /// by the object's initial position and the Line Direction property. </summary>
            Line
        }

        [SerializeField]
        private RestrictionType _restriction = RestrictionType.None;
        /// <summary> Determines how to restrict the object's drag movement. </summary>
        public RestrictionType Restriction {
            get => _restriction;
            set => _restriction = value;
        }

        [SerializeField]
        private Vector3 _planeNormal = Vector3.up;
        /// <summary> Defines the normal of the plane when using a plane restriction.
        /// If 'Local Space' is checked, this normal is rotated by the transform
        /// of the layout that the object started in. </summary>
        public Vector3 PlaneNormal {
            get => _planeNormal;
            set
            {
                _restriction = RestrictionType.Plane;
                _planeNormal = value;
            }
        }

        [SerializeField]
        private Vector3 _lineDirection = Vector3.right;
        /// <summary> Defines the direction of the line when using a line restriction.
        /// If 'Local Space'is checked, this direction is rotated by the transform
        /// of the layout that the object started in. </summary>
        public Vector3 LineDirection {
            get => _lineDirection;
            set
            {
                _restriction = RestrictionType.Line;
                _lineDirection = value;
            }
        }

        [SerializeField]
        private bool _localSpaceRestriction = true;
        /// <summary> When checked, the Plane Normal and Line Direction are applied in local space. </summary>
        public bool LocalSpaceRestriction {
            get => _localSpaceRestriction;
            set => _localSpaceRestriction = value;
        }

        [SerializeField]
        private Vector3 _holdOffset;
        // <summary> When dragged, this option adds an offset to the dragged object's position.
        // This can be used to float the object near the layout while it is being dragged.
        // If 'Local Space' is checked, this offset is rotated and scaled by the transform
        // of the layout that the object started in. </summary>
        public Vector3 HoldOffset {
            get => _holdOffset;
            set => _holdOffset = value;
        }

        [SerializeField]
        private bool _localSpaceOffset = true;
        /// <summary> When checked, the Hold Offset is applied in local space. </summary>
        public bool LocalSpaceOffset {
            get => _localSpaceOffset;
            set => _localSpaceOffset = value;
        }

        [SerializeField]
        private bool _rotateOnDrag = false;
        // <summary> When dragged, this option adds a rotation to the dragged object.
        // This can be used to tilt the object while it is being dragged.
        // If 'Local Space' is checked, this rotation will be in the local
        // space of the layout that the object started in. </summary>
        public bool RotateOnDrag {
            get => _rotateOnDrag;
            set => _rotateOnDrag = value;
        }

        [SerializeField]
        private Quaternion _holdRotation;
        /// <summary> The rotation to apply to the object when it is being dragged. </summary>
        public Quaternion HoldRotation {
            get => _holdRotation;
            set
            {
                _rotateOnDrag = true;
                _holdRotation = value;
            }
        }

        [SerializeField]
        private bool _localSpaceRotation = true;
        /// <summary> When checked, the Hold Rotation is applied in local space. </summary>
        public bool LocalSpaceRotation {
            get => _localSpaceRotation;
            set => _localSpaceRotation = value;
        }

        [SerializeField]
        private bool _hideCursor = false;
        /// <summary> When checked, Cursor.visible is set to false when the object is dragged. </summary>
        public bool HideCursor {
            get => _hideCursor;
            set => _hideCursor = value;
        }

        [SerializeField]
        private bool _setParentWhileDragging = false;
        /// <summary> Normally, the interactable is unparented while being dragged. Setting this will set the parent to the currently hovered drag target. </summary>
        public bool SetParentWhileDragging {
            get => _setParentWhileDragging;
            set => _setParentWhileDragging = value;
        }

        [SerializeField]
        private GameObject _handle = null;
        /// <summary> GameObject to use to select and drag this object. If not set, uses self. </summary>
        public GameObject Handle {
            get => _handle;
            set
            {
                _raycaster.Unregister(this);
                _handle = value;
                _raycaster.Register(this);
            }
        }

#if UNITY_PHYSICS
        [SerializeField, Obsolete("Use Handle instead.")]
        private Collider _collider;

        void OnValidate()
        {
            #pragma warning disable 618
            if (_collider && !_handle)
            {
                _handle = _collider.gameObject;
                _collider = null;
            }
            #pragma warning restore 618
        }

        [SerializeField]
        private Collider _bounds;
        /// <summary> If set, the object cannot be dragged outside of the bounds collider. </summary>
        public Collider Bounds {
            get => _bounds;
            set => _bounds = value;
        }
#endif

        [SerializeField]
        private LayerMask _layerMask = -1;
        /// <summary> When dragged, limits which Flexalon Drag Targets will accept this object
        /// by comparing the Layer Mask to the target GameObject's layer. </summary>
        public LayerMask LayerMask {
            get => _layerMask;
            set => _layerMask = value;
        }

        /// <summary> An event that occurs to a FlexalonInteractable. </summary>
        [System.Serializable]
        public class InteractableEvent : UnityEvent<FlexalonInteractable>{}

        [SerializeField]
        private InteractableEvent _clicked;
        /// <summary> Unity Event invoked when the object is pressed and released within MaxClickTime. </summary>
        public InteractableEvent Clicked => _clicked;

        [SerializeField]
        private InteractableEvent _hoverStart;
        /// <summary> Unity Event invoked when the object starts being hovered. </summary>
        public InteractableEvent HoverStart => _hoverStart;

        [SerializeField]
        private InteractableEvent _hoverEnd;
        /// <summary> Unity Event invoked when the object stops being hovered. </summary>
        public InteractableEvent HoverEnd => _hoverEnd;

        [SerializeField]
        private InteractableEvent _selectStart;
        /// <summary> Unity Event invoked when the object starts being selected (e.g. press down mouse over object). </summary>
        public InteractableEvent SelectStart => _selectStart;

        [SerializeField]
        private InteractableEvent _selectEnd;
        /// <summary> Unity Event invoked when the object stops being selected (e.g. release mouse). </summary>
        public InteractableEvent SelectEnd => _selectEnd;

        [SerializeField]
        private InteractableEvent _dragStart;
        /// <summary> Unity Event invoked when the object starts being dragged. </summary>
        public InteractableEvent DragStart => _dragStart;

        [SerializeField]
        private InteractableEvent _dragEnd;
        /// <summary> Unity Event invoked when the object stops being dragged. </summary>
        public InteractableEvent DragEnd => _dragEnd;

        [SerializeField]
        private InteractableEvent _dragTragetChanged;
        /// <summary> Event when the drag target or sibling index changes during a drag operation </summary>
        public InteractableEvent DragTargetChanged => _dragTragetChanged;

        private static List<FlexalonInteractable> _hoveredObjects = new List<FlexalonInteractable>();
        /// <summary> The currently hovered objects. </summary>
        public static List<FlexalonInteractable> HoveredObjects => _hoveredObjects;

        /// <summary> The first hovered object. </summary>
        public static FlexalonInteractable HoveredObject => _hoveredObjects.Count > 0 ? _hoveredObjects[0] : null;

        private static List<FlexalonInteractable> _selectedObjects = new List<FlexalonInteractable>();
        /// <summary> The currently selected / dragged objects. </summary>
        public static List<FlexalonInteractable> SelectedObjects => _selectedObjects;

        /// <summary> The first selected / dragged object. </summary>
        public static FlexalonInteractable SelectedObject => _selectedObjects.Count > 0 ? _selectedObjects[0] : null;

        private Vector3 _target;
        private Vector3 _lastTarget;
        private float _distance;
        private GameObject _placeholder;
        private Vector3 _startPosition;
        private int _startSiblingIndex;
        private UnityEngine.Plane _plane = new UnityEngine.Plane();
        private static FlexalonRaycaster _raycaster = new FlexalonRaycaster();
        private Transform _localSpace;
        private Transform _lastValidLocalSpace;
        private float _selectTime;
        private Vector3 _selectPosition;
        private Vector3 _clickOffset;
        private InputProvider _inputProvider;
        private FlexalonNode _node;
        private bool _wasActive;

#if UNITY_UI
        private Canvas _canvas;
        internal Canvas Canvas => _canvas;
#endif

        // For Editor
        internal bool ShowAllDragProperties => GetInputProvider().InputMode == InputMode.Raycast;

        /// <summary> The current state of the interactable. </summary>
        public enum InteractableState
        {
            /// <summary> The object is not being interacted with. </summary>
            Init,

            /// <summary> The object is being hovered over. </summary>
            Hovering,

            /// <summary> The object is being selected (e.g. press down mouse over object). </summary>
            Selecting,

            /// <summary> The object is being dragged. </summary>
            Dragging
        }

        private InteractableState _state = InteractableState.Init;
        /// <summary> The current state of the interactable. </summary>
        public InteractableState State => _state;

        /// <summary> The drag target that will be attached if the dragged object is released. </summary>
        public Transform DragTarget => _placeholder != null ? _placeholder.transform.parent : null;

        /// <summary> The sibling index that this object will be inserted into the drag target. </summary>
        public int DragSiblingIndex => _placeholder != null ? _placeholder.transform.GetSiblingIndex() : 0;

        void Awake()
        {
            if (_clicked == null)
            {
                _clicked = new InteractableEvent();
            }

            if (_hoverStart == null)
            {
                _hoverStart = new InteractableEvent();
            }

            if (_hoverEnd == null)
            {
                _hoverEnd = new InteractableEvent();
            }

            if (_selectStart == null)
            {
                _selectStart = new InteractableEvent();
            }

            if (_selectEnd == null)
            {
                _selectEnd = new InteractableEvent();
            }

            if (_dragStart == null)
            {
                _dragStart = new InteractableEvent();
            }

            if (_dragEnd == null)
            {
                _dragEnd = new InteractableEvent();
            }
        }

        void OnEnable()
        {
            _node = Flexalon.GetOrCreateNode(gameObject);
            _inputProvider = GetInputProvider();

            UpdateCanvas();

            if (!_handle)
            {
                _handle = gameObject;
            }

            _raycaster.Register(this);
        }

        void OnDisable()
        {
            _raycaster.Unregister(this);
            if (_state != InteractableState.Init)
            {
                UpdateState(_inputProvider.InputMode, default, false, false, false);
            }

            _node = null;
        }

        void Update()
        {
            var inputMode = _inputProvider.InputMode;
            Vector3 uiPointer = _inputProvider.UIPointer;
            Ray ray = _inputProvider.Ray;
            bool isHit = false;
            bool isActive = _inputProvider.Active;
            bool becameActive = isActive && !_wasActive;
            _wasActive = isActive;

            if (inputMode == InputMode.Raycast)
            {
                if (_selectedObjects.Count == 0 || _selectedObjects[0] == this)
                {
                    isHit = _raycaster.IsHit(uiPointer, ray, this);
                }
            }
            else
            {
                var focusedObject = _inputProvider.ExternalFocusedObject;
                isHit = focusedObject && focusedObject == gameObject;
            }

#if UNITY_UI
            if (_canvas)
            {
                if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    ray = new Ray(uiPointer, Vector3.forward);
                }
                else if (_canvas.renderMode == RenderMode.ScreenSpaceCamera)
                {
                    if (_canvas.worldCamera)
                    {
                        ray = _canvas.worldCamera.ScreenPointToRay(uiPointer);
                    }
                }
            }
#endif

            UpdateState(inputMode, ray, isHit, isActive, becameActive);
        }

        void FixedUpdate()
        {
            if (_state != InteractableState.Dragging)
            {
                return;
            }

            if (_target == _lastTarget)
            {
                return;
            }

            var currentDragTarget = _placeholder.transform.parent ? _placeholder.transform.parent.GetComponent<FlexalonDragTarget>() : null;
            bool dragTargetChanged;

            // Find a drag target to insert into.
            if (TryFindNearestDragTarget(currentDragTarget, out var newDragTarget, out var nearestChild))
            {
                dragTargetChanged = AddToLayout(currentDragTarget, newDragTarget, nearestChild);
            }
            else
            {
                dragTargetChanged = MovePlaceholder(null);
            }

            _lastTarget = _target;

            if (dragTargetChanged)
            {
                DragTargetChanged.Invoke(this);
            }
        }

        internal void UpdateCanvas()
        {
#if UNITY_UI
            if (_canvas)
            {
                return;
            }

            _canvas = GetComponentInParent<Canvas>();

            if (_canvas)
            {
                if (_restriction == RestrictionType.None)
                {
                    _restriction = RestrictionType.Plane;
                    _planeNormal = Vector3.forward;
                }
            }
#endif
        }

        private InputProvider GetInputProvider()
        {
            if (!TryGetComponent<InputProvider>(out var inputProvider))
            {
                inputProvider = Flexalon.GetInputProvider();
            }

            return inputProvider;
        }

        private void SetState(InteractableState state)
        {
            _state = state;
        }

        private void UpdateState(InputMode inputMode, Ray ray, bool isHit, bool isActive, bool becameActive)
        {
            if (_state == InteractableState.Init)
            {
                if (isHit && (!isActive || becameActive))
                {
                    SetState(InteractableState.Hovering);
                    OnHoverStart();
                }
            }

            if (_state == InteractableState.Hovering)
            {
                if (!isHit)
                {
                    SetState(InteractableState.Init);
                    OnHoverEnd();
                }
                else if (becameActive)
                {
                    SetState(InteractableState.Selecting);
                    OnSelectStart();
                }
            }

            if (_state == InteractableState.Selecting)
            {
                bool clickValid = _clickable && isHit &&
                    (Time.time - _selectTime <= _maxClickTime) &&
                    Vector3.Distance(_selectPosition, _raycaster.hitPosition) < _maxClickDistance;

                if (!isActive)
                {
                    if (clickValid)
                    {
                        Clicked.Invoke(this);
                    }

                    if (isHit)
                    {
                        SetState(InteractableState.Hovering);
                        OnSelectEnd();
                    }
                    else
                    {
                        SetState(InteractableState.Init);
                        OnSelectEnd();
                        OnHoverEnd();
                    }

                }
                else if (_draggable && !clickValid)
                {
                    SetState(InteractableState.Dragging);
                    OnDragStart(inputMode, ray);
                }
            }

            if (_state == InteractableState.Dragging)
            {
                if (!isActive)
                {
                    if (isHit)
                    {
                        SetState(InteractableState.Hovering);
                        OnDragEnd();
                        OnSelectEnd();
                    }
                    else
                    {
                        SetState(InteractableState.Init);
                        OnDragEnd();
                        OnSelectEnd();
                        OnHoverEnd();
                    }
                }
                else
                {
                    OnDragMove(inputMode, ray);
                }
            }
        }

        private void OnHoverStart()
        {
            _hoveredObjects.Add(this);
            HoverStart.Invoke(this);

            // Save this here in case the input provider changes the parent on select.
            _localSpace = transform.parent;
            _startSiblingIndex = transform.GetSiblingIndex();
        }

        private void OnHoverEnd()
        {
            _hoveredObjects.Remove(this);
            HoverEnd.Invoke(this);
            _localSpace = null;
        }

        private void OnSelectStart()
        {
            _selectTime = Time.time;
            _selectPosition = _raycaster.hitPosition;
            _selectedObjects.Add(this);
            SelectStart.Invoke(this);
        }

        private void OnSelectEnd()
        {
            _selectedObjects.Remove(this);
            SelectEnd.Invoke(this);
        }

        private void OnDragStart(InputMode inputMode, Ray ray)
        {
            if (_hideCursor)
            {
                Cursor.visible = false;
            }

            _target = _lastTarget = transform.position;
            _clickOffset = transform.position - _selectPosition;
            _distance = Vector3.Distance(_target, ray.origin + _clickOffset);
            _startPosition = transform.position;

            // Create a placeholder
            _placeholder = new GameObject("Drag Placeholder");
            var placeholderObj = Flexalon.AddComponent<FlexalonObject>(_placeholder);
            _node = Flexalon.GetOrCreateNode(gameObject);
            placeholderObj.Size = _node.Result.LayoutBounds.size;
            placeholderObj.Rotation = _node.Rotation;
            placeholderObj.Scale = _node.Scale;
            placeholderObj.Margin = _node.Margin;
            placeholderObj.Padding = _node.Padding;

            _node.IsDragging = true;

            // If we're in a valid drag target, swap with the placeholder.
            var parentDragTarget = _localSpace ? _localSpace.GetComponent<FlexalonDragTarget>() : null;
            if (parentDragTarget != null && CanAdd(parentDragTarget, parentDragTarget))
            {
                MovePlaceholder(_localSpace, _startSiblingIndex);

                // Input provider may be changing the parent before we get here.
                if (!_setParentWhileDragging && transform.parent == _localSpace)
                {
#if UNITY_UI
                    transform.SetParent(_canvas?.transform, true);
#else
                    transform.SetParent(null, true);
#endif
                }
            }
            else
            {
                _placeholder.transform.SetParent(null);
                _placeholder.SetActive(false);
            }

            DragStart.Invoke(this);
            DragTargetChanged.Invoke(this);
        }

        private void OnDragMove(InputMode inputMode, Ray ray)
        {
            if (inputMode == InputMode.External)
            {
                _target = transform.position;
            }
            else
            {
                UpdateTarget(ray);
                UpdateObjectPosition();
            }
        }

        private void OnDragEnd()
        {
            _node.IsDragging = false;

            // Swap places with the placeholder and destroy it.
            if (_placeholder.activeSelf)
            {
                transform.SetParent(_placeholder.transform.parent, true);
                transform.SetSiblingIndex(_placeholder.transform.GetSiblingIndex());
            }

            _lastValidLocalSpace = null;
            _placeholder.transform.SetParent(null);
            Destroy(_placeholder);

            if (_hideCursor)
            {
                Cursor.visible = true;
            }

            DragEnd.Invoke(this);
        }

        private static bool ClosestPointOnTwoLines(Vector3 p0, Vector3 v0, Vector3 p1, Vector3 v1, out Vector3 closestPointLine2)
        {
            closestPointLine2 = Vector3.zero;

            float a = Vector3.Dot(v0, v0);
            float b = Vector3.Dot(v0, v1);
            float e = Vector3.Dot(v1, v1);

            float d = a * e - b * b;

            // Lines are not parallel
            if (d != 0.0f)
            {
                Vector3 r = p0 - p1;
                float c = Vector3.Dot(v0, r);
                float f = Vector3.Dot(v1, r);
                float t = (a * f - c * b) / d;
                closestPointLine2 = p1 + v1 * t;
                return true;
            }

            return false;
        }

        // Sets _target to where we want to move the dragged object -- based on the input ray, restrictions, and bounds.
        private void UpdateTarget(Ray ray)
        {
            ray.origin += _clickOffset;

            if (_restriction == RestrictionType.Line)
            {
                var lineDir = _lineDirection;
                if (_localSpaceRestriction && _lastValidLocalSpace)
                {
                    lineDir = _lastValidLocalSpace.rotation * _lineDirection;
                }

                if (!ClosestPointOnTwoLines(ray.origin, ray.direction, _startPosition, lineDir.normalized, out _target))
                {
                    _target = _startPosition;
                }
            }
            else if (_restriction == RestrictionType.Plane)
            {
                var normal = _planeNormal;
                if (_localSpaceRestriction && _lastValidLocalSpace)
                {
                    normal = _lastValidLocalSpace.rotation * _planeNormal;
                }

                _plane.SetNormalAndPosition(normal.normalized, _startPosition);
                _plane.Raycast(ray, out var distance);
                _target = ray.origin + ray.direction * distance;
            }
            else
            {
                // If there's no restriction, just project forward at the same distance as the placeholder.
                if (_placeholder.gameObject.activeSelf && Flexalon.GetOrCreateNode(_placeholder).HasResult)
                {
                    _distance = Vector3.Distance(ray.origin, _placeholder.transform.position);
                }

                _target = ray.origin + ray.direction * _distance;
            }

#if UNITY_PHYSICS
            // Apply bounds restriction
            if (_bounds)
            {
                _target = _bounds.ClosestPoint(_target);
            }
#endif
        }

        private void UpdateObjectPosition()
        {
            // Apply hold offset
            var offset = Vector3.zero;
            if (_localSpaceOffset && _localSpace)
            {
                offset = _localSpace.localToWorldMatrix.MultiplyVector(_holdOffset);
            }
            else if (!_localSpaceOffset)
            {
                offset = _holdOffset;
            }

            // Interpolate object towards target.
            transform.position = Vector3.Lerp(transform.position, _target + offset, Time.deltaTime * _interpolationSpeed);

            // Apply hold rotation
            if (_rotateOnDrag)
            {
                var rotation = Quaternion.identity;
                if (_localSpaceRotation && _localSpace)
                {
                    rotation = _localSpace.rotation * _holdRotation;
                }
                else if (!_localSpaceRotation)
                {
                    rotation = _holdRotation;
                }

                transform.rotation = Quaternion.Lerp(transform.rotation, rotation, Time.deltaTime * _interpolationSpeed);
            }
        }

        private bool TryFindNearestChild(FlexalonDragTarget dragTarget, out Transform nearestChild, out float distanceSquared)
        {
            var moveDirection = (_target - _lastTarget).normalized;
            nearestChild = null;
            distanceSquared = float.MaxValue;
            foreach (Transform child in dragTarget.transform)
            {
                if (child == transform) { continue; }

                var childPos = dragTarget.transform.localToWorldMatrix.MultiplyPoint(child.GetComponent<FlexalonResult>().TargetPosition);
                var toChild = (childPos - _lastTarget).normalized;
                if (child == _placeholder.transform || Vector3.Dot(toChild, moveDirection) > 0)
                {
                    var distSq = Vector3.SqrMagnitude(childPos - _target);
                    if (distSq < distanceSquared)
                    {
                        distanceSquared = distSq;
                        nearestChild = child;
                    }
                }
            }

            return nearestChild != null;
        }

        // Find a drag target to insert into by checking if it contains the target point.
        private bool TryFindNearestDragTarget(FlexalonDragTarget currentDragTarget, out FlexalonDragTarget dragTarget, out Transform nearestChild)
        {
            if (!CanLeave(currentDragTarget))
            {
                dragTarget = currentDragTarget;
                TryFindNearestChild(currentDragTarget, out nearestChild, out var distanceSquared);
                return true;
            }

            dragTarget = null;
            nearestChild = null;
            var minDistance = float.MaxValue;
            GetInsertPositionAndRadius(_node, _target, out var worldInsertPosition, out var worldInsertRadius);

            foreach (var candidate in FlexalonDragTarget.DragTargets)
            {
                if (CanAdd(currentDragTarget, candidate) && candidate.OverlapsSphere(worldInsertPosition, worldInsertRadius))
                {
                    if (TryFindNearestChild(candidate, out var candidateNearestChild, out var distanceSquared))
                    {
                        if (distanceSquared < minDistance)
                        {
                            minDistance = distanceSquared;
                            dragTarget = candidate;
                            nearestChild = candidateNearestChild;
                        }
                    }
                    else if (dragTarget == null)
                    {
                        dragTarget = candidate;
                        break;
                    }
                }

            }

            return dragTarget != null;
        }

        // Moves the placeholder into the drag target at a particular index.
        private bool MovePlaceholder(Transform newParent, int siblingIndex = 0)
        {
#if UNITY_UI
            if (newParent == null)
            {
                newParent = _canvas?.transform;
            }
#endif

            if (newParent != _placeholder.transform.parent ||
                (newParent != null && siblingIndex != _placeholder.transform.GetSiblingIndex()))
            {
                _placeholder.SetActive(!!newParent);
                _placeholder.transform.SetParent(newParent);
                if (newParent)
                {
                    _placeholder.transform.SetSiblingIndex(siblingIndex);
                    _lastValidLocalSpace = newParent;
                }

                _localSpace = newParent;

                if (_setParentWhileDragging)
                {
                    if (newParent != transform.parent)
                    {
                        transform.SetParent(newParent, true);
                    }

                    if (newParent)
                    {
                        if (transform.GetSiblingIndex() != siblingIndex + 1)
                        {
                            transform.SetSiblingIndex(siblingIndex + 1);
                        }
                    }
                }

                return true;
            }

            return false;
        }

        // Finds an appropriate place to add the placeholder into the drag target.
        private bool AddToLayout(FlexalonDragTarget currentDragTarget, FlexalonDragTarget newDragTarget, Transform nearestChild)
        {
            var insertIndex = nearestChild ? nearestChild.GetSiblingIndex() : 0;

            // Special case -- if adding a new item at the end, the user usually wants to place
            // it after the last element.
            if (currentDragTarget != newDragTarget && insertIndex == newDragTarget.transform.childCount - 1)
            {
                insertIndex++;
            }

            return MovePlaceholder(newDragTarget.transform, insertIndex);
        }

        private bool CanLeave(FlexalonDragTarget dragTarget)
        {
            return dragTarget == null ||
                (dragTarget.CanRemoveObjects && dragTarget.transform.childCount > dragTarget.MinObjects);
        }

        private bool CanAdd(FlexalonDragTarget currentDragTarget, FlexalonDragTarget dragTarget)
        {
            if (currentDragTarget == dragTarget)
            {
                return true;
            }

            return dragTarget != null &&
                dragTarget.gameObject != gameObject &&
                dragTarget.CanAddObjects &&
                (dragTarget.MaxObjects == 0 || dragTarget.transform.childCount < dragTarget.MaxObjects) &&
                (_layerMask.value & (1 << dragTarget.gameObject.layer)) != 0;
        }

        private void GetInsertPositionAndRadius(FlexalonNode node, Vector3 target, out Vector3 position, out float radius)
        {
            var worldBoxScale = node.GetWorldBoxScale(true);
            var scale = transform.lossyScale;
            radius = _insertRadius * Mathf.Max(scale.x, scale.y, scale.z);
            position = target;
        }

        private void OnDrawGizmosSelected()
        {
            var node = Flexalon.GetNode(gameObject);
            if (node != null && _draggable)
            {
                Gizmos.color = Color.green;
                var target = _state == InteractableState.Dragging ? _target : transform.position;
                GetInsertPositionAndRadius(node, target, out var insertPosition, out var insertRadius);
                Gizmos.DrawWireSphere(insertPosition, insertRadius);
            }
        }
    }
}