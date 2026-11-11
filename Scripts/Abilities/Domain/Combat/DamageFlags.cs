using System;

namespace Neo.Abilities
{
    /// <summary>Options of one damage application through the <see cref="DamagePipeline" />.</summary>
    [Flags]
    public enum DamageFlags
    {
        None = 0,

        /// <summary>A direct hit of a cast or projectile: also publishes attack_landed / attack_received.</summary>
        Hit = 1 << 0,

        /// <summary>Damage produced by reflection; it is never reflected again.</summary>
        Reflected = 1 << 1,

        /// <summary>Skips the before_deal_damage / before_take_damage hooks.</summary>
        NoHooks = 1 << 2,

        /// <summary>Never rolls a critical hit.</summary>
        NoCrit = 1 << 3,

        /// <summary>Never rolls evasion.</summary>
        NoEvasion = 1 << 4,

        /// <summary>Grants the attacker no lifesteal.</summary>
        NoLifesteal = 1 << 5,

        /// <summary>Hook reflect requests are ignored for this application.</summary>
        NoReflect = 1 << 6
    }
}
