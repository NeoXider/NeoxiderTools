using Neo.NPC.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Neo.NPC
{
    /// <summary>
    ///     Автоматически гонит аниматор NPC по скорости NavMeshAgent: параметры Speed (float) и IsMoving (bool).
    ///     Добавьте на тот же объект, что NpcNavigation и NavMeshAgent; при наличии Animator анимации ходьбы/бега будут включаться по движению.
    /// </summary>
    [NeoDoc("NPC/NpcAnimatorDriver.md")]
    [CreateFromMenu("Neoxider/NPC/NpcAnimatorDriver")]
    [RequireComponent(typeof(NavMeshAgent))]
    [AddComponentMenu("Neoxider/NPC/" + nameof(NpcAnimatorDriver))]
    public sealed class NpcAnimatorDriver : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Animator для управления. Если не задан — берётся с этого объекта.")]
        private Animator animator;

        [SerializeField]
        [Tooltip("Имя параметра Animator: нормализованная скорость (0..1).")]
        private string speedParameter = "Speed";

        [SerializeField]
        [Tooltip("Имя параметра Animator: движется ли агент.")]
        private string isMovingParameter = "IsMoving";

        [SerializeField]
        [Min(0f)]
        [Tooltip("Время сглаживания перехода скорости (сек).")]
        private float dampTime = 0.1f;

        private NpcAnimationCore _core;
        private NavMeshAgent _agent;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (animator == null)
                animator = GetComponent<Animator>();
            if (animator != null && _agent != null)
                _core = new NpcAnimationCore(_agent, animator, speedParameter, isMovingParameter, dampTime);
        }

        private void Update()
        {
            _core?.Tick(Time.deltaTime);
        }
    }
}
