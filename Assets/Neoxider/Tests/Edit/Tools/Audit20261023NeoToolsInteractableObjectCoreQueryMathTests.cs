using Neo.Tools;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Tools
{
    [TestFixture]
    public sealed class Audit20261023NeoToolsInteractableObjectCoreQueryMathTests
    {
        [TestCase(0f, 3e20f, 2e20f, false)]
        [TestCase(0f, 2e20f, 2e20f, true)]
        [TestCase(0f, 1e20f, 2e20f, true)]
        [TestCase(-3e38f, 3e38f, 3e38f, false)]
        [TestCase(0f, 1e-23f, 5e-24f, false)]
        [TestCase(0f, 1e-23f, 1e-23f, true)]
        [TestCase(0f, float.Epsilon + float.Epsilon, float.Epsilon, false)]
        [TestCase(-float.Epsilon, float.Epsilon, float.Epsilon, false)]
        public void IsWithinRange_FiniteDistancesKeepTheirBoundaryAcrossFloatScales(
            float sourceX, float targetX, float maximumDistance, bool expected)
        {
            bool actual = InteractionQueryMath.IsWithinRange(new Vector3(sourceX, 0f, 0f),
                new Vector3(targetX, 0f, 0f), maximumDistance);

            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase(1e20f)]
        [TestCase(1e-23f)]
        public void IsWithinRange_DiagonalOutsideLimitDoesNotPassWhenFloatSquaresLoseScale(float scale)
        {
            Vector3 target = new Vector3(scale, scale, scale);

            bool actual = InteractionQueryMath.IsWithinRange(Vector3.zero, target, 1.5f * scale);

            Assert.That(actual, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TryGetNearestHit_MaximumFiniteDistanceRemainsAValidCandidate(bool isTarget)
        {
            InteractionRayHit[] hits =
            {
                new InteractionRayHit(float.NaN, Vector3.zero, true, true),
                new InteractionRayHit(float.PositiveInfinity, Vector3.zero, true, true),
                new InteractionRayHit(float.NegativeInfinity, Vector3.zero, true, true),
                new InteractionRayHit(-1f, Vector3.zero, true, true),
                new InteractionRayHit(float.MaxValue, Vector3.right, isTarget, !isTarget),
                new InteractionRayHit(0f, Vector3.zero, true, false)
            };

            bool found = InteractionQueryMath.TryGetNearestHit(hits, hits.Length - 1,
                out InteractionRayHit nearestHit);

            Assert.That(found, Is.True);
            Assert.That(nearestHit.Distance, Is.EqualTo(float.MaxValue));
            Assert.That(nearestHit.Point, Is.EqualTo(Vector3.right));
            Assert.That(nearestHit.IsTarget, Is.EqualTo(isTarget));
            Assert.That(nearestHit.BlocksInteraction, Is.EqualTo(!isTarget));
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void TrySelectTarget_MaximumFiniteDistanceIsAcceptedWithNoCloserBlocker(
            bool requireClearPath, bool includeEqualDistanceBlocker)
        {
            InteractionRayHit[] hits =
            {
                new InteractionRayHit(float.PositiveInfinity, Vector3.zero, true, false),
                new InteractionRayHit(float.NaN, Vector3.zero, false, true),
                new InteractionRayHit(float.MaxValue, Vector3.up, true, false),
                new InteractionRayHit(float.MaxValue, Vector3.forward, true, false),
                new InteractionRayHit(float.MaxValue, Vector3.right, false, true)
            };
            int count = includeEqualDistanceBlocker ? hits.Length : hits.Length - 1;

            bool found = InteractionQueryMath.TrySelectTarget(hits, count, requireClearPath,
                out InteractionRayHit targetHit);

            Assert.That(found, Is.True);
            Assert.That(targetHit.Distance, Is.EqualTo(float.MaxValue));
            Assert.That(targetHit.Point, Is.EqualTo(Vector3.up));
            Assert.That(targetHit.IsTarget, Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TrySelectTarget_MaximumFiniteTargetStillObeysCloserBlocker(bool requireClearPath)
        {
            InteractionRayHit[] hits =
            {
                new InteractionRayHit(float.MaxValue, Vector3.up, true, false),
                new InteractionRayHit(1f, Vector3.right, false, true)
            };

            bool found = InteractionQueryMath.TrySelectTarget(hits, hits.Length, requireClearPath,
                out InteractionRayHit targetHit);

            Assert.That(found, Is.EqualTo(!requireClearPath));
            Assert.That(targetHit.Distance, Is.EqualTo(float.MaxValue));
            Assert.That(targetHit.Point, Is.EqualTo(Vector3.up),
                "The nearest target is returned even when a closer obstacle rejects it.");
        }
    }
}
