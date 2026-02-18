using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Condition
{
    /// <summary>
    ///     Логика объединения условий.
    /// </summary>
    public enum LogicMode
    {
        /// <summary>Все условия должны быть true.</summary>
        AND,

        /// <summary>Хотя бы одно условие должно быть true.</summary>
        OR
    }

    /// <summary>
    ///     Режим проверки условий.
    /// </summary>
    public enum CheckMode
    {
        /// <summary>Только по вызову Check().</summary>
        Manual,

        /// <summary>Каждый кадр.</summary>
        EveryFrame,

        /// <summary>С заданным интервалом.</summary>
        Interval
    }

    /// <summary>
    ///     No-Code система условий.
    ///     Проверяет значения полей/свойств любых компонентов через Inspector, без кода.
    ///     Поддерживает AND/OR логику, инверсию, автоматическую и ручную проверку.
    /// </summary>
    /// <remarks>
    ///     Использование:
    ///     1. Добавить NeoCondition на GameObject
    ///     2. Добавить условия (Conditions) — выбрать объект, компонент, поле, оператор, порог
    ///     3. Настроить события OnTrue / OnFalse
    ///     4. Выбрать режим проверки (Manual / EveryFrame / Interval)
    ///     Для ручного режима — вызвать Check() из UnityEvent другого компонента.
    /// </remarks>
    [NeoDoc("Condition/NeoCondition.md")]
    [AddComponentMenu("Neoxider/Condition/NeoCondition")]
    public class NeoCondition : MonoBehaviour
    {
        [Header("Logic")] [Tooltip("Combine logic: AND (all true) or OR (at least one true).")] [SerializeField]
        private LogicMode _logicMode = LogicMode.AND;

        [Header("Conditions")] [Tooltip("List of conditions to evaluate.")] [SerializeField]
        private List<ConditionEntry> _conditions = new();

        [Header("Check Mode")] [Tooltip("When to evaluate conditions.")] [SerializeField]
        private CheckMode _checkMode = CheckMode.Interval;

        [Tooltip("Check interval in seconds (for Interval mode).")] [SerializeField]
        private float _checkInterval = 0.2f;

        [Tooltip("Check once on start.")] [SerializeField]
        private bool _checkOnStart = true;

        [Tooltip("Invoke events only when result changes (not every tick).")] [SerializeField]
        private bool _onlyOnChange = true;

        [Header("Events")] [Tooltip("Invoked when all conditions are met (result = true).")] [SerializeField]
        private UnityEvent _onTrue = new();

        [Tooltip("Invoked when conditions are NOT met (result = false).")] [SerializeField]
        private UnityEvent _onFalse = new();

        [Tooltip("Invoked on each check with the result.")] [SerializeField]
        private UnityEvent<bool> _onResult = new();

        [Tooltip("Invoked on each check with inverted result (!result).")] [SerializeField]
        private UnityEvent<bool> _onInvertedResult = new();

        private readonly HashSet<int> _loggedEntryErrors = new();
        private Coroutine _intervalCoroutine;

        private bool? _lastResult;

        /// <summary>Текущий результат последней проверки.</summary>
        public bool LastResult => _lastResult ?? false;

        /// <summary>Логика объединения.</summary>
        public LogicMode Logic
        {
            get => _logicMode;
            set => _logicMode = value;
        }

        /// <summary>Режим проверки.</summary>
        public CheckMode Mode
        {
            get => _checkMode;
            set
            {
                _checkMode = value;
                RestartCheckMode();
            }
        }

        /// <summary>Список условий (readonly доступ).</summary>
        public IReadOnlyList<ConditionEntry> Conditions => _conditions;

        /// <summary>Событие: условия выполнены.</summary>
        public UnityEvent OnTrue => _onTrue;

        /// <summary>Событие: условия не выполнены.</summary>
        public UnityEvent OnFalse => _onFalse;

        /// <summary>Событие: результат проверки.</summary>
        public UnityEvent<bool> OnResult => _onResult;

        /// <summary>Событие: инвертированный результат проверки (!result).</summary>
        public UnityEvent<bool> OnInvertedResult => _onInvertedResult;

        private void Start()
        {
            for (int i = 0; i < _conditions?.Count; i++)
            {
                _conditions[i]?.BindOtherToSourceIfNull(gameObject);
            }

            if (_checkOnStart)
            {
                Check();
            }

            RestartCheckMode();
        }

        private void Update()
        {
            if (_checkMode == CheckMode.EveryFrame)
            {
                Check();
            }
        }

        private void OnEnable()
        {
            RestartCheckMode();
        }

        private void OnDisable()
        {
            StopInterval();
        }

        /// <summary>
        ///     Проверить все условия и вызвать события.
        ///     Можно вызывать из UnityEvent других компонентов.
        /// </summary>
        [Button("Check")]
        public void Check()
        {
            bool result = Evaluate();
            bool changed = !_lastResult.HasValue || _lastResult.Value != result;
            _lastResult = result;

            if (_onlyOnChange && !changed)
            {
                return;
            }

            _onResult?.Invoke(result);
            _onInvertedResult?.Invoke(!result);

            if (result)
            {
                _onTrue?.Invoke();
            }
            else
            {
                _onFalse?.Invoke();
            }
        }

        /// <summary>
        ///     Оценить условия без вызова событий.
        /// </summary>
        /// <returns>true если условия выполнены согласно LogicMode.</returns>
        public bool Evaluate()
        {
            if (_conditions == null || _conditions.Count == 0)
            {
                return true;
            }

            if (_logicMode == LogicMode.AND)
            {
                for (int i = 0; i < _conditions.Count; i++)
                {
                    if (_conditions[i] == null)
                    {
                        continue;
                    }

                    if (!EvaluateEntrySafe(_conditions[i], i))
                    {
                        return false;
                    }
                }

                return true;
            }

            // OR
            for (int i = 0; i < _conditions.Count; i++)
            {
                if (_conditions[i] == null)
                {
                    continue;
                }

                if (EvaluateEntrySafe(_conditions[i], i))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Безопасная обёртка для оценки одного условия.
        ///     Ловит исключения (уничтоженные объекты и т.п.) и логирует однократно.
        /// </summary>
        private bool EvaluateEntrySafe(ConditionEntry entry, int index)
        {
            try
            {
                return entry.Evaluate(gameObject);
            }
            catch (MissingReferenceException)
            {
                if (!_loggedEntryErrors.Contains(index))
                {
                    Debug.LogWarning(
                        $"[NeoCondition] Условие #{index} на '{name}': объект или компонент был уничтожен. " +
                        "Условие возвращает false.");
                    _loggedEntryErrors.Add(index);
                }

                entry.InvalidateCache();
                return false;
            }
            catch (Exception ex)
            {
                if (!_loggedEntryErrors.Contains(index))
                {
                    Debug.LogWarning($"[NeoCondition] Условие #{index} на '{name}': ошибка — {ex.Message}. " +
                                     "Условие возвращает false.");
                    _loggedEntryErrors.Add(index);
                }

                return false;
            }
        }

        /// <summary>
        ///     Сбросить последний результат (следующий Check вызовет событие в любом случае).
        /// </summary>
        [Button("Reset")]
        public void ResetState()
        {
            _lastResult = null;
            _loggedEntryErrors.Clear();
        }

        /// <summary>
        ///     Сбросить кеш reflection во всех условиях (включая кеш поиска по имени).
        /// </summary>
        public void InvalidateAllCaches()
        {
            foreach (ConditionEntry entry in _conditions)
            {
                entry?.InvalidateCacheFull();
            }

            _loggedEntryErrors.Clear();
        }

        /// <summary>
        ///     Добавить условие в рантайме.
        /// </summary>
        public void AddCondition(ConditionEntry entry)
        {
            _conditions.Add(entry);
        }

        /// <summary>
        ///     Удалить условие.
        /// </summary>
        public void RemoveCondition(ConditionEntry entry)
        {
            _conditions.Remove(entry);
        }

        private void RestartCheckMode()
        {
            StopInterval();
            if (_checkMode == CheckMode.Interval && isActiveAndEnabled)
            {
                _intervalCoroutine = StartCoroutine(IntervalCheck());
            }
        }

        private void StopInterval()
        {
            if (_intervalCoroutine != null)
            {
                StopCoroutine(_intervalCoroutine);
                _intervalCoroutine = null;
            }
        }

        private IEnumerator IntervalCheck()
        {
            WaitForSeconds wait = new(Mathf.Max(_checkInterval, 0.01f));
            while (true)
            {
                yield return wait;
                Check();
            }
        }
    }
}
