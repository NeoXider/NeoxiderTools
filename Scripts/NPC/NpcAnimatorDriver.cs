using Neo.NPC.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Neo.NPC
{
    /// <summary>
    ///     Drives the NPC Animator from NavMeshAgent speed: Speed (float) and IsMoving (bool).
    ///     Add to the same object as NpcNavigation and NavMeshAgent; with an Animator, walk/run clips follow movement.
    /// </summary>
    [NeoDoc("NPC/NpcAnimatorDriver.md")]
    [CreateFromMenu("Neoxider/NPC/NpcAnimatorDriver")]
    [RequireComponent(typeof(NavMeshAgent))]
    [AddComponentMenu("Neoxider/NPC/" + nameof(NpcAnimatorDriver))]
    public sealed class NpcAnimatorDriver : MonoBehaviour
    {
        [SerializeField] [Tooltip("Animator to drive. If unset, taken from this GameObject.")]
        private Animator animator;

        [SerializeField] [Tooltip("Animator parameter name: normalized speed (0..1).")]
        private string speedParameter = "Speed";

        [SerializeField] [Tooltip("Animator parameter name: whether the agent is moving.")]
        private string isMovingParameter = "IsMoving";

        [SerializeField] [Min(0f)] [Tooltip("Smoothing time for speed transitions (seconds).")]
        private float dampTime = 0.1f;

        private NavMeshAgent _agent;

        private NpcAnimationCore _core;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (animator != null && _agent != null)
            {
                _core = new NpcAnimationCore(_agent, animator, speedParameter, isMovingParameter, dampTime);
            }
        }

        private void Update()
        {
            _core?.Tick(Time.deltaTime);
        }
    }
}
