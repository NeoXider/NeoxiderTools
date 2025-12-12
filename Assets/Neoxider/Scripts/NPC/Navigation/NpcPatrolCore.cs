using System;
using UnityEngine;
using UnityEngine.AI;

namespace Neo.NPC.Navigation
{
    /// <summary>
    /// Pure C# patrol logic for a NavMeshAgent.
    /// </summary>
    public sealed class NpcPatrolCore
    {
        public event Action PatrolStarted;
        public event Action PatrolCompleted;
        public event Action<int> PatrolPointReached;
        public event Action<Vector3> DestinationUnreachable;

        private readonly Transform self;
        private readonly NavMeshAgent agent;

        private Transform[] patrolPoints;
        private bool loop;
        private float waitTime;
        private float maxSampleDistance;
        private BoxCollider patrolZone;

        private bool isPatrolling;
        private bool isWaiting;
        private float waitUntilTime;

        public int CurrentIndex { get; private set; }

        public bool IsPatrolling => isPatrolling;
        public bool UsesPatrolZone => patrolZone != null;

        public NpcPatrolCore(Transform self, NavMeshAgent agent)
        {
            this.self = self;
            this.agent = agent;
        }

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

            isPatrolling = true;
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
            isPatrolling = false;
            isWaiting = false;

            if (stopAgent && agent != null)
            {
                agent.isStopped = true;
            }
        }

        public void Tick(float time)
        {
            if (!isPatrolling)
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
                isPatrolling = false;
                PatrolCompleted?.Invoke();
                return;
            }

            CurrentIndex++;

            if (CurrentIndex >= patrolPoints.Length)
            {
                if (!loop)
                {
                    isPatrolling = false;
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
                        isPatrolling = false;
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
                UnityEngine.Random.Range(center.x - half.x, center.x + half.x),
                UnityEngine.Random.Range(center.y - half.y, center.y + half.y),
                UnityEngine.Random.Range(center.z - half.z, center.z + half.z)
            );

            return t.TransformPoint(local);
        }
    }
}


