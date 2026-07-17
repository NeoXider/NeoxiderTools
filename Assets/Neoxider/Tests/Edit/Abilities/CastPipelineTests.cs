using System.Collections.Generic;
using Neo.Abilities;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Abilities
{
    /// <summary>
    ///     The cast pipeline: every rejection reason, successful cost payment, cooldown/charges and the
    ///     cooldown_reduction_percent / mana_cost_mul property hooks.
    /// </summary>
    public sealed class CastPipelineTests
    {
        private AbilitySystem _system;
        private AbilityUnit _caster;

        [SetUp]
        public void SetUp()
        {
            _system = new AbilitySystem();
            _caster = AbilityTestSupport.CreateUnit(_system, team: 1, health: 1000f, mana: 100f);
        }

        private AbilityBlueprint Ability(string id, TargetingMode targeting = TargetingMode.NoTarget,
            float cooldown = 0f, int maxCharges = 1, float chargeRestore = 0f, float range = 0f,
            AbilityTeamFilter teamFilter = AbilityTeamFilter.Enemies, params AbilityCost[] costs)
        {
            var ability = new AbilityBlueprint
            {
                Id = id,
                Targeting = targeting,
                Cooldown = cooldown,
                MaxCharges = maxCharges,
                ChargeRestoreTime = chargeRestore,
                Range = range,
                TeamFilter = teamFilter,
                Costs = new List<AbilityCost>(costs)
            };
            _system.RegisterAbility(ability);
            return ability;
        }

        private void Grant(string id)
        {
            _system.GrantAbility(_caster.Id, id);
        }

        [Test]
        public void UnknownCaster_Fails()
        {
            Ability("blast");
            CastResult result = _system.Cast(CastRequest.NoTarget(new UnitId(999), "blast"));
            Assert.That(result.Failure, Is.EqualTo(CastFailureReason.UnknownCaster));
        }

        [Test]
        public void UnknownAbility_Fails()
        {
            CastResult result = _system.Cast(CastRequest.NoTarget(_caster.Id, "does_not_exist"));
            Assert.That(result.Failure, Is.EqualTo(CastFailureReason.UnknownAbility));
        }

        [Test]
        public void NotGranted_Fails()
        {
            Ability("blast"); // WHY: registered but never granted
            CastResult result = _system.Cast(CastRequest.NoTarget(_caster.Id, "blast"));
            Assert.That(result.Failure, Is.EqualTo(CastFailureReason.NotGranted));
        }

        [Test]
        public void CasterDead_Fails()
        {
            Ability("blast");
            Grant("blast");
            _system.MarkDead(_caster, UnitId.None);

            CastResult result = _system.Cast(CastRequest.NoTarget(_caster.Id, "blast"));
            Assert.That(result.Failure, Is.EqualTo(CastFailureReason.CasterDead));
        }

        [Test]
        public void OnCooldown_Fails()
        {
            Ability("blast", cooldown: 10f);
            Grant("blast");

            Assert.That(_system.Cast(CastRequest.NoTarget(_caster.Id, "blast")).Success, Is.True);
            CastResult second = _system.Cast(CastRequest.NoTarget(_caster.Id, "blast"));

            Assert.That(second.Failure, Is.EqualTo(CastFailureReason.OnCooldown));
        }

        [Test]
        public void NoCharges_Fails()
        {
            Ability("dash", maxCharges: 2, chargeRestore: 5f);
            Grant("dash");

            Assert.That(_system.Cast(CastRequest.NoTarget(_caster.Id, "dash")).Success, Is.True);
            Assert.That(_system.Cast(CastRequest.NoTarget(_caster.Id, "dash")).Success, Is.True);
            CastResult third = _system.Cast(CastRequest.NoTarget(_caster.Id, "dash"));

            Assert.That(third.Failure, Is.EqualTo(CastFailureReason.NoCharges));
        }

        [Test]
        public void NotEnoughResources_Fails()
        {
            Ability("blast", costs: new AbilityCost(AbilityResourceIds.Mana, 500f));
            Grant("blast");

            CastResult result = _system.Cast(CastRequest.NoTarget(_caster.Id, "blast"));
            Assert.That(result.Failure, Is.EqualTo(CastFailureReason.NotEnoughResources));
            Assert.That(_caster.Resources.GetCurrent(AbilityResourceIds.Mana), Is.EqualTo(100f),
                "a rejected cast pays nothing");
        }

        [Test]
        public void OutOfRange_Fails()
        {
            var world = new FakeWorldAdapter();
            _system.World = world;
            AbilityUnit target = AbilityTestSupport.CreateUnit(_system, team: 2);
            world.SetPosition(_caster.Id, Vector3.zero);
            world.SetPosition(target.Id, new Vector3(10f, 0f, 0f));

            Ability("bolt", TargetingMode.Unit, range: 5f);
            Grant("bolt");

            CastResult result = _system.Cast(CastRequest.AtUnit(_caster.Id, "bolt", target.Id));
            Assert.That(result.Failure, Is.EqualTo(CastFailureReason.OutOfRange));
        }

        [Test]
        public void InRange_WithCastRangeBonus_Succeeds()
        {
            var world = new FakeWorldAdapter();
            _system.World = world;
            AbilityUnit target = AbilityTestSupport.CreateUnit(_system, team: 2);
            world.SetPosition(_caster.Id, Vector3.zero);
            world.SetPosition(target.Id, new Vector3(7f, 0f, 0f));

            Ability("bolt", TargetingMode.Unit, range: 5f);
            Grant("bolt");
            // WHY: +3 cast range bonus pushes effective range to 8 >= 7.
            _system.Modifiers.Apply(
                AbilityTestSupport.Modifier("scope", 0f).WithProperty(AbilityProperties.CastRangeBonus, PropertyOp.Add, 3f),
                UnitId.None, _caster.Id);

            CastResult result = _system.Cast(CastRequest.AtUnit(_caster.Id, "bolt", target.Id));
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void WrongTeam_Fails()
        {
            AbilityUnit ally = AbilityTestSupport.CreateUnit(_system, team: 1); // WHY: same team as caster
            Ability("smite", TargetingMode.Unit, teamFilter: AbilityTeamFilter.Enemies);
            Grant("smite");

            CastResult result = _system.Cast(CastRequest.AtUnit(_caster.Id, "smite", ally.Id));
            Assert.That(result.Failure, Is.EqualTo(CastFailureReason.WrongTeam));
        }

        [Test]
        public void InvalidTarget_Fails()
        {
            Ability("smite", TargetingMode.Unit);
            Grant("smite");

            CastResult result = _system.Cast(CastRequest.AtUnit(_caster.Id, "smite", new UnitId(4242)));
            Assert.That(result.Failure, Is.EqualTo(CastFailureReason.InvalidTarget));
        }

        [Test]
        public void TargetDead_Fails()
        {
            AbilityUnit enemy = AbilityTestSupport.CreateUnit(_system, team: 2);
            _system.MarkDead(enemy, UnitId.None);
            Ability("smite", TargetingMode.Unit);
            Grant("smite");

            CastResult result = _system.Cast(CastRequest.AtUnit(_caster.Id, "smite", enemy.Id));
            Assert.That(result.Failure, Is.EqualTo(CastFailureReason.TargetDead));
        }

        [Test]
        public void UntargetableEnemy_Fails()
        {
            AbilityUnit enemy = AbilityTestSupport.CreateUnit(_system, team: 2);
            enemy.SetPermanentState(AbilityStates.Untargetable, true);
            Ability("smite", TargetingMode.Unit);
            Grant("smite");

            CastResult result = _system.Cast(CastRequest.AtUnit(_caster.Id, "smite", enemy.Id));
            Assert.That(result.Failure, Is.EqualTo(CastFailureReason.TargetUntargetable));
        }

        [Test]
        public void Success_PaysCostOnce_SetsCooldown_FiresAbilityCast()
        {
            var log = new EventLog(_system);
            Ability("blast", cooldown: 10f, costs: new AbilityCost(AbilityResourceIds.Mana, 30f));
            Grant("blast");

            CastResult result = _system.Cast(CastRequest.NoTarget(_caster.Id, "blast"));

            Assert.That(result.Success, Is.True);
            Assert.That(result.CastId, Is.GreaterThan(0u));
            Assert.That(_caster.Resources.GetCurrent(AbilityResourceIds.Mana), Is.EqualTo(70f).Within(0.001f),
                "cost paid exactly once");

            _system.TryGetSlot(_caster.Id, "blast", out AbilitySlot slot);
            Assert.That(slot.CooldownRemaining, Is.EqualTo(10f).Within(0.001f));

            Assert.That(log.TryGetLast(AbilityEvents.AbilityCast, out AbilityEventArgs cast), Is.True);
            Assert.That(cast.CastId, Is.EqualTo(result.CastId));
        }

        [Test]
        public void EachSuccessfulCast_GetsFreshCastId()
        {
            Ability("blast", cooldown: 0f);
            Grant("blast");

            uint first = _system.Cast(CastRequest.NoTarget(_caster.Id, "blast")).CastId;
            uint second = _system.Cast(CastRequest.NoTarget(_caster.Id, "blast")).CastId;

            Assert.That(second, Is.Not.EqualTo(first));
            Assert.That(second, Is.GreaterThan(first));
        }

        [Test]
        public void CooldownReductionPercent_ShortensCooldown()
        {
            Ability("blast", cooldown: 10f);
            Grant("blast");
            _system.Modifiers.Apply(
                AbilityTestSupport.Modifier("haste", 0f).WithProperty(AbilityProperties.CooldownReductionPercent, PropertyOp.Add, 50f),
                UnitId.None, _caster.Id);

            _system.Cast(CastRequest.NoTarget(_caster.Id, "blast"));

            _system.TryGetSlot(_caster.Id, "blast", out AbilitySlot slot);
            Assert.That(slot.CooldownRemaining, Is.EqualTo(5f).Within(0.001f));
        }

        [Test]
        public void CooldownReductionPercent_ClampedAt90()
        {
            Ability("blast", cooldown: 10f);
            Grant("blast");
            _system.Modifiers.Apply(
                AbilityTestSupport.Modifier("haste", 0f).WithProperty(AbilityProperties.CooldownReductionPercent, PropertyOp.Add, 200f),
                UnitId.None, _caster.Id);

            _system.Cast(CastRequest.NoTarget(_caster.Id, "blast"));

            _system.TryGetSlot(_caster.Id, "blast", out AbilitySlot slot);
            Assert.That(slot.CooldownRemaining, Is.EqualTo(1f).Within(0.001f), "cdr capped at 90%");
        }

        [Test]
        public void ManaCostMul_ScalesCost()
        {
            Ability("blast", costs: new AbilityCost(AbilityResourceIds.Mana, 40f));
            Grant("blast");
            _system.Modifiers.Apply(
                AbilityTestSupport.Modifier("efficient", 0f).WithProperty(AbilityProperties.ManaCostMul, PropertyOp.Mul, 0.5f),
                UnitId.None, _caster.Id);

            _system.Cast(CastRequest.NoTarget(_caster.Id, "blast"));

            Assert.That(_caster.Resources.GetCurrent(AbilityResourceIds.Mana), Is.EqualTo(80f).Within(0.001f),
                "40 mana cost halved to 20");
        }

        [Test]
        public void Charges_DecrementOnCast_AndRestoreOverTime()
        {
            Ability("dash", maxCharges: 2, chargeRestore: 2f);
            Grant("dash");
            _system.TryGetSlot(_caster.Id, "dash", out AbilitySlot slot);
            Assert.That(slot.Charges, Is.EqualTo(2));

            _system.Cast(CastRequest.NoTarget(_caster.Id, "dash"));
            _system.Cast(CastRequest.NoTarget(_caster.Id, "dash"));
            Assert.That(slot.Charges, Is.EqualTo(0));

            _system.Tick(2f);
            Assert.That(slot.Charges, Is.EqualTo(1), "one charge restored after the restore time");

            _system.Tick(2f);
            Assert.That(slot.Charges, Is.EqualTo(2), "back to full");
            Assert.That(slot.CooldownRemaining, Is.EqualTo(0f).Within(0.001f));
        }
    }
}
