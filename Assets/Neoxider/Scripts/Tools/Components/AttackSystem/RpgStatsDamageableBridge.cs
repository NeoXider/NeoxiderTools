using Neo.Rpg;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    /// Bridge that implements IDamageable and IHealable by forwarding to an RPG combat receiver.
    /// Use on actors that should receive legacy IDamageable hits through the new RPG combat layer.
    /// </summary>
    [NeoDoc("Tools/Components/AttackSystem/RpgStatsDamageableBridge.md")]
    [CreateFromMenu("Neoxider/RPG/RpgStats Damageable Bridge")]
    [AddComponentMenu("Neoxider/RPG/" + nameof(RpgStatsDamageableBridge))]
    public sealed class RpgStatsDamageableBridge : MonoBehaviour, IDamageable, IHealable
    {
        [SerializeField] private RpgStatsManager _manager;
        [SerializeField] private RpgCombatant _combatant;
        [SerializeField] [Min(0f)] private float _damageMultiplier = 1f;
        [SerializeField] [Min(0f)] private float _healMultiplier = 1f;

        /// <summary>
        /// Gets or sets the damage multiplier applied before forwarding to RpgStatsManager.
        /// </summary>
        public float DamageMultiplier
        {
            get => _damageMultiplier;
            set => _damageMultiplier = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Gets or sets the heal multiplier applied before forwarding to RpgStatsManager.
        /// </summary>
        public float HealMultiplier
        {
            get => _healMultiplier;
            set => _healMultiplier = Mathf.Max(0f, value);
        }

        /// <inheritdoc />
        public void TakeDamage(int amount)
        {
            IRpgCombatReceiver receiver = ResolveReceiver();
            if (receiver != null && amount > 0)
            {
                float scaled = amount * _damageMultiplier;
                receiver.TakeDamage(scaled);
            }
        }

        /// <inheritdoc />
        public void Heal(int amount)
        {
            IRpgCombatReceiver receiver = ResolveReceiver();
            if (receiver != null && amount > 0)
            {
                float scaled = amount * _healMultiplier;
                receiver.Heal(scaled);
            }
        }

        private IRpgCombatReceiver ResolveReceiver()
        {
            if (_combatant != null)
            {
                return _combatant;
            }

            return _manager != null ? _manager : RpgStatsManager.Instance;
        }
    }
}
