using System;
using System.Collections;
using UnityEngine;

namespace Neo.Extensions
{
    /// <summary>
    ///     Extension methods for running delayed and conditional coroutine actions.
    /// </summary>
    public static class CoroutineExtensions
    {
        private static CoroutineHelper instance;

        private static CoroutineHelper Instance
        {
            get
            {
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

            /// <summary>
            ///     Initializes a new coroutine handle.
            /// </summary>
            internal CoroutineHandle(MonoBehaviour owner)
            {
                this.owner = owner;
                IsRunning = false;
            }

            /// <summary>
            ///     Current coroutine instance associated with this handle.
            /// </summary>
            public Coroutine Coroutine
            {
                get => coroutine;
                internal set
                {
                    coroutine = value;
                    IsRunning = true;
                }
            }

            /// <summary>
            ///     Indicates whether coroutine is currently running.
            /// </summary>
            public bool IsRunning { get; private set; }

            /// <summary>
            ///     Stops the coroutine if it is running.
            /// </summary>
            public void Stop()
            {
                if (!IsRunning || owner == null)
                {
                    return;
                }

                if (coroutine != null)
                {
                    owner.StopCoroutine(coroutine);
                    coroutine = null;
                }

                IsRunning = false;
            }

            internal void Complete()
            {
                coroutine = null;
                IsRunning = false;
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
            return StartDelayedCoroutine(Instance, routine);
        }

        #endregion

        #endregion

        #region Helper Methods

        private static CoroutineHandle StartDelayedCoroutine(MonoBehaviour owner, IEnumerator routine)
        {
            if (owner == null)
            {
                Debug.LogWarning(
                    "Attempting to start coroutine on null MonoBehaviour. Falling back to CoroutineHelper instance.");
                owner = Instance;
            }

            CoroutineHandle handle = new(owner);
            handle.Coroutine = owner.StartCoroutine(WrapCoroutine(routine, handle));
            return handle;
        }

        private static IEnumerator WrapCoroutine(IEnumerator routine, CoroutineHandle handle)
        {
            yield return routine;
            handle.Complete();
        }

        private static MonoBehaviour GetOrAddCoroutineComponent(GameObject gameObject)
        {
            CoroutineRunner runner = gameObject.GetComponent<CoroutineRunner>();
            if (runner == null)
            {
                runner = gameObject.AddComponent<CoroutineRunner>();
            }

            return runner;
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
                Debug.LogError($"Error executing delayed action: {e}");
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
                Debug.LogError($"Error executing delayed frames action: {e}");
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
                Debug.LogError($"Error executing WaitUntil action: {e}");
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
                Debug.LogError($"Error executing WaitWhile action: {e}");
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
                Debug.LogError($"Error executing EndOfFrame action: {e}");
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
                    Debug.LogError($"Error executing RepeatUntil action: {e}");
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
    [AddComponentMenu("")]
    public class CoroutineRunner : MonoBehaviour
    {
    }

    /// <summary>
    ///     Global helper MonoBehaviour used for coroutines without explicit owner.
    /// </summary>
    [AddComponentMenu("")]
    public class CoroutineHelper : MonoBehaviour
    {
    }
}