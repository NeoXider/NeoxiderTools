using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.StateMachine
{
    /// <summary>
    ///     Тип сравнения для предикатов.
    /// </summary>
    public enum ComparisonType
    {
        /// <summary>Больше чем</summary>
        GreaterThan,
        /// <summary>Меньше чем</summary>
        LessThan,
        /// <summary>Больше или равно</summary>
        GreaterThanOrEqual,
        /// <summary>Меньше или равно</summary>
        LessThanOrEqual,
        /// <summary>Равно</summary>
        Equal,
        /// <summary>Не равно</summary>
        NotEqual
    }

    /// <summary>
    ///     Базовый класс для предикатов условий переходов в State Machine.
    ///     Предикаты используются для проверки условий перед переходом между состояниями.
    /// </summary>
    /// <remarks>
    ///     Предикаты могут быть инвертированы через свойство IsInverted.
    ///     Поддерживается сериализация для использования в инспекторе Unity.
    /// </remarks>
    /// <example>
    ///     <code>
    /// public class CustomPredicate : StatePredicate
    /// {
    ///     public override bool Evaluate(IState currentState)
    ///     {
    ///         // Логика проверки условия
    ///         return true;
    ///     }
    /// }
    /// </code>
    /// </example>
    [Serializable]
    public abstract class StatePredicate
    {
        /// <summary>
        ///     Имя предиката для отладки и отображения в инспекторе.
        /// </summary>
        [SerializeField]
        protected string predicateName = "Unnamed Predicate";

        /// <summary>
        ///     Инвертировать результат оценки предиката.
        /// </summary>
        [SerializeField]
        protected bool isInverted = false;

        /// <summary>
        ///     Получить или установить имя предиката.
        /// </summary>
        public string PredicateName
        {
            get => predicateName;
            set => predicateName = value;
        }

        /// <summary>
        ///     Получить или установить инверсию результата.
        /// </summary>
        public bool IsInverted
        {
            get => isInverted;
            set => isInverted = value;
        }

        /// <summary>
        ///     Оценить предикат с контекстом текущего состояния.
        /// </summary>
        /// <param name="currentState">Текущее состояние State Machine.</param>
        /// <returns>Результат оценки предиката (с учетом инверсии).</returns>
        public virtual bool Evaluate(IState currentState)
        {
            bool result = EvaluateInternal(currentState);
            return isInverted ? !result : result;
        }

        /// <summary>
        ///     Оценить предикат без контекста состояния.
        /// </summary>
        /// <returns>Результат оценки предиката (с учетом инверсии).</returns>
        public virtual bool Evaluate()
        {
            bool result = EvaluateInternal(null);
            return isInverted ? !result : result;
        }

        /// <summary>
        ///     Внутренняя логика оценки предиката. Должна быть реализована в наследниках.
        /// </summary>
        /// <param name="currentState">Текущее состояние или null.</param>
        /// <returns>Результат оценки без учета инверсии.</returns>
        protected abstract bool EvaluateInternal(IState currentState);
    }

    /// <summary>
    ///     Предикат для проверки bool значения.
    /// </summary>
    [Serializable]
    public class BoolPredicate : StatePredicate
    {
        [SerializeField]
        private bool value;

        /// <summary>
        ///     Значение для проверки.
        /// </summary>
        public bool Value
        {
            get => value;
            set => this.value = value;
        }

        protected override bool EvaluateInternal(IState currentState)
        {
            return value;
        }
    }

    /// <summary>
    ///     Предикат для сравнения float значений.
    /// </summary>
    [Serializable]
    public class FloatComparisonPredicate : StatePredicate
    {
        [SerializeField]
        private float value;

        [SerializeField]
        private ComparisonType comparison = ComparisonType.GreaterThan;

        [SerializeField]
        private float threshold = 0f;

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

        protected override bool EvaluateInternal(IState currentState)
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
    ///     Предикат для сравнения int значений.
    /// </summary>
    [Serializable]
    public class IntComparisonPredicate : StatePredicate
    {
        [SerializeField]
        private int value;

        [SerializeField]
        private ComparisonType comparison = ComparisonType.GreaterThan;

        [SerializeField]
        private int threshold = 0;

        /// <summary>
        ///     Значение для сравнения.
        /// </summary>
        public int Value
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
        public int Threshold
        {
            get => threshold;
            set => threshold = value;
        }

        protected override bool EvaluateInternal(IState currentState)
        {
            return comparison switch
            {
                ComparisonType.GreaterThan => value > threshold,
                ComparisonType.LessThan => value < threshold,
                ComparisonType.GreaterThanOrEqual => value >= threshold,
                ComparisonType.LessThanOrEqual => value <= threshold,
                ComparisonType.Equal => value == threshold,
                ComparisonType.NotEqual => value != threshold,
                _ => false
            };
        }
    }

    /// <summary>
    ///     Предикат для сравнения строк.
    /// </summary>
    [Serializable]
    public class StringComparisonPredicate : StatePredicate
    {
        [SerializeField]
        private string value = "";

        [SerializeField]
        private string target = "";

        [SerializeField]
        private bool caseSensitive = false;

        /// <summary>
        ///     Значение для сравнения.
        /// </summary>
        public string Value
        {
            get => value;
            set => this.value = value;
        }

        /// <summary>
        ///     Целевое значение для сравнения.
        /// </summary>
        public string Target
        {
            get => target;
            set => target = value;
        }

        /// <summary>
        ///     Учитывать регистр при сравнении.
        /// </summary>
        public bool CaseSensitive
        {
            get => caseSensitive;
            set => caseSensitive = value;
        }

        protected override bool EvaluateInternal(IState currentState)
        {
            if (caseSensitive)
            {
                return value == target;
            }
            return string.Equals(value, target, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    ///     Предикат на основе UnityEvent.
    /// </summary>
    [Serializable]
    public class EventPredicate : StatePredicate
    {
        [SerializeField]
        private UnityEvent onEvaluate = new UnityEvent();

        private bool lastResult = false;

        /// <summary>
        ///     Событие для оценки. Должно устанавливать результат через SetResult().
        /// </summary>
        public UnityEvent OnEvaluate => onEvaluate;

        /// <summary>
        ///     Установить результат оценки предиката.
        /// </summary>
        /// <param name="result">Результат оценки.</param>
        public void SetResult(bool result)
        {
            lastResult = result;
        }

        protected override bool EvaluateInternal(IState currentState)
        {
            onEvaluate?.Invoke();
            return lastResult;
        }
    }

    /// <summary>
    ///     Предикат на основе делегата Func&lt;bool&gt;.
    /// </summary>
    [Serializable]
    public class CustomPredicate : StatePredicate
    {
        private Func<bool> customEvaluator;

        /// <summary>
        ///     Установить кастомный оценщик предиката.
        /// </summary>
        /// <param name="evaluator">Делегат для оценки предиката.</param>
        public void SetEvaluator(Func<bool> evaluator)
        {
            customEvaluator = evaluator;
        }

        protected override bool EvaluateInternal(IState currentState)
        {
            return customEvaluator?.Invoke() ?? false;
        }
    }

    /// <summary>
    ///     Предикат для проверки времени нахождения в состоянии.
    /// </summary>
    [Serializable]
    public class StateDurationPredicate : StatePredicate
    {
        [SerializeField]
        private float requiredDuration = 1f;

        [SerializeField]
        private ComparisonType comparison = ComparisonType.GreaterThanOrEqual;

        private float stateEnterTime = 0f;

        /// <summary>
        ///     Требуемая длительность в секундах.
        /// </summary>
        public float RequiredDuration
        {
            get => requiredDuration;
            set => requiredDuration = value;
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
        ///     Установить время входа в состояние.
        /// </summary>
        /// <param name="enterTime">Время входа.</param>
        public void SetEnterTime(float enterTime)
        {
            stateEnterTime = enterTime;
        }

        protected override bool EvaluateInternal(IState currentState)
        {
            float elapsedTime = Time.time - stateEnterTime;
            return comparison switch
            {
                ComparisonType.GreaterThan => elapsedTime > requiredDuration,
                ComparisonType.LessThan => elapsedTime < requiredDuration,
                ComparisonType.GreaterThanOrEqual => elapsedTime >= requiredDuration,
                ComparisonType.LessThanOrEqual => elapsedTime <= requiredDuration,
                ComparisonType.Equal => Mathf.Approximately(elapsedTime, requiredDuration),
                ComparisonType.NotEqual => !Mathf.Approximately(elapsedTime, requiredDuration),
                _ => false
            };
        }
    }

    /// <summary>
    ///     Предикат для комбинирования других предикатов через AND логику.
    /// </summary>
    [Serializable]
    public class AndPredicate : StatePredicate
    {
        [SerializeField]
        private List<StatePredicate> predicates = new List<StatePredicate>();

        /// <summary>
        ///     Список предикатов для комбинирования.
        /// </summary>
        public List<StatePredicate> Predicates => predicates;

        /// <summary>
        ///     Добавить предикат.
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
        ///     Удалить предикат.
        /// </summary>
        /// <param name="predicate">Предикат для удаления.</param>
        public void RemovePredicate(StatePredicate predicate)
        {
            predicates.Remove(predicate);
        }

        protected override bool EvaluateInternal(IState currentState)
        {
            if (predicates.Count == 0)
            {
                return true;
            }

            foreach (var predicate in predicates)
            {
                if (predicate == null)
                {
                    continue;
                }

                if (!predicate.Evaluate(currentState))
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    ///     Предикат для комбинирования других предикатов через OR логику.
    /// </summary>
    [Serializable]
    public class OrPredicate : StatePredicate
    {
        [SerializeField]
        private List<StatePredicate> predicates = new List<StatePredicate>();

        /// <summary>
        ///     Список предикатов для комбинирования.
        /// </summary>
        public List<StatePredicate> Predicates => predicates;

        /// <summary>
        ///     Добавить предикат.
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
        ///     Удалить предикат.
        /// </summary>
        /// <param name="predicate">Предикат для удаления.</param>
        public void RemovePredicate(StatePredicate predicate)
        {
            predicates.Remove(predicate);
        }

        protected override bool EvaluateInternal(IState currentState)
        {
            if (predicates.Count == 0)
            {
                return false;
            }

            foreach (var predicate in predicates)
            {
                if (predicate == null)
                {
                    continue;
                }

                if (predicate.Evaluate(currentState))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    ///     Предикат для инверсии другого предиката.
    /// </summary>
    [Serializable]
    public class NotPredicate : StatePredicate
    {
        [SerializeField]
        private StatePredicate predicate;

        /// <summary>
        ///     Предикат для инверсии.
        /// </summary>
        public StatePredicate Predicate
        {
            get => predicate;
            set => predicate = value;
        }

        protected override bool EvaluateInternal(IState currentState)
        {
            if (predicate == null)
            {
                return true;
            }

            return !predicate.Evaluate(currentState);
        }
    }
}

