using System;
using Neo.Reactive;
using UnityEngine;

namespace Neo.Rpg.Runtime
{
    /// <summary>
    ///     Live state of one resource pool on a runtime character. Owns the reactive properties
    ///     bound by UI / NeoCondition / `NoCodeBindText`.
    ///     <para>Created and updated by <c>RpgCharacter</c>. Not a <see cref="MonoBehaviour"/> — there is no
    ///     per-pool component in the hierarchy; the character holds the dictionary itself.</para>
    /// </summary>
    public sealed class RpgResourceRuntime
    {
        public string Id { get; }
        public RpgResourceDefinition Definition { get; }

        public float Current { get; private set; }
        public float Max { get; private set; }

        /// <summary>Max value as defined by the template (without buffs / upgrades). Used as a base
        /// when modifiers are recalculated.</summary>
        public float BaseMax { get; set; }

        /// <summary>Live regen amount per second resolved from definition + scaling stat + buffs.</summary>
        public float ResolvedRegenPerSecond { get; set; }

        /// <summary>Time (seconds) the regen is paused due to <c>pauseAfterSpend</c> / <c>pauseAfterDamage</c>.</summary>
        public float RegenPauseRemaining { get; set; }

        /// <summary>Time accumulator for <see cref="RpgRegenMode.FlatPerTick"/> / <c>PercentMaxPerTick</c>.</summary>
        public float TickAccumulator { get; set; }

        public readonly ReactivePropertyFloat CurrentState;
        public readonly ReactivePropertyFloat MaxState;
        public readonly ReactivePropertyFloat PercentState;

        public RpgResourceRuntime(RpgResourceDefinition definition)
        {
            Definition = definition;
            Id = definition.id.Value;
            BaseMax = definition.startMax;
            CurrentState = new ReactivePropertyFloat(0f);
            MaxState = new ReactivePropertyFloat(0f);
            PercentState = new ReactivePropertyFloat(0f);
            SetMax(definition.startMax, false);
            SetCurrent(definition.restoreToFull && definition.restoreOnAwake ? Max : definition.startCurrent);
            ResolvedRegenPerSecond = RpgStatResolver.ResolveRegen(this, null, null);
        }

        /// <summary>Sets <see cref="Current"/> and pushes the reactive properties. A NaN value keeps the current value.</summary>
        public void SetCurrent(float value, bool forceNotify = false)
        {
            SetCurrentGuarded(value, RpgResourceGuard.None, out _, forceNotify);
        }

        internal float ClampCurrent(float value)
        {
            // WHY: Mathf.Clamp passes NaN through; one NaN Current makes every comparison false (never dead, never full).
            if (float.IsNaN(value))
            {
                return Current;
            }

            float min = Definition.canGoBelowZero ? Definition.minValue : 0f;
            float ceiling = Definition.canOverfill ? float.MaxValue : Max;
            float clamped = Mathf.Clamp(value, min, ceiling);
            if (Definition.maxCap > 0f && clamped > Definition.maxCap)
            {
                clamped = Definition.maxCap;
            }
            return clamped;
        }

        internal bool SetCurrentGuarded(float value, in RpgResourceGuard guard, out float change,
            bool forceNotify = false)
        {
            float clamped = ClampCurrent(value);
            change = clamped - Current;
            Current = clamped;
            PushFloat(CurrentState, Current, forceNotify);
            if (!guard.IsCurrent())
            {
                return false;
            }
            PushFloat(PercentState, Max > 0f ? Mathf.Clamp01(Current / Max) : 0f, forceNotify);
            return guard.IsCurrent();
        }

        /// <summary>Sets <see cref="Max"/> and rescales reactive properties. Does NOT clamp Current down
        /// unless current would now overflow — caller decides whether to refill or preserve. A NaN value keeps
        /// the current Max.</summary>
        public void SetMax(float value, bool clampCurrentToMax, bool forceNotify = false)
        {
            SetMaxGuarded(value, clampCurrentToMax, RpgResourceGuard.None, forceNotify);
        }

        internal bool SetMaxGuarded(float value, bool clampCurrentToMax, in RpgResourceGuard guard,
            bool forceNotify = false)
        {
            if (float.IsNaN(value))
            {
                value = Max;
            }

            float floor = 0f;
            float ceiling = Definition.maxCap > 0f ? Definition.maxCap : float.MaxValue;
            Max = Mathf.Clamp(value, floor, ceiling);
            PushFloat(MaxState, Max, forceNotify);
            if (!guard.IsCurrent())
            {
                return false;
            }

            if (clampCurrentToMax && !Definition.canOverfill && Current > Max)
            {
                return SetCurrentGuarded(Max, guard, out _, forceNotify);
            }

            PushFloat(PercentState, Max > 0f ? Mathf.Clamp01(Current / Max) : 0f, forceNotify);
            return guard.IsCurrent();
        }

        public bool IsDepleted => Current <= 0f && !Definition.canGoBelowZero;

        public override string ToString()
        {
            return $"{Id} {Current:0.##}/{Max:0.##}";
        }

        private static void PushFloat(ReactivePropertyFloat property, float value, bool forceNotify)
        {
            if (forceNotify)
            {
                property.SetValueWithoutNotify(value);
                property.ForceNotify();
            }
            else
            {
                property.Value = value;
            }
        }
    }

    /// <summary>
    ///     Allocation-free staleness check for one guarded resource write: the owning service generation, the exact
    ///     runtime instance under its id, and an optional owner predicate must all still hold after each publication.
    /// </summary>
    // WHY: a value type instead of a captured Func<bool>; regen ticks call guarded writes every frame per resource.
    internal readonly struct RpgResourceGuard
    {
        internal static readonly RpgResourceGuard None = default;

        private readonly RpgCharacterResourceService _service;
        private readonly int _generation;
        private readonly string _id;
        private readonly RpgResourceRuntime _resource;
        private readonly Func<bool> _ownerIsCurrent;

        internal RpgResourceGuard(RpgCharacterResourceService service, int generation, string id,
            RpgResourceRuntime resource, Func<bool> ownerIsCurrent = null)
        {
            _service = service;
            _generation = generation;
            _id = id;
            _resource = resource;
            _ownerIsCurrent = ownerIsCurrent;
        }

        internal bool IsCurrent()
        {
            if (_service == null)
            {
                return true;
            }

            return _service.IsCurrent(_generation, _id, _resource) && (_ownerIsCurrent == null || _ownerIsCurrent());
        }
    }
}
