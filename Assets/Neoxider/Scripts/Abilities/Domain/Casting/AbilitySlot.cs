using System;

namespace Neo.Abilities
{
    /// <summary>
    ///     Per-unit runtime state of one granted ability: cooldown, charges, level.
    ///     Owned and ticked by <see cref="AbilitySystem" />.
    /// </summary>
    /// <remarks>
    ///     Level-scaled cast parameters (<see cref="AbilityBlueprint.CooldownByLevel" />,
    ///     <see cref="AbilityBlueprint.MaxChargesByLevel" />, ...) are read at the slot's current <see cref="Level" />.
    ///     Changing the level never restarts or rescales a running cooldown: it keeps its remaining time and the next
    ///     cast uses the new level's values. A raised charge cap grants the added charges at once; a lowered cap clamps
    ///     the available charges and stops the restore timer when nothing is left to restore.
    /// </remarks>
    public sealed class AbilitySlot
    {
        private float _cooldownDuration;

        internal AbilitySlot(AbilityBlueprint blueprint)
        {
            Blueprint = blueprint ?? throw new ArgumentNullException(nameof(blueprint));
            Charges = blueprint.UsesChargesAt(1) ? blueprint.ResolveMaxCharges(1) : 1;
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

        /// <summary>
        ///     Ability level for data-driven per-level scaling (SO layer). Clamped to at least 1. A change re-checks the
        ///     charge cap (see the class remarks); a running cooldown keeps its remaining time.
        /// </summary>
        public int Level
        {
            get => _level;
            set
            {
                int clamped = value < 1 ? 1 : value;
                if (clamped == _level)
                {
                    return;
                }

                int previousCapacity = ChargeCapacity;
                _level = clamped;
                ReconcileChargeCapacity(previousCapacity);
            }
        }

        /// <summary>Charge cap at the current level (<see cref="AbilityBlueprint.ResolveMaxCharges" />).</summary>
        public int MaxCharges => Blueprint.ResolveMaxCharges(_level);

        /// <summary>True when the ability uses the charge system at the current level.</summary>
        public bool UsesCharges => MaxCharges > 1;

        /// <summary>Authored cast range at the current level, before cast_range_bonus.</summary>
        public float Range => Blueprint.ResolveRange(_level);

        public bool IsReady => Charges > 0 || (!UsesCharges && CooldownRemaining <= 0f);

        public float NormalizedCooldown
        {
            get
            {
                return _cooldownDuration <= 0f
                    ? 0f
                    : Math.Clamp(CooldownRemaining / _cooldownDuration, 0f, 1f);
            }
        }

        private int ChargeCapacity
        {
            get
            {
                int max = MaxCharges;
                return max > 1 ? max : 1;
            }
        }

        internal void StartCooldown(float duration)
        {
            _cooldownDuration = MathF.Max(0f, duration);
            CooldownRemaining = _cooldownDuration;
            if (UsesCharges && _cooldownDuration <= 0f)
            {
                // WHY: a zero restore time must not permanently consume the available charges.
                Charges = MaxCharges;
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

            int maxCharges = MaxCharges;
            if (maxCharges > 1)
            {
                int missing = maxCharges - Charges;
                if (_cooldownDuration <= 0f || missing <= 0)
                {
                    Charges = maxCharges;
                    CooldownRemaining = 0f;
                }
                else
                {
                    // WHY: catch up every elapsed interval, including the first. Restoring only one
                    // charge can leave a negative timer that never advances again after a long step.
                    double elapsed = Math.Floor(-(double)CooldownRemaining / _cooldownDuration) + 1d;
                    int restored = elapsed >= missing ? missing : (int)elapsed;
                    Charges += restored;
                    CooldownRemaining = Charges >= maxCharges
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

        private void ReconcileChargeCapacity(int previousCapacity)
        {
            int capacity = ChargeCapacity;
            if (capacity == previousCapacity)
            {
                return;
            }

            if (capacity > previousCapacity)
            {
                // WHY: the added charge slots arrive full; a restore already running keeps its remaining time.
                long raised = (long)Charges + (capacity - previousCapacity);
                Charges = raised >= capacity ? capacity : (int)raised;
            }
            else if (Charges > capacity)
            {
                Charges = capacity;
            }

            if (Charges >= capacity && CooldownRemaining > 0f)
            {
                // WHY: nothing is left to restore, so a running timer would only show a phantom cooldown.
                CooldownRemaining = 0f;
            }
        }
    }
}
