using Neo.Abilities;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Abilities
{
    /// <summary>
    ///     Behaviour of every ready template (auto-cast technique, attack enhancer, auto-defense shield, mark, stacking
    ///     poison, thorns, aura, formation zone), the catalog and the content validator's verdict on them.
    /// </summary>
    public sealed class AbilityTemplatesTests
    {
        private FakeWorldAdapter _world;
        private AbilitySystem _system;
        private AbilityUnit _hero;
        private AbilityUnit _enemy;

        [SetUp]
        public void SetUp()
        {
            _world = new FakeWorldAdapter();
            _system = new AbilitySystem { World = _world };
            _hero = AbilityTestSupport.CreateUnit(_system, team: 1, health: 1000f, name: "Hero");
            _enemy = AbilityTestSupport.CreateUnit(_system, team: 2, health: 1000f, name: "Enemy");
            _world.SetPosition(_hero.Id, Vector3.zero);
            _world.SetPosition(_enemy.Id, new Vector3(3f, 0f, 0f));
            AbilityBlueprint strike = new AbilityBlueprint { Id = "strike", Targeting = TargetingMode.Unit };
            strike.ImpactEffects.Add(AbilityTestSupport.DamageNode(10f));
            _system.RegisterAbility(strike);
            _system.GrantAbility(_hero.Id, "strike");
            _system.GrantAbility(_enemy.Id, "strike");
        }

        [Test]
        public void Catalog_ListsEveryTemplate_AndEachBuildsValidContent()
        {
            Assert.That(AbilityTemplates.Catalog.Count, Is.EqualTo(8));
            foreach (AbilityTemplateInfo info in AbilityTemplates.Catalog)
            {
                AbilityTemplateBundle bundle = info.Create();
                Assert.That(bundle.TemplateId, Is.EqualTo(info.Id));
                Assert.That(bundle.Abilities.Count + bundle.Modifiers.Count, Is.GreaterThan(0), info.Id);
                Assert.That(AbilityContentValidator.Validate(bundle.Abilities, bundle.Modifiers, _system.Ops),
                    Is.Empty, info.Id + " has validation findings");
                Assert.That(AbilityTemplates.TryGet(info.Id.ToUpperInvariant(), out AbilityTemplateInfo found), Is.True);
                Assert.That(found, Is.SameAs(info));
            }
        }

        [Test]
        public void AutoCastTechnique_CastsAtTheEnemyEveryInterval()
        {
            AbilityTemplates.AutoCastTechnique("bolt", interval: 2f, damage: 20f, range: 8f).SetUp(_system, _hero.Id);

            _system.Tick(1f);
            Assert.That(_enemy.Health, Is.EqualTo(1000f));
            _system.Tick(1f);
            _system.Tick(2f);

            Assert.That(_enemy.Health, Is.EqualTo(960f).Within(0.001f));
        }

        [Test]
        public void AttackEnhancer_EmpowersTheNextHits_ThenEnds()
        {
            AbilityTemplates.AttackEnhancer("power", multiplier: 1.5f, hits: 2).SetUp(_system, _hero.Id);
            _system.Cast(CastRequest.NoTarget(_hero.Id, "power_cast"));

            for (int i = 0; i < 3; i++)
            {
                _system.Cast(CastRequest.AtUnit(_hero.Id, "strike", _enemy.Id));
            }

            Assert.That(_enemy.Health, Is.EqualTo(1000f - 15f - 15f - 10f).Within(0.001f));
            Assert.That(_system.Modifiers.CountActive(_hero.Id, "power"), Is.Zero);
        }

        [Test]
        public void AutoDefenseShield_TriggersBelowTheThreshold_WithAnInternalCooldown()
        {
            AbilityTemplates.AutoDefenseShield("barrier", threshold: 0.5f, shield: 100f, duration: 5f,
                internalCooldown: 10f).SetUp(_system, _hero.Id);

            Hit(_hero, 400f);
            Assert.That(_system.Modifiers.CountActive(_hero.Id, "barrier"), Is.Zero);
            Hit(_hero, 200f);
            Assert.That(_system.Modifiers.CountActive(_hero.Id, "barrier"), Is.EqualTo(1), "60% -> 40%");
            Hit(_hero, 50f);
            Assert.That(_hero.Health, Is.EqualTo(400f).Within(0.001f), "the shield absorbed the next hit");

            _hero.Resources.Increase(AbilityResourceIds.Health, 600f);
            _system.Tick(6f);
            Hit(_hero, 700f);
            Assert.That(_system.Modifiers.CountActive(_hero.Id, "barrier"), Is.Zero, "still on its internal cooldown");
            _hero.Resources.Increase(AbilityResourceIds.Health, 700f);
            _system.Tick(5f);
            Hit(_hero, 700f);
            Assert.That(_system.Modifiers.CountActive(_hero.Id, "barrier"), Is.EqualTo(1));
        }

        [Test]
        public void Mark_AddsBonusDamageToTheNextHits_ThenIsConsumed()
        {
            AbilityTemplates.Mark("mark", bonus: 20f, hits: 3).SetUp(_system, _hero.Id);
            _system.Cast(CastRequest.AtUnit(_hero.Id, "mark", _enemy.Id));

            for (int i = 0; i < 4; i++)
            {
                _system.Cast(CastRequest.AtUnit(_hero.Id, "strike", _enemy.Id));
            }

            Assert.That(_enemy.Health, Is.EqualTo(1000f - 30f * 3f - 10f).Within(0.001f));
            Assert.That(_system.Modifiers.CountActive(_enemy.Id, "mark_mark"), Is.Zero);
        }

        [Test]
        public void StackingPoison_StacksOnEveryHit_AndTicksPerStack()
        {
            AbilityTemplates.StackingPoison("venom", tickDamage: 4f, maxStacks: 3, duration: 5f).SetUp(_system, _hero.Id);

            for (int i = 0; i < 4; i++)
            {
                _system.Cast(CastRequest.AtUnit(_hero.Id, "strike", _enemy.Id));
            }

            Assert.That(_system.Modifiers.TryGetActive(_enemy.Id, "venom", out ModifierInstance poison), Is.True);
            Assert.That(poison.Stacks, Is.EqualTo(3), "the coating stacks on every hit up to the cap");
            float before = _enemy.Health;
            _system.Tick(1f);

            Assert.That(before - _enemy.Health, Is.EqualTo(12f).Within(0.001f), "4 per stack x 3 stacks");
        }

        [Test]
        public void Thorns_ReflectsAShareOfEachHit()
        {
            AbilityTemplates.Thorns("thorns", fraction: 0.5f).SetUp(_system, _enemy.Id);

            _system.Cast(CastRequest.AtUnit(_hero.Id, "strike", _enemy.Id));

            Assert.That(_enemy.Health, Is.EqualTo(990f).Within(0.001f));
            Assert.That(_hero.Health, Is.EqualTo(995f).Within(0.001f));
        }

        [Test]
        public void AuraAndFormation_BuffAlliesAndHurtEnemiesInside()
        {
            AbilityUnit ally = AbilityTestSupport.CreateUnit(_system, team: 1, health: 1000f, name: "Ally");
            _world.SetPosition(ally.Id, new Vector3(0f, 0f, 2f));
            AbilityTemplates.Aura("banner", radius: 5f, armor: 3f, damageMultiplier: 1.2f, duration: 4f)
                .SetUp(_system, _hero.Id);
            AbilityTemplates.FormationZone("trap", radius: 2f, duration: 2f, interval: 1f, damage: 10f)
                .SetUp(_system, _hero.Id);

            _system.Cast(CastRequest.NoTarget(_hero.Id, "banner"));
            _system.Cast(CastRequest.AtPoint(_hero.Id, "trap", new Vector3(3f, 0f, 0f)));

            Assert.That(ally.GetProperty(AbilityProperties.Armor), Is.EqualTo(3f).Within(0.001f));
            Assert.That(_enemy.Health, Is.EqualTo(988f).Within(0.001f),
                "the formation pulsed on spawn; its caster stands in its own aura, so 10 x1.2");
            _system.Cast(CastRequest.AtUnit(_hero.Id, "strike", _enemy.Id));
            Assert.That(_enemy.Health, Is.EqualTo(976f).Within(0.001f), "the aura's x1.2 applies to the hero's hit");
        }

        private void Hit(AbilityUnit target, float amount)
        {
            // WHY: magical, not pure: pure damage bypasses shields by design.
            DamageService.ApplyDamage(_system, _enemy.Id, target.Id, amount, AbilityDamageTypes.Magical);
        }
    }
}
