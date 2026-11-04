using System;
using UnityEngine;
using UnityEngine.AI;

namespace Neo.NPC.Navigation
{
    /// <summary>
    ///     Pure C# follow/seek logic for a NavMeshAgent.
    /// </summary>
    public sealed class NpcFollowTargetCore
    {
        private readonly NavMeshAgent agent;

        private readonly Transform self;
        private bool autoUpdatePath;
        private bool destinationReachedFired;
        private Vector3? explicitDestination;
        private bool isStopped;
        private bool pathBlockedReported;
        private bool pathDirty = true;

        private Vector3 lastDesiredPosition;
        private NavMeshPathStatus lastPathStatus;

        private float lastPathUpdateTime;
        private float maxSampleDistance;
        private float pathUpdateInterval;
        private float pathUpdatePhase;

        private float triggerDistance;

        public NpcFollowTargetCore(Transform self, NavMeshAgent agent)
        {
            this.self = self;
            this.agent = agent;
        }

        public bool HasTarget => Target != null;
        public Transform Target { get; private set; }

        public bool FollowEnabled { get; private set; } = true;

        /// <summary>
        ///     Fraction of the path update interval (0..1) by which the first periodic re-path after a new
        ///     target or destination is advanced. Agents with different phases spread their periodic path
        ///     requests over the interval instead of re-pathing in the same frame. 0 keeps the legacy timing;
        ///     the immediate first request of a new target is never delayed.
        /// </summary>
        public float PathUpdatePhase
        {
            get => pathUpdatePhase;
            set => pathUpdatePhase = float.IsNaN(value) ? 0f : Mathf.Clamp(value, 0f, 0.999f);
        }

        public event Action<Vector3> DestinationReached;
        public event Action<Vector3> PathBlocked;
        public event Action<Vector3> PathUpdated;
        public event Action<NavMeshPathStatus> PathStatusChanged;
        public event Action<Vector3> DestinationUnreachable;

        public void Configure(float triggerDistance, bool autoUpdatePath, float pathUpdateInterval,
            float maxSampleDistance)
        {
            this.triggerDistance = Mathf.Max(0f, triggerDistance);
            this.autoUpdatePath = autoUpdatePath;
            this.pathUpdateInterval = Mathf.Max(0.05f, pathUpdateInterval);
            this.maxSampleDistance = Mathf.Max(1f, maxSampleDistance);
        }

        public void SetFollowEnabled(bool enabled)
        {
            FollowEnabled = enabled;
            if (!FollowEnabled)
            {
                explicitDestination = null;
            }
        }

        public void SetTarget(Transform newTarget)
        {
            Target = newTarget;
            explicitDestination = null;
            destinationReachedFired = false;
            pathBlockedReported = false;
            pathDirty = true;
            lastPathUpdateTime = float.NegativeInfinity;
        }

        public bool SetDestination(Vector3 destination)
        {
            Target = null;
            explicitDestination = destination;
            destinationReachedFired = false;
            pathBlockedReported = false;
            pathDirty = true;
            lastPathUpdateTime = float.NegativeInfinity;
            return true;
        }

        public void Stop()
        {
            isStopped = true;
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                return;
            }

            agent.isStopped = true;
        }

        public void Resume()
        {
            isStopped = false;
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                return;
            }

            agent.isStopped = false;
        }

        public void Tick(float time, Func<Vector3, Vector3> constrainPosition)
        {
            if (!FollowEnabled || isStopped)
            {
                return;
            }

            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                return;
            }

            HandleTriggerDistance();

            Vector3? desired = GetDesiredDestination();
            if (desired.HasValue)
            {
                Vector3 constrained = constrainPosition != null ? constrainPosition(desired.Value) : desired.Value;

                if (autoUpdatePath && time >= lastPathUpdateTime + pathUpdateInterval)
                {
                    bool firstPeriodicUpdate = float.IsNegativeInfinity(lastPathUpdateTime);
                    if (pathDirty || (!agent.pathPending && !agent.hasPath) ||
                        (constrained - lastDesiredPosition).sqrMagnitude > 0.001f)
                    {
                        UpdatePath(constrained);
                    }

                    // WHY: agents retargeted in the same frame would otherwise re-path in lockstep forever;
                    // shifting only the first period keeps the interval while desynchronizing the crowd.
                    lastPathUpdateTime = firstPeriodicUpdate ? time - pathUpdateInterval * pathUpdatePhase : time;
                }
            }

            CheckPathStatus();
            CheckDestinationReached();
        }

        private Vector3? GetDesiredDestination()
        {
            if (Target != null)
            {
                return Target.position;
            }

            return explicitDestination;
        }

        private void HandleTriggerDistance()
        {
            if (triggerDistance <= 0f || Target == null)
            {
                if (agent.isStopped)
                {
                    agent.isStopped = false;
                }

                return;
            }

            float dist = Vector3.Distance(self.position, Target.position);
            if (dist > triggerDistance)
            {
                agent.isStopped = true;
            }
            else
            {
                agent.isStopped = false;
            }
        }

        public bool TryForceUpdatePath(Func<Vector3, Vector3> constrainPosition)
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                return false;
            }

            Vector3? desired = GetDesiredDestination();
            if (!desired.HasValue)
            {
                return false;
            }

            Vector3 constrained = constrainPosition != null ? constrainPosition(desired.Value) : desired.Value;
            return UpdatePath(constrained);
        }

        private bool UpdatePath(Vector3 desired)
        {
            if (NpcDestinationResolver.TryResolve(desired, maxSampleDistance, agent.areaMask, out Vector3 resolved))
            {
                if (agent.SetDestination(resolved))
                {
                    lastDesiredPosition = desired;
                    pathDirty = false;
                    destinationReachedFired = false;
                    pathBlockedReported = false;
                    PathUpdated?.Invoke(resolved);
                    return true;
                }
            }

            DestinationUnreachable?.Invoke(desired);
            return false;
        }

        private void CheckPathStatus()
        {
            if (agent.pathPending || !agent.hasPath)
            {
                return;
            }

            if (lastPathStatus != agent.pathStatus)
            {
                lastPathStatus = agent.pathStatus;
                PathStatusChanged?.Invoke(lastPathStatus);
            }

            if (agent.pathStatus is NavMeshPathStatus.PathInvalid or NavMeshPathStatus.PathPartial)
            {
                // WHY: report each blocked path once; a partial path otherwise re-fired every Tick (every frame).
                if (pathBlockedReported)
                {
                    return;
                }

                Vector3? desired = GetDesiredDestination();
                if (desired.HasValue)
                {
                    pathBlockedReported = true;
                    PathBlocked?.Invoke(desired.Value);
                }
            }
            else
            {
                pathBlockedReported = false;
            }
        }

        private void CheckDestinationReached()
        {
            if (destinationReachedFired)
            {
                return;
            }

            if (agent.pathPending || !agent.hasPath || agent.pathStatus != NavMeshPathStatus.PathComplete)
            {
                return;
            }

            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                destinationReachedFired = true;
                Vector3? desired = GetDesiredDestination();
                if (desired.HasValue)
                {
                    DestinationReached?.Invoke(desired.Value);
                }
            }
        }
    }
}
