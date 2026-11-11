using System;
using UnityEngine;

namespace Neo.Abilities
{
    /// <summary>
    ///     One typed contribution of a modifier to a unit property, e.g. { move_speed, Mul, 0.5 } for a slow.
    ///     Property ids are an open registry — see <see cref="AbilityProperties" /> for the built-in names.
    ///     <see cref="ValueByLevel" /> makes the strength follow the ability level captured on the modifier
    ///     (a slow of 20 / 30 / 40%); per-stack scaling adds on top.
    /// </summary>
    [Serializable]
    public struct PropertyContribution
    {
        [Tooltip("Property id from the open registry (see AbilityProperties for built-ins).")]
        public string PropertyId;

        [Tooltip("How this contribution combines: Add (flat), Mul (percent), Max (floor).")]
        public PropertyOp Op;

        [Tooltip("Contribution value. For Mul, 1 = no change, 1.25 = +25%, 0.5 = halved.")]
        public float Value;

        [Tooltip("Optional per-stack scaling: effective value = Value + PerStackValue * (stacks - 1).")]
        public float PerStackValue;

        [Tooltip("Optional per-ability-level values, indexed by the ability level captured on the modifier " +
                 "(clamped). Empty ⇒ use Value. E.g. Mul [0.8, 0.7, 0.6] = a 20 / 30 / 40% slow.")]
        public float[] ValueByLevel;

        public PropertyContribution(string propertyId, PropertyOp op, float value, float perStackValue = 0f)
        {
            PropertyId = propertyId;
            Op = op;
            Value = value;
            PerStackValue = perStackValue;
            ValueByLevel = null;
        }

        /// <summary>
        ///     Value + PerStackValue * (stacks - 1) from the flat <see cref="Value" />, ignoring <see cref="ValueByLevel" />;
        ///     the modifier engine uses <see cref="ValueFor" />.
        /// </summary>
        public float ValueForStacks(int stacks)
        {
            int extra = stacks > 1 ? stacks - 1 : 0;
            return Value + PerStackValue * extra;
        }

        /// <summary>
        ///     Effective value for a stack count and the ability level captured on the modifier:
        ///     (<see cref="ValueByLevel" /> sampled by level, else <see cref="Value" />) + PerStackValue * (stacks - 1).
        /// </summary>
        public float ValueFor(int stacks, int abilityLevel)
        {
            int extra = stacks > 1 ? stacks - 1 : 0;
            return LeveledValueResolver.SampleByLevel(ValueByLevel, abilityLevel, Value) + PerStackValue * extra;
        }
    }
}
