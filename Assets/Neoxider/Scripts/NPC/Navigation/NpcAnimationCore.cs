using UnityEngine;
using UnityEngine.AI;

namespace Neo.NPC.Navigation
{
    /// <summary>
    ///     Pure C# animation driver based on NavMeshAgent velocity.
    /// </summary>
    public sealed class NpcAnimationCore
    {
        private readonly NavMeshAgent agent;
        private readonly Animator animator;
        private readonly float dampTime;
        private readonly int isMovingHash;
        private readonly int speedHash;

        private float speedVelocity;

        public NpcAnimationCore(NavMeshAgent agent, Animator animator, string speedParameter, string isMovingParameter,
            float dampTime)
        {
            this.agent = agent;
            this.animator = animator;
            this.dampTime = Mathf.Max(0f, dampTime);

            speedHash = Animator.StringToHash(speedParameter);
            isMovingHash = Animator.StringToHash(isMovingParameter);
        }

        public void Tick(float deltaTime)
        {
            if (animator == null)
            {
                return;
            }

            float normalizedSpeed = 0f;

            if (agent != null && agent.enabled && agent.speed > 0.01f)
            {
                normalizedSpeed = agent.velocity.magnitude / agent.speed;
            }

            float smooth = Mathf.SmoothDamp(animator.GetFloat(speedHash), normalizedSpeed, ref speedVelocity, dampTime);
            animator.SetFloat(speedHash, smooth);

            bool moving = agent != null && agent.enabled && !agent.isStopped && agent.velocity.sqrMagnitude > 0.001f;
            animator.SetBool(isMovingHash, moving);
        }
    }
}