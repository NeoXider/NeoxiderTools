using System.Collections;
using System.Collections.Generic;
using Neo.Tools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public sealed class Audit20261023NeoToolsPhysicsRegressionTests
    {
        private const int TargetLayer = 31;
        private const float Tolerance = 0.001f;
        private static readonly bool[] BooleanCases = { false, true };
        private static readonly string[] RejectedFilters = { "Layer", "Tag" };
        private GameObject root;
        private bool ownsPlayMode;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // EnterPlayMode itself refuses an existing Play session; set ownership only after it succeeds.
            // Code before a reload instruction may be replayed when the runner restores its enumerator.
            ownsPlayMode = false;
            yield return new EnterPlayMode();
            ownsPlayMode = true;
            Assert.That(Application.isPlaying, Is.True, "Runtime callbacks require admitted Play Mode.");
            // Own only this hierarchy; never simulate/reset a foreign scene or change global physics settings.
            root = new GameObject(nameof(Audit20261023NeoToolsPhysicsRegressionTests));
            root.transform.position = new Vector3(18100f, 19700f, 22300f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }

            if (ownsPlayMode && Application.isPlaying)
            {
                yield return new ExitPlayMode();
            }

            ownsPlayMode = false;
        }

        [UnityTest]
        public IEnumerator DelayedActivation_WithZeroDelay_DetonatesOnStartOnce()
        {
            ExplosiveForce explosion = CreateExplosion(ExplosiveForce.ForceMode.AddForce,
                ExplosiveForce.FalloffType.Linear);
            explosion.gameObject.SetActive(false);
            SerializedObject settings = new(explosion);
            settings.FindProperty("activationMode").enumValueIndex = (int)ExplosiveForce.ActivationMode.Delayed;
            settings.ApplyModifiedPropertiesWithoutUndo();
            Rigidbody target = CreateBody("Target", Vector3.right * 2f);
            int detonations = 0;
            explosion.OnExplode.AddListener(() => detonations++);
            Physics.SyncTransforms();
            AssertNativePhysicsPreconditions();

            explosion.gameObject.SetActive(true);
            yield return WaitForNativeCondition(() => detonations > 0, "Native Start did not detonate the emitter.");
            yield return WaitForNativeCondition(() => target.linearVelocity.x > 0f,
                "The automatic explosion did not produce a native physics hit.");
            explosion.Explode();
            yield return WaitForNativePhysicsAfter(Time.fixedTimeAsDouble);

            Assert.That(explosion.HasExploded, Is.True);
            Assert.That(detonations, Is.EqualTo(1));
            AssertVector(target.linearVelocity * target.mass, Vector3.right * 10f);
        }

        [TestCase(ExplosiveForce.ForceMode.AddForce, ExplosiveForce.FalloffType.Linear, 10f)]
        [TestCase(ExplosiveForce.ForceMode.AddForce, ExplosiveForce.FalloffType.Quadratic, 15f)]
        [TestCase(ExplosiveForce.ForceMode.AddExplosionForce, ExplosiveForce.FalloffType.Linear, 10f)]
        [TestCase(ExplosiveForce.ForceMode.AddExplosionForce, ExplosiveForce.FalloffType.Quadratic, 15f)]
        public void Explosion_AppliesSelectedFalloffExactlyOnce(ExplosiveForce.ForceMode forceMode,
            ExplosiveForce.FalloffType falloff, float expectedImpulse)
        {
            ExplosiveForce explosion = CreateExplosion(forceMode, falloff);
            Rigidbody target = CreateBody("Target", Vector3.right * 2f);
            Physics.SyncTransforms();

            explosion.Explode();

            AssertVector(AccumulatedImpulse(target), Vector3.right * expectedImpulse);
        }

        [Test]
        public void Explosion_CompoundBody_ReceivesOneImpulseAndOneNotification()
        {
            ExplosiveForce explosion = CreateExplosion(ExplosiveForce.ForceMode.AddForce,
                ExplosiveForce.FalloffType.Linear);
            Rigidbody target = CreateBody("Target", Vector3.right * 2f);
            target.gameObject.AddComponent<SphereCollider>();
            CreateChildCollider(target, "Child collider");
            int notifications = 0;
            explosion.OnObjectAffected.AddListener((GameObject affected) => notifications++);
            Physics.SyncTransforms();

            explosion.Explode();

            Assert.That(notifications, Is.EqualTo(1));
            AssertVector(AccumulatedImpulse(target), Vector3.right * 10f);
        }

        [Test]
        public void Explosion_ChildColliderOfEmitterBody_DoesNotPushItself()
        {
            ExplosiveForce explosion = CreateExplosion(ExplosiveForce.ForceMode.AddForce,
                ExplosiveForce.FalloffType.Linear);
            Rigidbody sourceBody = explosion.gameObject.AddComponent<Rigidbody>();
            sourceBody.useGravity = false;
            CreateChildCollider(sourceBody, "Emitter collider");
            int notifications = 0;
            explosion.OnObjectAffected.AddListener((GameObject affected) => notifications++);
            Physics.SyncTransforms();

            explosion.Explode();

            Assert.That(notifications, Is.Zero);
            AssertVector(AccumulatedImpulse(sourceBody), Vector3.zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Explosion_AffectedListenerDestroysEmitter_StopsAfterCommittedImpulse(bool destroyGameObject)
        {
            ExplosiveForce explosion = CreateExplosion(ExplosiveForce.ForceMode.AddExplosionForce,
                ExplosiveForce.FalloffType.Linear);
            GameObject emitterObject = explosion.gameObject;
            Rigidbody first = CreateBody("First", Vector3.right * 2f);
            Rigidbody second = CreateBody("Second", Vector3.left * 2f);
            GameObject affectedObject = null;
            int affectedEvents = 0;
            int completions = 0;
            explosion.OnObjectAffected.AddListener((GameObject affected) =>
            {
                affectedEvents++;
                affectedObject = affected;
                if (destroyGameObject)
                {
                    Object.DestroyImmediate(emitterObject);
                }
                else
                {
                    Object.DestroyImmediate(explosion);
                }
            });
            explosion.OnExplode.AddListener(() => completions++);
            Physics.SyncTransforms();

            explosion.Explode();

            Assert.That(explosion == null, Is.True);
            Assert.That(emitterObject == null, Is.EqualTo(destroyGameObject));
            Assert.That(affectedEvents, Is.EqualTo(1));
            Assert.That(completions, Is.Zero);
            if (affectedObject == first.gameObject)
            {
                AssertVector(AccumulatedImpulse(first), Vector3.right * 10f);
                AssertVector(AccumulatedImpulse(second), Vector3.zero);
            }
            else
            {
                Assert.That(affectedObject, Is.EqualTo(second.gameObject));
                AssertVector(AccumulatedImpulse(first), Vector3.zero);
                AssertVector(AccumulatedImpulse(second), Vector3.left * 10f);
            }
        }

        [TestCase(false, 0f)]
        [TestCase(true, 0f)]
        [TestCase(false, 0.25f)]
        [TestCase(true, 0.25f)]
        public void Explosion_CompletionListenerDestroysEmitter_SkipsAutomaticCleanup(bool destroyGameObject,
            float destroyDelay)
        {
            ExplosiveForce explosion = CreateExplosion(ExplosiveForce.ForceMode.AddForce,
                ExplosiveForce.FalloffType.Linear);
            GameObject emitterObject = explosion.gameObject;
            SerializedObject settings = new(explosion);
            settings.FindProperty("destroyAfterExplosion").boolValue = true;
            settings.FindProperty("destroyDelay").floatValue = destroyDelay;
            settings.ApplyModifiedPropertiesWithoutUndo();
            Rigidbody first = CreateBody("First", Vector3.right * 2f);
            Rigidbody second = CreateBody("Second", Vector3.left * 2f);
            List<string> order = new();
            int affectedAtCompletion = -1;
            explosion.OnObjectAffected.AddListener((GameObject affected) => order.Add("affected"));
            explosion.OnExplode.AddListener(() =>
            {
                affectedAtCompletion = order.Count;
                order.Add("complete");
                if (destroyGameObject)
                {
                    Object.DestroyImmediate(emitterObject);
                }
                else
                {
                    Object.DestroyImmediate(explosion);
                }
            });
            Physics.SyncTransforms();

            explosion.Explode();

            Assert.That(explosion == null, Is.True);
            Assert.That(emitterObject == null, Is.EqualTo(destroyGameObject));
            Assert.That(affectedAtCompletion, Is.EqualTo(2));
            CollectionAssert.AreEqual(new[] { "affected", "affected", "complete" }, order);
            AssertVector(AccumulatedImpulse(first), Vector3.right * 10f);
            AssertVector(AccumulatedImpulse(second), Vector3.left * 10f);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Explosion_LiveExplicitCall_AffectsBothBodiesBeforeCompletion(bool disableComponent)
        {
            ExplosiveForce explosion = CreateExplosion(ExplosiveForce.ForceMode.AddForce,
                ExplosiveForce.FalloffType.Linear);
            Rigidbody first = CreateBody("First", Vector3.right * 2f);
            Rigidbody second = CreateBody("Second", Vector3.left * 2f);
            List<string> order = new();
            explosion.OnObjectAffected.AddListener((GameObject affected) => order.Add("affected"));
            explosion.OnExplode.AddListener(() => order.Add("complete"));
            explosion.enabled = !disableComponent;
            Physics.SyncTransforms();

            explosion.Explode();

            Assert.That(explosion == null, Is.False);
            Assert.That(explosion.enabled, Is.EqualTo(!disableComponent));
            Assert.That(explosion.HasExploded, Is.True);
            CollectionAssert.AreEqual(new[] { "affected", "affected", "complete" }, order);
            AssertVector(AccumulatedImpulse(first), Vector3.right * 10f);
            AssertVector(AccumulatedImpulse(second), Vector3.left * 10f);
        }

        [TestCase(true, 0f)]
        [TestCase(true, 10f)]
        [TestCase(false, 10f)]
        public void ManualImpulse_EnforcesOneShotOrCooldownAcrossCompoundColliders(bool oneShot, float cooldown)
        {
            ImpulseZone zone = CreateZone(oneShot, cooldown);
            Rigidbody target = CreateBody("Target", Vector3.right * 2f);
            Collider child = CreateChildCollider(target, "Child collider");
            int applications = 0;
            zone.OnImpulseApplied.AddListener((GameObject affected) => applications++);

            zone.ApplyImpulseToObject(target.gameObject);
            zone.ApplyImpulseToObject(child.gameObject);

            Assert.That(applications, Is.EqualTo(1));
            AssertVector(AccumulatedImpulse(target), Vector3.up * 12f);

            zone.ClearProcessedObjects();
            zone.ApplyImpulseToObject(child.gameObject);

            Assert.That(applications, Is.EqualTo(2));
            AssertVector(AccumulatedImpulse(target), Vector3.up * 24f);
        }

        [UnityTest]
        public IEnumerator CooldownMaintenance_PreservesBothIndependentCooldowns()
        {
            ImpulseZone zone = CreateZone(false, 10f);
            Rigidbody first = CreateBody("First", Vector3.right);
            Rigidbody second = CreateBody("Second", Vector3.left);
            int applications = 0;
            zone.OnImpulseApplied.AddListener((GameObject affected) => applications++);
            zone.ApplyImpulseToObject(first.gameObject);
            zone.ApplyImpulseToObject(second.gameObject);
            Assert.That(applications, Is.EqualTo(2), "Both independent targets must receive their first impulse.");
            AssertVector(AccumulatedImpulse(first), Vector3.up * 12f);
            AssertVector(AccumulatedImpulse(second), Vector3.up * 12f);

            int initialFrame = Time.frameCount;
            yield return WaitForNativePhysicsAfter(Time.fixedTimeAsDouble);
            yield return WaitForNativeCondition(() => Time.frameCount > initialFrame,
                "Cooldown maintenance requires a real native Update frame.");
            zone.ApplyImpulseToObject(first.gameObject);
            zone.ApplyImpulseToObject(second.gameObject);

            Assert.That(applications, Is.EqualTo(2));
            AssertVector(first.linearVelocity * first.mass, Vector3.up * 12f);
            AssertVector(second.linearVelocity * second.mass, Vector3.up * 12f);
        }

        [UnityTest]
        public IEnumerator TriggerCallback_ReentrantManualImpulse_DoesNotDoublePush()
        {
            ImpulseZone zone = CreateZone(false, 0f);
            Rigidbody target = CreateBody("Target", Vector3.right * 3f);
            AssertNativeTriggerPreconditions(zone, target);
            int entries = 0;
            int applications = 0;
            zone.OnObjectEntered.AddListener((GameObject entered) =>
            {
                Assert.That(entered, Is.EqualTo(target.gameObject));
                entries++;
                zone.ApplyImpulseToObject(entered);
            });
            zone.OnImpulseApplied.AddListener((GameObject affected) => applications++);

            target.position = zone.transform.position + Vector3.right * 0.5f;
            Physics.SyncTransforms();
            yield return WaitForNativeCondition(() => applications > 0,
                "The real trigger contact did not deliver its positive impulse.");
            yield return WaitForNativeCondition(() => target.linearVelocity.y > 0f,
                "The trigger impulse was not integrated by native physics.");

            Assert.That(entries, Is.EqualTo(1));
            Assert.That(applications, Is.EqualTo(1));
            AssertVector(target.linearVelocity * target.mass, Vector3.up * 12f);
        }

        [UnityTest]
        public IEnumerator NativeTrigger_RejectsLayerOrTagAfterPositiveContact(
            [ValueSource(nameof(RejectedFilters))] string rejectedFilter)
        {
            ImpulseZone zone = CreateZone(false, 0f);
            SerializedObject settings = new(zone);
            settings.FindProperty("requiredTag").stringValue = "Player";
            settings.ApplyModifiedPropertiesWithoutUndo();
            Rigidbody positive = CreateBody("Allowed", Vector3.right * 3f);
            positive.gameObject.tag = "Player";
            List<GameObject> entries = new();
            int applications = 0;
            zone.OnObjectEntered.AddListener((GameObject entered) => entries.Add(entered));
            zone.OnImpulseApplied.AddListener((GameObject affected) => applications++);
            AssertNativeTriggerPreconditions(zone, positive);
            positive.position = zone.transform.position + Vector3.right * 0.5f;
            Physics.SyncTransforms();

            yield return WaitForNativeCondition(() => positive.linearVelocity.y > 0f,
                "The allowed layer/tag target must receive a real native trigger impulse first.");

            Assert.That(applications, Is.EqualTo(1));
            CollectionAssert.AreEqual(new[] { positive.gameObject }, entries);
            AssertVector(positive.linearVelocity * positive.mass, Vector3.up * 12f);
            Object.DestroyImmediate(positive.gameObject);
            yield return WaitForNativePhysicsAfter(Time.fixedTimeAsDouble);

            Rigidbody rejected = CreateBody("Rejected", Vector3.right * 3f);
            rejected.gameObject.tag = rejectedFilter == "Tag" ? "Untagged" : "Player";
            if (rejectedFilter == "Layer")
            {
                rejected.gameObject.layer = 30;
            }

            AssertNativeTriggerPreconditions(zone, rejected);
            rejected.position = zone.transform.position + Vector3.right * 0.5f;
            Physics.SyncTransforms();
            yield return WaitForNativePhysicsAfter(Time.fixedTimeAsDouble);
            yield return WaitForNativePhysicsAfter(Time.fixedTimeAsDouble);

            Collider zoneCollider = zone.GetComponent<Collider>();
            Collider rejectedCollider = rejected.GetComponent<Collider>();
            bool overlaps = Physics.ComputePenetration(zoneCollider, zoneCollider.transform.position,
                zoneCollider.transform.rotation, rejectedCollider, rejectedCollider.transform.position,
                rejectedCollider.transform.rotation, out Vector3 separationDirection, out float separationDistance);
            Assert.That(overlaps, Is.True, "The rejected target must actually overlap the live trigger.");
            Assert.That(separationDirection.sqrMagnitude, Is.GreaterThan(0f));
            Assert.That(separationDistance, Is.GreaterThan(0f));
            Assert.That(entries.Count, Is.EqualTo(1));
            Assert.That(applications, Is.EqualTo(1));
            AssertVector(rejected.linearVelocity, Vector3.zero);
        }

        [UnityTest]
        public IEnumerator OneShot_PersistsAfterObservedCooldownExpiryUntilExplicitReset()
        {
            ImpulseZone oneShot = CreateZone(true, 0.1f);
            ImpulseZone repeatable = CreateZone(false, 0.1f);
            Rigidbody first = CreateBody("One shot", Vector3.right * 3f);
            Rigidbody second = CreateBody("Repeatable", Vector3.left * 3f);
            int oneShotApplications = 0;
            int repeatableApplications = 0;
            oneShot.OnImpulseApplied.AddListener((GameObject affected) => oneShotApplications++);
            repeatable.OnImpulseApplied.AddListener((GameObject affected) => repeatableApplications++);
            oneShot.ApplyImpulseToObject(first.gameObject);
            repeatable.ApplyImpulseToObject(second.gameObject);
            Assert.That(oneShotApplications, Is.EqualTo(1));
            Assert.That(repeatableApplications, Is.EqualTo(1));
            double expiry = Time.timeAsDouble + 0.1d;

            yield return WaitForNativeCondition(() => Time.timeAsDouble >= expiry,
                "Scaled time did not advance past the configured cooldown.");
            yield return WaitForNativePhysicsAfter(Time.fixedTimeAsDouble);
            AssertVector(first.linearVelocity * first.mass, Vector3.up * 12f);
            AssertVector(second.linearVelocity * second.mass, Vector3.up * 12f);

            oneShot.ApplyImpulseToObject(first.gameObject);
            repeatable.ApplyImpulseToObject(second.gameObject);
            Assert.That(repeatableApplications, Is.EqualTo(2), "Expiry must be observed by the repeatable control.");
            Assert.That(oneShotApplications, Is.EqualTo(1));
            AssertVector(AccumulatedImpulse(first), Vector3.zero);
            AssertVector(AccumulatedImpulse(second), Vector3.up * 12f);

            oneShot.ClearProcessedObjects();
            oneShot.ApplyImpulseToObject(first.gameObject);
            Assert.That(oneShotApplications, Is.EqualTo(2));
            AssertVector(AccumulatedImpulse(first), Vector3.up * 12f);
            yield return WaitForNativePhysicsAfter(Time.fixedTimeAsDouble);
            AssertVector(first.linearVelocity * first.mass, Vector3.up * 24f);
            AssertVector(second.linearVelocity * second.mass, Vector3.up * 24f);
        }

        [Test]
        public void TriggerCallback_DestroyedBody_DoesNotReportAnAppliedImpulse()
        {
            // Admitted synchronous callback-logic driver: immediate destruction is outside a PhysX contact step.
            // Real contact delivery/filtering/reentry is exercised independently by the coroutine cases.
            ImpulseZone zone = CreateZone(false, 0f);
            Rigidbody target = CreateBody("Target", Vector3.right);
            int entries = 0;
            int applications = 0;
            zone.OnObjectEntered.AddListener((GameObject entered) =>
            {
                entries++;
                Object.DestroyImmediate(target);
            });
            zone.OnImpulseApplied.AddListener((GameObject affected) => applications++);

            zone.SendMessage("OnTriggerEnter", target.GetComponent<Collider>());

            Assert.That(entries, Is.EqualTo(1));
            Assert.That(target == null, Is.True);
            Assert.That(applications, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TriggerCallback_DestroyedEmitter_DoesNotApplyImpulse(bool destroyGameObject)
        {
            ImpulseZone zone = CreateZone(false, 0f);
            GameObject zoneObject = zone.gameObject;
            Rigidbody target = CreateBody("Target", Vector3.right);
            Collider targetCollider = target.GetComponent<Collider>();
            int entries = 0;
            int applications = 0;
            zone.OnObjectEntered.AddListener((GameObject entered) =>
            {
                entries++;
                if (destroyGameObject)
                {
                    Object.DestroyImmediate(zoneObject);
                }
                else
                {
                    Object.DestroyImmediate(zone);
                }
            });
            zone.OnImpulseApplied.AddListener((GameObject affected) => applications++);

            zone.SendMessage("OnTriggerEnter", targetCollider);

            Assert.That(entries, Is.EqualTo(1));
            Assert.That(zone == null, Is.True);
            Assert.That(target == null, Is.False);
            Assert.That(targetCollider == null, Is.False);
            Assert.That(applications, Is.Zero);
            AssertVector(AccumulatedImpulse(target), Vector3.zero);
        }

        [Test]
        public void ManualImpulse_DisabledLiveEmitter_RemainsSupported()
        {
            ImpulseZone zone = CreateZone(false, 0f);
            Rigidbody target = CreateBody("Target", Vector3.right);
            int applications = 0;
            zone.OnImpulseApplied.AddListener((GameObject affected) => applications++);
            zone.enabled = false;

            zone.ApplyImpulseToObject(target.gameObject);

            Assert.That(zone == null, Is.False);
            Assert.That(zone.enabled, Is.False);
            Assert.That(applications, Is.EqualTo(1));
            AssertVector(AccumulatedImpulse(target), Vector3.up * 12f);
        }

        [UnityTest]
        public IEnumerator MagneticField_CompoundBody_ReceivesOneForceButTracksEachColliderObject()
        {
            MagneticField field = CreateField();
            Rigidbody target = CreateBody("Target", Vector3.right * 2f);
            target.gameObject.AddComponent<SphereCollider>();
            CreateChildCollider(target, "Child collider");
            int entries = 0;
            field.OnObjectEntered.AddListener((GameObject entered) => entries++);
            Physics.SyncTransforms();
            double initialPhysicsTime = Time.fixedTimeAsDouble;

            yield return WaitForNativePhysicsAfter(initialPhysicsTime);

            Assert.That(entries, Is.EqualTo(2));
            Assert.That(field.ObjectsInFieldCount, Is.EqualTo(2));
            AssertNativeForce(target, Vector3.zero, initialPhysicsTime, Vector3.right * 10f);
        }

        [UnityTest]
        public IEnumerator MagneticField_ReplacedRigidbody_ReceivesForceWithoutLeavingField()
        {
            MagneticField field = CreateField();
            Rigidbody target = CreateBody("Target", Vector3.right * 2f);
            GameObject targetObject = target.gameObject;
            Physics.SyncTransforms();
            double initialPhysicsTime = Time.fixedTimeAsDouble;
            yield return WaitForNativePhysicsAfter(initialPhysicsTime);
            AssertNativeForce(target, Vector3.zero, initialPhysicsTime, Vector3.right * 10f);
            Object.DestroyImmediate(target);
            Rigidbody replacement = targetObject.AddComponent<Rigidbody>();
            replacement.useGravity = false;
            replacement.linearDamping = 0f;
            Physics.SyncTransforms();
            double replacementPhysicsTime = Time.fixedTimeAsDouble;

            yield return WaitForNativePhysicsAfter(replacementPhysicsTime);

            Assert.That(field.ObjectsInFieldCount, Is.EqualTo(1));
            AssertNativeForce(replacement, Vector3.zero, replacementPhysicsTime, Vector3.right * 10f);
        }

        [UnityTest]
        public IEnumerator MagneticField_ChildColliderOfEmitterBody_IsExcluded()
        {
            MagneticField field = CreateField();
            Rigidbody sourceBody = field.gameObject.AddComponent<Rigidbody>();
            sourceBody.useGravity = false;
            CreateChildCollider(sourceBody, "Emitter collider");
            Rigidbody positive = CreateBody("Independent positive", Vector3.right * 2f);
            int entries = 0;
            int sourceEntries = 0;
            field.OnObjectEntered.AddListener((GameObject entered) =>
            {
                entries++;
                if (entered.GetComponent<Collider>().attachedRigidbody == sourceBody)
                {
                    sourceEntries++;
                }
            });
            Physics.SyncTransforms();
            double initialPhysicsTime = Time.fixedTimeAsDouble;

            yield return WaitForNativePhysicsAfter(initialPhysicsTime);

            Assert.That(entries, Is.EqualTo(1));
            Assert.That(sourceEntries, Is.Zero);
            Assert.That(field.ObjectsInFieldCount, Is.EqualTo(1));
            AssertVector(sourceBody.linearVelocity, Vector3.zero);
            AssertNativeForce(positive, Vector3.zero, initialPhysicsTime, Vector3.right * 10f);
        }

        [UnityTest]
        public IEnumerator MagneticField_ReparentedCollider_UsesItsNewBody()
        {
            MagneticField field = CreateField();
            Rigidbody first = CreateBody("First", Vector3.right);
            Object.DestroyImmediate(first.GetComponent<Collider>());
            Rigidbody second = CreateBody("Second", Vector3.right * 2f);
            Object.DestroyImmediate(second.GetComponent<Collider>());
            Collider child = CreateChildCollider(first, "Moving collider");
            Physics.SyncTransforms();
            double initialPhysicsTime = Time.fixedTimeAsDouble;
            yield return WaitForNativePhysicsAfter(initialPhysicsTime);
            AssertNativeForce(first, Vector3.zero, initialPhysicsTime, Vector3.right * 10f);
            AssertVector(second.linearVelocity, Vector3.zero);
            Vector3 firstVelocity = first.linearVelocity;
            child.transform.SetParent(second.transform, false);
            Physics.SyncTransforms();
            double transferredPhysicsTime = Time.fixedTimeAsDouble;

            yield return WaitForNativePhysicsAfter(transferredPhysicsTime);

            AssertVector(first.linearVelocity, firstVelocity);
            AssertNativeForce(second, Vector3.zero, transferredPhysicsTime, Vector3.right * 10f);
            Assert.That(field.ObjectsInFieldCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator MagneticField_DisableClearsMembershipAndReenableReportsFreshEntry()
        {
            MagneticField field = CreateField();
            Rigidbody target = CreateBody("Target", Vector3.right * 2f);
            int entries = 0;
            int exits = 0;
            field.OnObjectEntered.AddListener((GameObject entered) => entries++);
            field.OnObjectExited.AddListener((GameObject exited) => exits++);
            Physics.SyncTransforms();
            double initialPhysicsTime = Time.fixedTimeAsDouble;
            yield return WaitForNativePhysicsAfter(initialPhysicsTime);
            Assert.That(field.ObjectsInFieldCount, Is.EqualTo(1));
            Assert.That(entries, Is.EqualTo(1));
            AssertNativeForce(target, Vector3.zero, initialPhysicsTime, Vector3.right * 10f);

            field.enabled = false;
            Assert.That(field.ObjectsInFieldCount, Is.Zero);
            Assert.That(exits, Is.EqualTo(1));
            Vector3 disabledVelocity = target.linearVelocity;
            yield return WaitForNativePhysicsAfter(Time.fixedTimeAsDouble);
            AssertVector(target.linearVelocity, disabledVelocity);
            field.enabled = true;
            yield return WaitForNativePhysicsAfter(Time.fixedTimeAsDouble);
            Assert.That(entries, Is.EqualTo(2));
            Assert.That(field.ObjectsInFieldCount, Is.EqualTo(1));
            Assert.That(exits, Is.EqualTo(1));
            Assert.That(target.linearVelocity.x, Is.GreaterThan(disabledVelocity.x));
        }

        [UnityTest]
        public IEnumerator MagneticField_DisabledByEntryListener_StopsBeforeApplyingForce()
        {
            MagneticField field = CreateField();
            Rigidbody target = CreateBody("Target", Vector3.right * 2f);
            int entries = 0;
            int exits = 0;
            field.OnObjectEntered.AddListener((GameObject entered) =>
            {
                entries++;
                field.enabled = false;
            });
            field.OnObjectExited.AddListener((GameObject exited) => exits++);
            Physics.SyncTransforms();

            yield return WaitForNativePhysicsAfter(Time.fixedTimeAsDouble);

            Assert.That(entries, Is.EqualTo(1));
            Assert.That(field.ObjectsInFieldCount, Is.Zero);
            Assert.That(exits, Is.EqualTo(1));
            AssertVector(target.linearVelocity, Vector3.zero);
        }

        [UnityTest]
        public IEnumerator MagneticField_DisabledByExitListener_EmitsRemainingExitsOnce()
        {
            MagneticField field = CreateField();
            Rigidbody leaving = CreateBody("Leaving", Vector3.right);
            Rigidbody remaining = CreateBody("Remaining", Vector3.right * 2f);
            Physics.SyncTransforms();
            double initialPhysicsTime = Time.fixedTimeAsDouble;
            yield return WaitForNativePhysicsAfter(initialPhysicsTime);
            Assert.That(field.ObjectsInFieldCount, Is.EqualTo(2));
            AssertNativeForce(leaving, Vector3.zero, initialPhysicsTime, Vector3.right * 10f);
            AssertNativeForce(remaining, Vector3.zero, initialPhysicsTime, Vector3.right * 10f);
            int exits = 0;
            field.OnObjectExited.AddListener((GameObject exited) =>
            {
                exits++;
                if (field.enabled)
                {
                    field.enabled = false;
                }
            });
            leaving.transform.position = root.transform.position + Vector3.right * 10f;
            Physics.SyncTransforms();

            yield return WaitForNativePhysicsAfter(Time.fixedTimeAsDouble);

            Assert.That(field.ObjectsInFieldCount, Is.Zero);
            Assert.That(exits, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator MagneticField_EntryListenerDestroysEmitter_StopsBeforeAnyForce(
            [ValueSource(nameof(BooleanCases))] bool destroyGameObject)
        {
            MagneticField field = CreateField();
            GameObject emitterObject = field.gameObject;
            Rigidbody first = CreateBody("First", Vector3.right * 2f);
            Rigidbody second = CreateBody("Second", Vector3.left * 2f);
            int entries = 0;
            field.OnObjectEntered.AddListener((GameObject entered) =>
            {
                entries++;
                if (destroyGameObject)
                {
                    Object.DestroyImmediate(emitterObject);
                }
                else
                {
                    Object.DestroyImmediate(field);
                }
            });
            Physics.SyncTransforms();

            yield return WaitForNativePhysicsAfter(Time.fixedTimeAsDouble);

            Assert.That(field == null, Is.True);
            Assert.That(emitterObject == null, Is.EqualTo(destroyGameObject));
            Assert.That(entries, Is.EqualTo(1));
            AssertVector(first.linearVelocity, Vector3.zero);
            AssertVector(second.linearVelocity, Vector3.zero);
        }

        [UnityTest]
        public IEnumerator MagneticField_ModeListenerDestroysEmitter_DoesNotDiscoverOrPush(
            [ValueSource(nameof(BooleanCases))] bool destroyGameObject,
            [ValueSource(nameof(BooleanCases))] bool useFixedUpdate)
        {
            MagneticField field = CreateField();
            GameObject emitterObject = field.gameObject;
            Rigidbody first = CreateBody("First", Vector3.right * 2f);
            Rigidbody second = CreateBody("Second", Vector3.left * 2f);
            SerializedObject settings = new(field);
            settings.FindProperty("_toggle").boolValue = true;
            settings.FindProperty("useFixedUpdate").boolValue = useFixedUpdate;
            // Valid query throttling isolates the first timed mode callback from any earlier force pass.
            settings.FindProperty("updateInterval").floatValue = Time.time + 10f;
            settings.ApplyModifiedPropertiesWithoutUndo();
            field.SetAttractDuration(0.1f);
            field.SetRepelDuration(0.1f);
            field.ResetToggleTimer();
            int entries = 0;
            int modeChanges = 0;
            bool reportedForwardPhase = true;
            field.OnObjectEntered.AddListener((GameObject entered) => entries++);
            field.OnModeChanged.AddListener((bool forward) =>
            {
                modeChanges++;
                reportedForwardPhase = forward;
                if (destroyGameObject)
                {
                    Object.DestroyImmediate(emitterObject);
                }
                else
                {
                    Object.DestroyImmediate(field);
                }
            });
            Physics.SyncTransforms();

            yield return WaitForNativeCondition(() => modeChanges > 0,
                "The selected native update path did not deliver its timed mode callback.");
            yield return WaitForNativePhysicsAfter(Time.fixedTimeAsDouble);

            Assert.That(field == null, Is.True);
            Assert.That(emitterObject == null, Is.EqualTo(destroyGameObject));
            Assert.That(modeChanges, Is.EqualTo(1));
            Assert.That(reportedForwardPhase, Is.False);
            Assert.That(entries, Is.Zero);
            AssertVector(first.linearVelocity, Vector3.zero);
            AssertVector(second.linearVelocity, Vector3.zero);
        }

        [UnityTest]
        public IEnumerator MagneticField_OrdinaryCycle_PushesBothBodiesWithoutRepeatingEntries()
        {
            MagneticField field = CreateField();
            Rigidbody first = CreateBody("First", Vector3.right * 2f);
            Rigidbody second = CreateBody("Second", Vector3.left * 2f);
            int entries = 0;
            field.OnObjectEntered.AddListener((GameObject entered) => entries++);
            Physics.SyncTransforms();
            double initialPhysicsTime = Time.fixedTimeAsDouble;

            yield return WaitForNativePhysicsAfter(initialPhysicsTime);

            Assert.That(entries, Is.EqualTo(2));
            Assert.That(field.ObjectsInFieldCount, Is.EqualTo(2));
            AssertNativeForce(first, Vector3.zero, initialPhysicsTime, Vector3.right * 10f);
            AssertNativeForce(second, Vector3.zero, initialPhysicsTime, Vector3.right * 10f);
            Vector3 firstVelocity = first.linearVelocity;
            Vector3 secondVelocity = second.linearVelocity;
            double firstPassPhysicsTime = Time.fixedTimeAsDouble;

            yield return WaitForNativePhysicsAfter(firstPassPhysicsTime);

            Assert.That(entries, Is.EqualTo(2));
            Assert.That(field.ObjectsInFieldCount, Is.EqualTo(2));
            AssertNativeForce(first, firstVelocity, firstPassPhysicsTime, Vector3.right * 10f);
            AssertNativeForce(second, secondVelocity, firstPassPhysicsTime, Vector3.right * 10f);
            AssertNativeForce(first, Vector3.zero, initialPhysicsTime, Vector3.right * 10f);
            AssertNativeForce(second, Vector3.zero, initialPhysicsTime, Vector3.right * 10f);
        }

        private GameObject CreateObject(string name, Vector3 offset)
        {
            GameObject created = new(name);
            created.layer = TargetLayer;
            created.transform.SetParent(root.transform, false);
            created.transform.localPosition = offset;
            return created;
        }

        private Rigidbody CreateBody(string name, Vector3 offset)
        {
            GameObject target = CreateObject(name, offset);
            target.AddComponent<BoxCollider>();
            Rigidbody body = target.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.linearDamping = 0f;
            return body;
        }

        private static Collider CreateChildCollider(Rigidbody body, string name)
        {
            GameObject child = new(name);
            child.layer = TargetLayer;
            child.transform.SetParent(body.transform, false);
            return child.AddComponent<BoxCollider>();
        }

        private ExplosiveForce CreateExplosion(ExplosiveForce.ForceMode forceMode,
            ExplosiveForce.FalloffType falloff)
        {
            ExplosiveForce explosion = CreateObject("Explosion", Vector3.zero).AddComponent<ExplosiveForce>();
            SerializedObject settings = new(explosion);
            settings.FindProperty("activationMode").enumValueIndex = (int)ExplosiveForce.ActivationMode.Manual;
            settings.FindProperty("forceMode").enumValueIndex = (int)forceMode;
            settings.FindProperty("falloffType").enumValueIndex = (int)falloff;
            settings.FindProperty("affectedLayers").intValue = 1 << TargetLayer;
            settings.ApplyModifiedPropertiesWithoutUndo();
            explosion.SetForce(20f);
            explosion.SetRadius(4f);
            return explosion;
        }

        private ImpulseZone CreateZone(bool oneShot, float cooldown)
        {
            GameObject zoneObject = CreateObject("Impulse zone", Vector3.zero);
            zoneObject.AddComponent<BoxCollider>().isTrigger = true;
            ImpulseZone zone = zoneObject.AddComponent<ImpulseZone>();
            SerializedObject settings = new(zone);
            settings.FindProperty("oneTimeOnly").boolValue = oneShot;
            settings.FindProperty("cooldown").floatValue = cooldown;
            settings.FindProperty("direction").enumValueIndex = (int)ImpulseZone.ImpulseDirection.Custom;
            settings.FindProperty("customDirection").vector3Value = Vector3.up;
            settings.FindProperty("affectedLayers").intValue = 1 << TargetLayer;
            settings.ApplyModifiedPropertiesWithoutUndo();
            zone.SetImpulseForce(12f);
            return zone;
        }

        private MagneticField CreateField()
        {
            MagneticField field = CreateObject("Magnetic field", Vector3.zero).AddComponent<MagneticField>();
            SerializedObject settings = new(field);
            settings.FindProperty("falloffType").enumValueIndex = (int)MagneticField.FalloffType.Constant;
            settings.FindProperty("affectedLayers").intValue = 1 << TargetLayer;
            settings.ApplyModifiedPropertiesWithoutUndo();
            field.SetDirection(Vector3.right, false);
            field.SetRadius(4f);
            field.SetStrength(10f);
            return field;
        }

        private static Vector3 AccumulatedImpulse(Rigidbody body)
        {
            return body.GetAccumulatedForce() * Time.fixedDeltaTime;
        }

        private static IEnumerator WaitForNativeCondition(System.Func<bool> condition, string failure)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 5d;
            while (!condition())
            {
                Assert.That(Time.realtimeSinceStartupAsDouble, Is.LessThan(deadline), failure);
                yield return null;
            }
        }

        private static IEnumerator WaitForNativePhysicsAfter(double initialPhysicsTime)
        {
            AssertNativePhysicsPreconditions();
            yield return WaitForNativeCondition(() => Time.fixedTimeAsDouble > initialPhysicsTime,
                "No native physics step was observed within the real-time bound.");
        }

        private static void AssertNativePhysicsPreconditions()
        {
            Assert.That(Application.isPlaying, Is.True);
            Assert.That(Time.timeScale, Is.GreaterThan(0f), "This native physics scenario requires scaled time.");
            Assert.That(Physics.simulationMode, Is.EqualTo(SimulationMode.FixedUpdate),
                "The validation host must admit automatic fixed-step physics; this fixture does not reset it.");
        }

        private static void AssertNativeForce(Rigidbody body, Vector3 initialVelocity, double initialPhysicsTime,
            Vector3 expectedForce)
        {
            double elapsed = Time.fixedTimeAsDouble - initialPhysicsTime;
            Assert.That(elapsed, Is.GreaterThan(0d));
            Vector3 observedForce = (body.linearVelocity - initialVelocity) * (body.mass / (float)elapsed);
            AssertVector(observedForce, expectedForce);
        }

        private static void AssertNativeTriggerPreconditions(ImpulseZone zone, Rigidbody target)
        {
            AssertNativePhysicsPreconditions();
            Assert.That(zone.GetComponent<Collider>().isTrigger, Is.True);
            Assert.That(target.GetComponent<Collider>().enabled, Is.True);
            Assert.That(target.isKinematic, Is.False);
            Assert.That(Physics.GetIgnoreLayerCollision(zone.gameObject.layer, target.gameObject.layer), Is.False,
                "The validation host must allow the owned colliders to contact.");
        }

        private static void AssertVector(Vector3 actual, Vector3 expected)
        {
            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(Tolerance),
                $"Expected {expected}, received {actual}.");
        }
    }
}
