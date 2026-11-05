using UnityEngine;

namespace Neo.Abilities
{
    /// <summary>
    ///     Description of a zone to spawn with <see cref="AbilitySystem.SpawnZone" /> (code path parallel to the
    ///     <c>zone</c> effect op): an aura following a unit or a formation fixed at a point that pulses on spawn and then
    ///     every <see cref="Interval" />, applying <see cref="ModifierId" /> and/or the impact effects of
    ///     <see cref="PayloadAbilityId" /> to the units inside.
    /// </summary>
    public struct AbilityZoneSpec
    {
        /// <summary>Unit owning the zone (its team filters the units inside; it is the caster of the zone effects).</summary>
        public UnitId Caster;

        /// <summary>Optional unit the zone belongs to; the zone ends when it dies or is destroyed.</summary>
        public UnitId Anchor;

        /// <summary>When true the zone centre follows <see cref="Anchor" /> every tick (an aura).</summary>
        public bool FollowAnchor;

        /// <summary>Centre for a fixed zone (or the start centre of a following one).</summary>
        public Vector3 Center;

        public float Radius;

        /// <summary>Lifetime in seconds; 0 or less = until removed or until the anchor dies.</summary>
        public float Duration;

        /// <summary>Seconds between pulses after the spawn pulse; 0 = the spawn pulse only.</summary>
        public float Interval;

        /// <summary>Ability whose impact effects run on every pulse (primary targets = the units inside).</summary>
        public string PayloadAbilityId;

        /// <summary>Catalog modifier applied to every unit inside on every pulse (aura buffs use a short Refresh modifier).</summary>
        public string ModifierId;

        public AbilityTeamFilter TeamFilter;

        /// <summary>Level captured for leveled payload values and the applied modifier.</summary>
        public int AbilityLevel;

        /// <summary>Ability credited as the source of applied modifiers and receipts.</summary>
        public string SourceAbilityId;

        /// <summary>Optional visual archetype the host spawns (SpawnRequest.ZoneId / Duration describe the zone).</summary>
        public string ArchetypeId;

        /// <summary>Seed of the pulse RNG streams (0 = derived from the zone id).</summary>
        public uint Seed;
    }

    /// <summary>
    ///     A live zone of an <see cref="AbilitySystem" />. Zones are recycled after they end: compare <see cref="Id" />
    ///     and check <see cref="IsActive" /> before reusing a stored reference.
    /// </summary>
    public sealed class AbilityZone
    {
        internal AbilityZone()
        {
        }

        public int Id { get; internal set; }
        public bool IsActive { get; internal set; }
        public UnitId Caster { get; internal set; }
        public TeamId CasterTeam { get; internal set; }
        public UnitId Anchor { get; internal set; }
        public bool FollowAnchor { get; internal set; }
        public Vector3 Center { get; internal set; }
        public float Radius { get; internal set; }
        public float Duration { get; internal set; }
        public float Remaining { get; internal set; }
        public float Interval { get; internal set; }
        public string PayloadAbilityId { get; internal set; }
        public string ModifierId { get; internal set; }
        public AbilityTeamFilter TeamFilter { get; internal set; }
        public int AbilityLevel { get; internal set; }
        public string SourceAbilityId { get; internal set; }
        public string ArchetypeId { get; internal set; }

        /// <summary>Pulses so far, including the spawn pulse.</summary>
        public int PulseCount { get; internal set; }

        public bool IsPermanent => Duration <= 0f;

        internal float Accumulator;
        internal uint Seed;

        internal void Clear()
        {
            IsActive = false;
            PayloadAbilityId = null;
            ModifierId = null;
            SourceAbilityId = null;
            ArchetypeId = null;
            Accumulator = 0f;
            PulseCount = 0;
        }
    }
}
