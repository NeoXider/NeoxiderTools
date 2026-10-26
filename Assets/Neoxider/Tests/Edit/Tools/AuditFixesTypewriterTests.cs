using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Covers the TypewriterEffect run guard: a run that was cancelled by a restart must never
    ///     dispose the CancellationTokenSource installed by the run that replaced it.
    /// </summary>
    [TestFixture]
    public class AuditFixesTypewriterTests
    {
        private readonly List<TypewriterEffect> _effects = new List<TypewriterEffect>();
        private readonly List<object> _awaiters = new List<object>();

        // Consume the public awaiter protocol, including GetResult, rather than discarding the
        // returned asynchronous operation. UniTask is also referenced for the nonsettlement control.
        private object StartPlay(TypewriterEffect effect, string text, Action<string> onTextChanged,
            CancellationToken token = default)
        {
            MethodInfo play = typeof(TypewriterEffect).GetMethod("PlayAsync");
            Assert.That(play, Is.Not.Null, "TypewriterEffect.PlayAsync must exist");
            object operation = play.Invoke(effect, new object[] { text, onTextChanged, token });
            return TrackOperation(operation);
        }

        private object TrackOperation(object operation)
        {
            MethodInfo getAwaiter = operation.GetType().GetMethod("GetAwaiter", Type.EmptyTypes);
            Assert.That(getAwaiter, Is.Not.Null);
            object awaiter = getAwaiter.Invoke(operation, null);
            _awaiters.Add(awaiter);
            return awaiter;
        }

        [UnityTest]
        public IEnumerator PlayAsync_RestartedWhileTyping_KeepsTheNewRunAlive()
        {
            yield return new EnterPlayMode();
            // WHY: 0.1 chars/second => 10 s per character, so a run suspends on its first delay and
            // stays "typing" for the whole test.
            TypewriterEffect effect = new TypewriterEffect(0.1f);
            _effects.Add(effect);
            effect.UsePunctuationPauses = false;

            bool restarted = false;

            void OnFirstRunText(string _)
            {
                if (restarted)
                {
                    return;
                }

                restarted = true;
                StartPlay(effect, "second", __ => { });
            }

            try
            {
                StartPlay(effect, "first", OnFirstRunText);
                Assert.That(restarted, Is.True, "the first run must reach its text callback");
                Assert.That(effect.FullText, Is.EqualTo("second"));
                yield return null; // Let the cancelled first run finish on the actual player loop.
                Assert.That(effect.IsTyping, Is.True, "A retired run cannot release its replacement's source.");
                effect.Stop();
                string stoppedText = effect.CurrentText;
                yield return DrainOperations();
                yield return null;
                Assert.That(effect.IsTyping, Is.False);
                Assert.That(effect.CurrentText, Is.EqualTo(stoppedText), "No text may arrive after Stop.");
            }
            finally
            {
                effect.Stop();
            }
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator PlayAsync_ExternalCancellation_SettlesAndStopsTextCallbacks()
        {
            yield return new EnterPlayMode();
            TypewriterEffect effect = new TypewriterEffect(0.1f, true) { UsePunctuationPauses = false };
            _effects.Add(effect);
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            int callbacks = 0;
            int completed = 0;
            effect.OnComplete += () => completed++;
            try
            {
                StartPlay(effect, "cancel this run", text => callbacks++, cancellation.Token);
                Assert.That(effect.IsTyping, Is.True);
                Assert.That(callbacks, Is.GreaterThan(0), "Positive control: the run delivered text.");
                yield return null;
                cancellation.Cancel();
                yield return DrainOperations();
                int capturedCallbacks = callbacks;
                for (int frame = 0; frame < 3; frame++) yield return null;
                Assert.That(effect.IsTyping, Is.False);
                Assert.That(callbacks, Is.EqualTo(capturedCallbacks));
                Assert.That(completed, Is.Zero, "Cancellation is not successful completion.");
            }
            finally { effect.Stop(); }
            yield return new ExitPlayMode();
        }

        [Test]
        public void PlayAsync_CompletedRun_ReleasesItsOwnCancellationSource()
        {
            // WHY: 5000 chars/second rounds the per-character delay to 0 ms, so the run never suspends
            // and finishes inside the call.
            TypewriterEffect effect = new TypewriterEffect(5000f);
            _effects.Add(effect);
            effect.UsePunctuationPauses = false;

            try
            {
                object awaiter = StartPlay(effect, "done", _ => { });
                Assert.That(IsCompleted(awaiter), Is.True);
                _awaiters.Remove(awaiter);
                Complete(awaiter);
                Assert.That(effect.IsTyping, Is.False);
                Assert.That(effect.CurrentText, Is.EqualTo("done"));
            }
            finally
            {
                effect.Stop();
            }
        }

        [UnityTest]
        public IEnumerator Cleanup_FaultedTypewriter_PreservesGetResultFailureAndExitsPlayMode()
        {
            yield return new EnterPlayMode();
            TypewriterEffect faulting = new TypewriterEffect(5000f) { UsePunctuationPauses = false };
            TypewriterEffect running = new TypewriterEffect(0.1f, true) { UsePunctuationPauses = false };
            _effects.Add(faulting);
            _effects.Add(running);
            const string faultMessage = "typewriter character observer fault sentinel";
            faulting.OnCharacterTyped += character => throw new InvalidOperationException(faultMessage);
            ExceptionDispatchInfo failure = null;
            try
            {
                object faultedAwaiter = StartPlay(faulting, "fault", text => { });
                Assert.That(IsCompleted(faultedAwaiter), Is.True, "The actual SUT operation retains its asynchronous fault for GetResult.");
                StartPlay(running, "another owned run", text => { });
                Assert.That(running.IsTyping, Is.True);
                yield return StopAndObserveOwnedRuns(observed => failure = observed);
                if (Application.isPlaying) yield return new ExitPlayMode();
                Assert.That(Application.isPlaying, Is.False, "The yielded ExitPlayMode must complete before fault propagation.");
                Assert.That(_effects, Is.Empty);
                Assert.That(_awaiters, Is.Empty);
                Assert.That(running.IsTyping, Is.False, "Cleanup must continue to the later owned run after a fault.");
                Assert.That(failure, Is.Not.Null);
                InvalidOperationException preserved = Assert.Throws<InvalidOperationException>(() => failure.Throw());
                Assert.That(preserved.Message, Is.EqualTo(faultMessage));
            }
            finally
            {
                faulting.Stop();
                running.Stop();
            }
        }

        [UnityTest]
        public IEnumerator Cleanup_NonsettlingOperation_PreservesTimeoutAndExitsPlayMode()
        {
            yield return new EnterPlayMode();
            UniTaskCompletionSource pending = new UniTaskCompletionSource();
            object pendingAwaiter = TrackOperation(pending.Task);
            TypewriterEffect running = new TypewriterEffect(0.1f, true) { UsePunctuationPauses = false };
            _effects.Add(running);
            ExceptionDispatchInfo failure = null;
            try
            {
                Assert.That(IsCompleted(pendingAwaiter), Is.False, "Positive control: this real awaitable has not settled.");
                StartPlay(running, "owned cancellation alongside nonsettlement", text => { });
                Assert.That(running.IsTyping, Is.True);
                yield return StopAndObserveOwnedRuns(observed => failure = observed, 0.05d);
                if (Application.isPlaying) yield return new ExitPlayMode();
                Assert.That(Application.isPlaying, Is.False);
                Assert.That(_effects, Is.Empty);
                Assert.That(_awaiters, Is.Empty);
                Assert.That(running.IsTyping, Is.False);
                Assert.That(failure, Is.Not.Null);
                AssertionException preserved = Assert.Throws<AssertionException>(() => failure.Throw());
                Assert.That(preserved.Message, Does.Contain("did not settle"));
            }
            finally
            {
                // Release this deliberately stalled control without leaving an operation retained by teardown.
                pending.TrySetResult();
                _awaiters.Remove(pendingAwaiter);
                Complete(pendingAwaiter);
                running.Stop();
            }
        }

        [UnityTearDown]
        public IEnumerator StopAndObserveEveryRun()
        {
            ExceptionDispatchInfo failure = null;
            yield return StopAndObserveOwnedRuns(observed => failure = observed);
            if (Application.isPlaying) yield return new ExitPlayMode();
            failure?.Throw();
        }

        // Capture faults while draining every owned operation. Each top-level UnityTest/UnityTearDown
        // yields its own ExitPlayMode before inspecting/rethrowing this failure; UTF rejects nested mode exits.
        private IEnumerator StopAndObserveOwnedRuns(Action<ExceptionDispatchInfo> reportFailure, double timeoutSeconds = 3d)
        {
            ExceptionDispatchInfo failure = null;
            double deadline = Time.realtimeSinceStartupAsDouble + timeoutSeconds;
            try
            {
                foreach (TypewriterEffect effect in _effects)
                {
                    try { effect.Stop(); }
                    catch (Exception exception) { failure ??= ExceptionDispatchInfo.Capture(exception); }
                }
                while (_awaiters.Count > 0)
                {
                    object awaiter = _awaiters[0];
                    bool completed;
                    try { completed = IsCompleted(awaiter); }
                    catch (Exception exception)
                    {
                        failure ??= ExceptionDispatchInfo.Capture(exception);
                        _awaiters.RemoveAt(0);
                        continue;
                    }
                    if (!completed && Time.realtimeSinceStartupAsDouble < deadline)
                    {
                        yield return null;
                        continue;
                    }
                    _awaiters.RemoveAt(0);
                    if (!completed)
                    {
                        failure ??= ExceptionDispatchInfo.Capture(new AssertionException("Cancelled typewriter did not settle before cleanup deadline."));
                        continue;
                    }
                    try { Complete(awaiter); }
                    catch (Exception exception) { failure ??= ExceptionDispatchInfo.Capture(exception); }
                }
            }
            finally
            {
                _effects.Clear();
                _awaiters.Clear();
            }
            reportFailure(failure);
        }

        private IEnumerator DrainOperations()
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 3d;
            while (_awaiters.Count > 0)
            {
                object awaiter = _awaiters[0];
                while (!IsCompleted(awaiter))
                {
                    Assert.That(Time.realtimeSinceStartupAsDouble, Is.LessThan(deadline), "Cancelled typewriter did not settle.");
                    yield return null;
                }
                _awaiters.RemoveAt(0);
                Complete(awaiter);
            }
        }

        private static bool IsCompleted(object awaiter)
        {
            PropertyInfo completed = awaiter.GetType().GetProperty("IsCompleted");
            Assert.That(completed, Is.Not.Null);
            return (bool)completed.GetValue(awaiter);
        }

        private static void Complete(object awaiter)
        {
            MethodInfo getResult = awaiter.GetType().GetMethod("GetResult", Type.EmptyTypes);
            Assert.That(getResult, Is.Not.Null);
            try { getResult.Invoke(awaiter, null); }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            }
        }
    }
}
