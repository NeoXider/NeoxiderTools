using System;
using System.Collections.Generic;

namespace Neo.Abilities.Fixed
{
    /// <summary>
    ///     Desync check of a fixed system: 64-bit FNV-1a over raw ints only, in the field order of the float
    ///     AbilityStateHasher (version, tick count, clock, units sorted by id with pools, modifiers and their trigger
    ///     counters, slots sorted by id, zones in spawn order, pending projectile cast ids sorted). Allocation-free once
    ///     its sort buffers are warm.
    /// </summary>
    public static class FixedStateHasher
    {
        public const int Version = 1;

        public static ulong Compute(FixedAbilitySystem system)
        {
            return system == null ? 0UL : system.ComputeStateHash();
        }
    }

    public sealed partial class FixedAbilitySystem
    {
        private const ulong FnvOffset = 14695981039346656037UL;
        private const ulong FnvPrime = 1099511628211UL;

        private static readonly Comparison<FixedUnit> UnitOrder = CompareUnits;
        private static readonly Comparison<string> SlotOrder = CompareSlots;
        private static readonly Comparison<uint> CastOrder = CompareCasts;

        private readonly List<FixedUnit> _hashUnits = new List<FixedUnit>(16);
        private readonly List<string> _hashSlots = new List<string>(8);
        private readonly List<FixedModifierInstance> _hashModifiers = new List<FixedModifierInstance>(8);
        private readonly List<uint> _hashPending = new List<uint>(8);

        /// <summary>The state hash of this system (see <see cref="FixedStateHasher" />).</summary>
        public ulong ComputeStateHash()
        {
            ulong hash = FnvOffset;
            hash = Fold(hash, FixedStateHasher.Version);
            hash = Fold(hash, TickCount);
            hash = Fold(hash, TickCount);

            _hashUnits.Clear();
            for (int i = 0; i < _unitOrder.Count; i++)
            {
                _hashUnits.Add(_unitOrder[i]);
            }

            _hashUnits.Sort(UnitOrder);
            hash = Fold(hash, _hashUnits.Count);
            for (int i = 0; i < _hashUnits.Count; i++)
            {
                FixedUnit unit = _hashUnits[i];
                hash = Fold(hash, unit.Id);
                hash = Fold(hash, unit.Team);
                hash = Fold(hash, unit.IsAlive ? 1 : 0);
                hash = Fold(hash, unit.Level);
                hash = Fold(hash, unit.HealthRaw);
                hash = Fold(hash, unit.MaxHealthRaw);
                hash = Fold(hash, unit.ManaRaw);
                hash = Fold(hash, unit.MaxManaRaw);
                hash = FoldModifiers(hash, unit.Id);
                hash = FoldSlots(hash, unit.Id);
            }

            hash = Fold(hash, _zones.Count);
            for (int i = 0; i < _zones.Count; i++)
            {
                FixedZone zone = _zones[i];
                if (!zone.IsActive)
                {
                    continue;
                }

                hash = Fold(hash, zone.Id);
                hash = Fold(hash, zone.Center.X);
                hash = Fold(hash, zone.Center.Y);
                hash = Fold(hash, zone.Center.Z);
                hash = Fold(hash, zone.RadiusRaw);
                hash = Fold(hash, zone.RemainingTicks);
                hash = Fold(hash, zone.PulseCount);
            }

            _hashPending.Clear();
            foreach (KeyValuePair<uint, PendingProjectile> pair in _pendingProjectiles)
            {
                _hashPending.Add(pair.Key);
            }

            _hashPending.Sort(CastOrder);
            hash = Fold(hash, _hashPending.Count);
            for (int i = 0; i < _hashPending.Count; i++)
            {
                hash = Fold(hash, _hashPending[i]);
            }

            _hashUnits.Clear();
            _hashPending.Clear();
            return hash;
        }

        private ulong FoldModifiers(ulong hash, int owner)
        {
            Modifiers.GetModifiers(owner, _hashModifiers);
            hash = Fold(hash, _hashModifiers.Count);
            for (int i = 0; i < _hashModifiers.Count; i++)
            {
                FixedModifierInstance m = _hashModifiers[i];
                hash = FoldString(hash, m.Blueprint.Id);
                hash = Fold(hash, m.Caster);
                hash = Fold(hash, m.Stacks);
                hash = Fold(hash, m.AbilityLevel);
                hash = Fold(hash, m.RemainingTicks);
                hash = Fold(hash, m.ShieldConsumedRaw);
                if (m.ReactionStates != null)
                {
                    for (int r = 0; r < m.ReactionStates.Length; r++)
                    {
                        hash = Fold(hash, m.ReactionStates[r].Counter);
                        hash = Fold(hash, m.ReactionStates[r].Fired);
                    }
                }
            }

            _hashModifiers.Clear();
            return hash;
        }

        private ulong FoldSlots(ulong hash, int owner)
        {
            if (!_slots.TryGetValue(owner, out Dictionary<string, FixedAbilitySlot> slots))
            {
                return Fold(hash, 0);
            }

            _hashSlots.Clear();
            foreach (KeyValuePair<string, FixedAbilitySlot> pair in slots)
            {
                _hashSlots.Add(pair.Key);
            }

            _hashSlots.Sort(SlotOrder);
            hash = Fold(hash, _hashSlots.Count);
            for (int i = 0; i < _hashSlots.Count; i++)
            {
                FixedAbilitySlot slot = slots[_hashSlots[i]];
                hash = FoldString(hash, _hashSlots[i]);
                hash = Fold(hash, slot.Level);
                hash = Fold(hash, slot.Charges);
                hash = Fold(hash, slot.CooldownRemainingTicks);
            }

            _hashSlots.Clear();
            return hash;
        }

        private static ulong Fold(ulong hash, long value)
        {
            unchecked
            {
                for (int i = 0; i < 8; i++)
                {
                    hash ^= (byte)(value >> (i * 8));
                    hash *= FnvPrime;
                }

                return hash;
            }
        }

        private static ulong FoldString(ulong hash, string value)
        {
            if (value == null)
            {
                return Fold(hash, -1);
            }

            unchecked
            {
                for (int i = 0; i < value.Length; i++)
                {
                    hash ^= (ulong)FixedIdComparer.Fold(value[i]);
                    hash *= FnvPrime;
                }

                return Fold(hash, value.Length);
            }
        }

        private static int CompareUnits(FixedUnit x, FixedUnit y)
        {
            return x.Id.CompareTo(y.Id);
        }

        // WHY: ids are case-insensitive; ordering by folded characters keeps peers that registered "Fireball" and
        // "fireball" in the same order without culture tables.
        private static int CompareSlots(string x, string y)
        {
            int length = x.Length < y.Length ? x.Length : y.Length;
            for (int i = 0; i < length; i++)
            {
                int a = FixedIdComparer.Fold(x[i]);
                int b = FixedIdComparer.Fold(y[i]);
                if (a != b)
                {
                    return a < b ? -1 : 1;
                }
            }

            return x.Length.CompareTo(y.Length);
        }

        private static int CompareCasts(uint x, uint y)
        {
            return x.CompareTo(y);
        }
    }
}
