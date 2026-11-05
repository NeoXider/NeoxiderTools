using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Neo.Abilities
{
    /// <summary>What a <see cref="AbilityTraceEntry" /> records.</summary>
    public enum AbilityTraceKind
    {
        /// <summary>An event was published (Id = event id, Unit = target, Other = source).</summary>
        Event = 0,

        /// <summary>A modifier reaction fired (Id = modifier id, Index = reaction index, Reason = trigger).</summary>
        Reaction = 1,

        /// <summary>An effect op ran (Id = op id, Detail = ability id, Index = resolved target count).</summary>
        Effect = 2,

        /// <summary>A cast was accepted (Id = ability id, Unit = caster, Other = target unit).</summary>
        Cast = 3,

        /// <summary>A cast was rejected (Id = ability id, Reason = failure reason).</summary>
        CastFailed = 4,

        /// <summary>A modifier ticked (Id = modifier id, Unit = owner).</summary>
        ModifierTick = 5,

        /// <summary>A zone spawned, pulsed or expired (Index = zone id, Reason = phase).</summary>
        Zone = 6,

        /// <summary>A before hook cancelled damage.</summary>
        DamageCancelled = 7,

        /// <summary>A depth cap stopped a nested execution.</summary>
        DepthCap = 8,

        /// <summary>A trigger was evaluated but did not fire (charges spent, cooldown, Nth count).</summary>
        TriggerSkipped = 9,

        /// <summary>A projectile of a cast reported a hit (Index = cast id, Other = hit unit).</summary>
        ProjectileHit = 10
    }

    /// <summary>
    ///     One record of the <see cref="AbilityTrace" /> ring buffer. <see cref="Parent" /> is the sequence of the
    ///     record that caused this one (0 for a root such as a cast or a tick), so a chain of parents answers
    ///     "why did this fire". Strings are references to existing ids; recording never formats text.
    /// </summary>
    public readonly struct AbilityTraceEntry
    {
        public readonly long Sequence;
        public readonly long Parent;
        public readonly AbilityTraceKind Kind;
        public readonly float Time;
        public readonly long Tick;
        public readonly UnitId Unit;
        public readonly UnitId Other;
        public readonly string Id;
        public readonly string Detail;
        public readonly int Index;
        public readonly float Amount;
        public readonly string Reason;

        public AbilityTraceEntry(long sequence, long parent, AbilityTraceKind kind, float time, long tick, UnitId unit,
            UnitId other, string id, string detail, int index, float amount, string reason)
        {
            Sequence = sequence;
            Parent = parent;
            Kind = kind;
            Time = time;
            Tick = tick;
            Unit = unit;
            Other = other;
            Id = id;
            Detail = detail;
            Index = index;
            Amount = amount;
            Reason = reason;
        }

        public bool IsValid => Sequence > 0;

        /// <summary>One readable line, e.g. "#12 reaction 'mark'[0] on Unit(2): every 3rd attack_landed".</summary>
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder(96);
            builder.Append('#').Append(Sequence.ToString(CultureInfo.InvariantCulture)).Append(' ');
            switch (Kind)
            {
                case AbilityTraceKind.Event:
                    builder.Append("event ").Append(Id).Append(" on ").Append(Unit);
                    if (Other.IsValid)
                    {
                        builder.Append(" from ").Append(Other);
                    }

                    break;
                case AbilityTraceKind.Reaction:
                    builder.Append("reaction '").Append(Id).Append("'[").Append(Index.ToString(CultureInfo.InvariantCulture))
                        .Append("] on ").Append(Unit);
                    break;
                case AbilityTraceKind.Effect:
                    builder.Append("effect ").Append(Id).Append(" x").Append(Index.ToString(CultureInfo.InvariantCulture))
                        .Append(" by ").Append(Unit);
                    break;
                case AbilityTraceKind.Cast:
                    builder.Append("cast '").Append(Id).Append("' by ").Append(Unit);
                    break;
                case AbilityTraceKind.CastFailed:
                    builder.Append("cast '").Append(Id).Append("' by ").Append(Unit).Append(" rejected");
                    break;
                case AbilityTraceKind.ProjectileHit:
                    builder.Append("projectile of '").Append(Id).Append("' hit ").Append(Other);
                    break;
                case AbilityTraceKind.ModifierTick:
                    builder.Append("tick of '").Append(Id).Append("' on ").Append(Unit);
                    break;
                case AbilityTraceKind.Zone:
                    builder.Append("zone #").Append(Index.ToString(CultureInfo.InvariantCulture));
                    if (!string.IsNullOrEmpty(Id))
                    {
                        builder.Append(" '").Append(Id).Append('\'');
                    }

                    break;
                default:
                    builder.Append(Kind.ToString()).Append(' ').Append(Id).Append(" on ").Append(Unit);
                    break;
            }

            if (Amount != 0f)
            {
                builder.Append(" amount=").Append(Amount.ToString("0.###", CultureInfo.InvariantCulture));
            }

            if (!string.IsNullOrEmpty(Reason))
            {
                builder.Append(": ").Append(Reason);
            }

            return builder.ToString();
        }
    }

    /// <summary>
    ///     Runtime ring buffer of what the ability system did and why: published events, fired reactions with their
    ///     trigger, effect ops, casts, ticks, zones and cancellations, each linked to its cause. Disabled by default;
    ///     enable it for debugging or a "why did this fire" panel. Recording is allocation-free; only the explaining
    ///     helpers build strings.
    /// </summary>
    public sealed class AbilityTrace
    {
        private AbilityTraceEntry[] _entries;
        private long _next = 1;

        public AbilityTrace(int capacity = 256)
        {
            _entries = new AbilityTraceEntry[capacity < 8 ? 8 : capacity];
        }

        /// <summary>When false (default) nothing is recorded and every record call costs one branch.</summary>
        public bool Enabled { get; set; }

        /// <summary>Entries kept; older ones are overwritten.</summary>
        public int Capacity => _entries.Length;

        /// <summary>Sequence the next record will get (records so far + 1).</summary>
        public long NextSequence => _next;

        /// <summary>Number of entries currently held (at most <see cref="Capacity" />).</summary>
        public int Count => (int)Math.Min(_next - 1, _entries.Length);

        /// <summary>Resizes the buffer and drops every record.</summary>
        public void SetCapacity(int capacity)
        {
            _entries = new AbilityTraceEntry[capacity < 8 ? 8 : capacity];
            _next = 1;
        }

        /// <summary>Drops every record; sequences keep counting.</summary>
        public void Clear()
        {
            Array.Clear(_entries, 0, _entries.Length);
        }

        internal long Record(long parent, AbilityTraceKind kind, float time, long tick, UnitId unit, UnitId other,
            string id, string detail, int index, float amount, string reason)
        {
            long sequence = _next++;
            _entries[(int)(sequence % _entries.Length)] = new AbilityTraceEntry(sequence, parent, kind, time, tick, unit,
                other, id, detail, index, amount, reason);
            return sequence;
        }

        /// <summary>The record with this sequence, while it is still in the buffer.</summary>
        public bool TryGet(long sequence, out AbilityTraceEntry entry)
        {
            if (sequence > 0 && sequence < _next)
            {
                entry = _entries[(int)(sequence % _entries.Length)];
                if (entry.Sequence == sequence)
                {
                    return true;
                }
            }

            entry = default;
            return false;
        }

        /// <summary>Clears <paramref name="results" /> and fills it with the held records, oldest first.</summary>
        public void CopyTo(List<AbilityTraceEntry> results)
        {
            results.Clear();
            long first = Math.Max(1, _next - _entries.Length);
            for (long sequence = first; sequence < _next; sequence++)
            {
                if (TryGet(sequence, out AbilityTraceEntry entry))
                {
                    results.Add(entry);
                }
            }
        }

        /// <summary>The newest held record of a kind (and id, when given), or false.</summary>
        public bool TryFindLatest(AbilityTraceKind kind, string id, out AbilityTraceEntry entry)
        {
            long first = Math.Max(1, _next - _entries.Length);
            for (long sequence = _next - 1; sequence >= first; sequence--)
            {
                if (TryGet(sequence, out entry) && entry.Kind == kind &&
                    (id == null || AbilityIdComparer.Same(entry.Id, id)))
                {
                    return true;
                }
            }

            entry = default;
            return false;
        }

        /// <summary>
        ///     Explains a record by walking its causes: the record first, then "because" each parent still in the
        ///     buffer, up to <paramref name="maxDepth" /> lines.
        /// </summary>
        public string Explain(long sequence, int maxDepth = 12)
        {
            if (!TryGet(sequence, out AbilityTraceEntry entry))
            {
                return "#" + sequence.ToString(CultureInfo.InvariantCulture) + " is no longer in the trace buffer.";
            }

            StringBuilder builder = new StringBuilder(256);
            builder.Append(entry.ToString());
            int depth = 0;
            while (entry.Parent > 0 && depth++ < maxDepth)
            {
                long parent = entry.Parent;
                if (!TryGet(parent, out entry))
                {
                    builder.Append("\n  because #").Append(parent.ToString(CultureInfo.InvariantCulture))
                        .Append(" (overwritten)");
                    break;
                }

                builder.Append("\n  because ").Append(entry.ToString());
            }

            return builder.ToString();
        }

        /// <summary>The newest records as text, newest last.</summary>
        public string Dump(int maxLines = 32)
        {
            StringBuilder builder = new StringBuilder(maxLines * 64);
            long first = Math.Max(Math.Max(1, _next - _entries.Length), _next - maxLines);
            for (long sequence = first; sequence < _next; sequence++)
            {
                if (TryGet(sequence, out AbilityTraceEntry entry))
                {
                    if (builder.Length > 0)
                    {
                        builder.Append('\n');
                    }

                    builder.Append(entry.ToString());
                }
            }

            return builder.ToString();
        }
    }
}
