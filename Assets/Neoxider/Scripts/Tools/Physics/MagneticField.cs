using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Magnetic field that attracts or repels objects within a radius.
    ///     Supports continuous force, layer filtering, and optional Rigidbody setup.
    /// </summary>
    [NeoDoc("Tools/Physics/MagneticField.md")]
    [CreateFromMenu("Neoxider/Tools/Physics/MagneticField")]
    [AddComponentMenu("Neoxider/" + "Tools/" + "Physics/" + nameof(MagneticField))]
    public class MagneticField : MonoBehaviour
    {
        /// <summary>
        ///     How force falls off with distance.
        /// </summary>
        public enum FalloffType
        {
            /// <summary>Linear falloff</summary>
            Linear,

            /// <summary>Quadratic falloff</summary>
            Quadratic,

            /// <summary>No falloff</summary>
            Constant
        }

        /// <summary>
        ///     Magnetic field behavior mode.
        /// </summary>
        public enum FieldMode
        {
            /// <summary>Attract objects toward this field</summary>
            Attract,

            /// <summary>Repel objects</summary>
            Repel,

            /// <summary>Attract toward a target Transform</summary>
            ToTarget,

            /// <summary>Attract toward a point in space</summary>
            ToPoint,

            /// <summary>Pull along a direction vector</summary>
            Direction
        }

        /// <summary>Physics world the field queries.</summary>
        public enum PhysicsSpace
        {
            /// <summary>3D colliders; Force moves Rigidbody.</summary>
            ThreeD,

            /// <summary>2D colliders (triggers included); Force moves Rigidbody2D, Kinematic keeps Z.</summary>
            TwoD
        }

        /// <summary>How objects in the field are moved.</summary>
        public enum MoveMethod
        {
            /// <summary>AddForce on the object's body (needs a Rigidbody / Rigidbody2D).</summary>
            Force,

            /// <summary>
            ///     Moves the object's transform at Strength units per second (scaled by falloff); no body needed.
            ///     For pickups, XP orbs, coins and other objects that should not simulate physics.
            /// </summary>
            Kinematic
        }

        [Header("Settings")] [Tooltip("Field mode")] [SerializeField]
        private FieldMode mode = FieldMode.Attract;

        [Tooltip("Magnetic field strength")] [Min(0f)] [SerializeField]
        private float fieldStrength = 10f;

        [Tooltip("Field radius")] [Min(0f)] [SerializeField]
        private float radius = 5f;

        [Tooltip("Force falloff type over distance")] [SerializeField]
        private FalloffType falloffType = FalloffType.Quadratic;

        [Header("Filtering")] [Tooltip("Layers of objects affected by the field")] [SerializeField]
        private LayerMask affectedLayers = -1;

        [Header("Space & Motion")]
        [Tooltip("ThreeD queries 3D colliders, TwoD queries Collider2D (triggers included) for 2D games.")]
        [SerializeField]
        private PhysicsSpace _space = PhysicsSpace.ThreeD;

        [Tooltip("Force: AddForce on the object's body. Kinematic: moves the transform at Strength units per second " +
                 "(scaled by falloff), no Rigidbody needed: pickups, XP orbs, coins.")]
        [SerializeField]
        private MoveMethod _moveMethod = MoveMethod.Force;

        [Tooltip("An attracted object closer than this to the attraction point raises On Object Reached, for example " +
                 "to collect a pickup. Checked in both move methods. 0 = off.")]
        [Min(0f)] [SerializeField]
        private float _reachDistance;

        [Header("Toggle")]
        [Tooltip(
            "Toggle force direction over time (works with any mode). Alternates forward and reverse phases.")]
        [SerializeField]
        private bool _toggle;

        [Tooltip("Duration in forward direction (seconds)")] [Min(0.1f)] [SerializeField]
        private float attractDuration = 2f;

        [Tooltip("Duration in reverse direction (seconds)")] [Min(0.1f)] [SerializeField]
        private float repelDuration = 2f;

        [Tooltip("Start with forward direction (true) or reverse (false)")] [SerializeField]
        private bool startWithAttract = true;

        [Header("Attraction Target")] [Tooltip("Target transform (used in ToTarget mode)")] [SerializeField]
        private Transform targetTransform;

        [Tooltip("Point in space (used in ToPoint mode)")] [SerializeField]
        private Vector3 targetPoint = Vector3.zero;

        [Header("Attraction Direction")]
        [Tooltip(
            "Direction vector (Direction mode). If Local Direction is on, it is in this object's local space.")]
        [SerializeField]
        private Vector3 direction = Vector3.forward;

        [Tooltip("If true — direction is in local space (TransformDirection).")] [SerializeField]
        private bool directionIsLocal = true;

        [Tooltip("Direction visualization length in scene (and handle for Direction mode).")]
        [Min(0.01f)]
        [SerializeField]
        private float directionGizmoDistance = 10f;

        [Header("Options")] [Tooltip("Automatically add Rigidbody to objects without physics")] [SerializeField]
        private bool addRigidbodyIfNeeded;

        [Tooltip("Use FixedUpdate instead of Update for more stable physics")] [SerializeField]
        private bool useFixedUpdate = true;

        [Tooltip("Update interval for objects in field (0 = every frame)")] [Min(0f)] [SerializeField]
        private float updateInterval;

        [Tooltip("Invoked when object enters field")]
        public UnityEvent<GameObject> OnObjectEntered = new();

        [Tooltip("Invoked when object exits field")]
        public UnityEvent<GameObject> OnObjectExited = new();

        [Tooltip("Invoked when an attracted object is within Reach Distance of the attraction point " +
                 "(collect, despawn or score it here).")]
        public UnityEvent<GameObject> OnObjectReached = new();

        [Tooltip("Invoked when mode changes (for Toggle)")]
        public UnityEvent<bool> OnModeChanged = new();

        [Header("Performance")]
        [Tooltip("Initial size of the reusable overlap buffer. It doubles automatically when a query fills it, " +
                 "so every collider in range is processed; set it near the usual count to avoid regrowth.")]
        [Min(1)] [SerializeField]
        private int _maxOverlapColliders = 64;

        private readonly Dictionary<GameObject, Rigidbody> cachedRigidbodies = new();

        private readonly HashSet<GameObject> objectsInField = new();
        private readonly HashSet<GameObject> currentObjects = new();
        private readonly HashSet<Rigidbody> affectedBodies = new();
        private readonly List<GameObject> toRemove = new();
        private readonly List<GameObject> _reached = new();
        private readonly List<Collider2D> _colliders2D = new();
        private readonly Dictionary<GameObject, Rigidbody2D> cachedBodies2D = new();
        private readonly HashSet<Rigidbody2D> affectedBodies2D = new();
        private Collider[] _colliderBuffer;
        private float _stepSeconds;

        // WHY: like after OnDisable, the first update after creation runs at once instead of waiting one Update Interval.
        private float lastUpdateTime = float.NegativeInfinity;
        private float toggleStartTime;

        /// <summary>
        ///     Current field strength.
        /// </summary>
        public float CurrentStrength => fieldStrength;

        /// <summary>
        ///     Current field radius.
        /// </summary>
        public float CurrentRadius => radius;

        /// <summary>
        ///     Number of objects currently inside the field.
        /// </summary>
        public int ObjectsInFieldCount => objectsInField.Count;

        /// <summary>
        ///     Layers queried by the field. Changes apply to the next field update.
        /// </summary>
        public LayerMask AffectedLayers
        {
            get => affectedLayers;
            set => affectedLayers = value;
        }

        /// <summary>Physics world queried by the field (3D or 2D).</summary>
        public PhysicsSpace Space
        {
            get => _space;
            set => _space = value;
        }

        /// <summary>Force (bodies) or Kinematic (transforms) motion.</summary>
        public MoveMethod Motion
        {
            get => _moveMethod;
            set => _moveMethod = value;
        }

        /// <summary>Distance to the attraction point that raises <see cref="OnObjectReached" />; 0 = off.</summary>
        public float ReachDistance
        {
            get => _reachDistance;
            set => _reachDistance = Mathf.Max(0f, value);
        }

        /// <summary>
        ///     Current toggle phase (true = attract, false = repel) when Toggle is enabled.
        /// </summary>
        public bool CurrentToggleState { get; private set; }

        private void Awake()
        {
            if (_toggle)
            {
                toggleStartTime = Time.time;
                CurrentToggleState = startWithAttract;
            }
        }

        private void OnDisable()
        {
            GameObject[] exitedObjects = new GameObject[objectsInField.Count];
            objectsInField.CopyTo(exitedObjects);
            objectsInField.Clear();
            currentObjects.Clear();
            affectedBodies.Clear();
            cachedRigidbodies.Clear();
            affectedBodies2D.Clear();
            cachedBodies2D.Clear();
            _colliders2D.Clear();
            _reached.Clear();
            toRemove.Clear();
            if (_colliderBuffer != null)
            {
                System.Array.Clear(_colliderBuffer, 0, _colliderBuffer.Length);
            }

            lastUpdateTime = float.NegativeInfinity;
            foreach (GameObject obj in exitedObjects)
            {
                OnObjectExited?.Invoke(obj);
            }
        }

        private void Update()
        {
            if (useFixedUpdate)
            {
                return;
            }

            UpdateToggleState();
            UpdateField();
        }

        private void FixedUpdate()
        {
            if (!useFixedUpdate)
            {
                return;
            }

            UpdateToggleState();
            UpdateField();
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = GetFieldCenter();
            bool isRepel = mode == FieldMode.Repel;
            bool isAttracting = !isRepel;
            // WHY: Toggle reverse phase inverts gizmo colors
            if (_toggle && !CurrentToggleState)
            {
                isAttracting = !isAttracting;
            }

            Gizmos.color = isAttracting ? Color.blue : Color.red;
            Gizmos.DrawWireSphere(center, radius);

            Gizmos.color = new Color(isAttracting ? 0f : 1f, 0f, isAttracting ? 1f : 0f, 0.1f);
            Gizmos.DrawSphere(center, radius);

            if (mode == FieldMode.ToTarget && targetTransform != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, targetTransform.position);
                Gizmos.DrawSphere(targetTransform.position, 0.075f);
            }

            if (mode == FieldMode.ToPoint)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, targetPoint);
                Gizmos.DrawSphere(targetPoint, 0.075f);
            }

            if (mode == FieldMode.Direction)
            {
                Vector3 end = transform.position +
                              GetDirectionWorldNormalized() * Mathf.Max(0.01f, directionGizmoDistance);
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, end);
                Gizmos.DrawSphere(end, 0.075f);
            }
        }

        /// <summary>
        ///     Toggles between Attract and Repel mode.
        /// </summary>
        public void ToggleMode()
        {
            mode = mode == FieldMode.Attract ? FieldMode.Repel : FieldMode.Attract;
        }

        /// <summary>Current field mode.</summary>
        public FieldMode CurrentMode => mode;

        /// <summary>
        ///     Sets field mode.
        /// </summary>
        public void SetMode(FieldMode newMode)
        {
            mode = newMode;
        }

        /// <summary>
        ///     Sets the mode by its <see cref="FieldMode" /> index, for UnityEvents, which cannot pass an enum.
        ///     An undefined index is ignored.
        /// </summary>
        public void SetModeIndex(int modeIndex)
        {
            if (System.Enum.IsDefined(typeof(FieldMode), modeIndex))
            {
                SetMode((FieldMode)modeIndex);
            }
        }

        /// <summary>
        ///     Sets field strength.
        /// </summary>
        public void SetStrength(float newStrength)
        {
            fieldStrength = Mathf.Max(0f, newStrength);
        }

        /// <summary>
        ///     Sets field radius.
        /// </summary>
        public void SetRadius(float newRadius)
        {
            radius = Mathf.Max(0f, newRadius);
        }

        /// <summary>
        ///     Sets attract-phase duration for Toggle mode.
        /// </summary>
        public void SetAttractDuration(float duration)
        {
            attractDuration = Mathf.Max(0.1f, duration);
        }

        /// <summary>
        ///     Sets repel-phase duration for Toggle mode.
        /// </summary>
        public void SetRepelDuration(float duration)
        {
            repelDuration = Mathf.Max(0.1f, duration);
        }

        /// <summary>
        ///     Resets Toggle timer (restart cycle).
        /// </summary>
        public void ResetToggleTimer()
        {
            toggleStartTime = Time.time;
            CurrentToggleState = startWithAttract;
        }

        /// <summary>
        ///     Sets attraction target Transform.
        /// </summary>
        public void SetTarget(Transform target)
        {
            targetTransform = target;
            if (target != null)
            {
                mode = FieldMode.ToTarget;
            }
        }

        /// <summary>
        ///     Sets attraction point in space.
        /// </summary>
        public void SetTargetPoint(Vector3 point)
        {
            targetPoint = point;
            mode = FieldMode.ToPoint;
        }

        /// <summary>
        ///     Sets pull direction vector.
        /// </summary>
        public void SetDirection(Vector3 newDirection, bool local = true)
        {
            direction = newDirection;
            directionIsLocal = local;
            mode = FieldMode.Direction;
        }

        private void UpdateToggleState()
        {
            if (!_toggle)
            {
                return;
            }

            float elapsed = Time.time - toggleStartTime;
            float currentDuration = CurrentToggleState ? attractDuration : repelDuration;

            if (elapsed >= currentDuration)
            {
                CurrentToggleState = !CurrentToggleState;
                toggleStartTime = Time.time;
                OnModeChanged?.Invoke(CurrentToggleState);
            }
        }

        private void UpdateField()
        {
            if (this == null || !isActiveAndEnabled)
            {
                return;
            }

            if (updateInterval > 0f && Time.time - lastUpdateTime < updateInterval)
            {
                return;
            }

            // WHY: kinematic steps cover the real time since the last update, so an Update Interval does not slow
            // the motion; the clamp keeps a hitch from teleporting objects across the field.
            _stepSeconds = float.IsNegativeInfinity(lastUpdateTime)
                ? Time.deltaTime
                : Mathf.Min(Time.time - lastUpdateTime, 0.25f);
            lastUpdateTime = Time.time;

            Vector3 fieldCenter = GetFieldCenter();
            currentObjects.Clear();
            affectedBodies.Clear();
            affectedBodies2D.Clear();
            _reached.Clear();

            bool alive = _space == PhysicsSpace.TwoD ? UpdateObjects2D(fieldCenter) : UpdateObjects3D(fieldCenter);
            if (!alive)
            {
                return;
            }

            for (int i = 0; i < _reached.Count; i++)
            {
                if (this == null || !isActiveAndEnabled)
                {
                    return;
                }

                GameObject reached = _reached[i];
                if (reached != null)
                {
                    OnObjectReached?.Invoke(reached);
                }
            }

            toRemove.Clear();
            foreach (GameObject obj in objectsInField)
            {
                if (!currentObjects.Contains(obj))
                {
                    toRemove.Add(obj);
                }
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                GameObject obj = toRemove[i];
                objectsInField.Remove(obj);
                cachedRigidbodies.Remove(obj);
                cachedBodies2D.Remove(obj);
                OnObjectExited?.Invoke(obj);
            }
        }

        /// <summary>Returns false when a listener disabled or destroyed the field mid-update.</summary>
        private bool UpdateObjects3D(Vector3 fieldCenter)
        {
            Rigidbody sourceBody = GetComponentInParent<Rigidbody>();
            // WHY: a saturated buffer used to drop arbitrary colliders, churning enter/exit events every update.
            int colliderCount = PhysicsOverlap.OverlapSphereComplete(fieldCenter, radius, affectedLayers,
                ref _colliderBuffer, _maxOverlapColliders);

            for (int i = 0; i < colliderCount; i++)
            {
                Collider col = _colliderBuffer[i];
                if (col == null || col.gameObject == gameObject ||
                    (sourceBody != null && col.attachedRigidbody == sourceBody))
                {
                    continue;
                }

                GameObject obj = col.gameObject;
                if (!TrackObject(obj))
                {
                    continue;
                }

                if (this == null || !isActiveAndEnabled)
                {
                    return false;
                }

                if (obj == null || col == null)
                {
                    continue;
                }

                if (_moveMethod == MoveMethod.Kinematic)
                {
                    MoveKinematic(obj.transform, false);
                }
                else
                {
                    ApplyMagneticForce(obj, col);
                }
            }

            return true;
        }

        private bool UpdateObjects2D(Vector3 fieldCenter)
        {
            Rigidbody2D sourceBody = GetComponentInParent<Rigidbody2D>();
            ContactFilter2D filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(affectedLayers);
            Physics2D.OverlapCircle(fieldCenter, radius, filter, _colliders2D);

            for (int i = 0; i < _colliders2D.Count; i++)
            {
                Collider2D col = _colliders2D[i];
                if (col == null || col.gameObject == gameObject ||
                    (sourceBody != null && col.attachedRigidbody == sourceBody))
                {
                    continue;
                }

                GameObject obj = col.gameObject;
                if (!TrackObject(obj))
                {
                    continue;
                }

                if (this == null || !isActiveAndEnabled)
                {
                    return false;
                }

                if (obj == null || col == null)
                {
                    continue;
                }

                if (_moveMethod == MoveMethod.Kinematic)
                {
                    MoveKinematic(obj.transform, true);
                }
                else
                {
                    ApplyMagneticForce2D(obj, col);
                }
            }

            return true;
        }

        /// <summary>
        ///     Adds the object to this update's set and raises On Object Entered for newcomers. False for a duplicate.
        /// </summary>
        private bool TrackObject(GameObject obj)
        {
            if (!currentObjects.Add(obj))
            {
                return false;
            }

            if (!objectsInField.Contains(obj))
            {
                objectsInField.Add(obj);
                OnObjectEntered?.Invoke(obj);
            }

            return true;
        }

        private void MoveKinematic(Transform target, bool keepDepth)
        {
            Vector3 position = target.position;
            float speed = CalculateForceAtDistance(Vector3.Distance(GetFieldCenter(), position), fieldStrength);
            float step = speed * _stepSeconds;
            Vector3 next = position + CalculateForceDirection(position) * step;

            // WHY: a pull toward a point must not overshoot it and jitter around it on the next update.
            if (IsPullingToPoint())
            {
                Vector3 point = GetAttractionPoint();
                Vector3 toPoint = point - position;
                if (keepDepth)
                {
                    toPoint.z = 0f;
                }

                if (toPoint.sqrMagnitude <= step * step)
                {
                    next = point;
                }
            }

            if (keepDepth)
            {
                next.z = position.z;
            }

            target.position = next;
            CheckReached(target.gameObject, next);
        }

        private bool IsPullingToPoint()
        {
            bool forward = !_toggle || CurrentToggleState;
            return mode != FieldMode.Direction && (mode == FieldMode.Repel ? !forward : forward);
        }

        private void CheckReached(GameObject obj, Vector3 position)
        {
            if (_reachDistance <= 0f || !IsPullingToPoint())
            {
                return;
            }

            Vector3 offset = GetAttractionPoint() - position;
            if (_space == PhysicsSpace.TwoD)
            {
                offset.z = 0f;
            }

            if (offset.sqrMagnitude <= _reachDistance * _reachDistance)
            {
                _reached.Add(obj);
            }
        }

        private void ApplyMagneticForce2D(GameObject obj, Collider2D col)
        {
            Rigidbody2D body = col.attachedRigidbody;
            if (body == null && addRigidbodyIfNeeded)
            {
                if (!cachedBodies2D.TryGetValue(obj, out body) || body == null)
                {
                    body = obj.AddComponent<Rigidbody2D>();
                    cachedBodies2D[obj] = body;
                }
            }

            if (body == null || !affectedBodies2D.Add(body))
            {
                return;
            }

            Vector3 objectPosition = body.worldCenterOfMass;
            Vector3 forceDirection = CalculateForceDirection(objectPosition);
            float distance = Vector2.Distance(GetFieldCenter(), objectPosition);
            float force = CalculateForceAtDistance(distance, fieldStrength);
            body.AddForce((Vector2)forceDirection * force, ForceMode2D.Force);
            CheckReached(obj, objectPosition);
        }

        private void ApplyMagneticForce(GameObject obj, Collider col)
        {
            Rigidbody rb = GetOrAddRigidbody(obj, col);
            if (rb == null || !affectedBodies.Add(rb))
            {
                return;
            }

            Vector3 objectPosition = rb.worldCenterOfMass;
            Vector3 forceDirection = CalculateForceDirection(objectPosition);
            Vector3 fieldCenter = GetFieldCenter();
            float distance = Vector3.Distance(fieldCenter, objectPosition);
            float forceAtDistance = CalculateForceAtDistance(distance, fieldStrength);

            rb.AddForce(forceDirection * forceAtDistance, ForceMode.Force);
            CheckReached(obj, objectPosition);
        }

        private Rigidbody GetOrAddRigidbody(GameObject obj, Collider col)
        {
            Rigidbody rb = col.attachedRigidbody;
            if (cachedRigidbodies.TryGetValue(obj, out Rigidbody cached) && cached != null && cached == rb)
            {
                return cached;
            }

            if (rb == null && addRigidbodyIfNeeded)
            {
                rb = obj.AddComponent<Rigidbody>();
            }

            if (rb != null)
            {
                cachedRigidbodies[obj] = rb;
            }
            else
            {
                cachedRigidbodies.Remove(obj);
            }

            return rb;
        }

        private Vector3 CalculateForceDirection(Vector3 objectPosition)
        {
            Vector3 baseDir;
            if (mode == FieldMode.Direction)
            {
                baseDir = GetDirectionWorldNormalized();
            }
            else
            {
                Vector3 attractionPoint = GetAttractionPoint();
                Vector3 toAttractionPoint = attractionPoint - objectPosition;

                if (toAttractionPoint.sqrMagnitude < 0.0001f)
                {
                    toAttractionPoint = Random.onUnitSphere;
                }

                baseDir = toAttractionPoint.normalized;
            }

            bool isRepel = mode == FieldMode.Repel;
            if (isRepel)
            {
                baseDir = -baseDir;
            }

            // WHY: Toggle reverse phase inverts force
            if (_toggle && !CurrentToggleState)
            {
                baseDir = -baseDir;
            }

            return baseDir;
        }

        private Vector3 GetFieldCenter()
        {
            return transform.position;
        }

        private Vector3 GetAttractionPoint()
        {
            switch (mode)
            {
                case FieldMode.ToTarget:
                    return targetTransform != null ? targetTransform.position : transform.position;

                case FieldMode.ToPoint:
                    return targetPoint;

                default:
                    return transform.position;
            }
        }

        private Vector3 GetDirectionWorldNormalized()
        {
            Vector3 dir = directionIsLocal ? transform.TransformDirection(direction) : direction;
            if (dir.sqrMagnitude < 0.0001f)
            {
                dir = transform.forward;
            }

            return dir.normalized;
        }

        private float CalculateForceAtDistance(float distance, float baseForce)
        {
            if (distance >= radius)
            {
                return 0f;
            }

            if (falloffType == FalloffType.Constant)
            {
                return baseForce;
            }

            float normalizedDistance = distance / radius;
            float falloff = falloffType == FalloffType.Linear
                ? 1f - normalizedDistance
                : 1f - normalizedDistance * normalizedDistance;

            return baseForce * falloff;
        }

        [Button("Toggle Mode")]
        private void ToggleModeButton()
        {
            ToggleMode();
        }

        [Button("Reset Toggle Timer")]
        private void ResetToggleTimerButton()
        {
            ResetToggleTimer();
        }
    }
}
