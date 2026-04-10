using UnityEngine;

namespace Neo.NPC
{
    public sealed partial class NpcNavigation
    {
        private interface INavigationBehaviour
        {
            void Enter();
            void Exit();
            void Tick(float deltaTime, float time);
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
                host.followCore.Configure(host.triggerDistance, host.autoUpdatePath, host.pathUpdateInterval,
                    host.maxSampleDistance);
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
                host.patrolCore.Configure(host.patrolPoints, host.patrolZone, host.patrolWaitTime, host.loopPatrol,
                    host.maxSampleDistance);
                host.patrolCore.StartPatrol(true);
            }

            public void Exit()
            {
                host.patrolCore.StopPatrol(false);
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

                host.followCore.Configure(host.triggerDistance, host.autoUpdatePath, host.pathUpdateInterval,
                    host.maxSampleDistance);
                host.followCore.SetFollowEnabled(false);

                host.patrolCore.Configure(host.patrolPoints, host.patrolZone, host.patrolWaitTime, host.loopPatrol,
                    host.maxSampleDistance);
                host.patrolCore.StartPatrol(false);

                host.onTargetChanged?.Invoke(host.combinedTarget);
            }

            public void Exit()
            {
                host.followCore.SetFollowEnabled(false);
                host.patrolCore.StopPatrol(false);
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
                    host.patrolCore.StopPatrol(false);
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
                    host.patrolCore.StartPatrol(false);
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
