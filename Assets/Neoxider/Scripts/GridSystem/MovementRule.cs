using System.Collections.Generic;
using UnityEngine;

namespace Neo.GridSystem
{
    /// <summary>
    ///     Описывает правила движения по полю (например, 4/8/6/18/26 направлений, 3D-соседство, гекс, кастомные).
    /// </summary>
    public class MovementRule
    {
        public MovementRule(IEnumerable<Vector3Int> directions)
        {
            Directions = new List<Vector3Int>(directions);
        }

        /// <summary>
        ///     Список смещений для соседей (например, (1,0,0), (0,1,0), ...)
        /// </summary>
        public List<Vector3Int> Directions { get; private set; }

        /// <summary>
        ///     FourDirections2D (4 стороны):
        ///     ↑
        ///     ←  ■  →
        ///     ↓
        /// </summary>
        public static MovementRule FourDirections2D => new(new[]
        {
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0)
        });

        /// <summary>
        ///     EightDirections2D (8 направлений):
        ///     ↖  ↑  ↗
        ///     ←  ■  →
        ///     ↙  ↓  ↘
        /// </summary>
        public static MovementRule EightDirections2D => new(new[]
        {
            new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0),
            new Vector3Int(1, 1, 0), new Vector3Int(-1, 1, 0), new Vector3Int(1, -1, 0), new Vector3Int(-1, -1, 0)
        });

        /// <summary>
        ///     DiagonalDirections2D (только диагонали):
        ///     ↖     ↗
        ///     ■
        ///     ↙     ↘
        /// </summary>
        public static MovementRule DiagonalDirections2D => new(new[]
        {
            new Vector3Int(1, 1, 0), new Vector3Int(-1, 1, 0), new Vector3Int(1, -1, 0), new Vector3Int(-1, -1, 0)
        });

        /// <summary>
        ///     SixDirections3D (ортогональные, 3D):
        ///     x: ← →
        ///     y: ↑ ↓
        ///     z: ⬆ (вверх по Z), ⬇ (вниз по Z)
        /// </summary>
        public static MovementRule SixDirections3D => new(new[]
        {
            new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0),
            new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1)
        });

        /// <summary>
        ///     EighteenDirections3D (ортогональные + рёберные, 3D):
        ///     Вокруг центральной ячейки ■ — 6 ортогональных + 12 рёберных направлений
        /// </summary>
        public static MovementRule EighteenDirections3D => new(new[]
        {
            // 6 ортогональных
            new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0),
            new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1),
            // 12 рёберных
            new Vector3Int(1, 1, 0), new Vector3Int(1, -1, 0), new Vector3Int(-1, 1, 0), new Vector3Int(-1, -1, 0),
            new Vector3Int(1, 0, 1), new Vector3Int(1, 0, -1), new Vector3Int(-1, 0, 1), new Vector3Int(-1, 0, -1),
            new Vector3Int(0, 1, 1), new Vector3Int(0, 1, -1), new Vector3Int(0, -1, 1), new Vector3Int(0, -1, -1)
        });

        /// <summary>
        ///     TwentySixDirections3D (все возможные соседи, 3D):
        ///     Вокруг центральной ячейки ■ — все возможные смещения x, y, z ∈ {-1, 0, 1}, кроме (0,0,0)
        /// </summary>
        public static MovementRule TwentySixDirections3D => new(Get26Directions3D());

        /// <summary>
        ///     HexDirectionsX (гексагональная сетка, ось X):
        ///     ↗   ↖
        ///     →  ■  ←
        ///     ↘   ↙
        /// </summary>
        public static MovementRule HexDirectionsX => new(new[]
        {
            new Vector3Int(+1, 0, 0), new Vector3Int(-1, 0, 0),
            new Vector3Int(0, +1, 0), new Vector3Int(0, -1, 0),
            new Vector3Int(+1, -1, 0), new Vector3Int(-1, +1, 0)
        });

        /// <summary>
        ///     HexDirectionsY (гексагональная сетка, ось Y):
        ///     ↑   ↓
        ///     ↗  ■  ↙
        ///     →   ←
        /// </summary>
        public static MovementRule HexDirectionsY => new(new[]
        {
            new Vector3Int(0, +1, 0), new Vector3Int(0, -1, 0),
            new Vector3Int(+1, 0, 0), new Vector3Int(-1, 0, 0),
            new Vector3Int(+1, -1, 0), new Vector3Int(-1, +1, 0)
        });

        private static IEnumerable<Vector3Int> Get26Directions3D()
        {
            for (var x = -1; x <= 1; x++)
            for (var y = -1; y <= 1; y++)
            for (var z = -1; z <= 1; z++)
            {
                if (x == 0 && y == 0 && z == 0) continue;
                yield return new Vector3Int(x, y, z);
            }
        }
    }
}