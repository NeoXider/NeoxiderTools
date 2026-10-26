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
    public sealed class AbilitySystem
    {
        private readonly Dictionary<UnitId, AbilityUnit> _units = new Dictionary<UnitId, AbilityUnit>();

        private readonly Dictionary<string, ModifierBlueprint> _modifierCatalog =
            new Dictionary<string, ModifierBlueprint>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, AbilityBlueprint> _abilityCatalog =
            new Dictionary<string, AbilityBlueprint>(StringComparer.OrdinalIgnoreCase);

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

        private readonly List<UnitId> _unitScratch = new List<UnitId>(16);
        private readonly List<UnitId> _tickUnitScratch = new List<UnitId>(16);
        private readonly List<AreaHit> _areaScratch = new List<AreaHit>(16);
        private readonly List<uint> _pendingPruneScratch = new List<uint>(8);

        private static readonly Comparison<AreaHit> AreaHitComparison = CompareAreaHits;

        private IAbilityWorldAdapter _world = NullWorldAdapter.Instance;
        private uint _nextUnitId = 1;
        private uint _nextCastId = 1;
        private int _executionDepth;
        private int _reactionDepth;
        private float _time;

        public AbilitySystem()
        {
            Modifiers = new ModifierEngine();
            Events = new AbilityEventBus(HandleModifierReactions);
            Ops = new EffectOpRegistry();

            Modifiers.Applied += OnModifierApplied;
            Modifiers.Removed += OnModifierRemoved;
            Modifiers.TickDue += OnModifierTick;

            DefaultEffectOps.RegisterAll(Ops);
        }

        public ModifierEngine Modifiers { get; }
        public AbilityEventBus Events { get; }
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

        public void RegisterModifier(ModifierBlueprint blueprint)
        {
            if (blueprint != null && !string.IsNullOrEmpty(blueprint.Id))
            {
                _modifierCatalog[blueprint.Id] = blueprint;
            }
        }

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

        public bool TryGetAbility(string id, out AbilityBlueprint blueprint)
        {
            blueprint = null;
            return !string.IsNullOrEmpty(id) && _abilityCatalog.TryGetValue(id, out blueprint);
        }

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

        public void DestroyUnit(UnitId id)
        {
            if (!_units.Remove(id))
            {
                return;
            }

            Modifiers.RemoveAllFrom(id);
            if (_slots.Remove(id))
            {
                SlotsVersion++;
            }
        }

        public AbilityUnit GetUnit(UnitId id)
        {
            return _units.TryGetValue(id, out AbilityUnit unit) ? unit : null;
        }

        public bool TryGetUnit(UnitId id, out AbilityUnit unit)
        {
            return _units.TryGetValue(id, out unit);
        }

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
                unitSlots = new Dictionary<string, AbilitySlot>(StringComparer.OrdinalIgnoreCase);
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

        public bool TryGetSlot(UnitId unit, string abilityId, out AbilitySlot slot)
        {
            slot = null;
            return !string.IsNullOrEmpty(abilityId) &&
                   _slots.TryGetValue(unit, out Dictionary<string, AbilitySlot> unitSlots) &&
                   unitSlots.TryGetValue(abilityId, out slot);
        }

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

        public CastResult Cast(CastRequest request)
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
                return CastResult.Fail(ability.UsesCharges
                    ? CastFailureReason.NoCharges
                    : CastFailureReason.OnCooldown);
            }

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
            if (ability.Range > 0f && _world.TryGetPosition(caster.Id, out Vector3 casterPos))
            {
                float effectiveRange = ability.Range + caster.GetProperty(AbilityProperties.CastRangeBonus);
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

                if (targetPos.HasValue && (targetPos.Value - casterPos).magnitude > effectiveRange)
                {
                    return CastResult.Fail(CastFailureReason.OutOfRange);
                }
            }

            // WHY: payment notifications are synchronous and may change the blueprint or mana-cost
            // multiplier. Capture ids and effective amounts once before any payment callback can run.
            int costCount = ability.Costs.Count;
            AbilityCost[] capturedCosts = costCount > 0 ? new AbilityCost[costCount] : Array.Empty<AbilityCost>();
            for (int i = 0; i < costCount; i++)
            {
                AbilityCost cost = ability.Costs[i];
                float amount = EffectiveCost(caster, cost);
                if (float.IsNaN(amount) || float.IsInfinity(amount))
                {
                    // WHY: a NaN entry poisons cumulative costs and makes affordability comparisons
                    // false; reject every invalid amount before any resource is paid.
                    return CastResult.Fail(CastFailureReason.NotEnoughResources);
                }

                capturedCosts[i] = new AbilityCost(cost.ResourceId, amount);
            }

            // WHY: a per-operation decrease cap applies to each authored cost, while affordability
            // applies to their cumulative resource total. Validate both before paying any entry.
            for (int i = 0; i < capturedCosts.Length; i++)
            {
                AbilityCost cost = capturedCosts[i];
                if (cost.Amount <= 0f)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(cost.ResourceId) ||
                    !caster.Resources.CanSpend(cost.ResourceId, cost.Amount, out _))
                {
                    return CastResult.Fail(CastFailureReason.NotEnoughResources);
                }

                float total = cost.Amount;
                for (int previous = 0; previous < i; previous++)
                {
                    AbilityCost previousCost = capturedCosts[previous];
                    if (string.Equals(previousCost.ResourceId, cost.ResourceId, StringComparison.OrdinalIgnoreCase))
                    {
                        total += MathF.Max(0f, previousCost.Amount);
                    }
                }

                if (float.IsNaN(total) || float.IsInfinity(total) ||
                    caster.Resources.GetCurrent(cost.ResourceId) < total)
                {
                    return CastResult.Fail(CastFailureReason.NotEnoughResources);
                }
            }

            for (int i = 0; i < capturedCosts.Length; i++)
            {
                AbilityCost cost = capturedCosts[i];
                if (cost.Amount > 0f && !caster.Resources.TrySpend(cost.ResourceId, cost.Amount, out _))
                {
                    // WHY: a preceding payment callback may invalidate a later entry after preflight.
                    // Never accept a clipped payment; earlier successful payments are not rolled back.
                    return CastResult.Fail(CastFailureReason.NotEnoughResources);
                }
            }

            float cdr = Mathf.Clamp(caster.GetProperty(AbilityProperties.CooldownReductionPercent), 0f, 90f);
            if (ability.UsesCharges)
            {
                slot.Charges--;
                if (slot.CooldownRemaining <= 0f)
                {
                    float restore = ability.ChargeRestoreTime > 0f ? ability.ChargeRestoreTime : ability.Cooldown;
                    slot.StartCooldown(restore * (1f - cdr / 100f));
                }
            }
            else
            {
                slot.Charges = 0;
                slot.StartCooldown(ability.Cooldown * (1f - cdr / 100f));
            }

            uint castId = _nextCastId++;
            Events.Publish(new AbilityEventArgs(AbilityEvents.AbilityCast, caster.Id,
                targetUnit?.Id ?? UnitId.None, 0f, ability.Id, castId: castId));

            uint seed = request.Seed != 0 ? request.Seed : castId * 2654435761u ^ caster.Id.Value;
            EffectContext context = BuildCastContext(caster, ability, targetUnit, request, seed);
            context.CastId = castId;
            context.AbilityLevel = slot.Level;
            context.Specials = ability.Specials;

            // WHY: cast-phase effects run immediately, ahead of delivery, regardless of delivery type.
            if (ability.CastEffects.Count > 0)
            {
                ExecuteEffects(ability.CastEffects, context);
            }

            if (ability.Delivery == AbilityDeliveryType.Projectile)
            {
                _pendingProjectiles[castId] = new PendingProjectileCast(ability, caster.Id, seed, slot.Level)
                {
                    ExpireAt = _time + PendingProjectileTimeout
                };
                Vector3 origin = default;
                _world.TryGetPosition(caster.Id, out origin);
                Vector3 direction = request.Direction;
                if (direction == default && targetUnit != null &&
                    _world.TryGetPosition(targetUnit.Id, out Vector3 targetPosition))
                {
                    direction = targetPosition - origin;
                }
                else if (direction == default && ability.Targeting == TargetingMode.Point)
                {
                    direction = request.TargetPoint - origin;
                }

                _world.RequestSpawn(new SpawnRequest(ability.ProjectileArchetypeId, caster.Id, origin,
                    direction, targetUnit?.Id ?? UnitId.None, ability.Id, ability.ProjectileSpeed, castId));
            }
            else if (ability.ImpactEffects.Count > 0)
            {
                ExecuteEffects(ability.ImpactEffects, context);
            }

            return CastResult.Ok(castId);
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
                _pendingProjectiles.Remove(castId);
                return false;
            }

            // WHY: each hit gets its own mixed seed — replaying the raw cast seed would roll
            // identical crit/chance outcomes on every pierced target and correlate impact with cast.
            uint hitSeed = MixSeed(pending.Seed, pending.HitIndex);
            pending.HitIndex++;
            // WHY: an actively hitting projectile is alive; keep the leak guard from expiring it.
            pending.ExpireAt = _time + PendingProjectileTimeout;

            EffectContext context = new EffectContext(this, pending.Caster, pending.Ability.Id,
                new XorShiftRandom(hitSeed))
            {
                TargetPoint = hitPoint,
                HasTargetPoint = true,
                CastId = castId,
                AbilityLevel = pending.Level,
                Specials = pending.Ability.Specials
            };
            AbilityUnit hit = GetUnit(hitUnit);
            if (hit != null && hit.IsAlive)
            {
                context.PrimaryTargets.Add(hitUnit);
            }

            ExecuteEffects(pending.Ability.ImpactEffects, context);
            return true;
        }

        /// <summary>Host callback: the projectile of a cast is finished (expired or destroyed).</summary>
        public void ReleaseProjectileCast(uint castId)
        {
            _pendingProjectiles.Remove(castId);
        }

        /// <summary>Executes a node list in a context. Public so hosts can run data-driven effects directly.</summary>
        public void ExecuteEffects(List<EffectNodeData> nodes, EffectContext context)
        {
            if (nodes == null || nodes.Count == 0 || context == null || context.Depth >= EffectContext.MaxDepth)
            {
                return;
            }

            _executionDepth++;
            try
            {
                // WHY: op.Execute may re-enter ExecuteEffects via event reactions; the per-depth
                // scratch keeps the list an outer op is still iterating intact.
                List<UnitId> targetScratch = RentScratch(_targetScratchPool, _executionDepth - 1);
                for (int i = 0; i < nodes.Count; i++)
                {
                    EffectNodeData node = nodes[i];
                    if (node == null || string.IsNullOrEmpty(node.OpId))
                    {
                        continue;
                    }

                    if (node.Chance < 1f && context.Random.NextFloat() >= node.Chance)
                    {
                        continue;
                    }

                    if (!Ops.TryGet(node.OpId, out IEffectOperation op))
                    {
                        continue;
                    }

                    targetScratch.Clear();
                    ResolveTargets(node, context, targetScratch);
                    op.Execute(context, node, targetScratch);
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
        ///     Returns false when the unit has no such slot.
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
            PrunePendingProjectiles();

            Modifiers.Tick(deltaTime);

            foreach (KeyValuePair<UnitId, Dictionary<string, AbilitySlot>> pair in _slots)
            {
                foreach (KeyValuePair<string, AbilitySlot> slotPair in pair.Value)
                {
                    slotPair.Value.Tick(deltaTime);
                }
            }

            // WHY: resource callbacks may execute area effects, which reuse _unitScratch. Keep the
            // tick snapshot separate so those effects cannot skip or repeat the remaining units.
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

        private static float EffectiveCost(AbilityUnit caster, AbilityCost cost)
        {
            float amount = cost.Amount;
            if (string.Equals(cost.ResourceId, AbilityResourceIds.Mana, StringComparison.OrdinalIgnoreCase))
            {
                float multiplier = caster.GetProperty(AbilityProperties.ManaCostMul, 1f);
                if (float.IsNaN(multiplier) || float.IsInfinity(multiplier))
                {
                    return float.NaN;
                }

                amount *= MathF.Max(0f, multiplier);
            }

            return amount;
        }

        private EffectContext BuildCastContext(AbilityUnit caster, AbilityBlueprint ability,
            AbilityUnit targetUnit, CastRequest request, uint seed)
        {
            EffectContext context = new EffectContext(this, caster.Id, ability.Id, new XorShiftRandom(seed));

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
                        Vector3 dir = request.Direction.sqrMagnitude > 0f
                            ? request.Direction.normalized
                            : Vector3.forward;
                        float distance = ability.Range > 0f ? ability.Range : 10f;
                        context.TargetPoint = origin + dir * distance;
                        context.HasTargetPoint = true;
                    }

                    break;
                default:
                    context.PrimaryTargets.Add(caster.Id);
                    break;
            }

            return context;
        }

        private void ResolveTargets(EffectNodeData node, EffectContext context, List<UnitId> results)
        {
            switch (node.Target)
            {
                case EffectTargetSelector.Caster:
                    results.Add(context.Caster);
                    break;

                case EffectTargetSelector.Target:
                    results.AddRange(context.PrimaryTargets);
                    break;

                case EffectTargetSelector.AreaAroundTarget:
                {
                    Vector3 center;
                    if (context.PrimaryTargets.Count > 0 &&
                        _world.TryGetPosition(context.PrimaryTargets[0], out Vector3 targetPos))
                    {
                        center = targetPos;
                    }
                    else if (context.HasTargetPoint)
                    {
                        center = context.TargetPoint;
                    }
                    else
                    {
                        return;
                    }

                    CollectArea(center, node, context, results);
                    break;
                }

                case EffectTargetSelector.AreaAroundCaster:
                {
                    if (_world.TryGetPosition(context.Caster, out Vector3 casterPos))
                    {
                        CollectArea(casterPos, node, context, results);
                    }

                    break;
                }
            }
        }

        private void CollectArea(Vector3 center, EffectNodeData node, EffectContext context, List<UnitId> results)
        {
            float radius = LeveledValueResolver.SampleByLevel(node.RadiusByLevel, context.AbilityLevel, node.Radius);
            if (radius <= 0f)
            {
                return;
            }

            _unitScratch.Clear();
            _world.QueryUnitsInRadius(center, radius, _unitScratch);

            AbilityUnit caster = GetUnit(context.Caster);

            // WHY: unlimited case preserves the historical query order and stays zero-allocation.
            if (node.MaxTargets <= 0)
            {
                for (int i = 0; i < _unitScratch.Count; i++)
                {
                    AbilityUnit unit = GetUnit(_unitScratch[i]);
                    if (unit == null || !unit.IsAlive)
                    {
                        continue;
                    }

                    if (caster != null && !MatchesTeamFilter(caster, unit, node.TeamFilter))
                    {
                        continue;
                    }

                    results.Add(unit.Id);
                }

                return;
            }

            // WHY: capped case keeps the nearest N, sorted deterministically by distance then unit id.
            _areaScratch.Clear();
            for (int i = 0; i < _unitScratch.Count; i++)
            {
                UnitId id = _unitScratch[i];
                AbilityUnit unit = GetUnit(id);
                if (unit == null || !unit.IsAlive)
                {
                    continue;
                }

                if (caster != null && !MatchesTeamFilter(caster, unit, node.TeamFilter))
                {
                    continue;
                }

                float distSq = _world.TryGetPosition(id, out Vector3 pos)
                    ? (pos - center).sqrMagnitude
                    : float.MaxValue;
                _areaScratch.Add(new AreaHit(id, distSq));
            }

            _areaScratch.Sort(AreaHitComparison);
            int keep = node.MaxTargets < _areaScratch.Count ? node.MaxTargets : _areaScratch.Count;
            for (int i = 0; i < keep; i++)
            {
                results.Add(_areaScratch[i].Unit);
            }
        }

        private static int CompareAreaHits(AreaHit a, AreaHit b)
        {
            int byDistance = a.DistanceSq.CompareTo(b.DistanceSq);
            return byDistance != 0 ? byDistance : a.Unit.Value.CompareTo(b.Unit.Value);
        }

        private void OnModifierApplied(ModifierInstance instance, bool createdNew)
        {
            // WHY: max_health_bonus / max_mana_bonus feed resource pool maxima eagerly here —
            // pools are stored state, unlike the lazily recomputed property cache.
            SyncResourceMaxBonuses(instance.Owner);
            Events.Publish(new AbilityEventArgs(AbilityEvents.ModifierApplied, instance.Owner, instance.Caster,
                instance.Stacks, instance.SourceAbilityId, instance.Blueprint.Id));
        }

        private void OnModifierRemoved(ModifierInstance instance, bool expired)
        {
            SyncResourceMaxBonuses(instance.Owner);
            Events.Publish(new AbilityEventArgs(AbilityEvents.ModifierRemoved, instance.Owner, instance.Caster,
                expired ? 1f : 0f, instance.SourceAbilityId, instance.Blueprint.Id));
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
            EffectContext context = new EffectContext(this, instance.Caster, instance.SourceAbilityId,
                new XorShiftRandom(instance.Owner.Value * 747796405u + (uint)instance.InstanceId))
            {
                AbilityLevel = instance.AbilityLevel
            };
            if (TryGetAbility(instance.SourceAbilityId, out AbilityBlueprint ability))
            {
                context.Specials = ability.Specials;
            }

            context.PrimaryTargets.Add(instance.Owner);
            ExecuteEffects(instance.Blueprint.TickEffects, context);
        }

        private void HandleModifierReactions(AbilityEventArgs args)
        {
            if (_executionDepth >= EffectContext.MaxDepth || !args.Target.IsValid)
            {
                return;
            }

            // WHY: modifier lifecycle events would recurse through their own reactions; skip them.
            if (args.EventId == AbilityEvents.ModifierApplied || args.EventId == AbilityEvents.ModifierRemoved)
            {
                return;
            }

            // WHY: reaction effects can publish new events that re-enter this handler; the per-depth
            // scratch keeps the modifier list of the outer event intact.
            List<ModifierInstance> reactionScratch = RentScratch(_reactionScratchPool, _reactionDepth);
            _reactionDepth++;
            try
            {
                Modifiers.GetModifiers(args.Target, reactionScratch);
                for (int i = 0; i < reactionScratch.Count; i++)
                {
                    ModifierInstance m = reactionScratch[i];
                    if (!m.IsActive)
                    {
                        continue;
                    }

                    List<ModifierEventReaction> reactions = m.Blueprint.EventReactions;
                    if (reactions == null)
                    {
                        continue;
                    }

                    for (int r = 0; r < reactions.Count; r++)
                    {
                        ModifierEventReaction reaction = reactions[r];
                        if (reaction == null || reaction.Effects == null || reaction.Effects.Count == 0 ||
                            !string.Equals(reaction.EventId, args.EventId, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        EffectContext context = new EffectContext(this, m.Caster, m.SourceAbilityId,
                            new XorShiftRandom(args.Target.Value * 2891336453u + (uint)m.InstanceId))
                        {
                            Depth = _executionDepth,
                            CastId = args.CastId,
                            AbilityLevel = m.AbilityLevel
                        };
                        if (TryGetAbility(m.SourceAbilityId, out AbilityBlueprint reactionAbility))
                        {
                            context.Specials = reactionAbility.Specials;
                        }

                        UnitId target = reaction.TargetEventSource && args.Source.IsValid ? args.Source : m.Owner;
                        context.PrimaryTargets.Add(target);
                        ExecuteEffects(reaction.Effects, context);
                    }
                }
            }
            finally
            {
                _reactionDepth--;
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
                _pendingProjectiles.Remove(_pendingPruneScratch[i]);
            }
        }

        private readonly struct AreaHit
        {
            public readonly UnitId Unit;
            public readonly float DistanceSq;

            public AreaHit(UnitId unit, float distanceSq)
            {
                Unit = unit;
                DistanceSq = distanceSq;
            }
        }

        private sealed class PendingProjectileCast
        {
            public readonly AbilityBlueprint Ability;
            public readonly UnitId Caster;
            public readonly uint Seed;
            public readonly int Level;

            /// <summary>Hits already reported for this cast; salts the per-hit RNG seed.</summary>
            public uint HitIndex;

            /// <summary>Domain time after which the leak guard discards the entry.</summary>
            public float ExpireAt;

            public PendingProjectileCast(AbilityBlueprint ability, UnitId caster, uint seed, int level)
            {
                Ability = ability;
                Caster = caster;
                Seed = seed;
                Level = level;
            }
        }
    }
}
