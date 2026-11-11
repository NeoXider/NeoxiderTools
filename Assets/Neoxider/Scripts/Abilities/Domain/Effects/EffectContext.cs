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
    /// <remarks>
    ///     Contexts the <see cref="AbilitySystem" /> creates itself (casts, projectile hits, modifier ticks,
    ///     event reactions) are pooled and recycled as soon as their effects finish. An
    ///     <see cref="IEffectOperation" /> must not keep such a context, its <see cref="PrimaryTargets" />
    ///     or its <see cref="Random" /> after <see cref="IEffectOperation.Execute" /> returns; copy the values
    ///     it needs instead. <see cref="CreateNested" /> children of such a context share its RNG and expire
    ///     with it. Once expired, <see cref="IsReleased" /> is true and
    ///     <see cref="AbilitySystem.ExecuteEffects" /> ignores the context. Contexts built with the public
    ///     constructor (and their nested children) are never pooled.
    /// </remarks>
    public sealed class EffectContext
    {
        /// <summary>Hard cap for nested effect execution (event reactions triggering reactions...).</summary>
        public const int MaxDepth = 3;

        // WHY: a pooled context owns one reseedable RNG so recycling it allocates nothing.
        private IReseedableRandomSource _ownedRandom;
        private IRandomSourceFactory _ownedRandomFactory;

        // WHY: a pooled context bumps _lease every time it is recycled. Contexts derived from one lease
        // (the pooled context itself and its CreateNested children) remember the lease they belong to,
        // so a kept context is detectably stale instead of silently running with another cast's state.
        private EffectContext _leaseOwner;
        private int _ownerLease;
        private int _lease;

        // WHY: what the cast paid drives spent-scaled amounts. A pooled context owns and reuses its list (allocated on
        // first use); a CreateNested child borrows its parent's and copies it only if it records its own amounts.
        private List<AbilitySpentResource> _spent;
        private bool _ownsSpent;

        public EffectContext(AbilitySystem system, UnitId caster, string abilityId, IRandomSource random)
        {
            System = system;
            Caster = caster;
            AbilityId = abilityId;
            Random = random ?? new XorShiftRandom(1);
            PrimaryTargets = new List<UnitId>(4);
        }

        private EffectContext()
        {
            PrimaryTargets = new List<UnitId>(4);
        }

        public AbilitySystem System { get; private set; }
        public UnitId Caster { get; private set; }
        public string AbilityId { get; private set; }
        public IRandomSource Random { get; private set; }

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
                    if (AbilityIdComparer.Same(Specials[i].Name, name))
                    {
                        value = Specials[i].Value;
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }

        /// <summary>
        ///     Number of resource amounts the cast of this context paid: cast and impact effects, projectile hits,
        ///     payloads, and ticks / reactions of modifiers whose effects scale with a spent resource. 0 otherwise.
        /// </summary>
        public int SpentCount => _spent != null ? _spent.Count : 0;

        /// <summary>The <paramref name="index" />-th paid amount (0 ≤ index &lt; <see cref="SpentCount" />).</summary>
        public AbilitySpentResource GetSpentAt(int index)
        {
            return _spent[index];
        }

        /// <summary>Total amount of <paramref name="resourceId" /> (case-insensitive) the cast paid; 0 when none.</summary>
        public float GetSpent(string resourceId)
        {
            if (_spent == null || string.IsNullOrEmpty(resourceId))
            {
                return 0f;
            }

            float total = 0f;
            for (int i = 0; i < _spent.Count; i++)
            {
                if (AbilityIdComparer.Same(_spent[i].ResourceId, resourceId))
                {
                    total += _spent[i].Amount;
                }
            }

            return total;
        }

        /// <summary>
        ///     Records a paid amount. The system does it for every cast; hosts running effects directly may record one so
        ///     spent-scaled nodes see it. Non-positive amounts, non-finite amounts and empty ids are ignored.
        /// </summary>
        public void AddSpent(string resourceId, float amount)
        {
            if (string.IsNullOrEmpty(resourceId) || !(amount > 0f) || float.IsInfinity(amount))
            {
                return;
            }

            if (!_ownsSpent)
            {
                List<AbilitySpentResource> own = new List<AbilitySpentResource>(_spent != null ? _spent.Count + 2 : 2);
                if (_spent != null)
                {
                    own.AddRange(_spent);
                }

                _spent = own;
                _ownsSpent = true;
            }

            _spent.Add(new AbilitySpentResource(resourceId, amount));
        }

        /// <summary>Resolved primary targets of the context (cast targets / modifier owner / event source).</summary>
        public List<UnitId> PrimaryTargets { get; }

        /// <summary>Target point for point/area casts; falls back to the first target's position.</summary>
        public Vector3 TargetPoint { get; set; }

        public bool HasTargetPoint { get; set; }

        /// <summary>Current nesting depth (0 = direct cast). Incremented for reactions.</summary>
        public int Depth { get; set; }

        /// <summary>What started this execution: a cast, a projectile hit, a modifier tick, a reaction, a zone pulse.</summary>
        public EffectOrigin Origin { get; set; }

        /// <summary>
        ///     True for cast and projectile-hit executions: their damage is a direct hit (<see cref="DamageFlags.Hit" />)
        ///     and publishes attack_landed / attack_received.
        /// </summary>
        public bool IsDirectHit => Origin == EffectOrigin.Cast || Origin == EffectOrigin.ProjectileHit;

        /// <summary>Damage flags the built-in damage ops pass for this context.</summary>
        public DamageFlags HitFlags => IsDirectHit ? DamageFlags.Hit : DamageFlags.None;

        /// <summary>
        ///     Stacks of the modifier instance running these effects (ticks and reactions); 1 otherwise. Drives
        ///     <see cref="EffectNodeData.AmountPerStack" />.
        /// </summary>
        public int SourceStacks { get; set; } = 1;

        /// <summary>
        ///     The damage application in flight while a before_deal_damage / before_take_damage reaction runs, else null.
        ///     modify_damage and reflect adjust it.
        /// </summary>
        public DamageContext Damage { get; set; }

        /// <summary>
        ///     True when the system-created context this one belongs to has finished its effects: a pooled
        ///     context while it waits in the pool for its next use, a <see cref="CreateNested" /> child of it
        ///     from then on. A released context must not be used; <see cref="AbilitySystem.ExecuteEffects" />
        ///     ignores it. Always false for contexts built with the public constructor and their children.
        /// </summary>
        public bool IsReleased => _leaseOwner != null && _leaseOwner._lease != _ownerLease;

        /// <summary>
        ///     Builds a child context one level deeper that shares this context's RNG, cast id, point,
        ///     level, specials and spent amounts. Use it when an operation runs further effect nodes on its own. A child
        ///     of a system-created context has the same lifetime as its parent.
        /// </summary>
        public EffectContext CreateNested(UnitId newCaster, string abilityId, List<UnitId> targets)
        {
            EffectContext nested = new EffectContext(System, newCaster, abilityId, Random)
            {
                Depth = Depth + 1,
                CastId = CastId,
                TargetPoint = TargetPoint,
                HasTargetPoint = HasTargetPoint,
                AbilityLevel = AbilityLevel,
                Specials = Specials,
                Origin = Origin,
                SourceStacks = SourceStacks,
                Damage = Damage
            };
            nested._leaseOwner = _leaseOwner;
            nested._ownerLease = _ownerLease;
            nested._spent = _spent;
            nested._ownsSpent = false;
            if (targets != null)
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    nested.PrimaryTargets.Add(targets[i]);
                }
            }

            return nested;
        }

        internal static EffectContext CreatePooled()
        {
            return new EffectContext();
        }

        /// <summary>Prepares a pooled context exactly like a fresh one seeded with <paramref name="seed" />.</summary>
        internal void ResetPooled(AbilitySystem system, UnitId caster, string abilityId, uint seed)
        {
            IRandomSourceFactory factory = system.Rules.Random;
            if (_ownedRandom == null || !ReferenceEquals(_ownedRandomFactory, factory))
            {
                // WHY: a swapped random rule takes effect on the next use of every pooled context.
                _ownedRandom = factory.Create(seed) ?? new XorShiftRandom(seed);
                _ownedRandomFactory = factory;
            }
            else
            {
                _ownedRandom.Reseed(seed);
            }

            System = system;
            Caster = caster;
            AbilityId = abilityId;
            Random = _ownedRandom;
            CastId = 0;
            AbilityLevel = 1;
            Specials = null;
            TargetPoint = default;
            HasTargetPoint = false;
            Depth = 0;
            Origin = EffectOrigin.Direct;
            SourceStacks = 1;
            Damage = null;
            PrimaryTargets.Clear();
            ResetSpent();
            _leaseOwner = this;
            _ownerLease = _lease;
        }

        private void ResetSpent()
        {
            if (_ownsSpent)
            {
                _spent.Clear();
            }
            else
            {
                _spent = null;
            }
        }

        /// <summary>
        ///     Drops references held by a pooled context before it waits for its next use and expires every
        ///     context of the finished lease.
        /// </summary>
        internal void ClearPooled()
        {
            unchecked
            {
                _lease++;
            }

            AbilityId = null;
            Specials = null;
            Damage = null;
            PrimaryTargets.Clear();
            ResetSpent();
        }
    }

    /// <summary>What started an effect execution.</summary>
    public enum EffectOrigin
    {
        /// <summary>A host or custom op called <see cref="AbilitySystem.ExecuteEffects" /> directly.</summary>
        Direct = 0,

        /// <summary>Cast or instant impact effects of an accepted cast.</summary>
        Cast = 1,

        /// <summary>Impact effects of a reported projectile hit.</summary>
        ProjectileHit = 2,

        /// <summary>Interval tick effects of a modifier.</summary>
        ModifierTick = 3,

        /// <summary>A modifier reaction or trigger.</summary>
        Reaction = 4,

        /// <summary>A zone pulse.</summary>
        Zone = 5
    }
}
