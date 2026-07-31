using System.Reflection;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;

#pragma warning disable CS0618
namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Covers two attack-system contracts: an evade interrupted by a disable must release its state,
    ///     and knockback must reach the Rigidbody when the target has no AdvancedForceApplier.
    /// </summary>
    [TestFixture]
    public class AuditFixesAttackSystemTests
    {
        private GameObject _go;
        private GameObject _target;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("AuditFixesAttackSystem");
        }

        [TearDown]
        public void TearDown()
        {
            if (_target != null)
            {
                Object.DestroyImmediate(_target);
                _target = null;
            }

            if (_go != null)
            {
                Object.DestroyImmediate(_go);
                _go = null;
            }
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"{target.GetType().Name}.{methodName} must exist");
            method.Invoke(target, null);
        }

        private static void ApplyForceToTarget(AdvancedAttackCollider attack, GameObject target, Vector3 direction)
        {
            MethodInfo method = typeof(AdvancedAttackCollider).GetMethod(
                "ApplyForceToTarget", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "AdvancedAttackCollider.ApplyForceToTarget must exist");
            method.Invoke(attack, new object[] { target, direction });
        }

        [Test]
        public void Evade_DisabledMidEvade_StaysUsable()
        {
            Evade evade = _go.AddComponent<Evade>();
            evade.evadeDuration = 5f;

            Assert.That(evade.TryStartEvade(), Is.True, "the first evade must start");
            Assert.That(evade.IsEvading, Is.True);

            InvokePrivate(evade, "OnDisable");

            Assert.That(evade.IsEvading, Is.False, "a disabled evade must not stay active forever");
            Assert.That(evade.CanEvade, Is.True, "the ability must be available again after the interruption");
            Assert.That(evade.TryStartEvade(), Is.True, "evade must be startable after a disable");
        }

        [Test]
        public void Evade_DisabledMidEvade_DoesNotRaiseCompletedEvent()
        {
            Evade evade = _go.AddComponent<Evade>();
            evade.evadeDuration = 5f;

            // WHY: AddComponent does not run Unity's serialization pass, so every UnityEvent field on a
            // component created in code is null until something assigns it.
            evade.OnEvadeCompleted = new UnityEvent();

            int completedCount = 0;
            evade.OnEvadeCompleted.AddListener(() => completedCount++);

            evade.TryStartEvade();
            InvokePrivate(evade, "OnDisable");

            Assert.That(completedCount, Is.Zero, "an interrupted evade is not a completed evade");
        }

        [Test]
        public void AdvancedAttackCollider_TargetWithoutForceApplier_FallsBackToRigidbody()
        {
            AdvancedAttackCollider attack = _go.AddComponent<AdvancedAttackCollider>();
            attack.useAdvancedForceApplier = true;
            attack.scaleForceByMass = false;
            attack.forceMagnitude = 25f;

            _target = new GameObject("KnockbackTarget");
            Rigidbody2D body = _target.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;

            // WHY: Edit mode runs no physics step; when impulses are not applied to the body immediately
            // the fallback cannot be observed here and stays covered by play mode usage.
            body.AddForce(Vector2.right * 10f, ForceMode2D.Impulse);
            if (body.linearVelocity.sqrMagnitude <= 0f)
            {
                Assert.Ignore("2D impulses are not applied outside play mode in this Unity version.");
            }

            body.linearVelocity = Vector2.zero;

            ApplyForceToTarget(attack, _target, Vector3.right);

            Assert.That(body.linearVelocity.sqrMagnitude, Is.GreaterThan(0f),
                "knockback must fall through to the Rigidbody when the target has no AdvancedForceApplier");
        }

        [Test]
        public void AdvancedAttackCollider_TargetWithForceApplier_DelegatesToApplier()
        {
            AdvancedAttackCollider attack = _go.AddComponent<AdvancedAttackCollider>();
            attack.useAdvancedForceApplier = true;

            _target = new GameObject("ApplierTarget");
            AdvancedForceApplier applier = _target.AddComponent<AdvancedForceApplier>();
            applier.OnApplyFailed = new UnityEvent();

            int applyFailedCount = 0;
            applier.OnApplyFailed.AddListener(() => applyFailedCount++);

            ApplyForceToTarget(attack, _target, Vector3.right);

            // WHY: The target has no Rigidbody, so the applier reports a failed apply - proof that the
            // applier path handled the hit instead of the Rigidbody fallback.
            Assert.That(applyFailedCount, Is.EqualTo(1), "an existing AdvancedForceApplier must own the knockback");
        }
    }
}
#pragma warning restore CS0618
