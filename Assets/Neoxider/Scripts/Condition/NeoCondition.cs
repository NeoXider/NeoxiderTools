using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Condition
{
    /// <summary>How to combine multiple conditions.</summary>
    public enum LogicMode
    {
        /// <summary>All conditions must be true.</summary>
        AND,

        /// <summary>At least one condition must be true.</summary>
        OR
    }

    /// <summary>When to evaluate conditions.</summary>
    public enum CheckMode
    {
        /// <summary>Only when Check() is called.</summary>
        Manual,

        /// <summary>Every frame.</summary>
        EveryFrame,

        /// <summary>At a fixed interval.</summary>
        Interval
    }

    /// <summary>
    ///     No-Code condition system. Evaluates field/property values of any component via Inspector without code.
    ///     Supports AND/OR logic, inversion, and manual or automatic checking.
    /// </summary>
    /// <remarks>
    ///     Usage: 1) Add NeoCondition to a GameObject. 2) Add conditions (Conditions list): pick object, component, field,
    ///     operator, threshold.
    ///     3) Configure OnTrue / OnFalse events. 4) Set check mode (Manual / EveryFrame / Interval). For Manual, call Check()
    ///     from another component's UnityEvent.
    /// </remarks>
    [NeoDoc("Condition/NeoCondition.md")]
    [CreateFromMenu("Neoxider/Condition/NeoCondition")]
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

        /// <summary>Result of the last check.</summary>
        public bool LastResult => _lastResult ?? false;

        /// <summary>Logic for combining conditions (AND/OR).</summary>
        public LogicMode Logic
        {
            get => _logicMode;
            set => _logicMode = value;
        }

        /// <summary>When to evaluate (Manual / EveryFrame / Interval).</summary>
        public CheckMode Mode
        {
            get => _checkMode;
            set
            {
                _checkMode = value;
                RestartCheckMode();
            }
        }

        /// <summary>List of conditions (read-only).</summary>
        public IReadOnlyList<ConditionEntry> Conditions => _conditions;

        /// <summary>Invoked when conditions are met (result = true).</summary>
        public UnityEvent OnTrue => _onTrue;

        /// <summary>Invoked when conditions are not met (result = false).</summary>
        public UnityEvent OnFalse => _onFalse;

        /// <summary>Invoked on each check with the result.</summary>
        public UnityEvent<bool> OnResult => _onResult;

        /// <summary>Invoked on each check with inverted result (!result).</summary>
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

        /// <summary>Evaluates all conditions and invokes events. Can be called from another component's UnityEvent.</summary>
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

        /// <summary>Evaluates conditions without invoking events.</summary>
        /// <returns>True if conditions are met according to LogicMode.</returns>
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

        /// <summary>Safe wrapper to evaluate one condition; catches exceptions (e.g. destroyed objects) and logs once.</summary>
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
                        $"[NeoCondition] Condition #{index} on '{name}': object or component was destroyed. " +
                        "Condition evaluates to false.");
                    _loggedEntryErrors.Add(index);
                }

                entry.InvalidateCache();
                return false;
            }
            catch (Exception ex)
            {
                if (!_loggedEntryErrors.Contains(index))
                {
                    Debug.LogWarning($"[NeoCondition] Condition #{index} on '{name}': error — {ex.Message}. " +
                                     "Condition evaluates to false.");
                    _loggedEntryErrors.Add(index);
                }

                return false;
            }
        }

        /// <summary>Resets last result so the next Check() will invoke events regardless of change.</summary>
        [Button("Reset")]
        public void ResetState()
        {
            _lastResult = null;
            _loggedEntryErrors.Clear();
        }

        /// <summary>Clears reflection cache in all conditions (including name lookup cache).</summary>
        public void InvalidateAllCaches()
        {
            foreach (ConditionEntry entry in _conditions)
            {
                entry?.InvalidateCacheFull();
            }

            _loggedEntryErrors.Clear();
        }

        /// <summary>Adds a condition at runtime.</summary>
        public void AddCondition(ConditionEntry entry)
        {
            _conditions.Add(entry);
        }

        /// <summary>Removes a condition.</summary>
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
