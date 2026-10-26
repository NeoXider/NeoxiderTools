using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Neo.Extensions
{
    /// <summary>
    ///     Extension methods for running delayed and conditional coroutine actions.
    /// </summary>
    public static class CoroutineExtensions
    {
        private static CoroutineHelper instance;
        private static bool resetting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            resetting = true;
            try
            {
                if (instance != null)
                {
                    instance.StopAllTrackedCoroutines();
                }
            }
            finally
            {
                instance = null;
                resetting = false;
            }
        }

        private static CoroutineHelper Instance
        {
            get
            {
                if (resetting)
                {
                    return null;
                }

                if (instance == null)
                {
                    GameObject go = new("[CoroutineHelper]");
                    instance = go.AddComponent<CoroutineHelper>();
                    GameObject.DontDestroyOnLoad(go);
                }

                return instance;
            }
        }

        /// <summary>
        ///     Handle for controlling and tracking a started coroutine.
        /// </summary>
        public class CoroutineHandle
        {
            private readonly MonoBehaviour owner;
            private Coroutine coroutine;
            private CoroutineLifecycleTracker tracker;
            private readonly List<IEnumerator> routines = new();
            private CancellationTokenRegistration destructionRegistration;
            private bool completed;
            private bool advancing;

            /// <summary>
            ///     Initializes a new coroutine handle.
            /// </summary>
            internal CoroutineHandle(MonoBehaviour owner)
            {
                this.owner = owner;
                IsRunning = false;
            }

            /// <summary>
            ///     Current Unity coroutine for interoperation. Use Stop() for tracked cancellation and disposal.
            /// </summary>
            public Coroutine Coroutine
            {
                get => coroutine;
                internal set
                {
                    if (completed)
                    {
                        return;
                    }

                    coroutine = value;
                    IsRunning = true;
                }
            }

            /// <summary>
            ///     Tracked running state. Direct external StopCoroutine/StopAllCoroutines calls are not observable.
            /// </summary>
            public bool IsRunning { get; private set; }

            /// <summary>
            ///     Stops the coroutine if it is running.
            /// </summary>
            public void Stop()
            {
                if (!IsRunning)
                {
                    return;
                }

                Coroutine runningCoroutine = coroutine;
                Complete();
                if (owner != null && runningCoroutine != null)
                {
                    owner.StopCoroutine(runningCoroutine);
                }
            }

            internal void Complete()
            {
                if (completed)
                {
                    return;
                }

                completed = true;
                IsRunning = false;
                tracker?.Unregister(this);
                tracker = null;
                coroutine = null;
                destructionRegistration.Dispose();
                DisposeRoutines();
            }

            internal void BindTracker(CoroutineLifecycleTracker lifecycleTracker)
            {
                tracker = lifecycleTracker;
                tracker?.Register(this);
                if (owner != null)
                {
                    destructionRegistration = owner.destroyCancellationToken.Register(Complete);
                }
            }

            internal void TrackRoutine(IEnumerator routine)
            {
                routines.Add(routine);
            }

            internal bool Advance(IEnumerator routine, out object current)
            {
                current = null;
                if (completed)
                {
                    return false;
                }

                advancing = true;
                try
                {
                    bool moved = routine.MoveNext();
                    if (moved && !completed)
                    {
                        current = routine.Current;
                    }

                    return moved && !completed;
                }
                catch
                {
                    Complete();
                    throw;
                }
                finally
                {
                    advancing = false;
                    if (completed)
                    {
                        DisposeRoutines();
                    }
                }
            }

            internal void ReleaseRoutine(IEnumerator routine)
            {
                if (advancing)
                {
                    // Native cancellation may finalize a wrapper inside MoveNext/Current.
                    // Keep its iterators tracked until Advance unwinds before disposing them.
                    Complete();
                    return;
                }

                for (int index = routines.Count - 1; index >= 0; index--)
                {
                    if (ReferenceEquals(routines[index], routine))
                    {
                        routines.RemoveAt(index);
                        DisposeRoutine(routine);
                        return;
                    }
                }
            }

            private void DisposeRoutines()
            {
                if (advancing)
                {
                    return;
                }

                while (routines.Count > 0)
                {
                    IEnumerator routine = routines[routines.Count - 1];
                    routines.RemoveAt(routines.Count - 1);
                    DisposeRoutine(routine);
                }
            }

            private static void DisposeRoutine(IEnumerator routine)
            {
                try
                {
                    (routine as IDisposable)?.Dispose();
                }
                catch (Exception exception)
                {
                    NeoDiagnostics.LogException(exception);
                }
            }
        }

        #region Extension Methods

        /// <summary>
        ///     Executes an action after a specified delay in seconds
        /// </summary>
        public static CoroutineHandle Delay(this MonoBehaviour monoBehaviour, float seconds, Action action,
            bool useUnscaledTime = false)
        {
            return StartDelayedCoroutine(monoBehaviour, DelayedAction(seconds, action, useUnscaledTime));
        }

        /// <summary>
        ///     Waits until a condition is true, then executes an action
        /// </summary>
        public static CoroutineHandle WaitUntil(this MonoBehaviour monoBehaviour, Func<bool> predicate, Action action)
        {
            return StartDelayedCoroutine(monoBehaviour, WaitUntilAction(predicate, action));
        }

        /// <summary>
        ///     Waits while a condition is true, then executes an action
        /// </summary>
        public static CoroutineHandle WaitWhile(this MonoBehaviour monoBehaviour, Func<bool> predicate, Action action)
        {
            return StartDelayedCoroutine(monoBehaviour, WaitWhileAction(predicate, action));
        }

        /// <summary>
        ///     Executes an action after a specified number of frames
        /// </summary>
        public static CoroutineHandle DelayFrames(this MonoBehaviour monoBehaviour, int frameCount, Action action,
            bool useFixedUpdate = false)
        {
            return StartDelayedCoroutine(monoBehaviour, DelayedFramesAction(frameCount, action, useFixedUpdate));
        }

        /// <summary>
        ///     Executes an action on the next frame
        /// </summary>
        public static CoroutineHandle NextFrame(this MonoBehaviour monoBehaviour, Action action)
        {
            return monoBehaviour.DelayFrames(1, action);
        }

        /// <summary>
        ///     Executes an action at the end of the current frame
        /// </summary>
        public static CoroutineHandle EndOfFrame(this MonoBehaviour monoBehaviour, Action action)
        {
            return StartDelayedCoroutine(monoBehaviour, EndOfFrameAction(action));
        }

        /// <summary>
        ///     Repeats an action every frame until a condition is met
        /// </summary>
        public static CoroutineHandle RepeatUntil(this MonoBehaviour monoBehaviour, Func<bool> condition, Action action)
        {
            return StartDelayedCoroutine(monoBehaviour, RepeatUntilAction(condition, action));
        }

        #region GameObject Extensions

        /// <summary>
        ///     Executes an action after a delay using a coroutine runner on the target GameObject.
        /// </summary>
        public static CoroutineHandle Delay(this GameObject gameObject, float seconds, Action action,
            bool useUnscaledTime = false)
        {
            MonoBehaviour owner = GetOrAddCoroutineComponent(gameObject);
            return owner.Delay(seconds, action, useUnscaledTime);
        }

        /// <summary>
        ///     Waits until predicate is true, then executes an action using a runner on the GameObject.
        /// </summary>
        public static CoroutineHandle WaitUntil(this GameObject gameObject, Func<bool> predicate, Action action)
        {
            MonoBehaviour owner = GetOrAddCoroutineComponent(gameObject);
            return owner.WaitUntil(predicate, action);
        }

        /// <summary>
        ///     Waits while predicate is true, then executes an action using a runner on the GameObject.
        /// </summary>
        public static CoroutineHandle WaitWhile(this GameObject gameObject, Func<bool> predicate, Action action)
        {
            MonoBehaviour owner = GetOrAddCoroutineComponent(gameObject);
            return owner.WaitWhile(predicate, action);
        }

        /// <summary>
        ///     Executes an action after a number of frames using a runner on the target GameObject.
        /// </summary>
        public static CoroutineHandle DelayFrames(this GameObject gameObject, int frameCount, Action action,
            bool useFixedUpdate = false)
        {
            MonoBehaviour owner = GetOrAddCoroutineComponent(gameObject);
            return owner.DelayFrames(frameCount, action, useFixedUpdate);
        }

        #endregion

        #region Global Methods

        /// <summary>
        ///     Executes an action after a delay using global coroutine helper instance.
        /// </summary>
        public static CoroutineHandle Delay(float seconds, Action action, bool useUnscaledTime = false)
        {
            return Instance.Delay(seconds, action, useUnscaledTime);
        }

        /// <summary>
        ///     Waits until predicate is true, then executes an action using global helper.
        /// </summary>
        public static CoroutineHandle WaitUntil(Func<bool> predicate, Action action)
        {
            return Instance.WaitUntil(predicate, action);
        }

        /// <summary>
        ///     Waits while predicate is true, then executes an action using global helper.
        /// </summary>
        public static CoroutineHandle WaitWhile(Func<bool> predicate, Action action)
        {
            return Instance.WaitWhile(predicate, action);
        }

        /// <summary>
        ///     Executes an action after a number of frames using global helper.
        /// </summary>
        public static CoroutineHandle DelayFrames(int frameCount, Action action, bool useFixedUpdate = false)
        {
            return Instance.DelayFrames(frameCount, action, useFixedUpdate);
        }

        /// <summary>
        ///     Starts a custom coroutine and returns a handle to it.
        /// </summary>
        /// <param name="routine">The IEnumerator routine to start.</param>
        /// <returns>A handle to the running coroutine, allowing it to be stopped.</returns>
        public static CoroutineHandle Start(IEnumerator routine)
        {
            if (routine == null)
            {
                return new CoroutineHandle(null);
            }

            return StartDelayedCoroutine(Instance, routine);
        }

        #endregion

        #endregion

        #region Helper Methods

        private static CoroutineHandle StartDelayedCoroutine(MonoBehaviour owner, IEnumerator routine)
        {
            if (routine == null)
            {
                return new CoroutineHandle(null);
            }

            if (resetting)
            {
                CoroutineHandle cancelled = new(null);
                cancelled.TrackRoutine(routine);
                cancelled.Complete();
                return cancelled;
            }

            if (owner == null)
            {
                NeoDiagnostics.LogWarning(
                    "Attempting to start coroutine on null MonoBehaviour. Falling back to CoroutineHelper instance.");
                owner = Instance;
            }

            CoroutineHandle handle = new(owner);
            if (!owner.gameObject.activeInHierarchy)
            {
                handle.TrackRoutine(routine);
                handle.Complete();
                return handle;
            }

            handle.BindTracker(GetOrAddLifecycleTracker(owner));
            handle.Coroutine = null;
            try
            {
                Coroutine started = owner.StartCoroutine(WrapCoroutine(routine, handle));
                if (started == null)
                {
                    handle.Complete();
                }
                else
                {
                    handle.Coroutine = started;
                }
            }
            catch
            {
                handle.Complete();
                throw;
            }

            return handle;
        }

        private static IEnumerator WrapCoroutine(IEnumerator routine, CoroutineHandle handle, bool completeHandle = true)
        {
            handle.TrackRoutine(routine);
            return RunTrackedCoroutine(routine, handle, completeHandle);
        }

        private static IEnumerator RunTrackedCoroutine(IEnumerator routine, CoroutineHandle handle, bool completeHandle)
        {
            try
            {
                while (handle.Advance(routine, out object current))
                {
                    if (current is CustomYieldInstruction instruction)
                    {
                        // Keep Unity's polling phase while tracking predicate faults and disposal.
                        yield return new TrackedYieldInstruction(instruction, handle);
                    }
                    else
                    {
                        yield return current is IEnumerator nested ? WrapCoroutine(nested, handle, false) : current;
                    }
                }
            }
            finally
            {
                if (completeHandle)
                {
                    // A native stop may finalize the root before its nested wrappers.
                    // Complete the tracked tree first so cleanup keeps child-to-parent order.
                    handle.Complete();
                }

                handle.ReleaseRoutine(routine);
            }
        }

        private sealed class TrackedYieldInstruction : CustomYieldInstruction
        {
            private readonly CustomYieldInstruction instruction;
            private readonly CoroutineHandle handle;

            internal TrackedYieldInstruction(CustomYieldInstruction instruction, CoroutineHandle handle)
            {
                this.instruction = instruction;
                this.handle = handle;
                handle.TrackRoutine(instruction);
            }

            public override bool keepWaiting
            {
                get
                {
                    bool waiting = handle.Advance(instruction, out _);
                    if (!waiting)
                    {
                        handle.ReleaseRoutine(instruction);
                    }

                    return waiting;
                }
            }
        }

        private static MonoBehaviour GetOrAddCoroutineComponent(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return Instance;
            }

            CoroutineRunner runner = gameObject.GetComponent<CoroutineRunner>();
            if (runner == null)
            {
                runner = gameObject.AddComponent<CoroutineRunner>();
            }

            return runner;
        }

        private static CoroutineLifecycleTracker GetOrAddLifecycleTracker(MonoBehaviour owner)
        {
            if (owner == null || owner.gameObject == null)
            {
                return null;
            }

            CoroutineLifecycleTracker tracker = owner.GetComponent<CoroutineLifecycleTracker>();
            if (tracker == null)
            {
                tracker = owner.gameObject.AddComponent<CoroutineLifecycleTracker>();
            }

            return tracker;
        }

        private static IEnumerator DelayedAction(float seconds, Action action, bool useUnscaledTime)
        {
            if (useUnscaledTime)
            {
                yield return new WaitForSecondsRealtime(seconds);
            }
            else
            {
                yield return new WaitForSeconds(seconds);
            }

            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                NeoDiagnostics.LogError($"Error executing delayed action: {e}");
            }
        }

        private static IEnumerator DelayedFramesAction(int frameCount, Action action, bool useFixedUpdate)
        {
            if (useFixedUpdate)
            {
                for (int i = 0; i < frameCount; i++)
                {
                    yield return new WaitForFixedUpdate();
                }
            }
            else
            {
                for (int i = 0; i < frameCount; i++)
                {
                    yield return null;
                }
            }

            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                NeoDiagnostics.LogError($"Error executing delayed frames action: {e}");
            }
        }

        private static IEnumerator WaitUntilAction(Func<bool> predicate, Action action)
        {
            yield return new WaitUntil(predicate);
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                NeoDiagnostics.LogError($"Error executing WaitUntil action: {e}");
            }
        }

        private static IEnumerator WaitWhileAction(Func<bool> predicate, Action action)
        {
            yield return new WaitWhile(predicate);
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                NeoDiagnostics.LogError($"Error executing WaitWhile action: {e}");
            }
        }

        private static IEnumerator EndOfFrameAction(Action action)
        {
            yield return new WaitForEndOfFrame();
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                NeoDiagnostics.LogError($"Error executing EndOfFrame action: {e}");
            }
        }

        private static IEnumerator RepeatUntilAction(Func<bool> condition, Action action)
        {
            while (!condition())
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception e)
                {
                    NeoDiagnostics.LogError($"Error executing RepeatUntil action: {e}");
                    yield break;
                }

                yield return null;
            }
        }

        #endregion
    }

    /// <summary>
    ///     Internal helper MonoBehaviour used as coroutine owner on regular GameObjects.
    /// </summary>
    [NeoDoc("Extensions/README.md")]
    [AddComponentMenu("")]
    public class CoroutineRunner : MonoBehaviour
    {
    }

    /// <summary>
    ///     Global helper MonoBehaviour used for coroutines without explicit owner.
    /// </summary>
    [NeoDoc("Extensions/README.md")]
    [AddComponentMenu("")]
    public class CoroutineHelper : MonoBehaviour
    {
        internal void StopAllTrackedCoroutines()
        {
            StopAllCoroutines();
            CoroutineLifecycleTracker tracker = GetComponent<CoroutineLifecycleTracker>();
            tracker?.CompleteAll();
        }
    }

    /// <summary>
    ///     Completes coroutine handles when their owning GameObject is deactivated or destroyed.
    /// </summary>
    [NeoDoc("Extensions/README.md")]
    [AddComponentMenu("")]
    public class CoroutineLifecycleTracker : MonoBehaviour
    {
        private readonly HashSet<CoroutineExtensions.CoroutineHandle> _handles = new();

        internal void Register(CoroutineExtensions.CoroutineHandle handle)
        {
            if (handle != null)
            {
                _handles.Add(handle);
            }
        }

        internal void Unregister(CoroutineExtensions.CoroutineHandle handle)
        {
            if (handle != null)
            {
                _handles.Remove(handle);
            }
        }

        internal void CompleteAll()
        {
            if (_handles.Count == 0)
            {
                return;
            }

            List<CoroutineExtensions.CoroutineHandle> handles = new(_handles);
            _handles.Clear();

            foreach (CoroutineExtensions.CoroutineHandle handle in handles)
            {
                handle?.Stop();
            }
        }

        private void OnDestroy()
        {
            CompleteAll();
        }

        private void OnDisable()
        {
            if (!gameObject.activeInHierarchy)
            {
                CompleteAll();
            }
        }
    }
}
