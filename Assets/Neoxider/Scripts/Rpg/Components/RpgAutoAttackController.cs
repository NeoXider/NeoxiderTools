using UnityEngine;

namespace Neo.Rpg
{
    /// <summary>
    ///     Automatically triggers an <see cref="RpgAttackController"/> when a target is within range.
    ///     Fully NoCode — set the target tag and attack interval.
    /// </summary>
    [NeoDoc("Rpg/RpgAutoAttackController.md")]
    [RequireComponent(typeof(RpgAttackController))]
    [CreateFromMenu("Neoxider/RPG/RpgAutoAttackController")]
    [AddComponentMenu("Neoxider/RPG/" + nameof(RpgAutoAttackController))]
    public sealed class RpgAutoAttackController : MonoBehaviour
    {
        [Tooltip("Tag of the target to find and attack.")] [SerializeField]
        private string targetTag = "Player";

        [Tooltip("Distance at which to start attacking.")] [SerializeField] [Min(1f)]
        private float attackRange = 10f;

        [Tooltip("How often to attempt an attack (in seconds).")] [SerializeField] [Min(0.1f)]
        private float attackInterval = 1f;

        [Tooltip("How often to retry searching for target when current target is missing (in seconds).")]
        [SerializeField]
        [Min(0.1f)]
        private float targetFindInterval = 0.5f;

        private RpgAttackController _attackController;
        private Transform _target;
        private float _lastAttackTime;
        private float _nextTargetFindTime = -1f;

        private void Awake()
        {
            _attackController = GetComponent<RpgAttackController>();
        }

        private void Start()
        {
            FindTarget();
            _nextTargetFindTime = Time.time + targetFindInterval;
        }

        private void Update()
        {
            if (_target == null || !_target.gameObject.activeInHierarchy)
            {
                if (Time.time >= _nextTargetFindTime)
                {
                    FindTarget();
                    _nextTargetFindTime = Time.time + targetFindInterval;
                }

                if (_target == null)
                {
                    return;
                }
            }

            if (Time.time - _lastAttackTime < attackInterval)
            {
                return;
            }

            float dist = Vector3.Distance(transform.position, _target.position);
            if (dist <= attackRange)
            {
                if (_attackController.UsePrimaryAttack())
                {
                }

                // WHY: update regardless of success so failures don't spam attempts every frame.
                _lastAttackTime = Time.time;
            }
        }

        private void FindTarget()
        {
            if (!string.IsNullOrEmpty(targetTag))
            {
                var go = GameObject.FindGameObjectWithTag(targetTag);
                _target = go != null ? go.transform : null;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
