using Neo.Abilities;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor.Tests.Abilities
{
    /// <summary>
    ///     AbilityAutoCaster cast passes (nearest-enemy lock-on, cooldown gating, silent waiting,
    ///     failure events) and AbilityCooldownSource binding values, driven without a play loop.
    /// </summary>
    public sealed class AbilityAutoCastAndCooldownTests
    {
        private AbilitySceneRig _rig;
        private AbilityUnitBehaviour _player;
        private AbilityCasterBehaviour _caster;
        private AbilityAutoCaster _autoCaster;

        private int _castCount;
        private int _failedCount;
        private string _lastCastId;

        [SetUp]
        public void SetUp()
        {
            _rig = new AbilitySceneRig();
            _player = _rig.AddUnit("player", team: 1, Vector3.zero);
            _caster = _player.gameObject.AddComponent<AbilityCasterBehaviour>();
            _autoCaster = _player.gameObject.AddComponent<AbilityAutoCaster>();

            _castCount = 0;
            _failedCount = 0;
            _lastCastId = null;
            _autoCaster.OnCast.AddListener(id =>
            {
                _castCount++;
                _lastCastId = id;
            });
            _autoCaster.OnCastFailed.AddListener(_ => _failedCount++);
        }

        [TearDown]
        public void TearDown()
        {
            _rig.Dispose();
        }

        private void GrantZap(float range = 10f)
        {
            var zap = new AbilityBlueprint
            {
                Id = "zap",
                Targeting = TargetingMode.Unit,
                TeamFilter = AbilityTeamFilter.Enemies,
                Cooldown = 5f,
                Range = range
            };
            zap.ImpactEffects.Add(AbilityTestSupport.DamageNode(10f));
            _rig.System.RegisterAbility(zap);
            _rig.System.GrantAbility(_player.UnitId, "zap");
        }

        [Test]
        public void CastPass_UnitAbility_LocksOntoNearestEnemy()
        {
            GrantZap();
            AbilityUnitBehaviour far = _rig.AddUnit("far", team: 2, new Vector3(6f, 0f, 0f));
            AbilityUnitBehaviour near = _rig.AddUnit("near", team: 2, new Vector3(3f, 0f, 0f));

            _autoCaster.CastReadyAbilities();

            Assert.That(_castCount, Is.EqualTo(1));
            Assert.That(_lastCastId, Is.EqualTo("zap"));
            Assert.That(near.CurrentHealth, Is.EqualTo(90f).Within(0.001f), "nearest enemy takes the hit");
            Assert.That(far.CurrentHealth, Is.EqualTo(100f).Within(0.001f));
        }

        [Test]
        public void CastPass_OnCooldown_DoesNotRecast()
        {
            GrantZap();
            AbilityUnitBehaviour enemy = _rig.AddUnit("enemy", team: 2, new Vector3(3f, 0f, 0f));

            _autoCaster.CastReadyAbilities();
            _autoCaster.CastReadyAbilities();

            Assert.That(_castCount, Is.EqualTo(1));
            Assert.That(enemy.CurrentHealth, Is.EqualTo(90f).Within(0.001f));
        }

        [Test]
        public void CastPass_ManagerObjectAutoCaster_SearchesAroundCasterUnit()
        {
            GrantZap(range: 10f);
            AbilityUnitBehaviour enemy = _rig.AddUnit("enemy", team: 2, new Vector3(3f, 0f, 0f));

            // WHY: regression — the component sits on a far-away manager object with a serialized
            // caster reference; target search must center on the caster unit, not the manager.
            GameObject manager = _rig.Own(new GameObject("manager"));
            manager.transform.position = new Vector3(500f, 0f, 0f);
            var remote = manager.AddComponent<AbilityAutoCaster>();
            var serialized = new SerializedObject(remote);
            serialized.FindProperty("_caster").objectReferenceValue = _caster;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            remote.CastReadyAbilities();

            Assert.That(enemy.CurrentHealth, Is.EqualTo(90f).Within(0.001f),
                "enemy near the caster unit takes the hit");
            _rig.System.TryGetSlot(_player.UnitId, "zap", out AbilitySlot slot);
            Assert.That(slot.IsReady, Is.False, "the cast went through the referenced caster");
        }

        [Test]
        public void CastPass_NoTargetInRange_WaitsSilently()
        {
            GrantZap(range: 10f);
            _rig.AddUnit("distant", team: 2, new Vector3(50f, 0f, 0f));

            _autoCaster.CastReadyAbilities();

            Assert.That(_castCount, Is.Zero);
            Assert.That(_failedCount, Is.Zero);
            _rig.System.TryGetSlot(_player.UnitId, "zap", out AbilitySlot slot);
            Assert.That(slot.IsReady, Is.True, "cooldown must not be spent while waiting for a target");
        }

        [Test]
        public void CastPass_NoTargetAbility_CastsWithoutEnemies()
        {
            var nova = new AbilityBlueprint { Id = "nova", Targeting = TargetingMode.NoTarget, Cooldown = 2f };
            _rig.System.RegisterAbility(nova);
            _rig.System.GrantAbility(_player.UnitId, "nova");

            _autoCaster.CastReadyAbilities();

            Assert.That(_castCount, Is.EqualTo(1));
            Assert.That(_lastCastId, Is.EqualTo("nova"));
        }

        [Test]
        public void CastPass_FailedCast_FiresOnCastFailed_AndBacksOff()
        {
            var costly = new AbilityBlueprint
            {
                Id = "costly",
                Targeting = TargetingMode.NoTarget,
                Costs = { new AbilityCost(AbilityResourceIds.Mana, 10f) }
            };
            _rig.System.RegisterAbility(costly);
            _rig.System.GrantAbility(_player.UnitId, "costly");

            _autoCaster.CastReadyAbilities();
            // WHY: same-frame Time.time is unchanged, so the retry delay must swallow the second pass.
            _autoCaster.CastReadyAbilities();

            Assert.That(_castCount, Is.Zero);
            Assert.That(_failedCount, Is.EqualTo(1));
        }

        [Test]
        public void CooldownSource_UnknownId_ReadsReady()
        {
            var source = _player.gameObject.AddComponent<AbilityCooldownSource>();
            source.AbilityId = "missing";

            Assert.That(source.CooldownNormalized, Is.Zero);
            Assert.That(source.ReadyNormalized, Is.EqualTo(1f));
            Assert.That(source.SecondsRemaining, Is.Zero);
            Assert.That(source.IsReady, Is.False, "unknown id is never castable");
        }

        [Test]
        public void CooldownSource_AfterCast_TracksCooldown()
        {
            GrantZap();
            _rig.AddUnit("enemy", team: 2, new Vector3(3f, 0f, 0f));
            var source = _player.gameObject.AddComponent<AbilityCooldownSource>();
            source.AbilityId = "zap";

            Assert.That(source.IsReady, Is.True);
            _autoCaster.CastReadyAbilities();

            Assert.That(source.CooldownNormalized, Is.EqualTo(1f).Within(0.001f));
            Assert.That(source.SecondsRemaining, Is.EqualTo(5f).Within(0.001f));
            Assert.That(source.IsReady, Is.False);

            _rig.System.Tick(2.5f);
            Assert.That(source.CooldownNormalized, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(source.SecondsRemaining, Is.EqualTo(2.5f).Within(0.001f));
        }
    }
}
