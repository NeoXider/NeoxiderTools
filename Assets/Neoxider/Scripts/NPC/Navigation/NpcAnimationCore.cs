using UnityEngine;
using UnityEngine.AI;

namespace Neo.NPC.Navigation
{
    /// <summary>
    ///     Pure C# animation driver based on NavMeshAgent velocity.
    ///     Parameters missing from the bound controller are skipped instead of being written every frame; each
    ///     validated controller reports a missing or mistyped parameter once through the gated
    ///     <see cref="NeoDiagnostics.LogWarning"/> channel.
    /// </summary>
    public sealed class NpcAnimationCore
    {
        private readonly NavMeshAgent agent;
        private readonly Animator animator;
        private readonly float dampTime;
        private readonly int isMovingHash;
        private readonly int speedHash;
        private readonly string isMovingParameter;
        private readonly string speedParameter;

        private RuntimeAnimatorController validatedController;
        private bool hasSpeedParameter;
        private bool hasIsMovingParameter;

        private float speedVelocity;

        public NpcAnimationCore(NavMeshAgent agent, Animator animator, string speedParameter, string isMovingParameter,
            float dampTime)
        {
            this.agent = agent;
            this.animator = animator;
            this.dampTime = Mathf.Max(0f, dampTime);
            this.speedParameter = speedParameter ?? string.Empty;
            this.isMovingParameter = isMovingParameter ?? string.Empty;

            speedHash = Animator.StringToHash(this.speedParameter);
            isMovingHash = Animator.StringToHash(this.isMovingParameter);
        }

        /// <summary>True when the bound controller exposes the float speed parameter.</summary>
        public bool HasSpeedParameter => ValidateParameters() && hasSpeedParameter;

        /// <summary>True when the bound controller exposes the bool moving parameter.</summary>
        public bool HasIsMovingParameter => ValidateParameters() && hasIsMovingParameter;

        public void Tick(float deltaTime)
        {
            if (animator == null || !ValidateParameters())
            {
                return;
            }

            bool agentReady = agent != null && agent.enabled && agent.isOnNavMesh;

            if (hasSpeedParameter)
            {
                float normalizedSpeed = 0f;
                if (agentReady && agent.speed > 0.01f)
                {
                    normalizedSpeed = agent.velocity.magnitude / agent.speed;
                }

                float smooth = Mathf.SmoothDamp(animator.GetFloat(speedHash), normalizedSpeed, ref speedVelocity,
                    dampTime, Mathf.Infinity, Mathf.Max(0f, deltaTime));
                animator.SetFloat(speedHash, smooth);
            }

            if (hasIsMovingParameter)
            {
                bool moving = agentReady && !agent.isStopped && agent.velocity.sqrMagnitude > 0.001f;
                animator.SetBool(isMovingHash, moving);
            }
        }

        // WHY: Animator.SetFloat/SetBool on a parameter the controller lacks logs a warning on every call,
        // which turns a misnamed parameter into per-frame log spam for every NPC. Validate once per controller.
        private bool ValidateParameters()
        {
            if (animator == null)
            {
                return false;
            }

            RuntimeAnimatorController controller = animator.runtimeAnimatorController;
            if (controller == null)
            {
                validatedController = null;
                return false;
            }

            if (ReferenceEquals(controller, validatedController))
            {
                return hasSpeedParameter || hasIsMovingParameter;
            }

            int count = animator.parameterCount;
            if (count == 0)
            {
                // WHY: an uninitialized Animator reports no parameters yet; retry instead of caching a miss.
                return false;
            }

            hasSpeedParameter = false;
            hasIsMovingParameter = false;
            for (int i = 0; i < count; i++)
            {
                AnimatorControllerParameter parameter = animator.GetParameter(i);
                if (parameter.nameHash == speedHash && parameter.type == AnimatorControllerParameterType.Float)
                {
                    hasSpeedParameter = true;
                }
                else if (parameter.nameHash == isMovingHash && parameter.type == AnimatorControllerParameterType.Bool)
                {
                    hasIsMovingParameter = true;
                }
            }

            validatedController = controller;
            ReportMissingParameter(controller, speedParameter, hasSpeedParameter, "Float");
            ReportMissingParameter(controller, isMovingParameter, hasIsMovingParameter, "Bool");
            return hasSpeedParameter || hasIsMovingParameter;
        }

        // WHY: skipping a misnamed parameter silences Unity's own per-frame warning, so one gated warning per
        // validated controller remains the signal for a misconfigured NPC animator.
        private void ReportMissingParameter(RuntimeAnimatorController controller, string parameterName, bool found,
            string expectedType)
        {
            if (found || string.IsNullOrEmpty(parameterName) || !NeoDiagnostics.RuntimeWarningsEnabled)
            {
                return;
            }

            NeoDiagnostics.LogWarning(
                $"[NpcAnimationCore] Controller '{controller.name}' on '{animator.name}' has no {expectedType} parameter '{parameterName}'; it is not driven.",
                animator);
        }
    }
}
