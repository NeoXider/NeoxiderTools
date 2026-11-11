using System;
using Neo.NPC.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Neo.NPC
{
    /// <summary>
    ///     NPC navigation controller with switchable behaviours.
    /// </summary>
    [DisallowMultipleComponent]
    [NeoDoc("NPC/Navigation/NPCNavigation.md")]
    [RequireComponent(typeof(NavMeshAgent))]
    [CreateFromMenu("Neoxider/NPC/NpcNavigation", "Prefabs/Tools/AI/Npc.prefab")]
    [AddComponentMenu("Neoxider/NPC/" + nameof(NpcNavigation))]
    public sealed partial class NpcNavigation : MonoBehaviour
    {
        /// <summary>Behaviour selected by <see cref="SetMode"/>.</summary>
        public enum NavigationMode
        {
            /// <summary>Paths toward <see cref="FollowTarget"/> (or an explicit <see cref="SetDestination"/> point).</summary>
            FollowTarget,

            /// <summary>Walks the patrol points in order, or random points inside the patrol zone.</summary>
            Patrol,

            /// <summary>Patrols until <see cref="CombinedTarget"/> enters aggro range, then chases it.</summary>
            Combined
        }

        /// <summary>Who rotates the NPC while it moves.</summary>
        public enum RotationPolicy
        {
            /// <summary>NavMeshAgent rotation (optionally disabled with Update Rotation).</summary>
            Agent,

            /// <summary>The component turns toward the agent velocity at Turn Speed.</summary>
            ManualVelocity
        }

        [Header("NPC")] [SerializeField] private bool isActive = true;

        [SerializeField] private Animator animator;

        [Header("Mode")] [SerializeField] private NavigationMode mode = NavigationMode.FollowTarget;

        [Header("Agent")] [SerializeField] private RotationPolicy rotationPolicy = RotationPolicy.Agent;

        [Tooltip("Used only when RotationPolicy = Agent.")] [SerializeField]
        private bool updateRotation = true;

        [Min(0.1f)] [SerializeField] private float walkSpeed = 3f;
        [Min(0.1f)] [SerializeField] private float runSpeed = 6f;
        [Min(0.1f)] [SerializeField] private float acceleration = 8f;
        [Min(1f)] [SerializeField] private float turnSpeed = 260f;
        [Min(0.01f)] [SerializeField] private float stoppingDistance = 2f;
        [Min(0.01f)] [SerializeField] private float agentRadius = 0.5f;
        [SerializeField] private int areaMask = NavMesh.AllAreas;

        [Header("Path")]
        [Tooltip("Re-path toward a moving target every Path Update Interval. When off, a path is requested only on target/mode changes and by RefreshPath().")]
        [SerializeField]
        private bool autoUpdatePath = true;

        [Tooltip("Seconds between automatic re-paths. A failed Combined chase is also retried only after one interval.")]
        [Min(0.05f)]
        [SerializeField]
        private float pathUpdateInterval = 0.5f;

        [Min(0.1f)] [SerializeField] private float maxSampleDistance = 100f;

        [Tooltip("Spread the periodic re-path of many NPCs over Path Update Interval instead of running them in the same frame.")]
        [SerializeField]
        private bool staggerPathUpdates = true;

        [Header("Follow")] [SerializeField] private Transform followTarget;
        [Min(0f)] [SerializeField] private float triggerDistance;

        [Tooltip("Optional movement bounds. If set, NPC won't move outside.")] [SerializeField]
        private BoxCollider followMovementBounds;

        [Header("Patrol")] [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private BoxCollider patrolZone;
        [Min(0f)] [SerializeField] private float patrolWaitTime = 1f;
        [SerializeField] private bool loopPatrol = true;

        [Header("Combined")] [SerializeField] private Transform combinedTarget;
        [Min(0f)] [SerializeField] private float aggroDistance = 10f;
        [Min(0f)] [SerializeField] private float maxFollowDistance = 20f;

        public UnityEvent onMovementStarted = new();
        public UnityEvent onMovementStopped = new();
        public UnityEvent<NavigationMode> onModeChanged = new();
        public UnityEvent<Transform> onTargetChanged = new();

        public UnityEvent<Vector3> onDestinationReached = new();
        public UnityEvent<Vector3> onPathBlocked = new();
        public UnityEvent<Vector3> onPathUpdated = new();
        public UnityEvent<NavMeshPathStatus> onPathStatusChanged = new();

        public UnityEvent onPatrolStarted = new();
        public UnityEvent onPatrolCompleted = new();
        public UnityEvent<int> onPatrolPointReached = new();

        public UnityEvent onStartFollowing = new();
        public UnityEvent onStopFollowing = new();

        public UnityEvent<float> onSpeedChanged = new();

        [Header("Debug")] [SerializeField] private bool debugMode;
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private bool drawPathGizmos = true;

        [TextArea(2, 6)] [SerializeField] private string lastDecision;

        private NavMeshAgent agent;

        private NpcNavAgentCore agentCore;
        private CombinedBehaviour combinedBehaviour;

        private INavigationBehaviour currentBehaviour;
        private FollowBehaviour followBehaviour;
        private NpcFollowTargetCore followCore;
        private bool isCombinedFollowing;
        private PatrolBehaviour patrolBehaviour;
        private NpcPatrolCore patrolCore;

        private bool wasMoving;
        private bool initialized;

        private static int staggerSequence;
        private float staggerPhase;
        private float combinedRetryTime = float.NegativeInfinity;
        private Func<Vector3, Vector3> constrainPosition;

        private Func<Vector3, Vector3> ConstrainPositionFunc => constrainPosition ??= ConstrainPosition;

        private bool RecordsDecisions => debugMode || Application.isEditor;

        // WHY: the stagger sequence only spreads phases; restart it per play session when domain reload is off.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            staggerSequence = 0;
        }

        /// <summary>Movement switch: false stops the agent and skips behaviour ticks; same as <see cref="Stop"/> / <see cref="Resume"/>.</summary>
        public bool IsActive
        {
            get => isActive;
            set
            {
                if (value)
                {
                    Resume();
                }
                else
                {
                    Stop();
                }
            }
        }

        /// <summary>Currently selected behaviour.</summary>
        public NavigationMode Mode => mode;

        /// <summary>True after <see cref="SetRunning"/>(true): the agent uses the run speed.</summary>
        public bool IsRunning => agentCore != null && agentCore.IsRunning;

        /// <summary>
        ///     Re-paths toward a moving target every Path Update Interval. When false, a path is requested only when
        ///     the target or mode changes and by <see cref="RefreshPath"/>; a running follow picks the change up at once.
        /// </summary>
        public bool AutoUpdatePath
        {
            get => autoUpdatePath;
            set
            {
                autoUpdatePath = value;
                followCore?.Configure(triggerDistance, autoUpdatePath, pathUpdateInterval, maxSampleDistance);
            }
        }

        /// <summary>Target used in FollowTarget mode.</summary>
        public Transform FollowTarget => followTarget;

        /// <summary>Target used in Combined mode.</summary>
        public Transform CombinedTarget => combinedTarget;

        /// <summary>True while the NPC is actively chasing its target in Combined mode.</summary>
        public bool IsChasing => isCombinedFollowing;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();

            agentCore ??= new NpcNavAgentCore(agent);
            ApplyAgentSettings();

            followCore = new NpcFollowTargetCore(transform, agent);
            followCore.Configure(triggerDistance, autoUpdatePath, pathUpdateInterval, maxSampleDistance);
            staggerPhase = NextStaggerPhase();
            followCore.PathUpdatePhase = staggerPathUpdates ? staggerPhase : 0f;
            followCore.DestinationReached += p => onDestinationReached?.Invoke(p);
            followCore.PathBlocked += p => onPathBlocked?.Invoke(p);
            followCore.PathUpdated += p => onPathUpdated?.Invoke(p);
            followCore.PathStatusChanged += s => onPathStatusChanged?.Invoke(s);
            followCore.DestinationUnreachable += OnFollowDestinationUnreachable;

            patrolCore = new NpcPatrolCore(transform, agent);
            patrolCore.Configure(patrolPoints, patrolZone, patrolWaitTime, loopPatrol, maxSampleDistance);
            patrolCore.PatrolStarted += () => onPatrolStarted?.Invoke();
            patrolCore.PatrolCompleted += () => onPatrolCompleted?.Invoke();
            patrolCore.PatrolPointReached += i => onPatrolPointReached?.Invoke(i);
            patrolCore.DestinationUnreachable += OnPatrolDestinationUnreachable;

            followBehaviour = new FollowBehaviour(this);
            patrolBehaviour = new PatrolBehaviour(this);
            combinedBehaviour = new CombinedBehaviour(this);

            // WHY: only select the behaviour here; the first Enter() is driven by OnEnable
            // so events (onPatrolStarted, onTargetChanged, ...) fire exactly once on spawn.
            NavigationMode initialMode = mode;
            SetModeInternal(initialMode, true, false);
            // WHY: startup listeners can defer another mode while initialized is still false.
            // Reconcile that selection without entering it or notifying listeners again.
            if (mode != initialMode)
            {
                SetModeInternal(mode, false, false);
            }

            initialized = true;
        }

        private void OnEnable()
        {
            if (!initialized)
            {
                return;
            }

            currentBehaviour?.Enter();

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = !isActive;
            }
        }

        private void Update()
        {
            if (!isActive)
            {
                Stop();
                return;
            }

            float dt = Time.deltaTime;
            float time = Time.time;

            currentBehaviour?.Tick(dt, time);
            UpdateRotation(dt);
            UpdateMovementEvents();
        }

        private void OnDisable()
        {
            currentBehaviour?.Exit();
            isCombinedFollowing = false;
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }

            UpdateMovementEvents();
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos)
            {
                return;
            }

            NavMeshAgent a = agent != null ? agent : GetComponent<NavMeshAgent>();
            if (a != null && drawPathGizmos && a.hasPath)
            {
                Gizmos.color = Color.yellow;
                NavMeshPath path = a.path;
                Vector3 prev = transform.position;
                foreach (Vector3 c in path.corners)
                {
                    Gizmos.DrawLine(prev, c);
                    prev = c;
                }
            }

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, stoppingDistance);

            if (mode == NavigationMode.FollowTarget && triggerDistance > 0f)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(transform.position, triggerDistance);
            }

            if (mode == NavigationMode.Combined)
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
            if (!drawGizmos)
            {
                return;
            }

            if (followMovementBounds != null)
            {
                DrawBoxCollider(followMovementBounds, new Color(0.2f, 0.6f, 1f, 0.2f));
            }

            if (patrolZone != null)
            {
                DrawBoxCollider(patrolZone, new Color(0.2f, 1f, 0.2f, 0.2f));
            }
        }

        private void OnValidate()
        {
            triggerDistance = Mathf.Max(0f, triggerDistance);
            walkSpeed = Mathf.Max(0.1f, walkSpeed);
            runSpeed = Mathf.Max(0.1f, runSpeed);
            acceleration = Mathf.Max(0.1f, acceleration);
            turnSpeed = Mathf.Max(1f, turnSpeed);
            stoppingDistance = Mathf.Max(0.01f, stoppingDistance);
            agentRadius = Mathf.Max(0.01f, agentRadius);
            pathUpdateInterval = Mathf.Max(0.05f, pathUpdateInterval);
            maxSampleDistance = Mathf.Max(0.1f, maxSampleDistance);
            patrolWaitTime = Mathf.Max(0f, patrolWaitTime);
            aggroDistance = Mathf.Max(0f, aggroDistance);
            maxFollowDistance = Mathf.Max(0f, maxFollowDistance);

            if (!Application.isPlaying)
            {
                return;
            }

            ApplyAgentSettings();

            followCore?.Configure(triggerDistance, autoUpdatePath, pathUpdateInterval, maxSampleDistance);
            if (followCore != null)
            {
                followCore.PathUpdatePhase = staggerPathUpdates ? staggerPhase : 0f;
            }

            patrolCore?.Configure(patrolPoints, patrolZone, patrolWaitTime, loopPatrol, maxSampleDistance);
        }

        /// <summary>Switches the behaviour; safe before Awake (applied on enable) and a no-op for the current mode.</summary>
        public void SetMode(NavigationMode newMode)
        {
            if (!initialized)
            {
                mode = newMode;
                return;
            }

            if (mode == newMode)
            {
                return;
            }

            SetModeInternal(newMode, true, isActiveAndEnabled);
        }

        /// <summary>
        ///     Sets the FollowTarget-mode target. In FollowTarget mode it raises <see cref="onTargetChanged"/> and
        ///     requests a path immediately, even for the same target; skip the call when nothing changed.
        /// </summary>
        public void SetFollowTarget(Transform target)
        {
            followTarget = target;
            if (mode == NavigationMode.FollowTarget && followCore != null)
            {
                followCore.SetTarget(target);
                onTargetChanged?.Invoke(target);
                followCore.TryForceUpdatePath(ConstrainPositionFunc);
            }
        }

        /// <summary>Sets the Combined-mode target; a chase in progress follows the new target.</summary>
        public void SetCombinedTarget(Transform target)
        {
            combinedTarget = target;
            combinedRetryTime = float.NegativeInfinity;
            if (mode == NavigationMode.Combined)
            {
                if (isCombinedFollowing)
                {
                    followCore.SetTarget(target);
                }

                onTargetChanged?.Invoke(target);
            }
        }

        /// <summary>Switches between walk and run speed and raises <see cref="onSpeedChanged"/>.</summary>
        public void SetRunning(bool enable)
        {
            if (agentCore == null)
            {
                ApplyAgentSettings();
            }

            agentCore.SetRunning(enable);
            onSpeedChanged?.Invoke(agent != null ? agent.speed : 0f);
        }

        /// <summary>Applies a custom agent speed (minimum 0.1) until the next <see cref="SetRunning"/> call.</summary>
        public void SetSpeed(float speed)
        {
            if (agentCore == null)
            {
                ApplyAgentSettings();
            }

            agentCore.SetSpeed(speed);
            onSpeedChanged?.Invoke(agent != null ? agent.speed : 0f);
        }

        /// <summary>
        ///     Paths to an explicit point through the follow core (clears the follow target). Returns false before
        ///     initialization, off the NavMesh, or when the sampled destination is rejected.
        /// </summary>
        public bool SetDestination(Vector3 destination)
        {
            if (followCore == null)
            {
                return false;
            }

            followCore.SetDestination(destination);
            return followCore.TryForceUpdatePath(ConstrainPositionFunc);
        }

        /// <summary>
        ///     Requests a new path to the current follow target (or explicit destination) without raising
        ///     <see cref="onTargetChanged"/>. Use it to track a moving target while <see cref="AutoUpdatePath"/> is off.
        ///     Returns false while nothing is followed (Patrol, Combined before a chase), before initialization, off
        ///     the NavMesh, or when the destination cannot be resolved; an unresolved Combined chase returns to patrol.
        /// </summary>
        public bool RefreshPath()
        {
            if (followCore == null || !followCore.FollowEnabled)
            {
                return false;
            }

            return followCore.TryForceUpdatePath(ConstrainPositionFunc);
        }

        /// <summary>Stops the agent and pauses behaviour ticks until <see cref="Resume"/>.</summary>
        [Button(PlayModeOnly = true)]
        public void Stop()
        {
            isActive = false;
            followCore?.Stop();
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }

            UpdateMovementEvents();
        }

        /// <summary>Resumes movement and behaviour ticks after <see cref="Stop"/>.</summary>
        [Button(PlayModeOnly = true)]
        public void Resume()
        {
            isActive = true;
            followCore?.Resume();
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
            }
        }

        private void SetModeInternal(NavigationMode newMode, bool invokeEvent, bool enter = true)
        {
            currentBehaviour?.Exit();
            isCombinedFollowing = false;

            mode = newMode;
            currentBehaviour = mode switch
            {
                NavigationMode.FollowTarget => followBehaviour,
                NavigationMode.Patrol => patrolBehaviour,
                NavigationMode.Combined => combinedBehaviour,
                _ => followBehaviour
            };

            if (enter)
            {
                currentBehaviour.Enter();
                if (!isActive)
                {
                    Stop();
                }
            }

            if (invokeEvent)
            {
                onModeChanged?.Invoke(mode);
            }

            if (RecordsDecisions)
            {
                LogDecision($"Mode -> {mode}");
            }
        }

        private void ApplyAgentSettings()
        {
            // WHY: in Play Mode OnValidate may run before Awake (e.g. domain reload);
            // guard against uninitialized null.
            if (agentCore == null)
            {
                if (agent == null)
                {
                    agent = GetComponent<NavMeshAgent>();
                }

                if (agent == null)
                {
                    return;
                }

                agentCore = new NpcNavAgentCore(agent);
            }

            agentCore.Configure(
                walkSpeed,
                runSpeed,
                acceleration,
                turnSpeed,
                stoppingDistance,
                rotationPolicy == RotationPolicy.Agent && updateRotation,
                agentRadius,
                areaMask
            );
        }

        private Vector3 ConstrainPosition(Vector3 desired)
        {
            if (followMovementBounds == null)
            {
                return desired;
            }

            Transform t = followMovementBounds.transform;
            Vector3 local = t.InverseTransformPoint(desired);

            Vector3 center = followMovementBounds.center;
            Vector3 half = followMovementBounds.size * 0.5f;

            Vector3 clamped = new(
                Mathf.Clamp(local.x, center.x - half.x, center.x + half.x),
                Mathf.Clamp(local.y, center.y - half.y, center.y + half.y),
                Mathf.Clamp(local.z, center.z - half.z, center.z + half.z)
            );

            return t.TransformPoint(clamped);
        }

        private void UpdateMovementEvents()
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                wasMoving = false;
                return;
            }

            float velSqr = agent.velocity.sqrMagnitude;
            bool isMovingNow = !agent.isStopped && velSqr > 0.0004f;

            if (isMovingNow && !wasMoving)
            {
                onMovementStarted?.Invoke();
            }
            else if (!isMovingNow && wasMoving)
            {
                onMovementStopped?.Invoke();
            }

            wasMoving = isMovingNow;
        }

        private void UpdateRotation(float deltaTime)
        {
            if (rotationPolicy != RotationPolicy.ManualVelocity)
            {
                return;
            }

            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                return;
            }

            Vector3 dir = agent.desiredVelocity.sqrMagnitude > 0.0004f ? agent.desiredVelocity : agent.velocity;
            if (dir.sqrMagnitude < 0.0004f)
            {
                return;
            }

            Quaternion target = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * deltaTime);
        }

        private void OnFollowDestinationUnreachable(Vector3 desired)
        {
            if (mode == NavigationMode.Combined && isCombinedFollowing)
            {
                isCombinedFollowing = false;
                // WHY: without a cooldown an unreachable target inside aggro range restarts the chase every
                // frame, repeating the bounded NavMesh ring search and the start/stop events per frame.
                combinedRetryTime = Time.time + pathUpdateInterval;
                followCore.SetFollowEnabled(false);
                patrolCore.StartPatrol(false);
                onStopFollowing?.Invoke();
                if (RecordsDecisions)
                {
                    LogDecision($"Follow unreachable -> return to patrol ({desired})");
                }

                return;
            }

            if (RecordsDecisions)
            {
                LogDecision($"Destination unreachable ({desired})");
            }
        }

        private void OnPatrolDestinationUnreachable(Vector3 desired)
        {
            if (RecordsDecisions)
            {
                LogDecision($"Patrol unreachable -> advance ({desired})");
            }
        }

        private static float NextStaggerPhase()
        {
            // WHY: a golden-ratio sequence spreads consecutive spawns evenly without touching UnityEngine.Random.
            staggerSequence++;
            double phase = staggerSequence * 0.6180339887498949d;
            return (float)(phase - Math.Floor(phase));
        }

        private void LogDecision(string message)
        {
            if (!RecordsDecisions)
            {
                return;
            }

            string line = $"[{Time.time:0.00}] {message}";
            lastDecision = line;

            if (debugMode)
            {
                NeoDiagnostics.Log(line, this);
            }
        }

        private static void DrawBoxCollider(BoxCollider box, Color fill)
        {
            Matrix4x4 old = Gizmos.matrix;
            Gizmos.matrix = box.transform.localToWorldMatrix;

            Gizmos.color = fill;
            Gizmos.DrawCube(box.center, box.size);

            Gizmos.color = new Color(fill.r, fill.g, fill.b, 1f);
            Gizmos.DrawWireCube(box.center, box.size);

            Gizmos.matrix = old;
        }
    }
}
