using Neo.Abilities;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Abilities
{
    /// <summary>
    ///     Cast parameters by ability level: cooldown, range, charges, charge restore time and projectile speed are
    ///     sampled at the slot level, a running cooldown keeps its remaining time when the level changes, and a charge
    ///     cap change grants or clamps charges.
    /// </summary>
    public sealed class LeveledCastParameterTests
    {
        private AbilitySystem _system;
        private FakeWorldAdapter _world;
        private AbilityUnit _caster;
        private AbilityUnit _enemy;

        [SetUp]
        public void SetUp()
        {
            _system = new AbilitySystem();
            _world = new FakeWorldAdapter();
            _system.World = _world;
            _caster = AbilityTestSupport.CreateUnit(_system, team: 1);
            _enemy = AbilityTestSupport.CreateUnit(_system, team: 2);
            _world.SetPosition(_caster.Id, Vector3.zero);
            _world.SetPosition(_enemy.Id, new Vector3(7f, 0f, 0f));
        }

        private static AbilityBlueprint Ability(string id, TargetingMode targeting = TargetingMode.NoTarget)
        {
            AbilityBlueprint ability = new AbilityBlueprint { Id = id, Targeting = targeting };
            ability.ImpactEffects.Add(AbilityTestSupport.HealNode(0f));
            return ability;
        }

        private AbilitySlot Grant(AbilityBlueprint ability)
        {
            _system.RegisterAbility(ability);
            return _system.GrantAbility(_caster.Id, ability.Id);
        }

        private bool Cast(string abilityId)
        {
            return _system.Cast(CastRequest.NoTarget(_caster.Id, abilityId)).Success;
        }

        [Test]
        public void CooldownByLevel_StartsTheCooldownOfTheSlotLevel()
        {
            AbilityBlueprint ability = Ability("lv_cd");
            ability.Cooldown = 10f;
            ability.CooldownByLevel = new[] { 10f, 8f, 6f };
            AbilitySlot slot = Grant(ability);

            int[] levels = { 1, 2, 3, 7 };
            float[] expected = { 10f, 8f, 6f, 6f };
            for (int i = 0; i < levels.Length; i++)
            {
                _system.SetAbilityLevel(_caster.Id, "lv_cd", levels[i]);
                Assert.That(Cast("lv_cd"), Is.True, "level " + levels[i] + " casts");
                Assert.That(slot.CooldownRemaining, Is.EqualTo(expected[i]).Within(1e-4f), "level " + levels[i]);
                _system.Tick(20f);
            }
        }

        [Test]
        public void CooldownReduction_ScalesTheLeveledCooldown()
        {
            AbilityBlueprint ability = Ability("lv_cdr");
            ability.CooldownByLevel = new[] { 10f, 8f };
            AbilitySlot slot = Grant(ability);
            _caster.SetBaseProperty(AbilityProperties.CooldownReductionPercent, 50f);
            _system.SetAbilityLevel(_caster.Id, "lv_cdr", 2);

            Assert.That(Cast("lv_cdr"), Is.True);
            Assert.That(slot.CooldownRemaining, Is.EqualTo(4f).Within(1e-4f), "8 s at level 2, halved by 50% cdr");
        }

        [Test]
        public void RunningCooldown_KeepsItsRemainingTime_WhenTheLevelChanges()
        {
            AbilityBlueprint ability = Ability("lv_cd_keep");
            ability.CooldownByLevel = new[] { 10f, 8f, 6f };
            AbilitySlot slot = Grant(ability);

            Assert.That(Cast("lv_cd_keep"), Is.True);
            _system.Tick(3f);
            _system.SetAbilityLevel(_caster.Id, "lv_cd_keep", 3);
            Assert.That(slot.CooldownRemaining, Is.EqualTo(7f).Within(1e-4f), "the running cooldown is not rescaled");
            Assert.That(slot.IsReady, Is.False);

            _system.Tick(7f);
            Assert.That(slot.IsReady, Is.True);
            Assert.That(Cast("lv_cd_keep"), Is.True);
            Assert.That(slot.CooldownRemaining, Is.EqualTo(6f).Within(1e-4f), "the next cast uses the new level");
        }

        [Test]
        public void RangeByLevel_GatesUnitCasts_AndTheSlotReportsIt()
        {
            AbilityBlueprint ability = Ability("lv_range", TargetingMode.Unit);
            ability.Range = 5f;
            ability.RangeByLevel = new[] { 5f, 8f };
            AbilitySlot slot = Grant(ability);

            CastResult far = _system.Cast(CastRequest.AtUnit(_caster.Id, "lv_range", _enemy.Id));
            Assert.That(far.Failure, Is.EqualTo(CastFailureReason.OutOfRange), "7 units away, range 5 at level 1");

            _system.SetAbilityLevel(_caster.Id, "lv_range", 2);
            Assert.That(slot.Range, Is.EqualTo(8f));
            Assert.That(_system.Cast(CastRequest.AtUnit(_caster.Id, "lv_range", _enemy.Id)).Success, Is.True,
                "range 8 at level 2");
        }

        [Test]
        public void DirectionReach_UsesTheLeveledRange()
        {
            AbilityBlueprint ability = Ability("lv_dir", TargetingMode.Direction);
            ability.Delivery = AbilityDeliveryType.Projectile;
            ability.ProjectileArchetypeId = "bolt";
            ability.Range = 4f;
            ability.RangeByLevel = new[] { 4f, 9f };
            Grant(ability);
            _system.SetAbilityLevel(_caster.Id, "lv_dir", 2);

            Assert.That(_system.Cast(CastRequest.Towards(_caster.Id, "lv_dir", Vector3.right)).Success, Is.True);
            Assert.That(_world.Spawns[0].Direction.magnitude, Is.EqualTo(9f).Within(1e-4f));
        }

        [Test]
        public void MaxChargesByLevel_RaisedCapGrantsCharges_LoweredCapClamps()
        {
            AbilityBlueprint ability = Ability("lv_charges");
            ability.Cooldown = 5f;
            ability.ChargeRestoreTime = 4f;
            ability.MaxChargesByLevel = new[] { 1, 3, 2 };
            AbilitySlot slot = Grant(ability);
            Assert.That(slot.UsesCharges, Is.False, "one charge at level 1 is the classic cooldown");
            Assert.That(slot.Charges, Is.EqualTo(1));

            _system.SetAbilityLevel(_caster.Id, "lv_charges", 2);
            Assert.That(slot.MaxCharges, Is.EqualTo(3));
            Assert.That(slot.Charges, Is.EqualTo(3), "the two added charges arrive at once");

            Assert.That(Cast("lv_charges"), Is.True);
            Assert.That(Cast("lv_charges"), Is.True);
            Assert.That(slot.Charges, Is.EqualTo(1));
            Assert.That(slot.CooldownRemaining, Is.EqualTo(4f).Within(1e-4f), "the charge restore time");

            _system.SetAbilityLevel(_caster.Id, "lv_charges", 3);
            Assert.That(slot.Charges, Is.EqualTo(1), "below the lowered cap of 2: unchanged");
            Assert.That(slot.CooldownRemaining, Is.EqualTo(4f).Within(1e-4f), "the restore keeps running");

            _system.Tick(4f);
            Assert.That(slot.Charges, Is.EqualTo(2));
            Assert.That(slot.CooldownRemaining, Is.EqualTo(0f), "full at the new cap");

            Assert.That(Cast("lv_charges"), Is.True);
            _system.SetAbilityLevel(_caster.Id, "lv_charges", 1);
            Assert.That(slot.Charges, Is.EqualTo(1), "clamped to the single charge");
            Assert.That(slot.CooldownRemaining, Is.EqualTo(0f), "nothing left to restore stops the timer");
            Assert.That(slot.IsReady, Is.True);
        }

        [Test]
        public void ChargeRestoreTimeByLevel_TimesTheRestore_AndZeroFallsBackToTheLevelCooldown()
        {
            AbilityBlueprint ability = Ability("lv_restore");
            ability.MaxCharges = 2;
            ability.ChargeRestoreTime = 4f;
            ability.ChargeRestoreTimeByLevel = new[] { 4f, 2f, 0f };
            ability.CooldownByLevel = new[] { 9f, 9f, 3f };
            AbilitySlot slot = Grant(ability);

            _system.SetAbilityLevel(_caster.Id, "lv_restore", 2);
            Assert.That(Cast("lv_restore"), Is.True);
            Assert.That(slot.CooldownRemaining, Is.EqualTo(2f).Within(1e-4f));
            _system.Tick(10f);

            _system.SetAbilityLevel(_caster.Id, "lv_restore", 3);
            Assert.That(Cast("lv_restore"), Is.True);
            Assert.That(slot.CooldownRemaining, Is.EqualTo(3f).Within(1e-4f), "restore 0 uses the level-3 cooldown");
            Assert.That(ability.ResolveChargeRestoreTime(3), Is.EqualTo(3f));
        }

        [Test]
        public void ProjectileSpeedByLevel_ReachesTheSpawnRequest()
        {
            AbilityBlueprint ability = Ability("lv_speed", TargetingMode.Unit);
            ability.Delivery = AbilityDeliveryType.Projectile;
            ability.ProjectileArchetypeId = "bolt";
            ability.ProjectileSpeed = 20f;
            ability.ProjectileSpeedByLevel = new[] { 20f, 35f };
            Grant(ability);
            _system.SetAbilityLevel(_caster.Id, "lv_speed", 2);

            Assert.That(_system.Cast(CastRequest.AtUnit(_caster.Id, "lv_speed", _enemy.Id)).Success, Is.True);
            Assert.That(_world.Spawns[0].Magnitude, Is.EqualTo(35f));
        }

        [Test]
        public void BlueprintResolvers_FallBackToTheFlatFields_AndClampTheLevel()
        {
            AbilityBlueprint flat = new AbilityBlueprint
            {
                Range = 6f, Cooldown = 3f, MaxCharges = 2, ChargeRestoreTime = 1.5f, ProjectileSpeed = 12f
            };
            AbilityBlueprint leveled = new AbilityBlueprint
            {
                RangeByLevel = new[] { 1f, 2f },
                CooldownByLevel = new[] { 5f, 4f },
                MaxChargesByLevel = new[] { 1, 4 },
                ChargeRestoreTimeByLevel = new[] { 2f, 1f },
                ProjectileSpeedByLevel = new[] { 10f, 30f }
            };

            for (int level = 0; level <= 4; level++)
            {
                Assert.That(flat.ResolveRange(level), Is.EqualTo(6f));
                Assert.That(flat.ResolveCooldown(level), Is.EqualTo(3f));
                Assert.That(flat.ResolveMaxCharges(level), Is.EqualTo(2));
                Assert.That(flat.ResolveChargeRestoreTime(level), Is.EqualTo(1.5f));
                Assert.That(flat.ResolveProjectileSpeed(level), Is.EqualTo(12f));
            }

            Assert.That(leveled.ResolveRange(0), Is.EqualTo(1f), "below 1 clamps to the first entry");
            Assert.That(leveled.ResolveCooldown(9), Is.EqualTo(4f), "above the array clamps to the last entry");
            Assert.That(leveled.UsesChargesAt(1), Is.False);
            Assert.That(leveled.UsesChargesAt(2), Is.True);
            Assert.That(leveled.ResolveChargeRestoreTime(2), Is.EqualTo(1f));
            Assert.That(leveled.ResolveProjectileSpeed(2), Is.EqualTo(30f));
        }
    }
}
