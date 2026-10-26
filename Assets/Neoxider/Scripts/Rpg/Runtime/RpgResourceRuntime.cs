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

        /// <summary>Sets <see cref="Current"/> and pushes the reactive properties.</summary>
        public void SetCurrent(float value, bool forceNotify = false)
        {
            SetCurrentGuarded(value, null, out _, forceNotify);
        }

        internal float ClampCurrent(float value)
        {
            float min = Definition.canGoBelowZero ? Definition.minValue : 0f;
            float ceiling = Definition.canOverfill ? float.MaxValue : Max;
            float clamped = Mathf.Clamp(value, min, ceiling);
            if (Definition.maxCap > 0f && clamped > Definition.maxCap)
            {
                clamped = Definition.maxCap;
            }
            return clamped;
        }

        internal bool SetCurrentGuarded(float value, Func<bool> isCurrent, out float change, bool forceNotify = false)
        {
            float clamped = ClampCurrent(value);
            change = clamped - Current;
            Current = clamped;
            PushFloat(CurrentState, Current, forceNotify);
            if (isCurrent != null && !isCurrent())
            {
                return false;
            }
            PushFloat(PercentState, Max > 0f ? Mathf.Clamp01(Current / Max) : 0f, forceNotify);
            return isCurrent == null || isCurrent();
        }

        /// <summary>Sets <see cref="Max"/> and rescales reactive properties. Does NOT clamp Current down
        /// unless current would now overflow — caller decides whether to refill or preserve.</summary>
        public void SetMax(float value, bool clampCurrentToMax, bool forceNotify = false)
        {
            SetMaxGuarded(value, clampCurrentToMax, null, forceNotify);
        }

        internal bool SetMaxGuarded(float value, bool clampCurrentToMax, Func<bool> isCurrent,
            bool forceNotify = false)
        {
            float floor = 0f;
            float ceiling = Definition.maxCap > 0f ? Definition.maxCap : float.MaxValue;
            Max = Mathf.Clamp(value, floor, ceiling);
            PushFloat(MaxState, Max, forceNotify);
            if (isCurrent != null && !isCurrent())
            {
                return false;
            }

            if (clampCurrentToMax && !Definition.canOverfill && Current > Max)
            {
                return SetCurrentGuarded(Max, isCurrent, out _, forceNotify);
            }
            else
            {
                PushFloat(PercentState, Max > 0f ? Mathf.Clamp01(Current / Max) : 0f, forceNotify);
            }

            return isCurrent == null || isCurrent();
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
}
