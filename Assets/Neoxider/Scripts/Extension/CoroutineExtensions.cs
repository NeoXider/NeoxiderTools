using System;
using System.Collections;
using UnityEngine;

namespace Neo
{
    public static class CoroutineExtensions
    {
        /// <summary>
        /// Запускает корутину с заданной задержкой, после чего выполняется действие.
        /// </summary>
        public static IEnumerator WaitAndExecute(this MonoBehaviour monoBehaviour, float delay, Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }

        /// <summary>
        /// Ждет до тех пор, пока условие (predicate) не станет истинным, затем выполняет действие.
        /// </summary>
        public static IEnumerator WaitUntilAndExecute(this MonoBehaviour monoBehaviour, Func<bool> predicate, Action action)
        {
            yield return new WaitUntil(predicate);
            action?.Invoke();
        }

        /// <summary>
        /// Ждет до тех пор, пока условие (predicate) не станет ложным, затем выполняет действие.
        /// </summary>
        public static IEnumerator WaitWhileAndExecute(this MonoBehaviour monoBehaviour, Func<bool> predicate, Action action)
        {
            yield return new WaitWhile(predicate);
            action?.Invoke();
        }

        /// <summary>
        /// Выполняет действие через заданное количество кадров.
        /// </summary>
        public static IEnumerator ExecuteAfterFrames(this MonoBehaviour monoBehaviour, int frameCount, Action action)
        {
            for (int i = 0; i < frameCount; i++)
            {
                yield return null;
            }
            action?.Invoke();
        }

        /// <summary>
        /// Выполняет действие на следующем кадре.
        /// </summary>
        public static IEnumerator ExecuteNextFrame(this MonoBehaviour monoBehaviour, Action action)
        {
            yield return null;
            action?.Invoke();
        }

        /// <summary>
        /// Выполняет действие в конце текущего кадра.
        /// </summary>
        public static IEnumerator ExecuteAtEndOfFrame(this MonoBehaviour monoBehaviour, Action action)
        {
            yield return new WaitForEndOfFrame();
            action?.Invoke();
        }

        /// <summary>
        /// Ждет указанное время в реальном времени (игнорируя timeScale) и выполняет действие.
        /// </summary>
        public static IEnumerator WaitAndExecuteRealTime(this MonoBehaviour monoBehaviour, float delay, Action action)
        {
            yield return new WaitForSecondsRealtime(delay);
            action?.Invoke();
        }

        /// <summary>
        /// Повторно выполняет действие каждый кадр до тех пор, пока условие не станет истинным.
        /// Например, использовать для опроса состояния до момента, когда оно изменится.
        /// </summary>
        public static IEnumerator RepeatUntil(this MonoBehaviour monoBehaviour, Func<bool> condition, Action action)
        {
            while (!condition())
            {
                action?.Invoke();
                yield return null;
            }
        }
    }
} 