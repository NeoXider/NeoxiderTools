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

    /// <summary>A granted ability of a fixed unit: level, charges and cooldown in ticks.</summary>
    public sealed class FixedAbilitySlot
    {
        private int _cooldownTicks;
        private int _level = 1;

        internal FixedAbilitySlot(FixedAbilityBlueprint blueprint)
        {
            Blueprint = blueprint ?? throw new ArgumentNullException(nameof(blueprint));
            Charges = blueprint.UsesCharges ? blueprint.MaxCharges : 1;
        }

        public FixedAbilityBlueprint Blueprint { get; }
        public int CooldownRemainingTicks { get; internal set; }
        public int Charges { get; internal set; }

        public int Level
        {
            get => _level;
            set => _level = value < 1 ? 1 : value;
        }

        public bool IsReady => Charges > 0 || !Blueprint.UsesCharges && CooldownRemainingTicks <= 0;

        /// <summary>Length of the cooldown currently running, in ticks.</summary>
        public int CooldownTicks => _cooldownTicks;

        internal void StartCooldown(int ticks)
        {
            _cooldownTicks = ticks > 0 ? ticks : 0;
            CooldownRemainingTicks = _cooldownTicks;
            if (Blueprint.UsesCharges && _cooldownTicks <= 0)
            {
                Charges = Blueprint.MaxCharges;
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

            if (Blueprint.UsesCharges)
            {
                Charges = Charges + 1 < Blueprint.MaxCharges ? Charges + 1 : Blueprint.MaxCharges;
                CooldownRemainingTicks = Charges >= Blueprint.MaxCharges ? 0 : _cooldownTicks;
            }
            else
            {
                CooldownRemainingTicks = 0;
                Charges = 1;
            }
        }
    }
}
