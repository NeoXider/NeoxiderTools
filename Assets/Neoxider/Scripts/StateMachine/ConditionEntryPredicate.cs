using System;
using Neo.Condition;
using UnityEngine;

namespace Neo.StateMachine
{
    /// <summary>
    ///     Context slot for NeoCondition-style evaluation: 0 = StateMachine owner, 1+ = Context Overrides on the behaviour.
    ///     Scene object references are not stored on the ScriptableObject — only the slot index.
    /// </summary>
    public enum ConditionContextSlot
    {
        /// <summary>GameObject that owns the State Machine component.</summary>
        Owner = 0,

        /// <summary>First entry in Context Overrides on the component.</summary>
        Override1 = 1,

        /// <summary>Second entry in Context Overrides.</summary>
        Override2 = 2,

        /// <summary>Third entry in Context Overrides.</summary>
        Override3 = 3,

        /// <summary>Fourth entry in Context Overrides.</summary>
        Override4 = 4,

        /// <summary>Fifth entry in Context Overrides.</summary>
        Override5 = 5
    }

    /// <summary>
    ///     Transition predicate that evaluates a NeoCondition <see cref="ConditionEntry"/> against a context GameObject
    ///     chosen by <see cref="ConditionContextSlot"/> (same workflow as NeoCondition in the Inspector).
    /// </summary>
    [Serializable]
    public class ConditionEntryPredicate : StatePredicate
    {
        [SerializeField] [Tooltip("Condition to evaluate (same as NeoCondition entries).")]
        private ConditionEntry conditionEntry;

        [SerializeField]
        [Tooltip(
            "Which GameObject to read from: Owner = object with StateMachine; Override1..5 = from Context Overrides list on the component (set in scene). SO cannot store scene refs — use slot.")]
        private ConditionContextSlot contextSlot = ConditionContextSlot.Owner;

        /// <summary>Condition entry (object, component, property, compare, threshold).</summary>
        public ConditionEntry ConditionEntry
        {
            get => conditionEntry;
            set => conditionEntry = value;
        }

        /// <summary>Context slot: 0 = owner, 1..5 = Context Overrides on StateMachineBehaviour (scene).</summary>
        public ConditionContextSlot ContextSlot
        {
            get => contextSlot;
            set => contextSlot = value;
        }

        protected override bool EvaluateInternal(IState currentState)
        {
            if (conditionEntry == null)
            {
                return false;
            }

            int slot = (int)contextSlot;
            GameObject context = StateMachineEvaluationContext.GetContextBySlot(slot);

            if (context == null)
            {
                context = (currentState as MonoBehaviour)?.gameObject;
            }

            if (context == null)
            {
                context = StateMachineEvaluationContext.CurrentContextObject;
            }

            if (context == null || !context)
            {
                return false;
            }

            return conditionEntry.Evaluate(context);
        }
    }
}
