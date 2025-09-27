using System;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Neo.GridSystem
{
    /// <summary>
    ///     Конфигуратор генератора поля: размер, тип сетки, точка отсчёта, правила движения и т.д.
    /// </summary>
    [Serializable]
    public class FieldGeneratorConfig
    {
#if ODIN_INSPECTOR
        [BoxGroup("Размер поля")]
#endif
        public Vector3Int Size = new(10, 10, 1);

#if ODIN_INSPECTOR
        [BoxGroup("Правила движения")]
#endif
        public MovementRule MovementRule = MovementRule.FourDirections2D;

        public FieldGeneratorConfig()
        {
        }

        public FieldGeneratorConfig(Vector3Int size, MovementRule movementRule = null)
        {
            Size = size;
            MovementRule = movementRule ?? MovementRule.FourDirections2D;
        }
    }

    public enum GridType
    {
        Rectangular,
        Hexagonal,
        Custom
    }
}