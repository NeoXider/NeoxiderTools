using Neo.Reactive;
using Neo.Rpg.Components;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Rpg
{
    /// <summary>
    ///     Deals melee/contact damage to nearby <see cref="RpgCharacter"/> targets on a cooldown.
    ///     Uses proximity check (not physics collisions), compatible with NavMeshAgent.
    ///     Fully NoCode  - configure via Inspector, wire UnityEvents.
    /// </summary>
    [NeoDoc("Rpg/RpgContactDamage.md")]
    [CreateFromMenu("Neoxider/RPG/RpgContactDamage")]
    [AddComponentMenu("Neoxider/RPG/" + nameof(RpgContactDamage))]
    public sealed class RpgContactDamage : MonoBehaviour
    {
        [Header("Targeting")]
        [Tooltip("Tag of the target searched when no override is set. Empty disables the search, so only " +
                 "Target Override or Target Receiver Override is damaged.")]
        [SerializeField]
        private string targetTag = "Player";

        [Tooltip("If set, uses this specific transform as the target instead of searching by tag.")] [SerializeField]
        private Transform targetOverride;

        [Tooltip("If set, bypasses search and uses this specific combat receiver directly.")] [SerializeField]
        private Component targetReceiverOverride;

        [Tooltip("How often to retry searching for target when target is missing.")] [SerializeField] [Min(0.1f)]
        private float targetFindInterval = 0.5f;

        [Header("Damage")] [Tooltip("Damage dealt per hit.")] [SerializeField] [Min(1)]
        private int damage = 5;

        [Tooltip("Distance at which damage is applied.")] [SerializeField] [Min(0.1f)]
        private float damageRange = 1.8f;

        [Tooltip("Cooldown between hits in seconds.")] [SerializeField] [Min(0.1f)]
        private float cooldown = 1f;

        [Tooltip("Damage type string (passed to RpgDamageInfo for buff/resistance calculations).")] [SerializeField]
        private string damageType = "Contact";

        [Header("Self")] [Tooltip("Optional  - if set, stops attacking when own character is dead.")] [SerializeField]
        private RpgCharacter selfCharacter;

        [Header("Debug")] [SerializeField] private bool debugLog;

        [Header("Reactive State")] [Tooltip("Whether this weapon is currently active and dealing damage.")]
        public ReactivePropertyBool IsAttacking = new(false);

        [Tooltip("Whether a valid target is in range.")]
        public ReactivePropertyBool TargetInRange = new(false);

        [Header("Events")] [SerializeField] private UnityEventFloat _onDamageDealt = new();
        [SerializeField] private UnityEvent _onTargetFound = new();
        [SerializeField] private UnityEvent _onTargetLost = new();
        [SerializeField] private UnityEvent _onAttack = new();

        /// <summary>Raised when damage is dealt  - parameter is the amount.</summary>
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

        // WHY: true only after OnTargetFound was raised for the current target; caching a target must not set it,
        // otherwise OnTargetFound never fires and an inactive cached target re-raises OnTargetLost on every retry.
        private bool _hadTarget;
        private float _nextTargetFindTime = -1f;

        private void Start()
        {
            if (selfCharacter == null)
            {
                selfCharacter = GetComponent<RpgCharacter>();
            }

            CacheTarget();
            _nextTargetFindTime = Time.time + targetFindInterval;
        }

        private void Update()
        {
            if (selfCharacter != null && selfCharacter.IsDead)
            {
                IsAttacking.Value = false;
                return;
            }

            if (_cachedTarget == null || !_cachedTarget.gameObject.activeInHierarchy)
            {
                if (_hadTarget)
                {
                    _hadTarget = false;
                    TargetInRange.Value = false;
                    IsAttacking.Value = false;
                    _onTargetLost?.Invoke();
                    if (debugLog)
                    {
                        NeoDiagnostics.Log($"[RpgContactDamage] Target lost on {name}");
                    }
                }

                if (Time.time >= _nextTargetFindTime)
                {
                    CacheTarget();
                    _nextTargetFindTime = Time.time + targetFindInterval;
                }

                if (_cachedTarget == null || !_cachedTarget.gameObject.activeInHierarchy)
                {
                    return;
                }
            }

            // WHY: default to in-range if we have a direct override without a transform.
            float dist = damageRange;
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
                if (debugLog)
                {
                    NeoDiagnostics.Log($"[RpgContactDamage] Target found on {name}");
                }
            }

            if (Time.time - _lastHitTime < cooldown)
            {
                return;
            }

            if (_cachedTargetCombatant == null ||
                _cachedTargetCombatant is Object combatantObject && combatantObject == null ||
                _cachedTargetCombatant.IsDead)
            {
                return;
            }

            float dealt = _cachedTargetCombatant.TakeDamage(new RpgDamageInfo(damage, damageType, selfCharacter));
            _lastHitTime = Time.time;
            IsAttacking.Value = true;

            _onAttack?.Invoke();
            _onDamageDealt?.Invoke(dealt);
            if (debugLog)
            {
                NeoDiagnostics.Log($"[RpgContactDamage] {name} dealt {dealt} damage to target!");
            }
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
        public void SetDamage(int newDamage)
        {
            damage = Mathf.Max(1, newDamage);
        }

        /// <summary>Gets the current damage value.</summary>
        public int Damage => damage;

        /// <summary>Gets the current damage range.</summary>
        public float DamageRange => damageRange;

        /// <summary>Gets the cooldown between hits in seconds.</summary>
        public float Cooldown => cooldown;

        /// <summary>Gets the damage type passed to the receiver.</summary>
        public string DamageType => damageType;

        /// <summary>Sets the damage range at runtime (minimum 0.1).</summary>
        public void SetDamageRange(float range)
        {
            damageRange = Mathf.Max(0.1f, range);
        }

        /// <summary>Sets the cooldown between hits at runtime (minimum 0.1 seconds).</summary>
        public void SetCooldown(float seconds)
        {
            cooldown = Mathf.Max(0.1f, seconds);
        }

        /// <summary>Sets the damage type passed to the receiver for resist calculations.</summary>
        public void SetDamageType(string type)
        {
            damageType = type ?? string.Empty;
        }

        /// <summary>Currently cached target transform (null when none).</summary>
        public Transform CurrentTarget => _cachedTarget;

        private void CacheTarget()
        {
            Transform previousTarget = _cachedTarget;
            _cachedTarget = null;
            _cachedTargetCombatant = null;
            if (targetReceiverOverride != null && targetReceiverOverride is IRpgCombatReceiver rc)
            {
                _cachedTargetCombatant = rc;
                _cachedTarget = targetReceiverOverride.transform;
            }
            else
            {
                if (targetOverride != null)
                {
                    _cachedTarget = targetOverride;
                }
                else if (!string.IsNullOrEmpty(targetTag))
                {
                    GameObject go = GameObject.FindGameObjectWithTag(targetTag);
                    _cachedTarget = go != null ? go.transform : null;
                }

                _cachedTargetCombatant = _cachedTarget != null ? ResolveCombatant(_cachedTarget) : null;
            }

            if (_cachedTarget != previousTarget)
            {
                // WHY: a different target must be found (in range) again before OnTargetFound fires for it.
                _hadTarget = false;
            }
        }

        private static IRpgCombatReceiver ResolveCombatant(Transform target)
        {
            // WHY: TryGetComponent avoids the editor fake-null that would short-circuit the ?? fallbacks.
            if (target.TryGetComponent(out IRpgCombatReceiver direct))
            {
                return direct;
            }

            return target.GetComponentInChildren<IRpgCombatReceiver>() ??
                   target.GetComponentInParent<IRpgCombatReceiver>();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, damageRange);
        }
    }
}
