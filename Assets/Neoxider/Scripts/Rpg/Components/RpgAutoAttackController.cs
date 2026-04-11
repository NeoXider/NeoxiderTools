using UnityEngine;

namespace Neo.Rpg
{
    /// <summary>
    ///     Automatically triggers an <see cref="RpgAttackController"/> when a target is within range.
    ///     Fully NoCode — set the target tag and attack interval.
    /// </summary>
    [NeoDoc("Rpg/RpgAutoAttackController.md")]
    [RequireComponent(typeof(RpgAttackController))]
    [AddComponentMenu("Neoxider/RPG/" + nameof(RpgAutoAttackController))]
    public sealed class RpgAutoAttackController : MonoBehaviour
    {
        [Tooltip("Tag of the target to find and attack.")]
        [SerializeField] private string targetTag = "Player";

        [Tooltip("Distance at which to start attacking.")]
        [SerializeField] [Min(1f)] private float attackRange = 10f;

        [Tooltip("How often to attempt an attack (in seconds).")]
        [SerializeField] [Min(0.1f)] private float attackInterval = 1f;

        private RpgAttackController _attackController;
        private Transform _target;
        private float _lastAttackTime;

        private void Awake()
        {
            _attackController = GetComponent<RpgAttackController>();
        }

        private void Start()
        {
            FindTarget();
        }

        private void Update()
        {
            if (_target == null || !_target.gameObject.activeInHierarchy)
            {
                FindTarget();
                if (_target == null) return;
            }

            if (Time.time - _lastAttackTime < attackInterval) return;

            float dist = Vector3.Distance(transform.position, _target.position);
            if (dist <= attackRange)
            {
                // TryUsePrimaryAttack internally handles checking cooldowns and targeting if configured via preset/queries.
                // But typically, simple AI just triggers UsePrimaryAttack.
                if (_attackController.UsePrimaryAttack())
                {
                    // Success, the controller handled it. Or if it fails, it just waits for next interval.
                }
                
                // We update last attack time regardless so it doesn't spam every frame on failure 
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
