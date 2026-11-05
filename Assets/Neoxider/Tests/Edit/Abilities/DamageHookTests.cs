using System.Collections.Generic;
using Neo.Abilities;
using NUnit.Framework;

namespace Neo.Editor.Tests.Abilities
{
    /// <summary>
    ///     Contracts of the damage pre-hooks: before_deal_damage on the attacker, then before_take_damage on the victim,
    ///     with a mutable context (multiplier, flat add, absorb, reflect, cancel), deterministic ordering, the HitsOnly
    ///     filter and the nesting caps.
    /// </summary>
    public sealed class DamageHookTests
    {
        private AbilitySystem _system;
        private AbilityUnit _attacker;
        private AbilityUnit _victim;

        [SetUp]
        public void SetUp()
        {
            _system = new AbilitySystem();
            _attacker = AbilityTestSupport.CreateUnit(_system, team: 1, health: 1000f, name: "Attacker");
            _victim = AbilityTestSupport.CreateUnit(_system, team: 2, health: 1000f, name: "Victim");
        }

        [Test]
        public void Hooks_RunAttackerThenVictim_HandlersBeforeReactions()
        {
            List<string> order = new List<string>();
            _system.RegisterModifier(AbilityTestSupport.Modifier("deal_marker"));
            _system.RegisterModifier(AbilityTestSupport.Modifier("take_marker"));
            Apply(_attacker, Reaction(AbilityEvents.BeforeDealDamage, ApplyTo("deal_marker")));
            Apply(_victim, Reaction(AbilityEvents.BeforeTakeDamage, ApplyTo("take_marker")));
            _system.Events.Subscribe(AbilityEvents.BeforeDealDamage, args => order.Add("deal:" + args.Target.Value));
            _system.Events.Subscribe(AbilityEvents.BeforeTakeDamage, args => order.Add("take:" + args.Target.Value));
            _system.Events.Subscribe(AbilityEvents.ModifierApplied, args => order.Add("applied:" + args.ModifierId));

            DamageService.ApplyDamage(_system, _attacker.Id, _victim.Id, 10f, AbilityDamageTypes.Pure);

            Assert.That(order, Is.EqualTo(new[]
            {
                "deal:" + _attacker.Id.Value, "applied:deal_marker", "take:" + _victim.Id.Value, "applied:take_marker"
            }));
        }

        [Test]
        public void Multiplier_ScalesTheRawAmount()
        {
            Apply(_attacker, Reaction(AbilityEvents.BeforeDealDamage, Modify("multiply", 2f)));

            DamageResult result = DamageService.ApplyDamage(_system, _attacker.Id, _victim.Id, 10f,
                AbilityDamageTypes.Pure);

            Assert.That(result.HealthDamage, Is.EqualTo(20f).Within(0.001f));
            Assert.That(_victim.Health, Is.EqualTo(980f).Within(0.001f));
        }

        [Test]
        public void FlatAdd_IsMitigatedLikeTheRawAmount()
        {
            _victim.SetBaseProperty(AbilityProperties.Armor, 10f);
            Apply(_victim, Reaction(AbilityEvents.BeforeTakeDamage, Modify("add", 10f)));

            DamageResult result = DamageService.ApplyDamage(_system, _attacker.Id, _victim.Id, 10f,
                AbilityDamageTypes.Physical);

            float reduction = 0.6f / 1.6f;
            Assert.That(result.HealthDamage, Is.EqualTo(20f * (1f - reduction)).Within(0.001f));
        }

        [Test]
        public void Absorb_BlocksAfterMitigation_AndCountsAsAbsorbed()
        {
            Apply(_victim, Reaction(AbilityEvents.BeforeTakeDamage, Modify("absorb", 7f)));
            List<AbilityEventArgs> taken = Record(AbilityEvents.TakeDamage);

            DamageResult result = DamageService.ApplyDamage(_system, _attacker.Id, _victim.Id, 10f,
                AbilityDamageTypes.Pure);

            Assert.That(result.Absorbed, Is.EqualTo(7f).Within(0.001f));
            Assert.That(result.HealthDamage, Is.EqualTo(3f).Within(0.001f));
            Assert.That(taken.Count, Is.EqualTo(1));
            Assert.That(taken[0].Amount, Is.EqualTo(3f).Within(0.001f));
        }

        [Test]
        public void Cancel_DealsNothing_AndPublishesNoDamageReceipts()
        {
            Apply(_victim, Reaction(AbilityEvents.BeforeTakeDamage, Modify("cancel", 0f)));
            List<AbilityEventArgs> taken = Record(AbilityEvents.TakeDamage);
            List<AbilityEventArgs> dealt = Record(AbilityEvents.DealDamage);

            DamageResult result = DamageService.ApplyDamage(_system, _attacker.Id, _victim.Id, 50f,
                AbilityDamageTypes.Pure);

            Assert.That(result.Cancelled, Is.True);
            Assert.That(result.Negated, Is.True);
            Assert.That(_victim.Health, Is.EqualTo(1000f));
            Assert.That(taken, Is.Empty);
            Assert.That(dealt, Is.Empty);
        }

        [Test]
        public void CodeHandler_AdjustsActiveDamage_OnlyWhileTheHookRuns()
        {
            DamageContext seenInTake = null;
            _system.Events.Subscribe(AbilityEvents.BeforeTakeDamage, args => _system.ActiveDamage.Multiplier *= 0.5f);
            _system.Events.Subscribe(AbilityEvents.TakeDamage, args => seenInTake = _system.ActiveDamage);

            DamageResult result = DamageService.ApplyDamage(_system, _attacker.Id, _victim.Id, 40f,
                AbilityDamageTypes.Pure);

            Assert.That(result.HealthDamage, Is.EqualTo(20f).Within(0.001f));
            Assert.That(seenInTake, Is.Null, "after the before hooks the context is not adjustable");
            Assert.That(_system.ActiveDamage, Is.Null);
        }

        [Test]
        public void ModifyDamage_OutsideABeforeHook_DoesNothing()
        {
            Apply(_victim, Reaction(AbilityEvents.TakeDamage, Modify("multiply", 0f)));

            DamageService.ApplyDamage(_system, _attacker.Id, _victim.Id, 10f, AbilityDamageTypes.Pure);
            DamageResult second = DamageService.ApplyDamage(_system, _attacker.Id, _victim.Id, 10f,
                AbilityDamageTypes.Pure);

            Assert.That(second.HealthDamage, Is.EqualTo(10f).Within(0.001f));
        }

        [Test]
        public void Reflect_SendsAShareBack_AndReflectedDamageIsNeverReflectedAgain()
        {
            ModifierBlueprint thorns = AbilityTemplates.Thorns(fraction: 0.3f).Modifiers[0];
            thorns.EventReactions[0].HitsOnly = false;
            _system.RegisterModifier(thorns);
            _system.ApplyModifier(thorns.Id, _victim.Id, _victim.Id);
            _system.ApplyModifier(thorns.Id, _attacker.Id, _attacker.Id);
            List<AbilityEventArgs> reflected = Record(AbilityEvents.DamageReflected);

            DamageResult result = DamageService.ApplyDamage(_system, _attacker.Id, _victim.Id, 100f,
                AbilityDamageTypes.Pure);

            Assert.That(result.Reflected, Is.EqualTo(30f).Within(0.001f));
            Assert.That(_victim.Health, Is.EqualTo(900f).Within(0.001f));
            Assert.That(_attacker.Health, Is.EqualTo(970f).Within(0.001f));
            Assert.That(reflected.Count, Is.EqualTo(1), "the reflected 30 is not reflected back again");
            Assert.That(reflected[0].Target, Is.EqualTo(_victim.Id));
        }

        [Test]
        public void Priority_HigherReactionRunsFirst_TiesKeepApplicationOrder()
        {
            List<string> applied = new List<string>();
            _system.Events.Subscribe(AbilityEvents.ModifierApplied, args =>
            {
                if (args.Target == _attacker.Id)
                {
                    applied.Add(args.ModifierId);
                }
            });
            foreach (string id in new[] { "first", "second", "third" })
            {
                _system.RegisterModifier(AbilityTestSupport.Modifier(id));
            }

            ModifierEventReaction low = Reaction(AbilityEvents.BeforeTakeDamage, ApplyTo("first"));
            low.TargetEventSource = true;
            ModifierEventReaction tie = Reaction(AbilityEvents.BeforeTakeDamage, ApplyTo("second"));
            tie.TargetEventSource = true;
            ModifierEventReaction high = Reaction(AbilityEvents.BeforeTakeDamage, ApplyTo("third"));
            high.TargetEventSource = true;
            high.Priority = 10;
            Apply(_victim, low);
            Apply(_victim, tie);
            Apply(_victim, high);

            DamageService.ApplyDamage(_system, _attacker.Id, _victim.Id, 1f, AbilityDamageTypes.Pure);

            Assert.That(applied, Is.EqualTo(new[] { "third", "first", "second" }));
        }

        [Test]
        public void HitsOnly_IgnoresDamageOverTime_ButEnhancesCastHits()
        {
            AbilityTemplateBundle enhancer = AbilityTemplates.AttackEnhancer("power", 2f, 2, 30f);
            enhancer.RegisterInto(_system);
            _system.ApplyModifier("power", _attacker.Id, _attacker.Id);
            ModifierBlueprint burn = AbilityTestSupport.Modifier("burn", 10f);
            burn.TickInterval = 1f;
            burn.TickEffects.Add(AbilityTestSupport.DamageNode(5f));
            _system.RegisterModifier(burn);
            _system.ApplyModifier("burn", _attacker.Id, _victim.Id);
            AbilityBlueprint strike = new AbilityBlueprint { Id = "strike", Targeting = TargetingMode.Unit };
            strike.ImpactEffects.Add(AbilityTestSupport.DamageNode(10f));
            _system.RegisterAbility(strike);
            _system.GrantAbility(_attacker.Id, "strike");

            _system.Tick(1f);
            Assert.That(_victim.Health, Is.EqualTo(995f).Within(0.001f), "the tick is not a hit and is not doubled");
            Assert.That(_system.Modifiers.TryGetActive(_attacker.Id, "power", out ModifierInstance power), Is.True);
            Assert.That(power.GetRemainingCharges(0), Is.EqualTo(2));

            _system.Cast(CastRequest.AtUnit(_attacker.Id, "strike", _victim.Id));
            Assert.That(_victim.Health, Is.EqualTo(975f).Within(0.001f), "the cast hit is doubled");
            Assert.That(power.GetRemainingCharges(0), Is.EqualTo(1));
        }

        [Test]
        public void DamageInHooks_IsCappedByTheNestingLimits()
        {
            ModifierEventReaction retaliate = Reaction(AbilityEvents.BeforeTakeDamage,
                AbilityTestSupport.DamageNode(1f));
            retaliate.TargetEventSource = true;
            Apply(_victim, retaliate);
            ModifierEventReaction retaliateBack = Reaction(AbilityEvents.BeforeTakeDamage,
                AbilityTestSupport.DamageNode(1f));
            retaliateBack.TargetEventSource = true;
            Apply(_attacker, retaliateBack);

            DamageService.ApplyDamage(_system, _attacker.Id, _victim.Id, 1f, AbilityDamageTypes.Pure);

            Assert.That(_system.Damage.Depth, Is.Zero);
            Assert.That(_victim.Health, Is.GreaterThan(990f), "ping-pong damage stops at the depth cap");
            Assert.That(_attacker.Health, Is.GreaterThan(990f));
        }

        [Test]
        public void HooksSeeTheDamageTypeAndMayConvertIt()
        {
            _victim.SetPermanentState(AbilityStates.MagicImmune, true);
            _system.Events.Subscribe(AbilityEvents.BeforeDealDamage,
                args => _system.ActiveDamage.DamageType = AbilityDamageTypes.Magical);

            DamageResult result = DamageService.ApplyDamage(_system, _attacker.Id, _victim.Id, 10f,
                AbilityDamageTypes.Pure);

            Assert.That(result.Negated, Is.True, "a hook converting into an immune type is honoured");
            Assert.That(_victim.Health, Is.EqualTo(1000f));
        }

        private void Apply(AbilityUnit owner, ModifierEventReaction reaction)
        {
            ModifierBlueprint blueprint = AbilityTestSupport.Modifier("hook_" + owner.Id.Value + "_" +
                                                                      _system.Modifiers.TrackedOwnerCount + "_" +
                                                                      reaction.GetHashCode());
            blueprint.StackPolicy = ModifierStackPolicy.Independent;
            blueprint.EventReactions.Add(reaction);
            _system.Modifiers.Apply(blueprint, owner.Id, owner.Id);
        }

        private static ModifierEventReaction Reaction(string eventId, EffectNodeData effect)
        {
            ModifierEventReaction reaction = new ModifierEventReaction { EventId = eventId };
            reaction.Effects.Add(effect);
            return reaction;
        }

        private static EffectNodeData Modify(string mode, float amount)
        {
            return new EffectNodeData { OpId = AbilityEffectOps.ModifyDamage, CustomParam = mode, Amount = amount };
        }

        private static EffectNodeData ApplyTo(string modifierId)
        {
            return new EffectNodeData
            {
                OpId = AbilityEffectOps.ApplyModifier,
                Target = EffectTargetSelector.Target,
                ModifierId = modifierId
            };
        }

        private List<AbilityEventArgs> Record(string eventId)
        {
            List<AbilityEventArgs> events = new List<AbilityEventArgs>();
            _system.Events.Subscribe(eventId, events.Add);
            return events;
        }
    }
}
