using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace Neo.Tools
{
    /// <summary>
    ///     AI navigation component with pathfinding, movement, patrol, and animation support.
    ///     Deprecated: use Neo.NPC.NpcNavigation + modules.
    /// </summary>
    [LegacyComponent("Neo.NPC.NpcNavigation")]
    [Obsolete("Deprecated: use Neo.NPC.NpcNavigation + modules.")]
    [NeoDoc("Tools/Other/AiNavigation.md")]
    [RequireComponent(typeof(NavMeshAgent))]
    [CreateFromMenu("Neoxider/Tools/Other/AiNavigation")]
    [AddComponentMenu("Neoxider/Tools/Other/AiNavigation (Legacy)")]
    public class AiNavigation : MonoBehaviour
    {
        public enum MovementMode
        {
            FollowTarget,
            Patrol,
            Combined
        }

        [Header("DEPRECATED: use NPCNavigation (Neoxider/NPC/NpcNavigation)")]
        [Header("Movement Mode")]
        [SerializeField]
        private MovementMode movementMode = MovementMode.FollowTarget;

        [Header("Follow Target")] [SerializeField]
        private Transform target;

        [Tooltip("Maximum target distance for following (0 = no distance gate).")] [Min(0)] [SerializeField]
        private float triggerDistance;

        [Tooltip("Distance from target where agent stops.")] [SerializeField]
        private float stoppingDistance = 2f;

        [Header("Patrol")] [Tooltip("Patrol points array.")] [SerializeField]
        private Transform[] patrolPoints;

        [Tooltip("Zone for random patrol (if set, ignores patrol points).")] [SerializeField]
        private BoxCollider patrolZone;

        [Tooltip("Wait time at each patrol point.")] [SerializeField]
        private float patrolWaitTime = 1f;

        [Tooltip("Loop patrol route.")] [SerializeField]
        private bool loopPatrol = true;

        [Header("Combined Mode")]
        [Tooltip("Distance to start following target (0 = never start automatically).")]
        [SerializeField]
        private float aggroDistance = 10f;

        [Tooltip("Distance to stop following and return to patrol (0 = never stop).")] [SerializeField]
        private float maxFollowDistance = 20f;

        [Header("General")] [SerializeField] private bool updateRotation = true;

        [Header("Movement")] [Tooltip("Normal walking speed.")] [SerializeField]
        private float walkSpeed = 3f;

        [Tooltip("Running speed.")] [SerializeField]
        private float runSpeed = 6f;

        [SerializeField] private float acceleration = 8f;

        [Tooltip("Turn speed in degrees per second.")] [SerializeField]
        private float turnSpeed = 260f;

        [SerializeField] private float maxPathLength = 100f;

        [Header("Path")] [SerializeField] private bool autoUpdatePath = true;

        [SerializeField] private float pathUpdateInterval = 0.5f;

        [SerializeField] private float pathCheckRadius = 0.5f;

        [Header("Animation Settings")] [Tooltip("Animator to control (optional).")] [SerializeField]
        private Animator animator;

        [Tooltip("Float parameter for normalized speed (0-1).")] [SerializeField]
        private string speedParameter = "Speed";

        [Tooltip("Bool parameter for movement state.")] [SerializeField]
        private string isMovingParameter = "IsMoving";

        [Tooltip("Smoothing time for speed transitions.")] [SerializeField]
        private float animationDampTime = 0.1f;

        [Header("Debug")] [Tooltip("Enable detailed logging for troubleshooting.")] [SerializeField]
        private bool debugMode;

        public UnityEvent<Vector3> onDestinationReached;

        public UnityEvent<Vector3> onPathBlocked;
        public UnityEvent<float> onSpeedChanged;
        public UnityEvent<Vector3> onPathUpdated;
        public UnityEvent<NavMeshPathStatus> onPathStatusChanged;

        [Header("Patrol Events")] public UnityEvent<int> onPatrolPointReached;

        public UnityEvent onPatrolStarted;
        public UnityEvent onPatrolCompleted;

        [Header("Combined Mode Events")] public UnityEvent onStartFollowing;

        public UnityEvent onStopFollowing;

        private NavMeshAgent agent;
        private float aggroDistanceSqr;
        private float currentSpeedVelocity;
        private bool hasStopped = true;
        private Transform initialTarget;
        private bool isFollowingTarget;
        private bool isInitialized;
        private bool isMovementPaused;
        private int isMovingHash;
        private bool isWaitingAtPatrol;
        private NavMeshPathStatus lastPathStatus;
        private float lastPathUpdateTime;
        private Vector3 lastTargetPosition;
        private float maxFollowDistanceSqr;
        private float nextDebugLogTime;
        private int requestVersion;
        private bool hasActiveRequest;
        private Vector3 requestedDestination;
        private int requestSubmittedFrame;
        private Coroutine patrolWaitCoroutine;
        private Coroutine patrolRetryCoroutine;
        private int patrolVersion;
        private int speedHash;

        public Transform Target => target;
        public bool HasReachedDestination { get; private set; }
        public bool IsPathBlocked { get; private set; }
        public NavMeshPathStatus PathStatus => IsOnNavMesh ? agent.pathStatus : NavMeshPathStatus.PathInvalid;

        public float RemainingDistance
        {
            get
            {
                if (agent != null && agent.enabled && agent.isOnNavMesh && agent.hasPath)
                {
                    return agent.remainingDistance;
                }

                return 0f;
            }
        }

        public float CurrentSpeed => IsOnNavMesh ? agent.velocity.magnitude : 0f;
        public bool IsOnNavMesh => agent != null && agent.enabled && agent.isOnNavMesh;
        public bool HasPath => agent != null && agent.enabled && agent.isOnNavMesh && agent.hasPath;
        public bool IsMoving => !hasStopped;
        public bool IsRunning { get; private set; }

        public bool IsPatrolling { get; private set; }

        public int CurrentPatrolIndex { get; private set; }

        public MovementMode CurrentMode => movementMode;

        /// <summary>
        ///     Whether a patrol zone is used instead of waypoints.
        /// </summary>
        public bool UsesPatrolZone => patrolZone != null;

        public float StoppingDistance
        {
            get => stoppingDistance;
            set
            {
                stoppingDistance = Mathf.Max(0.01f, value);
                if (agent != null)
                {
                    agent.stoppingDistance = stoppingDistance;
                }
            }
        }

        public float WalkSpeed
        {
            get => walkSpeed;
            set
            {
                walkSpeed = Mathf.Max(0.1f, value);
                if (agent != null && !IsRunning)
                {
                    agent.speed = walkSpeed;
                }
            }
        }

        public float RunSpeed
        {
            get => runSpeed;
            set
            {
                runSpeed = Mathf.Max(0.1f, value);
                if (agent != null && IsRunning)
                {
                    agent.speed = runSpeed;
                }
            }
        }

        public float Acceleration
        {
            get => acceleration;
            set
            {
                acceleration = Mathf.Max(0.1f, value);
                if (agent != null)
                {
                    agent.acceleration = acceleration;
                }
            }
        }

        public float TurnSpeed
        {
            get => turnSpeed;
            set
            {
                turnSpeed = Mathf.Max(1f, value);
                if (agent != null)
                {
                    agent.angularSpeed = turnSpeed;
                }
            }
        }

        public float TriggerDistance
        {
            get => triggerDistance;
            set => triggerDistance = Mathf.Max(0f, value);
        }

        public bool AutoUpdatePath
        {
            get => autoUpdatePath;
            set
            {
                if (autoUpdatePath == value)
                {
                    return;
                }

                autoUpdatePath = value;
                if (autoUpdatePath && target != null && isActiveAndEnabled && !isMovementPaused)
                {
                    UpdatePath();
                }
            }
        }

        private void OnValidate()
        {
            if (agent == null)
            {
                agent = GetComponent<NavMeshAgent>();
            }

            triggerDistance = Mathf.Max(0f, triggerDistance);
            stoppingDistance = Mathf.Max(0.01f, stoppingDistance);
            walkSpeed = Mathf.Max(0.1f, walkSpeed);
            runSpeed = Mathf.Max(0.1f, runSpeed);
            acceleration = Mathf.Max(0.1f, acceleration);
            turnSpeed = Mathf.Max(1f, turnSpeed);
            pathUpdateInterval = Mathf.Max(0.1f, pathUpdateInterval);
            pathCheckRadius = Mathf.Max(0.1f, pathCheckRadius);
            maxPathLength = Mathf.Max(1f, maxPathLength);

            CacheSquaredDistances();
        }

        private void CacheSquaredDistances()
        {
            aggroDistanceSqr = aggroDistance * aggroDistance;
            maxFollowDistanceSqr = maxFollowDistance * maxFollowDistance;
        }

        private void CacheAnimatorHashes()
        {
            if (animator != null)
            {
                speedHash = Animator.StringToHash(speedParameter);
                isMovingHash = Animator.StringToHash(isMovingParameter);
            }
        }

        private void Awake()
        {
            InitializeComponents();
            ConfigureAgent();
            CacheAnimatorHashes();
            CacheSquaredDistances();
            isInitialized = true;
        }

        private void Start()
        {
            initialTarget = target;
            if (hasActiveRequest || isMovementPaused)
            {
                return;
            }

            switch (movementMode)
            {
                case MovementMode.FollowTarget:
                    if (target != null)
                    {
                        SetTarget(target);
                    }
                    else
                    {
                        NeoDiagnostics.LogWarning(
                            $"AiNavigation ({gameObject.name}): FollowTarget mode requires target to be set!");
                    }

                    break;

                case MovementMode.Patrol:
                    StartPatrol();
                    break;

                case MovementMode.Combined:
                    // WHY: Target check warning removed to prevent log spam in deprecated component.

                    if (aggroDistance <= 0f)
                    {
                        NeoDiagnostics.LogWarning(
                            $"AiNavigation ({gameObject.name}): Combined mode requires aggroDistance > 0! Currently {aggroDistance}");
                    }

                    StartPatrol();
                    break;
            }
        }

        private void Update()
        {
            if (!isInitialized)
            {
                return;
            }

            int version = requestVersion;
            UpdateMovementMode();
            if (this == null || version != requestVersion || !isActiveAndEnabled)
            {
                return;
            }

            // Observe the previous request before a due refresh stamps this submission frame.
            CheckPathStatus();
            if (this == null || version != requestVersion || !isActiveAndEnabled)
            {
                return;
            }

            if (!UpdatePathfinding())
            {
                return;
            }

            UpdateAnimation();
        }

        private void OnDrawGizmos()
        {
            if (HasPath)
            {
                Gizmos.color = Color.yellow;
                NavMeshPath path = agent.path;
                Vector3 previousCorner = transform.position;
                foreach (Vector3 corner in path.corners)
                {
                    Gizmos.DrawLine(previousCorner, corner);
                    previousCorner = corner;
                }
            }

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, stoppingDistance);

            if (triggerDistance > 0f)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(transform.position, triggerDistance);
            }

            if (movementMode == MovementMode.Combined)
            {
                if (aggroDistance > 0f)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(transform.position, aggroDistance);
                }

                if (maxFollowDistance > 0f)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireSphere(transform.position, maxFollowDistance);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (movementMode == MovementMode.Patrol || movementMode == MovementMode.Combined)
            {
                DrawPatrolPath();
            }
        }

        private void DrawPatrolPath()
        {
            if (patrolZone != null)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Gizmos.matrix = patrolZone.transform.localToWorldMatrix;
                Gizmos.DrawCube(patrolZone.center, patrolZone.size);
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(patrolZone.center, patrolZone.size);
                Gizmos.matrix = oldMatrix;
                return;
            }

            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                return;
            }

            Gizmos.color = Color.green;

            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] == null)
                {
                    continue;
                }

                Vector3 pointPos = patrolPoints[i].position;
                Gizmos.DrawWireSphere(pointPos, 0.5f);

                if (i == CurrentPatrolIndex && Application.isPlaying)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(pointPos, 0.7f);
                    Gizmos.color = Color.green;
                }

                if (i < patrolPoints.Length - 1 && patrolPoints[i + 1] != null)
                {
                    Gizmos.DrawLine(pointPos, patrolPoints[i + 1].position);
                }
                else if (loopPatrol && i == patrolPoints.Length - 1 && patrolPoints[0] != null)
                {
                    Gizmos.DrawLine(pointPos, patrolPoints[0].position);
                }
            }
        }

        private void OnDisable()
        {
            requestVersion++;
            CancelPatrolCoroutines();
        }

        /// <summary>
        ///     Set new navigation target.
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            if (newTarget == null)
            {
                target = null;
                Stop();
                return;
            }

            if (!isInitialized || agent == null || !agent.enabled || !isActiveAndEnabled)
            {
                NeoDiagnostics.LogWarning("AiNavigation: Cannot set target - agent not ready");
                return;
            }

            if (!agent.isOnNavMesh)
            {
                NeoDiagnostics.LogWarning($"AiNavigation: Agent {gameObject.name} is not on NavMesh!");
                return;
            }

            Vector3 destination = newTarget.position;
            if (!TrySubmitPath(destination, true))
            {
                return;
            }

            CancelPatrolCoroutines();
            target = newTarget;
            isMovementPaused = false;
            agent.isStopped = false;
            agent.stoppingDistance = stoppingDistance;

            // All request state is committed before user callbacks. A callback's Stop/replacement wins.
            onPathUpdated?.Invoke(destination);
        }

        /// <summary>
        ///     Set destination by position.
        /// </summary>
        public bool SetDestination(Vector3 destination)
        {
            if (!isInitialized || agent == null || !agent.enabled || !isActiveAndEnabled)
            {
                NeoDiagnostics.LogWarning("AiNavigation: Cannot set destination - agent not ready");
                return false;
            }

            if (!agent.isOnNavMesh)
            {
                NeoDiagnostics.LogWarning($"AiNavigation: Agent {gameObject.name} is not on NavMesh!");
                return false;
            }

            if (NavMesh.SamplePosition(destination, out NavMeshHit hit, maxPathLength, NavMesh.AllAreas))
            {
                if (!TrySubmitPath(hit.position, true))
                {
                    return false;
                }

                CancelPatrolCoroutines();
                target = null;
                isMovementPaused = false;
                agent.isStopped = false;
                onPathUpdated?.Invoke(hit.position);
                return true;
            }

            NeoDiagnostics.LogWarning($"AiNavigation: Could not find valid NavMesh position near {destination}");
            return false;
        }

        /// <summary>
        ///     Enable or disable running.
        /// </summary>
        public void SetRunning(bool enable)
        {
            if (!isInitialized || agent == null)
            {
                return;
            }

            IsRunning = enable;
            agent.speed = enable ? runSpeed : walkSpeed;
            onSpeedChanged?.Invoke(agent.speed);
        }

        /// <summary>
        ///     Set absolute movement speed.
        /// </summary>
        public void SetSpeed(float speed)
        {
            if (!isInitialized || agent == null)
            {
                return;
            }

            agent.speed = Mathf.Max(0.1f, speed);
            onSpeedChanged?.Invoke(agent.speed);
        }

        /// <summary>
        ///     Stop agent immediately.
        /// </summary>
        [Button(PlayModeOnly = true)]
        public void Stop()
        {
            requestVersion++;
            hasActiveRequest = false;
            CancelPatrolCoroutines();
            isMovementPaused = true;
            hasStopped = true;
            HasReachedDestination = false;
            if (IsOnNavMesh)
            {
                agent.isStopped = true;
            }

            if (movementMode != MovementMode.Combined)
            {
                target = null;
            }
        }

        /// <summary>
        ///     Resume agent movement.
        /// </summary>
        [Button(PlayModeOnly = true)]
        public void Resume()
        {
            if (!isInitialized || !IsOnNavMesh || !isActiveAndEnabled)
            {
                return;
            }

            requestVersion++;
            agent.isStopped = false;
            isMovementPaused = false;
            hasStopped = IsPatrolling && !agent.pathPending &&
                         (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance);
            HasReachedDestination = false;
            hasActiveRequest = !hasStopped && (agent.hasPath || agent.pathPending);
            if (hasActiveRequest)
            {
                requestedDestination = agent.destination;
                requestSubmittedFrame = Time.frameCount;
            }

            if (target != null)
            {
                UpdatePath();
            }
            else if (!hasActiveRequest)
            {
                hasStopped = true;
            }
        }

        /// <summary>
        ///     Warp agent to position.
        /// </summary>
        public bool WarpToPosition(Vector3 position)
        {
            if (!isInitialized || agent == null || !agent.enabled || !isActiveAndEnabled)
            {
                return false;
            }

            if (NavMesh.SamplePosition(position, out NavMeshHit hit, maxPathLength, NavMesh.AllAreas))
            {
                return agent.Warp(hit.position);
            }

            NeoDiagnostics.LogWarning($"AiNavigation: Could not warp to position {position}");
            return false;
        }

        /// <summary>
        ///     Check if position is reachable.
        /// </summary>
        public bool IsPositionReachable(Vector3 position)
        {
            if (!isInitialized || !IsOnNavMesh)
            {
                return false;
            }

            if (NavMesh.SamplePosition(position, out NavMeshHit hit, maxPathLength, NavMesh.AllAreas))
            {
                NavMeshPath path = new();
                if (NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, path))
                {
                    return path.status == NavMeshPathStatus.PathComplete;
                }
            }

            return false;
        }

        /// <summary>
        ///     Get path to position.
        /// </summary>
        public NavMeshPath GetPathToPosition(Vector3 position)
        {
            NavMeshPath path = new();

            if (NavMesh.SamplePosition(position, out NavMeshHit hit, maxPathLength, NavMesh.AllAreas))
            {
                NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, path);
            }

            return path;
        }

        /// <summary>
        ///     Start patrol.
        /// </summary>
        [Button(PlayModeOnly = true)]
        public void StartPatrol()
        {
            CancelPatrolCoroutines();
            if (patrolZone == null &&
                (patrolPoints == null || !Array.Exists(patrolPoints, point => point != null)))
            {
                if (IsPatrolling)
                {
                    StopPatrol();
                }

                NeoDiagnostics.LogWarningThrottled($"AiNavigation.NoPatrolRoute.{NeoDiagnostics.StableId(this)}",
                    $"AiNavigation ({gameObject.name}): No valid patrol points or patrol zone set!", this);
                return;
            }

            if (!isInitialized || !IsOnNavMesh || !isActiveAndEnabled)
            {
                return;
            }

            if (debugMode)
            {
                NeoDiagnostics.Log($"[{gameObject.name}] Starting patrol" +
                                   (patrolZone != null ? " in zone" : " with points"));
            }

            if (movementMode == MovementMode.Combined && target == null && initialTarget != null)
            {
                target = initialTarget;

                if (debugMode)
                {
                    NeoDiagnostics.Log($"[{gameObject.name}] Restored target from initialTarget: {target.name}");
                }
            }

            IsPatrolling = true;
            isMovementPaused = false;
            isFollowingTarget = false;
            isWaitingAtPatrol = false;
            CurrentPatrolIndex = 0;

            if (patrolZone != null)
            {
                MoveToRandomPointInZone();
            }
            else
            {
                MoveToPatrolPoint(CurrentPatrolIndex);
            }

            onPatrolStarted?.Invoke();
        }

        /// <summary>
        ///     Stop patrol.
        /// </summary>
        [Button(PlayModeOnly = true)]
        public void StopPatrol()
        {
            IsPatrolling = false;
            Stop();
        }

        /// <summary>
        ///     Set movement mode at runtime.
        /// </summary>
        public void SetMovementMode(MovementMode mode)
        {
            movementMode = mode;

            switch (mode)
            {
                case MovementMode.FollowTarget:
                    Transform followTarget = target != null ? target : initialTarget;
                    StopPatrol();
                    if (followTarget != null)
                    {
                        SetTarget(followTarget);
                    }

                    break;

                case MovementMode.Patrol:
                    StartPatrol();
                    break;

                case MovementMode.Combined:
                    StartPatrol();
                    break;
            }
        }

        /// <summary>
        ///     Set patrol points at runtime.
        /// </summary>
        public void SetPatrolPoints(Transform[] points)
        {
            patrolPoints = points;
            if (IsPatrolling && patrolZone == null)
            {
                CancelPatrolCoroutines();
                CurrentPatrolIndex = 0;
                MoveToPatrolPoint(CurrentPatrolIndex);
            }
        }

        /// <summary>
        ///     Set patrol zone at runtime. If set, patrol points will be ignored.
        /// </summary>
        public void SetPatrolZone(BoxCollider zone)
        {
            patrolZone = zone;
            if (IsPatrolling)
            {
                CancelPatrolCoroutines();
                if (patrolZone != null)
                {
                    MoveToRandomPointInZone();
                }
                else
                {
                    MoveToPatrolPoint(CurrentPatrolIndex);
                }
            }
        }

        /// <summary>
        ///     Clear patrol zone (will use patrol points instead).
        /// </summary>
        public void ClearPatrolZone()
        {
            SetPatrolZone(null);
        }

        private void UpdateMovementMode()
        {
            if (isMovementPaused || !IsOnNavMesh)
            {
                return;
            }

            switch (movementMode)
            {
                case MovementMode.FollowTarget:
                    HandleTriggerDistance();
                    break;

                case MovementMode.Patrol:
                    HandlePatrol();
                    break;

                case MovementMode.Combined:
                    HandleCombinedMode();
                    break;
            }
        }

        private void HandleTriggerDistance()
        {
            if (target == null || triggerDistance <= 0f || !IsOnNavMesh)
            {
                return;
            }

            float distanceToTarget = Vector3.Distance(transform.position, target.position);

            if (distanceToTarget > triggerDistance)
            {
                if (!agent.isStopped)
                {
                    agent.isStopped = true;
                    hasStopped = true;
                }
            }
            else
            {
                if (agent.isStopped && agent.isOnNavMesh)
                {
                    Resume();
                }
            }
        }

        private void InitializeComponents()
        {
            agent = GetComponent<NavMeshAgent>();

            if (agent == null)
            {
                NeoDiagnostics.LogError("AiNavigation: NavMeshAgent component is missing");
                enabled = false;
            }
        }

        private void ConfigureAgent()
        {
            if (agent == null)
            {
                return;
            }

            agent.speed = walkSpeed;
            agent.angularSpeed = turnSpeed;
            agent.acceleration = acceleration;
            agent.updateRotation = updateRotation;
            agent.stoppingDistance = stoppingDistance;
            agent.radius = pathCheckRadius;
            agent.areaMask = NavMesh.AllAreas;
        }

        private bool UpdatePathfinding()
        {
            if (!autoUpdatePath || isMovementPaused)
            {
                return true;
            }

            bool shouldUpdatePath = (movementMode == MovementMode.FollowTarget && target != null) ||
                                    (movementMode == MovementMode.Combined && isFollowingTarget && target != null);

            if (shouldUpdatePath && Time.time >= lastPathUpdateTime + pathUpdateInterval)
            {
                // Reserve the interval before callbacks, including unsuccessful native submissions.
                lastPathUpdateTime = Time.time;
                return UpdatePath();
            }

            return true;
        }

        private bool UpdatePath()
        {
            if (target != null && !isMovementPaused && isActiveAndEnabled)
            {
                Vector3 destination = target.position;
                if (TrySubmitPath(destination, false))
                {
                    int version = requestVersion;
                    onPathUpdated?.Invoke(destination);
                    return this != null && version == requestVersion && isActiveAndEnabled;
                }
            }

            return true;
        }

        private bool TrySubmitPath(Vector3 destination, bool restartArrival)
        {
            if (!isInitialized || !isActiveAndEnabled || !IsOnNavMesh || !agent.SetDestination(destination))
            {
                return false;
            }

            requestVersion++;
            requestedDestination = destination;
            requestSubmittedFrame = Time.frameCount;
            lastPathUpdateTime = Time.time;
            if (restartArrival || (destination - lastTargetPosition).sqrMagnitude > 0.01f)
            {
                hasStopped = false;
                HasReachedDestination = false;
                IsPathBlocked = false;
            }

            hasActiveRequest = !hasStopped;
            lastTargetPosition = destination;
            return true;
        }

        private void UpdateAnimation()
        {
            if (animator == null)
            {
                return;
            }

            float normalizedSpeed = 0f;
            if (IsOnNavMesh && agent.speed > 0f)
            {
                normalizedSpeed = agent.velocity.magnitude / agent.speed;
            }

            float smoothSpeed = Mathf.SmoothDamp(
                animator.GetFloat(speedHash),
                normalizedSpeed,
                ref currentSpeedVelocity,
                animationDampTime
            );

            animator.SetFloat(speedHash, smoothSpeed);
            animator.SetBool(isMovingHash, !hasStopped);
        }

        private void CheckPathStatus()
        {
            if (!isActiveAndEnabled || !IsOnNavMesh || isMovementPaused || hasStopped || !hasActiveRequest ||
                agent.pathPending || requestSubmittedFrame == Time.frameCount)
            {
                return;
            }

            int version = requestVersion;
            Vector3 destination = requestedDestination;
            bool hasPath = agent.hasPath;
            NavMeshPathStatus status = hasPath ? agent.pathStatus : NavMeshPathStatus.PathInvalid;
            bool reached = hasPath
                ? status == NavMeshPathStatus.PathComplete && agent.remainingDistance <= agent.stoppingDistance
                : (agent.nextPosition - destination).sqrMagnitude <= agent.stoppingDistance * agent.stoppingDistance;
            if (reached && !hasPath)
            {
                // An accepted destination already within stopping distance needs no remaining path.
                status = NavMeshPathStatus.PathComplete;
            }

            if (reached)
            {
                hasStopped = true;
                hasActiveRequest = false;
                HasReachedDestination = true;
                IsPathBlocked = false;
                onDestinationReached?.Invoke(destination);
            }
            else if (!hasPath || status == NavMeshPathStatus.PathPartial || status == NavMeshPathStatus.PathInvalid)
            {
                if (!IsPathBlocked)
                {
                    IsPathBlocked = true;
                    HasReachedDestination = false;
                    onPathBlocked?.Invoke(destination);
                }
            }
            else
            {
                IsPathBlocked = false;
            }

            if (this == null || version != requestVersion || !isActiveAndEnabled || !IsOnNavMesh || isMovementPaused)
            {
                return;
            }

            if (lastPathStatus != status)
            {
                lastPathStatus = status;
                onPathStatusChanged?.Invoke(status);
            }
        }

        private void HandlePatrol()
        {
            if (!IsPatrolling)
            {
                return;
            }

            if (isWaitingAtPatrol)
            {
                return;
            }

            if (hasStopped && agent != null && !agent.pathPending)
            {
                patrolWaitCoroutine = StartCoroutine(WaitAtPatrolPoint(patrolVersion));
            }
        }

        private void HandleCombinedMode()
        {
            if (initialTarget == null)
            {
                if (debugMode && Time.time >= nextDebugLogTime)
                {
                    NeoDiagnostics.LogWarning(
                        $"[{gameObject.name}] Combined: initialTarget is null! Set target in inspector.");
                    nextDebugLogTime = Time.time + 1f;
                }

                if (!IsPatrolling)
                {
                    StartPatrol();
                }

                HandlePatrol();
                return;
            }

            float distanceSqr = (transform.position - initialTarget.position).sqrMagnitude;

            if (debugMode && Time.time >= nextDebugLogTime)
            {
                float dist = Mathf.Sqrt(distanceSqr);
                NeoDiagnostics.Log(
                    $"[{gameObject.name}] Combined: dist={dist:F1}, aggroDistance={aggroDistance}, isFollowing={isFollowingTarget}, isPatrolling={IsPatrolling}, target={initialTarget.name}");
                nextDebugLogTime = Time.time + 1f;
            }

            bool shouldFollow = aggroDistanceSqr > 0f && distanceSqr <= aggroDistanceSqr;
            bool shouldStopFollowing = maxFollowDistanceSqr > 0f && distanceSqr > maxFollowDistanceSqr;

            if (shouldFollow && !isFollowingTarget)
            {
                CancelPatrolCoroutines();
                if (debugMode)
                {
                    float dist = Mathf.Sqrt(distanceSqr);
                    NeoDiagnostics.Log(
                        $"[{gameObject.name}] AGGRO! Starting to follow {initialTarget.name} at distance {dist:F1}m");
                }

                isFollowingTarget = true;
                IsPatrolling = false;
                isWaitingAtPatrol = false;
                target = initialTarget;
                int version = ++requestVersion;
                onStartFollowing?.Invoke();
                if (this == null || version != requestVersion || !isActiveAndEnabled || isMovementPaused)
                {
                    return;
                }

                if (agent != null && agent.enabled && agent.isOnNavMesh)
                {
                    agent.stoppingDistance = stoppingDistance;
                    agent.isStopped = false;
                    if (!hasActiveRequest || (requestedDestination - initialTarget.position).sqrMagnitude > 0.01f)
                    {
                        TrySubmitPath(initialTarget.position, true);
                    }

                    if (debugMode)
                    {
                        NeoDiagnostics.Log($"[{gameObject.name}] Set destination to target: {initialTarget.position}");
                    }
                }
            }
            else if (shouldStopFollowing && isFollowingTarget)
            {
                if (debugMode)
                {
                    float dist = Mathf.Sqrt(distanceSqr);
                    NeoDiagnostics.Log($"[{gameObject.name}] DE-AGGRO! Returning to patrol at distance {dist:F1}m");
                }

                isFollowingTarget = false;
                int version = ++requestVersion;
                onStopFollowing?.Invoke();
                if (this == null || version != requestVersion || !isActiveAndEnabled || isMovementPaused)
                {
                    return;
                }

                ReturnToPatrol();
            }

            if (isFollowingTarget)
            {
                if (target == null)
                {
                    target = initialTarget;
                }

                HandleTriggerDistance();
            }
            else
            {
                HandlePatrol();
            }
        }

        private void MoveToPatrolPoint(int index)
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                StopPatrol();
                return;
            }

            // Inspect at most one route, including routes whose waypoints were destroyed at runtime.
            for (int inspected = 0; inspected < patrolPoints.Length; inspected++)
            {
                if (index >= patrolPoints.Length)
                {
                    if (!loopPatrol)
                    {
                        StopPatrol();
                        onPatrolCompleted?.Invoke();
                        return;
                    }

                    index = 0;
                }

                if (index < 0)
                {
                    break;
                }

                CurrentPatrolIndex = index;
                Transform point = patrolPoints[index];
                if (point != null)
                {
                    if (agent != null)
                    {
                        agent.stoppingDistance = stoppingDistance;
                    }

                    SetDestination(point.position);
                    return;
                }

                index++;
            }

            StopPatrol();
        }

        private void MoveToRandomPointInZone()
        {
            if (patrolZone == null)
            {
                return;
            }

            Vector3 randomPoint = GetRandomPointInBox(patrolZone);

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, patrolZone.size.magnitude, NavMesh.AllAreas))
            {
                if (agent != null)
                {
                    agent.stoppingDistance = stoppingDistance;
                }

                hasStopped = false;
                SetDestination(hit.position);

                if (debugMode)
                {
                    NeoDiagnostics.Log($"[{gameObject.name}] Moving to random point in zone: {hit.position}");
                }
            }
            else
            {
                if (debugMode)
                {
                    NeoDiagnostics.LogWarning(
                        $"[{gameObject.name}] Could not find valid NavMesh position in patrol zone");
                }

                if (patrolRetryCoroutine == null)
                {
                    patrolRetryCoroutine = StartCoroutine(RetryMoveToRandomPoint(patrolVersion));
                }
            }
        }

        private IEnumerator RetryMoveToRandomPoint(int version)
        {
            yield return new WaitForSeconds(0.5f);

            if (version == patrolVersion && IsPatrolling && isActiveAndEnabled && !isMovementPaused &&
                patrolZone != null)
            {
                patrolRetryCoroutine = null;
                MoveToRandomPointInZone();
            }
        }

        private Vector3 GetRandomPointInBox(BoxCollider box)
        {
            Vector3 center = box.transform.TransformPoint(box.center);
            Vector3 size = box.size;
            Vector3 scale = box.transform.lossyScale;

            Vector3 halfExtents = new(
                size.x * scale.x * 0.5f,
                size.y * scale.y * 0.5f,
                size.z * scale.z * 0.5f
            );

            Vector3 randomLocal = new(
                Random.Range(-halfExtents.x, halfExtents.x),
                Random.Range(-halfExtents.y, halfExtents.y),
                Random.Range(-halfExtents.z, halfExtents.z)
            );

            return center + box.transform.rotation * randomLocal;
        }

        private IEnumerator WaitAtPatrolPoint(int version)
        {
            isWaitingAtPatrol = true;
            onPatrolPointReached?.Invoke(CurrentPatrolIndex);
            if (version != patrolVersion || !IsPatrolling || !isActiveAndEnabled || isMovementPaused)
            {
                yield break;
            }

            if (debugMode)
            {
                string pointInfo = patrolZone != null ? "random point in zone" : $"point {CurrentPatrolIndex}";
                NeoDiagnostics.Log($"[{gameObject.name}] Reached patrol {pointInfo}, waiting {patrolWaitTime}s");
            }

            yield return new WaitForSeconds(patrolWaitTime);

            if (version != patrolVersion || !IsPatrolling || !isActiveAndEnabled || isMovementPaused)
            {
                yield break;
            }

            patrolWaitCoroutine = null;
            isWaitingAtPatrol = false;
            MoveToNextPatrolPoint();
        }

        private void MoveToNextPatrolPoint()
        {
            if (!IsPatrolling || isMovementPaused || !isActiveAndEnabled)
            {
                return;
            }

            if (patrolZone != null)
            {
                if (debugMode)
                {
                    NeoDiagnostics.Log($"[{gameObject.name}] Moving to next random point in zone");
                }

                MoveToRandomPointInZone();
                return;
            }

            CurrentPatrolIndex++;

            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                StopPatrol();
                return;
            }

            if (CurrentPatrolIndex >= patrolPoints.Length)
            {
                if (loopPatrol)
                {
                    CurrentPatrolIndex = 0;

                    if (debugMode)
                    {
                        NeoDiagnostics.Log($"[{gameObject.name}] Patrol loop: returning to point 0");
                    }
                }
                else
                {
                    IsPatrolling = false;
                    onPatrolCompleted?.Invoke();

                    if (debugMode)
                    {
                        NeoDiagnostics.Log($"[{gameObject.name}] Patrol completed (no loop)");
                    }

                    return;
                }
            }

            if (debugMode)
            {
                NeoDiagnostics.Log($"[{gameObject.name}] Moving to patrol point {CurrentPatrolIndex}");
            }

            MoveToPatrolPoint(CurrentPatrolIndex);
        }

        private void ReturnToPatrol()
        {
            requestVersion++;
            hasActiveRequest = false;
            CancelPatrolCoroutines();
            if (IsOnNavMesh)
            {
                agent.isStopped = true;
            }

            hasStopped = true;
            IsPatrolling = true;
            isFollowingTarget = false;

            if (debugMode)
            {
                NeoDiagnostics.Log($"[{gameObject.name}] Waiting {patrolWaitTime}s before resuming patrol");
            }

            patrolWaitCoroutine = StartCoroutine(WaitBeforeResumePatrol(patrolVersion));
        }

        private IEnumerator WaitBeforeResumePatrol(int version)
        {
            isWaitingAtPatrol = true;

            yield return new WaitForSeconds(patrolWaitTime);

            if (version != patrolVersion || !IsPatrolling || !isActiveAndEnabled || isMovementPaused)
            {
                yield break;
            }

            patrolWaitCoroutine = null;
            isWaitingAtPatrol = false;

            if (IsOnNavMesh)
            {
                agent.isStopped = false;
            }

            if (patrolZone != null)
            {
                if (debugMode)
                {
                    NeoDiagnostics.Log($"[{gameObject.name}] Resuming patrol, moving to random point in zone");
                }

                MoveToRandomPointInZone();
            }
            else
            {
                if (debugMode)
                {
                    NeoDiagnostics.Log($"[{gameObject.name}] Resuming patrol, moving to point {CurrentPatrolIndex}");
                }

                MoveToPatrolPoint(CurrentPatrolIndex);
            }
        }

        private void CancelPatrolCoroutines()
        {
            patrolVersion++;
            if (patrolWaitCoroutine != null)
            {
                StopCoroutine(patrolWaitCoroutine);
                patrolWaitCoroutine = null;
            }

            if (patrolRetryCoroutine != null)
            {
                StopCoroutine(patrolRetryCoroutine);
                patrolRetryCoroutine = null;
            }

            isWaitingAtPatrol = false;
        }
    }
}
