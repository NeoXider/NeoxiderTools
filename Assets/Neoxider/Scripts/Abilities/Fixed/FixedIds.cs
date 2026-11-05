using System.Collections.Generic;

namespace Neo.Abilities.Fixed
{
    /// <summary>
    ///     Case-insensitive id comparison of the fixed core (ASCII folding, invariant upper case beyond ASCII), the
    ///     same rules as the float domain, without culture tables.
    /// </summary>
    public sealed class FixedIdComparer : IEqualityComparer<string>
    {
        public static readonly FixedIdComparer Instance = new FixedIdComparer();

        private FixedIdComparer()
        {
        }

        public static bool Same(string a, string b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (a == null || b == null || a.Length != b.Length)
            {
                return false;
            }

            for (int i = 0; i < a.Length; i++)
            {
                int x = a[i];
                int y = b[i];
                if (x != y && Fold(x) != Fold(y))
                {
                    return false;
                }
            }

            return true;
        }

        public bool Equals(string a, string b)
        {
            return Same(a, b);
        }

        public int GetHashCode(string value)
        {
            if (value == null)
            {
                return 0;
            }

            unchecked
            {
                int hash = (int)2166136261;
                for (int i = 0; i < value.Length; i++)
                {
                    hash = (hash ^ Fold(value[i])) * 16777619;
                }

                return hash;
            }
        }

        internal static int Fold(int c)
        {
            if ((uint)(c - 'a') <= 'z' - 'a')
            {
                return c - ('a' - 'A');
            }

            return c < 0x80 ? c : char.ToUpperInvariant((char)c);
        }
    }

    /// <summary>Built-in event ids; the same strings as the float domain's AbilityEvents.</summary>
    public static class FixedEvents
    {
        public const string TakeDamage = "take_damage";
        public const string DealDamage = "deal_damage";
        public const string HealReceived = "heal_received";
        public const string Death = "death";
        public const string Revive = "revive";
        public const string Kill = "kill";
        public const string AbilityCast = "ability_cast";
        public const string ModifierApplied = "modifier_applied";
        public const string ModifierRemoved = "modifier_removed";
        public const string ShieldAbsorbed = "shield_absorbed";
        public const string ShieldBroken = "shield_broken";
        public const string CriticalHit = "critical_hit";
        public const string Evaded = "evaded";
        public const string BeforeDealDamage = "before_deal_damage";
        public const string BeforeTakeDamage = "before_take_damage";
        public const string AttackLanded = "attack_landed";
        public const string AttackReceived = "attack_received";
        public const string DamageReflected = "damage_reflected";
        public const string AnimationEvent = "animation_event";
        public const string StateEntered = "state_entered";
        public const string StateExited = "state_exited";
        public const string ZoneSpawned = "zone_spawned";
        public const string ZoneExpired = "zone_expired";
    }

    /// <summary>Built-in property ids; the same strings as the float domain's AbilityProperties.</summary>
    public static class FixedProperties
    {
        public const string MoveSpeed = "move_speed";
        public const string AttackDamage = "attack_damage";
        public const string CritChance = "crit_chance";
        public const string CritMultiplier = "crit_multiplier";
        public const string OutgoingDamageMul = "outgoing_damage_mul";
        public const string SpellPower = "spell_power";
        public const string LifestealPercent = "lifesteal_percent";
        public const string Armor = "armor";
        public const string MagicResistPercent = "magic_resist_percent";
        public const string IncomingDamageMul = "incoming_damage_mul";
        public const string ShieldHp = "shield_hp";
        public const string EvasionChance = "evasion_chance";
        public const string MaxHealthBonus = "max_health_bonus";
        public const string MaxManaBonus = "max_mana_bonus";
        public const string HealthRegen = "health_regen";
        public const string ManaRegen = "mana_regen";
        public const string HealingReceivedMul = "healing_received_mul";
        public const string CooldownReductionPercent = "cooldown_reduction_percent";
        public const string CastRangeBonus = "cast_range_bonus";
        public const string ManaCostMul = "mana_cost_mul";
    }

    /// <summary>Built-in resource pool ids.</summary>
    public static class FixedResources
    {
        public const string Health = "health";
        public const string Mana = "mana";
    }

    /// <summary>Built-in boolean states.</summary>
    public static class FixedStates
    {
        public const string Stunned = "stunned";
        public const string Rooted = "rooted";
        public const string Silenced = "silenced";
        public const string Disarmed = "disarmed";
        public const string Invulnerable = "invulnerable";
        public const string Untargetable = "untargetable";
        public const string MagicImmune = "magic_immune";
    }

    /// <summary>Built-in damage types.</summary>
    public static class FixedDamageTypes
    {
        public const string Physical = "physical";
        public const string Magical = "magical";
        public const string Pure = "pure";
    }

    /// <summary>Built-in effect operation ids (the float domain's ids, minus the motion family).</summary>
    public static class FixedOps
    {
        public const string Damage = "damage";
        public const string Heal = "heal";
        public const string ApplyModifier = "apply_modifier";
        public const string RemoveModifier = "remove_modifier";
        public const string Dispel = "dispel";
        public const string ResourceChange = "resource_change";
        public const string Spawn = "spawn";
        public const string Execute = "execute";
        public const string Chain = "chain";
        public const string ModifyDamage = "modify_damage";
        public const string Reflect = "reflect";
        public const string Projectile = "projectile";
        public const string AreaApply = "area_apply";
        public const string Zone = "zone";
        public const string CastAbility = "cast_ability";

        /// <summary>modify_damage modes (CustomParam).</summary>
        public const string ModeMultiply = "multiply";
        public const string ModeAdd = "add";
        public const string ModeAbsorb = "absorb";
        public const string ModeCancel = "cancel";
    }
}
