using System.Collections.Generic;
using UnityEngine;

namespace Neo.StateMachine
{
    /// <summary>
    ///     Контекст оценки переходов: объект-владелец и опциональные объекты из компонента в сцене.
    ///     SO не хранит ссылки на сцену; сценовые объекты задаются на StateMachineBehaviour и передаются сюда по слотам.
    /// </summary>
    internal static class StateMachineEvaluationContext
    {
        [System.ThreadStatic] private static GameObject currentContextObject;
        [System.ThreadStatic] private static IReadOnlyList<GameObject> currentOverrides;
        [System.ThreadStatic] private static Stack<GameObject> contextStack;
        [System.ThreadStatic] private static Stack<IReadOnlyList<GameObject>> overridesStack;

        public static GameObject CurrentContextObject => currentContextObject;

        /// <summary>
        ///     Контекст по слоту: 0 = владелец (GameObject с StateMachine), 1+ = элемент из списка Context Overrides на компоненте.
        /// </summary>
        public static GameObject GetContextBySlot(int slot)
        {
            if (slot <= 0)
                return currentContextObject;

            if (currentOverrides != null && slot <= currentOverrides.Count)
            {
                GameObject go = currentOverrides[slot - 1];
                return go != null && go ? go : null;
            }

            return null;
        }

        public static void Push(GameObject contextObject, IReadOnlyList<GameObject> overrides = null)
        {
            contextStack ??= new Stack<GameObject>();
            overridesStack ??= new Stack<IReadOnlyList<GameObject>>();
            contextStack.Push(currentContextObject);
            overridesStack.Push(currentOverrides);
            currentContextObject = contextObject;
            currentOverrides = overrides;
        }

        public static void Pop()
        {
            if (contextStack == null || contextStack.Count == 0)
            {
                currentContextObject = null;
                currentOverrides = null;
                return;
            }

            currentContextObject = contextStack.Pop();
            currentOverrides = overridesStack.Count > 0 ? overridesStack.Pop() : null;
        }
    }
}
