using Neo.Reactive;

namespace Neo.Rpg.Runtime
{
    /// <summary>
    ///     Live state of a single stat (e.g. Strength = 18). The final value is
    ///     <c>base + levelGrowth + upgradeBonus + buffBonus + equipmentBonus</c>, and is
    ///     recalculated by <see cref="RpgStatResolver"/> whenever any source changes.
    /// </summary>
    public sealed class RpgStatRuntime
    {
        public string Id { get; }
        public RpgStatDefinition Definition { get; }

        /// <summary>Base value from template / save (before all modifiers).</summary>
        public float BaseValue { get; set; }

        /// <summary>Sum of upgrade points spent on this stat (Dark-Souls flow).</summary>
        public int UpgradeCount { get; set; }

        /// <summary>Final value after all modifiers — written by the resolver.</summary>
        public float CurrentValue { get; private set; }

        public readonly ReactivePropertyFloat ValueState;

        public RpgStatRuntime(RpgStatDefinition definition)
        {
            Definition = definition;
            Id = definition.id.Value;
            BaseValue = definition.baseValue;
            CurrentValue = definition.baseValue;
            ValueState = new ReactivePropertyFloat(CurrentValue);
        }

        /// <summary>Writes the resolved value and pushes the reactive property.</summary>
        public void SetCurrent(float value, bool forceNotify = false)
        {
            float min = Definition.minValue >= 0f ? Definition.minValue : float.MinValue;
            float max = Definition.maxValue >= 0f ? Definition.maxValue : float.MaxValue;
            if (value < min) value = min;
            if (value > max) value = max;
            CurrentValue = value;

            if (forceNotify)
            {
                ValueState.SetValueWithoutNotify(value);
                ValueState.ForceNotify();
            }
            else
            {
                ValueState.Value = value;
            }
        }

        public override string ToString() => $"{Id}={CurrentValue:0.##} (base {BaseValue:0.##}+{UpgradeCount})";
    }
}
