using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Neo.Extensions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    public sealed partial class Audit20261023CoroutineUnityLifecycleTests
    {
        private float _previousTimeScale;
        private float _previousFixedDeltaTime;

        [UnityTest]
        public IEnumerator RawStopCoroutine_ThenTrackedStop_ReleasesTheNestedTreeOnce()
        {
            yield return RawStopScenario(false);
        }

        [UnityTest]
        public IEnumerator RawStopAllCoroutines_ThenTrackedStop_ReleasesTheNestedTreeOnce()
        {
            yield return RawStopScenario(true);
        }

        [UnityTest]
        public IEnumerator NativeStopDuringMoveNext_DefersDisposalUntilAdvanceReturns()
        {
            yield return NativeAdvanceScenario(false, false);
        }

        [UnityTest]
        public IEnumerator NativeStopDuringCurrent_DefersDisposalUntilAdvanceReturns()
        {
            yield return NativeAdvanceScenario(true, false);
        }

        [UnityTest]
        public IEnumerator HierarchyDeactivationDuringCurrent_DefersDisposalUntilAdvanceReturns()
        {
            yield return NativeAdvanceScenario(true, true);
        }

        [UnityTest]
        public IEnumerator Reset_CleanupCannotStartReplacementWorkOnTheClosingHost()
        {
            int cleanup = 0;
            int replacementCalls = 0;
            CoroutineExtensions.CoroutineHandle replacement = null;
            CoroutineExtensions.CoroutineHandle original = Track(CoroutineExtensions.Start(
                Enclose(WaitForInstruction(new WaitUntil(() => false)), () =>
                {
                    cleanup++;
                    replacement = Track(CoroutineExtensions.DelayFrames(0, () => replacementCalls++));
                })));
            yield return AwaitNativeFrames(1);
            int hostsBeforeReset = Object.FindObjectsByType<CoroutineHelper>(
                FindObjectsInactive.Include, FindObjectsSortMode.None).Length;

            // Drive the actual subsystem lifecycle hook, not private handle state.
            MethodInfo reset = typeof(CoroutineExtensions).GetMethod("ResetStaticState",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(reset);
            reset.Invoke(null, null);

            AssertCompleted(original);
            Assert.IsNotNull(replacement);
            AssertCompleted(replacement);
            Assert.AreEqual(1, cleanup);
            Assert.AreEqual(0, replacementCalls);
            Assert.AreEqual(hostsBeforeReset, Object.FindObjectsByType<CoroutineHelper>(
                FindObjectsInactive.Include, FindObjectsSortMode.None).Length);
            yield return AwaitNativeFrames(1);
            Assert.AreEqual(0, replacementCalls);
        }

        [UnityTest]
        public IEnumerator CleanupFault_StillReleasesTheParentAndDoesNotReplayCleanup()
        {
            List<string> cleanup = new();
            ThrowingDisposeRoutine inner = new(cleanup);
            CoroutineExtensions.CoroutineHandle handle = Track(CoroutineExtensions.Start(
                Enclose(inner, () => cleanup.Add("parent"))));
            for (int frame = 0; frame < 6 && inner.MoveCount == 0; frame++)
            {
                yield return AwaitNativeFrames(1);
            }

            Assert.That(inner.MoveCount, Is.GreaterThan(0));
            bool errorsWereEnabled = NeoDiagnostics.RuntimeErrorsEnabled;
            try
            {
                NeoDiagnostics.Configure(errors: true);
                LogAssert.Expect(LogType.Exception,
                    new Regex("InvalidOperationException: Audit20261023 cleanup fault"));
                handle.Stop();
                handle.Stop();
            }
            finally
            {
                NeoDiagnostics.Configure(errors: errorsWereEnabled);
            }

            AssertCompleted(handle);
            Assert.AreEqual(1, inner.DisposeCount);
            CollectionAssert.AreEqual(new[] { "inner", "parent" }, cleanup);
        }

        [UnityTest]
        public IEnumerator CustomWaitPredicates_RunAfterUpdateAndBeforeLateUpdate()
        {
            CoroutineRunner owner = CreateOwner();
            Audit20261023CoroutineUnityPhaseProbe probe = CreatePhaseProbe(owner.gameObject);
            yield return AwaitNativeFrames(1);
            int firstEligibleFrame = Time.frameCount + 1;
            int calls = 0;
            List<bool> phases = new();
            List<int> predicateFrames = new();
            List<int> callbackFrames = new();
            List<bool> callbackPhases = new();
            Func<bool> ready = () =>
            {
                if (Time.frameCount < firstEligibleFrame)
                {
                    return false;
                }

                phases.Add(probe.UpdateFrame == Time.frameCount && probe.LateUpdateFrame != Time.frameCount &&
                           probe.LastPhase == "Update");
                predicateFrames.Add(Time.frameCount);
                return true;
            };
            Action callback = () =>
            {
                calls++;
                callbackFrames.Add(Time.frameCount);
                callbackPhases.Add(probe.UpdateFrame == Time.frameCount && probe.LateUpdateFrame != Time.frameCount &&
                                   probe.LastPhase == "Update");
            };
            CoroutineExtensions.CoroutineHandle until = Track(owner.WaitUntil(ready, callback));
            CoroutineExtensions.CoroutineHandle whileHandle = Track(owner.WaitWhile(() => !ready(), callback));
            yield return AwaitCompletion(until);
            yield return AwaitCompletion(whileHandle);

            Assert.AreEqual(2, calls);
            Assert.That(phases.Count, Is.GreaterThanOrEqualTo(2));
            Assert.IsTrue(phases.TrueForAll(correct => correct));
            Assert.AreEqual(2, callbackPhases.Count);
            Assert.IsTrue(callbackPhases.TrueForAll(correct => correct));
            CollectionAssert.AreEqual(predicateFrames, callbackFrames,
                "The wrapper must not postpone callbacks beyond the frame that satisfies each wait.");
        }

        [UnityTest]
        public IEnumerator FixedFrameDelay_ResumesAfterFixedUpdateBeforeUpdate()
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            CoroutineRunner owner = CreateOwner();
            Audit20261023CoroutineUnityPhaseProbe probe = CreatePhaseProbe(owner.gameObject);
            yield return AwaitNativeFrames(1);
            int initialFixedCount = probe.FixedCount;
            int fixedCountAtCallback = -1;
            string phase = null;
            CoroutineExtensions.CoroutineHandle handle = Track(owner.DelayFrames(1, () =>
            {
                fixedCountAtCallback = probe.FixedCount;
                phase = probe.LastPhase;
            }, useFixedUpdate: true));

            yield return AwaitCompletion(handle);

            Assert.That(fixedCountAtCallback, Is.GreaterThan(initialFixedCount));
            Assert.AreEqual("FixedUpdate", phase);
        }

        [UnityTest]
        public IEnumerator EndOfFrame_ResumesAfterLateUpdateInTheSameFrame()
        {
            if (Application.isBatchMode)
            {
                Assert.Ignore("Native end-of-frame acceptance requires a render-capable interactive Editor run; " +
                              "batch execution does not supply this acceptance evidence.");
            }

            CoroutineRunner owner = CreateOwner();
            Audit20261023CoroutineUnityPhaseProbe probe = CreatePhaseProbe(owner.gameObject);
            yield return AwaitNativeFrames(1);
            int calls = 0;
            int frameAtCallback = -1;
            int lateFrameAtCallback = -1;
            string phase = null;
            CoroutineExtensions.CoroutineHandle handle = Track(owner.EndOfFrame(() =>
            {
                calls++;
                frameAtCallback = Time.frameCount;
                lateFrameAtCallback = probe.LateUpdateFrame;
                phase = probe.LastPhase;
            }));

            yield return AwaitCompletion(handle);

            Assert.AreEqual(1, calls);
            Assert.AreEqual(frameAtCallback, lateFrameAtCallback);
            Assert.AreEqual("LateUpdate", phase);
            yield return AwaitNativeFrames(1);
            Assert.AreEqual(1, calls);
        }

        [UnityTest]
        public IEnumerator PausedTime_OnlyTheUnscaledDelayCompletesUntilResume()
        {
            CoroutineRunner owner = CreateOwner();
            int scaledCalls = 0;
            int unscaledCalls = 0;
            Time.timeScale = 0f;
            CoroutineExtensions.CoroutineHandle scaled = Track(owner.Delay(0.02f, () => scaledCalls++));
            CoroutineExtensions.CoroutineHandle unscaled = Track(owner.Delay(0.02f, () => unscaledCalls++, true));

            yield return AwaitCompletion(unscaled);

            Assert.AreEqual(1, unscaledCalls);
            Assert.AreEqual(0, scaledCalls);
            Assert.IsTrue(scaled.IsRunning);
            Time.timeScale = 1f;
            yield return AwaitCompletion(scaled);
            Assert.AreEqual(1, scaledCalls);
            Assert.AreEqual(1, unscaledCalls);
        }

        [UnityTest]
        public IEnumerator PreviewCache_PlayModeClear_DefersOwnedSpriteDestructionAndPreservesTexture()
        {
            GameObject source = CreateOwner().gameObject;
            Sprite preview = source.GetPreviewSprite();
            Assert.IsNotNull(preview, "The editor mini-thumbnail is required for this native ownership case.");
            Texture2D texture = preview.texture;

            PrefabPreviewExtensions.ClearPreviewSpriteCache();

            Assert.IsTrue(preview != null, "Destroy must remain deferred in Play Mode.");
            yield return AwaitNativeFrames(2);
            Assert.IsTrue(preview == null);
            Assert.IsTrue(texture != null, "The preview texture is borrowed from AssetPreview.");
        }

        private IEnumerator RawStopScenario(bool stopAll)
        {
            List<string> cleanup = new();
            bool entered = false;
            int continued = 0;
            CoroutineExtensions.CoroutineHandle handle = Track(CoroutineExtensions.Start(
                Enclose(WaitForCancellation(() => cleanup.Add("inner"), () => continued++, () => entered = true),
                    () => cleanup.Add("parent"))));
            for (int frame = 0; frame < 6 && !entered; frame++)
            {
                yield return AwaitNativeFrames(1);
            }

            Assert.IsTrue(entered);
            CoroutineHelper host = GetGlobalHost();
            Coroutine native = handle.Coroutine;
            Assert.IsNotNull(native);
            if (stopAll)
            {
                host.StopAllCoroutines();
            }
            else
            {
                host.StopCoroutine(native);
            }

            // Native unwinding differs: do not assume raw stop itself updates the tracked state.
            handle.Stop();
            handle.Stop();
            AssertCompleted(handle);
            CollectionAssert.AreEqual(new[] { "inner", "parent" }, cleanup);
            yield return AwaitNativeFrames(1);
            Assert.AreEqual(0, continued);
            CollectionAssert.AreEqual(new[] { "inner", "parent" }, cleanup);
        }

        private IEnumerator NativeAdvanceScenario(bool duringCurrent, bool deactivate)
        {
            NativeCancellationRoutine routine = new(duringCurrent);
            CoroutineExtensions.CoroutineHandle handle = Track(CoroutineExtensions.Start(routine));
            CoroutineHelper host = GetGlobalHost();
            routine.Cancel = () =>
            {
                if (deactivate)
                {
                    host.gameObject.SetActive(false);
                }
                else
                {
                    host.StopCoroutine(handle.Coroutine);
                    handle.Stop();
                }
            };
            try
            {
                yield return AwaitCompletion(handle);
                Assert.AreEqual(1, routine.CancelCount);
                Assert.AreEqual(1, routine.DisposeCount);
                Assert.IsFalse(routine.DisposedDuringAdvance);
                Assert.AreEqual("dispose", routine.Trace[routine.Trace.Count - 1]);
                Assert.That(routine.Trace.IndexOf("dispose"), Is.GreaterThan(routine.Trace.IndexOf("exit")));
            }
            finally
            {
                if (host != null && deactivate)
                {
                    host.gameObject.SetActive(true);
                }
            }
        }

        private static IEnumerator AwaitCompletion(CoroutineExtensions.CoroutineHandle handle)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 2d;
            while (handle.IsRunning && Time.realtimeSinceStartupAsDouble < deadline)
            {
                yield return null;
            }

            AssertCompleted(handle);
        }

        private static CoroutineHelper GetGlobalHost()
        {
            // Identify the actual owner for a native cancellation call; never synthesize handle state.
            FieldInfo field = typeof(CoroutineExtensions).GetField("instance",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            CoroutineHelper host = (CoroutineHelper)field.GetValue(null);
            Assert.IsNotNull(host);
            return host;
        }

        private sealed class NativeCancellationRoutine : IEnumerator, IDisposable
        {
            private readonly bool _duringCurrent;
            private int _moves;
            private bool _inAdvance;

            public NativeCancellationRoutine(bool duringCurrent)
            {
                _duringCurrent = duringCurrent;
            }

            public Action Cancel { get; set; }
            public int CancelCount { get; private set; }
            public int DisposeCount { get; private set; }
            public bool DisposedDuringAdvance { get; private set; }
            public List<string> Trace { get; } = new();

            public object Current
            {
                get
                {
                    if (_moves > 1 && _duringCurrent)
                    {
                        CancelInsideAdvance();
                    }

                    return null;
                }
            }

            public bool MoveNext()
            {
                _moves++;
                if (_moves > 1 && !_duringCurrent)
                {
                    CancelInsideAdvance();
                }

                return true;
            }

            private void CancelInsideAdvance()
            {
                _inAdvance = true;
                Trace.Add("enter");
                try
                {
                    CancelCount++;
                    Cancel();
                    Trace.Add("after cancellation");
                }
                finally
                {
                    _inAdvance = false;
                    Trace.Add("exit");
                }
            }

            public void Dispose()
            {
                DisposedDuringAdvance |= _inAdvance;
                DisposeCount++;
                Trace.Add("dispose");
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }
        }

        private sealed class ThrowingDisposeRoutine : IEnumerator, IDisposable
        {
            private readonly List<string> _cleanup;

            public ThrowingDisposeRoutine(List<string> cleanup)
            {
                _cleanup = cleanup;
            }

            public int MoveCount { get; private set; }
            public int DisposeCount { get; private set; }
            public object Current => null;

            public bool MoveNext()
            {
                MoveCount++;
                return true;
            }

            public void Dispose()
            {
                DisposeCount++;
                _cleanup.Add("inner");
                throw new InvalidOperationException("Audit20261023 cleanup fault");
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }
        }
    }
}
