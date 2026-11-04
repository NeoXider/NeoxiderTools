using System;
using Neo.Reactive;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Core.Resources
{
    /// <summary>
    ///     Inspector config for one resource pool: id, current/max, regen, limits, reactive state and events.
    ///     For NeoCondition use CurrentValue, PercentValue, MaxValue. For UI bind to CurrentState, PercentState, MaxState.
    /// </summary>
    [Serializable]
    public sealed class ResourceEntryInspector
    {
        [Tooltip("Resource id (e.g. HP, Mana)")]
        public string id = "HP";

        [Tooltip("Starting value (replaced by max when Restore On Awake is on).")]
        [Min(0)] public float current = 100f;

        [Tooltip("Upper bound; 0 = uncapped.")]
        [Min(0)] public float max = 100f;

        [Tooltip("Continuous regeneration per second, applied in Regen Interval steps. 0 = off.")]
        [Min(0)] public float regenPerSecond;

        [Tooltip("Seconds between regen steps; each step adds Regen Per Second x interval.")]
        [Min(0)] public float regenInterval = 1f;

        [Tooltip("Largest single decrease (damage/spend); -1 = no limit.")]
        [Min(-1)] public float maxDecreaseAmount = -1f;

        [Tooltip("Largest single increase (heal/regen step); -1 = no limit.")]
        [Min(-1)] public float maxIncreaseAmount = -1f;

        [Tooltip("Start full (current = max) when the component initializes.")]
        public bool restoreOnAwake = true;

        [Tooltip("Allow increases while the pool is at 0 (regenerating mana/energy). Off = depleted HP stays dead.")]
        public bool ignoreCanHeal;

        [Tooltip("Discrete heal added every Heal Delay seconds. 0 = off.")]
        [Min(0)] public float healAmount;

        [Tooltip("Seconds between discrete heals.")]
        [Min(0)] public float healDelay = 1f;

        [Header("Reactive (this pool)")] [Tooltip("Current value; bind UI or subscribe via CurrentState.OnChanged.")]
        public ReactivePropertyFloat CurrentState = new(100f);

        [Tooltip("Current/max 0–1; bind UI or subscribe via PercentState.OnChanged.")]
        public ReactivePropertyFloat PercentState = new(1f);

        [Tooltip("Max value; bind UI or subscribe via MaxState.OnChanged.")]
        public ReactivePropertyFloat MaxState = new(100f);

        [Header("Events (this pool)")]
        [Tooltip("Invoked when current or max changes (current, max). Depleted when current <= 0.")]
        public UnityEventFloatFloat OnChanged = new();

        [Tooltip("(HP) Invoked when resource is decreased (e.g. damage).")]
        public UnityEventFloat OnDamage = new();

        [Tooltip("(HP) Invoked when resource is increased (e.g. heal).")]
        public UnityEventFloat OnHeal = new();

        [Tooltip("(HP) Invoked when depleted (current <= 0).")]
        public UnityEvent OnDeath = new();

        [Tooltip("(HP) Invoked when max is changed.")]
        public UnityEventFloat OnChangeMax = new();

        /// <summary>Current value (for NeoCondition).</summary>
        public float CurrentValue => CurrentState.CurrentValue;

        /// <summary>Current/max 0–1 (for NeoCondition).</summary>
        public float PercentValue => PercentState.CurrentValue;

        /// <summary>Max value (for NeoCondition).</summary>
        public float MaxValue => MaxState.CurrentValue;
    }
}
