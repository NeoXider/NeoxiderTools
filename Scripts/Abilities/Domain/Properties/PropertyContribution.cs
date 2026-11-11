using System;
using UnityEngine;

namespace Neo.Abilities
{
    /// <summary>
    ///     One typed contribution of a modifier to a unit property, e.g. { move_speed, Mul, 0.5 } for a slow.
    ///     Property ids are an open registry — see <see cref="AbilityProperties" /> for the built-in names.
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

        public PropertyContribution(string propertyId, PropertyOp op, float value, float perStackValue = 0f)
        {
            PropertyId = propertyId;
            Op = op;
            Value = value;
            PerStackValue = perStackValue;
        }

        public float ValueForStacks(int stacks)
        {
            int extra = stacks > 1 ? stacks - 1 : 0;
            return Value + PerStackValue * extra;
        }
    }
}
