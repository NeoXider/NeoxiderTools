using UnityEngine;

namespace Neo.Rpg
{
    /// <summary>
    ///     ScriptableObject definition for melee, ranged, and area RPG attacks.
    /// </summary>
    [CreateAssetMenu(fileName = "Rpg Attack Definition", menuName = "Neoxider/RPG/Rpg Attack Definition")]
    public sealed class RpgAttackDefinition : ScriptableObject
    {
        [SerializeField] private string _id = string.Empty;
        [SerializeField] private string _displayName = "Attack";
        [SerializeField] private RpgAttackDeliveryType _deliveryType = RpgAttackDeliveryType.Direct;
        [SerializeField] private RpgHitMode _hitMode = RpgHitMode.Damage;
        [SerializeField] private string _damageType = "";
        [SerializeField] [Min(0f)] private float _power = 10f;
        [SerializeField] [Min(0f)] private float _range = 2f;
        [SerializeField] [Min(0f)] private float _radius = 0.5f;
        [SerializeField] [Min(0f)] private float _castDelay;
        [SerializeField] [Min(0f)] private float _cooldown = 0.5f;
        [SerializeField] private bool _use2D = true;
        [SerializeField] private bool _use3D = true;
        [SerializeField] private LayerMask _targetLayers = -1;
        [SerializeField] [Min(1)] private int _maxTargets = 1;
        [SerializeField] private RpgAttackEffectRefs _effects = new();
        [SerializeField] private RpgProjectile _projectilePrefab;
        [SerializeField] [Min(0.01f)] private float _projectileSpeed = 10f;
        [SerializeField] [Min(0.05f)] private float _projectileLifetime = 5f;
        [SerializeField] [Min(1)] private int _projectileMaxHits = 1;
        [SerializeField] private GameObject _impactEffectPrefab;

        [Tooltip("Resource id to spend (e.g. Mana, HP). Empty or 0 cost = free.")] [SerializeField]
        private string _costResourceId = "Mana";

        [SerializeField] [Min(0f)] private float _costAmount;

        /// <summary>
        ///     Gets the resource id to spend for this attack (e.g. Mana, HP).
        /// </summary>
        public string CostResourceId => string.IsNullOrWhiteSpace(_costResourceId) ? "Mana" : _costResourceId;

        /// <summary>
        ///     Gets the amount of resource required (0 = free).
        /// </summary>
        public float CostAmount => _costAmount;

        /// <summary>
        ///     Gets the unique attack identifier.
        /// </summary>
        public string Id => string.IsNullOrWhiteSpace(_id) ? name : _id;

        /// <summary>
        ///     Gets the display name.
        /// </summary>
        public string DisplayName => _displayName;

        /// <summary>
        ///     Gets how the attack is delivered.
        /// </summary>
        public RpgAttackDeliveryType DeliveryType => _deliveryType;

        /// <summary>
        ///     Gets whether the effect deals damage or healing.
        /// </summary>
        public RpgHitMode HitMode => _hitMode;

        /// <summary>
        ///     Gets the damage type identifier (e.g. Physical, Fire, Magic).
        /// </summary>
        public string DamageType => _damageType;

        /// <summary>
        ///     Gets the base power before source multipliers.
        /// </summary>
        public float Power => _power;

        /// <summary>
        ///     Gets the attack range.
        /// </summary>
        public float Range => _range;

        /// <summary>
        ///     Gets the hit radius for sphere/circle queries.
        /// </summary>
        public float Radius => _radius;

        /// <summary>
        ///     Gets the cast delay before applying the attack.
        /// </summary>
        public float CastDelay => _castDelay;

        /// <summary>
        ///     Gets the attack cooldown in seconds.
        /// </summary>
        public float Cooldown => _cooldown;

        /// <summary>
        ///     Gets whether the attack uses 2D physics.
        /// </summary>
        public bool Use2D => _use2D;

        /// <summary>
        ///     Gets whether the attack uses 3D physics.
        /// </summary>
        public bool Use3D => _use3D;

        /// <summary>
        ///     Gets the target layer filter.
        /// </summary>
        public LayerMask TargetLayers => _targetLayers;

        /// <summary>
        ///     Gets the maximum number of targets affected.
        /// </summary>
        public int MaxTargets => Mathf.Max(1, _maxTargets);

        /// <summary>
        ///     Gets the additional buffs/statuses applied by the attack.
        /// </summary>
        public RpgAttackEffectRefs Effects => _effects;

        /// <summary>
        ///     Gets the projectile prefab used by projectile attacks.
        /// </summary>
        public RpgProjectile ProjectilePrefab => _projectilePrefab;

        /// <summary>
        ///     Gets the projectile travel speed.
        /// </summary>
        public float ProjectileSpeed => _projectileSpeed;

        /// <summary>
        ///     Gets the projectile lifetime.
        /// </summary>
        public float ProjectileLifetime => _projectileLifetime;

        /// <summary>
        ///     Gets the maximum number of projectile hits before despawn.
        /// </summary>
        public int ProjectileMaxHits => Mathf.Max(1, _projectileMaxHits);

        /// <summary>
        ///     Gets the impact VFX prefab spawned on hit.
        /// </summary>
        public GameObject ImpactEffectPrefab => _impactEffectPrefab;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_id) && !string.IsNullOrWhiteSpace(name))
            {
                _id = name;
            }
        }
    }
}
