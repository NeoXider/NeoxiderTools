using System;
using System.Collections;
using System.Collections.Generic;
using Neo.Network;
using UnityEngine;
using UnityEngine.Events;
#if MIRROR
using Mirror;
#endif

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

    /// <summary>Who evaluates the condition when isNetworked is true.</summary>
    public enum ConditionAuthority
    {
        /// <summary>Server re-evaluates conditions itself (secure, default). Use when conditions check shared state.</summary>
        ServerRevalidate,

        /// <summary>Server trusts client result. Use when conditions check client-local state (UI, input) that the server cannot access.</summary>
        TrustClient
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
    public class NeoCondition : NeoNetworkComponent
    {
        [Tooltip("ServerRevalidate: server evaluates conditions itself (secure). TrustClient: server trusts client result (for client-local conditions like UI/input).")]
        [SerializeField]
        private ConditionAuthority _authority = ConditionAuthority.ServerRevalidate;

#if MIRROR
        /// <summary>Server-authoritative last result, synced to late-joining clients.</summary>
        [SyncVar]
        private bool _syncResult;

        private float _lastCmdTime;
        private const float CmdRateLimit = 0.05f;
#endif

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

#if MIRROR
        protected override void OnValidate()
        {
            if (isNetworked)
            {
                base.OnValidate();
            }
        }
#endif
        private float _nextEveryFrameCheck;

        private void Update()
        {
            if (_checkMode == CheckMode.EveryFrame)
            {
                // Throttle EveryFrame to avoid per-frame reflection overhead (min ~60hz)
                if (Time.time < _nextEveryFrameCheck) return;
                _nextEveryFrameCheck = Time.time + 0.016f;
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

#if MIRROR
            if (isNetworked && (NeoNetworkState.IsClient || NeoNetworkState.IsServer))
            {
                if (NeoNetworkState.IsClient && !NeoNetworkState.IsServer)
                {
                    if (_authority == ConditionAuthority.TrustClient)
                        CmdClientResult(result); // Client sends its result (for client-local conditions)
                    else
                        CmdRequestCheck();        // Client asks server to re-evaluate (secure)
                    return;
                }
            }
#endif

            InvokeEvents(result);

#if MIRROR
            if (isNetworked && NeoNetworkState.IsServer)
            {
                _syncResult = result;
                RpcInvokeEvents(result);
            }
#endif
        }

        private void InvokeEvents(bool result)
        {
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

#if MIRROR
        /// <summary>Client requests server to re-evaluate conditions (ServerRevalidate mode).</summary>
        [Command(requiresAuthority = false)]
        private void CmdRequestCheck(NetworkConnectionToClient sender = null)
        {
            if (Time.time - _lastCmdTime < CmdRateLimit) return;
            _lastCmdTime = Time.time;

            // Server evaluates conditions itself — never trust client-provided result
            bool result = Evaluate();
            bool changed = !_lastResult.HasValue || _lastResult.Value != result;
            _lastResult = result;

            if (_onlyOnChange && !changed) return;

            _syncResult = result;
            InvokeEvents(result);
            RpcInvokeEvents(result);
        }

        /// <summary>Client sends its local result (TrustClient mode). Use only for client-local conditions.</summary>
        [Command(requiresAuthority = false)]
        private void CmdClientResult(bool result, NetworkConnectionToClient sender = null)
        {
            if (Time.time - _lastCmdTime < CmdRateLimit) return;
            _lastCmdTime = Time.time;

            bool changed = !_lastResult.HasValue || _lastResult.Value != result;
            _lastResult = result;

            if (_onlyOnChange && !changed) return;

            _syncResult = result;
            InvokeEvents(result);
            RpcInvokeEvents(result);
        }

        [ClientRpc(includeOwner = true)]
        private void RpcInvokeEvents(bool result)
        {
            if (isServer) return; // Prevent double invocation on host
            _lastResult = result;
            InvokeEvents(result);
        }

        /// <summary>Late-join: apply server-authoritative result to newly connected client.</summary>
        protected override void ApplyNetworkState()
        {
            _lastResult = _syncResult;
            InvokeEvents(_syncResult);
        }
#endif

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

