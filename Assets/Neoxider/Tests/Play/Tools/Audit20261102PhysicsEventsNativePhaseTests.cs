using System;
using System.Collections;
using System.Collections.Generic;
using Neo.Network;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if MIRROR
using Mirror;
#endif

namespace Neo.Tests.Play
{
    /// <summary>
    /// Native offline callback coverage. OptionalSolo means isNetworked=true with no active network;
    /// it does not start a server, host, client or replace a borrowed network session.
    /// </summary>
    public sealed class Audit20261102PhysicsEventsNativePhaseTests
    {
        private const float StepSeconds = 0.02f;
        private const int StepBudget = 12;
        private Scene _scene;
        private GameObject _relayObject;
        private GameObject _actorObject;

        private enum Phase { Enter, Stay, Exit }

        private readonly struct Observation
        {
            public Observation(Phase phase, GameObject otherObject, UnityEngine.Object otherCollider,
                object payload, int contacts)
            {
                Phase = phase;
                OtherObject = otherObject;
                OtherCollider = otherCollider;
                Payload = payload;
                Contacts = contacts;
            }

            public Phase Phase { get; }
            public GameObject OtherObject { get; }
            public UnityEngine.Object OtherCollider { get; }
            public object Payload { get; }
            public int Contacts { get; }
        }

        [SetUp]
        public void SetUp()
        {
            _scene = default;
            _relayObject = null;
            _actorObject = null;
            Assert.That(Application.isPlaying, Is.True, "Native relay callbacks are delivered only in Play Mode.");
            Assert.That(NeoNetworkState.IsNetworkActive, Is.False, "Never take over another network session.");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // WHY: Destroy exactly the owned objects and unload only the owned scene, as PhysicsEventsNetworkTests do.
            AsyncOperation unloading = null;
            try
            {
                if (_actorObject != null) UnityEngine.Object.DestroyImmediate(_actorObject);
            }
            finally
            {
                try
                {
                    if (_relayObject != null) UnityEngine.Object.DestroyImmediate(_relayObject);
                }
                finally
                {
                    if (_scene.IsValid() && _scene.isLoaded) unloading = SceneManager.UnloadSceneAsync(_scene);
                }
            }

            double deadline = Time.realtimeSinceStartupAsDouble + 10d;
            while (unloading != null && !unloading.isDone)
            {
                Assert.That(Time.realtimeSinceStartupAsDouble, Is.LessThan(deadline), "Owned local scene did not unload.");
                yield return null;
            }

            Assert.That(!_scene.IsValid() || !_scene.isLoaded, Is.True, "Release only the exact owned scene.");
            Assert.That(NeoNetworkState.IsNetworkActive, Is.False, "This fixture never starts networking.");
        }

        [UnityTest] public IEnumerator Native2DTrigger_Offline_DeliversAllPhasesAndPayload() => Run2D(true, false);
        [UnityTest] public IEnumerator Native2DTrigger_OptionalSolo_DeliversAllPhasesAndPayload() => Run2D(true, true);
        [UnityTest] public IEnumerator Native2DCollision_Offline_DeliversAllPhasesAndPayload() => Run2D(false, false);
        [UnityTest] public IEnumerator Native2DCollision_OptionalSolo_DeliversAllPhasesAndPayload() => Run2D(false, true);
        [UnityTest] public IEnumerator Native3DTrigger_Offline_DeliversAllPhasesAndPayload() => Run3D(true, false);
        [UnityTest] public IEnumerator Native3DTrigger_OptionalSolo_DeliversAllPhasesAndPayload() => Run3D(true, true);
        [UnityTest] public IEnumerator Native3DCollision_Offline_DeliversAllPhasesAndPayload() => Run3D(false, false);
        [UnityTest] public IEnumerator Native3DCollision_OptionalSolo_DeliversAllPhasesAndPayload() => Run3D(false, true);

        private IEnumerator Run2D(bool trigger, bool networked)
        {
            _scene = SceneManager.CreateScene("NeoNativeRelay2D-" + Guid.NewGuid().ToString("N"),
                new CreateSceneParameters(LocalPhysicsMode.Physics2D));
            PhysicsScene2D physics = _scene.GetPhysicsScene2D();
            Assert.That(physics.IsValid(), Is.True);
            Assert.That(physics, Is.Not.EqualTo(Physics2D.defaultPhysicsScene));
            Assert.That(Physics2D.GetIgnoreLayerCollision(0, 0), Is.False, "Layer0 contact is a required setup precondition.");

            _relayObject = CreateInactive("Relay2D");
#if MIRROR
            _relayObject.AddComponent<NetworkIdentity>();
#endif
            BoxCollider2D relayCollider = _relayObject.AddComponent<BoxCollider2D>();
            relayCollider.isTrigger = trigger;
            PhysicsEvents2D relay = _relayObject.AddComponent<PhysicsEvents2D>();
            relay.isNetworked = networked;
            relay.filterByLayer = true;
            relay.layers = ~0;
            relay.filterByTag = false;

            _actorObject = CreateInactive("Actor2D");
            _actorObject.transform.position = Vector3.up * 5f;
            BoxCollider2D actorCollider = _actorObject.AddComponent<BoxCollider2D>();
            Rigidbody2D body = _actorObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeAll;
            body.sleepMode = RigidbodySleepMode2D.NeverSleep;

            List<Observation> inspector = new();
            List<Observation> code = new();
            if (trigger)
            {
                relay.onTriggerEnter.AddListener(c => inspector.Add(Trigger(Phase.Enter, c)));
                relay.onTriggerStay.AddListener(c => inspector.Add(Trigger(Phase.Stay, c)));
                relay.onTriggerExit.AddListener(c => inspector.Add(Trigger(Phase.Exit, c)));
                relay.TriggerEnterOccurred += c => code.Add(Trigger(Phase.Enter, c));
                relay.TriggerStayOccurred += c => code.Add(Trigger(Phase.Stay, c));
                relay.TriggerExitOccurred += c => code.Add(Trigger(Phase.Exit, c));
            }
            else
            {
                relay.onCollisionEnter.AddListener(c => inspector.Add(Collision(Phase.Enter, c)));
                relay.onCollisionStay.AddListener(c => inspector.Add(Collision(Phase.Stay, c)));
                relay.onCollisionExit.AddListener(c => inspector.Add(Collision(Phase.Exit, c)));
                relay.CollisionEnterOccurred += c => code.Add(Collision(Phase.Enter, c));
                relay.CollisionStayOccurred += c => code.Add(Collision(Phase.Stay, c));
                relay.CollisionExitOccurred += c => code.Add(Collision(Phase.Exit, c));
            }

            _relayObject.SetActive(true);
            _actorObject.SetActive(true);
            Assert.That(body.simulated && body.bodyType == RigidbodyType2D.Dynamic, Is.True);
            Assert.That(physics.Simulate(StepSeconds), Is.True);
            yield return null;
            Assert.That(inspector, Is.Empty, "Separated bodies must not report a contact.");
            Assert.That(code, Is.Empty);

            body.position = Vector2.up * 0.75f;
            yield return SimulateUntil2D(physics, body, () => HasBoth(inspector, code, Phase.Enter), "2D enter");
            yield return SimulateUntil2D(physics, body, () => HasBoth(inspector, code, Phase.Stay), "2D stay");
            body.position = Vector2.up * 5f;
            yield return SimulateUntil2D(physics, body, () => HasBoth(inspector, code, Phase.Exit), "2D exit");
            int settledCount = inspector.Count;
            Assert.That(physics.Simulate(StepSeconds), Is.True);
            yield return null;
            Assert.That(inspector.Count, Is.EqualTo(settledCount), "Separated bodies must not repeat exit or stay.");
            AssertPhases(inspector, code, _actorObject, actorCollider, !trigger);
        }

        private IEnumerator Run3D(bool trigger, bool networked)
        {
            _scene = SceneManager.CreateScene("NeoNativeRelay3D-" + Guid.NewGuid().ToString("N"),
                new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            PhysicsScene physics = _scene.GetPhysicsScene();
            Assert.That(physics.IsValid(), Is.True);
            Assert.That(physics, Is.Not.EqualTo(Physics.defaultPhysicsScene));
            Assert.That(Physics.GetIgnoreLayerCollision(0, 0), Is.False, "Layer0 contact is a required setup precondition.");

            _relayObject = CreateInactive("Relay3D");
#if MIRROR
            _relayObject.AddComponent<NetworkIdentity>();
#endif
            BoxCollider relayCollider = _relayObject.AddComponent<BoxCollider>();
            relayCollider.isTrigger = trigger;
            PhysicsEvents3D relay = _relayObject.AddComponent<PhysicsEvents3D>();
            relay.isNetworked = networked;
            relay.filterByLayer = true;
            relay.layers = ~0;
            relay.filterByTag = false;

            _actorObject = CreateInactive("Actor3D");
            _actorObject.transform.position = Vector3.up * 5f;
            BoxCollider actorCollider = _actorObject.AddComponent<BoxCollider>();
            Rigidbody body = _actorObject.AddComponent<Rigidbody>();
            body.isKinematic = false;
            body.useGravity = false;
            // WHY: Triggers keep the frozen body. A collision needs a live contact axis: a fully frozen body has no
            // established native stay delivery.
            body.constraints = trigger
                ? RigidbodyConstraints.FreezeAll
                : RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
            body.sleepThreshold = 0f;

            List<Observation> inspector = new();
            List<Observation> code = new();
            List<Observation> native = new();
            List<bool> nativeAwake = new();
            if (!trigger)
            {
                Audit20261102Collision3DNativeControl control = _relayObject.AddComponent<Audit20261102Collision3DNativeControl>();
                control.Entered += collision =>
                {
                    native.Add(Collision(Phase.Enter, collision));
                    nativeAwake.Add(!body.IsSleeping());
                };
                control.Stayed += collision =>
                {
                    native.Add(Collision(Phase.Stay, collision));
                    nativeAwake.Add(!body.IsSleeping());
                };
                control.Exited += collision =>
                {
                    native.Add(Collision(Phase.Exit, collision));
                    nativeAwake.Add(!body.IsSleeping());
                };
            }
            if (trigger)
            {
                relay.onTriggerEnter.AddListener(c => inspector.Add(Trigger(Phase.Enter, c)));
                relay.onTriggerStay.AddListener(c => inspector.Add(Trigger(Phase.Stay, c)));
                relay.onTriggerExit.AddListener(c => inspector.Add(Trigger(Phase.Exit, c)));
                relay.TriggerEnterOccurred += c => code.Add(Trigger(Phase.Enter, c));
                relay.TriggerStayOccurred += c => code.Add(Trigger(Phase.Stay, c));
                relay.TriggerExitOccurred += c => code.Add(Trigger(Phase.Exit, c));
            }
            else
            {
                relay.onCollisionEnter.AddListener(c => inspector.Add(Collision(Phase.Enter, c)));
                relay.onCollisionStay.AddListener(c => inspector.Add(Collision(Phase.Stay, c)));
                relay.onCollisionExit.AddListener(c => inspector.Add(Collision(Phase.Exit, c)));
                relay.CollisionEnterOccurred += c => code.Add(Collision(Phase.Enter, c));
                relay.CollisionStayOccurred += c => code.Add(Collision(Phase.Stay, c));
                relay.CollisionExitOccurred += c => code.Add(Collision(Phase.Exit, c));
            }

            _relayObject.SetActive(true);
            _actorObject.SetActive(true);
            Assert.That(body.isKinematic, Is.False);
            physics.Simulate(StepSeconds);
            yield return null;
            Assert.That(inspector, Is.Empty, "Separated bodies must not report a contact.");
            Assert.That(code, Is.Empty);

            body.position = Vector3.up * 0.75f;
            Action sustainContact = trigger ? null : () => body.linearVelocity = Vector3.down * 0.25f;
            Func<string> diagnostic = trigger ? null : () =>
                "native=" + PhaseCounts(native) + "; inspector=" + PhaseCounts(inspector) + "; code=" + PhaseCounts(code)
                + "; position=" + body.position + "; velocity=" + body.linearVelocity
                + "; sleeping=" + body.IsSleeping() + "; constraints=" + body.constraints
                + "; kinematic=" + body.isKinematic + "; sleepThreshold=" + body.sleepThreshold;
            yield return SimulateUntil3D(physics, body,
                () => HasBoth(inspector, code, Phase.Enter) && (trigger || native.Exists(value => value.Phase == Phase.Enter)),
                "3D enter", sustainContact, diagnostic);
            if (trigger)
            {
                yield return SimulateUntil3D(physics, body, () => HasBoth(inspector, code, Phase.Stay), "3D stay");
            }
            else
            {
                // WHY: Unity's own OnCollisionStay on a plain MonoBehaviour admits the fixture first. A failed control
                // is a fixture problem; a passing control with no relay phase is a relay failure.
                yield return SimulateUntil3D(physics, body, () => native.Exists(value => value.Phase == Phase.Stay),
                    "native control 3D stay", sustainContact, diagnostic);
                Assert.That(HasBoth(inspector, code, Phase.Stay), Is.True,
                    "Raw Unity stay was delivered but both relay surfaces must also receive it. " + diagnostic());
            }
            body.position = Vector3.up * 5f;
            if (!trigger) body.linearVelocity = Vector3.zero;
            yield return SimulateUntil3D(physics, body,
                () => HasBoth(inspector, code, Phase.Exit) && (trigger || native.Exists(value => value.Phase == Phase.Exit)),
                "3D exit", diagnostics: diagnostic);
            int settledCount = inspector.Count;
            int nativeSettledCount = native.Count;
            physics.Simulate(StepSeconds);
            yield return null;
            Assert.That(inspector.Count, Is.EqualTo(settledCount), "Separated bodies must not repeat exit or stay.");
            AssertPhases(inspector, code, _actorObject, actorCollider, !trigger);
            if (!trigger)
            {
                Assert.That(native.Count, Is.EqualTo(nativeSettledCount), "The raw control must settle outside too.");
                Assert.That(native.Count, Is.EqualTo(inspector.Count), "Relay phase cardinality must match raw Unity delivery.");
                Assert.That(nativeAwake.Count, Is.EqualTo(native.Count));
                for (int i = 0; i < native.Count; i++)
                {
                    Assert.That(native[i].Phase, Is.EqualTo(inspector[i].Phase));
                    Assert.That(native[i].OtherObject, Is.SameAs(_actorObject));
                    Assert.That(native[i].OtherCollider, Is.SameAs(actorCollider));
                    if (native[i].Phase == Phase.Stay)
                        Assert.That(nativeAwake[i], Is.True, "The raw native stay control requires an awake actor.");
                    if (native[i].Phase != Phase.Exit) Assert.That(native[i].Contacts, Is.GreaterThan(0));
                }
                TestContext.WriteLine("Admitted 3D collision: " + diagnostic());
            }
        }

        private GameObject CreateInactive(string name)
        {
            GameObject result = new(name);
            result.SetActive(false);
            result.layer = 0;
            SceneManager.MoveGameObjectToScene(result, _scene);
            return result;
        }

        private static IEnumerator SimulateUntil2D(PhysicsScene2D physics, Rigidbody2D body,
            Func<bool> observed, string phase)
        {
            for (int step = 0; step < StepBudget && !observed(); step++)
            {
                body.WakeUp();
                Assert.That(physics.Simulate(StepSeconds), Is.True);
                yield return null;
            }
            Assert.That(observed(), Is.True, "Native callback not observed within bounded local steps: " + phase);
        }

        private static IEnumerator SimulateUntil3D(PhysicsScene physics, Rigidbody body,
            Func<bool> observed, string phase, Action beforeStep = null, Func<string> diagnostics = null)
        {
            for (int step = 0; step < StepBudget && !observed(); step++)
            {
                beforeStep?.Invoke();
                body.WakeUp();
                bool sleepingBefore = diagnostics != null && body.IsSleeping();
                physics.Simulate(StepSeconds);
                bool sleepingAfter = diagnostics != null && body.IsSleeping();
                yield return null;
                if (diagnostics != null)
                    TestContext.WriteLine(phase + "; step=" + step + "; sleepingBefore/AfterSim="
                        + sleepingBefore + "/" + sleepingAfter + "; " + diagnostics());
            }
            Assert.That(observed(), Is.True, "Native callback not observed within bounded local steps: " + phase
                + (diagnostics != null ? "; " + diagnostics() : string.Empty));
        }

        private static string PhaseCounts(List<Observation> observations)
        {
            int enters = 0;
            int stays = 0;
            int exits = 0;
            foreach (Observation observation in observations)
            {
                if (observation.Phase == Phase.Enter) enters++;
                else if (observation.Phase == Phase.Stay) stays++;
                else exits++;
            }
            return enters + "/" + stays + "/" + exits;
        }

        private static bool HasBoth(List<Observation> inspector, List<Observation> code, Phase phase)
        {
            return inspector.Exists(value => value.Phase == phase) && code.Exists(value => value.Phase == phase);
        }

        private static Observation Trigger(Phase phase, Collider collider) =>
            new(phase, collider != null ? collider.gameObject : null, collider, collider, 0);

        private static Observation Trigger(Phase phase, Collider2D collider) =>
            new(phase, collider != null ? collider.gameObject : null, collider, collider, 0);

        private static Observation Collision(Phase phase, UnityEngine.Collision collision) =>
            new(phase, collision != null ? collision.gameObject : null, collision != null ? collision.collider : null,
                collision, collision != null ? collision.contactCount : 0);

        private static Observation Collision(Phase phase, Collision2D collision) =>
            new(phase, collision != null ? collision.gameObject : null, collision != null ? collision.collider : null,
                collision, collision != null ? collision.contactCount : 0);

        private static void AssertPhases(List<Observation> inspector, List<Observation> code,
            GameObject expectedObject, UnityEngine.Object expectedCollider, bool collision)
        {
            Assert.That(inspector.Count, Is.GreaterThanOrEqualTo(3), "Enter, stay and exit must all be delivered.");
            Assert.That(code.Count, Is.EqualTo(inspector.Count), "UnityEvent and typed C# event must observe each native phase.");
            Assert.That(inspector[0].Phase, Is.EqualTo(Phase.Enter));
            Assert.That(inspector[inspector.Count - 1].Phase, Is.EqualTo(Phase.Exit));
            int enters = 0;
            int stays = 0;
            int exits = 0;
            for (int i = 0; i < inspector.Count; i++)
            {
                Observation actual = inspector[i];
                Observation typed = code[i];
                Assert.That(typed.Phase, Is.EqualTo(actual.Phase));
                Assert.That(actual.OtherObject, Is.SameAs(expectedObject));
                Assert.That(typed.OtherObject, Is.SameAs(expectedObject));
                Assert.That(actual.OtherCollider, Is.SameAs(expectedCollider));
                Assert.That(typed.OtherCollider, Is.SameAs(expectedCollider));
                Assert.That(actual.Payload, Is.Not.Null);
                Assert.That(typed.Payload, Is.SameAs(actual.Payload));
                if (collision && actual.Phase != Phase.Exit)
                {
                    Assert.That(actual.Contacts, Is.GreaterThan(0), "Collision data must originate from a native contact.");
                    Assert.That(typed.Contacts, Is.EqualTo(actual.Contacts));
                }
                if (actual.Phase == Phase.Enter) enters++;
                else if (actual.Phase == Phase.Stay) stays++;
                else exits++;
            }
            Assert.That(enters, Is.EqualTo(1));
            Assert.That(stays, Is.GreaterThanOrEqualTo(1));
            Assert.That(exits, Is.EqualTo(1));
        }
    }
}
