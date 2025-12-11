using UnityEngine;
using UnityEngine.AI;

namespace Neo.NPC.Navigation
{
    /// <summary>
    /// Pure C# navigation agent configuration.
    /// </summary>
    public sealed class NpcNavAgentCore
    {
        private readonly NavMeshAgent agent;

        public float WalkSpeed { get; private set; }
        public float RunSpeed { get; private set; }
        public float Acceleration { get; private set; }
        public float TurnSpeed { get; private set; }
        public float StoppingDistance { get; private set; }
        public bool UpdateRotation { get; private set; }
        public float Radius { get; private set; }
        public int AreaMask { get; private set; }

        public bool IsRunning { get; private set; }

        public NpcNavAgentCore(NavMeshAgent agent)
        {
            this.agent = agent;
        }

        public void Configure(
            float walkSpeed,
            float runSpeed,
            float acceleration,
            float turnSpeed,
            float stoppingDistance,
            bool updateRotation,
            float radius,
            int areaMask)
        {
            WalkSpeed = Mathf.Max(0.1f, walkSpeed);
            RunSpeed = Mathf.Max(0.1f, runSpeed);
            Acceleration = Mathf.Max(0.1f, acceleration);
            TurnSpeed = Mathf.Max(1f, turnSpeed);
            StoppingDistance = Mathf.Max(0.01f, stoppingDistance);
            UpdateRotation = updateRotation;
            Radius = Mathf.Max(0.01f, radius);
            AreaMask = areaMask;

            ApplyToAgent();
        }

        public void ApplyToAgent()
        {
            if (agent == null)
            {
                return;
            }

            agent.speed = IsRunning ? RunSpeed : WalkSpeed;
            agent.acceleration = Acceleration;
            agent.angularSpeed = TurnSpeed;
            agent.stoppingDistance = StoppingDistance;
            agent.updateRotation = UpdateRotation;
            agent.radius = Radius;
            agent.areaMask = AreaMask;
        }

        public void SetRunning(bool enable)
        {
            IsRunning = enable;
            if (agent == null)
            {
                return;
            }

            agent.speed = IsRunning ? RunSpeed : WalkSpeed;
        }

        public void SetSpeed(float speed)
        {
            if (agent == null)
            {
                return;
            }

            agent.speed = Mathf.Max(0.1f, speed);
        }
    }
}

