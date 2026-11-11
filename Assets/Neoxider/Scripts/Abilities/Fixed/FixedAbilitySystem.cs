using System;
using System.Collections.Generic;

namespace Neo.Abilities.Fixed
{
    /// <summary>
    ///     The engine-free integer ability domain: units with raw pools and properties, modifiers with stack policies
    ///     and triggers, damage hooks, effect operations, zones, projectiles and casts, advanced in whole ticks with
    ///     <see cref="Step()" />. No floating point, no UnityEngine; every rule is replaceable through
    ///     <see cref="Rules" />. Same content model and seeding as the float AbilitySystem, so converted content
    ///     plays the same within the Q-format grid.
    /// </summary>
    public sealed partial class FixedAbilitySystem : IFixedEventInterceptor
    {
        /// <summary>Maximum nesting of casts started from effects.</summary>
        public const int MaxCastDepth = 4;

        /// <summary>Default catch-up cap of <see cref="Advance" />.</summary>
        public const int DefaultMaxStepsPerAdvance = 8;

        private readonly Dictionary<int, FixedUnit> _units = new Dictionary<int, FixedUnit>();
        private readonly List<FixedUnit> _unitOrder = new List<FixedUnit>(16);

        private readonly Dictionary<string, FixedModifierBlueprint> _modifierCatalog =
            new Dictionary<string, FixedModifierBlueprint>(FixedIdComparer.Instance);

        private readonly Dictionary<string, FixedAbilityBlueprint> _abilityCatalog =
            new Dictionary<string, FixedAbilityBlueprint>(FixedIdComparer.Instance);

        private readonly Dictionary<int, Dictionary<string, FixedAbilitySlot>> _slots =
            new Dictionary<int, Dictionary<string, FixedAbilitySlot>>();

        private readonly Dictionary<uint, PendingProjectile> _pendingProjectiles = new Dictionary<uint, PendingProjectile>();
        private readonly List<PendingProjectile> _pendingPool = new List<PendingProjectile>(16);
        private readonly List<uint> _pendingPruneScratch = new List<uint>(8);
        private readonly List<FixedEffectContext> _contextPool = new List<FixedEffectContext>(8);
        private readonly List<List<int>> _targetScratchPool = new List<List<int>>(FixedEffectContext.MaxDepth + 1);
        private readonly List<List<FixedAbilityCost>> _costScratchPool = new List<List<FixedAbilityCost>>(2);

        // WHY: what each cast depth paid, handed to its effect context; nested casts use the next depth's list.
        private readonly List<List<FixedSpentResource>> _spentScratchPool =
            new List<List<FixedSpentResource>>(MaxCastDepth + 1);
        private readonly List<FixedUnit> _stepUnitScratch = new List<FixedUnit>(16);

        private IFixedWorldAdapter _world;
        private int _nextUnitId = 1;
        private uint _nextCastId = 1u;
        private int _executionDepth;
        private int _costDepth;
        private int _castDepth;

        public FixedAbilitySystem(FixedRules rules = null)
        {
            Rules = rules ?? new FixedRules();
            Modifiers = new FixedModifierEngine(this)
            {
                Applied = OnModifierApplied,
                Removed = OnModifierRemoved,
                TickDue = OnModifierTick
            };
            Damage = new FixedDamagePipeline(this);
            Events = new FixedEventBus(this);
            Ops = new FixedEffectOpRegistry();
            FixedDefaultOps.RegisterAll(Ops);
            DefaultWorld = new FixedUnitWorldAdapter(this);
            _world = DefaultWorld;
            PendingProjectileTimeoutTicks = 30 * Rules.TicksPerSecond;
        }

        public FixedRules Rules { get; }
        public FixedModifierEngine Modifiers { get; }
        public FixedDamagePipeline Damage { get; }
        public FixedEventBus Events { get; }
        public FixedEffectOpRegistry Ops { get; }

        /// <summary>The adapter used when <see cref="World" /> is set to null (unit positions, linear queries).</summary>
        public FixedUnitWorldAdapter DefaultWorld { get; }

        /// <summary>World seam (positions, radius queries, spawns). Never null.</summary>
        public IFixedWorldAdapter World
        {
            get => _world;
            set => _world = value ?? DefaultWorld;
        }

        /// <summary>The damage application whose before hooks are running, else null.</summary>
        public FixedDamageContext ActiveDamage => Damage.Active;

        /// <summary>Ticks stepped so far.</summary>
        public long TickCount { get; private set; }

        /// <summary>Ticks a projectile cast stays routable without a reported hit (0 = forever).</summary>
        public int PendingProjectileTimeoutTicks { get; set; }

        /// <summary>Catch-up cap of <see cref="Advance" />.</summary>
        public int MaxStepsPerAdvance { get; set; } = DefaultMaxStepsPerAdvance;

        /// <summary>Bumped whenever a unit's granted slots change.</summary>
        public int SlotsVersion { get; private set; }

        /// <summary>Live units in creation order (the order of ticks and of the default world adapter).</summary>
        public IReadOnlyList<FixedUnit> UnitsInCreationOrder => _unitOrder;

        public int UnitCount => _unitOrder.Count;

        public void RegisterModifier(FixedModifierBlueprint blueprint)
        {
            if (blueprint != null && !string.IsNullOrEmpty(blueprint.Id))
            {
                _modifierCatalog[blueprint.Id] = blueprint;
            }
        }

        public bool TryGetModifier(string id, out FixedModifierBlueprint blueprint)
        {
            blueprint = null;
            return !string.IsNullOrEmpty(id) && _modifierCatalog.TryGetValue(id, out blueprint);
        }

        public void RegisterAbility(FixedAbilityBlueprint blueprint)
        {
            if (blueprint != null && !string.IsNullOrEmpty(blueprint.Id))
            {
                _abilityCatalog[blueprint.Id] = blueprint;
            }
        }

        public bool TryGetAbility(string id, out FixedAbilityBlueprint blueprint)
        {
            blueprint = null;
            return !string.IsNullOrEmpty(id) && _abilityCatalog.TryGetValue(id, out blueprint);
        }

        /// <summary>Creates a unit with the next free id (1, 2, ...).</summary>
        public FixedUnit CreateUnit(int team)
        {
            while (_units.ContainsKey(_nextUnitId))
            {
                _nextUnitId++;
            }

            return AddUnit(_nextUnitId++, team);
        }

        /// <summary>Creates a unit with an id chosen by the host (replication, the game's entity ids); null when taken.</summary>
        public FixedUnit CreateUnitWithId(int id, int team)
        {
            if (id <= 0 || _units.ContainsKey(id))
            {
                return null;
            }

            if (id >= _nextUnitId)
            {
                _nextUnitId = id + 1;
            }

            return AddUnit(id, team);
        }

        /// <summary>Removes a unit with its modifiers (firing their removal receipts) and slots.</summary>
        public void DestroyUnit(int id)
        {
            if (!_units.TryGetValue(id, out FixedUnit unit))
            {
                return;
            }

            _units.Remove(id);
            _unitOrder.Remove(unit);
            Modifiers.RemoveAllFrom(id);
            Modifiers.ForgetOwner(id);
            if (_slots.Remove(id))
            {
                SlotsVersion++;
            }
        }

        public FixedUnit GetUnit(int id)
        {
            return id != 0 && _units.TryGetValue(id, out FixedUnit unit) ? unit : null;
        }

        public bool TryGetUnit(int id, out FixedUnit unit)
        {
            unit = GetUnit(id);
            return unit != null;
        }

        /// <summary>Marks a unit dead: publishes death and clears its modifiers.</summary>
        public void MarkDead(FixedUnit unit, int killer, string abilityId = null, uint castId = 0u)
        {
            if (unit == null || !unit.IsAlive)
            {
                return;
            }

            unit.IsAlive = false;
            Events.Publish(new FixedEventArgs(FixedEvents.Death, unit.Id, killer, 0, abilityId, castId: castId));
            Modifiers.RemoveAllFrom(unit.Id);
        }

        /// <summary>Revives a unit at a raw fraction of its max health (never at 0).</summary>
        public void Revive(FixedUnit unit, int healthFractionRaw)
        {
            if (unit == null || unit.IsAlive)
            {
                return;
            }

            unit.IsAlive = true;
            int max = unit.MaxHealthRaw;
            if (max > 0)
            {
                int fraction = FixedMath.Clamp(healthFractionRaw, 0, Rules.Format.One);
                int target = FixedMath.MulDivToInt(max, fraction, Rules.Format.One);
                unit.SetCurrent(FixedResources.Health, target > 0 ? target : FixedMath.Min(Rules.Format.One, max));
            }

            Events.Publish(new FixedEventArgs(FixedEvents.Revive, unit.Id, 0, unit.HealthRaw));
        }

        public FixedAbilitySlot GrantAbility(int unit, string abilityId)
        {
            if (!_units.ContainsKey(unit) || !TryGetAbility(abilityId, out FixedAbilityBlueprint blueprint))
            {
                return null;
            }

            if (!_slots.TryGetValue(unit, out Dictionary<string, FixedAbilitySlot> unitSlots))
            {
                unitSlots = new Dictionary<string, FixedAbilitySlot>(FixedIdComparer.Instance);
                _slots[unit] = unitSlots;
            }

            if (!unitSlots.TryGetValue(blueprint.Id, out FixedAbilitySlot slot))
            {
                slot = new FixedAbilitySlot(blueprint);
                unitSlots[blueprint.Id] = slot;
                SlotsVersion++;
            }

            return slot;
        }

        public bool RevokeAbility(int unit, string abilityId)
        {
            if (!string.IsNullOrEmpty(abilityId) && _slots.TryGetValue(unit, out Dictionary<string, FixedAbilitySlot> slots) &&
                slots.Remove(abilityId))
            {
                SlotsVersion++;
                return true;
            }

            return false;
        }

        public bool TryGetSlot(int unit, string abilityId, out FixedAbilitySlot slot)
        {
            slot = null;
            return !string.IsNullOrEmpty(abilityId) && _slots.TryGetValue(unit, out Dictionary<string, FixedAbilitySlot> slots) &&
                   slots.TryGetValue(abilityId, out slot);
        }

        public void GetSlots(int unit, List<FixedAbilitySlot> results)
        {
            results.Clear();
            if (_slots.TryGetValue(unit, out Dictionary<string, FixedAbilitySlot> slots))
            {
                foreach (KeyValuePair<string, FixedAbilitySlot> pair in slots)
                {
                    results.Add(pair.Value);
                }
            }
        }

        public bool SetAbilityLevel(int unit, string abilityId, int level)
        {
            if (!TryGetSlot(unit, abilityId, out FixedAbilitySlot slot))
            {
                return false;
            }

            slot.Level = level;
            return true;
        }

        /// <summary>Applies a catalog modifier directly (the apply_modifier op's path).</summary>
        public FixedModifierApplyResult ApplyModifier(string modifierId, int caster, int target, string sourceAbilityId = null,
            int abilityLevel = 1)
        {
            return TryGetModifier(modifierId, out FixedModifierBlueprint blueprint)
                ? Modifiers.Apply(blueprint, caster, target, sourceAbilityId, abilityLevel)
                : new FixedModifierApplyResult(null, false);
        }

        /// <summary>
        ///     Validates and executes a cast: grant, liveness, stun / silence, cooldown and charges, target, team, range
        ///     and costs; then pays, starts the cooldown, publishes ability_cast and runs the cast effects and either the
        ///     impact effects or a projectile spawn.
        /// </summary>
        public FixedCastResult Cast(in FixedCastRequest request)
        {
            if (_castDepth >= MaxCastDepth)
            {
                return FixedCastResult.Fail(FixedCastFailure.NestingLimit);
            }

            _castDepth++;
            try
            {
                return CastInternal(in request);
            }
            finally
            {
                _castDepth--;
            }
        }

        /// <summary>Host callback: a projectile of <paramref name="castId" /> hit a unit (0 for none) at a point.</summary>
        public bool NotifyProjectileHit(uint castId, int hitUnit, FixedVec3 hitPoint)
        {
            if (!_pendingProjectiles.TryGetValue(castId, out PendingProjectile pending))
            {
                return false;
            }

            if (GetUnit(pending.Caster) == null)
            {
                RemovePending(castId);
                return false;
            }

            uint hitSeed = FixedSeeds.Mix(pending.Seed, pending.HitIndex);
            pending.HitIndex++;
            pending.ExpireAtTick = PendingProjectileTimeoutTicks > 0 ? TickCount + PendingProjectileTimeoutTicks : long.MaxValue;
            FixedAbilityBlueprint ability = pending.Ability;
            FixedEffectContext context = RentContext(pending.Caster, ability.Id, hitSeed);
            try
            {
                context.TargetPoint = hitPoint;
                context.HasTargetPoint = true;
                context.CastId = castId;
                context.AbilityLevel = pending.Level;
                context.Specials = ability.Specials;
                context.Origin = FixedEffectOrigin.ProjectileHit;
                pending.CopySpentTo(context);
                FixedUnit hit = GetUnit(hitUnit);
                if (hit != null && hit.IsAlive)
                {
                    context.PrimaryTargets.Add(hitUnit);
                }

                ExecuteEffects(ability.ImpactEffects, context);
            }
            finally
            {
                ReturnContext(context);
            }

            return true;
        }

        /// <summary>Host callback: the projectile of a cast is finished.</summary>
        public void ReleaseProjectileCast(uint castId)
        {
            RemovePending(castId);
        }

        /// <summary>Number of projectile casts waiting for hits.</summary>
        public int PendingProjectileCount => _pendingProjectiles.Count;

        /// <summary>Runs effect nodes in a context created by the system (depth-capped, allocation-free).</summary>
        public void ExecuteEffects(List<FixedEffectNode> nodes, FixedEffectContext context)
        {
            if (nodes == null || nodes.Count == 0 || context == null || context.Depth >= FixedEffectContext.MaxDepth)
            {
                return;
            }

            _executionDepth++;
            try
            {
                List<int> targets = RentScratch(_targetScratchPool, _executionDepth - 1);
                IFixedTargetResolver targeting = Rules.Targeting;
                int one = Rules.Format.One;
                for (int i = 0; i < nodes.Count; i++)
                {
                    FixedEffectNode node = nodes[i];
                    if (node == null || string.IsNullOrEmpty(node.OpId))
                    {
                        continue;
                    }

                    int chance = node.ResolveChanceRaw(context.AbilityLevel);
                    if (chance < one && context.Random.NextRaw(one) >= chance)
                    {
                        continue;
                    }

                    if (!Ops.TryGet(node.OpId, out IFixedEffectOperation op))
                    {
                        continue;
                    }

                    targets.Clear();
                    targeting.Resolve(node, context, targets);
                    op.Execute(context, node, targets);
                }

                targets.Clear();
            }
            finally
            {
                _executionDepth--;
            }
        }

        /// <summary>Advances one tick: projectiles' leak guard, modifiers, zones, cooldowns, regeneration.</summary>
        public void Step()
        {
            TickCount++;
            PrunePendingProjectiles();
            Modifiers.Step();
            StepZones();
            foreach (KeyValuePair<int, Dictionary<string, FixedAbilitySlot>> pair in _slots)
            {
                foreach (KeyValuePair<string, FixedAbilitySlot> slot in pair.Value)
                {
                    slot.Value.Step();
                }
            }

            _stepUnitScratch.Clear();
            for (int i = 0; i < _unitOrder.Count; i++)
            {
                _stepUnitScratch.Add(_unitOrder[i]);
            }

            int ticksPerSecond = Rules.TicksPerSecond;
            for (int i = 0; i < _stepUnitScratch.Count; i++)
            {
                FixedUnit unit = _stepUnitScratch[i];
                if (!unit.IsAlive || !_units.ContainsKey(unit.Id))
                {
                    continue;
                }

                int healthRegen = unit.GetProperty(FixedProperties.HealthRegen);
                if (healthRegen > 0)
                {
                    unit.Increase(FixedResources.Health, Regen(ref unit.HealthRegenCarry, healthRegen, ticksPerSecond));
                }

                int manaRegen = unit.GetProperty(FixedProperties.ManaRegen);
                if (manaRegen > 0)
                {
                    unit.Increase(FixedResources.Mana, Regen(ref unit.ManaRegenCarry, manaRegen, ticksPerSecond));
                }
            }

            _stepUnitScratch.Clear();
        }

        /// <summary>Runs <paramref name="ticks" /> steps (no cap).</summary>
        public void Step(int ticks)
        {
            for (int i = 0; i < ticks; i++)
            {
                Step();
            }
        }

        /// <summary>Catch-up: runs up to <see cref="MaxStepsPerAdvance" /> of the pending ticks; returns how many ran.</summary>
        public int Advance(int pendingTicks)
        {
            int cap = MaxStepsPerAdvance > 0 ? MaxStepsPerAdvance : int.MaxValue;
            int steps = pendingTicks < cap ? pendingTicks : cap;
            for (int i = 0; i < steps; i++)
            {
                Step();
            }

            return steps > 0 ? steps : 0;
        }

        /// <summary>True when <paramref name="target" /> passes the filter relative to <paramref name="caster" /> (self is an ally).</summary>
        public static bool MatchesTeamFilter(FixedUnit caster, FixedUnit target, FixedTeamFilter filter)
        {
            if (caster == null || target == null)
            {
                return false;
            }

            switch (filter)
            {
                case FixedTeamFilter.Allies:
                    return caster.IsAllyOf(target);
                case FixedTeamFilter.Enemies:
                    return caster.IsEnemyOf(target);
                default:
                    return true;
            }
        }

        internal void LaunchProjectile(FixedAbilityBlueprint payload, int caster, FixedVec3 origin, FixedVec3 direction,
            int target, string archetypeId, int speedRaw, int level, uint seed, FixedEffectContext spentSource = null)
        {
            uint castId = _nextCastId++;
            PendingProjectile pending = RentPending(payload, caster, seed, level);
            pending.CaptureSpent(spentSource);
            _pendingProjectiles[castId] = pending;
            _world.RequestSpawn(new FixedSpawnRequest(archetypeId, caster, origin, direction, target, payload.Id, speedRaw,
                castId));
        }

        internal void ExecutePayload(FixedAbilityBlueprint payload, FixedEffectContext parent, List<int> targets)
        {
            if (payload == null || payload.ImpactEffects.Count == 0 || parent.Depth + 1 >= FixedEffectContext.MaxDepth)
            {
                return;
            }

            uint seed = (uint)parent.Random.NextInt(int.MaxValue) | 1u;
            FixedEffectContext context = RentContext(parent.Caster, payload.Id, seed);
            try
            {
                context.Depth = parent.Depth + 1;
                context.CastId = parent.CastId;
                context.AbilityLevel = parent.AbilityLevel;
                context.Specials = payload.Specials;
                context.Origin = parent.Origin;
                context.SourceStacks = parent.SourceStacks;
                context.TargetPoint = parent.TargetPoint;
                context.HasTargetPoint = parent.HasTargetPoint;
                context.CopySpentFrom(parent);
                for (int i = 0; i < targets.Count; i++)
                {
                    context.PrimaryTargets.Add(targets[i]);
                }

                ExecuteEffects(payload.ImpactEffects, context);
            }
            finally
            {
                ReturnContext(context);
            }
        }

        internal FixedEffectContext RentContext(int caster, string abilityId, uint seed)
        {
            int last = _contextPool.Count - 1;
            FixedEffectContext context;
            if (last >= 0)
            {
                context = _contextPool[last];
                _contextPool.RemoveAt(last);
            }
            else
            {
                context = new FixedEffectContext();
            }

            context.Reset(this, caster, abilityId, seed);
            return context;
        }

        internal void ReturnContext(FixedEffectContext context)
        {
            context.Clear();
            _contextPool.Add(context);
        }

        private FixedUnit AddUnit(int id, int team)
        {
            FixedUnit unit = new FixedUnit(id, team, this);
            _units[id] = unit;
            _unitOrder.Add(unit);
            return unit;
        }

        private FixedCastResult CastInternal(in FixedCastRequest request)
        {
            FixedUnit caster = GetUnit(request.Caster);
            if (caster == null)
            {
                return FixedCastResult.Fail(FixedCastFailure.UnknownCaster);
            }

            if (!TryGetAbility(request.AbilityId, out _))
            {
                return FixedCastResult.Fail(FixedCastFailure.UnknownAbility);
            }

            if (!TryGetSlot(request.Caster, request.AbilityId, out FixedAbilitySlot slot))
            {
                return FixedCastResult.Fail(FixedCastFailure.NotGranted);
            }

            FixedAbilityBlueprint ability = slot.Blueprint;
            if (!caster.IsAlive)
            {
                return FixedCastResult.Fail(FixedCastFailure.CasterDead);
            }

            if (caster.HasState(FixedStates.Stunned))
            {
                return FixedCastResult.Fail(FixedCastFailure.Stunned);
            }

            if (caster.HasState(FixedStates.Silenced))
            {
                return FixedCastResult.Fail(FixedCastFailure.Silenced);
            }

            if (!slot.IsReady)
            {
                return FixedCastResult.Fail(slot.UsesCharges ? FixedCastFailure.NoCharges : FixedCastFailure.OnCooldown);
            }

            int level = slot.Level;

            FixedUnit targetUnit = null;
            if (ability.Targeting == FixedTargeting.Unit)
            {
                targetUnit = GetUnit(request.TargetUnit);
                if (targetUnit == null)
                {
                    return FixedCastResult.Fail(FixedCastFailure.InvalidTarget);
                }

                if (!targetUnit.IsAlive)
                {
                    return FixedCastResult.Fail(FixedCastFailure.TargetDead);
                }

                if (targetUnit.Id != caster.Id && targetUnit.HasState(FixedStates.Untargetable))
                {
                    return FixedCastResult.Fail(FixedCastFailure.TargetUntargetable);
                }

                if (!MatchesTeamFilter(caster, targetUnit, ability.TeamFilter))
                {
                    return FixedCastResult.Fail(FixedCastFailure.WrongTeam);
                }
            }

            int rangeRaw = ability.ResolveRangeRaw(level);
            if (rangeRaw > 0 && _world.TryGetPosition(caster.Id, out FixedVec3 casterPosition))
            {
                long range = (long)rangeRaw + caster.GetProperty(FixedProperties.CastRangeBonus);
                bool hasTarget = false;
                FixedVec3 targetPosition = FixedVec3.Zero;
                if (ability.Targeting == FixedTargeting.Unit && targetUnit != null &&
                    _world.TryGetPosition(targetUnit.Id, out FixedVec3 position))
                {
                    hasTarget = true;
                    targetPosition = position;
                }
                else if (ability.Targeting == FixedTargeting.Point)
                {
                    hasTarget = true;
                    targetPosition = request.TargetPoint;
                }

                if (hasTarget && (range < 0L ||
                                  !FixedVec3.IsWithinRadius(targetPosition, casterPosition, FixedMath.ClampToInt(range))))
                {
                    return FixedCastResult.Fail(FixedCastFailure.OutOfRange);
                }
            }

            List<FixedSpentResource> spent = RentScratch(_spentScratchPool, _castDepth);
            spent.Clear();
            if (!TryPayCosts(caster, ability, level, targetUnit != null ? targetUnit.Id : 0, spent))
            {
                spent.Clear();
                return FixedCastResult.Fail(FixedCastFailure.NotEnoughResources);
            }

            IFixedCooldownRule cooldowns = Rules.Cooldowns;
            if (slot.UsesCharges)
            {
                slot.Charges--;
                if (slot.CooldownRemainingTicks <= 0)
                {
                    slot.StartCooldown(cooldowns.ResolveCooldownTicks(caster, ability, ability.ResolveChargeRestoreTicks(level)));
                }
            }
            else
            {
                slot.Charges = 0;
                slot.StartCooldown(cooldowns.ResolveCooldownTicks(caster, ability, ability.ResolveCooldownTicks(level)));
            }

            uint castId = _nextCastId++;
            try
            {
                Events.Publish(new FixedEventArgs(FixedEvents.AbilityCast, caster.Id, targetUnit != null ? targetUnit.Id : 0,
                    0, ability.Id, castId: castId));
                ExecuteAcceptedCast(in request, caster, ability, slot, targetUnit, castId, spent);
            }
            finally
            {
                spent.Clear();
            }

            return FixedCastResult.Ok(castId);
        }

        private void ExecuteAcceptedCast(in FixedCastRequest request, FixedUnit caster, FixedAbilityBlueprint ability,
            FixedAbilitySlot slot, FixedUnit targetUnit, uint castId, List<FixedSpentResource> spent)
        {
            // WHY: read after the ability_cast receipt, as the float core does.
            int level = slot.Level;
            uint seed = FixedSeeds.ForCast(request.Seed, castId, caster.Id);
            FixedVec3 reach = ability.Targeting == FixedTargeting.Direction
                ? DirectionReach(caster, ability, level, request.Direction)
                : FixedVec3.Zero;
            FixedEffectContext context = RentContext(caster.Id, ability.Id, seed);
            try
            {
                switch (ability.Targeting)
                {
                    case FixedTargeting.Unit:
                        if (targetUnit != null)
                        {
                            context.PrimaryTargets.Add(targetUnit.Id);
                        }

                        break;
                    case FixedTargeting.Point:
                        context.TargetPoint = request.TargetPoint;
                        context.HasTargetPoint = true;
                        break;
                    case FixedTargeting.Direction:
                        if (_world.TryGetPosition(caster.Id, out FixedVec3 origin))
                        {
                            context.TargetPoint = origin + reach;
                            context.HasTargetPoint = true;
                        }

                        break;
                    default:
                        context.PrimaryTargets.Add(caster.Id);
                        break;
                }

                context.CastId = castId;
                context.AbilityLevel = level;
                context.Specials = ability.Specials;
                context.Origin = FixedEffectOrigin.Cast;
                for (int i = 0; i < spent.Count; i++)
                {
                    context.AddSpent(spent[i].ResourceId, spent[i].AmountRaw);
                }

                if (ability.CastEffects.Count > 0)
                {
                    ExecuteEffects(ability.CastEffects, context);
                }

                if (ability.Delivery == FixedDelivery.Projectile)
                {
                    PendingProjectile pending = RentPending(ability, caster.Id, seed, level);
                    pending.CaptureSpent(context);
                    _pendingProjectiles[castId] = pending;
                    _world.TryGetPosition(caster.Id, out FixedVec3 origin);
                    FixedVec3 direction = request.Direction;
                    if (ability.Targeting == FixedTargeting.Direction)
                    {
                        direction = reach;
                    }
                    else if (direction.IsZero && targetUnit != null &&
                             _world.TryGetPosition(targetUnit.Id, out FixedVec3 targetPosition))
                    {
                        direction = targetPosition - origin;
                    }
                    else if (direction.IsZero && ability.Targeting == FixedTargeting.Point)
                    {
                        direction = request.TargetPoint - origin;
                    }

                    _world.RequestSpawn(new FixedSpawnRequest(ability.ProjectileArchetypeId, caster.Id, origin, direction,
                        targetUnit != null ? targetUnit.Id : 0, ability.Id, ability.ResolveProjectileSpeedRaw(level), castId));
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

        private FixedVec3 DirectionReach(FixedUnit caster, FixedAbilityBlueprint ability, int level, FixedVec3 direction)
        {
            int one = Rules.Format.One;
            long length = direction.LengthFloor;
            int rangeRaw = ability.ResolveRangeRaw(level);
            long reach = rangeRaw > 0
                ? (long)rangeRaw + caster.GetProperty(FixedProperties.CastRangeBonus)
                : FixedMath.Max(length, 10L * one);
            int clamped = FixedMath.ClampToInt(FixedMath.Max(0L, reach));
            return length > 0L ? direction.WithLength(clamped) : new FixedVec3(0, 0, clamped);
        }

        private bool TryPayCosts(FixedUnit caster, FixedAbilityBlueprint ability, int level, int target,
            List<FixedSpentResource> spent)
        {
            int count = ability.Costs.Count;
            if (count == 0)
            {
                return true;
            }

            List<FixedAbilityCost> captured = RentScratch(_costScratchPool, _costDepth);
            _costDepth++;
            try
            {
                captured.Clear();
                for (int i = 0; i < count; i++)
                {
                    FixedAbilityCost cost = ability.Costs[i];
                    FixedAbilityCost leveled = cost;
                    leveled.AmountRaw = cost.ResolveAmountRaw(ability.Specials, this, caster.Id, level, target);
                    int amount = Rules.Costs.ResolveCost(caster, ability, leveled);
                    if (cost.Mode != FixedCostMode.Fixed)
                    {
                        if (string.IsNullOrEmpty(cost.ResourceId))
                        {
                            return false;
                        }

                        long available = caster.GetCurrent(cost.ResourceId);
                        for (int previous = 0; previous < captured.Count; previous++)
                        {
                            if (FixedIdComparer.Same(captured[previous].ResourceId, cost.ResourceId))
                            {
                                available -= FixedMath.Max(0, captured[previous].AmountRaw);
                            }
                        }

                        int availableRaw = FixedMath.ClampToInt(FixedMath.Max(0L, available));
                        if (availableRaw < cost.MinAmountRaw)
                        {
                            return false;
                        }

                        amount = cost.Mode == FixedCostMode.All ? availableRaw : FixedMath.Min(availableRaw, amount);
                    }

                    captured.Add(new FixedAbilityCost(cost.ResourceId, amount));
                }

                for (int i = 0; i < captured.Count; i++)
                {
                    FixedAbilityCost cost = captured[i];
                    if (cost.AmountRaw <= 0)
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(cost.ResourceId) || !caster.CanSpend(cost.ResourceId, cost.AmountRaw))
                    {
                        return false;
                    }

                    long total = cost.AmountRaw;
                    for (int previous = 0; previous < i; previous++)
                    {
                        if (FixedIdComparer.Same(captured[previous].ResourceId, cost.ResourceId))
                        {
                            total += FixedMath.Max(0, captured[previous].AmountRaw);
                        }
                    }

                    if (caster.GetCurrent(cost.ResourceId) < total)
                    {
                        return false;
                    }
                }

                for (int i = 0; i < captured.Count; i++)
                {
                    FixedAbilityCost cost = captured[i];
                    if (cost.AmountRaw > 0 && !caster.TrySpend(cost.ResourceId, cost.AmountRaw))
                    {
                        return false;
                    }

                    if (cost.AmountRaw > 0)
                    {
                        spent.Add(new FixedSpentResource(cost.ResourceId, cost.AmountRaw));
                    }
                }

                return true;
            }
            finally
            {
                captured.Clear();
                _costDepth--;
            }
        }

        private void OnModifierApplied(FixedModifierInstance instance, bool createdNew)
        {
            SyncResourceMaxBonuses(instance.Owner);
            Events.Publish(new FixedEventArgs(FixedEvents.ModifierApplied, instance.Owner, instance.Caster, instance.Stacks,
                instance.SourceAbilityId, instance.Blueprint.Id));
            if (createdNew && instance.TriggerFlags != 0)
            {
                InitializeStateWatch(instance);
                FireLifecycleTriggers(instance, FixedModifierTrigger.Applied);
            }

            EvaluateStateWatchers(instance.Owner);
        }

        private void OnModifierRemoved(FixedModifierInstance instance, bool expired)
        {
            SyncResourceMaxBonuses(instance.Owner);
            Events.Publish(new FixedEventArgs(FixedEvents.ModifierRemoved, instance.Owner, instance.Caster, expired ? 1 : 0,
                instance.SourceAbilityId, instance.Blueprint.Id));
            if (instance.TriggerFlags != 0)
            {
                FireLifecycleTriggers(instance, FixedModifierTrigger.Removed);
            }

            EvaluateStateWatchers(instance.Owner);
        }

        private void SyncResourceMaxBonuses(int owner)
        {
            if (_units.TryGetValue(owner, out FixedUnit unit))
            {
                unit.SyncResourceMaxBonuses();
            }
        }

        private void OnModifierTick(FixedModifierInstance instance)
        {
            List<FixedEffectNode> effects = instance.Blueprint.TickEffects;
            if (effects == null || effects.Count == 0)
            {
                return;
            }

            uint seed = FixedSeeds.Roll(unchecked((uint)instance.Owner * 747796405u + (uint)instance.InstanceId),
                instance.TickRollIndex++);
            FixedEffectContext context = RentContext(instance.Caster, instance.SourceAbilityId, seed);
            try
            {
                context.AbilityLevel = instance.AbilityLevel;
                if (TryGetAbility(instance.SourceAbilityId, out FixedAbilityBlueprint ability))
                {
                    context.Specials = ability.Specials;
                }

                context.Origin = FixedEffectOrigin.ModifierTick;
                context.SourceStacks = instance.Stacks;
                instance.CopySpentTo(context);
                context.PrimaryTargets.Add(instance.Owner);
                ExecuteEffects(effects, context);
            }
            finally
            {
                ReturnContext(context);
            }
        }

        private static int Regen(ref long carry, int perSecondRaw, int ticksPerSecond)
        {
            carry += perSecondRaw;
            long give = carry / ticksPerSecond;
            carry -= give * ticksPerSecond;
            return FixedMath.ClampToInt(give);
        }

        private static List<T> RentScratch<T>(List<List<T>> pool, int depth)
        {
            while (pool.Count <= depth)
            {
                pool.Add(new List<T>(16));
            }

            return pool[depth];
        }

        private PendingProjectile RentPending(FixedAbilityBlueprint ability, int caster, uint seed, int level)
        {
            int last = _pendingPool.Count - 1;
            PendingProjectile pending;
            if (last >= 0)
            {
                pending = _pendingPool[last];
                _pendingPool.RemoveAt(last);
            }
            else
            {
                pending = new PendingProjectile();
            }

            pending.Ability = ability;
            pending.Caster = caster;
            pending.Seed = seed;
            pending.Level = level;
            pending.HitIndex = 0u;
            pending.ExpireAtTick = PendingProjectileTimeoutTicks > 0 ? TickCount + PendingProjectileTimeoutTicks : long.MaxValue;
            return pending;
        }

        private void RemovePending(uint castId)
        {
            if (_pendingProjectiles.TryGetValue(castId, out PendingProjectile pending))
            {
                _pendingProjectiles.Remove(castId);
                pending.Ability = null;
                pending.ClearSpent();
                _pendingPool.Add(pending);
            }
        }

        private void PrunePendingProjectiles()
        {
            if (_pendingProjectiles.Count == 0)
            {
                return;
            }

            _pendingPruneScratch.Clear();
            foreach (KeyValuePair<uint, PendingProjectile> pair in _pendingProjectiles)
            {
                if (TickCount >= pair.Value.ExpireAtTick)
                {
                    _pendingPruneScratch.Add(pair.Key);
                }
            }

            for (int i = 0; i < _pendingPruneScratch.Count; i++)
            {
                RemovePending(_pendingPruneScratch[i]);
            }

            _pendingPruneScratch.Clear();
        }

        private sealed class PendingProjectile
        {
            public FixedAbilityBlueprint Ability;
            public int Caster;
            public uint Seed;
            public int Level;
            public uint HitIndex;
            public long ExpireAtTick;

            /// <summary>What the launching cast paid, replayed into every hit; null until first needed.</summary>
            private List<FixedSpentResource> _spent;

            public void CaptureSpent(FixedEffectContext source)
            {
                _spent?.Clear();
                int count = source != null ? source.SpentCount : 0;
                if (count == 0)
                {
                    return;
                }

                _spent ??= new List<FixedSpentResource>(count);
                for (int i = 0; i < count; i++)
                {
                    _spent.Add(source.GetSpentAt(i));
                }
            }

            public void CopySpentTo(FixedEffectContext context)
            {
                if (_spent == null)
                {
                    return;
                }

                for (int i = 0; i < _spent.Count; i++)
                {
                    context.AddSpent(_spent[i].ResourceId, _spent[i].AmountRaw);
                }
            }

            public void ClearSpent()
            {
                _spent?.Clear();
            }
        }
    }
}
