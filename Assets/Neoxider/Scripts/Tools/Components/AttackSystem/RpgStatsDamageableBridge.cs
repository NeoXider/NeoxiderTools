using Neo.Rpg;
using Neo.Rpg.Components;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Bridge that implements IDamageable and IHealable by forwarding to an <see cref="RpgCharacter"/>.
    ///     Use on actors that should receive legacy IDamageable hits through the new RPG layer.
    /// </summary>
    [NeoDoc("Tools/Components/AttackSystem/RpgStatsDamageableBridge.md")]
    [CreateFromMenu("Neoxider/RPG/RpgStats Damageable Bridge")]
    [AddComponentMenu("Neoxider/RPG/" + nameof(RpgStatsDamageableBridge))]
    public sealed class RpgStatsDamageableBridge : MonoBehaviour, IDamageable, IHealable
    {
        [Tooltip("Character to forward damage/heal to. When empty, searches this GameObject's parents.")]
        [SerializeField] private RpgCharacter _character;

        [SerializeField] [Min(0f)] private float _damageMultiplier = 1f;
        [SerializeField] [Min(0f)] private float _healMultiplier = 1f;

        public float DamageMultiplier
        {
            get => _damageMultiplier;
            set => _damageMultiplier = Mathf.Max(0f, value);
        }

        public float HealMultiplier
        {
            get => _healMultiplier;
            set => _healMultiplier = Mathf.Max(0f, value);
        }

        public void TakeDamage(int amount)
        {
            RpgCharacter ch = ResolveCharacter();
            if (ch != null && amount > 0) ch.Damage(amount * _damageMultiplier);
        }

        public void Heal(int amount)
        {
            RpgCharacter ch = ResolveCharacter();
            if (ch != null && amount > 0) ch.Heal(amount * _healMultiplier);
        }

        private RpgCharacter ResolveCharacter() =>
            _character != null ? _character : (_character = GetComponentInParent<RpgCharacter>());
    }
}
