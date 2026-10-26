using System;

namespace Neo.Abilities
{
    /// <summary>
    ///     Per-unit runtime state of one granted ability: cooldown, charges, level.
    ///     Owned and ticked by <see cref="AbilitySystem" />.
    /// </summary>
    public sealed class AbilitySlot
    {
        private float _cooldownDuration;

        internal AbilitySlot(AbilityBlueprint blueprint)
        {
            Blueprint = blueprint ?? throw new ArgumentNullException(nameof(blueprint));
            Charges = blueprint.UsesCharges ? blueprint.MaxCharges : 1;
        }

        /// <summary>
        ///     Blueprint reference captured on grant. Catalog replacement does not change this reference;
        ///     the blueprint object itself remains mutable and is not copied or made immutable.
        /// </summary>
        public AbilityBlueprint Blueprint { get; }

        /// <summary>Seconds until the ability (or next charge) is ready. 0 = ready.</summary>
        public float CooldownRemaining { get; internal set; }

        /// <summary>Available charges. Non-charge abilities use 0/1 semantics.</summary>
        public int Charges { get; internal set; }

        private int _level = 1;

        /// <summary>Ability level for data-driven per-level scaling (SO layer). Clamped to at least 1.</summary>
        public int Level
        {
            get => _level;
            set => _level = value < 1 ? 1 : value;
        }

        public bool IsReady => Charges > 0 || (!Blueprint.UsesCharges && CooldownRemaining <= 0f);

        public float NormalizedCooldown
        {
            get
            {
                return _cooldownDuration <= 0f
                    ? 0f
                    : Math.Clamp(CooldownRemaining / _cooldownDuration, 0f, 1f);
            }
        }

        internal void StartCooldown(float duration)
        {
            _cooldownDuration = MathF.Max(0f, duration);
            CooldownRemaining = _cooldownDuration;
            if (Blueprint.UsesCharges && _cooldownDuration <= 0f)
            {
                // WHY: a zero restore time must not permanently consume the available charges.
                Charges = Blueprint.MaxCharges;
            }
        }

        internal void Tick(float deltaTime)
        {
            if (CooldownRemaining <= 0f)
            {
                return;
            }

            CooldownRemaining -= deltaTime;
            if (CooldownRemaining > 0f)
            {
                return;
            }

            if (Blueprint.UsesCharges)
            {
                int missing = Blueprint.MaxCharges - Charges;
                if (_cooldownDuration <= 0f || missing <= 0)
                {
                    Charges = Blueprint.MaxCharges;
                    CooldownRemaining = 0f;
                }
                else
                {
                    // WHY: catch up every elapsed interval, including the first. Restoring only one
                    // charge can leave a negative timer that never advances again after a long step.
                    double elapsed = Math.Floor(-(double)CooldownRemaining / _cooldownDuration) + 1d;
                    int restored = elapsed >= missing ? missing : (int)elapsed;
                    Charges += restored;
                    CooldownRemaining = Charges >= Blueprint.MaxCharges
                        ? 0f
                        : CooldownRemaining + restored * _cooldownDuration;
                }
            }
            else
            {
                CooldownRemaining = 0f;
                Charges = 1;
            }
        }
    }
}
