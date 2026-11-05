using System.Collections.Generic;
using Neo.Abilities;
using NUnit.Framework;

namespace Neo.Editor.Tests.Abilities
{
    /// <summary>
    ///     Contracts of the reaction triggers: attack_landed (direct hits only) next to deal_damage, the edge-triggered
    ///     health threshold, every Nth event, state enter / exit, animation moments with a tag filter, Applied /
    ///     Removed, charges with removal when spent, the internal cooldown and the ability filter.
    /// </summary>
    public sealed class ModifierTriggerTests
    {
        private AbilitySystem _system;
        private AbilityUnit _hero;
        private AbilityUnit _enemy;

        [SetUp]
        public void SetUp()
        {
            _system = new AbilitySystem();
            _hero = AbilityTestSupport.CreateUnit(_system, team: 1, health: 1000f, mana: 1000f, name: "Hero");
            _enemy = AbilityTestSupport.CreateUnit(_system, team: 2, health: 1000f, mana: 1000f, name: "Enemy");
            AbilityBlueprint strike = new AbilityBlueprint { Id = "strike", Targeting = TargetingMode.Unit };
            strike.ImpactEffects.Add(AbilityTestSupport.DamageNode(10f));
            _system.RegisterAbility(strike);
            _system.GrantAbility(_hero.Id, "strike");
        }

        [Test]
        public void AttackLanded_FiresForCastHits_NotForTicks_WhileDealDamageFiresForBoth()
        {
            List<AbilityEventArgs> landed = Record(AbilityEvents.AttackLanded);
            List<AbilityEventArgs> received = Record(AbilityEvents.AttackReceived);
            List<AbilityEventArgs> dealt = Record(AbilityEvents.DealDamage);
            ModifierBlueprint burn = AbilityTestSupport.Modifier("burn", 5f);
            burn.TickInterval = 1f;
            burn.TickEffects.Add(AbilityTestSupport.DamageNode(1f));
            _system.RegisterModifier(burn);
            _system.ApplyModifier("burn", _hero.Id, _enemy.Id);

            _system.Cast(CastRequest.AtUnit(_hero.Id, "strike", _enemy.Id));
            _system.Tick(1f);

            Assert.That(dealt.Count, Is.EqualTo(2), "deal_damage fires for the hit and the tick");
            Assert.That(landed.Count, Is.EqualTo(1), "attack_landed fires for the hit only");
            Assert.That(landed[0].Target, Is.EqualTo(_hero.Id));
            Assert.That(landed[0].Source, Is.EqualTo(_enemy.Id));
            Assert.That(landed[0].AbilityId, Is.EqualTo("strike"));
            Assert.That(received.Count, Is.EqualTo(1));
            Assert.That(received[0].Target, Is.EqualTo(_enemy.Id));
        }

        [Test]
        public void AttackLanded_CountsAFullyAbsorbedHit_ButNotAnEvadedOne()
        {
            List<AbilityEventArgs> landed = Record(AbilityEvents.AttackLanded);
            ModifierBlueprint shield = AbilityTestSupport.Modifier("shield")
                .WithProperty(AbilityProperties.ShieldHp, PropertyOp.Add, 100f);
            _system.Modifiers.Apply(shield, _enemy.Id, _enemy.Id);
            AbilityBlueprint zap = new AbilityBlueprint { Id = "zap", Targeting = TargetingMode.Unit };
            zap.ImpactEffects.Add(AbilityTestSupport.DamageNode(10f, AbilityDamageTypes.Magical));
            _system.RegisterAbility(zap);
            _system.GrantAbility(_hero.Id, "zap");

            _system.Cast(CastRequest.AtUnit(_hero.Id, "zap", _enemy.Id));
            Assert.That(_enemy.Health, Is.EqualTo(1000f), "pure damage would bypass the shield; magical is absorbed");
            Assert.That(landed.Count, Is.EqualTo(1), "a shielded hit still lands");

            _system.Modifiers.RemoveById(_enemy.Id, "shield");
            _enemy.SetBaseProperty(AbilityProperties.EvasionChance, 1f);
            AbilityBlueprint slash = new AbilityBlueprint { Id = "slash", Targeting = TargetingMode.Unit };
            slash.ImpactEffects.Add(AbilityTestSupport.DamageNode(10f, AbilityDamageTypes.Physical));
            _system.RegisterAbility(slash);
            _system.GrantAbility(_hero.Id, "slash");
            _system.Cast(CastRequest.AtUnit(_hero.Id, "slash", _enemy.Id));

            Assert.That(landed.Count, Is.EqualTo(1), "an evaded hit does not land");
        }

        [Test]
        public void HealthBelow_FiresOnceOnTheCrossing_AndAgainAfterHealingAboveTheThreshold()
        {
            ModifierEventReaction reaction = new ModifierEventReaction
            {
                EventId = AbilityEvents.TakeDamage,
                Trigger = ModifierTrigger.HealthBelow,
                Threshold = 0.5f
            };
            reaction.Effects.Add(Mana(1f, EffectTargetSelector.Target));
            Carry(_enemy, reaction);
            Drain(_enemy);

            Hit(_enemy, 400f);
            Assert.That(ManaOf(_enemy), Is.Zero, "60% is above the threshold");
            Hit(_enemy, 200f);
            Assert.That(ManaOf(_enemy), Is.EqualTo(1f), "60% -> 40% crosses 50%");
            Hit(_enemy, 100f);
            Assert.That(ManaOf(_enemy), Is.EqualTo(1f), "already below: no new edge");
            _enemy.Resources.Increase(AbilityResourceIds.Health, 400f);
            Hit(_enemy, 300f);
            Assert.That(ManaOf(_enemy), Is.EqualTo(2f), "healed to 70%, then 70% -> 40% crosses again");
        }

        [Test]
        public void EveryNth_FiresOnEveryThirdAttack_WithACounterPerInstance()
        {
            ModifierEventReaction reaction = new ModifierEventReaction
            {
                EventId = AbilityEvents.AttackLanded,
                Trigger = ModifierTrigger.EveryNth,
                EveryN = 3
            };
            reaction.Effects.Add(Mana(1f, EffectTargetSelector.Caster));
            ModifierInstance instance = Carry(_hero, reaction);
            Drain(_hero);

            for (int i = 0; i < 7; i++)
            {
                _system.Cast(CastRequest.AtUnit(_hero.Id, "strike", _enemy.Id));
            }

            Assert.That(ManaOf(_hero), Is.EqualTo(2f), "attacks 3 and 6 fire");
            Assert.That(instance.GetReactionCounter(0), Is.EqualTo(7));
        }

        [Test]
        public void StateEnterAndExit_FireOnTransitions_NotForTheStateAlreadyPresent()
        {
            ModifierEventReaction enter = new ModifierEventReaction
            {
                Trigger = ModifierTrigger.StateEnter,
                StateId = AbilityStates.Stunned
            };
            enter.Effects.Add(Mana(1f, EffectTargetSelector.Target));
            ModifierEventReaction exit = new ModifierEventReaction
            {
                Trigger = ModifierTrigger.StateExit,
                StateId = AbilityStates.Stunned
            };
            exit.Effects.Add(Mana(10f, EffectTargetSelector.Target));
            Carry(_enemy, enter, exit);
            List<AbilityEventArgs> entered = Record(AbilityEvents.StateEntered);
            List<AbilityEventArgs> exited = Record(AbilityEvents.StateExited);
            Drain(_enemy);
            ModifierBlueprint stun = AbilityTestSupport.Modifier("stun", 1f).WithState(AbilityStates.Stunned);

            _system.Modifiers.Apply(stun, _hero.Id, _enemy.Id);
            Assert.That(ManaOf(_enemy), Is.EqualTo(1f));
            _system.Modifiers.Apply(stun, _hero.Id, _enemy.Id);
            Assert.That(ManaOf(_enemy), Is.EqualTo(1f), "a refresh is no transition");
            _system.Tick(1.5f);
            Assert.That(ManaOf(_enemy), Is.EqualTo(11f));
            _enemy.SetPermanentState(AbilityStates.Stunned, true);
            Assert.That(ManaOf(_enemy), Is.EqualTo(12f), "permanent states are watched too");
            Assert.That(entered.Count, Is.EqualTo(2));
            Assert.That(exited.Count, Is.EqualTo(1));
            Assert.That(entered[0].Tag, Is.EqualTo(AbilityStates.Stunned));
        }

        [Test]
        public void AnimationEvent_FiresReactionsWhoseTagFilterMatches()
        {
            ModifierEventReaction hitFrame = new ModifierEventReaction
            {
                EventId = AbilityEvents.AnimationEvent,
                TagFilter = "hit_frame"
            };
            hitFrame.Effects.Add(Mana(1f, EffectTargetSelector.Target));
            Carry(_hero, hitFrame);
            Drain(_hero);

            _system.RaiseAnimationEvent(_hero.Id, "wind_up");
            _system.RaiseAnimationEvent(_hero.Id, "HIT_FRAME");
            _system.RaiseAnimationEvent(_enemy.Id, "hit_frame");

            Assert.That(ManaOf(_hero), Is.EqualTo(1f));
        }

        [Test]
        public void AppliedAndRemovedTriggers_RunOnceForTheInstance()
        {
            ModifierEventReaction applied = new ModifierEventReaction { Trigger = ModifierTrigger.Applied };
            applied.Effects.Add(Mana(1f, EffectTargetSelector.Target));
            ModifierEventReaction removed = new ModifierEventReaction { Trigger = ModifierTrigger.Removed };
            removed.Effects.Add(Mana(10f, EffectTargetSelector.Target));
            ModifierBlueprint blueprint = AbilityTestSupport.Modifier("lifecycle", 1f);
            blueprint.EventReactions.Add(applied);
            blueprint.EventReactions.Add(removed);
            Drain(_enemy);

            _system.Modifiers.Apply(blueprint, _hero.Id, _enemy.Id);
            _system.Modifiers.Apply(blueprint, _hero.Id, _enemy.Id);
            Assert.That(ManaOf(_enemy), Is.EqualTo(1f), "a refresh does not re-run Applied");
            _system.Tick(2f);
            Assert.That(ManaOf(_enemy), Is.EqualTo(11f));
        }

        [Test]
        public void MaxTriggers_ConsumesCharges_AndRemoveWhenSpentEndsTheModifier()
        {
            AbilityTemplateBundle mark = AbilityTemplates.Mark("mark", bonus: 5f, hits: 2);
            mark.SetUp(_system, _hero.Id);
            _system.Cast(CastRequest.AtUnit(_hero.Id, "mark", _enemy.Id));
            Assert.That(_system.Modifiers.TryGetActive(_enemy.Id, "mark_mark", out ModifierInstance instance), Is.True);

            _system.Cast(CastRequest.AtUnit(_hero.Id, "strike", _enemy.Id));
            Assert.That(instance.GetRemainingCharges(0), Is.EqualTo(1));
            _system.Cast(CastRequest.AtUnit(_hero.Id, "strike", _enemy.Id));
            _system.Cast(CastRequest.AtUnit(_hero.Id, "strike", _enemy.Id));

            Assert.That(instance.IsActive, Is.False, "spent on the second hit");
            Assert.That(_enemy.Health, Is.EqualTo(1000f - 15f - 15f - 10f).Within(0.001f));
        }

        [Test]
        public void InternalCooldown_BlocksRepeatsUntilItElapses()
        {
            ModifierEventReaction reaction = new ModifierEventReaction
            {
                EventId = AbilityEvents.TakeDamage,
                Cooldown = 2f
            };
            reaction.Effects.Add(Mana(1f, EffectTargetSelector.Target));
            Carry(_enemy, reaction);
            Drain(_enemy);

            Hit(_enemy, 1f);
            Hit(_enemy, 1f);
            _system.Tick(1f);
            Hit(_enemy, 1f);
            Assert.That(ManaOf(_enemy), Is.EqualTo(1f));
            _system.Tick(1.01f);
            Hit(_enemy, 1f);
            Assert.That(ManaOf(_enemy), Is.EqualTo(2f));
        }

        [Test]
        public void AbilityFilter_LimitsAReactionToOneAbility()
        {
            ModifierEventReaction onStrike = new ModifierEventReaction
            {
                EventId = AbilityEvents.AbilityCast,
                AbilityFilter = "strike"
            };
            onStrike.Effects.Add(Mana(1f, EffectTargetSelector.Target));
            Carry(_hero, onStrike);
            AbilityBlueprint other = new AbilityBlueprint { Id = "other", Targeting = TargetingMode.NoTarget };
            _system.RegisterAbility(other);
            _system.GrantAbility(_hero.Id, "other");
            Drain(_hero);

            _system.Cast(CastRequest.NoTarget(_hero.Id, "other"));
            _system.Cast(CastRequest.AtUnit(_hero.Id, "strike", _enemy.Id));

            Assert.That(ManaOf(_hero), Is.EqualTo(1f));
        }

        [Test]
        public void ClassicReactions_StillFireOnEveryEvent()
        {
            ModifierEventReaction classic = new ModifierEventReaction { EventId = AbilityEvents.TakeDamage };
            classic.Effects.Add(Mana(1f, EffectTargetSelector.Target));
            Carry(_enemy, classic);
            Drain(_enemy);

            Hit(_enemy, 1f);
            Hit(_enemy, 1f);
            Hit(_enemy, 1f);

            Assert.That(ManaOf(_enemy), Is.EqualTo(3f));
        }

        private ModifierInstance Carry(AbilityUnit owner, params ModifierEventReaction[] reactions)
        {
            ModifierBlueprint blueprint = AbilityTestSupport.Modifier("carrier_" + owner.Id.Value);
            blueprint.EventReactions.AddRange(reactions);
            return _system.Modifiers.Apply(blueprint, owner.Id, owner.Id).Instance;
        }

        private void Hit(AbilityUnit target, float amount)
        {
            DamageService.ApplyDamage(_system, _hero.Id == target.Id ? _enemy.Id : _hero.Id, target.Id, amount,
                AbilityDamageTypes.Pure);
        }

        private static EffectNodeData Mana(float amount, EffectTargetSelector target)
        {
            return new EffectNodeData
            {
                OpId = AbilityEffectOps.ResourceChange,
                Target = target,
                ResourceId = AbilityResourceIds.Mana,
                Amount = amount
            };
        }

        private const float ManaBaseline = 100f;

        /// <summary>Mana gained since <see cref="Drain" /> (a full pool clamps increases, an empty one rejects them).</summary>
        private static float ManaOf(AbilityUnit unit)
        {
            return unit.Resources.GetCurrent(AbilityResourceIds.Mana) - ManaBaseline;
        }

        private static void Drain(AbilityUnit unit)
        {
            unit.Resources.SetCurrent(AbilityResourceIds.Mana, ManaBaseline);
        }

        private List<AbilityEventArgs> Record(string eventId)
        {
            List<AbilityEventArgs> events = new List<AbilityEventArgs>();
            _system.Events.Subscribe(eventId, events.Add);
            return events;
        }
    }
}
