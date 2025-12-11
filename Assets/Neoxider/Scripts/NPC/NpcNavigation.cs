using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Neo.NPC
{
    /// <summary>
    /// NPC navigation controller with switchable behaviours.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    [AddComponentMenu("Neo/NPC/" + nameof(NpcNavigation))]
    public sealed class NpcNavigation : MonoBehaviour
    {
        public enum NavigationMode
        {
            FollowTarget,
            Patrol,
            Combined
        }

        public enum RotationPolicy
        {
            Agent,
            ManualVelocity
        }

        [Header("NPC")]
        [SerializeField] private bool isActive = true;

        [SerializeField] private Animator animator;

        [Header("Mode")]
        [SerializeField] private NavigationMode mode = NavigationMode.FollowTarget;

        [Header("Agent")]
        [SerializeField] private RotationPolicy rotationPolicy = RotationPolicy.Agent;

        [Tooltip("Used only when RotationPolicy = Agent.")]
        [SerializeField]
        private bool updateRotation = true;
        [Min(0.1f)]
        [SerializeField] private float walkSpeed = 3f;
        [Min(0.1f)]
        [SerializeField] private float runSpeed = 6f;
        [Min(0.1f)]
        [SerializeField] private float acceleration = 8f;
        [Min(1f)]
        [SerializeField] private float turnSpeed = 260f;
        [Min(0.01f)]
        [SerializeField] private float stoppingDistance = 2f;
        [Min(0.01f)]
        [SerializeField] private float agentRadius = 0.5f;
        [SerializeField] private int areaMask = NavMesh.AllAreas;

        [Header("Path")]
        [SerializeField] private bool autoUpdatePath = true;
        [Min(0.05f)]
        [SerializeField] private float pathUpdateInterval = 0.5f;
        [Min(0.1f)]
        [SerializeField] private float maxSampleDistance = 100f;

        [Header("Follow")]
        [SerializeField] private Transform followTarget;
        [Min(0f)]
        [SerializeField] private float triggerDistance;
        [Tooltip("Optional movement bounds. If set, NPC won't move outside.")]
        [SerializeField] private BoxCollider followMovementBounds;

        [Header("Patrol")]
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private BoxCollider patrolZone;
        [Min(0f)]
        [SerializeField] private float patrolWaitTime = 1f;
        [SerializeField] private bool loopPatrol = true;

        [Header("Combined")]
        [SerializeField] private Transform combinedTarget;
        [Min(0f)]
        [SerializeField] private float aggroDistance = 10f;
        [Min(0f)]
        [SerializeField] private float maxFollowDistance = 20f;

        [Header("Events")]
        public UnityEvent onMovementStarted;
        public UnityEvent onMovementStopped;
        public UnityEvent<NavigationMode> onModeChanged;
        public UnityEvent<Transform> onTargetChanged;

        public UnityEvent<Vector3> onDestinationReached;
        public UnityEvent<Vector3> onPathBlocked;
        public UnityEvent<Vector3> onPathUpdated;
        public UnityEvent<NavMeshPathStatus> onPathStatusChanged;

        public UnityEvent onPatrolStarted;
        public UnityEvent onPatrolCompleted;
        public UnityEvent<int> onPatrolPointReached;

        public UnityEvent onStartFollowing;
        public UnityEvent onStopFollowing;

        public UnityEvent<float> onSpeedChanged;

        [Header("Debug")]
        [SerializeField] private bool debugMode;
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private bool drawPathGizmos = true;

        [TextArea(2, 6)] [SerializeField] private string lastDecision;

        private NavMeshAgent agent;

        private Neo.NPC.Navigation.NpcNavAgentCore agentCore;
        private Neo.NPC.Navigation.NpcFollowTargetCore followCore;
        private Neo.NPC.Navigation.NpcPatrolCore patrolCore;

        private INavigationBehaviour currentBehaviour;
        private FollowBehaviour followBehaviour;
        private PatrolBehaviour patrolBehaviour;
        private CombinedBehaviour combinedBehaviour;

        private bool wasMoving;
        private bool isCombinedFollowing;

        public bool IsActive
        {
            get => isActive;
            set => isActive = value;
        }

        public NavigationMode Mode => mode;

        private interface INavigationBehaviour
        {
            void Enter();
            void Exit();
            void Tick(float deltaTime, float time);
        }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();

            agentCore = new Neo.NPC.Navigation.NpcNavAgentCore(agent);
            ApplyAgentSettings();

            followCore = new Neo.NPC.Navigation.NpcFollowTargetCore(transform, agent);
            followCore.Configure(triggerDistance, autoUpdatePath, pathUpdateInterval, maxSampleDistance);
            followCore.DestinationReached += p => onDestinationReached?.Invoke(p);
            followCore.PathBlocked += p => onPathBlocked?.Invoke(p);
            followCore.PathUpdated += p => onPathUpdated?.Invoke(p);
            followCore.PathStatusChanged += s => onPathStatusChanged?.Invoke(s);
            followCore.DestinationUnreachable += OnFollowDestinationUnreachable;

            patrolCore = new Neo.NPC.Navigation.NpcPatrolCore(transform, agent);
            patrolCore.Configure(patrolPoints, patrolZone, patrolWaitTime, loopPatrol, maxSampleDistance);
            patrolCore.PatrolStarted += () => onPatrolStarted?.Invoke();
            patrolCore.PatrolCompleted += () => onPatrolCompleted?.Invoke();
            patrolCore.PatrolPointReached += i => onPatrolPointReached?.Invoke(i);
            patrolCore.DestinationUnreachable += OnPatrolDestinationUnreachable;

            followBehaviour = new FollowBehaviour(this);
            patrolBehaviour = new PatrolBehaviour(this);
            combinedBehaviour = new CombinedBehaviour(this);

            SetModeInternal(mode, invokeEvent: true);
        }

        private void OnDisable()
        {
            currentBehaviour?.Exit();
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
            patrolCore?.Configure(patrolPoints, patrolZone, patrolWaitTime, loopPatrol, maxSampleDistance);
        }

        private void Update()
        {
            if (!isActive)
            {
                if (agent != null)
                {
                    agent.isStopped = true;
                }

                return;
            }

            float dt = Time.deltaTime;
            float time = Time.time;

            currentBehaviour?.Tick(dt, time);
            UpdateRotation(dt);
            UpdateMovementEvents();
        }

        public void SetMode(NavigationMode newMode)
        {
            if (mode == newMode)
            {
                return;
            }

            SetModeInternal(newMode, invokeEvent: true);
        }

        public void SetFollowTarget(Transform target)
        {
            followTarget = target;
            if (mode == NavigationMode.FollowTarget)
            {
                onTargetChanged?.Invoke(target);
                followCore.SetTarget(target);
                followCore.TryForceUpdatePath(ConstrainPosition);
            }
        }

        public void SetCombinedTarget(Transform target)
        {
            combinedTarget = target;
            if (mode == NavigationMode.Combined)
            {
                onTargetChanged?.Invoke(target);
            }
        }

        public void SetRunning(bool enable)
        {
            agentCore.SetRunning(enable);
            onSpeedChanged?.Invoke(agent != null ? agent.speed : 0f);
        }

        public void SetSpeed(float speed)
        {
            agentCore.SetSpeed(speed);
            onSpeedChanged?.Invoke(agent != null ? agent.speed : 0f);
        }

        public bool SetDestination(Vector3 destination)
        {
            followCore.SetDestination(destination);
            return followCore.TryForceUpdatePath(ConstrainPosition);
        }

        public void Stop()
        {
            followCore.Stop();
            agent.isStopped = true;
        }

        public void Resume()
        {
            agent.isStopped = false;
        }

        private void SetModeInternal(NavigationMode newMode, bool invokeEvent)
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

            currentBehaviour.Enter();

            if (invokeEvent)
            {
                onModeChanged?.Invoke(mode);
            }

            LogDecision($"Mode -> {mode}");
        }

        private void ApplyAgentSettings()
        {
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
                followCore.SetFollowEnabled(false);
                patrolCore.StartPatrol(resetIndex: false);
                onStopFollowing?.Invoke();
                LogDecision($"Follow unreachable -> return to patrol ({desired})");
                return;
            }

            LogDecision($"Destination unreachable ({desired})");
        }

        private void OnPatrolDestinationUnreachable(Vector3 desired)
        {
            LogDecision($"Patrol unreachable -> advance ({desired})");
        }

        private void LogDecision(string message)
        {
            string line = $"[{Time.time:0.00}] {message}";
            lastDecision = line;

            if (debugMode)
            {
                Debug.Log(line, this);
            }
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

        private sealed class FollowBehaviour : INavigationBehaviour
        {
            private readonly NpcNavigation host;

            public FollowBehaviour(NpcNavigation host)
            {
                this.host = host;
            }

            public void Enter()
            {
                host.followCore.Configure(host.triggerDistance, host.autoUpdatePath, host.pathUpdateInterval, host.maxSampleDistance);
                host.followCore.SetFollowEnabled(true);
                host.followCore.SetTarget(host.followTarget);
                host.onTargetChanged?.Invoke(host.followTarget);
                host.followCore.TryForceUpdatePath(host.ConstrainPosition);
            }

            public void Exit()
            {
                host.followCore.SetFollowEnabled(false);
            }

            public void Tick(float deltaTime, float time)
            {
                host.followCore.Tick(time, host.ConstrainPosition);
            }
        }

        private sealed class PatrolBehaviour : INavigationBehaviour
        {
            private readonly NpcNavigation host;

            public PatrolBehaviour(NpcNavigation host)
            {
                this.host = host;
            }

            public void Enter()
            {
                host.followCore.SetFollowEnabled(false);
                host.patrolCore.Configure(host.patrolPoints, host.patrolZone, host.patrolWaitTime, host.loopPatrol, host.maxSampleDistance);
                host.patrolCore.StartPatrol(resetIndex: true);
            }

            public void Exit()
            {
                host.patrolCore.StopPatrol(stopAgent: false);
            }

            public void Tick(float deltaTime, float time)
            {
                host.patrolCore.Tick(time);
            }
        }

        private sealed class CombinedBehaviour : INavigationBehaviour
        {
            private readonly NpcNavigation host;
            private bool isFollowing;

            public CombinedBehaviour(NpcNavigation host)
            {
                this.host = host;
            }

            public void Enter()
            {
                isFollowing = false;

                host.followCore.Configure(host.triggerDistance, host.autoUpdatePath, host.pathUpdateInterval, host.maxSampleDistance);
                host.followCore.SetFollowEnabled(false);

                host.patrolCore.Configure(host.patrolPoints, host.patrolZone, host.patrolWaitTime, host.loopPatrol, host.maxSampleDistance);
                host.patrolCore.StartPatrol(resetIndex: false);

                host.onTargetChanged?.Invoke(host.combinedTarget);
            }

            public void Exit()
            {
                host.followCore.SetFollowEnabled(false);
                host.patrolCore.StopPatrol(stopAgent: false);
                isFollowing = false;
            }

            public void Tick(float deltaTime, float time)
            {
                if (host.combinedTarget == null || host.aggroDistance <= 0f)
                {
                    host.patrolCore.Tick(time);
                    return;
                }

                float distSqr = (host.transform.position - host.combinedTarget.position).sqrMagnitude;
                float aggroSqr = host.aggroDistance * host.aggroDistance;
                float deaggroSqr = host.maxFollowDistance * host.maxFollowDistance;

                bool shouldFollow = distSqr <= aggroSqr;
                bool shouldStop = host.maxFollowDistance > 0f && distSqr > deaggroSqr;

                if (shouldFollow && !isFollowing)
                {
                    isFollowing = true;
                    host.isCombinedFollowing = true;
                    host.patrolCore.StopPatrol(stopAgent: false);
                    host.followCore.SetTarget(host.combinedTarget);
                    host.followCore.SetFollowEnabled(true);
                    host.onStartFollowing?.Invoke();
                    host.LogDecision("Combined -> start following");
                }
                else if (shouldStop && isFollowing)
                {
                    isFollowing = false;
                    host.isCombinedFollowing = false;
                    host.followCore.SetFollowEnabled(false);
                    host.patrolCore.StartPatrol(resetIndex: false);
                    host.onStopFollowing?.Invoke();
                    host.LogDecision("Combined -> stop following");
                }

                if (isFollowing)
                {
                    host.followCore.Tick(time, host.ConstrainPosition);
                }
                else
                {
                    host.patrolCore.Tick(time);
                }
            }
        }
    }
}

