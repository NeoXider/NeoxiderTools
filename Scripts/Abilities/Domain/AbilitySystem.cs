using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Abilities
{
    /// <summary>
    ///     The façade of the ability domain: units, modifiers, catalogs, effect execution and the
    ///     cast pipeline. Pure C# and deterministic — drive it with <see cref="Tick" />; the scene
    ///     layer (<c>AbilitySystemBehaviour</c>) owns exactly one instance per world.
    ///     Everything observable flows through <see cref="Events" /> (receipt stream for UI,
    ///     presentation and network replication).
    /// </summary>
    public sealed partial class AbilitySystem : IAbilityEventInterceptor
    {
        private readonly Dictionary<UnitId, AbilityUnit> _units = new Dictionary<UnitId, AbilityUnit>();

        private readonly Dictionary<string, ModifierBlueprint> _modifierCatalog =
            new Dictionary<string, ModifierBlueprint>(AbilityIdComparer.Instance);

        private readonly Dictionary<string, AbilityBlueprint> _abilityCatalog =
            new Dictionary<string, AbilityBlueprint>(AbilityIdComparer.Instance);

        private readonly Dictionary<UnitId, Dictionary<string, AbilitySlot>> _slots =
            new Dictionary<UnitId, Dictionary<string, AbilitySlot>>();

        private readonly Dictionary<uint, PendingProjectileCast> _pendingProjectiles =
            new Dictionary<uint, PendingProjectileCast>();

        // WHY: effect execution re-enters synchronously through event-driven modifier reactions
        // (depth-capped), so scratch lists are pooled per nesting level — a single shared list
        // would be cleared by an inner execution while the outer loop still iterates it.
        private readonly List<List<ModifierInstance>> _reactionScratchPool =
            new List<List<ModifierInstance>>(EffectContext.MaxDepth + 1);

        private readonly List<List<UnitId>> _targetScratchPool =
            new List<List<UnitId>>(EffectContext.MaxDepth + 1);

        // WHY: payment callbacks may cast again before the outer payment loop finishes.
        private readonly List<List<AbilityCost>> _costScratchPool = new List<List<AbilityCost>>(2);

        // WHY: what each cast depth paid, handed to its effect context; nested casts use the next depth's list.
        private readonly List<List<AbilitySpentResource>> _spentScratchPool =
            new List<List<AbilitySpentResource>>(MaxCastDepth + 1);

        // WHY: casts, projectile hits, modifier ticks and reactions each need a context; recycling them
        // keeps survivor-scale combat (hundreds of ticks and hits per second) free of per-event garbage.
        private readonly List<EffectContext> _contextPool = new List<EffectContext>(8);

        // WHY: projectile weapons are the most common survivor attack; recycling their pending-cast
        // records keeps projectile casts as garbage-free as instant ones.
        private readonly List<PendingProjectileCast> _pendingPool = new List<PendingProjectileCast>(16);

        private readonly List<UnitId> _tickUnitScratch = new List<UnitId>(16);
        private readonly List<uint> _pendingPruneScratch = new List<uint>(8);

        private const string ReleasedContextWarningKey = "Neo.Abilities.ReleasedEffectContext";

        /// <summary>Maximum nesting of casts started from effects (cast_ability inside a cast...).</summary>
        public const int MaxCastDepth = 4;

        /// <summary>
        ///     Minimum reach in world units of a <see cref="TargetingMode.Direction" /> cast whose ability Range is
        ///     0 (unlimited): its target point and its projectile's <see cref="SpawnRequest.Direction" /> end at the
        ///     length of the aim vector, but never closer than this (so a unit-length aim such as
        ///     <c>transform.forward</c> still flies this far).
        /// </summary>
        public const float DefaultDirectionReach = 10f;

        private IAbilityWorldAdapter _world = NullWorldAdapter.Instance;
        private uint _nextUnitId = 1;
        private uint _nextCastId = 1;
        private int _executionDepth;
        private int _reactionDepth;
        private int _costDepth;
        private int _castDepth;
        private float _time;

        // WHY: sequence of the trace record whose effects are running; new records name it as their cause.
        private long _traceCause;

        public AbilitySystem()
        {
            Rules = new AbilityRules();
            Trace = new AbilityTrace();
            Modifiers = new ModifierEngine { Rules = Rules };
            Damage = new DamagePipeline(this);
            Events = new AbilityEventBus(this);
            Ops = new EffectOpRegistry();

            Modifiers.Applied += OnModifierApplied;
            Modifiers.Removed += OnModifierRemoved;
            Modifiers.TickDue += OnModifierTick;

            DefaultEffectOps.RegisterAll(Ops);
        }

        /// <summary>Live modifiers of every unit: apply, remove, dispel, tick and aggregation feeds.</summary>
        public ModifierEngine Modifiers { get; }

        /// <summary>Replaceable rules of this system: formulas, stacking, aggregation, targeting, time, numbers, random.</summary>
        public AbilityRules Rules { get; }

        /// <summary>Ring buffer answering "why did this fire" (disabled by default: set <c>Trace.Enabled</c>).</summary>
        public AbilityTrace Trace { get; }

        /// <summary>The damage pipeline of this system (hooks, formula, shields, receipts, reflection).</summary>
        public DamagePipeline Damage { get; }

        /// <summary>
        ///     The damage application whose before_deal_damage / before_take_damage hooks are running, else null.
        ///     Event handlers of those hooks adjust it; never keep it.
        /// </summary>
        public DamageContext ActiveDamage => Damage.Active;

        /// <summary>Domain clock in seconds: the sum of every accepted <see cref="Tick" /> step.</summary>
        public float ElapsedTime => _time;

        /// <summary>Number of accepted <see cref="Tick" /> steps.</summary>
        public long TickCount { get; private set; }

        /// <summary>Receipt stream of this system (damage, heals, casts, modifiers, deaths).</summary>
        public AbilityEventBus Events { get; }

        /// <summary>Open registry of effect operations; built-ins are installed by the constructor.</summary>
        public EffectOpRegistry Ops { get; }

        /// <summary>World seam (positions, spatial queries, spawns). Never null.</summary>
        public IAbilityWorldAdapter World
        {
            get => _world;
            set => _world = value ?? NullWorldAdapter.Instance;
        }

        /// <summary>
        ///     Seconds a pending projectile cast stays routable without a reported hit before the
        ///     leak guard discards it (hosts may never realize a spawn: unbound archetype id, null
        ///     world adapter). Every reported hit refreshes the timer. Non-positive disables the guard.
        /// </summary>
        public float PendingProjectileTimeout { get; set; } = 30f;

        /// <summary>Adds or replaces a catalog modifier by id (case-insensitive). Null or id-less blueprints are ignored.</summary>
        public void RegisterModifier(ModifierBlueprint blueprint)
        {
            if (blueprint != null && !string.IsNullOrEmpty(blueprint.Id))
            {
                _modifierCatalog[blueprint.Id] = blueprint;
            }
        }

        /// <summary>Looks up a catalog modifier by id (case-insensitive).</summary>
        public bool TryGetModifier(string id, out ModifierBlueprint blueprint)
        {
            blueprint = null;
            return !string.IsNullOrEmpty(id) && _modifierCatalog.TryGetValue(id, out blueprint);
        }

        /// <summary>
        ///     Registers the blueprint for future grants. Existing slots retain their granted blueprint
        ///     reference; revoke and grant again to replace a unit's ability and reset its slot state.
        /// </summary>
        public void RegisterAbility(AbilityBlueprint blueprint)
        {
            if (blueprint != null && !string.IsNullOrEmpty(blueprint.Id))
            {
                _abilityCatalog[blueprint.Id] = blueprint;
            }
        }

        /// <summary>Looks up a catalog ability by id (case-insensitive).</summary>
        public bool TryGetAbility(string id, out AbilityBlueprint blueprint)
        {
            blueprint = null;
            return !string.IsNullOrEmpty(id) && _abilityCatalog.TryGetValue(id, out blueprint);
        }

        /// <summary>Creates a unit with the next free id. Add resource pools and base properties afterwards.</summary>
        public AbilityUnit CreateUnit(TeamId team, string displayName = null)
        {
            AbilityUnit unit = new AbilityUnit(new UnitId(_nextUnitId++), this)
            {
                Team = team,
                DisplayName = displayName
            };
            _units[unit.Id] = unit;
            return unit;
        }

        /// <summary>Registers a unit with an externally supplied id (network replication).</summary>
        public AbilityUnit CreateUnitWithId(UnitId id, TeamId team, string displayName = null)
        {
            if (!id.IsValid || _units.ContainsKey(id))
            {
                return null;
            }

            AbilityUnit unit = new AbilityUnit(id, this) { Team = team, DisplayName = displayName };
            _units[unit.Id] = unit;
            if (id.Value >= _nextUnitId)
            {
                _nextUnitId = id.Value + 1;
            }

            return unit;
        }

        /// <summary>
        ///     Removes a unit with its modifiers (firing their removal receipts), granted slots and modifier
        ///     version bookkeeping. Unknown ids are ignored.
        /// </summary>
        public void DestroyUnit(UnitId id)
        {
            if (!_units.Remove(id))
            {
                return;
            }

            Modifiers.RemoveAllFrom(id);
            // WHY: a destroyed owner's version entry would otherwise stay in the engine for the whole
            // session (survivor waves churn thousands of units through fresh ids).
            Modifiers.ForgetOwner(id);
            if (_slots.Remove(id))
            {
                SlotsVersion++;
            }
        }

        /// <summary>The unit with this id, or null.</summary>
        public AbilityUnit GetUnit(UnitId id)
        {
            return _units.TryGetValue(id, out AbilityUnit unit) ? unit : null;
        }

        /// <summary>Looks up a unit by id.</summary>
        public bool TryGetUnit(UnitId id, out AbilityUnit unit)
        {
            return _units.TryGetValue(id, out unit);
        }

        /// <summary>Clears <paramref name="results" /> and fills it with every live unit. Allocation-free.</summary>
        public void GetUnits(List<AbilityUnit> results)
        {
            results.Clear();
            foreach (KeyValuePair<UnitId, AbilityUnit> pair in _units)
            {
                results.Add(pair.Value);
            }
        }

        /// <summary>Marks a unit dead: fires death/kill events and clears its modifiers.</summary>
        public void MarkDead(AbilityUnit unit, UnitId killer, string abilityId = null, uint castId = 0)
        {
            if (unit == null || !unit.IsAlive)
            {
                return;
            }

            unit.IsAlive = false;
            Events.Publish(new AbilityEventArgs(AbilityEvents.Death, unit.Id, killer, 0f, abilityId,
                castId: castId));
            Modifiers.RemoveAllFrom(unit.Id);
        }

        /// <summary>Revives a unit with the given health fraction (0..1 of max). Fires the revive event.</summary>
        public void Revive(AbilityUnit unit, float healthFraction = 1f)
        {
            if (unit == null || unit.IsAlive)
            {
                return;
            }

            unit.IsAlive = true;
            float max = unit.MaxHealth;
            if (max > 0f)
            {
                // WHY: uses SetCurrent, not Increase, because a just-killed unit sits at 0 HP, where the
                // heal gate would otherwise reject the restore. Never revive at 0 HP.
                float target = Mathf.Clamp01(healthFraction) * max;
                if (target <= 0f)
                {
                    target = Mathf.Min(1f, max);
                }

                unit.Resources.SetCurrent(AbilityResourceIds.Health, target);
            }

            // WHY: published after the health restore so receipt consumers (VFX, ragdoll, replication)
            // read the final state; without it a Death receipt would never be matched by a revive.
            Events.Publish(new AbilityEventArgs(AbilityEvents.Revive, unit.Id, UnitId.None, unit.Health));
        }

        /// <summary>
        ///     Bumped whenever any unit's granted-slot set changes (grant, revoke, unit destroy) so
        ///     per-frame pollers (e.g. AbilityAutoCaster) can cache slot lists and rebuild only on change.
        /// </summary>
        public int SlotsVersion { get; private set; }

        /// <summary>
        ///     Grants the current catalog blueprint when the unit has no slot for this id. Repeated
        ///     grants return the existing slot without replacing its blueprint, level or cooldown.
        /// </summary>
        public AbilitySlot GrantAbility(UnitId unit, string abilityId)
        {
            if (!_units.ContainsKey(unit) || !TryGetAbility(abilityId, out AbilityBlueprint blueprint))
            {
                return null;
            }

            if (!_slots.TryGetValue(unit, out Dictionary<string, AbilitySlot> unitSlots))
            {
                unitSlots = new Dictionary<string, AbilitySlot>(AbilityIdComparer.Instance);
                _slots[unit] = unitSlots;
            }

            if (!unitSlots.TryGetValue(blueprint.Id, out AbilitySlot slot))
            {
                slot = new AbilitySlot(blueprint);
                unitSlots[blueprint.Id] = slot;
                SlotsVersion++;
            }

            return slot;
        }

        /// <summary>Removes a granted ability slot (with its cooldown, charges and level). Returns false when not granted.</summary>
        public bool RevokeAbility(UnitId unit, string abilityId)
        {
            if (!string.IsNullOrEmpty(abilityId) &&
                _slots.TryGetValue(unit, out Dictionary<string, AbilitySlot> unitSlots) &&
                unitSlots.Remove(abilityId))
            {
                SlotsVersion++;
                return true;
            }

            return false;
        }

        /// <summary>The granted slot of an ability on a unit (case-insensitive id).</summary>
        public bool TryGetSlot(UnitId unit, string abilityId, out AbilitySlot slot)
        {
            slot = null;
            return !string.IsNullOrEmpty(abilityId) &&
                   _slots.TryGetValue(unit, out Dictionary<string, AbilitySlot> unitSlots) &&
                   unitSlots.TryGetValue(abilityId, out slot);
        }

        /// <summary>Clears <paramref name="results" /> and fills it with the unit's granted slots. Allocation-free.</summary>
        public void GetSlots(UnitId unit, List<AbilitySlot> results)
        {
            results.Clear();
            if (_slots.TryGetValue(unit, out Dictionary<string, AbilitySlot> unitSlots))
            {
                foreach (KeyValuePair<string, AbilitySlot> pair in unitSlots)
                {
                    results.Add(pair.Value);
                }
            }
        }

        /// <summary>
        ///     Validates and executes a cast: grant, liveness, stun/silence, cooldown/charges, target, team
        ///     filter, range and costs, then pays, starts the cooldown, publishes <c>ability_cast</c>, runs cast
        ///     effects and either the impact effects (instant) or a projectile spawn request. Allocation-free
        ///     once warm for instant and projectile casts; what the world adapter does with the spawn request
        ///     is up to the adapter.
        /// </summary>
        public CastResult Cast(CastRequest request)
        {
            if (_castDepth >= MaxCastDepth)
            {
                TraceNote(AbilityTraceKind.CastFailed, request.Caster, request.TargetUnit, 0f, request.AbilityId, null,
                    CastFailureReasonNames.Of(CastFailureReason.NestingLimit), (int)CastFailureReason.NestingLimit);
                return CastResult.Fail(CastFailureReason.NestingLimit);
            }

            _castDepth++;
            try
            {
                CastResult result = CastInternal(request);
                if (!result.Success && Trace.Enabled)
                {
                    Trace.Record(_traceCause, AbilityTraceKind.CastFailed, _time, TickCount, request.Caster,
                        request.TargetUnit, request.AbilityId, null, (int)result.Failure, 0f,
                        CastFailureReasonNames.Of(result.Failure));
                }

                return result;
            }
            finally
            {
                _castDepth--;
            }
        }

        private CastResult CastInternal(CastRequest request)
        {
            AbilityUnit caster = GetUnit(request.Caster);
            if (caster == null)
            {
                return CastResult.Fail(CastFailureReason.UnknownCaster);
            }

            if (!TryGetAbility(request.AbilityId, out _))
            {
                return CastResult.Fail(CastFailureReason.UnknownAbility);
            }

            if (!TryGetSlot(request.Caster, request.AbilityId, out AbilitySlot slot))
            {
                return CastResult.Fail(CastFailureReason.NotGranted);
            }

            // WHY: the slot owns charges/cooldowns and auto-target selection uses its blueprint.
            // A later catalog replacement must not mix new cast rules with the old granted state.
            AbilityBlueprint ability = slot.Blueprint;

            if (!caster.IsAlive)
            {
                return CastResult.Fail(CastFailureReason.CasterDead);
            }

            if (caster.HasState(AbilityStates.Stunned))
            {
                return CastResult.Fail(CastFailureReason.Stunned);
            }

            if (caster.HasState(AbilityStates.Silenced))
            {
                return CastResult.Fail(CastFailureReason.Silenced);
            }

            if (!slot.IsReady)
            {
                return CastResult.Fail(slot.UsesCharges
                    ? CastFailureReason.NoCharges
                    : CastFailureReason.OnCooldown);
            }

            int level = slot.Level;

            AbilityUnit targetUnit = null;
            if (ability.Targeting == TargetingMode.Unit)
            {
                targetUnit = GetUnit(request.TargetUnit);
                if (targetUnit == null)
                {
                    return CastResult.Fail(CastFailureReason.InvalidTarget);
                }

                if (!targetUnit.IsAlive)
                {
                    return CastResult.Fail(CastFailureReason.TargetDead);
                }

                if (targetUnit.Id != caster.Id && targetUnit.HasState(AbilityStates.Untargetable))
                {
                    return CastResult.Fail(CastFailureReason.TargetUntargetable);
                }

                if (!MatchesTeamFilter(caster, targetUnit, ability.TeamFilter))
                {
                    return CastResult.Fail(CastFailureReason.WrongTeam);
                }
            }

            // WHY: range is only checked when the world adapter can supply both positions.
            float range = ability.ResolveRange(level);
            if (range > 0f && _world.TryGetPosition(caster.Id, out Vector3 casterPos))
            {
                float effectiveRange = range + caster.GetProperty(AbilityProperties.CastRangeBonus);
                Vector3? targetPos = null;
                if (ability.Targeting == TargetingMode.Unit && targetUnit != null &&
                    _world.TryGetPosition(targetUnit.Id, out Vector3 tp))
                {
                    targetPos = tp;
                }
                else if (ability.Targeting == TargetingMode.Point)
                {
                    targetPos = request.TargetPoint;
                }

                // WHY: the exact test radius queries and AbilityAutoCaster use, so a unit they select on the
                // range boundary (or far away, where float squares overflow) is never rejected here.
                if (targetPos.HasValue && (effectiveRange < 0f ||
                                           !AbilitySpatialGrid.IsWithinRadius(targetPos.Value, casterPos,
                                               effectiveRange)))
                {
                    return CastResult.Fail(CastFailureReason.OutOfRange);
                }
            }

            // WHY: casts started by payment callbacks run one cast depth deeper, so each depth owns its record.
            List<AbilitySpentResource> spent = RentScratch(_spentScratchPool, _castDepth);
            spent.Clear();
            if (!TryPayCosts(caster, ability, level, targetUnit != null ? targetUnit.Id : UnitId.None, spent))
            {
                spent.Clear();
                return CastResult.Fail(CastFailureReason.NotEnoughResources);
            }

            ICooldownRule cooldowns = Rules.Cooldowns;
            if (slot.UsesCharges)
            {
                slot.Charges--;
                if (slot.CooldownRemaining <= 0f)
                {
                    slot.StartCooldown(cooldowns.ResolveCooldown(caster, ability, ability.ResolveChargeRestoreTime(level)));
                }
            }
            else
            {
                slot.Charges = 0;
                slot.StartCooldown(cooldowns.ResolveCooldown(caster, ability, ability.ResolveCooldown(level)));
            }

            uint castId = _nextCastId++;
            long previousCause = _traceCause;
            if (Trace.Enabled)
            {
                _traceCause = Trace.Record(previousCause, AbilityTraceKind.Cast, _time, TickCount, caster.Id,
                    targetUnit?.Id ?? UnitId.None, ability.Id, null, (int)castId, 0f, null);
            }

            try
            {
                Events.Publish(new AbilityEventArgs(AbilityEvents.AbilityCast, caster.Id,
                    targetUnit?.Id ?? UnitId.None, 0f, ability.Id, castId: castId));
                ExecuteAcceptedCast(request, caster, ability, slot, targetUnit, castId, spent);
            }
            finally
            {
                _traceCause = previousCause;
                spent.Clear();
            }

            return CastResult.Ok(castId);
        }

        private void ExecuteAcceptedCast(CastRequest request, AbilityUnit caster, AbilityBlueprint ability,
            AbilitySlot slot, AbilityUnit targetUnit, uint castId, List<AbilitySpentResource> spent)
        {
            // WHY: read after the ability_cast receipt, as before, so a handler that levels the ability on cast still
            // scales this cast's effects.
            int level = slot.Level;
            uint seed = request.Seed != 0 ? request.Seed : castId * 2654435761u ^ caster.Id.Value;
            // WHY: evaluated once, before cast effects can change cast_range_bonus, so the cast's target point
            // and its projectile's spawn aim always end at the same place.
            Vector3 directionReach = ability.Targeting == TargetingMode.Direction
                ? DirectionReach(caster, ability, level, request.Direction)
                : default;
            EffectContext context = RentContext(caster.Id, ability.Id, seed);
            try
            {
                FillCastTargets(context, caster, ability, targetUnit, request, directionReach);
                context.CastId = castId;
                context.AbilityLevel = level;
                context.Specials = ability.Specials;
                context.Origin = EffectOrigin.Cast;
                for (int i = 0; i < spent.Count; i++)
                {
                    context.AddSpent(spent[i].ResourceId, spent[i].Amount);
                }

                // WHY: cast-phase effects run immediately, ahead of delivery, regardless of delivery type.
                if (ability.CastEffects.Count > 0)
                {
                    ExecuteEffects(ability.CastEffects, context);
                }

                if (ability.Delivery == AbilityDeliveryType.Projectile)
                {
                    PendingProjectileCast pending = RentPending(ability, caster.Id, seed, level,
                        _time + PendingProjectileTimeout);
                    pending.CaptureSpent(context);
                    _pendingProjectiles[castId] = pending;
                    _world.TryGetPosition(caster.Id, out Vector3 origin);
                    Vector3 direction = request.Direction;
                    if (ability.Targeting == TargetingMode.Direction)
                    {
                        // WHY: hosts detonate a non-homing projectile at Position + Direction. A unit-length
                        // aim vector (transform.forward) ended skillshots one world unit ahead; the spawn now
                        // carries the same reach the cast's target point uses.
                        direction = directionReach;
                    }
                    else if (direction == default && targetUnit != null &&
                             _world.TryGetPosition(targetUnit.Id, out Vector3 targetPosition))
                    {
                        direction = targetPosition - origin;
                    }
                    else if (direction == default && ability.Targeting == TargetingMode.Point)
                    {
                        direction = request.TargetPoint - origin;
                    }

                    _world.RequestSpawn(new SpawnRequest(ability.ProjectileArchetypeId, caster.Id, origin,
                        direction, targetUnit?.Id ?? UnitId.None, ability.Id, ability.ResolveProjectileSpeed(level),
                        castId));
                }
                else if (ability.ImpactEffects.Count > 0)
                {
                    ExecuteEffects(ability.ImpactEffects, context);
                }
            }
            finally
            {
                ReturnContext(context);
            }
        }

        /// <summary>
        ///     Pays every positive authored cost or nothing that preflight can detect. Ids and effective
        ///     amounts are captured once because payment callbacks run synchronously and may change the
        ///     blueprint or the mana-cost multiplier. Amounts resolve at the slot level (AmountByLevel / AmountKey)
        ///     before the cost rule; UpTo / All entries take what is left of their resource after the earlier
        ///     entries. What was paid is appended to <paramref name="spent" />.
        /// </summary>
        private bool TryPayCosts(AbilityUnit caster, AbilityBlueprint ability, int level, UnitId target,
            List<AbilitySpentResource> spent)
        {
            int costCount = ability.Costs.Count;
            if (costCount == 0)
            {
                return true;
            }

            List<AbilityCost> capturedCosts = RentScratch(_costScratchPool, _costDepth);
            _costDepth++;
            try
            {
                capturedCosts.Clear();
                for (int i = 0; i < costCount; i++)
                {
                    AbilityCost cost = ability.Costs[i];
                    AbilityCost leveled = cost;
                    leveled.Amount = cost.ResolveAmount(ability.Specials, this, caster.Id, level, target);
                    float amount = Rules.Costs.ResolveCost(caster, ability, leveled);
                    if (float.IsNaN(amount) || float.IsInfinity(amount))
                    {
                        // WHY: a NaN entry poisons cumulative costs and makes affordability comparisons
                        // false; reject every invalid amount before any resource is paid.
                        return false;
                    }

                    if (cost.Mode != AbilityCostMode.Fixed)
                    {
                        if (string.IsNullOrEmpty(cost.ResourceId))
                        {
                            return false;
                        }

                        float available = caster.Resources.GetCurrent(cost.ResourceId);
                        for (int previous = 0; previous < capturedCosts.Count; previous++)
                        {
                            AbilityCost previousCost = capturedCosts[previous];
                            if (AbilityIdComparer.Same(previousCost.ResourceId, cost.ResourceId))
                            {
                                available -= MathF.Max(0f, previousCost.Amount);
                            }
                        }

                        available = MathF.Max(0f, available);
                        if (available < cost.MinAmount)
                        {
                            return false;
                        }

                        amount = cost.Mode == AbilityCostMode.All ? available : MathF.Min(available, amount);
                    }

                    capturedCosts.Add(new AbilityCost(cost.ResourceId, amount));
                }

                // WHY: a per-operation decrease cap applies to each authored cost, while affordability
                // applies to their cumulative resource total. Validate both before paying any entry.
                for (int i = 0; i < capturedCosts.Count; i++)
                {
                    AbilityCost cost = capturedCosts[i];
                    if (cost.Amount <= 0f)
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(cost.ResourceId) ||
                        !caster.Resources.CanSpend(cost.ResourceId, cost.Amount, out _))
                    {
                        return false;
                    }

                    float total = cost.Amount;
                    for (int previous = 0; previous < i; previous++)
                    {
                        AbilityCost previousCost = capturedCosts[previous];
                        if (AbilityIdComparer.Same(previousCost.ResourceId, cost.ResourceId))
                        {
                            total += MathF.Max(0f, previousCost.Amount);
                        }
                    }

                    if (float.IsNaN(total) || float.IsInfinity(total) ||
                        caster.Resources.GetCurrent(cost.ResourceId) < total)
                    {
                        return false;
                    }
                }

                for (int i = 0; i < capturedCosts.Count; i++)
                {
                    AbilityCost cost = capturedCosts[i];
                    if (cost.Amount > 0f && !caster.Resources.TrySpend(cost.ResourceId, cost.Amount, out _))
                    {
                        // WHY: a preceding payment callback may invalidate a later entry after preflight.
                        // Never accept a clipped payment; earlier successful payments are not rolled back.
                        return false;
                    }

                    if (cost.Amount > 0f)
                    {
                        spent.Add(new AbilitySpentResource(cost.ResourceId, cost.Amount));
                    }
                }

                return true;
            }
            finally
            {
                capturedCosts.Clear();
                _costDepth--;
            }
        }

        private EffectContext RentContext(UnitId caster, string abilityId, uint seed)
        {
            int last = _contextPool.Count - 1;
            EffectContext context;
            if (last >= 0)
            {
                context = _contextPool[last];
                _contextPool.RemoveAt(last);
            }
            else
            {
                context = EffectContext.CreatePooled();
            }

            context.ResetPooled(this, caster, abilityId, seed);
            return context;
        }

        private void ReturnContext(EffectContext context)
        {
            context.ClearPooled();
            _contextPool.Add(context);
        }

        /// <summary>
        ///     Host callback: a projectile of cast <paramref name="castId" /> hit a unit or point.
        ///     May be called multiple times for piercing projectiles.
        /// </summary>
        public bool NotifyProjectileHit(uint castId, UnitId hitUnit, Vector3 hitPoint)
        {
            if (!_pendingProjectiles.TryGetValue(castId, out PendingProjectileCast pending))
            {
                return false;
            }

            AbilityUnit caster = GetUnit(pending.Caster);
            if (caster == null)
            {
                RemovePending(castId);
                return false;
            }

            // WHY: each hit gets its own mixed seed — replaying the raw cast seed would roll
            // identical crit/chance outcomes on every pierced target and correlate impact with cast.
            uint hitSeed = MixSeed(pending.Seed, pending.HitIndex);
            pending.HitIndex++;
            // WHY: an actively hitting projectile is alive; keep the leak guard from expiring it.
            pending.ExpireAt = _time + PendingProjectileTimeout;

            // WHY: impact effects may release (and recycle) this record re-entrantly, e.g. a death
            // listener despawning the projectile; read everything needed before they run.
            AbilityBlueprint ability = pending.Ability;
            EffectContext context = RentContext(pending.Caster, ability.Id, hitSeed);
            long previousCause = _traceCause;
            if (Trace.Enabled)
            {
                _traceCause = Trace.Record(previousCause, AbilityTraceKind.ProjectileHit, _time, TickCount,
                    pending.Caster, hitUnit, ability.Id, null, (int)castId, 0f, null);
            }

            try
            {
                context.TargetPoint = hitPoint;
                context.HasTargetPoint = true;
                context.CastId = castId;
                context.AbilityLevel = pending.Level;
                context.Specials = ability.Specials;
                context.Origin = EffectOrigin.ProjectileHit;
                pending.CopySpentTo(context);
                AbilityUnit hit = GetUnit(hitUnit);
                if (hit != null && hit.IsAlive)
                {
                    context.PrimaryTargets.Add(hitUnit);
                }

                ExecuteEffects(ability.ImpactEffects, context);
            }
            finally
            {
                ReturnContext(context);
                _traceCause = previousCause;
            }

            return true;
        }

        /// <summary>Host callback: the projectile of a cast is finished (expired or destroyed).</summary>
        public void ReleaseProjectileCast(uint castId)
        {
            RemovePending(castId);
        }

        private PendingProjectileCast RentPending(AbilityBlueprint ability, UnitId caster, uint seed, int level,
            float expireAt)
        {
            int last = _pendingPool.Count - 1;
            PendingProjectileCast pending;
            if (last >= 0)
            {
                pending = _pendingPool[last];
                _pendingPool.RemoveAt(last);
            }
            else
            {
                pending = new PendingProjectileCast();
            }

            pending.Reset(ability, caster, seed, level, expireAt);
            return pending;
        }

        private void RemovePending(uint castId)
        {
            if (_pendingProjectiles.Remove(castId, out PendingProjectileCast pending))
            {
                pending.Clear();
                _pendingPool.Add(pending);
            }
        }

        /// <summary>
        ///     Executes a node list in a context. Public so hosts can run data-driven effects directly.
        ///     A released context (<see cref="EffectContext.IsReleased" />) is ignored.
        /// </summary>
        public void ExecuteEffects(List<EffectNodeData> nodes, EffectContext context)
        {
            if (nodes == null || nodes.Count == 0 || context == null || context.Depth >= EffectContext.MaxDepth)
            {
                return;
            }

            if (context.IsReleased)
            {
                // WHY: a custom op kept a recycled context; running it would use another cast's targets
                // and RNG. Fail closed and say so (gated and throttled, constant message: no garbage).
                NeoDiagnostics.LogWarningThrottled(ReleasedContextWarningKey,
                    "[Neo.Abilities] ExecuteEffects ignored a released EffectContext. An IEffectOperation kept a " +
                    "system-created context (or a CreateNested child of it) after Execute returned; copy the " +
                    "values it needs and build a new EffectContext instead.");
                return;
            }

            _executionDepth++;
            try
            {
                // WHY: op.Execute may re-enter ExecuteEffects via event reactions; the per-depth
                // scratch keeps the list an outer op is still iterating intact.
                List<UnitId> targetScratch = RentScratch(_targetScratchPool, _executionDepth - 1);
                IEffectTargetResolver targeting = Rules.Targeting;
                for (int i = 0; i < nodes.Count; i++)
                {
                    EffectNodeData node = nodes[i];
                    if (node == null || string.IsNullOrEmpty(node.OpId))
                    {
                        continue;
                    }

                    float chance = node.ResolveChance(context.AbilityLevel);
                    if (chance < 1f && context.Random.NextFloat() >= chance)
                    {
                        continue;
                    }

                    if (!Ops.TryGet(node.OpId, out IEffectOperation op))
                    {
                        continue;
                    }

                    targetScratch.Clear();
                    targeting.Resolve(node, context, targetScratch);
                    if (Trace.Enabled)
                    {
                        long previousCause = _traceCause;
                        _traceCause = Trace.Record(previousCause, AbilityTraceKind.Effect, _time, TickCount,
                            context.Caster, targetScratch.Count > 0 ? targetScratch[0] : UnitId.None, op.Id,
                            context.AbilityId, targetScratch.Count, 0f, null);
                        try
                        {
                            op.Execute(context, node, targetScratch);
                        }
                        finally
                        {
                            _traceCause = previousCause;
                        }
                    }
                    else
                    {
                        op.Execute(context, node, targetScratch);
                    }
                }
            }
            finally
            {
                _executionDepth--;
            }
        }

        /// <summary>Applies a catalog modifier directly (code path parallel to the apply_modifier op).</summary>
        public ModifierApplyResult ApplyModifier(string modifierId, UnitId caster, UnitId target,
            string sourceAbilityId = null, int abilityLevel = 1)
        {
            return TryGetModifier(modifierId, out ModifierBlueprint blueprint)
                ? Modifiers.Apply(blueprint, caster, target, sourceAbilityId, abilityLevel)
                : new ModifierApplyResult(null, false);
        }

        /// <summary>
        ///     Convenience: sets a granted ability's level (clamped to at least 1) so leveled values scale.
        ///     Returns false when the unit has no such slot. Level-scaled cast parameters apply from the next cast: a
        ///     running cooldown keeps its remaining time; a changed charge cap grants or clamps charges (see
        ///     <see cref="AbilitySlot" />).
        /// </summary>
        public bool SetAbilityLevel(UnitId unit, string abilityId, int level)
        {
            if (!TryGetSlot(unit, abilityId, out AbilitySlot slot))
            {
                return false;
            }

            slot.Level = level; // WHY: setter clamps to >= 1
            return true;
        }

        /// <summary>Advances modifiers, cooldowns, resource pools and property-driven regeneration.</summary>
        /// <remarks>Nonpositive/nonfinite steps, or a step overflowing the domain clock, are ignored.</remarks>
        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
            {
                return;
            }

            float nextTime = _time + deltaTime;
            if (float.IsNaN(nextTime) || float.IsInfinity(nextTime))
            {
                return;
            }

            _time = nextTime;
            TickCount++;
            PrunePendingProjectiles();

            Modifiers.Tick(deltaTime);
            TickZones(deltaTime);

            foreach (KeyValuePair<UnitId, Dictionary<string, AbilitySlot>> pair in _slots)
            {
                foreach (KeyValuePair<string, AbilitySlot> slotPair in pair.Value)
                {
                    slotPair.Value.Tick(deltaTime);
                }
            }

            // WHY: resource callbacks may execute area effects that query units. Keep the tick
            // snapshot separate so those effects cannot skip or repeat the remaining units.
            _tickUnitScratch.Clear();
            foreach (KeyValuePair<UnitId, AbilityUnit> pair in _units)
            {
                _tickUnitScratch.Add(pair.Key);
            }

            for (int i = 0; i < _tickUnitScratch.Count; i++)
            {
                AbilityUnit unit = GetUnit(_tickUnitScratch[i]);
                if (unit == null || !unit.IsAlive)
                {
                    continue;
                }

                unit.Resources.Tick(deltaTime);

                float healthRegen = unit.GetProperty(AbilityProperties.HealthRegen);
                if (healthRegen > 0f)
                {
                    unit.Resources.Increase(AbilityResourceIds.Health, healthRegen * deltaTime);
                }

                float manaRegen = unit.GetProperty(AbilityProperties.ManaRegen);
                if (manaRegen > 0f)
                {
                    unit.Resources.Increase(AbilityResourceIds.Mana, manaRegen * deltaTime);
                }
            }
        }

        /// <summary>True when <paramref name="target" /> passes <paramref name="filter" /> relative to <paramref name="caster" /> (self counts as an ally).</summary>
        public static bool MatchesTeamFilter(AbilityUnit caster, AbilityUnit target, AbilityTeamFilter filter)
        {
            if (caster == null || target == null)
            {
                return false;
            }

            switch (filter)
            {
                case AbilityTeamFilter.Allies:
                    return caster.Id == target.Id || caster.Team.IsAllyOf(target.Team);
                case AbilityTeamFilter.Enemies:
                    return caster.Id != target.Id && caster.Team.IsEnemyOf(target.Team);
                default:
                    return true;
            }
        }

        private void FillCastTargets(EffectContext context, AbilityUnit caster, AbilityBlueprint ability,
            AbilityUnit targetUnit, CastRequest request, Vector3 directionReach)
        {
            switch (ability.Targeting)
            {
                case TargetingMode.Unit:
                    if (targetUnit != null)
                    {
                        context.PrimaryTargets.Add(targetUnit.Id);
                    }

                    break;
                case TargetingMode.Point:
                    context.TargetPoint = request.TargetPoint;
                    context.HasTargetPoint = true;
                    break;
                case TargetingMode.Direction:
                    if (_world.TryGetPosition(caster.Id, out Vector3 origin))
                    {
                        context.TargetPoint = origin + directionReach;
                        context.HasTargetPoint = true;
                    }

                    break;
                default:
                    context.PrimaryTargets.Add(caster.Id);
                    break;
            }
        }

        /// <summary>
        ///     Aim of a <see cref="TargetingMode.Direction" /> cast scaled to its reach. The aim is the normalized
        ///     request direction (world forward when it is zero or not finite). A positive Range reaches the
        ///     effective cast range, Range + cast_range_bonus (at least 0): the distance the range check and
        ///     AbilityAutoCaster use. Range 0 (unlimited) reaches the length of the request direction, at least
        ///     <see cref="DefaultDirectionReach" />, so an aim vector towards a target or cursor arrives there.
        /// </summary>
        private static Vector3 DirectionReach(AbilityUnit caster, AbilityBlueprint ability, int level, Vector3 direction)
        {
            // WHY: double precision keeps far aims (components beyond ~1.8e19, whose float square overflows)
            // pointing the right way instead of collapsing to world forward.
            double x = direction.x;
            double y = direction.y;
            double z = direction.z;
            double length = Math.Sqrt(x * x + y * y + z * z);
            Vector3 aim;
            if (length > 0d && !double.IsInfinity(length))
            {
                aim = new Vector3((float)(x / length), (float)(y / length), (float)(z / length));
            }
            else
            {
                aim = Vector3.forward;
                length = 0d;
            }

            float range = ability.ResolveRange(level);
            float reach = range > 0f
                ? range + caster.GetProperty(AbilityProperties.CastRangeBonus)
                : (float)Math.Min(Math.Max(length, DefaultDirectionReach), float.MaxValue);
            if (float.IsNaN(reach) || float.IsInfinity(reach))
            {
                // WHY: a non-finite authored range or bonus would turn the aim into NaN components.
                reach = DefaultDirectionReach;
            }

            return aim * MathF.Max(0f, reach);
        }

        private void OnModifierApplied(ModifierInstance instance, bool createdNew)
        {
            // WHY: max_health_bonus / max_mana_bonus feed resource pool maxima eagerly here —
            // pools are stored state, unlike the lazily recomputed property cache.
            SyncResourceMaxBonuses(instance.Owner);
            Events.Publish(new AbilityEventArgs(AbilityEvents.ModifierApplied, instance.Owner, instance.Caster,
                instance.Stacks, instance.SourceAbilityId, instance.Blueprint.Id));
            if (createdNew && instance.TriggerFlags != 0)
            {
                InitializeStateWatch(instance);
                FireLifecycleTriggers(instance, ModifierTrigger.Applied);
            }

            EvaluateStateWatchers(instance.Owner);
        }

        private void OnModifierRemoved(ModifierInstance instance, bool expired)
        {
            SyncResourceMaxBonuses(instance.Owner);
            Events.Publish(new AbilityEventArgs(AbilityEvents.ModifierRemoved, instance.Owner, instance.Caster,
                expired ? 1f : 0f, instance.SourceAbilityId, instance.Blueprint.Id));
            if (instance.TriggerFlags != 0)
            {
                FireLifecycleTriggers(instance, ModifierTrigger.Removed);
            }

            EvaluateStateWatchers(instance.Owner);
        }

        private void SyncResourceMaxBonuses(UnitId owner)
        {
            if (_units.TryGetValue(owner, out AbilityUnit unit))
            {
                unit.SyncResourceMaxBonuses();
            }
        }

        private void OnModifierTick(ModifierInstance instance)
        {
            List<EffectNodeData> tickEffects = instance.Blueprint.TickEffects;
            if (tickEffects == null || tickEffects.Count == 0)
            {
                return;
            }

            // WHY: replaying one fixed seed made every tick roll the same chance/crit/evasion outcome
            // (a 50% DoT node fired on all ticks or none). The per-instance index keeps rolls
            // independent per tick and still deterministic for an identical tick sequence.
            uint seed = RollSeed(instance.Owner.Value * 747796405u + (uint)instance.InstanceId,
                instance.TickRollIndex++);
            EffectContext context = RentContext(instance.Caster, instance.SourceAbilityId, seed);
            long previousCause = _traceCause;
            if (Trace.Enabled)
            {
                _traceCause = Trace.Record(previousCause, AbilityTraceKind.ModifierTick, _time, TickCount,
                    instance.Owner, instance.Caster, instance.Blueprint.Id, instance.SourceAbilityId, instance.Stacks,
                    0f, null);
            }

            try
            {
                context.AbilityLevel = instance.AbilityLevel;
                if (TryGetAbility(instance.SourceAbilityId, out AbilityBlueprint ability))
                {
                    context.Specials = ability.Specials;
                }

                context.Origin = EffectOrigin.ModifierTick;
                context.SourceStacks = instance.Stacks;
                instance.CopySpentTo(context);
                context.PrimaryTargets.Add(instance.Owner);
                ExecuteEffects(tickEffects, context);
            }
            finally
            {
                ReturnContext(context);
                _traceCause = previousCause;
            }
        }

        private static List<T> RentScratch<T>(List<List<T>> pool, int depth)
        {
            while (pool.Count <= depth)
            {
                pool.Add(new List<T>(16));
            }

            return pool[depth];
        }

        // WHY: the first roll keeps the historical base seed so single-tick/single-reaction outcomes of
        // existing content do not change; later rolls get an avalanche-mixed seed.
        private static uint RollSeed(uint baseSeed, uint index)
        {
            return index == 0 ? baseSeed : MixSeed(baseSeed, index);
        }

        // WHY: murmur-style avalanche; xorshift32 is GF(2)-linear, so plain XOR salting would keep
        // per-hit streams correlated.
        private static uint MixSeed(uint seed, uint salt)
        {
            uint h = seed + 0x9E3779B9u + salt * 0x85EBCA6Bu;
            h ^= h >> 16;
            h *= 0x85EBCA6Bu;
            h ^= h >> 13;
            h *= 0xC2B2AE35u;
            h ^= h >> 16;
            return h;
        }

        // WHY: hosts may never realize a requested projectile spawn (unbound archetype id, null
        // world adapter no-op), so pending casts self-expire instead of leaking for the session.
        private void PrunePendingProjectiles()
        {
            if (PendingProjectileTimeout <= 0f || _pendingProjectiles.Count == 0)
            {
                return;
            }

            _pendingPruneScratch.Clear();
            foreach (KeyValuePair<uint, PendingProjectileCast> pair in _pendingProjectiles)
            {
                if (_time >= pair.Value.ExpireAt)
                {
                    _pendingPruneScratch.Add(pair.Key);
                }
            }

            for (int i = 0; i < _pendingPruneScratch.Count; i++)
            {
                RemovePending(_pendingPruneScratch[i]);
            }
        }

        // WHY: recycled through _pendingPool, so fields are mutable; Reset/Clear bracket each use.
        private sealed class PendingProjectileCast
        {
            public AbilityBlueprint Ability;
            public UnitId Caster;
            public uint Seed;
            public int Level;

            /// <summary>Hits already reported for this cast; salts the per-hit RNG seed.</summary>
            public uint HitIndex;

            /// <summary>Domain time after which the leak guard discards the entry.</summary>
            public float ExpireAt;

            /// <summary>What the launching cast paid; replayed into every hit's context. Null until first needed.</summary>
            private List<AbilitySpentResource> _spent;

            public void Reset(AbilityBlueprint ability, UnitId caster, uint seed, int level, float expireAt)
            {
                Ability = ability;
                Caster = caster;
                Seed = seed;
                Level = level;
                HitIndex = 0;
                ExpireAt = expireAt;
                _spent?.Clear();
            }

            public void CaptureSpent(EffectContext source)
            {
                int count = source != null ? source.SpentCount : 0;
                if (count == 0)
                {
                    return;
                }

                _spent ??= new List<AbilitySpentResource>(count);
                for (int i = 0; i < count; i++)
                {
                    _spent.Add(source.GetSpentAt(i));
                }
            }

            public void CopySpentTo(EffectContext context)
            {
                if (_spent == null)
                {
                    return;
                }

                for (int i = 0; i < _spent.Count; i++)
                {
                    context.AddSpent(_spent[i].ResourceId, _spent[i].Amount);
                }
            }

            public void Clear()
            {
                Ability = null;
                Caster = UnitId.None;
                _spent?.Clear();
            }
        }
    }
}
