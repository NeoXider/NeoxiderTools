using Neo.Abilities;
using NUnit.Framework;

namespace Neo.Editor.Tests.Abilities
{
    /// <summary>
    ///     The damage pipeline: armor/resist by type, invulnerability/immunity, shield absorption,
    ///     health pool, death/kill events and non-negative clamping.
    /// </summary>
    public sealed class DamageServiceTests
    {
        private AbilitySystem _system;
        private AbilityUnit _source;
        private AbilityUnit _target;
        private EventLog _log;

        [SetUp]
        public void SetUp()
        {
            _system = new AbilitySystem();
            _source = AbilityTestSupport.CreateUnit(_system, team: 1);
            _target = AbilityTestSupport.CreateUnit(_system, team: 2, health: 1000f);
            _log = new EventLog(_system);
        }

        private DamageResult Deal(float amount, string type, uint castId = 0)
        {
            return DamageService.ApplyDamage(_system, _source.Id, _target.Id, amount, type, "atk", castId);
        }

        [Test]
        public void Physical_ReducedByArmor()
        {
            _target.SetBaseProperty(AbilityProperties.Armor, 10f); // reduction = 0.06*10/(1+0.6) = 0.375

            DamageResult result = Deal(100f, AbilityDamageTypes.Physical);

            Assert.That(result.HealthDamage, Is.EqualTo(62.5f).Within(0.01f));
            Assert.That(_target.Health, Is.EqualTo(937.5f).Within(0.01f));
        }

        [Test]
        public void Magical_ReducedByMagicResistPercent()
        {
            _target.SetBaseProperty(AbilityProperties.MagicResistPercent, 25f); // *0.75

            DamageResult result = Deal(100f, AbilityDamageTypes.Magical);

            Assert.That(result.HealthDamage, Is.EqualTo(75f).Within(0.01f));
        }

        [Test]
        public void Pure_IgnoresArmorAndResist()
        {
            _target.SetBaseProperty(AbilityProperties.Armor, 10f);
            _target.SetBaseProperty(AbilityProperties.MagicResistPercent, 25f);

            DamageResult result = Deal(100f, AbilityDamageTypes.Pure);

            Assert.That(result.HealthDamage, Is.EqualTo(100f).Within(0.01f));
        }

        [Test]
        public void Invulnerable_BlocksAllDamage()
        {
            _target.SetPermanentState(AbilityStates.Invulnerable, true);

            DamageResult result = Deal(100f, AbilityDamageTypes.Pure);

            Assert.That(result.Negated, Is.True);
            Assert.That(result.HealthDamage, Is.EqualTo(0f));
            Assert.That(_target.Health, Is.EqualTo(1000f));
            Assert.That(_log.Any(AbilityEvents.TakeDamage), Is.False);
        }

        [Test]
        public void MagicImmune_BlocksMagicalButNotPhysical()
        {
            _target.SetPermanentState(AbilityStates.MagicImmune, true);

            DamageResult magic = Deal(100f, AbilityDamageTypes.Magical);
            Assert.That(magic.Negated, Is.True);
            Assert.That(_target.Health, Is.EqualTo(1000f));

            DamageResult physical = Deal(100f, AbilityDamageTypes.Physical); // armor 0 => full
            Assert.That(physical.HealthDamage, Is.EqualTo(100f).Within(0.01f));
        }

        [Test]
        public void Shield_AbsorbsBeforeHealth_AndTracksConsumption()
        {
            _system.Modifiers.Apply(
                AbilityTestSupport.Modifier("barrier", 0f).WithProperty(AbilityProperties.ShieldHp, PropertyOp.Add, 50f),
                UnitId.None, _target.Id);

            DamageResult result = Deal(30f, AbilityDamageTypes.Magical);

            Assert.That(result.Absorbed, Is.EqualTo(30f).Within(0.01f));
            Assert.That(result.HealthDamage, Is.EqualTo(0f));
            Assert.That(_target.Health, Is.EqualTo(1000f), "shield fully soaks the hit");
            Assert.That(_log.Count(AbilityEvents.ShieldAbsorbed), Is.EqualTo(1));
            Assert.That(_log.Any(AbilityEvents.ShieldBroken), Is.False);
        }

        [Test]
        public void Shield_Overflow_BreaksAndSpillsToHealth()
        {
            _system.Modifiers.Apply(
                AbilityTestSupport.Modifier("barrier", 0f).WithProperty(AbilityProperties.ShieldHp, PropertyOp.Add, 50f),
                UnitId.None, _target.Id);

            Deal(30f, AbilityDamageTypes.Magical); // consume 30 of 50
            DamageResult result = Deal(40f, AbilityDamageTypes.Magical); // absorb 20, spill 20

            Assert.That(result.Absorbed, Is.EqualTo(20f).Within(0.01f));
            Assert.That(result.HealthDamage, Is.EqualTo(20f).Within(0.01f));
            Assert.That(_target.Health, Is.EqualTo(980f).Within(0.01f));
            Assert.That(_log.Any(AbilityEvents.ShieldBroken), Is.True);
            Assert.That(AbilityTestSupport.ActiveModifiers(_system, _target.Id).Count, Is.EqualTo(0),
                "broken shield modifier is removed");
        }

        [Test]
        public void PureDamage_BypassesShield()
        {
            // Pure is true damage: it bypasses shield absorption by design (only invulnerability
            // stops it). Documented on AbilityDamageTypes.Pure.
            _system.Modifiers.Apply(
                AbilityTestSupport.Modifier("barrier", 0f).WithProperty(AbilityProperties.ShieldHp, PropertyOp.Add, 50f),
                UnitId.None, _target.Id);

            DamageResult result = Deal(30f, AbilityDamageTypes.Pure);

            Assert.That(result.Absorbed, Is.EqualTo(0f), "pure skips shield absorption in code");
            Assert.That(result.HealthDamage, Is.EqualTo(30f).Within(0.01f));
        }

        [Test]
        public void LethalDamage_KillsTarget_AndFiresDeathAndKillWithCastId()
        {
            _target = AbilityTestSupport.CreateUnit(_system, team: 2, health: 50f);
            const uint castId = 77u;

            DamageResult result = DamageService.ApplyDamage(_system, _source.Id, _target.Id, 100f,
                AbilityDamageTypes.Pure, "finisher", castId);

            Assert.That(result.Killed, Is.True);
            Assert.That(_target.IsAlive, Is.False);

            Assert.That(_log.TryGetLast(AbilityEvents.Death, out AbilityEventArgs death), Is.True);
            Assert.That(death.Target, Is.EqualTo(_target.Id));
            Assert.That(death.CastId, Is.EqualTo(castId));

            Assert.That(_log.TryGetLast(AbilityEvents.Kill, out AbilityEventArgs kill), Is.True);
            Assert.That(kill.Target, Is.EqualTo(_source.Id), "kill event Target field carries the killer");
            Assert.That(kill.Source, Is.EqualTo(_target.Id));
            Assert.That(kill.CastId, Is.EqualTo(castId));
        }

        [Test]
        public void Revive_AfterLethalDamage_RestoresHealth()
        {
            _target = AbilityTestSupport.CreateUnit(_system, team: 2, health: 100f);
            DamageService.ApplyDamage(_system, _source.Id, _target.Id, 200f, AbilityDamageTypes.Pure);
            Assert.That(_target.IsAlive, Is.False);

            _system.Revive(_target, 1f);

            Assert.That(_target.IsAlive, Is.True);
            Assert.That(_target.Health, Is.EqualTo(100f).Within(0.001f));
        }

        [Test]
        public void DeadTarget_TakesNoFurtherDamage()
        {
            _system.MarkDead(_target, _source.Id);

            DamageResult result = Deal(100f, AbilityDamageTypes.Pure);

            Assert.That(result.Negated, Is.True);
            Assert.That(result.HealthDamage, Is.EqualTo(0f));
        }

        [Test]
        public void NonPositiveDamage_IsNegated()
        {
            Assert.That(Deal(0f, AbilityDamageTypes.Pure).Negated, Is.True);
            Assert.That(Deal(-25f, AbilityDamageTypes.Pure).Negated, Is.True);
            Assert.That(_target.Health, Is.EqualTo(1000f), "damage never restores health");
        }

        [Test]
        public void OutgoingAndIncomingMultipliers_StackOntoDamage()
        {
            _source.SetBaseProperty(AbilityProperties.OutgoingDamageMul, 2f);
            _target.SetBaseProperty(AbilityProperties.IncomingDamageMul, 1.5f);

            DamageResult result = Deal(10f, AbilityDamageTypes.Pure); // 10 * 2 * 1.5

            Assert.That(result.HealthDamage, Is.EqualTo(30f).Within(0.01f));
        }
    }
}
