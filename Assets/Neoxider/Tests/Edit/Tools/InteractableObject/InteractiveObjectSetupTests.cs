using System.Collections.Generic;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Neo.Editor.Tests.Tools
{
    /// <summary>
    ///     Collider resolution, the debug-ray rules of <see cref="InteractiveObject" /> and the EventSystem lookup of
    ///     its scene setup, pinned without Play Mode.
    /// </summary>
    [TestFixture]
    public sealed class InteractiveObjectSetupTests
    {
        private readonly List<GameObject> _owned = new();

        [TearDown]
        public void DestroyOwned()
        {
            for (int i = _owned.Count - 1; i >= 0; i--)
            {
                if (_owned[i] != null)
                {
                    Object.DestroyImmediate(_owned[i]);
                }
            }

            _owned.Clear();
        }

        /// <summary>
        ///     Regression: the Editor returns a fake-null object from GetComponent for a missing component, and the old
        ///     '??' fallback took it for a hit, so a target whose collider sits only on a child never resolved it in the
        ///     Editor while player builds did.
        /// </summary>
        [Test]
        public void ChildOnlyColliders_AreResolvedFor3DAnd2D()
        {
            InteractiveObject target = CreateTarget("ChildOnlyTarget");
            GameObject child = Create("Child", target.transform);
            BoxCollider child3D = child.AddComponent<BoxCollider>();
            GameObject child2DObject = Create("Child2D", target.transform);
            BoxCollider2D child2D = child2DObject.AddComponent<BoxCollider2D>();

            Assert.That(target.ResolvedCollider3D, Is.SameAs(child3D));
            Assert.That(target.ResolvedCollider2D, Is.SameAs(child2D));
        }

        [Test]
        public void OwnColliderWinsOverChildren_AndInvalidationPicksUpRuntimeChanges()
        {
            InteractiveObject target = CreateTarget("OwnColliderTarget");
            GameObject child = Create("Child", target.transform);
            BoxCollider child3D = child.AddComponent<BoxCollider>();
            Assert.That(target.ResolvedCollider3D, Is.SameAs(child3D));

            SphereCollider own = target.gameObject.AddComponent<SphereCollider>();
            Assert.That(target.ResolvedCollider3D, Is.SameAs(child3D), "Resolution is cached until invalidated.");

            target.InvalidateCachedColliders();
            Assert.That(target.ResolvedCollider3D, Is.SameAs(own), "The target's own collider comes first.");
            Assert.That(target.ResolvedCollider2D == null, Is.True, "No 2D collider exists in the hierarchy.");
        }

        [Test]
        public void DebugRay_OneShotExpiresPersistentDoesNotAndSegmentsKeepTheWindow()
        {
            InteractionDebugRay ray = new();
            Assert.That(ray.IsVisible(false, true, 0f), Is.True, "A fresh one-shot window starts at time zero.");
            Assert.That(ray.IsVisible(false, false, 0f), Is.False);

            ray.Record(Vector3.zero, Vector3.forward, Color.red, false, 10f, 1f);
            Assert.That(ray.End, Is.EqualTo(Vector3.forward));
            Assert.That(ray.Color, Is.EqualTo(Color.red));
            Assert.That(ray.IsVisible(false, true, 11f), Is.True);
            Assert.That(ray.IsVisible(false, true, 11.01f), Is.False);
            Assert.That(ray.IsVisible(true, false, 1000f), Is.True, "The persistent ray is always drawn.");

            ray.SetSegment(Vector3.up, Vector3.right, Color.gray);
            Assert.That(ray.Start, Is.EqualTo(Vector3.up));
            Assert.That(ray.VisibleUntil, Is.EqualTo(11f), "A persistent segment update keeps the one-shot window.");

            ray.Record(Vector3.zero, Vector3.back, Color.green, false, 5f, -3f);
            Assert.That(ray.VisibleUntil, Is.EqualTo(5f), "A negative duration clamps to zero.");
            ray.Record(Vector3.zero, Vector3.back, Color.green, true, 5f, 1f);
            Assert.That(float.IsPositiveInfinity(ray.VisibleUntil), Is.True);
        }

        /// <summary>
        ///     Many targets set up before the EventSystem registers as current cost one scene search, not one per
        ///     target. The remembered instance is dropped once deactivated or destroyed (as by a scene change).
        ///     Edit Mode never runs EventSystem.OnEnable, so <c>EventSystem.current</c> stays unset, as during a load.
        /// </summary>
        [Test]
        public void EventSystemLookup_SearchesOnceWhileTheFoundInstanceStaysActive()
        {
            InteractiveObjectSceneSetup.ResetStaticState();
            try
            {
                Assume.That(EventSystem.current == null, Is.True, "Edit Mode leaves EventSystem.current unset.");
                Assume.That(Object.FindFirstObjectByType<EventSystem>() == null, Is.True,
                    "The test scene must start without an EventSystem.");

                GameObject firstObject = Create("EventSystem A", null);
                EventSystem first = firstObject.AddComponent<EventSystem>();
                Assume.That(EventSystem.current == null, Is.True, "Adding it must not register it as current.");

                Assert.That(InteractiveObjectSceneSetup.FindEventSystem(), Is.SameAs(first));
                Assert.That(InteractiveObjectSceneSetup.SceneSearchCount, Is.EqualTo(1));

                for (int i = 0; i < 8; i++)
                {
                    InteractiveObjectSceneSetup.EnsureEventSystem(CreateTarget("Target " + i), true, false);
                }

                Assert.That(InteractiveObjectSceneSetup.SceneSearchCount, Is.EqualTo(1),
                    "Eight targets reuse the remembered EventSystem.");

                GameObject secondObject = Create("EventSystem B", null);
                EventSystem second = secondObject.AddComponent<EventSystem>();
                Assert.That(InteractiveObjectSceneSetup.FindEventSystem(), Is.SameAs(first),
                    "The remembered instance is kept while it stays active.");
                Assert.That(InteractiveObjectSceneSetup.SceneSearchCount, Is.EqualTo(1));

                firstObject.SetActive(false);
                Assert.That(InteractiveObjectSceneSetup.FindEventSystem(), Is.SameAs(second),
                    "A deactivated instance is dropped and the scene is searched again.");
                Assert.That(InteractiveObjectSceneSetup.FindEventSystem(), Is.SameAs(second));
                Assert.That(InteractiveObjectSceneSetup.SceneSearchCount, Is.EqualTo(2));

                Object.DestroyImmediate(secondObject);
                firstObject.SetActive(true);
                Assert.That(InteractiveObjectSceneSetup.FindEventSystem(), Is.SameAs(first),
                    "A destroyed instance is dropped and the scene is searched again.");
                Assert.That(InteractiveObjectSceneSetup.SceneSearchCount, Is.EqualTo(3));

                Object.DestroyImmediate(firstObject);
                Assert.That(InteractiveObjectSceneSetup.FindEventSystem() == null, Is.True);
                Assert.That(InteractiveObjectSceneSetup.SceneSearchCount, Is.EqualTo(4));
            }
            finally
            {
                InteractiveObjectSceneSetup.ResetStaticState();
            }
        }

        [TestCase(true, true, true, 0)]
        [TestCase(false, true, true, 1)]
        [TestCase(false, false, true, 2)]
        [TestCase(false, false, false, 3)]
        public void DebugRay_PersistentColourFollowsInteractHoverRangePriority(bool interacting, bool hovered,
            bool inRange, int expected)
        {
            Color[] colours = { Color.green, Color.yellow, Color.cyan, Color.gray };
            Assert.That(InteractionDebugRay.ResolvePersistentColor(interacting, hovered, inRange),
                Is.EqualTo(colours[expected]));
        }

        private InteractiveObject CreateTarget(string name)
        {
            GameObject owner = Create(name, null);
#if MIRROR
            owner.AddComponent<Mirror.NetworkIdentity>();
#endif
            return owner.AddComponent<InteractiveObject>();
        }

        private GameObject Create(string name, Transform parent)
        {
            GameObject instance = new(name);
            _owned.Add(instance);
            if (parent != null)
            {
                instance.transform.SetParent(parent, false);
            }

            return instance;
        }
    }
}
