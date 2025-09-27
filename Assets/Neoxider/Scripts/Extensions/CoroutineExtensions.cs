using System;
using System.Collections;
using UnityEngine;

namespace Neo.Extensions
{
    public static class CoroutineExtensions
    {
        private static CoroutineHelper instance;

        private static CoroutineHelper Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("[CoroutineHelper]");
                    instance = go.AddComponent<CoroutineHelper>();
                    GameObject.DontDestroyOnLoad(go);
                }

                return instance;
            }
        }

        public class CoroutineHandle
        {
            private readonly MonoBehaviour owner;
            private Coroutine coroutine;

            internal CoroutineHandle(MonoBehaviour owner)
            {
                this.owner = owner;
                IsRunning = false;
            }

            public Coroutine Coroutine
            {
                get => coroutine;
                internal set
                {
                    coroutine = value;
                    IsRunning = true;
                }
            }

            public bool IsRunning { get; private set; }

            public void Stop()
            {
                if (!IsRunning || owner == null) return;

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
            return DelayFrames(monoBehaviour, 1, action);
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

        public static CoroutineHandle Delay(this GameObject gameObject, float seconds, Action action,
            bool useUnscaledTime = false)
        {
            var owner = GetOrAddCoroutineComponent(gameObject);
            return owner.Delay(seconds, action, useUnscaledTime);
        }

        public static CoroutineHandle WaitUntil(this GameObject gameObject, Func<bool> predicate, Action action)
        {
            var owner = GetOrAddCoroutineComponent(gameObject);
            return owner.WaitUntil(predicate, action);
        }

        public static CoroutineHandle WaitWhile(this GameObject gameObject, Func<bool> predicate, Action action)
        {
            var owner = GetOrAddCoroutineComponent(gameObject);
            return owner.WaitWhile(predicate, action);
        }

        public static CoroutineHandle DelayFrames(this GameObject gameObject, int frameCount, Action action,
            bool useFixedUpdate = false)
        {
            var owner = GetOrAddCoroutineComponent(gameObject);
            return owner.DelayFrames(frameCount, action, useFixedUpdate);
        }

        #endregion

        #region Global Methods

        public static CoroutineHandle Delay(float seconds, Action action, bool useUnscaledTime = false)
        {
            return Instance.Delay(seconds, action, useUnscaledTime);
        }

        public static CoroutineHandle WaitUntil(Func<bool> predicate, Action action)
        {
            return Instance.WaitUntil(predicate, action);
        }

        public static CoroutineHandle WaitWhile(Func<bool> predicate, Action action)
        {
            return Instance.WaitWhile(predicate, action);
        }

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

            var handle = new CoroutineHandle(owner);
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
            var runner = gameObject.GetComponent<CoroutineRunner>();
            if (runner == null) runner = gameObject.AddComponent<CoroutineRunner>();
            return runner;
        }

        private static IEnumerator DelayedAction(float seconds, Action action, bool useUnscaledTime)
        {
            if (useUnscaledTime)
                yield return new WaitForSecondsRealtime(seconds);
            else
                yield return new WaitForSeconds(seconds);

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
                for (var i = 0; i < frameCount; i++)
                    yield return new WaitForFixedUpdate();
            else
                for (var i = 0; i < frameCount; i++)
                    yield return null;

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

    // Helper component for running coroutines on GameObjects
    [AddComponentMenu("")]
    public class CoroutineRunner : MonoBehaviour
    {
    }

    // Helper class for managing global coroutines
    [AddComponentMenu("")]
    public class CoroutineHelper : MonoBehaviour
    {
    }
}