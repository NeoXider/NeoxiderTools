using System;
using Neo.Rpg;
using Neo.Rpg.Components;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Bridge that implements IDamageable and IHealable by forwarding to an <see cref="RpgCharacter"/>.
    ///     Use on actors that should receive legacy IDamageable hits through the new RPG layer.
    /// </summary>
    /// <remarks>
    ///     A runtime receiver set with <see cref="Initialize"/> (any <see cref="IRpgCombatReceiver"/>, including plain
    ///     C# objects) takes precedence over the serialized/parent character until <see cref="ClearReceiver"/>.
    /// </remarks>
    [NeoDoc("Rpg/Combat/README.md")]
    [CreateFromMenu("Neoxider/RPG/RpgStats Damageable Bridge")]
    [AddComponentMenu("Neoxider/RPG/" + nameof(RpgStatsDamageableBridge))]
    public sealed class RpgStatsDamageableBridge : MonoBehaviour, IDamageable, IHealable
    {
        [Tooltip("Character to forward damage/heal to. When empty, searches this GameObject's parents.")]
        [SerializeField]
        private RpgCharacter _character;

        [SerializeField] [Min(0f)] private float _damageMultiplier = 1f;
        [SerializeField] [Min(0f)] private float _healMultiplier = 1f;

        private IRpgCombatReceiver _receiverOverride;

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

        /// <summary>True while a runtime receiver set with <see cref="Initialize"/> is active.</summary>
        public bool HasReceiverOverride => _receiverOverride != null;

        /// <summary>
        ///     Routes hits to <paramref name="receiver"/> instead of the serialized/parent character (spawners, pools,
        ///     plain C# actors). Not serialized; the bridge never destroys the receiver.
        /// </summary>
        public void Initialize(IRpgCombatReceiver receiver)
        {
            _receiverOverride = receiver ?? throw new ArgumentNullException(nameof(receiver));
        }

        /// <summary>Drops the runtime receiver and returns to the serialized/parent character path.</summary>
        public void ClearReceiver()
        {
            _receiverOverride = null;
        }

        public void TakeDamage(int amount)
        {
            TryTakeDamage(amount, out _);
        }

        public void Heal(int amount)
        {
            TryHeal(amount, out _);
        }

        /// <summary>
        ///     Forwards a positive hit, scaled by <see cref="DamageMultiplier"/>, to the active receiver.
        /// </summary>
        /// <returns>
        ///     True when the request reached a live receiver; <paramref name="applied"/> is its synchronous answer and can
        ///     be 0 (invulnerable, already dead, routed to a server). False for nonpositive amounts, a missing receiver or
        ///     a destroyed override (which is never silently redirected to another actor).
        /// </returns>
        public bool TryTakeDamage(int amount, out float applied)
        {
            applied = 0f;
            if (amount <= 0)
            {
                return false;
            }

            float scaled = amount * _damageMultiplier;
            if (_receiverOverride != null)
            {
                if (IsDestroyed(_receiverOverride))
                {
                    return false;
                }

                applied = _receiverOverride.TakeDamage(new RpgDamageInfo(scaled, string.Empty));
                return true;
            }

            RpgCharacter character = ResolveCharacter();
            if (character == null)
            {
                return false;
            }

            applied = character.Damage(scaled);
            return true;
        }

        /// <summary>Forwards a positive heal, scaled by <see cref="HealMultiplier"/>; same contract as <see cref="TryTakeDamage"/>.</summary>
        public bool TryHeal(int amount, out float applied)
        {
            applied = 0f;
            if (amount <= 0)
            {
                return false;
            }

            float scaled = amount * _healMultiplier;
            if (_receiverOverride != null)
            {
                if (IsDestroyed(_receiverOverride))
                {
                    return false;
                }

                applied = _receiverOverride.Heal(scaled);
                return true;
            }

            RpgCharacter character = ResolveCharacter();
            if (character == null)
            {
                return false;
            }

            applied = character.Heal(scaled);
            return true;
        }

        private RpgCharacter ResolveCharacter()
        {
            return _character != null ? _character : _character = GetComponentInParent<RpgCharacter>();
        }

        private static bool IsDestroyed(IRpgCombatReceiver receiver)
        {
            return receiver is UnityEngine.Object unityObject && unityObject == null;
        }
    }
}
