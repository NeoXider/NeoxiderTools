using System.Collections.Generic;

namespace Neo.Abilities
{
    public sealed partial class AbilitySystem
    {
        private const string TriggerEvent = "event";
        private const string TriggerEveryNth = "every Nth event";
        private const string TriggerHealthBelow = "health crossed below the threshold";
        private const string TriggerStateEnter = "state entered";
        private const string TriggerStateExit = "state exited";
        private const string TriggerApplied = "modifier applied";
        private const string TriggerRemoved = "modifier removed";
        private const string SkipCharges = "charges spent";
        private const string SkipCooldown = "internal cooldown";
        private const string SkipCount = "Nth counter";

        private readonly List<List<ReactionMatch>> _matchScratchPool =
            new List<List<ReactionMatch>>(EffectContext.MaxDepth + 1);

        /// <summary>
        ///     Publishes an animation moment of <paramref name="unit" /> (event animation_event, Tag =
        ///     <paramref name="moment" />). Presentation layers call it from animation events or timeline signals;
        ///     reactions with EventId animation_event and a matching TagFilter fire.
        /// </summary>
        public void RaiseAnimationEvent(UnitId unit, string moment, UnitId other = default, float amount = 0f,
            string abilityId = null)
        {
            if (!unit.IsValid || string.IsNullOrEmpty(moment) || !_units.ContainsKey(unit))
            {
                return;
            }

            Events.Publish(new AbilityEventArgs(AbilityEvents.AnimationEvent, unit, other, amount, abilityId,
                tag: moment));
        }

        /// <summary>
        ///     Advances the system by a host frame delta through <see cref="AbilityRules.Time" />: one step of the delta
        ///     for the default variable source, whole ticks for a <see cref="FixedStepTimeSource" />. Returns the steps run.
        /// </summary>
        public int Advance(float hostDeltaSeconds)
        {
            int steps = Rules.Time.Consume(hostDeltaSeconds, out float step);
            for (int i = 0; i < steps; i++)
            {
                Tick(step);
            }

            return steps;
        }

        /// <summary>
        ///     Runs <paramref name="ticks" /> fixed steps of <see cref="AbilityRules.Time" /> (a lockstep or server driver
        ///     advancing by tick numbers). Returns false when the time source has no fixed step.
        /// </summary>
        public bool StepTicks(int ticks)
        {
            float step = Rules.Time.StepSeconds;
            if (!(step > 0f))
            {
                return false;
            }

            for (int i = 0; i < ticks; i++)
            {
                Tick(step);
            }

            return true;
        }

        /// <summary>
        ///     True when an optional receipt (damage hooks, attack events) of <paramref name="eventId" /> on
        ///     <paramref name="unit" /> has a consumer: a reacting modifier of that unit, a handler of that event id or
        ///     the enabled trace. Global and Target observers alone do not request it (every scene unit wrapper holds a
        ///     Target subscription), but they receive it whenever it is published.
        /// </summary>
        internal bool WantsEvent(string eventId, UnitId unit)
        {
            return Modifiers.ReactsTo(unit, eventId) || Events.HasHandlers(eventId) || Trace.Enabled;
        }

        internal void TraceNote(AbilityTraceKind kind, UnitId unit, UnitId other, float amount, string id,
            string detail, string reason, int index = 0)
        {
            if (Trace.Enabled)
            {
                Trace.Record(_traceCause, kind, _time, TickCount, unit, other, id, detail, index, amount, reason);
            }
        }

        /// <summary>Called when a permanent state of a unit changes, so StateEnter / StateExit triggers see it.</summary>
        internal void OnPermanentStateChanged(UnitId unit)
        {
            EvaluateStateWatchers(unit);
        }

        long IAbilityEventInterceptor.BeginPublish(in AbilityEventArgs args)
        {
            long previous = _traceCause;
            if (Trace.Enabled)
            {
                _traceCause = Trace.Record(previous, AbilityTraceKind.Event, _time, TickCount, args.Target, args.Source,
                    args.EventId, args.ModifierId ?? args.AbilityId, (int)args.CastId, args.Amount, args.Tag);
            }

            return previous;
        }

        void IAbilityEventInterceptor.React(in AbilityEventArgs args)
        {
            HandleModifierReactions(in args);
        }

        void IAbilityEventInterceptor.EndPublish(long cookie)
        {
            _traceCause = cookie;
        }

        private void HandleModifierReactions(in AbilityEventArgs args)
        {
            if (_executionDepth >= EffectContext.MaxDepth || !args.Target.IsValid)
            {
                return;
            }

            // WHY: modifier lifecycle events would recurse through their own reactions; the Applied / Removed
            // triggers cover them instead.
            if (args.EventId == AbilityEvents.ModifierApplied || args.EventId == AbilityEvents.ModifierRemoved)
            {
                return;
            }

            if (!Modifiers.HasReactions(args.Target))
            {
                return;
            }

            // WHY: reaction effects can publish new events that re-enter this handler; the per-depth
            // scratch keeps the modifier list and the matches of the outer event intact.
            List<ModifierInstance> modifiers = RentScratch(_reactionScratchPool, _reactionDepth);
            List<ReactionMatch> matches = RentScratch(_matchScratchPool, _reactionDepth);
            _reactionDepth++;
            try
            {
                Modifiers.GetModifiers(args.Target, modifiers);
                bool isDamageHook = args.EventId == AbilityEvents.BeforeDealDamage ||
                                    args.EventId == AbilityEvents.BeforeTakeDamage;
                DamageContext damage = isDamageHook ? Damage.Active : null;
                bool isHit = damage != null && damage.HasFlag(DamageFlags.Hit);
                if (!Modifiers.HasPrioritizedReactions(args.Target))
                {
                    // WHY: without priorities the order is application order, then list order: dispatch in one pass.
                    DispatchInOrder(modifiers, in args, isDamageHook, isHit, damage);
                    return;
                }

                bool prioritized = false;
                for (int i = 0; i < modifiers.Count; i++)
                {
                    ModifierInstance m = modifiers[i];
                    List<ModifierEventReaction> reactions = m.Blueprint.EventReactions;
                    if (reactions == null)
                    {
                        continue;
                    }

                    for (int r = 0; r < reactions.Count; r++)
                    {
                        ModifierEventReaction reaction = reactions[r];
                        if (!Matches(reaction, in args) || reaction.HitsOnly && isDamageHook && !isHit)
                        {
                            continue;
                        }

                        matches.Add(new ReactionMatch(m, r, reaction.Priority));
                        prioritized |= reaction.Priority != 0;
                    }
                }

                if (matches.Count == 0)
                {
                    return;
                }

                if (prioritized)
                {
                    SortByPriority(matches);
                }

                for (int i = 0; i < matches.Count; i++)
                {
                    ReactionMatch match = matches[i];
                    ModifierInstance m = match.Instance;
                    List<ModifierEventReaction> reactions = m.Blueprint.EventReactions;
                    if (!m.IsActive || reactions == null || match.Index >= reactions.Count)
                    {
                        continue;
                    }

                    ModifierEventReaction reaction = reactions[match.Index];
                    if (reaction == null || !PassesEventTrigger(m, match.Index, reaction, in args))
                    {
                        continue;
                    }

                    FireReaction(m, match.Index, reaction, args.EventId, args.Source, args.CastId, damage,
                        TriggerReason(reaction.Trigger));
                }
            }
            finally
            {
                modifiers.Clear();
                matches.Clear();
                _reactionDepth--;
            }
        }

        private void DispatchInOrder(List<ModifierInstance> modifiers, in AbilityEventArgs args, bool isDamageHook,
            bool isHit, DamageContext damage)
        {
            for (int i = 0; i < modifiers.Count; i++)
            {
                ModifierInstance m = modifiers[i];
                List<ModifierEventReaction> reactions = m.Blueprint.EventReactions;
                if (reactions == null)
                {
                    continue;
                }

                for (int r = 0; r < reactions.Count && m.IsActive; r++)
                {
                    ModifierEventReaction reaction = reactions[r];
                    if (!Matches(reaction, in args) || reaction.HitsOnly && isDamageHook && !isHit ||
                        !PassesEventTrigger(m, r, reaction, in args))
                    {
                        continue;
                    }

                    FireReaction(m, r, reaction, args.EventId, args.Source, args.CastId, damage,
                        TriggerReason(reaction.Trigger));
                }
            }
        }

        private static bool Matches(ModifierEventReaction reaction, in AbilityEventArgs args)
        {
            if (reaction == null || !reaction.IsEventBound)
            {
                return false;
            }

            bool hasEffects = reaction.Effects != null && reaction.Effects.Count > 0;
            if (!hasEffects && reaction.MaxTriggers <= 0)
            {
                return false;
            }

            if (!AbilityIdComparer.Same(reaction.EventId, args.EventId))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(reaction.TagFilter) && !AbilityIdComparer.Same(reaction.TagFilter, args.Tag))
            {
                return false;
            }

            return string.IsNullOrEmpty(reaction.AbilityFilter) ||
                   AbilityIdComparer.Same(reaction.AbilityFilter, args.AbilityId);
        }

        private bool PassesEventTrigger(ModifierInstance m, int index, ModifierEventReaction reaction,
            in AbilityEventArgs args)
        {
            switch (reaction.Trigger)
            {
                case ModifierTrigger.EveryNth:
                {
                    ref ReactionState state = ref m.StateFor(index);
                    state.Counter++;
                    int n = reaction.EveryN < 1 ? 1 : reaction.EveryN;
                    if (state.Counter % n != 0)
                    {
                        TraceNote(AbilityTraceKind.TriggerSkipped, m.Owner, args.Source, state.Counter, m.Blueprint.Id,
                            args.EventId, SkipCount, index);
                        return false;
                    }

                    return true;
                }

                case ModifierTrigger.HealthBelow:
                {
                    if (!AbilityIdComparer.Same(args.EventId, AbilityEvents.TakeDamage))
                    {
                        return false;
                    }

                    AbilityUnit owner = GetUnit(m.Owner);
                    float max = owner != null ? owner.MaxHealth : 0f;
                    if (!(max > 0f))
                    {
                        return false;
                    }

                    float threshold = reaction.Threshold < 0f ? 0f : reaction.Threshold > 1f ? 1f : reaction.Threshold;
                    float limit = threshold * max;
                    float after = owner.Health;
                    float before = after + (args.Amount > 0f ? args.Amount : 0f);
                    return before >= limit && after < limit;
                }

                default:
                    return true;
            }
        }

        /// <summary>Applies charges and the internal cooldown, then runs the reaction's effects.</summary>
        private void FireReaction(ModifierInstance m, int index, ModifierEventReaction reaction, string eventId,
            UnitId eventSource, uint castId, DamageContext damage, string reason)
        {
            bool spent = false;
            if (reaction.MaxTriggers > 0 || reaction.Cooldown > 0f)
            {
                ref ReactionState state = ref m.StateFor(index);
                if (reaction.MaxTriggers > 0 && state.Fired >= reaction.MaxTriggers)
                {
                    TraceNote(AbilityTraceKind.TriggerSkipped, m.Owner, eventSource, state.Fired, m.Blueprint.Id,
                        eventId, SkipCharges, index);
                    return;
                }

                if (reaction.Cooldown > 0f && _time < state.ReadyAt)
                {
                    TraceNote(AbilityTraceKind.TriggerSkipped, m.Owner, eventSource, state.ReadyAt - _time,
                        m.Blueprint.Id, eventId, SkipCooldown, index);
                    return;
                }

                state.Fired++;
                if (reaction.Cooldown > 0f)
                {
                    state.ReadyAt = _time + reaction.Cooldown;
                }

                spent = reaction.MaxTriggers > 0 && state.Fired >= reaction.MaxTriggers;
            }
            else if (m.ReactionStates != null && index < m.ReactionStates.Length)
            {
                m.ReactionStates[index].Fired++;
            }

            List<EffectNodeData> effects = reaction.Effects;
            if (Trace.Enabled)
            {
                long previousCause = _traceCause;
                _traceCause = Trace.Record(previousCause, AbilityTraceKind.Reaction, _time, TickCount, m.Owner,
                    eventSource, m.Blueprint.Id, eventId, index, m.Stacks, reason);
                try
                {
                    RunReactionEffects(m, reaction, effects, eventSource, castId, damage);
                }
                finally
                {
                    _traceCause = previousCause;
                }
            }
            else
            {
                RunReactionEffects(m, reaction, effects, eventSource, castId, damage);
            }

            if (spent && reaction.RemoveWhenSpent && m.IsActive)
            {
                Modifiers.Remove(m);
            }
        }

        private void RunReactionEffects(ModifierInstance m, ModifierEventReaction reaction,
            List<EffectNodeData> effects, UnitId eventSource, uint castId, DamageContext damage)
        {
            if (effects == null || effects.Count == 0)
            {
                return;
            }

            uint seed = RollSeed(m.Owner.Value * 2891336453u + (uint)m.InstanceId, m.ReactionRollIndex++);
            EffectContext context = RentContext(m.Caster, m.SourceAbilityId, seed);
            try
            {
                context.Depth = _executionDepth;
                context.CastId = castId;
                context.AbilityLevel = m.AbilityLevel;
                context.Origin = EffectOrigin.Reaction;
                context.SourceStacks = m.Stacks;
                context.Damage = damage;
                if (TryGetAbility(m.SourceAbilityId, out AbilityBlueprint reactionAbility))
                {
                    context.Specials = reactionAbility.Specials;
                }

                UnitId target = reaction.TargetEventSource && eventSource.IsValid ? eventSource : m.Owner;
                context.PrimaryTargets.Add(target);
                ExecuteEffects(effects, context);
            }
            finally
            {
                ReturnContext(context);
            }
        }

        private void FireLifecycleTriggers(ModifierInstance instance, ModifierTrigger trigger)
        {
            List<ModifierEventReaction> reactions = instance.Blueprint.EventReactions;
            if (reactions == null || _executionDepth >= EffectContext.MaxDepth)
            {
                return;
            }

            string eventId = trigger == ModifierTrigger.Applied
                ? AbilityEvents.ModifierApplied
                : AbilityEvents.ModifierRemoved;
            string reason = trigger == ModifierTrigger.Applied ? TriggerApplied : TriggerRemoved;
            for (int r = 0; r < reactions.Count; r++)
            {
                ModifierEventReaction reaction = reactions[r];
                if (reaction == null || reaction.Trigger != trigger)
                {
                    continue;
                }

                // WHY: an Applied effect may dispel the instance itself; later Applied reactions must not run.
                if (trigger == ModifierTrigger.Applied && !instance.IsActive)
                {
                    return;
                }

                FireReaction(instance, r, reaction, eventId, instance.Caster, 0, null, reason);
            }
        }

        private void InitializeStateWatch(ModifierInstance instance)
        {
            List<ModifierEventReaction> reactions = instance.Blueprint.EventReactions;
            AbilityUnit unit = GetUnit(instance.Owner);
            if (reactions == null || unit == null)
            {
                return;
            }

            for (int r = 0; r < reactions.Count; r++)
            {
                ModifierEventReaction reaction = reactions[r];
                if (reaction != null && (reaction.Trigger == ModifierTrigger.StateEnter ||
                                         reaction.Trigger == ModifierTrigger.StateExit))
                {
                    StateValueOf(instance, r) = unit.HasState(reaction.StateId);
                }
            }
        }

        private static ref bool StateValueOf(ModifierInstance instance, int index)
        {
            return ref instance.StateFor(index).StateValue;
        }

        private void EvaluateStateWatchers(UnitId owner)
        {
            if (!Modifiers.HasStateWatchers(owner) || _executionDepth >= EffectContext.MaxDepth)
            {
                return;
            }

            AbilityUnit unit = GetUnit(owner);
            if (unit == null)
            {
                return;
            }

            List<ModifierInstance> modifiers = RentScratch(_reactionScratchPool, _reactionDepth);
            _reactionDepth++;
            try
            {
                Modifiers.GetModifiers(owner, modifiers);
                for (int i = 0; i < modifiers.Count; i++)
                {
                    ModifierInstance m = modifiers[i];
                    List<ModifierEventReaction> reactions = m.Blueprint.EventReactions;
                    if (reactions == null || !m.WatchesStates)
                    {
                        continue;
                    }

                    for (int r = 0; r < reactions.Count; r++)
                    {
                        ModifierEventReaction reaction = reactions[r];
                        if (!m.IsActive || reaction == null || string.IsNullOrEmpty(reaction.StateId) ||
                            reaction.Trigger != ModifierTrigger.StateEnter && reaction.Trigger != ModifierTrigger.StateExit)
                        {
                            continue;
                        }

                        bool now = unit.HasState(reaction.StateId);
                        ref bool previous = ref StateValueOf(m, r);
                        if (previous == now)
                        {
                            continue;
                        }

                        previous = now;
                        bool entered = now && reaction.Trigger == ModifierTrigger.StateEnter;
                        bool exited = !now && reaction.Trigger == ModifierTrigger.StateExit;
                        if (!entered && !exited)
                        {
                            continue;
                        }

                        string eventId = entered ? AbilityEvents.StateEntered : AbilityEvents.StateExited;
                        Events.Publish(new AbilityEventArgs(eventId, owner, UnitId.None, 0f, m.SourceAbilityId,
                            m.Blueprint.Id, tag: reaction.StateId));
                        if (m.IsActive)
                        {
                            FireReaction(m, r, reaction, eventId, m.Caster, 0, null,
                                entered ? TriggerStateEnter : TriggerStateExit);
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

        private static string TriggerReason(ModifierTrigger trigger)
        {
            switch (trigger)
            {
                case ModifierTrigger.EveryNth:
                    return TriggerEveryNth;
                case ModifierTrigger.HealthBelow:
                    return TriggerHealthBelow;
                default:
                    return TriggerEvent;
            }
        }

        // WHY: insertion sort is stable and allocation-free; match lists hold a handful of entries.
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
            public readonly ModifierInstance Instance;
            public readonly int Index;
            public readonly int Priority;

            public ReactionMatch(ModifierInstance instance, int index, int priority)
            {
                Instance = instance;
                Index = index;
                Priority = priority;
            }
        }
    }
}
