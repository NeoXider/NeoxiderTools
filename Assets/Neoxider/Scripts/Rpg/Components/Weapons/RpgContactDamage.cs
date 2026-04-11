using Neo.Reactive;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Rpg
{
    /// <summary>
    ///     Deals melee/contact damage to nearby <see cref="RpgCombatant"/> targets on a cooldown.
    ///     Uses proximity check (not physics collisions), compatible with NavMeshAgent.
    ///     Fully NoCode — configure via Inspector, wire UnityEvents.
    /// </summary>
    [NeoDoc("Rpg/RpgContactDamage.md")]
    [AddComponentMenu("Neoxider/RPG/" + nameof(RpgContactDamage))]
    public sealed class RpgContactDamage : MonoBehaviour
    {
        [Header("Targeting")]
        [Tooltip("Tag of the target to damage. Leave empty to damage any RpgCombatant in range.")]
        [SerializeField] private string targetTag = "Player";

        [Tooltip("If set, uses this specific transform as the target instead of searching by tag.")]
        [SerializeField] private Transform targetOverride;

        [Tooltip("If set, bypasses search and uses this specific combat receiver directly.")]
        [SerializeField] private Component targetReceiverOverride;

        [Header("Damage")]
        [Tooltip("Damage dealt per hit.")]
        [SerializeField] [Min(1)] private int damage = 5;

        [Tooltip("Distance at which damage is applied.")]
        [SerializeField] [Min(0.1f)] private float damageRange = 1.8f;

        [Tooltip("Cooldown between hits in seconds.")]
        [SerializeField] [Min(0.1f)] private float cooldown = 1f;

        [Tooltip("Damage type string (passed to RpgDamageInfo for buff/resistance calculations).")]
        [SerializeField] private string damageType = "Contact";

        [Header("Self")]
        [Tooltip("Optional — if set, stops attacking when own combatant is dead.")]
        [SerializeField] private RpgCombatant selfCombatant;

        [Header("Debug")]
        [SerializeField] private bool debugLog;

        [Header("Reactive State")]
        [Tooltip("Whether this weapon is currently active and dealing damage.")]
        public ReactivePropertyBool IsAttacking = new(false);

        [Tooltip("Whether a valid target is in range.")]
        public ReactivePropertyBool TargetInRange = new(false);

        [Header("Events")]
        [SerializeField] private UnityEventFloat _onDamageDealt = new();
        [SerializeField] private UnityEvent _onTargetFound = new();
        [SerializeField] private UnityEvent _onTargetLost = new();
        [SerializeField] private UnityEvent _onAttack = new();

        /// <summary>Raised when damage is dealt — parameter is the amount.</summary>
        public UnityEventFloat OnDamageDealt => _onDamageDealt;
        /// <summary>Raised when a valid target is first found.</summary>
        public UnityEvent OnTargetFound => _onTargetFound;
        /// <summary>Raised when the target is lost (destroyed, deactivated).</summary>
        public UnityEvent OnTargetLost => _onTargetLost;
        /// <summary>Raised each time an attack occurs.</summary>
        public UnityEvent OnAttack => _onAttack;

        private Transform _cachedTarget;
        private IRpgCombatReceiver _cachedTargetCombatant;
        private float _lastHitTime = -999f;
        private bool _hadTarget;

        private void Start()
        {
            if (selfCombatant == null)
                selfCombatant = GetComponent<RpgCombatant>();
            CacheTarget();
        }

        private void Update()
        {
            if (selfCombatant != null && selfCombatant.IsDead)
            {
                IsAttacking.Value = false;
                return;
            }

            if ((_cachedTarget == null || !_cachedTarget.gameObject.activeInHierarchy) && targetReceiverOverride == null)
            {
                if (_hadTarget)
                {
                    _hadTarget = false;
                    TargetInRange.Value = false;
                    IsAttacking.Value = false;
                    _onTargetLost?.Invoke();
                    if (debugLog) Debug.Log($"[RpgContactDamage] Target lost on {name}");
                }
                CacheTarget();
                if (_cachedTarget == null && targetReceiverOverride == null) return;
            }

            float dist = damageRange; // Default to in-range if we have a direct override without a transform
            if (_cachedTarget != null)
            {
                dist = Vector3.Distance(transform.position, _cachedTarget.position);
            }
            
            bool inRange = dist <= damageRange;
            TargetInRange.Value = inRange;

            if (!inRange)
            {
                IsAttacking.Value = false;
                return;
            }

            if (!_hadTarget)
            {
                _hadTarget = true;
                _onTargetFound?.Invoke();
                if (debugLog) Debug.Log($"[RpgContactDamage] Target found on {name}");
            }

            if (Time.time - _lastHitTime < cooldown) return;

            if (_cachedTargetCombatant == null || _cachedTargetCombatant.IsDead) return;

            float dealt = _cachedTargetCombatant.TakeDamage(new RpgDamageInfo(damage, damageType, selfCombatant));
            _lastHitTime = Time.time;
            IsAttacking.Value = true;

            _onAttack?.Invoke();
            _onDamageDealt?.Invoke(dealt);
            if (debugLog) Debug.Log($"[RpgContactDamage] {name} dealt {dealt} damage to target!");
        }

        /// <summary>Sets the attack target at runtime (code API).</summary>
        public void SetTarget(Transform newTarget)
        {
            targetOverride = newTarget;
            CacheTarget();
        }

        /// <summary>Sets the explicit target receiver at runtime.</summary>
        public void SetTargetReceiver(Component receiver)
        {
            targetReceiverOverride = receiver;
            CacheTarget();
        }

        /// <summary>Sets the damage value at runtime.</summary>
        public void SetDamage(int newDamage) => damage = Mathf.Max(1, newDamage);

        /// <summary>Gets the current damage value.</summary>
        public int Damage => damage;

        /// <summary>Gets the current damage range.</summary>
        public float DamageRange => damageRange;

        private void CacheTarget()
        {
            if (targetReceiverOverride != null && targetReceiverOverride is IRpgCombatReceiver rc)
            {
                _cachedTargetCombatant = rc;
                _cachedTarget = targetReceiverOverride.transform;
                _hadTarget = true;
                return;
            }

            if (targetOverride != null)
            {
                _cachedTarget = targetOverride;
            }
            else if (!string.IsNullOrEmpty(targetTag))
            {
                var go = GameObject.FindGameObjectWithTag(targetTag);
                _cachedTarget = go != null ? go.transform : null;
            }

            _cachedTargetCombatant = _cachedTarget != null
                ? _cachedTarget.GetComponent<IRpgCombatReceiver>()
                  ?? _cachedTarget.GetComponentInChildren<IRpgCombatReceiver>()
                  ?? _cachedTarget.GetComponentInParent<IRpgCombatReceiver>()
                : null;

            _hadTarget = _cachedTarget != null && _cachedTargetCombatant != null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, damageRange);
        }
    }
}
