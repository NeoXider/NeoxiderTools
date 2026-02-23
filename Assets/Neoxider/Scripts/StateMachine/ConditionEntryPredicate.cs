using System;
using Neo.Condition;
using UnityEngine;

namespace Neo.StateMachine
{
    /// <summary>
    ///     Предикат перехода, использующий универсальное условие Neoxider (объект → компонент → свойство → сравнение → порог).
    ///     Позволяет выбирать условия так же, как в NeoCondition, и использовать их в переходах State Machine.
    /// </summary>
    /// <summary>
    ///     Контекст для условия: задаётся слотом (0 = объект с StateMachine, 1+ = из списка Context Overrides на компоненте).
    ///     Ссылки на объекты сцены не хранятся в SO — только номер слота.
    /// </summary>
    public enum ConditionContextSlot
    {
        /// <summary>Объект с компонентом StateMachine (владелец).</summary>
        Owner = 0,

        /// <summary>Первый элемент из Context Overrides на компоненте.</summary>
        Override1 = 1,

        /// <summary>Второй элемент из Context Overrides.</summary>
        Override2 = 2,

        /// <summary>Третий элемент из Context Overrides.</summary>
        Override3 = 3,

        /// <summary>Четвёртый элемент из Context Overrides.</summary>
        Override4 = 4,

        /// <summary>Пятый элемент из Context Overrides.</summary>
        Override5 = 5
    }

    [Serializable]
    public class ConditionEntryPredicate : StatePredicate
    {
        [SerializeField] [Tooltip("Condition to evaluate (same as NeoCondition entries).")]
        private ConditionEntry conditionEntry;

        [SerializeField] [Tooltip("Which GameObject to read from: Owner = object with StateMachine; Override1..5 = from Context Overrides list on the component (set in scene). SO cannot store scene refs — use slot.")]
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
                return false;

            int slot = (int)contextSlot;
            GameObject context = StateMachineEvaluationContext.GetContextBySlot(slot);

            if (context == null)
                context = (currentState as MonoBehaviour)?.gameObject;

            if (context == null)
                context = StateMachineEvaluationContext.CurrentContextObject;

            if (context == null || !context)
                return false;

            return conditionEntry.Evaluate(context);
        }
    }
}
