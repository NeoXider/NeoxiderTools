using System.Collections;
using Neo.Tools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

#pragma warning disable CS0618
namespace Neo.Editor.Tests
{
    [TestFixture]
    public sealed class Audit20261023NeoRpgCombatHitFilterTests
    {
        private GameObject _source;
        private GameObject _target;
        private Health _ownerHealth;
        private Component _targetCollider;
        private int _hits;
        private int _triggerEvents;
        private GameObject _lastHitTarget;
        private Component _lastTriggerCollider;
        private bool _ownsPlayMode;
        private bool _hasTimeScaleSnapshot;
        private float _previousTimeScale;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Assert.That(Application.isPlaying, Is.False, "This EditMode fixture must own its Play Mode transition.");
            yield return new EnterPlayMode();
            _ownsPlayMode = true;
            Assert.That(Application.isPlaying, Is.True);
            // Let the host's ordinary Start callbacks settle; do not reset its scenes or providers.
            yield return null;
            _previousTimeScale = Time.timeScale;
            _hasTimeScaleSnapshot = true;
            Time.timeScale = 1f;
            yield return WaitForNativePhysicsSteps();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            try
            {
                if (_source != null)
                {
                    Object.DestroyImmediate(_source);
                    _source = null;
                }

                if (_target != null)
                {
                    Object.DestroyImmediate(_target);
                    _target = null;
                }
            }
            finally
            {
                if (_hasTimeScaleSnapshot)
                {
                    Time.timeScale = _previousTimeScale;
                    _hasTimeScaleSnapshot = false;
                }
            }

            if (_ownsPlayMode && Application.isPlaying)
            {
                _ownsPlayMode = false;
                yield return new ExitPlayMode();
            }
        }

        [UnityTest]
        public IEnumerator IgnoredTrigger3D_DoesNotNotifyOrConsumeRepeatHitAllowance()
        {
            yield return VerifyIgnoredTrigger(false);
        }

        [UnityTest]
        public IEnumerator IgnoredTrigger2D_DoesNotNotifyOrConsumeRepeatHitAllowance()
        {
            yield return VerifyIgnoredTrigger(true);
        }

        private AdvancedAttackCollider CreateActors(bool use2D, bool preventRepeatHits)
        {
            Vector3 origin = new(8192f, 8192f, 0f);
            _source = new GameObject("CombatHitSource");
            _source.SetActive(false);
            _source.transform.position = origin + Vector3.right * 4f;
            _target = new GameObject("CombatHitTarget");
            _target.transform.position = origin;
            if (use2D)
            {
                BoxCollider2D sourceCollider = _source.AddComponent<BoxCollider2D>();
                sourceCollider.isTrigger = true;
                Rigidbody2D body = _source.AddComponent<Rigidbody2D>();
                body.gravityScale = 0f;
                body.constraints = RigidbodyConstraints2D.FreezeAll;
                _targetCollider = _target.AddComponent<BoxCollider2D>();
            }
            else
            {
                BoxCollider sourceCollider = _source.AddComponent<BoxCollider>();
                sourceCollider.isTrigger = true;
                Rigidbody body = _source.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;
                _targetCollider = _target.AddComponent<BoxCollider>();
            }

            _ownerHealth = _source.AddComponent<Health>();
            Health targetHealth = _target.AddComponent<Health>();
            AdvancedAttackCollider attack = _source.AddComponent<AdvancedAttackCollider>();
            attack.use2D = use2D;
            attack.use3D = !use2D;
            attack.OnHit = new UnityEvent<GameObject>();
            attack.OnHit.AddListener(target =>
            {
                _hits++;
                _lastHitTarget = target;
            });
            if (use2D)
            {
                attack.OnAttackTriggerEnter2D = new UnityEvent<Collider2D>();
                attack.OnAttackTriggerEnter2D.AddListener(collider =>
                {
                    _triggerEvents++;
                    _lastTriggerCollider = collider;
                });
            }
            else
            {
                attack.OnAttackTriggerEnter3D = new UnityEvent<Collider>();
                attack.OnAttackTriggerEnter3D.AddListener(collider =>
                {
                    _triggerEvents++;
                    _lastTriggerCollider = collider;
                });
            }

            using (SerializedObject settings = new(attack))
            {
                settings.FindProperty("preventRepeatHits").boolValue = preventRepeatHits;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }

            _source.SetActive(true);
            targetHealth.SetMaxHp(10, true);
            _ownerHealth.SetMaxHp(10, true);
            attack.ActivateTrigger(3);
            ClearObservedHits();
            Assert.That(attack.isActiveAndEnabled, Is.True);
            Assert.That(_source.activeInHierarchy, Is.True);
            Assert.That(_target.activeInHierarchy, Is.True);
            return attack;
        }

        private IEnumerator VerifyIgnoredTrigger(bool use2D)
        {
            AdvancedAttackCollider attack = CreateActors(use2D, true);
            Health health = _target.GetComponent<Health>();
            // Establish actual native damage and payload delivery before any negative inference.
            MoveHitbox(use2D, true);
            yield return WaitForNativePhysicsSteps();
            AssertAcceptedHit(health);
            MoveHitbox(use2D, false);
            yield return WaitForNativePhysicsSteps();
            health.Restore();
            attack.ActivateTrigger(3);
            ClearObservedHits();

            using (SerializedObject settings = new(attack))
            {
                SerializedProperty ignored = settings.FindProperty("ignoreObjects");
                ignored.arraySize = 1;
                ignored.GetArrayElementAtIndex(0).objectReferenceValue = _target;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }

            MoveHitbox(use2D, true);
            yield return WaitForNativePhysicsSteps();
            Assert.That(health.HpValue, Is.EqualTo(10));
            Assert.That(_ownerHealth.HpValue, Is.EqualTo(10));
            Assert.That(_hits, Is.Zero);
            Assert.That(_triggerEvents, Is.Zero);
            MoveHitbox(use2D, false);
            yield return WaitForNativePhysicsSteps();
            using (SerializedObject settings = new(attack))
            {
                settings.FindProperty("ignoreObjects").arraySize = 0;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }

            // Same swing: removing the ignore entry must not leave a poisoned repeat-hit cache.
            MoveHitbox(use2D, true);
            yield return WaitForNativePhysicsSteps();
            AssertAcceptedHit(health);
            MoveHitbox(use2D, false);
            yield return WaitForNativePhysicsSteps();
            MoveHitbox(use2D, true);
            yield return WaitForNativePhysicsSteps();
            AssertAcceptedHit(health);
        }

        [UnityTest]
        public IEnumerator DisabledAttack_DoesNotDamageFromTriggerCallback()
        {
            AdvancedAttackCollider attack = CreateActors(false, false);
            Health health = _target.GetComponent<Health>();
            MoveHitbox(false, true);
            yield return WaitForNativePhysicsSteps();
            AssertAcceptedHit(health);
            MoveHitbox(false, false);
            yield return WaitForNativePhysicsSteps();
            health.Restore();
            attack.ActivateTrigger(3);
            ClearObservedHits();
            attack.enabled = false;
            MoveHitbox(false, true);
            yield return WaitForNativePhysicsSteps();

            Assert.That(health.HpValue, Is.EqualTo(10));
            Assert.That(_ownerHealth.HpValue, Is.EqualTo(10));
            Assert.That(_hits, Is.Zero);
            Assert.That(_triggerEvents, Is.Zero);
            MoveHitbox(false, false);
            yield return WaitForNativePhysicsSteps();
            attack.enabled = true;
            MoveHitbox(false, true);
            yield return WaitForNativePhysicsSteps();
            AssertAcceptedHit(health);
        }

        private void MoveHitbox(bool use2D, bool overlap)
        {
            Vector3 position = _target.transform.position + (overlap ? Vector3.zero : Vector3.right * 4f);
            if (use2D)
            {
                Rigidbody2D body = _source.GetComponent<Rigidbody2D>();
                body.position = position;
                body.WakeUp();
                Physics2D.SyncTransforms();
            }
            else
            {
                _source.GetComponent<Rigidbody>().position = position;
                Physics.SyncTransforms();
            }
        }

        private static IEnumerator WaitForNativePhysicsSteps()
        {
            Assert.That(Application.isPlaying, Is.True, "Native physics requires admitted Play Mode.");
            Assert.That(Time.fixedDeltaTime, Is.GreaterThan(0f));
            double targetFixedTime = Time.fixedTimeAsDouble + 3d * Time.fixedDeltaTime;
            double deadline = Time.realtimeSinceStartupAsDouble + 3d;
            while (Time.fixedTimeAsDouble < targetFixedTime && Time.realtimeSinceStartupAsDouble < deadline)
            {
                Assert.That(Time.timeScale, Is.EqualTo(1f), "The fixture's captured/restored interval control was changed.");
                yield return null;
            }

            Assert.That(Time.fixedTimeAsDouble, Is.GreaterThanOrEqualTo(targetFixedTime),
                "The native fixed-step scheduler must advance before interpreting contact results.");
        }

        private void AssertAcceptedHit(Health health)
        {
            Assert.That(health.HpValue, Is.EqualTo(7));
            Assert.That(_ownerHealth.HpValue, Is.EqualTo(10));
            Assert.That(_hits, Is.EqualTo(1));
            Assert.That(_triggerEvents, Is.EqualTo(1));
            Assert.That(_lastHitTarget, Is.SameAs(_target));
            Assert.That(_lastTriggerCollider, Is.SameAs(_targetCollider));
        }

        private void ClearObservedHits()
        {
            _hits = 0;
            _triggerEvents = 0;
            _lastHitTarget = null;
            _lastTriggerCollider = null;
        }
    }
}
#pragma warning restore CS0618
