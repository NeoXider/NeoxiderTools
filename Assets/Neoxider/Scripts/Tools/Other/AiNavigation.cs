using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Enhanced AI navigation component that provides pathfinding and movement behavior
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    public class AiNavigation : MonoBehaviour
    {
        #region Validation

        private void OnValidate()
        {
            if (agent == null)
            {
                agent = GetComponent<NavMeshAgent>();
            }

            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            // Ensure positive values
            triggerDistance = Mathf.Max(0f, triggerDistance);
            stoppingDistance = Mathf.Max(0.01f, stoppingDistance);
            baseSpeed = Mathf.Max(0.1f, baseSpeed);
            sprintSpeedMultiplier = Mathf.Max(1f, sprintSpeedMultiplier);
            acceleration = Mathf.Max(0.1f, acceleration);
            turnSpeed = Mathf.Max(1f, turnSpeed);
            pathUpdateInterval = Mathf.Max(0.1f, pathUpdateInterval);
            pathCheckRadius = Mathf.Max(0.1f, pathCheckRadius);
            maxPathLength = Mathf.Max(1f, maxPathLength);
        }

        #endregion

        #region Inspector Fields

        [Header("Navigation Settings")] [SerializeField]
        private Transform target;

        [Min(0)] [SerializeField] private float triggerDistance = 0;
        [SerializeField] private float stoppingDistance = 0.1f;
        [SerializeField] private bool updateRotation = true;

        [Header("Movement Settings")] [SerializeField]
        private float baseSpeed = 3.5f;

        [SerializeField] private float sprintSpeedMultiplier = 1.5f;
        [SerializeField] private float acceleration = 8.0f;
        [SerializeField] private float turnSpeed = 120f;
        [SerializeField] private float maxPathLength = 100f;

        [Header("Path Settings")] [SerializeField]
        private bool autoUpdatePath = true;

        [SerializeField] private float pathUpdateInterval = 0.5f;
        [SerializeField] private float pathCheckRadius = 0.5f;
        [SerializeField] private bool usePathOptimization = true;

        [Header("Animation Settings")] [SerializeField]
        private string speedParameterName = "Speed";

        [SerializeField] private string isMovingParameterName = "IsMoving";
        [SerializeField] private float animationDampTime = 0.1f;

        #endregion

        #region Events

        public UnityEvent<Vector3> OnDestinationReached;
        public UnityEvent<Vector3> OnPathBlocked;
        public UnityEvent<float> OnSpeedChanged;
        public UnityEvent<Vector3> OnPathUpdated;
        public UnityEvent<NavMeshPathStatus> OnPathStatusChanged;

        #endregion

        #region Private Fields

        private NavMeshAgent agent;
        private Animator animator;
        private bool hasStopped = true;
        private float lastPathUpdateTime;
        private Vector3 lastTargetPosition;
        private NavMeshPathStatus lastPathStatus;
        private Coroutine pathUpdateCoroutine;
        private bool isInitialized;
        private float currentSpeedVelocity;

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the current navigation target
        /// </summary>
        public Transform Target => target;

        /// <summary>
        ///     Gets whether the agent has reached its destination
        /// </summary>
        public bool HasReachedDestination => hasStopped && target != null;

        /// <summary>
        ///     Gets whether the agent's path is blocked
        /// </summary>
        public bool IsPathBlocked { get; private set; }

        /// <summary>
        ///     Gets the current path status
        /// </summary>
        public NavMeshPathStatus PathStatus => agent != null ? agent.pathStatus : NavMeshPathStatus.PathInvalid;

        /// <summary>
        ///     Gets the remaining distance to the destination
        /// </summary>
        public float RemainingDistance => agent != null ? agent.remainingDistance : 0f;

        /// <summary>
        ///     Gets the current speed of the agent
        /// </summary>
        public float CurrentSpeed => agent != null ? agent.velocity.magnitude : 0f;
        
        /// <summary>
        ///     Gets or sets the distance from the destination at which the agent should stop.
        /// </summary>
        public float StoppingDistance
        {
            get => stoppingDistance;
            set
            {
                stoppingDistance = Mathf.Max(0.01f, value);
                if (agent != null) agent.stoppingDistance = stoppingDistance;
            }
        }

        /// <summary>
        ///     Gets or sets the base movement speed of the agent.
        /// </summary>
        public float BaseSpeed
        {
            get => baseSpeed;
            set
            {
                baseSpeed = Mathf.Max(0.1f, value);
                if (agent != null) agent.speed = baseSpeed;
            }
        }

        /// <summary>
        ///     Gets or sets the acceleration of the agent.
        /// </summary>
        public float Acceleration
        {
            get => acceleration;
            set
            {
                acceleration = Mathf.Max(0.1f, value);
                if (agent != null) agent.acceleration = acceleration;
            }
        }

        /// <summary>
        ///     Gets or sets the turning speed of the agent in degrees per second.
        /// </summary>
        public float TurnSpeed
        {
            get => turnSpeed;
            set
            {
                turnSpeed = Mathf.Max(1f, value);
                if (agent != null) agent.angularSpeed = turnSpeed;
            }
        }
        
        /// <summary>
        ///     Gets or sets the distance at which the agent starts moving towards the target.
        ///     If set to 0, the agent will always move if a target is set.
        /// </summary>
        public float TriggerDistance
        {
            get => triggerDistance;
            set => triggerDistance = Mathf.Max(0f, value);
        }

        /// <summary>
        ///     Gets or sets whether the agent's path should be updated automatically.
        /// </summary>
        public bool AutoUpdatePath
        {
            get => autoUpdatePath;
            set
            {
                autoUpdatePath = value;
                if (autoUpdatePath && target != null && pathUpdateCoroutine == null)
                {
                    pathUpdateCoroutine = StartCoroutine(PathUpdateRoutine());
                }
                else if (!autoUpdatePath && pathUpdateCoroutine != null)
                {
                    StopCoroutine(pathUpdateCoroutine);
                    pathUpdateCoroutine = null;
                }
            }
        }

        #endregion

        #region Unity Methods

        private void Awake()
        {
            InitializeComponents();
            ConfigureAgent();
            isInitialized = true;
        }

        private void Start()
        {
            if (target != null)
            {
                SetTarget(target);
            }
        }

        private void Update()
        {
            if (!isInitialized)
            {
                return;
            }

            HandleTriggerDistance();
            UpdatePathfinding();
            UpdateAnimation();
            CheckPathStatus();
        }

        private void OnDrawGizmosSelected()
        {
            if (agent != null && agent.hasPath)
            {
                // Draw path
                Gizmos.color = Color.yellow;
                NavMeshPath path = agent.path;
                Vector3 previousCorner = transform.position;
                foreach (Vector3 corner in path.corners)
                {
                    Gizmos.DrawLine(previousCorner, corner);
                    previousCorner = corner;
                }

                // Draw stopping distance
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, stoppingDistance);
            }
        }

        private void OnDisable()
        {
            if (pathUpdateCoroutine != null)
            {
                StopCoroutine(pathUpdateCoroutine);
                pathUpdateCoroutine = null;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Sets a new navigation target
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("AiNavigation: Cannot set target before initialization");
                return;
            }

            target = newTarget;
            if (target != null)
            {
                agent.stoppingDistance = stoppingDistance;
                UpdatePath();
                hasStopped = false;
                IsPathBlocked = false;

                // Start path update coroutine if auto-update is enabled
                if (autoUpdatePath && pathUpdateCoroutine == null)
                {
                    pathUpdateCoroutine = StartCoroutine(PathUpdateRoutine());
                }
            }
            else
            {
                Stop();
            }
        }

        /// <summary>
        ///     Sets a new navigation target by position
        /// </summary>
        public bool SetDestination(Vector3 destination)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("AiNavigation: Cannot set destination before initialization");
                return false;
            }

            // Sample position on NavMesh
            if (NavMesh.SamplePosition(destination, out NavMeshHit hit, maxPathLength, NavMesh.AllAreas))
            {
                target = null;
                agent.SetDestination(hit.position);
                hasStopped = false;
                IsPathBlocked = false;

                // Start path update coroutine if auto-update is enabled
                if (autoUpdatePath && pathUpdateCoroutine == null)
                {
                    pathUpdateCoroutine = StartCoroutine(PathUpdateRoutine());
                }

                return true;
            }

            Debug.LogWarning($"AiNavigation: Could not find valid NavMesh position near {destination}");
            return false;
        }

        /// <summary>
        ///     Sets a multiplier for the base speed.
        /// </summary>
        public void SetSpeedMultiplier(float multiplier)
        {
            if (!isInitialized)
            {
                return;
            }

            agent.speed = baseSpeed * Mathf.Max(0.1f, multiplier);
            OnSpeedChanged?.Invoke(agent.speed);
        }
        
        /// <summary>
        ///     Sets the agent's movement speed to an absolute value.
        ///     Note: This is a direct override and will be reset by calls to SetSpeedMultiplier or EnableSprint.
        /// </summary>
        public void SetAbsoluteSpeed(float newSpeed)
        {
            if (!isInitialized)
            {
                return;
            }

            agent.speed = Mathf.Max(0.1f, newSpeed);
            OnSpeedChanged?.Invoke(agent.speed);
        }

        /// <summary>
        ///     Enables sprint mode by applying the sprint speed multiplier.
        /// </summary>
        public void EnableSprint(bool enable)
        {
            SetSpeedMultiplier(enable ? sprintSpeedMultiplier : 1f);
        }

        /// <summary>
        ///     Stops the agent immediately
        /// </summary>
        public void Stop()
        {
            if (!isInitialized)
            {
                return;
            }

            agent.isStopped = true;
            hasStopped = true;
            target = null;

            if (pathUpdateCoroutine != null)
            {
                StopCoroutine(pathUpdateCoroutine);
                pathUpdateCoroutine = null;
            }
        }

        /// <summary>
        ///     Resumes the agent's movement
        /// </summary>
        public void Resume()
        {
            if (!isInitialized)
            {
                return;
            }

            agent.isStopped = false;
            hasStopped = false;

            if (target != null)
            {
                UpdatePath();
            }
        }

        /// <summary>
        ///     Warps the agent to a new position
        /// </summary>
        public bool WarpToPosition(Vector3 position)
        {
            if (!isInitialized)
            {
                return false;
            }

            if (NavMesh.SamplePosition(position, out NavMeshHit hit, maxPathLength, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
                return true;
            }

            Debug.LogWarning($"AiNavigation: Could not warp to position {position}");
            return false;
        }

        /// <summary>
        ///     Checks if a position is reachable
        /// </summary>
        public bool IsPositionReachable(Vector3 position)
        {
            if (!isInitialized)
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
        ///     Gets the path to a position
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

        #endregion

        #region Private Methods

        private void HandleTriggerDistance()
        {
            if (target == null || triggerDistance <= 0f)
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
                // Target is in range
                if (agent.isStopped)
                {
                    Resume();
                }
            }
        }

        private void InitializeComponents()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();

            if (agent == null)
            {
                Debug.LogError("AiNavigation: NavMeshAgent component is missing");
            }

            if (animator == null)
            {
                Debug.LogError("AiNavigation: Animator component is missing");
            }
        }

        private void ConfigureAgent()
        {
            if (agent == null)
            {
                return;
            }

            agent.speed = baseSpeed;
            agent.angularSpeed = turnSpeed;
            agent.acceleration = acceleration;
            agent.updateRotation = updateRotation;
            agent.stoppingDistance = stoppingDistance;
            agent.radius = pathCheckRadius;

            // Set area mask to all areas
            agent.areaMask = NavMesh.AllAreas;
        }

        private void UpdatePathfinding()
        {
            if (!autoUpdatePath || target == null)
            {
                return;
            }

            if (Time.time >= lastPathUpdateTime + pathUpdateInterval)
            {
                if ((target.position - lastTargetPosition).sqrMagnitude > 0.01f)
                {
                    UpdatePath();
                }

                lastPathUpdateTime = Time.time;
            }
        }

        private IEnumerator PathUpdateRoutine()
        {
            WaitForSeconds wait = new(pathUpdateInterval);

            while (true)
            {
                if (target != null)
                {
                    UpdatePath();
                }

                yield return wait;
            }
        }

        private void UpdatePath()
        {
            if (target != null)
            {
                agent.SetDestination(target.position);
                lastTargetPosition = target.position;
                OnPathUpdated?.Invoke(target.position);
            }
        }

        private void UpdateAnimation()
        {
            if (animator == null)
            {
                return;
            }

            // Calculate speed for animation
            float currentSpeed = agent.velocity.magnitude / agent.speed;
            currentSpeed = Mathf.SmoothDamp(animator.GetFloat(speedParameterName), currentSpeed,
                ref currentSpeedVelocity,
                animationDampTime);
            animator.SetFloat(speedParameterName, currentSpeed);

            // Update movement state
            animator.SetBool(isMovingParameterName, !hasStopped);
        }

        private void CheckPathStatus()
        {
            if (target == null)
            {
                return;
            }

            // Check if destination reached
            if (!hasStopped && !agent.pathPending)
            {
                if (agent.remainingDistance <= agent.stoppingDistance)
                {
                    hasStopped = true;
                    OnDestinationReached?.Invoke(target.position);
                }
                else if (agent.pathStatus == NavMeshPathStatus.PathPartial ||
                         agent.pathStatus == NavMeshPathStatus.PathInvalid)
                {
                    if (!IsPathBlocked)
                    {
                        IsPathBlocked = true;
                        OnPathBlocked?.Invoke(target.position);
                    }
                }
                else
                {
                    IsPathBlocked = false;
                }

                // Notify path status changes
                if (lastPathStatus != agent.pathStatus)
                {
                    lastPathStatus = agent.pathStatus;
                    OnPathStatusChanged?.Invoke(agent.pathStatus);
                }
            }
        }

        #endregion
    }
}
