using System;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.StateMachine
{
    /// <summary>
    ///     Базовый класс для условий переходов в State Machine (legacy).
    ///     Рекомендуется использовать StatePredicate для новых реализаций.
    /// </summary>
    /// <remarks>
    ///     Этот класс оставлен для обратной совместимости.
    ///     Для новых проектов рекомендуется использовать StatePredicate, который предоставляет больше возможностей.
    /// </remarks>
    /// <example>
    ///     <code>
    /// public class CustomCondition : StateCondition
    /// {
    ///     public override bool Evaluate()
    ///     {
    ///         // Логика проверки условия
    ///         return true;
    ///     }
    /// }
    /// </code>
    /// </example>
    [Serializable]
    public abstract class StateCondition
    {
        /// <summary>
        ///     Оценить условие.
        /// </summary>
        /// <returns>True, если условие выполнено.</returns>
        public abstract bool Evaluate();
    }

    /// <summary>
    ///     Условие для проверки bool значения.
    /// </summary>
    [Serializable]
    public class BoolStateCondition : StateCondition
    {
        [SerializeField] private bool value;

        /// <summary>
        ///     Значение для проверки.
        /// </summary>
        public bool Value
        {
            get => value;
            set => this.value = value;
        }

        public override bool Evaluate()
        {
            return value;
        }
    }

    /// <summary>
    ///     Условие для сравнения float значений.
    /// </summary>
    [Serializable]
    public class FloatStateCondition : StateCondition
    {
        [SerializeField] private float value;

        [SerializeField] private ComparisonType comparison = ComparisonType.GreaterThan;

        [SerializeField] private float threshold;

        /// <summary>
        ///     Значение для сравнения.
        /// </summary>
        public float Value
        {
            get => value;
            set => this.value = value;
        }

        /// <summary>
        ///     Тип сравнения.
        /// </summary>
        public ComparisonType Comparison
        {
            get => comparison;
            set => comparison = value;
        }

        /// <summary>
        ///     Пороговое значение для сравнения.
        /// </summary>
        public float Threshold
        {
            get => threshold;
            set => threshold = value;
        }

        public override bool Evaluate()
        {
            return comparison switch
            {
                ComparisonType.GreaterThan => value > threshold,
                ComparisonType.LessThan => value < threshold,
                ComparisonType.GreaterThanOrEqual => value >= threshold,
                ComparisonType.LessThanOrEqual => value <= threshold,
                ComparisonType.Equal => Mathf.Approximately(value, threshold),
                ComparisonType.NotEqual => !Mathf.Approximately(value, threshold),
                _ => false
            };
        }
    }

    /// <summary>
    ///     Условие на основе UnityEvent.
    /// </summary>
    [Serializable]
    public class EventStateCondition : StateCondition
    {
        [SerializeField] private UnityEvent onEvaluate = new();

        private bool lastResult;

        /// <summary>
        ///     Событие для оценки. Должно устанавливать результат через SetResult().
        /// </summary>
        public UnityEvent OnEvaluate => onEvaluate;

        /// <summary>
        ///     Установить результат оценки условия.
        /// </summary>
        /// <param name="result">Результат оценки.</param>
        public void SetResult(bool result)
        {
            lastResult = result;
        }

        public override bool Evaluate()
        {
            onEvaluate?.Invoke();
            return lastResult;
        }
    }
}