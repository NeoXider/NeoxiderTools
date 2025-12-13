using System;
using UnityEngine;

namespace Neo.NPC.Navigation
{
    /// <summary>
    /// Pure C# logic that switches between patrol and follow depending on distance to a target.
    /// </summary>
    public sealed class NpcAggroFollowCore
    {
        public event Action StartFollowing;
        public event Action StopFollowing;

        private readonly Transform self;
        private readonly Func<Vector3> getTargetPosition;

        private float aggroDistanceSqr;
        private float maxFollowDistanceSqr;

        public bool IsFollowing { get; private set; }

        public NpcAggroFollowCore(Transform self, Func<Vector3> getTargetPosition)
        {
            this.self = self;
            this.getTargetPosition = getTargetPosition;
        }

        public void Configure(float aggroDistance, float maxFollowDistance)
        {
            float a = Mathf.Max(0f, aggroDistance);
            float m = Mathf.Max(0f, maxFollowDistance);

            aggroDistanceSqr = a * a;
            maxFollowDistanceSqr = m * m;
        }

        public void ResetState()
        {
            IsFollowing = false;
        }

        public void Tick()
        {
            if (getTargetPosition == null)
            {
                return;
            }

            Vector3 targetPos = getTargetPosition();
            float distSqr = (self.position - targetPos).sqrMagnitude;

            bool canAggro = aggroDistanceSqr > 0f;
            bool shouldFollow = canAggro && distSqr <= aggroDistanceSqr;
            bool canDeaggro = maxFollowDistanceSqr > 0f;
            bool shouldStopFollowing = canDeaggro && distSqr > maxFollowDistanceSqr;

            if (shouldFollow && !IsFollowing)
            {
                IsFollowing = true;
                StartFollowing?.Invoke();
            }
            else if (shouldStopFollowing && IsFollowing)
            {
                IsFollowing = false;
                StopFollowing?.Invoke();
            }
        }
    }
}




