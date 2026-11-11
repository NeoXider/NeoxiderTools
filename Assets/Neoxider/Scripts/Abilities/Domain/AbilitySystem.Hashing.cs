using System;
using System.Collections.Generic;

namespace Neo.Abilities
{
    /// <summary>
    ///     Desync check for deterministic hosts: a 64-bit FNV-1a hash of the simulation state of an
    ///     <see cref="AbilitySystem" /> (clock, units, pools, modifiers with their trigger counters, slots, zones and
    ///     pending projectiles) in a fixed, sorted order. Peers that ran the same inputs produce the same value.
    ///     Allocation-free once its sort buffers are warm.
    /// </summary>
    public static class AbilityStateHasher
    {
        /// <summary>Changes whenever the hashed fields or their order change; part of every hash.</summary>
        public const int Version = 1;

        public static ulong Compute(AbilitySystem system)
        {
            return system == null ? 0UL : system.ComputeStateHash();
        }
    }

    public sealed partial class AbilitySystem
    {
        private const ulong FnvOffset = 14695981039346656037UL;
        private const ulong FnvPrime = 1099511628211UL;

        // WHY: List.Sort(IComparer) wraps the comparer in a new delegate on Unity's Mono; cached Comparison
        // delegates keep hashing allocation-free.
        private static readonly Comparison<UnitId> UnitIdOrder = CompareUnitIds;
        private static readonly Comparison<string> SlotIdOrder = CompareSlotIds;
        private static readonly Comparison<uint> CastIdOrder = CompareCastIds;

        private readonly List<UnitId> _hashUnits = new List<UnitId>(16);
        private readonly List<string> _hashSlots = new List<string>(8);
        private readonly List<ModifierInstance> _hashModifiers = new List<ModifierInstance>(8);
        private readonly List<uint> _hashPending = new List<uint>(8);

        internal ulong ComputeStateHash()
        {
            FixedPointNumeric fixedPoint = Rules.Numeric as FixedPointNumeric;
            ulong hash = FnvOffset;
            hash = Fold(hash, AbilityStateHasher.Version);
            hash = Fold(hash, TickCount);
            hash = FoldFloat(hash, _time, fixedPoint);

            _hashUnits.Clear();
            foreach (KeyValuePair<UnitId, AbilityUnit> pair in _units)
            {
                _hashUnits.Add(pair.Key);
            }

            _hashUnits.Sort(UnitIdOrder);
            hash = Fold(hash, _hashUnits.Count);
            for (int i = 0; i < _hashUnits.Count; i++)
            {
                AbilityUnit unit = _units[_hashUnits[i]];
                hash = Fold(hash, unit.Id.Value);
                hash = Fold(hash, unit.Team.Value);
                hash = Fold(hash, unit.IsAlive ? 1 : 0);
                hash = Fold(hash, unit.Level);
                hash = FoldFloat(hash, unit.Resources.GetCurrent(AbilityResourceIds.Health), fixedPoint);
                hash = FoldFloat(hash, unit.Resources.GetMax(AbilityResourceIds.Health), fixedPoint);
                hash = FoldFloat(hash, unit.Resources.GetCurrent(AbilityResourceIds.Mana), fixedPoint);
                hash = FoldFloat(hash, unit.Resources.GetMax(AbilityResourceIds.Mana), fixedPoint);
                hash = FoldModifiers(hash, unit.Id, fixedPoint);
                hash = FoldSlots(hash, unit.Id, fixedPoint);
            }

            hash = Fold(hash, _zones.Count);
            for (int i = 0; i < _zones.Count; i++)
            {
                AbilityZone zone = _zones[i];
                if (!zone.IsActive)
                {
                    continue;
                }

                hash = Fold(hash, zone.Id);
                hash = FoldFloat(hash, zone.Center.x, fixedPoint);
                hash = FoldFloat(hash, zone.Center.y, fixedPoint);
                hash = FoldFloat(hash, zone.Center.z, fixedPoint);
                hash = FoldFloat(hash, zone.Radius, fixedPoint);
                hash = FoldFloat(hash, zone.Remaining, fixedPoint);
                hash = Fold(hash, zone.PulseCount);
            }

            _hashPending.Clear();
            foreach (KeyValuePair<uint, PendingProjectileCast> pair in _pendingProjectiles)
            {
                _hashPending.Add(pair.Key);
            }

            _hashPending.Sort(CastIdOrder);
            hash = Fold(hash, _hashPending.Count);
            for (int i = 0; i < _hashPending.Count; i++)
            {
                hash = Fold(hash, _hashPending[i]);
            }

            _hashUnits.Clear();
            _hashPending.Clear();
            return hash;
        }

        private ulong FoldModifiers(ulong hash, UnitId owner, FixedPointNumeric fixedPoint)
        {
            Modifiers.GetModifiers(owner, _hashModifiers);
            hash = Fold(hash, _hashModifiers.Count);
            for (int i = 0; i < _hashModifiers.Count; i++)
            {
                ModifierInstance m = _hashModifiers[i];
                hash = FoldString(hash, m.Blueprint.Id);
                hash = Fold(hash, m.Caster.Value);
                hash = Fold(hash, m.Stacks);
                hash = Fold(hash, m.AbilityLevel);
                hash = FoldFloat(hash, m.RemainingDuration, fixedPoint);
                hash = FoldFloat(hash, m.ShieldConsumed, fixedPoint);
                // WHY: only spent-scaled modifiers capture paid amounts, so every other state hash is unchanged.
                for (int s = 0; s < m.SpentCount; s++)
                {
                    AbilitySpentResource paid = m.GetSpentAt(s);
                    hash = FoldString(hash, paid.ResourceId);
                    hash = FoldFloat(hash, paid.Amount, fixedPoint);
                }

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

        private ulong FoldSlots(ulong hash, UnitId owner, FixedPointNumeric fixedPoint)
        {
            if (!_slots.TryGetValue(owner, out Dictionary<string, AbilitySlot> slots))
            {
                return Fold(hash, 0);
            }

            _hashSlots.Clear();
            foreach (KeyValuePair<string, AbilitySlot> pair in slots)
            {
                _hashSlots.Add(pair.Key);
            }

            _hashSlots.Sort(SlotIdOrder);
            hash = Fold(hash, _hashSlots.Count);
            for (int i = 0; i < _hashSlots.Count; i++)
            {
                AbilitySlot slot = slots[_hashSlots[i]];
                hash = FoldString(hash, _hashSlots[i]);
                hash = Fold(hash, slot.Level);
                hash = Fold(hash, slot.Charges);
                hash = FoldFloat(hash, slot.CooldownRemaining, fixedPoint);
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

        private static ulong FoldFloat(ulong hash, float value, FixedPointNumeric fixedPoint)
        {
            // WHY: on the fixed grid the raw integer is the value; in float mode the exact bits are.
            return fixedPoint != null
                ? Fold(hash, fixedPoint.ToRaw(value))
                : Fold(hash, BitConverter.SingleToInt32Bits(value));
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
                    char c = value[i];
                    if (c >= 'a' && c <= 'z')
                    {
                        c = (char)(c - 32);
                    }
                    else if (c > 127)
                    {
                        c = char.ToUpperInvariant(c);
                    }

                    hash ^= c;
                    hash *= FnvPrime;
                }

                return Fold(hash, value.Length);
            }
        }

        private static int CompareUnitIds(UnitId x, UnitId y)
        {
            return x.Value.CompareTo(y.Value);
        }

        // WHY: ids are case-insensitive in the domain; ordering ignoring case keeps peers that registered "Fireball"
        // and "fireball" in the same order.
        private static int CompareSlotIds(string x, string y)
        {
            return string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
        }

        private static int CompareCastIds(uint x, uint y)
        {
            return x.CompareTo(y);
        }
    }
}
