using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.StateMachine
{
    /// <summary>
    ///     Transition evaluation context: owning GameObject and optional override objects from the scene component.
    ///     ScriptableObjects do not store scene references; scene objects are set on StateMachineBehaviour and passed in by slot.
    /// </summary>
    internal static partial class StateMachineEvaluationContext
    {
        [ThreadStatic] private static IReadOnlyList<GameObject> currentOverrides;
        [ThreadStatic] private static Stack<GameObject> contextStack;
        [ThreadStatic] private static Stack<IReadOnlyList<GameObject>> overridesStack;

        [field: ThreadStatic] public static GameObject CurrentContextObject { get; private set; }

        /// <summary>
        ///     Context by slot: 0 = owner (GameObject with State Machine), 1+ = entry from Context Overrides on the component.
        /// </summary>
        public static GameObject GetContextBySlot(int slot)
        {
            if (slot <= 0)
            {
                return CurrentContextObject;
            }

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
            contextStack.Push(CurrentContextObject);
            overridesStack.Push(currentOverrides);
            CurrentContextObject = contextObject;
            currentOverrides = overrides;
        }

        public static void Pop()
        {
            if (contextStack == null || contextStack.Count == 0)
            {
                CurrentContextObject = null;
                currentOverrides = null;
                return;
            }

            CurrentContextObject = contextStack.Pop();
            currentOverrides = overridesStack.Count > 0 ? overridesStack.Pop() : null;
        }

        // WHY: with Enter Play Mode Options disabling Domain Reload an evaluation aborted by an exception
        // leaves the context pointing at a destroyed GameObject, and the next session starts on top of the
        // previous stack. Only the calling (main) thread is reset — [ThreadStatic] copies of worker threads
        // die with those threads.
        // The lifecycle attribute is source-generated, so the containing class must stay partial.
#if UNITY_6000_5_OR_NEWER
        [OnExitingPlayMode]
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            CurrentContextObject = null;
            currentOverrides = null;
            contextStack?.Clear();
            overridesStack?.Clear();
        }
    }
}
