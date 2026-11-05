using System.Collections.Generic;

namespace Neo.Abilities.Fixed
{
    public sealed partial class FixedAbilitySystem
    {
        private readonly List<List<FixedModifierInstance>> _reactionScratchPool =
            new List<List<FixedModifierInstance>>(FixedEffectContext.MaxDepth + 1);

        private readonly List<List<ReactionMatch>> _matchScratchPool =
            new List<List<ReactionMatch>>(FixedEffectContext.MaxDepth + 1);

        private int _reactionDepth;

        /// <summary>Publishes animation_event (Tag = moment) on a unit; reactions filter it with TagFilter.</summary>
        public void RaiseAnimationEvent(int unit, string moment, int other = 0, int amountRaw = 0, string abilityId = null)
        {
            if (unit == 0 || string.IsNullOrEmpty(moment) || !_units.ContainsKey(unit))
            {
                return;
            }

            Events.Publish(new FixedEventArgs(FixedEvents.AnimationEvent, unit, other, amountRaw, abilityId, tag: moment));
        }

        /// <summary>True when an optional receipt (hooks, attack events) has a listener on this unit.</summary>
        internal bool WantsEvent(string eventId, int unit)
        {
            return Modifiers.ReactsTo(unit, eventId) || Events.HasHandlers(eventId);
        }

        internal void OnPermanentStateChanged(int unit)
        {
            EvaluateStateWatchers(unit);
        }

        void IFixedEventInterceptor.React(in FixedEventArgs args)
        {
            HandleModifierReactions(in args);
        }

        private void HandleModifierReactions(in FixedEventArgs args)
        {
            if (_executionDepth >= FixedEffectContext.MaxDepth || args.Target == 0 ||
                args.EventId == FixedEvents.ModifierApplied ||
                args.EventId == FixedEvents.ModifierRemoved || !Modifiers.HasReactions(args.Target))
            {
                return;
            }

            List<FixedModifierInstance> modifiers = RentScratch(_reactionScratchPool, _reactionDepth);
            List<ReactionMatch> matches = RentScratch(_matchScratchPool, _reactionDepth);
            _reactionDepth++;
            try
            {
                Modifiers.GetModifiers(args.Target, modifiers);
                bool isDamageHook = args.EventId == FixedEvents.BeforeDealDamage ||
                                    args.EventId == FixedEvents.BeforeTakeDamage;
                FixedDamageContext damage = isDamageHook ? Damage.Active : null;
                bool isHit = damage != null && damage.HasFlag(FixedDamageFlags.Hit);
                if (!Modifiers.HasPrioritizedReactions(args.Target))
                {
                    DispatchInOrder(modifiers, in args, isDamageHook, isHit, damage);
                    return;
                }

                for (int i = 0; i < modifiers.Count; i++)
                {
                    FixedModifierInstance m = modifiers[i];
                    List<FixedModifierReaction> reactions = m.Blueprint.EventReactions;
                    if (reactions == null)
                    {
                        continue;
                    }

                    for (int r = 0; r < reactions.Count; r++)
                    {
                        FixedModifierReaction reaction = reactions[r];
                        if (Matches(reaction, in args) && !(reaction.HitsOnly && isDamageHook && !isHit))
                        {
                            matches.Add(new ReactionMatch(m, r, reaction.Priority));
                        }
                    }
                }

                SortByPriority(matches);
                for (int i = 0; i < matches.Count; i++)
                {
                    ReactionMatch match = matches[i];
                    FixedModifierInstance m = match.Instance;
                    List<FixedModifierReaction> reactions = m.Blueprint.EventReactions;
                    if (!m.IsActive || reactions == null || match.Index >= reactions.Count)
                    {
                        continue;
                    }

                    FixedModifierReaction reaction = reactions[match.Index];
                    if (reaction != null && PassesEventTrigger(m, match.Index, reaction, in args))
                    {
                        FireReaction(m, match.Index, reaction, args.Source, args.CastId, damage);
                    }
                }
            }
            finally
            {
                modifiers.Clear();
                matches.Clear();
                _reactionDepth--;
            }
        }

        private void DispatchInOrder(List<FixedModifierInstance> modifiers, in FixedEventArgs args, bool isDamageHook,
            bool isHit, FixedDamageContext damage)
        {
            for (int i = 0; i < modifiers.Count; i++)
            {
                FixedModifierInstance m = modifiers[i];
                List<FixedModifierReaction> reactions = m.Blueprint.EventReactions;
                if (reactions == null)
                {
                    continue;
                }

                for (int r = 0; r < reactions.Count && m.IsActive; r++)
                {
                    FixedModifierReaction reaction = reactions[r];
                    if (!Matches(reaction, in args) || reaction.HitsOnly && isDamageHook && !isHit ||
                        !PassesEventTrigger(m, r, reaction, in args))
                    {
                        continue;
                    }

                    FireReaction(m, r, reaction, args.Source, args.CastId, damage);
                }
            }
        }

        private static bool Matches(FixedModifierReaction reaction, in FixedEventArgs args)
        {
            if (reaction == null || !reaction.IsEventBound)
            {
                return false;
            }

            bool hasEffects = reaction.Effects != null && reaction.Effects.Count > 0;
            if (!hasEffects && reaction.MaxTriggers <= 0 || !FixedIdComparer.Same(reaction.EventId, args.EventId))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(reaction.TagFilter) && !FixedIdComparer.Same(reaction.TagFilter, args.Tag))
            {
                return false;
            }

            return string.IsNullOrEmpty(reaction.AbilityFilter) || FixedIdComparer.Same(reaction.AbilityFilter, args.AbilityId);
        }

        private bool PassesEventTrigger(FixedModifierInstance m, int index, FixedModifierReaction reaction,
            in FixedEventArgs args)
        {
            switch (reaction.Trigger)
            {
                case FixedModifierTrigger.EveryNth:
                {
                    ref FixedReactionState state = ref m.StateFor(index);
                    state.Counter++;
                    int n = reaction.EveryN < 1 ? 1 : reaction.EveryN;
                    return state.Counter % n == 0;
                }
                case FixedModifierTrigger.HealthBelow:
                {
                    if (!FixedIdComparer.Same(args.EventId, FixedEvents.TakeDamage))
                    {
                        return false;
                    }

                    FixedUnit owner = GetUnit(m.Owner);
                    int max = owner != null ? owner.MaxHealthRaw : 0;
                    if (max <= 0)
                    {
                        return false;
                    }

                    // WHY: compared as exact ratios (health × one against threshold × max) so no rounding moves the edge.
                    long one = Rules.Format.One;
                    long limit = (long)FixedMath.Clamp(reaction.ThresholdRaw, 0, Rules.Format.One) * max;
                    long after = owner.HealthRaw;
                    long before = after + (args.AmountRaw > 0 ? args.AmountRaw : 0);
                    return before * one >= limit && after * one < limit;
                }
                default:
                    return true;
            }
        }

        private void FireReaction(FixedModifierInstance m, int index, FixedModifierReaction reaction, int eventSource,
            uint castId, FixedDamageContext damage)
        {
            bool spent = false;
            if (reaction.MaxTriggers > 0 || reaction.CooldownTicks > 0)
            {
                ref FixedReactionState state = ref m.StateFor(index);
                if (reaction.MaxTriggers > 0 && state.Fired >= reaction.MaxTriggers ||
                    reaction.CooldownTicks > 0 && TickCount < state.ReadyAtTick)
                {
                    return;
                }

                state.Fired++;
                if (reaction.CooldownTicks > 0)
                {
                    state.ReadyAtTick = TickCount + reaction.CooldownTicks;
                }

                spent = reaction.MaxTriggers > 0 && state.Fired >= reaction.MaxTriggers;
            }
            else if (m.ReactionStates != null && index < m.ReactionStates.Length)
            {
                m.ReactionStates[index].Fired++;
            }

            List<FixedEffectNode> effects = reaction.Effects;
            if (effects != null && effects.Count > 0)
            {
                uint seed = FixedSeeds.Roll(unchecked((uint)m.Owner * 2891336453u + (uint)m.InstanceId),
                    m.ReactionRollIndex++);
                FixedEffectContext context = RentContext(m.Caster, m.SourceAbilityId, seed);
                try
                {
                    context.Depth = _executionDepth;
                    context.CastId = castId;
                    context.AbilityLevel = m.AbilityLevel;
                    context.Origin = FixedEffectOrigin.Reaction;
                    context.SourceStacks = m.Stacks;
                    context.Damage = damage;
                    if (TryGetAbility(m.SourceAbilityId, out FixedAbilityBlueprint ability))
                    {
                        context.Specials = ability.Specials;
                    }

                    context.PrimaryTargets.Add(reaction.TargetEventSource && eventSource != 0 ? eventSource : m.Owner);
                    ExecuteEffects(effects, context);
                }
                finally
                {
                    ReturnContext(context);
                }
            }

            if (spent && reaction.RemoveWhenSpent && m.IsActive)
            {
                Modifiers.Remove(m);
            }
        }

        private void FireLifecycleTriggers(FixedModifierInstance instance, FixedModifierTrigger trigger)
        {
            List<FixedModifierReaction> reactions = instance.Blueprint.EventReactions;
            if (reactions == null || _executionDepth >= FixedEffectContext.MaxDepth)
            {
                return;
            }

            for (int r = 0; r < reactions.Count; r++)
            {
                FixedModifierReaction reaction = reactions[r];
                if (reaction == null || reaction.Trigger != trigger)
                {
                    continue;
                }

                if (trigger == FixedModifierTrigger.Applied && !instance.IsActive)
                {
                    return;
                }

                FireReaction(instance, r, reaction, instance.Caster, 0u, null);
            }
        }

        private void InitializeStateWatch(FixedModifierInstance instance)
        {
            List<FixedModifierReaction> reactions = instance.Blueprint.EventReactions;
            FixedUnit unit = GetUnit(instance.Owner);
            if (reactions == null || unit == null)
            {
                return;
            }

            for (int r = 0; r < reactions.Count; r++)
            {
                FixedModifierReaction reaction = reactions[r];
                if (reaction != null && (reaction.Trigger == FixedModifierTrigger.StateEnter ||
                                         reaction.Trigger == FixedModifierTrigger.StateExit))
                {
                    instance.StateFor(r).StateValue = unit.HasState(reaction.StateId);
                }
            }
        }

        private void EvaluateStateWatchers(int owner)
        {
            if (!Modifiers.HasStateWatchers(owner) || _executionDepth >= FixedEffectContext.MaxDepth)
            {
                return;
            }

            FixedUnit unit = GetUnit(owner);
            if (unit == null)
            {
                return;
            }

            List<FixedModifierInstance> modifiers = RentScratch(_reactionScratchPool, _reactionDepth);
            _reactionDepth++;
            try
            {
                Modifiers.GetModifiers(owner, modifiers);
                for (int i = 0; i < modifiers.Count; i++)
                {
                    FixedModifierInstance m = modifiers[i];
                    List<FixedModifierReaction> reactions = m.Blueprint.EventReactions;
                    if (reactions == null || !m.WatchesStates)
                    {
                        continue;
                    }

                    for (int r = 0; r < reactions.Count; r++)
                    {
                        FixedModifierReaction reaction = reactions[r];
                        if (!m.IsActive || reaction == null || string.IsNullOrEmpty(reaction.StateId) ||
                            reaction.Trigger != FixedModifierTrigger.StateEnter &&
                            reaction.Trigger != FixedModifierTrigger.StateExit)
                        {
                            continue;
                        }

                        bool now = unit.HasState(reaction.StateId);
                        ref FixedReactionState state = ref m.StateFor(r);
                        if (state.StateValue == now)
                        {
                            continue;
                        }

                        state.StateValue = now;
                        bool entered = now && reaction.Trigger == FixedModifierTrigger.StateEnter;
                        bool exited = !now && reaction.Trigger == FixedModifierTrigger.StateExit;
                        if (!entered && !exited)
                        {
                            continue;
                        }

                        Events.Publish(new FixedEventArgs(entered ? FixedEvents.StateEntered : FixedEvents.StateExited, owner,
                            0, 0, m.SourceAbilityId, m.Blueprint.Id, tag: reaction.StateId));
                        if (m.IsActive)
                        {
                            FireReaction(m, r, reaction, m.Caster, 0u, null);
                        }
                    }
                }
            }
            finally
            {
                modifiers.Clear();
                _reactionDepth--;
            }
        }

        // WHY: stable insertion sort (higher priority first, ties keep application then list order); no allocation.
        private static void SortByPriority(List<ReactionMatch> matches)
        {
            for (int i = 1; i < matches.Count; i++)
            {
                ReactionMatch current = matches[i];
                int j = i - 1;
                while (j >= 0 && matches[j].Priority < current.Priority)
                {
                    matches[j + 1] = matches[j];
                    j--;
                }

                matches[j + 1] = current;
            }
        }

        private readonly struct ReactionMatch
        {
            public readonly FixedModifierInstance Instance;
            public readonly int Index;
            public readonly int Priority;

            public ReactionMatch(FixedModifierInstance instance, int index, int priority)
            {
                Instance = instance;
                Index = index;
                Priority = priority;
            }
        }
    }
}
