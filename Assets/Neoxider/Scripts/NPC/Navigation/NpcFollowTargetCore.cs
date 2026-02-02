using System;
using UnityEngine;
using UnityEngine.AI;

namespace Neo.NPC.Navigation
{
    /// <summary>
    /// Pure C# follow/seek logic for a NavMeshAgent.
    /// </summary>
    public sealed class NpcFollowTargetCore
    {
        public event Action<Vector3> DestinationReached;
        public event Action<Vector3> PathBlocked;
        public event Action<Vector3> PathUpdated;
        public event Action<NavMeshPathStatus> PathStatusChanged;
        public event Action<Vector3> DestinationUnreachable;

        private readonly Transform self;
        private readonly NavMeshAgent agent;

        private float triggerDistance;
        private bool autoUpdatePath;
        private float pathUpdateInterval;
        private float maxSampleDistance;

        private bool followEnabled = true;

        private Transform target;
        private Vector3? explicitDestination;

        private float lastPathUpdateTime;
        private Vector3 lastDesiredPosition;
        private NavMeshPathStatus lastPathStatus;
        private bool destinationReachedFired;

        public bool HasTarget => target != null;
        public Transform Target => target;

        public bool FollowEnabled => followEnabled;

        public NpcFollowTargetCore(Transform self, NavMeshAgent agent)
        {
            this.self = self;
            this.agent = agent;
        }

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
            followEnabled = enabled;
            if (!followEnabled)
            {
                explicitDestination = null;
            }
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            explicitDestination = null;
            destinationReachedFired = false;
        }

        public bool SetDestination(Vector3 destination)
        {
            target = null;
            explicitDestination = destination;
            destinationReachedFired = false;
            return true;
        }

        public void Stop()
        {
            if (agent == null)
            {
                return;
            }

            agent.isStopped = true;
        }

        public void Resume()
        {
            if (agent == null)
            {
                return;
            }

            agent.isStopped = false;
        }

        public void Tick(float time, Func<Vector3, Vector3> constrainPosition)
        {
            if (!followEnabled)
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
                    if ((constrained - lastDesiredPosition).sqrMagnitude > 0.001f)
                    {
                        UpdatePath(constrained);
                    }

                    lastPathUpdateTime = time;
                }
            }

            CheckPathStatus();
            CheckDestinationReached();
        }

        private Vector3? GetDesiredDestination()
        {
            if (target != null)
            {
                return target.position;
            }

            return explicitDestination;
        }

        private void HandleTriggerDistance()
        {
            if (triggerDistance <= 0f || target == null)
            {
                if (agent.isStopped)
                {
                    agent.isStopped = false;
                }

                return;
            }

            float dist = Vector3.Distance(self.position, target.position);
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
                agent.SetDestination(resolved);
                lastDesiredPosition = desired;
                destinationReachedFired = false;
                PathUpdated?.Invoke(resolved);
                return true;
            }

            DestinationUnreachable?.Invoke(desired);
            return false;
        }

        private void CheckPathStatus()
        {
            if (!agent.hasPath)
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
                Vector3? desired = GetDesiredDestination();
                if (desired.HasValue)
                {
                    PathBlocked?.Invoke(desired.Value);
                }
            }
        }

        private void CheckDestinationReached()
        {
            if (destinationReachedFired)
            {
                return;
            }

            if (agent.pathPending || !agent.hasPath)
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


