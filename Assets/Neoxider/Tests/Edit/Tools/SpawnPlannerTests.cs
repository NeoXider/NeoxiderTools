using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    public sealed class SpawnPlannerTests
    {
        [Test]
        public void PickWeighted_UsesWeightsAndSkipsLockedEntries()
        {
            float[] weights = { 1f, 3f, 6f };
            float[] unlock = { 0f, 0f, 30f };

            Assert.That(SpawnPlanner.PickWeighted(3, weights, unlock, 0f, 0.1f), Is.EqualTo(0));
            Assert.That(SpawnPlanner.PickWeighted(3, weights, unlock, 0f, 0.3f), Is.EqualTo(1),
                "entry 2 is locked until 30 s, so the total is 4 and 0.3 lands on the weight-3 entry");
            Assert.That(SpawnPlanner.PickWeighted(3, weights, unlock, 30f, 0.5f), Is.EqualTo(2),
                "after unlock the weight-6 entry covers rolls from 0.4");
        }

        [Test]
        public void PickWeighted_EmptyListsMeanUniformAndNothingAvailableIsMinusOne()
        {
            Assert.That(SpawnPlanner.PickWeighted(4, null, null, 0f, 0.99f), Is.EqualTo(3));
            Assert.That(SpawnPlanner.PickWeighted(2, new float[] { 0f, 0f }, null, 0f, 0.5f), Is.EqualTo(-1));
            Assert.That(SpawnPlanner.PickWeighted(2, null, new[] { 5f, 5f }, 1f, 0.5f), Is.EqualTo(-1));
            Assert.That(SpawnPlanner.PickWeighted(0, null, null, 0f, 0.5f), Is.EqualTo(-1));
        }

        [Test]
        public void RingPoint_StaysInsideTheRingOnTheChosenPlane()
        {
            Vector3 center = new Vector3(2f, 3f, 4f);
            for (int i = 0; i < 64; i++)
            {
                float a = i / 64f;
                float r = (i * 7 % 64) / 64f;
                Vector3 xy = SpawnPlanner.RingPoint(center, 5f, 8f, SpawnPlane.XY, a, r);
                Vector3 xz = SpawnPlanner.RingPoint(center, 5f, 8f, SpawnPlane.XZ, a, r);
                float dxy = new Vector2(xy.x - center.x, xy.y - center.y).magnitude;
                float dxz = new Vector2(xz.x - center.x, xz.z - center.z).magnitude;
                Assert.That(dxy, Is.InRange(4.999f, 8.001f));
                Assert.That(dxz, Is.InRange(4.999f, 8.001f));
                Assert.That(xy.z, Is.EqualTo(center.z), "XY keeps the depth of the center");
                Assert.That(xz.y, Is.EqualTo(center.y), "XZ keeps the height of the center");
            }
        }

        [Test]
        public void DelayMultiplier_IsOneWithoutACurveAndNeverReachesZero()
        {
            Assert.That(SpawnPlanner.DelayMultiplier(null, 10f), Is.EqualTo(1f));
            Assert.That(SpawnPlanner.DelayMultiplier(new AnimationCurve(), 10f), Is.EqualTo(1f));
            AnimationCurve ramp = AnimationCurve.Linear(0f, 1f, 60f, 0f);
            Assert.That(SpawnPlanner.DelayMultiplier(ramp, 30f), Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(SpawnPlanner.DelayMultiplier(ramp, 60f), Is.EqualTo(0.01f));
        }

        [Test]
        public void Spawner_RingAroundTargetAndSelectionApiAreApplied()
        {
            GameObject host = new GameObject("RingSpawner");
            GameObject target = new GameObject("Target");
            GameObject prefab = new GameObject("Prefab");
            try
            {
                target.transform.position = new Vector3(10f, -2f, 0f);
                Spawner spawner = host.AddComponent<Spawner>();
                spawner.prefabs = new[] { prefab, prefab };
                spawner.AroundTarget = target.transform;
                spawner.SetRing(3f, 4f, SpawnPlane.XY);
                spawner.SetSelection(new[] { 0f, 1f }, null);

                for (int i = 0; i < 16; i++)
                {
                    Vector3 p = spawner.GetSpawnPosition();
                    float d = Vector2.Distance(p, target.transform.position);
                    Assert.That(d, Is.InRange(2.999f, 4.001f));
                    Assert.That(spawner.PickPrefabIndex(), Is.EqualTo(1), "weight 0 never spawns");
                }

                spawner.MaxActive = -3;
                Assert.That(spawner.MaxActive, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(prefab);
            }
        }
    }
}
