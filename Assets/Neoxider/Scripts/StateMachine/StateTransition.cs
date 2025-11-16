using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Neo.StateMachine.NoCode;

namespace Neo.StateMachine
{
    /// <summary>
    ///     Класс для определения переходов между состояниями в State Machine.
    ///     Поддерживает условия переходов через предикаты и приоритеты.
    /// </summary>
    /// <remarks>
    ///     Переходы могут быть определены как по типам состояний (для кода), так и по именам (для NoCode).
    ///     Поддерживается комбинирование условий через список предикатов.
    /// </remarks>
    /// <example>
    ///     <code>
    /// var transition = new StateTransition
    /// {
    ///     fromStateType = typeof(IdleState),
    ///     toStateType = typeof(RunningState),
    ///     priority = 1
    /// };
    /// 
    /// transition.AddPredicate(new FloatComparisonPredicate
    /// {
    ///     Value = player.Health,
    ///     Comparison = ComparisonType.GreaterThan,
    ///     Threshold = 50f
    /// });
    /// 
    /// stateMachine.RegisterTransition(transition);
    /// </code>
    /// </example>
    [Serializable]
    public class StateTransition
    {
        [SerializeField]
        private StateData fromStateData;

        [SerializeField]
        private StateData toStateData;

        [SerializeField]
        private List<StatePredicate> predicates = new List<StatePredicate>();

        [SerializeField]
        private int priority = 0;

        [SerializeField]
        private bool isEnabled = true;

        [SerializeField]
        private string transitionName = "Unnamed Transition";

        private Type fromStateType;
        private Type toStateType;

        /// <summary>
        ///     Тип исходного состояния (для кода).
        /// </summary>
        public Type FromStateType
        {
            get => fromStateType;
            set => fromStateType = value;
        }

        /// <summary>
        ///     Тип целевого состояния (для кода).
        /// </summary>
        public Type ToStateType
        {
            get => toStateType;
            set => toStateType = value;
        }

        /// <summary>
        ///     Исходное состояние (ScriptableObject для NoCode).
        /// </summary>
        public StateData FromStateData
        {
            get => fromStateData;
            set => fromStateData = value;
        }

        /// <summary>
        ///     Целевое состояние (ScriptableObject для NoCode).
        /// </summary>
        public StateData ToStateData
        {
            get => toStateData;
            set => toStateData = value;
        }

        /// <summary>
        ///     Имя исходного состояния (получается из ScriptableObject).
        /// </summary>
        public string FromStateName => fromStateData != null ? fromStateData.StateName : "";

        /// <summary>
        ///     Имя целевого состояния (получается из ScriptableObject).
        /// </summary>
        public string ToStateName => toStateData != null ? toStateData.StateName : "";

        /// <summary>
        ///     Список предикатов для оценки условий перехода.
        /// </summary>
        public List<StatePredicate> Predicates => predicates;

        /// <summary>
        ///     Приоритет перехода. Переходы с большим приоритетом проверяются первыми.
        /// </summary>
        public int Priority
        {
            get => priority;
            set => priority = value;
        }

        /// <summary>
        ///     Включен ли переход. Отключенные переходы не оцениваются.
        /// </summary>
        public bool IsEnabled
        {
            get => isEnabled;
            set => isEnabled = value;
        }

        /// <summary>
        ///     Имя перехода для отладки и отображения в инспекторе.
        /// </summary>
        public string TransitionName
        {
            get => transitionName;
            set => transitionName = value;
        }

        /// <summary>
        ///     Проверить возможность перехода из текущего состояния.
        /// </summary>
        /// <param name="currentState">Текущее состояние State Machine.</param>
        /// <returns>True, если переход возможен.</returns>
        public bool CanTransition(IState currentState)
        {
            if (!isEnabled)
            {
                return false;
            }

            if (currentState == null)
            {
                return false;
            }

            // Проверка типа состояния (для кода)
            if (fromStateType != null)
            {
                if (currentState.GetType() != fromStateType)
                {
                    return false;
                }
            }

            return Evaluate();
        }

        /// <summary>
        ///     Оценить все предикаты перехода.
        /// </summary>
        /// <returns>True, если все предикаты выполнены.</returns>
        public bool Evaluate()
        {
            if (!isEnabled)
            {
                return false;
            }

            if (predicates.Count == 0)
            {
                return true;
            }

            // Все предикаты должны быть выполнены (AND логика)
            return predicates.All(p => p != null && p.Evaluate());
        }

        /// <summary>
        ///     Оценить предикаты с контекстом текущего состояния.
        /// </summary>
        /// <param name="currentState">Текущее состояние.</param>
        /// <returns>True, если все предикаты выполнены.</returns>
        public bool EvaluatePredicates(IState currentState)
        {
            if (!isEnabled)
            {
                return false;
            }

            if (predicates.Count == 0)
            {
                return true;
            }

            return predicates.All(p => p != null && p.Evaluate(currentState));
        }

        /// <summary>
        ///     Добавить предикат к переходу.
        /// </summary>
        /// <param name="predicate">Предикат для добавления.</param>
        public void AddPredicate(StatePredicate predicate)
        {
            if (predicate != null && !predicates.Contains(predicate))
            {
                predicates.Add(predicate);
            }
        }

        /// <summary>
        ///     Удалить предикат из перехода.
        /// </summary>
        /// <param name="predicate">Предикат для удаления.</param>
        public void RemovePredicate(StatePredicate predicate)
        {
            predicates.Remove(predicate);
        }

        /// <summary>
        ///     Проверить, соответствует ли переход указанному типу состояния.
        /// </summary>
        /// <param name="stateType">Тип состояния для проверки.</param>
        /// <returns>True, если переход соответствует типу.</returns>
        public bool MatchesFromState(Type stateType)
        {
            if (fromStateType != null)
            {
                return fromStateType == stateType;
            }

            return false;
        }

        /// <summary>
        ///     Проверить, соответствует ли переход указанному имени состояния.
        /// </summary>
        /// <param name="stateName">Имя состояния для проверки.</param>
        /// <returns>True, если переход соответствует имени.</returns>
        public bool MatchesFromState(string stateName)
        {
            if (fromStateData != null)
            {
                return fromStateData.StateName == stateName;
            }

            return false;
        }
    }
}

