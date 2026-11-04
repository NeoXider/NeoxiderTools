using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Neo.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Tests.Play
{
    /// <summary>
    ///     Play Mode regressions of the 2026-11 UI quality passes: AnimationFly callback isolation and request
    ///     copies, UI feedback and page switching that must keep working while Time.timeScale is 0, FakeLoad
    ///     progress streams, ButtonChangePage press scale and CategoryBar teardown.
    /// </summary>
    public sealed class UIRuntimeRegressionPlayModeTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private float _previousTimeScale;
        private bool _previousErrors;
        private bool _previousFakeLoadSessionFlag;

        [SetUp]
        public void SetUp()
        {
            _previousTimeScale = Time.timeScale;
            _previousErrors = NeoDiagnostics.RuntimeErrorsEnabled;
            _previousFakeLoadSessionFlag = GetFakeLoadSessionFlag();
            NeoDiagnostics.Configure(errors: true);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = _previousTimeScale;
            NeoDiagnostics.Configure(errors: _previousErrors);
            SetFakeLoadSessionFlag(_previousFakeLoadSessionFlag);
            foreach (GameObject item in _objects)
            {
                if (item != null)
                {
                    Object.DestroyImmediate(item);
                }
            }

            _objects.Clear();
        }

        [UnityTest]
        public IEnumerator AnimationFly_ThrowingArrivalCallback_StillCompletesAndReleasesTheVisual()
        {
            AnimationFly fly = CreateFly();
            GameObject template = Track(new GameObject("Throwing callback template"));
            GameObject visual = null;
            int allArrived = 0;
            int rewards = 0;
            AnimationFly.AnimationFlyRequest request = WorldRequest(template);
            request.CompletionMode = AnimationFlyCompletionMode.Destroy;
            request.OnItemStarted = item => visual = item;
            request.OnItemArrived = _ => throw new InvalidOperationException("arrival boom");
            request.OnReward = () => rewards++;
            request.OnAllArrived = () => allArrived++;
            LogAssert.Expect(LogType.Exception, new Regex("arrival boom"));

            AnimationFly.AnimationFlyResult result = fly.Play(request);
            yield return WaitUntilRealtime(() => result.IsCompleted, 3f);
            yield return null;

            Assert.That(result.IsCompleted, Is.True, "A throwing callback must not stall the flight result.");
            Assert.That(allArrived, Is.EqualTo(1));
            Assert.That(rewards, Is.EqualTo(1));
            Assert.That(visual == null, Is.True, "The arrived visual must still be destroyed.");
            Assert.That(fly.OwnedVisualCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator AnimationFly_RequestEditedAfterPlay_DoesNotRewireTheRunningFlight()
        {
            AnimationFly fly = CreateFly();
            GameObject template = Track(new GameObject("Reused request template"));
            int firstRewards = 0;
            int secondRewards = 0;
            AnimationFly.AnimationFlyRequest request = WorldRequest(template);
            request.Count = 2;
            request.DelayBetweenItems = 0.05f;
            request.RewardTiming = AnimationFlyRewardTiming.OnEachArrived;
            request.OnReward = () => firstRewards++;

            AnimationFly.AnimationFlyResult first = fly.Play(request);
            request.OnReward = () => secondRewards++;
            request.Count = 1;
            AnimationFly.AnimationFlyResult second = fly.Play(request);
            yield return WaitUntilRealtime(() => first.IsCompleted && second.IsCompleted, 3f);

            Assert.That(first.TotalCount, Is.EqualTo(2));
            Assert.That(firstRewards, Is.EqualTo(2), "Each flight keeps the callbacks it was started with.");
            Assert.That(secondRewards, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ButtonScale_PressWhileTimeScaleIsZero_ReachesThePressedScale()
        {
            GameObject button = Track(new GameObject("Paused press", typeof(RectTransform)));
            ButtonScale scale = button.AddComponent<ButtonScale>();
            JsonUtility.FromJsonOverwrite("{\"resizeDuration\":0.05}", scale);
            Time.timeScale = 0f;

            scale.SetPressed(true);
            yield return WaitRealtime(0.3f);

            Assert.That(button.transform.localScale.x, Is.EqualTo(0.85f).Within(0.0001f),
                "UI press feedback must not freeze under a paused time scale.");
        }

        [UnityTest]
        public IEnumerator ButtonShake_Stop_RestoresThePositionFromWhenTheShakeStarted()
        {
            GameObject button = Track(new GameObject("Moved after Awake", typeof(RectTransform)));
            ButtonShake shake = button.AddComponent<ButtonShake>();
            Vector3 laidOut = new Vector3(40f, 55f, 3f);
            button.transform.localPosition = laidOut;

            shake.Shake();
            yield return null;
            yield return null;
            shake.StopShake();

            Assert.That(button.transform.localPosition, Is.EqualTo(laidOut),
                "A button moved after Awake (layout, animation) must not jump back to its Awake position.");

            button.SetActive(false);
            button.SetActive(true);
            Assert.That(button.transform.localPosition, Is.EqualTo(laidOut));
        }

        [UnityTest]
        public IEnumerator ButtonShake_TimedShakeWhileTimeScaleIsZero_Ends()
        {
            GameObject button = Track(new GameObject("Paused shake", typeof(RectTransform)));
            ButtonShake shake = button.AddComponent<ButtonShake>();
            JsonUtility.FromJsonOverwrite("{\"_shakeDuration\":0.05}", shake);
            Vector3 origin = button.transform.localPosition;
            Time.timeScale = 0f;

            shake.Shake();
            Assert.That(shake.IsShaking, Is.True);
            yield return WaitRealtime(0.3f);

            Assert.That(shake.IsShaking, Is.False);
            Assert.That(button.transform.localPosition, Is.EqualTo(origin));
        }

        [UnityTest]
        public IEnumerator FakeLoad_WhileTimeScaleIsZero_FinishesOnceEvenWhenRestarted()
        {
            FakeLoadProbe probe = new FakeLoadProbe();
            FakeLoad fakeLoad = CreateFakeLoad("{\"_loadOnAwake\":false,\"isLoadOne\":false,\"timeLoad\":{\"x\":0.05,\"y\":0.05}}",
                probe);
            Time.timeScale = 0f;

            fakeLoad.Load();
            fakeLoad.Load();
            yield return WaitRealtime(0.4f);

            Assert.That(probe.Finished, Is.EqualTo(1), "A restarted load must finish exactly once.");
            Assert.That(probe.LastProgress, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator FakeLoad_DefaultOneShotLoadCalledTwiceWhileRunning_FinishesOnce()
        {
            SetFakeLoadSessionFlag(false);
            FakeLoadProbe probe = new FakeLoadProbe();
            FakeLoad fakeLoad = CreateFakeLoad("{\"_loadOnAwake\":false,\"timeLoad\":{\"x\":0.1,\"y\":0.1}}", probe);

            fakeLoad.Load();
            fakeLoad.Load();
            Assert.That(probe.Finished, Is.Zero, "A second call during the one-shot load must not finish it instantly.");
            yield return WaitRealtime(0.5f);

            Assert.That(probe.Starts, Is.EqualTo(2), "A call during the load restarts it.");
            Assert.That(probe.Finished, Is.EqualTo(1), "The default one-shot mode must finish exactly once.");
            Assert.That(probe.ProgressAfterFinish, Is.Zero, "No stale stream may report progress after the finish.");
            Assert.That(probe.LastProgress, Is.EqualTo(1f));

            fakeLoad.Load();
            Assert.That(probe.Finished, Is.EqualTo(1), "A one-shot revisit never finishes inside Load().");
            yield return null;
            Assert.That(probe.Finished, Is.EqualTo(2), "After the session load finished, one-shot mode completes next frame.");
            Assert.That(probe.Starts, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator FakeLoad_EndLoadWhileRunning_StopsTheProgressAndFinishesOnce()
        {
            SetFakeLoadSessionFlag(false);
            FakeLoadProbe probe = new FakeLoadProbe();
            FakeLoad fakeLoad = CreateFakeLoad("{\"_loadOnAwake\":false,\"timeLoad\":{\"x\":0.1,\"y\":0.1}}", probe);

            fakeLoad.Load();
            fakeLoad.EndLoad();
            Assert.That(probe.Finished, Is.EqualTo(1));
            yield return WaitRealtime(0.4f);

            Assert.That(probe.Finished, Is.EqualTo(1), "Skipping the loader must not let the stream finish again.");
            Assert.That(probe.ProgressAfterFinish, Is.Zero);
            Assert.That(probe.LastProgress, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator FakeLoad_ZeroDurationOneShotLoad_FinishesSynchronouslyAndLaterLoadsCompleteNextFrame()
        {
            SetFakeLoadSessionFlag(false);
            FakeLoadProbe probe = new FakeLoadProbe();
            FakeLoad fakeLoad = CreateFakeLoad("{\"_loadOnAwake\":false,\"timeLoad\":{\"x\":0,\"y\":0}}", probe);

            fakeLoad.Load();
            Assert.That(probe.Starts, Is.EqualTo(1));
            Assert.That(probe.Finished, Is.EqualTo(1), "A zero-length load finishes inside Load().");

            fakeLoad.Load();
            fakeLoad.Load();
            Assert.That(probe.Starts, Is.EqualTo(1),
                "The finished zero-length load must not stay marked as running and restart.");
            Assert.That(probe.Finished, Is.EqualTo(1), "A one-shot revisit never finishes inside Load().");
            yield return null;
            Assert.That(probe.Finished, Is.EqualTo(2), "Two revisit calls in one frame complete once, on the next frame.");
            yield return null;
            Assert.That(probe.Finished, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator FakeLoad_DeactivatedDuringTheLoad_ContinuesOnActivationAndFinishesOnce()
        {
            SetFakeLoadSessionFlag(false);
            FakeLoadProbe probe = new FakeLoadProbe();
            FakeLoad fakeLoad = CreateFakeLoad("{\"_loadOnAwake\":false,\"timeLoad\":{\"x\":0.2,\"y\":0.2}}", probe);

            fakeLoad.Load();
            yield return WaitRealtime(0.05f);
            float progressBeforePause = probe.LastProgress;
            fakeLoad.gameObject.SetActive(false);
            yield return WaitRealtime(0.4f);
            Assert.That(probe.Finished, Is.Zero, "An inactive loader cannot tick.");
            Assert.That(fakeLoad.IsLoading, Is.True, "The paused load still counts as loading.");

            fakeLoad.gameObject.SetActive(true);
            yield return WaitRealtime(0.5f);

            Assert.That(probe.Starts, Is.EqualTo(1), "Activation continues the same load instead of restarting it.");
            Assert.That(probe.Finished, Is.EqualTo(1));
            Assert.That(probe.LastProgress, Is.EqualTo(1f));
            Assert.That(progressBeforePause, Is.GreaterThan(0f));
            Assert.That(fakeLoad.IsLoading, Is.False);
        }

        [UnityTest]
        public IEnumerator FakeLoad_EndLoadFromTheFirstProgressTick_FinishesOnceAndStopsReporting()
        {
            SetFakeLoadSessionFlag(false);
            FakeLoadProbe probe = new FakeLoadProbe();
            FakeLoad fakeLoad = CreateFakeLoad("{\"_loadOnAwake\":false,\"timeLoad\":{\"x\":0.1,\"y\":0.1}}", probe);
            bool skipped = false;
            fakeLoad.OnChange.AddListener(_ =>
            {
                if (!skipped)
                {
                    skipped = true;
                    fakeLoad.EndLoad();
                }
            });

            fakeLoad.Load();
            Assert.That(probe.Finished, Is.EqualTo(1));
            yield return WaitRealtime(0.4f);

            Assert.That(probe.Finished, Is.EqualTo(1), "The skipped stream must not finish a second time.");
            Assert.That(probe.ProgressAfterFinish, Is.Zero, "The skipped stream must stop reporting progress.");
        }

        [UnityTest]
        public IEnumerator UIPages_AnimatedAndDelayedSwitchWhileTimeScaleIsZero_StillSwitch()
        {
            GameObject root = Track(new GameObject("Paused page switcher"));
            root.SetActive(false);
            GameObject first = Track(new GameObject("Page 0"));
            GameObject second = Track(new GameObject("Page 1"));
            global::Neo.UI.UI pages = root.AddComponent<global::Neo.UI.UI>();
            SetPrivateField(pages, "_pages", new[] { first, second });
            JsonUtility.FromJsonOverwrite("{\"_timeDelay\":0.05,\"startId\":0}", pages);
            root.SetActive(true);
            Assert.That(pages.UseUnscaledTime, Is.True, "Page delays default to unscaled time.");
            Assert.That(first.activeSelf, Is.True);
            Time.timeScale = 0f;

            pages.SetPageAnim(1);
            yield return WaitRealtime(0.4f);

            Assert.That(second.activeSelf, Is.True, "An animated switch from a paused menu must still happen.");
            Assert.That(first.activeSelf, Is.False);

            pages.SetPageDelay(0);
            yield return WaitRealtime(0.3f);

            Assert.That(first.activeSelf, Is.True, "A delayed switch must not wait for the paused time scale.");
            Assert.That(pages.id, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator ButtonChangePage_PressOnANonUniformScale_ScalesEveryAxisAndRestoresIt()
        {
            GameObject button = Track(new GameObject("Non-uniform page button", typeof(RectTransform)));
            button.SetActive(false);
            Vector3 baseScale = new Vector3(2f, 1f, 0.5f);
            button.transform.localScale = baseScale;
            ButtonChangePage changePage = button.AddComponent<ButtonChangePage>();
            JsonUtility.FromJsonOverwrite("{\"_timeAnimImage\":0.05,\"_canSwitchPage\":false}", changePage);
            button.SetActive(true);

            changePage.OnPointerDown(null);
            yield return WaitRealtime(0.3f);

            Vector3 pressed = button.transform.localScale;
            Assert.That(pressed.x, Is.EqualTo(1.7f).Within(0.001f));
            Assert.That(pressed.y, Is.EqualTo(0.85f).Within(0.001f),
                "The press must scale each axis of the base scale instead of copying x to every axis.");
            Assert.That(pressed.z, Is.EqualTo(0.425f).Within(0.001f));

            changePage.OnPointerUp(null);
            yield return WaitRealtime(0.3f);

            Assert.That(Vector3.Distance(button.transform.localScale, baseScale), Is.LessThan(0.001f));
        }

        [UnityTest]
        public IEnumerator CategoryBar_DestroyingTheWholeBarWithSpawnedItems_DestroysTheMarkerWithoutErrors()
        {
            GameObject root = Track(new GameObject("Destroyed category bar", typeof(RectTransform)));
            root.SetActive(false);
            GameObject template = new GameObject("Item template", typeof(RectTransform), typeof(UnityEngine.UI.Button),
                typeof(CategoryBarItem));
            template.transform.SetParent(root.transform, false);
            template.SetActive(false);
            GameObject marker = new GameObject("Selection marker", typeof(RectTransform));
            marker.transform.SetParent(root.transform, false);
            CategoryBar bar = root.AddComponent<CategoryBar>();
            SetPrivateField(bar, "_itemPrefab", template.GetComponent<CategoryBarItem>());
            SetPrivateField(bar, "_selectionMarker", (RectTransform)marker.transform);
            bar.SetCategories(new[]
            {
                new CategoryBar.Entry { Id = "first" },
                new CategoryBar.Entry { Id = "second" }
            });
            root.SetActive(true);
            yield return null;

            Assert.That(bar.IsInitialized, Is.True);
            Assert.That(marker.transform.parent, Is.Not.SameAs(root.transform),
                "The marker must sit on the selected spawned item before the bar is destroyed.");

            // WHY: an unexpected error log (re-parenting onto a GameObject that is being destroyed) fails this test.
            Object.Destroy(root);
            yield return null;
            yield return null;

            Assert.That(root == null, Is.True);
            Assert.That(marker == null, Is.True, "The marker belongs to the destroyed hierarchy and dies with it.");
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private FakeLoad CreateFakeLoad(string settingsJson, FakeLoadProbe probe)
        {
            GameObject loader = Track(new GameObject("Regression loader"));
            loader.SetActive(false);
            FakeLoad fakeLoad = loader.AddComponent<FakeLoad>();
            JsonUtility.FromJsonOverwrite(settingsJson, fakeLoad);
            fakeLoad.OnStart = new UnityEvent();
            fakeLoad.OnFinisLoad = new UnityEvent();
            fakeLoad.OnChange = new UnityEvent<float>();
            fakeLoad.OnChangePercent = new UnityEvent<int>();
            fakeLoad.OnStart.AddListener(() => probe.Starts++);
            fakeLoad.OnFinisLoad.AddListener(() => probe.Finished++);
            fakeLoad.OnChange.AddListener(value =>
            {
                probe.LastProgress = value;
                if (probe.Finished > 0)
                {
                    probe.ProgressAfterFinish++;
                }
            });
            loader.SetActive(true);
            return fakeLoad;
        }

        private static bool GetFakeLoadSessionFlag()
        {
            return (bool)FakeLoadSessionFlagField().GetValue(null);
        }

        private static void SetFakeLoadSessionFlag(bool value)
        {
            FakeLoadSessionFlagField().SetValue(null, value);
        }

        private static FieldInfo FakeLoadSessionFlagField()
        {
            FieldInfo field = typeof(FakeLoad).GetField("isInitialized", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "FakeLoad.isInitialized static flag expected");
            return field;
        }

        private AnimationFly CreateFly()
        {
            AnimationFly fly = Track(new GameObject("Regression AnimationFly")).AddComponent<AnimationFly>();
            fly.arcStrength = 0f;
            fly.useUnscaledTime = true;
            fly.delayBetweenBonuses = 0f;
            fly.spawnSpace = AnimationFlySpawnSpace.World;
            return fly;
        }

        private static AnimationFly.AnimationFlyRequest WorldRequest(GameObject prefab)
        {
            return new AnimationFly.AnimationFlyRequest
            {
                Prefab = prefab,
                StartPosition = Vector3.zero,
                EndPosition = Vector3.one,
                StartSpace = AnimationFlyCoordinateSpace.World,
                EndSpace = AnimationFlyCoordinateSpace.World,
                SpawnSpace = AnimationFlySpawnSpace.World,
                Duration = 0.02f,
                CompletionMode = AnimationFlyCompletionMode.DisableAndPool
            };
        }

        private static IEnumerator WaitRealtime(float seconds)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
        }

        private static IEnumerator WaitUntilRealtime(Func<bool> condition, float timeoutSeconds)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!condition() && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
        }

        private GameObject Track(GameObject item)
        {
            _objects.Add(item);
            return item;
        }

        private sealed class FakeLoadProbe
        {
            public int Starts;
            public int Finished;
            public int ProgressAfterFinish;
            public float LastProgress = -1f;
        }
    }
}
