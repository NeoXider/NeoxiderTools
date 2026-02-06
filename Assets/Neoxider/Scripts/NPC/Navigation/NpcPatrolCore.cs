using System;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Neo.NPC.Navigation
{
    /// <summary>
    ///     Pure C# patrol logic for a NavMeshAgent.
    /// </summary>
    public sealed class NpcPatrolCore
    {
        private readonly NavMeshAgent agent;

        private readonly Transform self;

        private bool isWaiting;
        private bool loop;
        private float maxSampleDistance;

        private Transform[] patrolPoints;
        private BoxCollider patrolZone;
        private float waitTime;
        private float waitUntilTime;

        public NpcPatrolCore(Transform self, NavMeshAgent agent)
        {
            this.self = self;
            this.agent = agent;
        }

        public int CurrentIndex { get; private set; }

        public bool IsPatrolling { get; private set; }

        public bool UsesPatrolZone => patrolZone != null;
        public event Action PatrolStarted;
        public event Action PatrolCompleted;
        public event Action<int> PatrolPointReached;
        public event Action<Vector3> DestinationUnreachable;

        public void Configure(Transform[] points, BoxCollider zone, float waitTime, bool loop, float maxSampleDistance)
        {
            patrolPoints = points;
            patrolZone = zone;
            this.waitTime = Mathf.Max(0f, waitTime);
            this.loop = loop;
            this.maxSampleDistance = Mathf.Max(1f, maxSampleDistance);
        }

        public void StartPatrol(bool resetIndex)
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                return;
            }

            if (patrolZone == null && (patrolPoints == null || patrolPoints.Length == 0))
            {
                return;
            }

            if (resetIndex)
            {
                CurrentIndex = 0;
            }

            IsPatrolling = true;
            isWaiting = false;

            agent.isStopped = false;

            if (patrolZone != null)
            {
                TryMoveToRandomPointInZone(5);
            }
            else
            {
                TryMoveToIndexOrAdvance(CurrentIndex, patrolPoints.Length);
            }

            PatrolStarted?.Invoke();
        }

        public void StopPatrol(bool stopAgent)
        {
            IsPatrolling = false;
            isWaiting = false;

            if (stopAgent && agent != null)
            {
                agent.isStopped = true;
            }
        }

        public void Tick(float time)
        {
            if (!IsPatrolling)
            {
                return;
            }

            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                return;
            }

            if (isWaiting)
            {
                if (time >= waitUntilTime)
                {
                    isWaiting = false;
                    MoveNext();
                }

                return;
            }

            if (agent.pathPending || !agent.hasPath)
            {
                return;
            }

            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                isWaiting = true;
                waitUntilTime = time + waitTime;
                PatrolPointReached?.Invoke(CurrentIndex);
            }
        }

        public void SetPatrolPoints(Transform[] points, bool restart)
        {
            patrolPoints = points;
            if (restart)
            {
                StartPatrol(true);
            }
        }

        public void SetPatrolZone(BoxCollider zone, bool restart)
        {
            patrolZone = zone;
            if (restart)
            {
                StartPatrol(true);
            }
        }

        public void ClearPatrolZone()
        {
            patrolZone = null;
        }

        private void MoveNext()
        {
            if (patrolZone != null)
            {
                TryMoveToRandomPointInZone(5);
                return;
            }

            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                IsPatrolling = false;
                PatrolCompleted?.Invoke();
                return;
            }

            CurrentIndex++;

            if (CurrentIndex >= patrolPoints.Length)
            {
                if (!loop)
                {
                    IsPatrolling = false;
                    PatrolCompleted?.Invoke();
                    return;
                }

                CurrentIndex = 0;
            }

            TryMoveToIndexOrAdvance(CurrentIndex, patrolPoints.Length);
        }

        private bool TryMoveToIndexOrAdvance(int index, int maxAttempts)
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                return false;
            }

            int attempts = Mathf.Max(1, maxAttempts);
            int current = Mathf.Clamp(index, 0, patrolPoints.Length - 1);

            for (int i = 0; i < attempts; i++)
            {
                Transform p = patrolPoints[current];
                if (p != null)
                {
                    if (TrySetDestination(p.position))
                    {
                        CurrentIndex = current;
                        return true;
                    }

                    DestinationUnreachable?.Invoke(p.position);
                }

                current++;

                if (current >= patrolPoints.Length)
                {
                    if (!loop)
                    {
                        IsPatrolling = false;
                        PatrolCompleted?.Invoke();
                        return false;
                    }

                    current = 0;
                }
            }

            return false;
        }

        private bool TryMoveToRandomPointInZone(int retries)
        {
            if (patrolZone == null)
            {
                return false;
            }

            int attempts = Mathf.Max(1, retries);
            for (int i = 0; i < attempts; i++)
            {
                Vector3 randomPoint = GetRandomPointInBox(patrolZone);
                if (TrySetDestination(randomPoint))
                {
                    return true;
                }

                DestinationUnreachable?.Invoke(randomPoint);
            }

            return false;
        }

        private bool TrySetDestination(Vector3 desired)
        {
            if (agent == null)
            {
                return false;
            }

            if (NpcDestinationResolver.TryResolve(desired, maxSampleDistance, agent.areaMask, out Vector3 resolved))
            {
                agent.SetDestination(resolved);
                return true;
            }

            return false;
        }

        private static Vector3 GetRandomPointInBox(BoxCollider box)
        {
            Transform t = box.transform;

            Vector3 center = box.center;
            Vector3 half = box.size * 0.5f;

            Vector3 local = new(
                Random.Range(center.x - half.x, center.x + half.x),
                Random.Range(center.y - half.y, center.y + half.y),
                Random.Range(center.z - half.z, center.z + half.z)
            );

            return t.TransformPoint(local);
        }
    }
}