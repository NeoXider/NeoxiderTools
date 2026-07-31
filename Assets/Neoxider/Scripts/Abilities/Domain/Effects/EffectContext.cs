using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Abilities
{
    /// <summary>
    ///     Execution context for a list of effect nodes: who casts, whom it hit, where, with which RNG.
    ///     One context accompanies a whole cast (or tick / event reaction) so depth limits and
    ///     deterministic rolls stay consistent.
    /// </summary>
    public sealed class EffectContext
    {
        /// <summary>Hard cap for nested effect execution (event reactions triggering reactions...).</summary>
        public const int MaxDepth = 3;

        public EffectContext(AbilitySystem system, UnitId caster, string abilityId, IRandomSource random)
        {
            System = system;
            Caster = caster;
            AbilityId = abilityId;
            Random = random ?? new XorShiftRandom(1);
            PrimaryTargets = new List<UnitId>(4);
        }

        public AbilitySystem System { get; }
        public UnitId Caster { get; }
        public string AbilityId { get; }
        public IRandomSource Random { get; }

        /// <summary>Cast id this context executes under (0 for ticks/reactions outside a cast).</summary>
        public uint CastId { get; set; }

        /// <summary>
        ///     Ability level driving leveled-value resolution. Set from the casting slot's level (or the
        ///     level captured on a modifier for ticks/reactions). Defaults to 1 so flat values are unaffected.
        /// </summary>
        public int AbilityLevel { get; set; } = 1;

        /// <summary>Named special values of the current ability (for <see cref="EffectNodeData.AmountKey" />). May be null.</summary>
        public List<AbilitySpecialValue> Specials { get; set; }

        /// <summary>Resolves a named special value by name (case-insensitive). Returns false when absent.</summary>
        public bool TryGetSpecial(string name, out LeveledValue value)
        {
            if (Specials != null && !string.IsNullOrEmpty(name))
            {
                for (int i = 0; i < Specials.Count; i++)
                {
                    if (string.Equals(Specials[i].Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        value = Specials[i].Value;
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }

        /// <summary>Resolved primary targets of the context (cast targets / modifier owner / event source).</summary>
        public List<UnitId> PrimaryTargets { get; }

        /// <summary>Target point for point/area casts; falls back to the first target's position.</summary>
        public Vector3 TargetPoint { get; set; }

        public bool HasTargetPoint { get; set; }

        /// <summary>Current nesting depth (0 = direct cast). Incremented for reactions.</summary>
        public int Depth { get; set; }

        public EffectContext CreateNested(UnitId newCaster, string abilityId, List<UnitId> targets)
        {
            var nested = new EffectContext(System, newCaster, abilityId, Random)
            {
                Depth = Depth + 1,
                TargetPoint = TargetPoint,
                HasTargetPoint = HasTargetPoint,
                AbilityLevel = AbilityLevel,
                Specials = Specials
            };
            if (targets != null)
            {
                nested.PrimaryTargets.AddRange(targets);
            }

            return nested;
        }
    }
}
