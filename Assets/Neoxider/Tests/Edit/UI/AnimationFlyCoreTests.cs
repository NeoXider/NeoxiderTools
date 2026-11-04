using System.Collections;
using System.Collections.Generic;
using Neo.Tests.Performance;
using Neo.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Edit
{
    /// <summary>
    ///     Covers the plain C# pieces split out of <see cref="AnimationFly" /> (number rules and visual
    ///     ownership) and the request copy made by <see cref="AnimationFly.Play(AnimationFly.AnimationFlyRequest)" />.
    /// </summary>
    public sealed class AnimationFlyCoreTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject item in _objects)
            {
                if (item != null)
                {
                    Object.DestroyImmediate(item);
                }
            }

            _objects.Clear();
        }

        [Test]
        public void ResolveCount_AppliesBothMultipliersCeilingAndCaps()
        {
            Assert.That(AnimationFlyMath.ResolveCount(3, 1.5f, 1f, 0, 1000), Is.EqualTo(5));
            Assert.That(AnimationFlyMath.ResolveCount(10, 1f, 2f, 0, 15), Is.EqualTo(15));
            Assert.That(AnimationFlyMath.ResolveCount(10, 1f, 1f, 4, 15), Is.EqualTo(4));
            Assert.That(AnimationFlyMath.ResolveCount(30, 1f, 1f, 40, 15), Is.EqualTo(15),
                "A request cap above the global cap must not raise it.");
            Assert.That(AnimationFlyMath.ResolveCount(10, 1f, 1f, 40, 15), Is.EqualTo(10),
                "A cap only limits; it never raises the requested count.");
            Assert.That(AnimationFlyMath.ResolveCount(-2, 1f, 1f, 0, 15), Is.Zero);
            Assert.That(AnimationFlyMath.ResolveCount(5, float.NaN, 1f, 0, 15), Is.Zero);
        }

        [Test]
        public void ResolveCount_ProductBeyondIntRange_ReturnsTheCapInsteadOfWrappingToZero()
        {
            Assert.That(AnimationFlyMath.ResolveCount(1000, 1e10f, 1f, 0, 15), Is.EqualTo(15));
            Assert.That(AnimationFlyMath.ResolveCount(5, float.PositiveInfinity, 1f, 0, 15), Is.EqualTo(15));
            Assert.That(AnimationFlyMath.ResolveCount(int.MaxValue, 2f, 1f, 0, int.MaxValue),
                Is.EqualTo(int.MaxValue));
            Assert.That(AnimationFlyMath.ResolveCount(5, float.NegativeInfinity, 1f, 0, 15), Is.Zero);
            Assert.That(AnimationFlyMath.ResolveCount(5, 1f, 1f, 0, -1), Is.Zero,
                "A non-positive global cap spawns nothing.");
        }

        [Test]
        public void ResolveDurationAndDelay_FallBackOnInvalidInputAndDivideBySpeed()
        {
            Assert.That(AnimationFlyMath.ResolveDuration(null, 2f, 1f), Is.EqualTo(2f));
            Assert.That(AnimationFlyMath.ResolveDuration(4f, 2f, 2f), Is.EqualTo(2f));
            Assert.That(AnimationFlyMath.ResolveDuration(float.NaN, 2f, 1f), Is.EqualTo(2f));
            Assert.That(AnimationFlyMath.ResolveDuration(float.NaN, float.PositiveInfinity, 1f), Is.EqualTo(1f));
            Assert.That(AnimationFlyMath.ResolveDuration(0f, 2f, 1f), Is.EqualTo(AnimationFlyMath.MinDuration));
            Assert.That(AnimationFlyMath.ResolveDuration(3f, 2f, -5f), Is.EqualTo(3f),
                "A non-positive speed multiplier must be treated as 1.");

            Assert.That(AnimationFlyMath.ResolveItemDelay(null, 0.2f, 2f), Is.EqualTo(0.1f).Within(0.00001f));
            Assert.That(AnimationFlyMath.ResolveItemDelay(-1f, 0.2f, 1f), Is.Zero);
            Assert.That(AnimationFlyMath.ResolveItemDelay(float.NaN, float.NaN, 1f), Is.Zero);
        }

        [Test]
        public void StageRatioAndMagnetPoint_FollowTheDocumentedBounds()
        {
            Assert.That(AnimationFlyMath.ClampStageRatio(0f), Is.EqualTo(AnimationFlyMath.MinStageRatio));
            Assert.That(AnimationFlyMath.ClampStageRatio(2f), Is.EqualTo(AnimationFlyMath.MaxStageRatio));

            Vector3 approach = AnimationFlyMath.MagnetApproachPoint(new Vector3(100f, 0f, 0f), Vector3.zero, 20f);
            Assert.That(Vector3.Distance(approach, new Vector3(20f, 0f, 0f)), Is.LessThan(0.0001f));

            Vector3 degenerate = AnimationFlyMath.MagnetApproachPoint(Vector3.one, Vector3.one, 20f);
            Assert.That(Vector3.Distance(degenerate, Vector3.one), Is.LessThan(0.0001f));
            Vector3 noDistance = AnimationFlyMath.MagnetApproachPoint(new Vector3(10f, 0f, 0f), Vector3.zero, 0f);
            Assert.That(Vector3.Distance(noDistance, new Vector3(1.5f, 0f, 0f)), Is.LessThan(0.0001f));
        }

        [Test]
        public void VisualPool_ReturnsUpToLimitAndHandsOutLiveVisualsOnly()
        {
            AnimationFlyVisualPool pool = new AnimationFlyVisualPool();
            GameObject key = Track(new GameObject("Pool key"));
            Transform parent = Track(new GameObject("Pool parent")).transform;
            GameObject first = Track(new GameObject("First"));
            GameObject second = Track(new GameObject("Second"));
            GameObject third = Track(new GameObject("Third"));
            pool.Track(first);
            pool.Track(second);
            pool.Track(third);

            Assert.That(pool.TryReturn(key, AnimationFlySpawnSpace.World, first, 2), Is.True);
            Assert.That(pool.TryReturn(key, AnimationFlySpawnSpace.World, second, 2), Is.True);
            Assert.That(pool.TryReturn(key, AnimationFlySpawnSpace.World, third, 2), Is.False,
                "The per-key limit hands the overflow back to the caller for destruction.");
            Assert.That(pool.Owns(third), Is.False);
            Assert.That(pool.GetPooledCount(key, AnimationFlySpawnSpace.World), Is.EqualTo(2));
            Assert.That(pool.GetPooledCount(key, AnimationFlySpawnSpace.Canvas), Is.Zero);

            Object.DestroyImmediate(second);
            Assert.That(pool.TryTake(key, AnimationFlySpawnSpace.World, parent, out GameObject taken), Is.True);
            Assert.That(taken, Is.SameAs(first), "A destroyed pooled entry must be skipped.");
            Assert.That(taken.transform.parent, Is.SameAs(parent));
            Assert.That(pool.Owns(first), Is.True, "A reused visual stays owned while it flies.");
            Assert.That(pool.TryTake(key, AnimationFlySpawnSpace.World, parent, out GameObject none), Is.False);
            Assert.That(none, Is.Null);
        }

        [Test]
        public void VisualPool_PruneAndDestroyAll_ReleaseEveryOwnedVisual()
        {
            AnimationFlyVisualPool pool = new AnimationFlyVisualPool();
            GameObject destroyedOutside = new GameObject("Destroyed outside");
            GameObject inFlight = new GameObject("In flight");
            GameObject handedOver = Track(new GameObject("Handed over"));
            pool.Track(destroyedOutside);
            pool.Track(inFlight);
            pool.Track(handedOver);
            pool.Release(handedOver);

            Object.DestroyImmediate(destroyedOutside);
            Assert.That(pool.PruneDestroyed(), Is.EqualTo(1));
            Assert.That(pool.OwnedCount, Is.EqualTo(1));

            int visited = 0;
            pool.DestroyAll(_ => visited++);

            Assert.That(visited, Is.EqualTo(1));
            Assert.That(inFlight == null, Is.True);
            Assert.That(handedOver != null, Is.True, "A released (KeepAlive) visual belongs to the caller.");
            Assert.That(pool.OwnedCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator Play_ByType_DoesNotWriteTheResolvedEndIntoTheCallersRequest()
        {
            GameObject flyObject = Track(new GameObject("Request copy fly"));
            AnimationFly fly = flyObject.AddComponent<AnimationFly>();
            fly.spawnSpace = AnimationFlySpawnSpace.World;
            fly.delayBetweenBonuses = 0f;
            GameObject template = Track(new GameObject("Typed template"));
            Transform typedEnd = Track(new GameObject("Typed end")).transform;
            fly.bonusPrefabList.Add(new AnimationFly.BonusPrefabData
            {
                bonusType = 7,
                prefab = template,
                endPos = typedEnd
            });
            fly.RefreshPrefabCache();
            GameObject started = null;
            AnimationFly.AnimationFlyRequest request = new AnimationFly.AnimationFlyRequest
            {
                Type = 7,
                Count = 1,
                StartSpace = AnimationFlyCoordinateSpace.World,
                EndSpace = AnimationFlyCoordinateSpace.World,
                SpawnSpace = AnimationFlySpawnSpace.World,
                Duration = 10f,
                CompletionMode = AnimationFlyCompletionMode.KeepAlive,
                RewardTiming = AnimationFlyRewardTiming.Manual,
                OnItemStarted = item => started = Track(item)
            };

            AnimationFly.AnimationFlyResult result = fly.Play(request);
            yield return null;

            Assert.That(result.TotalCount, Is.EqualTo(1));
            Assert.That(started, Is.Not.Null);
            Assert.That(request.EndTransform, Is.Null,
                "Type resolution must work on a copy; a reused request would otherwise keep the first type's end.");
            Assert.That(fly.OwnedVisualCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Play_WithDestroyedRequestParent_FallsBackToSpawnParent()
        {
            GameObject flyObject = Track(new GameObject("Destroyed parent fly"));
            AnimationFly fly = flyObject.AddComponent<AnimationFly>();
            fly.spawnSpace = AnimationFlySpawnSpace.World;
            fly.delayBetweenBonuses = 0f;
            Transform spawnParent = Track(new GameObject("Spawn parent")).transform;
            fly.spawnParent = spawnParent;
            Transform staleParent = new GameObject("Stale parent").transform;
            Object.DestroyImmediate(staleParent.gameObject);
            GameObject template = Track(new GameObject("Parent fallback template"));
            GameObject started = null;

            fly.Play(new AnimationFly.AnimationFlyRequest
            {
                Prefab = template,
                Parent = staleParent,
                StartSpace = AnimationFlyCoordinateSpace.World,
                EndSpace = AnimationFlyCoordinateSpace.World,
                SpawnSpace = AnimationFlySpawnSpace.World,
                Duration = 10f,
                CompletionMode = AnimationFlyCompletionMode.KeepAlive,
                OnItemStarted = item => started = Track(item)
            });
            yield return null;

            Assert.That(started, Is.Not.Null);
            Assert.That(started.transform.parent, Is.SameAs(spawnParent),
                "A destroyed request parent must fall back to Spawn Parent instead of the scene root.");
        }

        [Test]
        public void ItemDelayYield_ReusesOneInstructionPerFlightUntilTheDelayOrTimeModeChanges()
        {
            AnimationFly.ItemDelayYield delay = default;
            object scaled = delay.Get(0.1f, false);
            Assert.That(scaled, Is.InstanceOf<WaitForSeconds>());
            Assert.That(delay.Get(0.1f, false), Is.SameAs(scaled), "A burst must reuse one instruction per item.");

            object realtime = delay.Get(0.1f, true);
            Assert.That(realtime, Is.InstanceOf<WaitForSecondsRealtime>());
            Assert.That(delay.Get(0.1f, true), Is.SameAs(realtime));
            Assert.That(delay.Get(0.2f, true), Is.Not.SameAs(realtime), "A new delay needs a new instruction.");

            AnimationFly.ItemDelayYield warm = default;
            warm.Get(0.05f, true);
            GcAlloc.AssertNone(() =>
            {
                for (int item = 0; item < 256; item++)
                {
                    warm.Get(0.05f, true);
                }
            }, "Spawning the items of one burst must not allocate a yield instruction per item.");
        }

        [Test]
        public void TypeLookup_OnAFlyThatNeverRanInit_ResolvesFromTheAuthoredList()
        {
            // WHY: an AnimationFly that is not the singleton (Set Instance On Awake off) never runs Init, and
            // OnValidate only fills the cache in the Editor; Edit Mode AddComponent reproduces that state.
            AnimationFly fly = Track(new GameObject("Extra fly")).AddComponent<AnimationFly>();
            GameObject template = Track(new GameObject("Extra template"));
            Transform end = Track(new GameObject("Extra end")).transform;
            fly.bonusPrefabList.Add(new AnimationFly.BonusPrefabData
            {
                bonusType = 3,
                prefab = template,
                endPos = end
            });

            Assert.That(fly.GetPrefab(3), Is.SameAs(template),
                "A bonus type must resolve without RefreshPrefabCache on a component whose Init never ran.");
            Assert.That(fly.GetPos(3), Is.SameAs(end));
        }

        [UnityTest]
        public IEnumerator ScreenStartInWorldSpawnSpace_UsesThisComponentsDefaultDepth()
        {
            GameObject cameraObject = Track(new GameObject("Depth camera"));
            cameraObject.transform.position = new Vector3(0f, 0f, -3f);
            Camera camera = cameraObject.AddComponent<Camera>();
            AnimationFly fly = Track(new GameObject("Depth fly")).AddComponent<AnimationFly>();
            fly.animationCamera = camera;
            fly.spawnSpace = AnimationFlySpawnSpace.World;
            fly.delayBetweenBonuses = 0f;
            fly.defaultScreenToWorldDepth = 7f;
            GameObject template = Track(new GameObject("Depth template"));
            float startedZ = float.NaN;

            fly.Play(new AnimationFly.AnimationFlyRequest
            {
                Prefab = template,
                StartPosition = new Vector3(10f, 10f, 0f),
                EndPosition = new Vector3(20f, 20f, 0f),
                StartSpace = AnimationFlyCoordinateSpace.Screen,
                EndSpace = AnimationFlyCoordinateSpace.Screen,
                SpawnSpace = AnimationFlySpawnSpace.World,
                Duration = 10f,
                CompletionMode = AnimationFlyCompletionMode.KeepAlive,
                OnItemStarted = item => startedZ = Track(item).transform.position.z
            });
            yield return null;

            Assert.That(startedZ, Is.EqualTo(4f).Within(0.001f),
                "A screen point without depth must be placed at this component's Default Screen To World Depth.");
        }

        [Test]
        public void RequestClone_CopiesValuesButNotTheInstance()
        {
            Transform end = Track(new GameObject("Clone end")).transform;
            AnimationFly.AnimationFlyRequest source = new AnimationFly.AnimationFlyRequest
            {
                Count = 4,
                Duration = 0.5f,
                EndTransform = end,
                MotionPreset = AnimationFlyMotionPreset.Magnet
            };

            AnimationFly.AnimationFlyRequest copy = source.Clone();
            source.Count = 9;
            source.EndTransform = null;

            Assert.That(copy, Is.Not.SameAs(source));
            Assert.That(copy.Count, Is.EqualTo(4));
            Assert.That(copy.Duration, Is.EqualTo(0.5f));
            Assert.That(copy.EndTransform, Is.SameAs(end));
            Assert.That(copy.MotionPreset, Is.EqualTo(AnimationFlyMotionPreset.Magnet));
        }

        private GameObject Track(GameObject item)
        {
            _objects.Add(item);
            return item;
        }
    }
}
