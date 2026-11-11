using System;

namespace Neo.Abilities.Fixed
{
    /// <summary>How a fixed ability picks its target (same values as the float TargetingMode).</summary>
    public enum FixedTargeting
    {
        NoTarget = 0,
        Self = 1,
        Unit = 2,
        Point = 3,
        Direction = 4
    }

    /// <summary>Which units a selector or zone admits relative to the caster (self counts as an ally).</summary>
    public enum FixedTeamFilter
    {
        Any = 0,
        Enemies = 1,
        Allies = 2
    }

    /// <summary>Instant impact or a projectile whose hits the host reports.</summary>
    public enum FixedDelivery
    {
        Instant = 0,
        Projectile = 1
    }

    /// <summary>Which units an effect node acts on.</summary>
    public enum FixedTargetSelector
    {
        Target = 0,
        Caster = 1,
        AreaAroundTarget = 2,
        AreaAroundCaster = 3,
        ConeFromCaster = 4
    }

    /// <summary>How a property contribution combines: flat add, multiply, or a floor.</summary>
    public enum FixedPropertyOp
    {
        Add = 0,
        Mul = 1,
        Max = 2
    }

    /// <summary>What re-applying a modifier does (same values as the float ModifierStackPolicy).</summary>
    public enum FixedStackPolicy
    {
        Independent = 0,
        Refresh = 1,
        Stack = 2,
        Unique = 3,
        Prolong = 4,
        IndependentCapped = 5
    }

    /// <summary>Decision of an <see cref="IFixedStackingRule" />.</summary>
    public enum FixedStackAction
    {
        CreateNew = 0,
        Refresh = 1,
        AddStack = 2,
        Prolong = 3,
        Ignore = 4,
        ReplaceOldest = 5
    }

    /// <summary>When a modifier reaction fires (same values as the float ModifierTrigger).</summary>
    public enum FixedModifierTrigger
    {
        Event = 0,
        EveryNth = 1,
        HealthBelow = 2,
        StateEnter = 3,
        StateExit = 4,
        Applied = 5,
        Removed = 6
    }

    /// <summary>What started an effect execution; casts and projectile hits are direct hits.</summary>
    public enum FixedEffectOrigin
    {
        Direct = 0,
        Cast = 1,
        ProjectileHit = 2,
        ModifierTick = 3,
        Reaction = 4,
        Zone = 5
    }

    /// <summary>Unit whose property scales a leveled amount.</summary>
    public enum FixedScaleSource
    {
        Caster = 0,
        Target = 1
    }

    /// <summary>Level that samples a leveled amount.</summary>
    public enum FixedLevelSource
    {
        None = 0,
        AbilityLevel = 1,
        CasterUnitLevel = 2,
        TargetUnitLevel = 3
    }

    /// <summary>How the scaling term combines with a leveled amount (the float ScaleAmountMode).</summary>
    public enum FixedScaleMode
    {
        /// <summary>value += k × stat.</summary>
        Add = 0,

        /// <summary>value × (1 + k × stat), applied last.</summary>
        Multiply = 1
    }

    /// <summary>What the scaling term reads (the float ScaleAmountStat).</summary>
    public enum FixedScaleStat
    {
        Property = 0,
        ResourceCurrent = 1,
        ResourceMax = 2,
        ResourceMissing = 3
    }

    /// <summary>How much of a resource a cost takes (the float AbilityCostMode).</summary>
    public enum FixedCostMode
    {
        /// <summary>Exactly the resolved amount.</summary>
        Fixed = 0,

        /// <summary>min(available, amount); needs at least MinAmountRaw available.</summary>
        UpTo = 1,

        /// <summary>Everything available; needs at least MinAmountRaw available.</summary>
        All = 2
    }

    /// <summary>Flags of one damage application (same bits as the float DamageFlags).</summary>
    [Flags]
    public enum FixedDamageFlags
    {
        None = 0,
        Hit = 1 << 0,
        Reflected = 1 << 1,
        NoHooks = 1 << 2,
        NoCrit = 1 << 3,
        NoEvasion = 1 << 4,
        NoLifesteal = 1 << 5,
        NoReflect = 1 << 6
    }

    /// <summary>Why a cast was rejected (same values as the float CastFailureReason).</summary>
    public enum FixedCastFailure
    {
        None = 0,
        UnknownCaster = 1,
        UnknownAbility = 2,
        NotGranted = 3,
        CasterDead = 4,
        Stunned = 5,
        Silenced = 6,
        OnCooldown = 7,
        NoCharges = 8,
        NotEnoughResources = 9,
        InvalidTarget = 10,
        TargetDead = 11,
        TargetUntargetable = 12,
        WrongTeam = 13,
        OutOfRange = 14,
        NestingLimit = 15
    }
}
