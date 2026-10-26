using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Neo.Extensions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public sealed partial class Audit20261023CoroutineUnityLifecycleTests
    {
        private readonly List<GameObject> _objects = new();
        private readonly List<CoroutineExtensions.CoroutineHandle> _handles = new();
        private readonly HashSet<CoroutineHelper> _previousHosts = new();
        private Audit20261023CoroutineUnityPhaseProbe _frameProbe;
        private bool _tearingDown;

        [UnitySetUp]
        public IEnumerator EnterRuntime()
        {
            // Keep the mode transition in this top-level UnitySetUp enumerator.
            yield return new EnterPlayMode();
            Assert.IsTrue(Application.isPlaying);
            _tearingDown = false;
            _previousTimeScale = Time.timeScale;
            _previousFixedDeltaTime = Time.fixedDeltaTime;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            _previousHosts.Clear();
            foreach (CoroutineHelper helper in Object.FindObjectsByType<CoroutineHelper>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                _previousHosts.Add(helper);
            }

            _frameProbe = CreatePhaseProbe(CreateOwner().gameObject);
            yield return AwaitNativeFrames(1);
        }

        [UnityTearDown]
        public IEnumerator ExitRuntime()
        {
            Time.timeScale = _previousTimeScale;
            Time.fixedDeltaTime = _previousFixedDeltaTime;
            _tearingDown = true;
            CoroutineExtensions.CoroutineHandle[] handles = _handles.ToArray();
            _handles.Clear();
            foreach (CoroutineExtensions.CoroutineHandle handle in handles)
            {
                handle?.Stop();
            }

            for (int index = _objects.Count - 1; index >= 0; index--)
            {
                if (_objects[index] != null)
                {
                    Object.DestroyImmediate(_objects[index]);
                }
            }

            foreach (CoroutineHelper helper in Object.FindObjectsByType<CoroutineHelper>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!_previousHosts.Contains(helper))
                {
                    Object.DestroyImmediate(helper.gameObject);
                }
            }

            _handles.Clear();
            _objects.Clear();
            _previousHosts.Clear();
            _frameProbe = null;
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator ImmediateCompletion_DoesNotResurrectHandles()
        {
            CoroutineRunner owner = CreateOwner();
            int calls = 0;
            CoroutineExtensions.CoroutineHandle frames = Track(owner.DelayFrames(0, () => calls++));
            CoroutineExtensions.CoroutineHandle repeat = Track(owner.RepeatUntil(() => true, () => calls++));
            CoroutineExtensions.CoroutineHandle custom = Track(CoroutineExtensions.Start(CompleteImmediately(() => calls++)));
            AudioSource source = owner.gameObject.AddComponent<AudioSource>();
            CoroutineExtensions.CoroutineHandle fade = Track(source.FadeTo(0.35f, 0f));

            Assert.AreEqual(2, calls);
            AssertCompleted(frames);
            AssertCompleted(repeat);
            AssertCompleted(custom);
            AssertCompleted(fade);
            Assert.That(source.volume, Is.EqualTo(0.35f).Within(0.001f));
            yield return AwaitNativeFrames(1);
            Assert.AreEqual(2, calls);
        }

        [UnityTest]
        public IEnumerator Stop_DisposesNestedIteratorsOnceInReverseOrder()
        {
            List<string> cleanup = new();
            int calls = 0;
            bool entered = false;
            CoroutineExtensions.CoroutineHandle handle = Track(CoroutineExtensions.Start(
                Enclose(WaitForCancellation(() => cleanup.Add("inner"), () => calls++, () => entered = true),
                    () => cleanup.Add("outer"))));
            for (int frame = 0; frame < 4 && !entered; frame++)
            {
                yield return AwaitNativeFrames(1);
            }

            Assert.IsTrue(entered, "Cancellation must exercise an entered resource-owning iterator.");
            Assert.IsTrue(handle.IsRunning);

            handle.Stop();
            handle.Stop();

            AssertCompleted(handle);
            CollectionAssert.AreEqual(new[] { "inner", "outer" }, cleanup);
            yield return AwaitNativeFrames(1);
            Assert.AreEqual(0, calls);
            CollectionAssert.AreEqual(new[] { "inner", "outer" }, cleanup);
        }

        [UnityTest]
        public IEnumerator Stop_FromInsideRoutineAndCleanup_IsIdempotent()
        {
            int calls = 0;
            int disposed = 0;
            CoroutineExtensions.CoroutineHandle handle = null;
            handle = Track(CoroutineExtensions.Start(StopAfterYield(
                () => { calls++; handle.Stop(); },
                () => { disposed++; handle.Stop(); })));
            yield return AwaitCompletion(handle);
            yield return AwaitNativeFrames(1);

            AssertCompleted(handle);
            Assert.AreEqual(1, calls);
            Assert.AreEqual(1, disposed);
        }

        [UnityTest]
        public IEnumerator InactiveHierarchy_CancelsWorkAndRejectsNewCallbacks()
        {
            CoroutineRunner parent = CreateOwner();
            CoroutineRunner owner = CreateOwner();
            owner.transform.SetParent(parent.transform);
            int calls = 0;
            CoroutineExtensions.CoroutineHandle pending = Track(owner.NextFrame(() => calls++));

            parent.gameObject.SetActive(false);
            Assert.IsTrue(owner.gameObject.activeSelf);
            AssertCompleted(pending);
            CoroutineExtensions.CoroutineHandle rejected = Track(owner.DelayFrames(0, () => calls++));
            AssertCompleted(rejected);
            parent.gameObject.SetActive(true);
            yield return AwaitNativeFrames(2);
            Assert.AreEqual(0, calls);
        }

        [UnityTest]
        public IEnumerator DestroyingOwnerComponent_CancelsOnlyItsWork()
        {
            CoroutineRunner owner = CreateOwner();
            GameObject ownerObject = owner.gameObject;
            int calls = 0;
            CoroutineExtensions.CoroutineHandle pending = Track(owner.Delay(60f, () => calls += 10));
            CoroutineExtensions.CoroutineHandle global = Track(CoroutineExtensions.DelayFrames(1, () => calls++));

            Object.Destroy(owner);
            yield return AwaitNativeFrames(2);

            Assert.IsTrue(owner == null, "The native component destruction must have completed.");
            Assert.IsTrue(ownerObject != null);
            AssertCompleted(pending);
            AssertCompleted(global);
            Assert.AreEqual(1, calls);

            CoroutineRunner disabledOwner = CreateOwner();
            CoroutineExtensions.CoroutineHandle continuing = Track(disabledOwner.NextFrame(() => calls++));
            disabledOwner.enabled = false;
            yield return AwaitNativeFrames(2);
            AssertCompleted(continuing);
            Assert.AreEqual(2, calls, "Disabling only a MonoBehaviour preserves Unity's coroutine semantics.");
        }

        [UnityTest]
        public IEnumerator PredicateFault_CompletesHandleAndReleasesEnclosingIterator()
        {
            bool fail = false;
            int disposed = 0;
            int polls = 0;
            WaitUntil instruction = new(() =>
            {
                polls++;
                return fail ? throw new InvalidOperationException("Audit20261023 predicate fault") : false;
            });
            CoroutineExtensions.CoroutineHandle handle = Track(CoroutineExtensions.Start(
                Enclose(WaitForInstruction(instruction), () => disposed++)));
            yield return AwaitNativeFrames(1);
            Assert.That(polls, Is.GreaterThan(0), "Unity must poll the actual wait before the fault is armed.");
            Assert.IsTrue(handle.IsRunning);

            int pollsBeforeFault = polls;
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: Audit20261023 predicate fault"));
            fail = true;
            for (int frame = 0; frame < 6 && handle.IsRunning; frame++)
            {
                yield return AwaitNativeFrames(1);
            }

            Assert.That(polls, Is.GreaterThan(pollsBeforeFault));
            AssertCompleted(handle);
            Assert.AreEqual(1, disposed);
            yield return AwaitNativeFrames(1);
            Assert.AreEqual(1, disposed);
        }

        [UnityTest]
        public IEnumerator CustomYieldInstruction_IsDisposedOnCancellation()
        {
            DisposableWait instruction = new();
            CoroutineExtensions.CoroutineHandle handle = Track(CoroutineExtensions.Start(WaitForInstruction(instruction)));
            for (int frame = 0; frame < 4 && instruction.PollCount == 0; frame++)
            {
                yield return AwaitNativeFrames(1);
            }

            Assert.That(instruction.PollCount, Is.GreaterThan(0));

            handle.Stop();
            handle.Stop();

            AssertCompleted(handle);
            Assert.AreEqual(1, instruction.DisposeCount);
            yield return AwaitNativeFrames(1);
            Assert.AreEqual(1, instruction.DisposeCount);
        }

        [UnityTest]
        public IEnumerator DestroyedAudioAndLookTargets_EndGlobalOperations()
        {
            CoroutineRunner audioOwner = CreateOwner();
            AudioSource source = audioOwner.gameObject.AddComponent<AudioSource>();
            CoroutineRunner lookOwner = CreateOwner();
            CoroutineExtensions.CoroutineHandle fade = Track(source.FadeTo(0.5f, 60f));
            CoroutineExtensions.CoroutineHandle look = Track(CoroutineExtensions.Start(
                lookOwner.transform.SmoothLookAtRoutine(Vector3.right, 0.1f)));

            Object.Destroy(source);
            Object.Destroy(lookOwner.gameObject);
            yield return AwaitNativeFrames(3);

            Assert.IsTrue(source == null);
            Assert.IsTrue(lookOwner == null);
            AssertCompleted(fade);
            AssertCompleted(look);
            Assert.IsTrue(audioOwner.gameObject != null);
        }

        private CoroutineRunner CreateOwner()
        {
            GameObject gameObject = new("Audit coroutine owner");
            _objects.Add(gameObject);
            CoroutineRunner owner = gameObject.AddComponent<CoroutineRunner>();
            Assert.IsNotNull(owner, "Unity must attach the actual runtime coroutine owner.");
            return owner;
        }

        private static Audit20261023CoroutineUnityPhaseProbe CreatePhaseProbe(GameObject gameObject)
        {
            Audit20261023CoroutineUnityPhaseProbe probe = gameObject
                .AddComponent<Audit20261023CoroutineUnityPhaseProbe>();
            Assert.IsNotNull(probe, "Unity must attach the relocated runtime phase probe.");
            Assert.AreEqual("Neo.Tests.Fixtures", probe.GetType().Assembly.GetName().Name);
            Assert.IsTrue(probe.isActiveAndEnabled);
            return probe;
        }

        private IEnumerator AwaitNativeFrames(int frameCount)
        {
            Assert.IsTrue(Application.isPlaying);
            Assert.IsNotNull(_frameProbe);
            int lastRequiredFrame = Time.frameCount + frameCount;
            double deadline = Time.realtimeSinceStartupAsDouble + 2d;
            // EditMode UnityTests still advance on EditorApplication.update in Play Mode.
            // A null yield alone does not establish a native PlayerLoop frame.
            while (_frameProbe.LateUpdateFrame < lastRequiredFrame &&
                   Time.realtimeSinceStartupAsDouble < deadline)
            {
                yield return null;
            }

            Assert.That(_frameProbe.UpdateFrame, Is.GreaterThanOrEqualTo(lastRequiredFrame),
                "The real PlayerLoop must execute Update on the runtime probe.");
            Assert.That(_frameProbe.LateUpdateFrame, Is.GreaterThanOrEqualTo(lastRequiredFrame),
                "The real PlayerLoop must execute LateUpdate on the runtime probe.");
        }

        private CoroutineExtensions.CoroutineHandle Track(CoroutineExtensions.CoroutineHandle handle)
        {
            if (_tearingDown)
            {
                handle?.Stop();
            }
            else
            {
                _handles.Add(handle);
            }

            return handle;
        }

        private static void AssertCompleted(CoroutineExtensions.CoroutineHandle handle)
        {
            Assert.IsFalse(handle.IsRunning);
            Assert.IsNull(handle.Coroutine);
        }

        private static IEnumerator CompleteImmediately(Action action)
        {
            action();
            yield break;
        }

        private static IEnumerator Enclose(IEnumerator inner, Action onDispose)
        {
            try
            {
                yield return inner;
            }
            finally
            {
                onDispose();
            }
        }

        private static IEnumerator WaitForCancellation(Action onDispose, Action onContinue, Action onEnter)
        {
            try
            {
                onEnter();
                yield return new WaitForSeconds(60f);
                onContinue();
            }
            finally
            {
                onDispose();
            }
        }

        private static IEnumerator StopAfterYield(Action action, Action onDispose)
        {
            try
            {
                yield return null;
                action();
            }
            finally
            {
                onDispose();
            }
        }

        private static IEnumerator WaitForInstruction(CustomYieldInstruction instruction)
        {
            yield return instruction;
        }

        private sealed class DisposableWait : CustomYieldInstruction, IDisposable
        {
            public int PollCount { get; private set; }
            public int DisposeCount { get; private set; }

            public override bool keepWaiting
            {
                get
                {
                    PollCount++;
                    return true;
                }
            }

            public void Dispose()
            {
                DisposeCount++;
            }
        }
    }
}
