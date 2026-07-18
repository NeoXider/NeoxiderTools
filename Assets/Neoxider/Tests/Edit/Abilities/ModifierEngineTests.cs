using System.Collections.Generic;
using Neo.Abilities;
using NUnit.Framework;

namespace Neo.Editor.Tests.Abilities
{
    /// <summary>
    ///     Modifier stack policies, expiry, dispel and the property/state aggregation feed onto units.
    /// </summary>
    public sealed class ModifierEngineTests
    {
        private AbilitySystem _system;
        private AbilityUnit _caster;
        private AbilityUnit _unit;

        [SetUp]
        public void SetUp()
        {
            _system = new AbilitySystem();
            _caster = AbilityTestSupport.CreateUnit(_system, team: 1);
            _unit = AbilityTestSupport.CreateUnit(_system, team: 2);
        }

        private ModifierApplyResult Apply(ModifierBlueprint bp)
        {
            return _system.Modifiers.Apply(bp, _caster.Id, _unit.Id, "src");
        }

        [Test]
        public void Independent_CreatesOneInstancePerApplication()
        {
            ModifierBlueprint bp = AbilityTestSupport.Modifier("dot", 10f, ModifierStackPolicy.Independent);

            Apply(bp);
            Apply(bp);
            ModifierApplyResult third = Apply(bp);

            Assert.That(third.CreatedNew, Is.True);
            Assert.That(AbilityTestSupport.ActiveModifiers(_system, _unit.Id).Count, Is.EqualTo(3));
        }

        [Test]
        public void Refresh_KeepsSingleInstance_AndResetsDuration()
        {
            ModifierBlueprint bp = AbilityTestSupport.Modifier("slow", 10f, ModifierStackPolicy.Refresh);
            ModifierInstance instance = Apply(bp).Instance;

            _system.Modifiers.Tick(4f);
            Assert.That(instance.RemainingDuration, Is.EqualTo(6f).Within(0.0001f));

            ModifierApplyResult second = Apply(bp);

            Assert.That(second.CreatedNew, Is.False);
            Assert.That(second.Instance, Is.SameAs(instance));
            Assert.That(instance.RemainingDuration, Is.EqualTo(10f).Within(0.0001f));
            Assert.That(AbilityTestSupport.ActiveModifiers(_system, _unit.Id).Count, Is.EqualTo(1));
        }

        [Test]
        public void Stack_IncrementsUpToMaxStacks_AndScalesPropertyPerStack()
        {
            ModifierBlueprint bp = AbilityTestSupport.Modifier("armor_stack", 10f, ModifierStackPolicy.Stack);
            bp.MaxStacks = 3;
            bp.WithProperty(AbilityProperties.Armor, PropertyOp.Add, 5f, 5f); // WHY: 5, 10, 15 per stack

            ModifierInstance instance = Apply(bp).Instance;
            Assert.That(instance.Stacks, Is.EqualTo(1));
            Assert.That(_unit.GetProperty(AbilityProperties.Armor), Is.EqualTo(5f).Within(0.0001f));

            Apply(bp);
            Assert.That(instance.Stacks, Is.EqualTo(2));
            Assert.That(_unit.GetProperty(AbilityProperties.Armor), Is.EqualTo(10f).Within(0.0001f));

            Apply(bp);
            Assert.That(instance.Stacks, Is.EqualTo(3));
            Assert.That(_unit.GetProperty(AbilityProperties.Armor), Is.EqualTo(15f).Within(0.0001f));

            Apply(bp);
            Assert.That(instance.Stacks, Is.EqualTo(3));
            Assert.That(AbilityTestSupport.ActiveModifiers(_system, _unit.Id).Count, Is.EqualTo(1));
        }

        [Test]
        public void Duration_GuaranteesRemovalAndRaisesExpiredRemovedEvent()
        {
            bool removed = false;
            bool expiredFlag = false;
            _system.Modifiers.Removed += (m, expired) =>
            {
                removed = true;
                expiredFlag = expired;
            };

            Apply(AbilityTestSupport.Modifier("buff", 2f));

            _system.Modifiers.Tick(1f);
            Assert.That(removed, Is.False, "must survive until duration elapses");

            _system.Modifiers.Tick(1f);

            Assert.That(removed, Is.True);
            Assert.That(expiredFlag, Is.True, "natural expiry reports expired=true");
            Assert.That(AbilityTestSupport.ActiveModifiers(_system, _unit.Id).Count, Is.EqualTo(0));
        }

        [Test]
        public void Permanent_Duration_NeverExpires()
        {
            Apply(AbilityTestSupport.Modifier("aura", 0f)); // WHY: Duration <= 0 => permanent

            _system.Modifiers.Tick(1000f);

            List<ModifierInstance> active = AbilityTestSupport.ActiveModifiers(_system, _unit.Id);
            Assert.That(active.Count, Is.EqualTo(1));
            Assert.That(active[0].IsPermanent, Is.True);
        }

        [Test]
        public void ExplicitRemoval_ReportsExpiredFalse()
        {
            bool? expired = null;
            _system.Modifiers.Removed += (m, e) => expired = e;

            ModifierInstance instance = Apply(AbilityTestSupport.Modifier("buff", 10f)).Instance;
            _system.Modifiers.Remove(instance);

            Assert.That(expired, Is.False);
            Assert.That(instance.IsActive, Is.False);
        }

        [Test]
        public void RemoveById_RemovesAllMatchingInstances()
        {
            ModifierBlueprint bp = AbilityTestSupport.Modifier("dot", 10f, ModifierStackPolicy.Independent);
            Apply(bp);
            Apply(bp);
            Apply(bp);

            int removed = _system.Modifiers.RemoveById(_unit.Id, "dot");

            Assert.That(removed, Is.EqualTo(3));
            Assert.That(AbilityTestSupport.ActiveModifiers(_system, _unit.Id).Count, Is.EqualTo(0));
        }

        [Test]
        public void RemoveById_SingleInstance_RemovesOnlyOne()
        {
            ModifierBlueprint bp = AbilityTestSupport.Modifier("dot", 10f, ModifierStackPolicy.Independent);
            Apply(bp);
            Apply(bp);

            int removed = _system.Modifiers.RemoveById(_unit.Id, "dot", allInstances: false);

            Assert.That(removed, Is.EqualTo(1));
            Assert.That(AbilityTestSupport.ActiveModifiers(_system, _unit.Id).Count, Is.EqualTo(1));
        }

        [Test]
        public void RemoveWhere_RemovesOnlyMatching()
        {
            Apply(AbilityTestSupport.Modifier("keep", 10f));
            ModifierBlueprint debuff = AbilityTestSupport.Modifier("burn", 10f);
            debuff.IsDebuff = true;
            Apply(debuff);

            int removed = _system.Modifiers.RemoveWhere(_unit.Id, m => m.Blueprint.IsDebuff);

            Assert.That(removed, Is.EqualTo(1));
            List<ModifierInstance> active = AbilityTestSupport.ActiveModifiers(_system, _unit.Id);
            Assert.That(active.Count, Is.EqualTo(1));
            Assert.That(active[0].Blueprint.Id, Is.EqualTo("keep"));
        }

        [Test]
        public void Dispel_DefaultRemovesDebuffsAndKeepsBuffs()
        {
            ModifierBlueprint buff = AbilityTestSupport.Modifier("blessing", 10f);
            buff.IsDebuff = false;
            ModifierBlueprint debuff = AbilityTestSupport.Modifier("poison", 10f);
            debuff.IsDebuff = true;
            ModifierBlueprint stubborn = AbilityTestSupport.Modifier("curse", 10f);
            stubborn.IsDebuff = true;
            stubborn.Dispellable = false;

            _system.RegisterModifier(buff);
            _system.RegisterModifier(debuff);
            _system.RegisterModifier(stubborn);
            _system.Modifiers.Apply(buff, _caster.Id, _unit.Id);
            _system.Modifiers.Apply(debuff, _caster.Id, _unit.Id);
            _system.Modifiers.Apply(stubborn, _caster.Id, _unit.Id);

            var dispel = new EffectNodeData { OpId = AbilityEffectOps.Dispel, Target = EffectTargetSelector.Target };
            EffectContext ctx = AbilityTestSupport.Context(_system, _caster.Id, targets: _unit.Id);
            _system.ExecuteEffects(AbilityTestSupport.Nodes(dispel), ctx);

            var remaining = new List<string>();
            foreach (ModifierInstance m in AbilityTestSupport.ActiveModifiers(_system, _unit.Id))
            {
                remaining.Add(m.Blueprint.Id);
            }

            Assert.That(remaining, Does.Contain("blessing"));
            Assert.That(remaining, Does.Contain("curse"), "non-dispellable debuff survives");
            Assert.That(remaining, Does.Not.Contain("poison"));
        }

        [Test]
        public void Dispel_BuffsVariant_StripsBuffsOnly()
        {
            ModifierBlueprint buff = AbilityTestSupport.Modifier("haste", 10f);
            buff.IsDebuff = false;
            ModifierBlueprint debuff = AbilityTestSupport.Modifier("chill", 10f);
            debuff.IsDebuff = true;
            _system.Modifiers.Apply(buff, _caster.Id, _unit.Id);
            _system.Modifiers.Apply(debuff, _caster.Id, _unit.Id);

            var dispel = new EffectNodeData
            {
                OpId = AbilityEffectOps.Dispel,
                Target = EffectTargetSelector.Target,
                CustomParam = "buffs"
            };
            EffectContext ctx = AbilityTestSupport.Context(_system, _caster.Id, targets: _unit.Id);
            _system.ExecuteEffects(AbilityTestSupport.Nodes(dispel), ctx);

            var remaining = new List<string>();
            foreach (ModifierInstance m in AbilityTestSupport.ActiveModifiers(_system, _unit.Id))
            {
                remaining.Add(m.Blueprint.Id);
            }

            Assert.That(remaining, Does.Contain("chill"));
            Assert.That(remaining, Does.Not.Contain("haste"));
        }

        [Test]
        public void VersionOf_BumpsOnApplyAndRemove()
        {
            int v0 = _system.Modifiers.VersionOf(_unit.Id);

            ModifierInstance instance = Apply(AbilityTestSupport.Modifier("buff", 10f)).Instance;
            int v1 = _system.Modifiers.VersionOf(_unit.Id);
            Assert.That(v1, Is.GreaterThan(v0));

            _system.Modifiers.Remove(instance);
            int v2 = _system.Modifiers.VersionOf(_unit.Id);
            Assert.That(v2, Is.GreaterThan(v1));
        }

        [Test]
        public void GetProperty_ReflectsNewlyAppliedModifierImmediately()
        {
            Assert.That(_unit.GetProperty(AbilityProperties.Armor), Is.EqualTo(0f));

            // WHY: Prime the cache, then mutate underlying modifiers.
            Apply(AbilityTestSupport.Modifier("iron", 10f).WithProperty(AbilityProperties.Armor, PropertyOp.Add, 10f));

            Assert.That(_unit.GetProperty(AbilityProperties.Armor), Is.EqualTo(10f).Within(0.0001f),
                "property cache must invalidate on the version bump");
        }

        [Test]
        public void GetProperty_ReflectsRemovalImmediately()
        {
            ModifierInstance instance = Apply(AbilityTestSupport.Modifier("iron", 10f)
                .WithProperty(AbilityProperties.Armor, PropertyOp.Add, 10f)).Instance;
            Assert.That(_unit.GetProperty(AbilityProperties.Armor), Is.EqualTo(10f).Within(0.0001f));

            _system.Modifiers.Remove(instance);

            Assert.That(_unit.GetProperty(AbilityProperties.Armor), Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void SetBaseProperty_InvalidatesCache()
        {
            Assert.That(_unit.GetProperty(AbilityProperties.Armor), Is.EqualTo(0f));
            _unit.SetBaseProperty(AbilityProperties.Armor, 25f);
            Assert.That(_unit.GetProperty(AbilityProperties.Armor), Is.EqualTo(25f).Within(0.0001f));
        }

        [Test]
        public void GetProperty_CombinesBaseAddAndMul()
        {
            _unit.SetBaseProperty(AbilityProperties.AttackDamage, 100f);
            Apply(AbilityTestSupport.Modifier("might", 10f)
                .WithProperty(AbilityProperties.AttackDamage, PropertyOp.Add, 20f)
                .WithProperty(AbilityProperties.AttackDamage, PropertyOp.Mul, 1.5f));

            // WHY: (100 + 20) * 1.5
            Assert.That(_unit.GetProperty(AbilityProperties.AttackDamage), Is.EqualTo(180f).Within(0.0001f));
        }

        [Test]
        public void MaxHealthBonus_RaisesPoolMax_AndRevertsOnRemoval()
        {
            AbilityUnit unit = AbilityTestSupport.CreateUnit(_system, team: 1, health: 100f);
            ModifierBlueprint bp = AbilityTestSupport.Modifier("vitality", 0f)
                .WithProperty(AbilityProperties.MaxHealthBonus, PropertyOp.Add, 50f);

            ModifierInstance instance = _system.Modifiers.Apply(bp, unit.Id, unit.Id).Instance;

            Assert.That(unit.MaxHealth, Is.EqualTo(150f).Within(0.0001f), "bonus raises the pool max");
            Assert.That(unit.Health, Is.EqualTo(100f).Within(0.0001f), "raising max does not heal");

            unit.Resources.Increase(AbilityResourceIds.Health, 50f);
            Assert.That(unit.Health, Is.EqualTo(150f).Within(0.0001f), "the bonus headroom is usable");

            _system.Modifiers.Remove(instance);

            Assert.That(unit.MaxHealth, Is.EqualTo(100f).Within(0.0001f), "removal restores the base max");
            Assert.That(unit.Health, Is.EqualTo(100f).Within(0.0001f), "current clamps back to the base max");
        }

        [Test]
        public void MaxManaBonus_PerStack_ScalesPoolMax()
        {
            AbilityUnit unit = AbilityTestSupport.CreateUnit(_system, team: 1, health: 100f, mana: 50f);
            ModifierBlueprint bp = AbilityTestSupport.Modifier("wisdom", 10f, ModifierStackPolicy.Stack);
            bp.MaxStacks = 3;
            bp.WithProperty(AbilityProperties.MaxManaBonus, PropertyOp.Add, 25f, 25f); // WHY: 25/50/75 per stack

            _system.Modifiers.Apply(bp, unit.Id, unit.Id);
            Assert.That(unit.Resources.GetMax(AbilityResourceIds.Mana), Is.EqualTo(75f).Within(0.0001f));

            _system.Modifiers.Apply(bp, unit.Id, unit.Id);
            Assert.That(unit.Resources.GetMax(AbilityResourceIds.Mana), Is.EqualTo(100f).Within(0.0001f),
                "stack growth re-syncs the pool max");

            _system.Modifiers.RemoveAllFrom(unit.Id);
            Assert.That(unit.Resources.GetMax(AbilityResourceIds.Mana), Is.EqualTo(50f).Within(0.0001f));
        }

        [Test]
        public void CollectContributions_AppliesPerStackScaling()
        {
            ModifierBlueprint bp = AbilityTestSupport.Modifier("stackarmor", 10f, ModifierStackPolicy.Stack);
            bp.MaxStacks = 5;
            bp.WithProperty(AbilityProperties.Armor, PropertyOp.Add, 2f, 2f);
            Apply(bp);
            Apply(bp);
            Apply(bp); // WHY: 3 stacks => 2 + 2*2 = 6

            var contributions = new List<ResolvedContribution>();
            _system.Modifiers.CollectContributions(_unit.Id, AbilityProperties.Armor, contributions);

            Assert.That(contributions.Count, Is.EqualTo(1));
            Assert.That(contributions[0].Op, Is.EqualTo(PropertyOp.Add));
            Assert.That(contributions[0].Value, Is.EqualTo(6f).Within(0.0001f));
        }
    }
}
