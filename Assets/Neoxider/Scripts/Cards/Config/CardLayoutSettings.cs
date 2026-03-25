using System;
using UnityEngine;

namespace Neo.Cards
{
    /// <summary>
    ///     Layout parameters for <see cref="CardLayoutCalculator" />.
    /// </summary>
    [Serializable]
    public struct CardLayoutSettings
    {
        public float Spacing;
        public float ArcAngle;
        public float ArcRadius;
        public int GridColumns;
        public float GridRowSpacing;
        public float StackStep;
        public float ScatteredRadius;
        public float ScatteredRotationRange;
        public Vector3 PositionJitter;
        public Vector3 RotationJitter;

        /// <summary>
        ///     Default values for layout calculation.
        /// </summary>
        public static CardLayoutSettings Default => new()
        {
            Spacing = 60f,
            ArcAngle = 30f,
            ArcRadius = 400f,
            GridColumns = 5,
            GridRowSpacing = 80f,
            StackStep = 2f,
            ScatteredRadius = 120f,
            ScatteredRotationRange = 20f,
            PositionJitter = Vector3.zero,
            RotationJitter = Vector3.zero
        };
    }
}
