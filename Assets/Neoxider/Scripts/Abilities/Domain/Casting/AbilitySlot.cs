using System;

namespace Neo.Abilities
{
    /// <summary>
    ///     Per-unit runtime state of one granted ability: cooldown, charges, level.
    ///     Owned and ticked by <see cref="AbilitySystem" />.
    /// </summary>
    public sealed class AbilitySlot
    {
        internal AbilitySlot(AbilityBlueprint blueprint)
        {
            Blueprint = blueprint ?? throw new ArgumentNullException(nameof(blueprint));
            Charges = blueprint.UsesCharges ? blueprint.MaxCharges : 1;
        }

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
                float total = Blueprint.UsesCharges && Blueprint.ChargeRestoreTime > 0f
                    ? Blueprint.ChargeRestoreTime
                    : Blueprint.Cooldown;
                return total <= 0f ? 0f : CooldownRemaining / total;
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
                Charges++;
                if (Charges < Blueprint.MaxCharges)
                {
                    float restore = Blueprint.ChargeRestoreTime > 0f
                        ? Blueprint.ChargeRestoreTime
                        : Blueprint.Cooldown;
                    CooldownRemaining += restore;
                }
                else
                {
                    CooldownRemaining = 0f;
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
