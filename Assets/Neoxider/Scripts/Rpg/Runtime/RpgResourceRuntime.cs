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
            Max = definition.startMax;
            BaseMax = definition.startMax;
            Current = definition.restoreToFull && definition.restoreOnAwake
                ? definition.startMax
                : Mathf.Clamp(definition.startCurrent, 0f, definition.startMax);

            CurrentState = new ReactivePropertyFloat(Current);
            MaxState = new ReactivePropertyFloat(Max);
            PercentState = new ReactivePropertyFloat(Max > 0f ? Current / Max : 0f);
        }

        /// <summary>Sets <see cref="Current"/> and pushes the reactive properties.</summary>
        public void SetCurrent(float value, bool forceNotify = false)
        {
            float min = Definition.canGoBelowZero ? Definition.minValue : 0f;
            float ceiling = Definition.canOverfill ? float.MaxValue : Max;
            float clamped = Mathf.Clamp(value, min, ceiling);
            if (Definition.maxCap > 0f && clamped > Definition.maxCap)
            {
                clamped = Definition.maxCap;
            }

            Current = clamped;
            PushFloat(CurrentState, Current, forceNotify);
            PushFloat(PercentState, Max > 0f ? Mathf.Clamp01(Current / Max) : 0f, forceNotify);
        }

        /// <summary>Sets <see cref="Max"/> and rescales reactive properties. Does NOT clamp Current down
        /// unless current would now overflow — caller decides whether to refill or preserve.</summary>
        public void SetMax(float value, bool clampCurrentToMax, bool forceNotify = false)
        {
            float floor = 0f;
            float ceiling = Definition.maxCap > 0f ? Definition.maxCap : float.MaxValue;
            Max = Mathf.Clamp(value, floor, ceiling);
            PushFloat(MaxState, Max, forceNotify);

            if (clampCurrentToMax && !Definition.canOverfill && Current > Max)
            {
                SetCurrent(Max, forceNotify);
            }
            else
            {
                PushFloat(PercentState, Max > 0f ? Mathf.Clamp01(Current / Max) : 0f, forceNotify);
            }
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
