using Neo.Abilities;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Abilities
{
    /// <summary>
    ///     The motion family (knockback / pull / teleport) over the <see cref="IAbilityWorldAdapter.TryMoveUnit" />
    ///     seam: displacement math, the caster/point origin, the never-past-the-caster pull clamp, teleport
    ///     variants (blink / yank / swap), Unmovable/Invulnerable immunity and headless (null adapter) safety.
    /// </summary>
    public sealed class MotionOpTests
    {
        private AbilitySystem _system;
        private FakeWorldAdapter _world;
        private AbilityUnit _caster;

        [SetUp]
        public void SetUp()
        {
            _system = new AbilitySystem();
            _world = new FakeWorldAdapter();
            _system.World = _world;
            _caster = AbilityTestSupport.CreateUnit(_system, team: 1);
        }

        private static void AssertAt(Vector3 actual, Vector3 expected)
        {
            Assert.That((actual - expected).magnitude, Is.LessThan(0.001f),
                $"expected {expected} but was {actual}");
        }

        private EffectNodeData Node(string op, float amount = 0f, string custom = null,
            EffectTargetSelector target = EffectTargetSelector.Target)
        {
            return new EffectNodeData { OpId = op, Target = target, Amount = amount, CustomParam = custom };
        }

        // ---------------------------------------------------------------- knockback

        [Test]
        public void Knockback_PushesTargetAwayFromCaster()
        {
            AbilityUnit target = AbilityTestSupport.CreateUnit(_system, team: 2);
            _world.SetPosition(_caster.Id, Vector3.zero);
            _world.SetPosition(target.Id, new Vector3(2f, 0f, 0f));

            EffectContext ctx = AbilityTestSupport.Context(_system, _caster.Id, targets: target.Id);
            _system.ExecuteEffects(AbilityTestSupport.Nodes(Node(AbilityEffectOps.Knockback, 3f)), ctx);

            AssertAt(_world.Positions[target.Id], new Vector3(5f, 0f, 0f)); // pushed 3 further out along +x
        }

        [Test]
        public void Knockback_UsesTargetPointOriginWhenPresent()
        {
            AbilityUnit target = AbilityTestSupport.CreateUnit(_system, team: 2);
            _world.SetPosition(_caster.Id, new Vector3(50f, 50f, 0f)); // caster far away — ignored
            _world.SetPosition(target.Id, new Vector3(2f, 0f, 0f));

            EffectContext ctx = AbilityTestSupport.Context(_system, _caster.Id, targets: target.Id);
            ctx.HasTargetPoint = true;
            ctx.TargetPoint = new Vector3(1f, 0f, 0f);

            _system.ExecuteEffects(AbilityTestSupport.Nodes(Node(AbilityEffectOps.Knockback, 3f)), ctx);

            AssertAt(_world.Positions[target.Id], new Vector3(5f, 0f, 0f)); // away from the point, not the caster
        }

        // ---------------------------------------------------------------- pull

        [Test]
        public void Pull_DragsTargetTowardCaster()
        {
            AbilityUnit target = AbilityTestSupport.CreateUnit(_system, team: 2);
            _world.SetPosition(_caster.Id, Vector3.zero);
            _world.SetPosition(target.Id, new Vector3(5f, 0f, 0f));

            EffectContext ctx = AbilityTestSupport.Context(_system, _caster.Id, targets: target.Id);
            _system.ExecuteEffects(AbilityTestSupport.Nodes(Node(AbilityEffectOps.Pull, 2f)), ctx);

            AssertAt(_world.Positions[target.Id], new Vector3(3f, 0f, 0f));
        }

        [Test]
        public void Pull_NeverOvershootsPastTheCaster()
        {
            AbilityUnit target = AbilityTestSupport.CreateUnit(_system, team: 2);
            _world.SetPosition(_caster.Id, Vector3.zero);
            _world.SetPosition(target.Id, new Vector3(5f, 0f, 0f));

            EffectContext ctx = AbilityTestSupport.Context(_system, _caster.Id, targets: target.Id);
            _system.ExecuteEffects(AbilityTestSupport.Nodes(Node(AbilityEffectOps.Pull, 10f)), ctx);

            AssertAt(_world.Positions[target.Id], Vector3.zero); // clamped exactly to the caster
        }

        // ---------------------------------------------------------------- teleport

        [Test]
        public void Teleport_Caster_BlinksToTargetPoint()
        {
            _world.SetPosition(_caster.Id, Vector3.zero);
            EffectContext ctx = AbilityTestSupport.Context(_system, _caster.Id);
            ctx.HasTargetPoint = true;
            ctx.TargetPoint = new Vector3(7f, 7f, 0f);

            _system.ExecuteEffects(
                AbilityTestSupport.Nodes(Node(AbilityEffectOps.Teleport, target: EffectTargetSelector.Caster)), ctx);

            AssertAt(_world.Positions[_caster.Id], new Vector3(7f, 7f, 0f));
        }

        [Test]
        public void Teleport_Caster_BlinksToFirstTargetPositionWithoutPoint()
        {
            AbilityUnit anchor = AbilityTestSupport.CreateUnit(_system, team: 2);
            _world.SetPosition(_caster.Id, Vector3.zero);
            _world.SetPosition(anchor.Id, new Vector3(4f, 0f, 0f));

            EffectContext ctx = AbilityTestSupport.Context(_system, _caster.Id, targets: anchor.Id);
            _system.ExecuteEffects(
                AbilityTestSupport.Nodes(Node(AbilityEffectOps.Teleport, target: EffectTargetSelector.Caster)), ctx);

            AssertAt(_world.Positions[_caster.Id], new Vector3(4f, 0f, 0f));
        }

        [Test]
        public void Teleport_Target_YanksTargetToCaster()
        {
            AbilityUnit target = AbilityTestSupport.CreateUnit(_system, team: 2);
            _world.SetPosition(_caster.Id, new Vector3(1f, 1f, 0f));
            _world.SetPosition(target.Id, new Vector3(5f, 0f, 0f));

            EffectContext ctx = AbilityTestSupport.Context(_system, _caster.Id, targets: target.Id);
            _system.ExecuteEffects(AbilityTestSupport.Nodes(Node(AbilityEffectOps.Teleport)), ctx);

            AssertAt(_world.Positions[target.Id], new Vector3(1f, 1f, 0f));
        }

        [Test]
        public void Teleport_Swap_ExchangesCasterAndFirstTarget()
        {
            AbilityUnit target = AbilityTestSupport.CreateUnit(_system, team: 2);
            _world.SetPosition(_caster.Id, Vector3.zero);
            _world.SetPosition(target.Id, new Vector3(9f, 0f, 0f));

            EffectContext ctx = AbilityTestSupport.Context(_system, _caster.Id, targets: target.Id);
            _system.ExecuteEffects(
                AbilityTestSupport.Nodes(Node(AbilityEffectOps.Teleport, custom: "swap")), ctx);

            AssertAt(_world.Positions[_caster.Id], new Vector3(9f, 0f, 0f));
            AssertAt(_world.Positions[target.Id], Vector3.zero);
        }

        // ---------------------------------------------------------------- immunity

        [Test]
        public void Motion_SkipsUnmovableTargets()
        {
            AbilityUnit target = AbilityTestSupport.CreateUnit(_system, team: 2);
            target.SetPermanentState(AbilityStates.Unmovable, true);
            _world.SetPosition(_caster.Id, Vector3.zero);
            _world.SetPosition(target.Id, new Vector3(2f, 0f, 0f));

            EffectContext ctx = AbilityTestSupport.Context(_system, _caster.Id, targets: target.Id);
            _system.ExecuteEffects(AbilityTestSupport.Nodes(Node(AbilityEffectOps.Knockback, 3f)), ctx);

            AssertAt(_world.Positions[target.Id], new Vector3(2f, 0f, 0f)); // unmoved
        }

        [Test]
        public void Motion_SkipsInvulnerableTargets()
        {
            AbilityUnit target = AbilityTestSupport.CreateUnit(_system, team: 2);
            target.SetPermanentState(AbilityStates.Invulnerable, true);
            _world.SetPosition(_caster.Id, Vector3.zero);
            _world.SetPosition(target.Id, new Vector3(5f, 0f, 0f));

            EffectContext ctx = AbilityTestSupport.Context(_system, _caster.Id, targets: target.Id);
            _system.ExecuteEffects(AbilityTestSupport.Nodes(Node(AbilityEffectOps.Pull, 2f)), ctx);

            AssertAt(_world.Positions[target.Id], new Vector3(5f, 0f, 0f)); // unmoved
        }

        [Test]
        public void Teleport_Target_RespectsImmunity()
        {
            AbilityUnit target = AbilityTestSupport.CreateUnit(_system, team: 2);
            target.SetPermanentState(AbilityStates.Unmovable, true);
            _world.SetPosition(_caster.Id, new Vector3(1f, 1f, 0f));
            _world.SetPosition(target.Id, new Vector3(5f, 0f, 0f));

            EffectContext ctx = AbilityTestSupport.Context(_system, _caster.Id, targets: target.Id);
            _system.ExecuteEffects(AbilityTestSupport.Nodes(Node(AbilityEffectOps.Teleport)), ctx);

            AssertAt(_world.Positions[target.Id], new Vector3(5f, 0f, 0f)); // unmoved
        }

        // ---------------------------------------------------------------- headless safety

        [Test]
        public void Motion_WithNullWorldAdapter_IsNoOpAndSafe()
        {
            var headless = new AbilitySystem(); // default NullWorldAdapter — no positions, TryMoveUnit false
            AbilityUnit caster = AbilityTestSupport.CreateUnit(headless, team: 1);
            AbilityUnit target = AbilityTestSupport.CreateUnit(headless, team: 2);
            EffectContext ctx = AbilityTestSupport.Context(headless, caster.Id, targets: target.Id);

            Assert.DoesNotThrow(() =>
            {
                headless.ExecuteEffects(AbilityTestSupport.Nodes(Node(AbilityEffectOps.Knockback, 3f)), ctx);
                headless.ExecuteEffects(AbilityTestSupport.Nodes(Node(AbilityEffectOps.Pull, 3f)), ctx);
                headless.ExecuteEffects(AbilityTestSupport.Nodes(Node(AbilityEffectOps.Teleport, custom: "swap")), ctx);
            });
        }
    }
}
