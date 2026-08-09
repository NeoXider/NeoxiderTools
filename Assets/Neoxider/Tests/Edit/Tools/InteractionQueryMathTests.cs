using Neo.Tools;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Tools
{
    [TestFixture]
    public sealed class InteractionQueryMathTests
    {
        [Test]
        public void InteractiveObject_ImplementsReusableTargetContract()
        {
            Assert.That(typeof(IInteractiveTarget).IsAssignableFrom(typeof(InteractiveObject)), Is.True);
            Assert.That(typeof(IInteractiveTarget).GetMethod(nameof(IInteractiveTarget.InteractDown)), Is.Not.Null);
            Assert.That(typeof(IInteractiveTarget).GetMethod(nameof(IInteractiveTarget.InteractUp)), Is.Not.Null);
            Assert.That(typeof(IInteractiveTarget).GetProperty(nameof(IInteractiveTarget.IsInteractable)),
                Is.Not.Null);
        }

        [Test]
        public void InteractiveTargetContract_CanBeImplementedWithoutMonoBehaviour()
        {
            TestInteractiveTarget target = new TestInteractiveTarget();
            IInteractiveTarget contract = target;

            contract.InteractDown();
            contract.InteractUp();

            Assert.That(contract.IsInteractable, Is.True);
            Assert.That(target.DownCount, Is.EqualTo(1));
            Assert.That(target.UpCount, Is.EqualTo(1));
        }

        [TestCase(0f, true)]
        [TestCase(-1f, true)]
        [TestCase(5f, true)]
        [TestCase(4.99f, false)]
        public void IsWithinRange_UsesInclusiveSquaredDistance(float maximumDistance, bool expected)
        {
            bool actual = InteractionQueryMath.IsWithinRange(Vector3.zero, new Vector3(3f, 4f, 0f),
                maximumDistance);

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void GetObstacleCheckDistance_AppliesPaddingAndClampsToZero()
        {
            Assert.That(InteractionQueryMath.GetObstacleCheckDistance(3f), Is.EqualTo(2.9f).Within(0.0001f));
            Assert.That(InteractionQueryMath.GetObstacleCheckDistance(0.05f), Is.Zero);
            Assert.That(InteractionQueryMath.GetObstacleCheckDistance(3f, -2f), Is.EqualTo(3f));
        }

        [Test]
        public void TryGetNearestHit_IsIndependentOfInputOrder()
        {
            InteractionRayHit[] hits =
            {
                new InteractionRayHit(8f, new Vector3(8f, 0f, 0f), false, true),
                new InteractionRayHit(2f, new Vector3(2f, 0f, 0f), true, false),
                new InteractionRayHit(5f, new Vector3(5f, 0f, 0f), false, true)
            };

            bool found = InteractionQueryMath.TryGetNearestHit(hits, hits.Length,
                out InteractionRayHit nearestHit);

            Assert.That(found, Is.True);
            Assert.That(nearestHit.Distance, Is.EqualTo(2f));
            Assert.That(nearestHit.IsTarget, Is.True);
        }

        [Test]
        public void TrySelectTarget_BlockerBeforeTarget_RequiresClearPath()
        {
            InteractionRayHit[] hits =
            {
                new InteractionRayHit(10f, new Vector3(10f, 0f, 0f), true, false),
                new InteractionRayHit(4f, new Vector3(4f, 0f, 0f), false, true)
            };

            bool blocked = InteractionQueryMath.TrySelectTarget(hits, hits.Length, true,
                out InteractionRayHit blockedTarget);
            bool acceptedWithoutObstacleCheck = InteractionQueryMath.TrySelectTarget(hits, hits.Length, false,
                out InteractionRayHit acceptedTarget);

            Assert.That(blocked, Is.False);
            Assert.That(blockedTarget.Distance, Is.EqualTo(10f));
            Assert.That(acceptedWithoutObstacleCheck, Is.True);
            Assert.That(acceptedTarget.Distance, Is.EqualTo(10f));
        }

        [Test]
        public void TrySelectTarget_NonBlockingHitAndEqualDistanceBlocker_DoNotHideTarget()
        {
            InteractionRayHit[] hits =
            {
                new InteractionRayHit(1f, Vector3.one, false, false),
                new InteractionRayHit(5f, Vector3.right * 5f, false, true),
                new InteractionRayHit(5f, Vector3.forward * 5f, true, false)
            };

            bool found = InteractionQueryMath.TrySelectTarget(hits, hits.Length, true,
                out InteractionRayHit targetHit);

            Assert.That(found, Is.True);
            Assert.That(targetHit.Point, Is.EqualTo(Vector3.forward * 5f));
        }

        [Test]
        public void CameraResolver_PrefersCachedThenMainCamera()
        {
            GameObject cachedObject = new GameObject("CachedInteractionCamera");
            GameObject mainObject = new GameObject("MainInteractionCamera");
            Camera cached = cachedObject.AddComponent<Camera>();
            Camera main = mainObject.AddComponent<Camera>();
            mainObject.tag = "MainCamera";
            try
            {
                Assert.That(InteractionCameraResolver.Resolve(cached, false), Is.SameAs(cached));
                Assert.That(InteractionCameraResolver.Resolve(null, false), Is.SameAs(Camera.main));
            }
            finally
            {
                Object.DestroyImmediate(cachedObject);
                Object.DestroyImmediate(mainObject);
            }
        }

        private sealed class TestInteractiveTarget : IInteractiveTarget
        {
            public bool IsInteractable => true;

            public int DownCount { get; private set; }

            public int UpCount { get; private set; }

            public void InteractDown()
            {
                DownCount++;
            }

            public void InteractUp()
            {
                UpCount++;
            }
        }
    }
}
