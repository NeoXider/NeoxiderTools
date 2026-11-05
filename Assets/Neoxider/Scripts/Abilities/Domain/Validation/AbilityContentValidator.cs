using System;
using System.Collections.Generic;
using System.Globalization;

namespace Neo.Abilities
{
    /// <summary>How serious an <see cref="AbilityContentIssue" /> is.</summary>
    public enum AbilityIssueSeverity
    {
        /// <summary>Probably unintended; the content still runs.</summary>
        Warning = 0,

        /// <summary>The node, trigger or reference cannot work as authored.</summary>
        Error = 1
    }

    /// <summary>One finding of <see cref="AbilityContentValidator" />, attached to an ability or modifier id.</summary>
    public readonly struct AbilityContentIssue
    {
        public readonly AbilityIssueSeverity Severity;
        public readonly bool IsModifier;
        public readonly string OwnerId;
        public readonly string Message;

        public AbilityContentIssue(AbilityIssueSeverity severity, bool isModifier, string ownerId, string message)
        {
            Severity = severity;
            IsModifier = isModifier;
            OwnerId = ownerId;
            Message = message;
        }

        public override string ToString()
        {
            return (Severity == AbilityIssueSeverity.Error ? "Error " : "Warning ") +
                   (IsModifier ? "modifier '" : "ability '") + OwnerId + "': " + Message;
        }
    }

    /// <summary>
    ///     Validates authored abilities and modifiers against the runtime model: unknown ops, payload / cast /
    ///     modifier references, trigger settings (EveryNth, HealthBelow, state watchers, charges, cooldowns), damage
    ///     hook ops outside hooks, zone and cone geometry, stack policies and cast_ability cycles. Pure C#; the Ability
    ///     Designer shows its findings next to its own asset checks.
    /// </summary>
    public static class AbilityContentValidator
    {
        /// <summary>Validates the given content into <paramref name="results" /> (appended). <paramref name="ops" /> may be null.</summary>
        public static void Validate(IReadOnlyList<AbilityBlueprint> abilities, IReadOnlyList<ModifierBlueprint> modifiers,
            EffectOpRegistry ops, List<AbilityContentIssue> results)
        {
            if (results == null)
            {
                return;
            }

            HashSet<string> abilityIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> modifierIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, AbilityBlueprint> abilityById =
                new Dictionary<string, AbilityBlueprint>(StringComparer.OrdinalIgnoreCase);
            if (abilities != null)
            {
                for (int i = 0; i < abilities.Count; i++)
                {
                    if (abilities[i] != null && !string.IsNullOrEmpty(abilities[i].Id) && abilityIds.Add(abilities[i].Id))
                    {
                        abilityById[abilities[i].Id] = abilities[i];
                    }
                }
            }

            if (modifiers != null)
            {
                for (int i = 0; i < modifiers.Count; i++)
                {
                    if (modifiers[i] != null && !string.IsNullOrEmpty(modifiers[i].Id))
                    {
                        modifierIds.Add(modifiers[i].Id);
                    }
                }
            }

            Context context = new Context(abilityIds, modifierIds, ops, results);
            if (abilities != null)
            {
                for (int i = 0; i < abilities.Count; i++)
                {
                    AbilityBlueprint ability = abilities[i];
                    if (ability == null)
                    {
                        continue;
                    }

                    context.Owner(false, ability.Id);
                    CheckNodes(context, "Cast", ability.CastEffects, NodeScope.Ability);
                    CheckNodes(context, "Impact", ability.ImpactEffects, NodeScope.Ability);
                    if (ability.Delivery == AbilityDeliveryType.Projectile && string.IsNullOrEmpty(ability.ProjectileArchetypeId))
                    {
                        context.Warn("Projectile delivery without a ProjectileArchetypeId: the host has nothing to spawn.");
                    }
                }

                CheckCastCycles(abilityById, results);
            }

            if (modifiers != null)
            {
                for (int i = 0; i < modifiers.Count; i++)
                {
                    ModifierBlueprint modifier = modifiers[i];
                    if (modifier == null)
                    {
                        continue;
                    }

                    context.Owner(true, modifier.Id);
                    CheckModifier(context, modifier);
                }
            }
        }

        /// <summary>Convenience overload returning a new list.</summary>
        public static List<AbilityContentIssue> Validate(IReadOnlyList<AbilityBlueprint> abilities,
            IReadOnlyList<ModifierBlueprint> modifiers, EffectOpRegistry ops = null)
        {
            List<AbilityContentIssue> results = new List<AbilityContentIssue>();
            Validate(abilities, modifiers, ops, results);
            return results;
        }

        private static void CheckModifier(Context context, ModifierBlueprint modifier)
        {
            if ((modifier.StackPolicy == ModifierStackPolicy.Stack ||
                 modifier.StackPolicy == ModifierStackPolicy.IndependentCapped) && modifier.MaxStacks < 1)
            {
                context.Error(modifier.StackPolicy + " policy needs MaxStacks >= 1 (is " + modifier.MaxStacks + ").");
            }

            if (modifier.StackPolicy == ModifierStackPolicy.Prolong && modifier.Duration <= 0f &&
                (modifier.DurationByLevel == null || modifier.DurationByLevel.Length == 0))
            {
                context.Warn("Prolong policy on a permanent modifier has nothing to extend.");
            }

            CheckNodes(context, "Tick", modifier.TickEffects, NodeScope.ModifierTick);
            List<ModifierEventReaction> reactions = modifier.EventReactions;
            if (reactions == null)
            {
                return;
            }

            for (int r = 0; r < reactions.Count; r++)
            {
                ModifierEventReaction reaction = reactions[r];
                if (reaction == null)
                {
                    continue;
                }

                string prefix = "[Reaction #" + (r + 1) + "] ";
                bool hook = AbilityIdComparer.Same(reaction.EventId, AbilityEvents.BeforeDealDamage) ||
                            AbilityIdComparer.Same(reaction.EventId, AbilityEvents.BeforeTakeDamage);
                switch (reaction.Trigger)
                {
                    case ModifierTrigger.EveryNth:
                        if (reaction.EveryN < 1)
                        {
                            context.Warn(prefix + "EveryNth with EveryN " + reaction.EveryN + " fires on every event.");
                        }

                        break;
                    case ModifierTrigger.HealthBelow:
                        if (!AbilityIdComparer.Same(reaction.EventId, AbilityEvents.TakeDamage))
                        {
                            context.Error(prefix + "HealthBelow is evaluated on take_damage; EventId is '" +
                                          reaction.EventId + "'.");
                        }

                        if (!(reaction.Threshold > 0f) || reaction.Threshold > 1f)
                        {
                            context.Error(prefix + "HealthBelow threshold " +
                                          reaction.Threshold.ToString("0.###", CultureInfo.InvariantCulture) +
                                          " must be in (0, 1].");
                        }

                        break;
                    case ModifierTrigger.StateEnter:
                    case ModifierTrigger.StateExit:
                        if (string.IsNullOrWhiteSpace(reaction.StateId))
                        {
                            context.Error(prefix + reaction.Trigger + " needs a StateId.");
                        }

                        break;
                }

                if (reaction.IsEventBound && string.IsNullOrWhiteSpace(reaction.EventId))
                {
                    context.Error(prefix + "has no event id.");
                }

                if (reaction.IsEventBound && (AbilityIdComparer.Same(reaction.EventId, AbilityEvents.ModifierApplied) ||
                                              AbilityIdComparer.Same(reaction.EventId, AbilityEvents.ModifierRemoved)))
                {
                    context.Warn(prefix + "modifier lifecycle events never reach reactions; use the Applied / " +
                                 "Removed trigger.");
                }

                if (reaction.MaxTriggers < 0)
                {
                    context.Error(prefix + "MaxTriggers is negative.");
                }

                if (reaction.RemoveWhenSpent && reaction.MaxTriggers <= 0)
                {
                    context.Warn(prefix + "RemoveWhenSpent without MaxTriggers never removes the modifier.");
                }

                if (reaction.Cooldown < 0f)
                {
                    context.Error(prefix + "Cooldown is negative.");
                }

                if (reaction.HitsOnly && !hook)
                {
                    context.Warn(prefix + "HitsOnly only filters before_deal_damage / before_take_damage.");
                }

                if (!string.IsNullOrEmpty(reaction.TagFilter) && reaction.IsEventBound &&
                    !AbilityIdComparer.Same(reaction.EventId, AbilityEvents.AnimationEvent) &&
                    !AbilityIdComparer.Same(reaction.EventId, AbilityEvents.StateEntered) &&
                    !AbilityIdComparer.Same(reaction.EventId, AbilityEvents.StateExited))
                {
                    context.Warn(prefix + "TagFilter is set but '" + reaction.EventId + "' publishes no Tag.");
                }

                bool selfDamageLoop = AbilityIdComparer.Same(reaction.EventId, AbilityEvents.TakeDamage) &&
                                      !reaction.TargetEventSource && DealsDamageToTarget(reaction.Effects);
                if (selfDamageLoop)
                {
                    context.Warn(prefix + "a take_damage reaction that damages its own owner re-triggers itself " +
                                 "(stopped by the depth cap).");
                }

                CheckNodes(context, "Reaction #" + (r + 1), reaction.Effects,
                    hook ? NodeScope.DamageHook : NodeScope.Reaction);
            }
        }

        private static bool DealsDamageToTarget(List<EffectNodeData> nodes)
        {
            if (nodes == null)
            {
                return false;
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                EffectNodeData node = nodes[i];
                if (node != null && node.Target == EffectTargetSelector.Target &&
                    (AbilityIdComparer.Same(node.OpId, AbilityEffectOps.Damage) ||
                     AbilityIdComparer.Same(node.OpId, AbilityEffectOps.Execute)))
                {
                    return true;
                }
            }

            return false;
        }

        private static void CheckNodes(Context context, string phase, List<EffectNodeData> nodes, NodeScope scope)
        {
            if (nodes == null)
            {
                return;
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                EffectNodeData node = nodes[i];
                if (node == null)
                {
                    continue;
                }

                string prefix = "[" + phase + " #" + (i + 1) + "] ";
                string op = node.OpId;
                if (string.IsNullOrWhiteSpace(op))
                {
                    context.Error(prefix + "has no op id.");
                    continue;
                }

                if (context.Ops != null && !context.Ops.TryGet(op, out _))
                {
                    context.Error(prefix + "op '" + op + "' is not registered.");
                }

                if (node.Target == EffectTargetSelector.ConeFromCaster && (!(node.Angle > 0f) || node.Angle > 360f))
                {
                    context.Error(prefix + "ConeFromCaster needs an Angle in (0, 360].");
                }

                if (node.AmountPerStack != 0f && scope == NodeScope.Ability)
                {
                    context.Warn(prefix + "AmountPerStack only scales modifier ticks and reactions.");
                }

                bool isHookOp = AbilityIdComparer.Same(op, AbilityEffectOps.ModifyDamage) ||
                                AbilityIdComparer.Same(op, AbilityEffectOps.Reflect);
                if (isHookOp && scope != NodeScope.DamageHook)
                {
                    context.Warn(prefix + op + " only works in a before_deal_damage / before_take_damage reaction.");
                }

                if (AbilityIdComparer.Same(op, AbilityEffectOps.ModifyDamage))
                {
                    if (!ModifyDamageEffectOperation.IsKnownMode(node.CustomParam))
                    {
                        context.Error(prefix + "modify_damage mode '" + node.CustomParam +
                                      "' is unknown (multiply, add, absorb, cancel).");
                    }
                    else if ((string.IsNullOrEmpty(node.CustomParam) ||
                              string.Equals(node.CustomParam, ModifyDamageEffectOperation.ModeMultiply,
                                  StringComparison.OrdinalIgnoreCase)) && node.Amount == 0f &&
                             string.IsNullOrEmpty(node.AmountKey) && (node.AmountByLevel == null ||
                                                                      node.AmountByLevel.Length == 0))
                    {
                        context.Warn(prefix + "modify_damage multiply with Amount 0 cancels the damage; use 1 for no change.");
                    }
                }

                bool needsPayload = AbilityIdComparer.Same(op, AbilityEffectOps.Projectile) ||
                                    AbilityIdComparer.Same(op, AbilityEffectOps.CastAbility);
                if (needsPayload)
                {
                    if (string.IsNullOrWhiteSpace(node.AbilityId))
                    {
                        context.Error(prefix + op + " needs an AbilityId.");
                    }
                    else if (!context.AbilityIds.Contains(node.AbilityId))
                    {
                        context.Error(prefix + "AbilityId '" + node.AbilityId + "' does not match any ability.");
                    }
                }

                if (AbilityIdComparer.Same(op, AbilityEffectOps.AreaApply) || AbilityIdComparer.Same(op, AbilityEffectOps.Zone))
                {
                    if (string.IsNullOrWhiteSpace(node.AbilityId) && string.IsNullOrWhiteSpace(node.ModifierId))
                    {
                        context.Error(prefix + op + " needs a ModifierId, an AbilityId payload or both.");
                    }

                    if (!string.IsNullOrWhiteSpace(node.AbilityId) && !context.AbilityIds.Contains(node.AbilityId))
                    {
                        context.Error(prefix + "AbilityId '" + node.AbilityId + "' does not match any ability.");
                    }
                }

                if (AbilityIdComparer.Same(op, AbilityEffectOps.Zone))
                {
                    bool positive = node.RadiusByLevel != null && node.RadiusByLevel.Length > 0
                        ? AllPositive(node.RadiusByLevel)
                        : node.Radius > 0f;
                    if (!positive)
                    {
                        context.Error(prefix + "zone needs a positive Radius.");
                    }

                    if (node.Duration < 0f || node.Interval < 0f)
                    {
                        context.Error(prefix + "zone Duration and Interval must not be negative.");
                    }

                    if (node.Duration <= 0f && node.Target != EffectTargetSelector.Caster &&
                        !string.Equals(node.CustomParam, "follow", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Warn(prefix + "a fixed zone without Duration lasts until removed by code.");
                    }
                }

                bool referencesModifier = AbilityIdComparer.Same(op, AbilityEffectOps.ApplyModifier) ||
                                          AbilityIdComparer.Same(op, AbilityEffectOps.RemoveModifier) ||
                                          AbilityIdComparer.Same(op, AbilityEffectOps.AreaApply) ||
                                          AbilityIdComparer.Same(op, AbilityEffectOps.Zone);
                if (referencesModifier && !string.IsNullOrWhiteSpace(node.ModifierId) &&
                    context.ModifierIds.Count > 0 && !context.ModifierIds.Contains(node.ModifierId))
                {
                    context.Error(prefix + "ModifierId '" + node.ModifierId + "' does not match any modifier.");
                }
            }
        }

        private static bool AllPositive(float[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (!(values[i] > 0f))
                {
                    return false;
                }
            }

            return true;
        }

        private static void CheckCastCycles(Dictionary<string, AbilityBlueprint> abilities, List<AbilityContentIssue> results)
        {
            Dictionary<string, int> state = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            List<string> path = new List<string>();
            foreach (KeyValuePair<string, AbilityBlueprint> pair in abilities)
            {
                Visit(pair.Key, abilities, state, path, results);
            }
        }

        private static void Visit(string id, Dictionary<string, AbilityBlueprint> abilities, Dictionary<string, int> state,
            List<string> path, List<AbilityContentIssue> results)
        {
            if (state.TryGetValue(id, out int mark))
            {
                if (mark == 1)
                {
                    int start = path.IndexOf(id);
                    string cycle = string.Join(" -> ", path.GetRange(start, path.Count - start)) + " -> " + id;
                    results.Add(new AbilityContentIssue(AbilityIssueSeverity.Warning, false, id,
                        "cast_ability cycle " + cycle + " (stopped by AbilitySystem.MaxCastDepth)."));
                }

                return;
            }

            if (!abilities.TryGetValue(id, out AbilityBlueprint ability))
            {
                return;
            }

            state[id] = 1;
            path.Add(id);
            VisitNodes(ability.CastEffects, abilities, state, path, results);
            VisitNodes(ability.ImpactEffects, abilities, state, path, results);
            path.RemoveAt(path.Count - 1);
            state[id] = 2;
        }

        private static void VisitNodes(List<EffectNodeData> nodes, Dictionary<string, AbilityBlueprint> abilities,
            Dictionary<string, int> state, List<string> path, List<AbilityContentIssue> results)
        {
            if (nodes == null)
            {
                return;
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                EffectNodeData node = nodes[i];
                if (node != null && !string.IsNullOrEmpty(node.AbilityId) &&
                    (AbilityIdComparer.Same(node.OpId, AbilityEffectOps.CastAbility) ||
                     AbilityIdComparer.Same(node.OpId, AbilityEffectOps.AreaApply)))
                {
                    Visit(node.AbilityId, abilities, state, path, results);
                }
            }
        }

        private enum NodeScope
        {
            Ability,
            ModifierTick,
            Reaction,
            DamageHook
        }

        private sealed class Context
        {
            public readonly HashSet<string> AbilityIds;
            public readonly HashSet<string> ModifierIds;
            public readonly EffectOpRegistry Ops;
            private readonly List<AbilityContentIssue> _results;
            private bool _isModifier;
            private string _ownerId;

            public Context(HashSet<string> abilityIds, HashSet<string> modifierIds, EffectOpRegistry ops,
                List<AbilityContentIssue> results)
            {
                AbilityIds = abilityIds;
                ModifierIds = modifierIds;
                Ops = ops;
                _results = results;
            }

            public void Owner(bool isModifier, string id)
            {
                _isModifier = isModifier;
                _ownerId = id;
            }

            public void Warn(string message)
            {
                _results.Add(new AbilityContentIssue(AbilityIssueSeverity.Warning, _isModifier, _ownerId, message));
            }

            public void Error(string message)
            {
                _results.Add(new AbilityContentIssue(AbilityIssueSeverity.Error, _isModifier, _ownerId, message));
            }
        }
    }
}
