using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

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
            agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponent<Animator>();

        // Ensure positive values
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
            SetTarget(target);
    }

    private void Update()
    {
        if (!isInitialized) return;

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
            var path = agent.path;
            var previousCorner = transform.position;
            foreach (var corner in path.corners)
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
                pathUpdateCoroutine = StartCoroutine(PathUpdateRoutine());
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
        if (NavMesh.SamplePosition(destination, out var hit, maxPathLength, NavMesh.AllAreas))
        {
            target = null;
            agent.SetDestination(hit.position);
            hasStopped = false;
            IsPathBlocked = false;

            // Start path update coroutine if auto-update is enabled
            if (autoUpdatePath && pathUpdateCoroutine == null)
                pathUpdateCoroutine = StartCoroutine(PathUpdateRoutine());

            return true;
        }

        Debug.LogWarning($"AiNavigation: Could not find valid NavMesh position near {destination}");
        return false;
    }

    /// <summary>
    ///     Sets the agent's movement speed
    /// </summary>
    public void SetSpeed(float multiplier)
    {
        if (!isInitialized) return;

        agent.speed = baseSpeed * Mathf.Max(0.1f, multiplier);
        OnSpeedChanged?.Invoke(agent.speed);
    }

    /// <summary>
    ///     Enables sprint mode
    /// </summary>
    public void EnableSprint(bool enable)
    {
        SetSpeed(enable ? sprintSpeedMultiplier : 1f);
    }

    /// <summary>
    ///     Stops the agent immediately
    /// </summary>
    public void Stop()
    {
        if (!isInitialized) return;

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
        if (!isInitialized) return;

        agent.isStopped = false;
        hasStopped = false;

        if (target != null) UpdatePath();
    }

    /// <summary>
    ///     Warps the agent to a new position
    /// </summary>
    public bool WarpToPosition(Vector3 position)
    {
        if (!isInitialized) return false;

        if (NavMesh.SamplePosition(position, out var hit, maxPathLength, NavMesh.AllAreas))
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
        if (!isInitialized) return false;

        if (NavMesh.SamplePosition(position, out var hit, maxPathLength, NavMesh.AllAreas))
        {
            var path = new NavMeshPath();
            if (NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, path))
                return path.status == NavMeshPathStatus.PathComplete;
        }

        return false;
    }

    /// <summary>
    ///     Gets the path to a position
    /// </summary>
    public NavMeshPath GetPathToPosition(Vector3 position)
    {
        var path = new NavMeshPath();

        if (NavMesh.SamplePosition(position, out var hit, maxPathLength, NavMesh.AllAreas))
            NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, path);

        return path;
    }

    #endregion

    #region Private Methods

    private void InitializeComponents()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (agent == null) Debug.LogError("AiNavigation: NavMeshAgent component is missing");

        if (animator == null) Debug.LogError("AiNavigation: Animator component is missing");
    }

    private void ConfigureAgent()
    {
        if (agent == null) return;

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
        if (!autoUpdatePath || target == null) return;

        if (Time.time >= lastPathUpdateTime + pathUpdateInterval)
        {
            if ((target.position - lastTargetPosition).sqrMagnitude > 0.01f) UpdatePath();
            lastPathUpdateTime = Time.time;
        }
    }

    private IEnumerator PathUpdateRoutine()
    {
        var wait = new WaitForSeconds(pathUpdateInterval);

        while (true)
        {
            if (target != null) UpdatePath();

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
        if (animator == null) return;

        // Calculate speed for animation
        var currentSpeed = agent.velocity.magnitude / agent.speed;
        currentSpeed = Mathf.SmoothDamp(animator.GetFloat(speedParameterName), currentSpeed, ref currentSpeedVelocity,
            animationDampTime);
        animator.SetFloat(speedParameterName, currentSpeed);

        // Update movement state
        animator.SetBool(isMovingParameterName, !hasStopped);
    }

    private void CheckPathStatus()
    {
        if (target == null) return;

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