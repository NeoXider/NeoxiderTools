using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.GridSystem
{
    /// <summary>
    /// Defines horizontal/vertical board anchor relative to generator transform.
    /// </summary>
    public enum GridOrigin2D
    {
        BottomLeft,
        BottomCenter,
        BottomRight,
        MiddleLeft,
        Center,
        MiddleRight,
        TopLeft,
        TopCenter,
        TopRight
    }

    /// <summary>
    /// Defines depth anchor relative to generator transform.
    /// </summary>
    public enum GridOriginDepth
    {
        Front,
        Center,
        Back
    }

    /// <summary>
    /// Defines default passability interpretation for pathfinding calls.
    /// </summary>
    public enum CellPassabilityMode
    {
        WalkableOnly,
        WalkableEnabledAndUnoccupied
    }

    /// <summary>
    /// Serializable configuration for <see cref="FieldGenerator"/>.
    /// </summary>
    [Serializable]
    public class FieldGeneratorConfig
    {
#if ODIN_INSPECTOR
        [BoxGroup("Размер поля")]
#endif
        public Vector3Int Size = new(10, 10, 1);

        public GridType GridType = GridType.Rectangular;
#if ODIN_INSPECTOR
        [BoxGroup("Правила движения")]
#endif
        public MovementRule MovementRule = MovementRule.FourDirections2D;

        [Header("Shape")]
        public GridShapeMask ShapeMask;

        public List<Vector3Int> DisabledCells = new();
        public List<Vector3Int> ForcedEnabledCells = new();
        public List<Vector3Int> BlockedCells = new();
        public List<Vector3Int> ForcedWalkableCells = new();

        [Header("Pathfinding")]
        public CellPassabilityMode PassabilityMode = CellPassabilityMode.WalkableEnabledAndUnoccupied;

        [Header("Origin")]
        public GridOrigin2D Origin2D = GridOrigin2D.Center;
        public GridOriginDepth OriginDepth = GridOriginDepth.Center;
        public Vector3Int OriginOffset = Vector3Int.zero;

        /// <summary>
        /// Creates default field generator configuration.
        /// </summary>
        public FieldGeneratorConfig()
        {
        }

        /// <summary>
        /// Creates field generator configuration with explicit size, movement and shape type.
        /// </summary>
        /// <param name="size">Grid dimensions in cells.</param>
        /// <param name="movementRule">Neighbor offsets rule. Uses 4-way 2D when null.</param>
        /// <param name="gridType">Base grid type.</param>
        public FieldGeneratorConfig(
            Vector3Int size,
            MovementRule movementRule = null,
            GridType gridType = GridType.Rectangular)
        {
            Size = size;
            GridType = gridType;
            MovementRule = movementRule ?? MovementRule.FourDirections2D;
        }
    }

    /// <summary>
    /// Supported base grid topology presets.
    /// </summary>
    public enum GridType
    {
        Rectangular,
        Hexagonal,
        Custom
    }
}