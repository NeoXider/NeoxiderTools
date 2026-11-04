using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Neo.Tests.Performance;
using Neo.Tools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Overlap buffer, re-entrancy and runtime-configuration contracts of the Tools/Physics emitters, and the
    ///     allocation claims of their warm paths (repeated explosions, field updates, manual impulses).
    /// </summary>
    [TestFixture]
    public sealed class Audit20261102NeoToolsPhysicsQueryBufferTests
    {
        private const int TargetLayer = 31;
        private const float Tolerance = 0.001f;
        private GameObject root;
        private bool ownsPlayMode;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            ownsPlayMode = false;
            yield return new EnterPlayMode();
            ownsPlayMode = true;
            Assert.That(Application.isPlaying, Is.True, "Runtime physics queries require admitted Play Mode.");
            root = new GameObject(nameof(Audit20261102NeoToolsPhysicsQueryBufferTests));
            root.transform.position = new Vector3(-24100f, 18700f, -21300f);
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

        [Test]
        public void MagneticField_MoreCollidersThanTheBuffer_TracksEveryObjectWithoutChurn()
        {
            MagneticField field = CreateObject("Magnetic field", Vector3.zero).AddComponent<MagneticField>();
            SerializedObject settings = new(field);
            settings.FindProperty("falloffType").enumValueIndex = (int)MagneticField.FalloffType.Constant;
            settings.FindProperty("_maxOverlapColliders").intValue = 2;
            settings.ApplyModifiedPropertiesWithoutUndo();
            field.AffectedLayers = 1 << TargetLayer;
            field.SetDirection(Vector3.right, false);
            field.SetRadius(6f);
            field.SetStrength(10f);
            List<Rigidbody> bodies = new();
            for (int i = 0; i < 7; i++)
            {
                bodies.Add(CreateBody("Body " + i, new Vector3(0f, i * 0.6f - 2f, 1f)));
            }

            int entries = 0;
            int exits = 0;
            field.OnObjectEntered.AddListener((GameObject entered) => entries++);
            field.OnObjectExited.AddListener((GameObject exited) => exits++);
            Physics.SyncTransforms();

            InvokeUpdateField(field);
            InvokeUpdateField(field);

            Assert.That(field.ObjectsInFieldCount, Is.EqualTo(bodies.Count));
            Assert.That(entries, Is.EqualTo(bodies.Count), "Every body enters once even with a 2-slot buffer.");
            Assert.That(exits, Is.Zero, "A saturated buffer must not churn exit events between updates.");
            foreach (Rigidbody body in bodies)
            {
                AssertVector(body.GetAccumulatedForce(), Vector3.right * 20f);
            }
        }

        [Test]
        public void Explosion_MoreBodiesThanTheInitialBuffer_ReachesEveryBodyOnce()
        {
            ExplosiveForce explosion = CreateExplosion();
            List<Rigidbody> bodies = new();
            for (int i = 0; i < 40; i++)
            {
                float angle = i * Mathf.PI * 2f / 40f;
                bodies.Add(CreateBody("Body " + i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 2f));
            }

            int notifications = 0;
            explosion.OnObjectAffected.AddListener((GameObject affected) => notifications++);
            Physics.SyncTransforms();

            explosion.Explode();

            Assert.That(notifications, Is.EqualTo(bodies.Count));
            foreach (Rigidbody body in bodies)
            {
                // WHY: at the far-from-origin root the ring offsets are not exactly representable, so each body sits
                // up to ~0.001 off radius 2; the expected Linear falloff is computed from the body's actual distance.
                Vector3 offset = body.worldCenterOfMass - explosion.transform.position;
                float distance = offset.magnitude;
                Assert.That(distance, Is.EqualTo(2f).Within(0.01f), "Every body must sit on the ring.");
                Vector3 expected = offset.normalized * (20f * (1f - distance / 4f));
                AssertVector(AccumulatedImpulse(body), expected);
            }
        }

        [Test]
        public void Explosion_ReentrantListener_DoesNotCorruptTheOuterPass()
        {
            ExplosiveForce explosion = CreateExplosion();
            Rigidbody first = CreateBody("First", Vector3.right * 2f);
            Rigidbody second = CreateBody("Second", Vector3.left * 2f);
            Rigidbody third = CreateBody("Third", Vector3.up * 2f);
            int notifications = 0;
            bool nested = false;
            explosion.OnObjectAffected.AddListener((GameObject affected) =>
            {
                notifications++;
                if (!nested)
                {
                    nested = true;
                    explosion.Explode();
                }
            });
            int completions = 0;
            explosion.OnExplode.AddListener(() => completions++);
            Physics.SyncTransforms();

            explosion.Explode();

            Assert.That(notifications, Is.EqualTo(6), "Nested and outer passes must each reach all three bodies.");
            Assert.That(completions, Is.EqualTo(2));
            AssertVector(AccumulatedImpulse(first), Vector3.right * 20f);
            AssertVector(AccumulatedImpulse(second), Vector3.left * 20f);
            AssertVector(AccumulatedImpulse(third), Vector3.up * 20f);

            explosion.Explode();

            Assert.That(notifications, Is.EqualTo(9), "Shared buffers must be clean for the next explosion.");
        }

        [Test]
        public void ImpulseZone_TypedConfiguration_SelectsCustomDirectionCooldownAndLayers()
        {
            GameObject zoneObject = CreateObject("Impulse zone", Vector3.zero);
            zoneObject.AddComponent<BoxCollider>().isTrigger = true;
            ImpulseZone zone = zoneObject.AddComponent<ImpulseZone>();
            zone.AffectedLayers = 1 << TargetLayer;
            zone.SetImpulseForce(12f);
            zone.SetCustomDirection(Vector3.back);
            zone.SetCooldown(10f);
            Rigidbody target = CreateBody("Target", Vector3.right * 2f);
            int applications = 0;
            zone.OnImpulseApplied.AddListener((GameObject affected) => applications++);

            zone.ApplyImpulseToObject(target.gameObject);
            zone.ApplyImpulseToObject(target.gameObject);

            Assert.That(zone.ImpulseForce, Is.EqualTo(12f));
            Assert.That(zone.AffectedLayers.value, Is.EqualTo(1 << TargetLayer));
            Assert.That(applications, Is.EqualTo(1), "The configured cooldown must block the second impulse.");
            AssertVector(AccumulatedImpulse(target), Vector3.back * 12f);

            zone.SetCooldown(0f);
            zone.SetDirection(ImpulseZone.ImpulseDirection.AwayFromCenter);
            zone.ApplyImpulseToObject(target.gameObject);

            Assert.That(applications, Is.EqualTo(2));
            AssertVector(AccumulatedImpulse(target), Vector3.back * 12f + Vector3.right * 12f);
        }

        [UnityTest]
        public IEnumerator ImpulseZone_CooldownBookkeeping_PurgesExpiredAndDestroyedEntries()
        {
            GameObject zoneObject = CreateObject("Impulse zone", Vector3.zero);
            zoneObject.AddComponent<BoxCollider>().isTrigger = true;
            ImpulseZone zone = zoneObject.AddComponent<ImpulseZone>();
            zone.AffectedLayers = 1 << TargetLayer;
            zone.SetCooldown(0.1f);
            Rigidbody kept = CreateBody("Kept", Vector3.right * 3f);
            Rigidbody destroyed = CreateBody("Destroyed", Vector3.left * 3f);
            zone.ApplyImpulseToObject(kept.gameObject);
            zone.ApplyImpulseToObject(destroyed.gameObject);
            IDictionary timers = CooldownTimers(zone);
            Assert.That(timers.Count, Is.EqualTo(2));
            Object.DestroyImmediate(destroyed.gameObject);
            double expiry = Time.timeAsDouble + 0.1d;

            double deadline = Time.realtimeSinceStartupAsDouble + 5d;
            while (Time.timeAsDouble < expiry || timers.Count > 0)
            {
                Assert.That(Time.realtimeSinceStartupAsDouble, Is.LessThan(deadline),
                    "Cooldown bookkeeping did not purge expired entries within the real-time bound.");
                yield return null;
            }

            int applications = 0;
            zone.OnImpulseApplied.AddListener((GameObject affected) => applications++);
            zone.ApplyImpulseToObject(kept.gameObject);
            Assert.That(applications, Is.EqualTo(1), "An expired cooldown must allow the next impulse.");
            Assert.That(timers.Count, Is.EqualTo(1));
        }

        [Test]
        public void Explosion_WarmRepeatedManualExplode_DoesNotAllocate([Values(false, true)] bool emitterHasBody)
        {
            ExplosiveForce explosion = CreateExplosion();
            if (emitterHasBody)
            {
                explosion.gameObject.AddComponent<Rigidbody>().isKinematic = true;
            }

            for (int i = 0; i < 40; i++)
            {
                float angle = i * Mathf.PI * 2f / 40f;
                CreateBody("Body " + i, new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 2f);
            }

            int notifications = 0;
            explosion.OnObjectAffected.AddListener((GameObject affected) => notifications++);
            Physics.SyncTransforms();
            Action explode = explosion.Explode;
            Action explosions = () =>
            {
                for (int i = 0; i < 8; i++)
                {
                    explode();
                }
            };

            // WHY: the first explosion grows the shared overlap buffer past its 32-slot start and sizes the body set.
            explode();
            Assert.That(notifications, Is.EqualTo(40));

            long calls = GcAlloc.TryCount(explosions, out string notAdmitted);

            Assert.That(notifications, Is.EqualTo(40 * 9));
            GcAlloc.IgnoreIfNotAdmitted(notAdmitted);
            Assert.That(calls, Is.Zero,
                "A warm repeated explosion must reuse its overlap buffer and affected-body set.");
        }

        [Test]
        public void MagneticField_WarmUpdate_DoesNotAllocate()
        {
            MagneticField field = CreateObject("Magnetic field", Vector3.zero).AddComponent<MagneticField>();
            SerializedObject settings = new(field);
            settings.FindProperty("_maxOverlapColliders").intValue = 2;
            settings.ApplyModifiedPropertiesWithoutUndo();
            field.AffectedLayers = 1 << TargetLayer;
            field.SetRadius(6f);
            field.SetStrength(10f);
            for (int i = 0; i < 7; i++)
            {
                CreateBody("Body " + i, new Vector3(0f, i * 0.6f - 2f, 1f));
            }

            Physics.SyncTransforms();
            Action update = CreateAction(field, "UpdateField");
            Action updates = () =>
            {
                for (int i = 0; i < 16; i++)
                {
                    update();
                }
            };

            // WHY: the first passes grow the 2-slot buffer, register every body and fill the Rigidbody cache.
            update();
            update();

            long calls = GcAlloc.TryCount(updates, out string notAdmitted);

            Assert.That(field.ObjectsInFieldCount, Is.EqualTo(7));
            GcAlloc.IgnoreIfNotAdmitted(notAdmitted);
            Assert.That(calls, Is.Zero, "A warm field update with a stable population must not allocate.");
        }

        [Test]
        public void MagneticField_UpdateInterval_RunsTheFirstUpdateAtOnce()
        {
            MagneticField field = CreateObject("Magnetic field", Vector3.zero).AddComponent<MagneticField>();
            SerializedObject settings = new(field);
            settings.FindProperty("falloffType").enumValueIndex = (int)MagneticField.FalloffType.Constant;
            settings.FindProperty("updateInterval").floatValue = 1000000f;
            settings.ApplyModifiedPropertiesWithoutUndo();
            field.AffectedLayers = 1 << TargetLayer;
            field.SetDirection(Vector3.right, false);
            field.SetRadius(6f);
            field.SetStrength(10f);
            Rigidbody body = CreateBody("Body", new Vector3(0f, 1f, 0f));
            Physics.SyncTransforms();

            InvokeUpdateField(field);
            InvokeUpdateField(field);

            Assert.That(field.ObjectsInFieldCount, Is.EqualTo(1), "The first update must not wait one interval.");
            AssertVector(body.GetAccumulatedForce(), Vector3.right * 10f);
        }

        [Test]
        public void ImpulseZone_WarmManualImpulse_DoesNotAllocate()
        {
            GameObject zoneObject = CreateObject("Impulse zone", Vector3.zero);
            zoneObject.AddComponent<BoxCollider>().isTrigger = true;
            ImpulseZone zone = zoneObject.AddComponent<ImpulseZone>();
            zone.AffectedLayers = 1 << TargetLayer;
            zone.SetImpulseForce(4f);
            zone.SetDirection(ImpulseZone.ImpulseDirection.TransformForward);
            GameObject target = CreateBody("Target", Vector3.right * 2f).gameObject;
            int applications = 0;
            zone.OnImpulseApplied.AddListener((GameObject affected) => applications++);
            Action impulses = () =>
            {
                for (int i = 0; i < 16; i++)
                {
                    zone.ApplyImpulseToObject(target);
                }
            };
            zone.ApplyImpulseToObject(target);

            long calls = GcAlloc.TryCount(impulses, out string notAdmitted);

            Assert.That(applications, Is.EqualTo(17));
            GcAlloc.IgnoreIfNotAdmitted(notAdmitted);
            Assert.That(calls, Is.Zero, "A warm manual impulse must not allocate.");
        }

        /// <remarks>
        ///     The cooldown bookkeeping in Update runs only once the earliest deadline is due: before it, even a
        ///     destroyed body's entry waits, and neither the skipped nor the due pass allocates once its scratch list
        ///     has grown.
        /// </remarks>
        [Test]
        public void ImpulseZone_WarmUpdateWithLiveCooldowns_SkipsTheScanUntilDueAndDoesNotAllocate()
        {
            GameObject zoneObject = CreateObject("Impulse zone", Vector3.zero);
            zoneObject.AddComponent<BoxCollider>().isTrigger = true;
            ImpulseZone zone = zoneObject.AddComponent<ImpulseZone>();
            zone.AffectedLayers = 1 << TargetLayer;
            zone.SetCooldown(1000f);
            List<Rigidbody> bodies = new();
            for (int i = 0; i < 8; i++)
            {
                Rigidbody body = CreateBody("Body " + i, new Vector3(i - 4f, 0f, 2f));
                bodies.Add(body);
                zone.ApplyImpulseToObject(body.gameObject);
            }

            IDictionary timers = CooldownTimers(zone);
            Assert.That(timers.Count, Is.EqualTo(8));
            Action update = CreateAction(zone, "Update");
            Action idleFrames = () =>
            {
                for (int i = 0; i < 32; i++)
                {
                    update();
                }
            };
            update();
            Object.DestroyImmediate(bodies[0].gameObject);
            MakeCooldownScanDue(zone);
            update();
            Assert.That(timers.Count, Is.EqualTo(7), "A due scan prunes a destroyed body and keeps live cooldowns.");

            Object.DestroyImmediate(bodies[1].gameObject);
            long idleCalls = GcAlloc.TryCount(idleFrames, out string notAdmitted);
            Assert.That(timers.Count, Is.EqualTo(7),
                "Before the earliest deadline the scan is skipped, so even a destroyed body's entry waits.");

            MakeCooldownScanDue(zone);
            long dueCalls = GcAlloc.TryCount(update, out string dueNotAdmitted);
            Assert.That(timers.Count, Is.EqualTo(6));

            GcAlloc.IgnoreIfNotAdmitted(notAdmitted ?? dueNotAdmitted);
            Assert.That(idleCalls, Is.Zero, "An idle zone's Update must not allocate.");
            Assert.That(dueCalls, Is.Zero, "A due cooldown scan reuses its scratch list.");
        }

        private static void MakeCooldownScanDue(ImpulseZone zone)
        {
            FieldInfo field = typeof(ImpulseZone).GetField("_nextCooldownExpiry",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(zone, float.NegativeInfinity);
        }

        private static Action CreateAction(object target, string name)
        {
            MethodInfo method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, name);
            return (Action)method.CreateDelegate(typeof(Action), target);
        }

        private static void InvokeUpdateField(MagneticField field)
        {
            MethodInfo update = typeof(MagneticField).GetMethod("UpdateField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(update, Is.Not.Null);
            update.Invoke(field, null);
        }

        private static IDictionary CooldownTimers(ImpulseZone zone)
        {
            FieldInfo field = typeof(ImpulseZone).GetField("cooldownTimers",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (IDictionary)field.GetValue(zone);
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
            target.AddComponent<BoxCollider>().size = Vector3.one * 0.25f;
            Rigidbody body = target.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.linearDamping = 0f;
            return body;
        }

        private ExplosiveForce CreateExplosion()
        {
            ExplosiveForce explosion = CreateObject("Explosion", Vector3.zero).AddComponent<ExplosiveForce>();
            SerializedObject settings = new(explosion);
            settings.FindProperty("activationMode").enumValueIndex = (int)ExplosiveForce.ActivationMode.Manual;
            settings.FindProperty("forceMode").enumValueIndex = (int)ExplosiveForce.ForceMode.AddForce;
            settings.FindProperty("falloffType").enumValueIndex = (int)ExplosiveForce.FalloffType.Linear;
            settings.ApplyModifiedPropertiesWithoutUndo();
            explosion.AffectedLayers = 1 << TargetLayer;
            explosion.SetForce(20f);
            explosion.SetRadius(4f);
            return explosion;
        }

        private static Vector3 AccumulatedImpulse(Rigidbody body)
        {
            return body.GetAccumulatedForce() * Time.fixedDeltaTime;
        }

        private static void AssertVector(Vector3 actual, Vector3 expected)
        {
            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(Tolerance),
                $"Expected {expected}, received {actual}.");
        }
    }
}
