using System;

namespace Neo.Abilities.Fixed
{
    /// <summary>A cast request of the fixed core; a non-zero seed fixes its random stream.</summary>
    public readonly struct FixedCastRequest
    {
        public readonly int Caster;
        public readonly string AbilityId;
        public readonly int TargetUnit;
        public readonly FixedVec3 TargetPoint;
        public readonly FixedVec3 Direction;
        public readonly uint Seed;

        public FixedCastRequest(int caster, string abilityId, int targetUnit, FixedVec3 targetPoint, FixedVec3 direction,
            uint seed)
        {
            Caster = caster;
            AbilityId = abilityId;
            TargetUnit = targetUnit;
            TargetPoint = targetPoint;
            Direction = direction;
            Seed = seed;
        }

        public static FixedCastRequest NoTarget(int caster, string abilityId, uint seed = 0u)
        {
            return new FixedCastRequest(caster, abilityId, 0, FixedVec3.Zero, FixedVec3.Zero, seed);
        }

        public static FixedCastRequest AtUnit(int caster, string abilityId, int target, uint seed = 0u)
        {
            return new FixedCastRequest(caster, abilityId, target, FixedVec3.Zero, FixedVec3.Zero, seed);
        }

        public static FixedCastRequest AtPoint(int caster, string abilityId, FixedVec3 point, uint seed = 0u)
        {
            return new FixedCastRequest(caster, abilityId, 0, point, FixedVec3.Zero, seed);
        }

        public static FixedCastRequest Towards(int caster, string abilityId, FixedVec3 direction, uint seed = 0u)
        {
            return new FixedCastRequest(caster, abilityId, 0, FixedVec3.Zero, direction, seed);
        }
    }

    /// <summary>Outcome of a fixed cast.</summary>
    public readonly struct FixedCastResult
    {
        public readonly bool Success;
        public readonly FixedCastFailure Failure;
        public readonly uint CastId;

        private FixedCastResult(bool success, FixedCastFailure failure, uint castId)
        {
            Success = success;
            Failure = failure;
            CastId = castId;
        }

        public static FixedCastResult Ok(uint castId)
        {
            return new FixedCastResult(true, FixedCastFailure.None, castId);
        }

        public static FixedCastResult Fail(FixedCastFailure failure)
        {
            return new FixedCastResult(false, failure, 0u);
        }
    }

    /// <summary>
    ///     A granted ability of a fixed unit: level, charges and cooldown in ticks. Level-scaled cast parameters are read
    ///     at the current level; a level change keeps a running cooldown's remaining ticks, grants the charges a raised
    ///     cap adds and clamps the charges to a lowered cap (stopping the restore timer when nothing is left to restore),
    ///     exactly like the float AbilitySlot.
    /// </summary>
    public sealed class FixedAbilitySlot
    {
        private int _cooldownTicks;
        private int _level = 1;

        internal FixedAbilitySlot(FixedAbilityBlueprint blueprint)
        {
            Blueprint = blueprint ?? throw new ArgumentNullException(nameof(blueprint));
            Charges = blueprint.UsesChargesAt(1) ? blueprint.ResolveMaxCharges(1) : 1;
        }

        public FixedAbilityBlueprint Blueprint { get; }
        public int CooldownRemainingTicks { get; internal set; }
        public int Charges { get; internal set; }

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

        /// <summary>Charge cap at the current level.</summary>
        public int MaxCharges => Blueprint.ResolveMaxCharges(_level);

        public bool UsesCharges => MaxCharges > 1;

        /// <summary>Raw cast range at the current level, before cast_range_bonus.</summary>
        public int RangeRaw => Blueprint.ResolveRangeRaw(_level);

        public bool IsReady => Charges > 0 || !UsesCharges && CooldownRemainingTicks <= 0;

        /// <summary>Length of the cooldown currently running, in ticks.</summary>
        public int CooldownTicks => _cooldownTicks;

        private int ChargeCapacity
        {
            get
            {
                int max = MaxCharges;
                return max > 1 ? max : 1;
            }
        }

        internal void StartCooldown(int ticks)
        {
            _cooldownTicks = ticks > 0 ? ticks : 0;
            CooldownRemainingTicks = _cooldownTicks;
            if (UsesCharges && _cooldownTicks <= 0)
            {
                Charges = MaxCharges;
            }
        }

        internal void Step()
        {
            if (CooldownRemainingTicks <= 0)
            {
                return;
            }

            CooldownRemainingTicks--;
            if (CooldownRemainingTicks > 0)
            {
                return;
            }

            int maxCharges = MaxCharges;
            if (maxCharges > 1)
            {
                Charges = Charges + 1 < maxCharges ? Charges + 1 : maxCharges;
                CooldownRemainingTicks = Charges >= maxCharges ? 0 : _cooldownTicks;
            }
            else
            {
                CooldownRemainingTicks = 0;
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
                long raised = (long)Charges + (capacity - previousCapacity);
                Charges = raised >= capacity ? capacity : (int)raised;
            }
            else if (Charges > capacity)
            {
                Charges = capacity;
            }

            if (Charges >= capacity && CooldownRemainingTicks > 0)
            {
                CooldownRemainingTicks = 0;
            }
        }
    }
}
