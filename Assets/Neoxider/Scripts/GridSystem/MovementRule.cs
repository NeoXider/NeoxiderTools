using System.Collections.Generic;
using UnityEngine;

namespace Neo.GridSystem
{
    /// <summary>
    ///     Defines neighbor offset rules used to traverse grid cells.
    /// </summary>
    public class MovementRule
    {
        /// <summary>
        ///     Creates movement rule from explicit direction offsets.
        /// </summary>
        /// <param name="directions">Neighbor offset collection.</param>
        public MovementRule(IEnumerable<Vector3Int> directions)
        {
            Directions = new List<Vector3Int>(directions);
        }

        /// <summary>
        ///     Neighbor offsets used by this rule.
        /// </summary>
        public List<Vector3Int> Directions { get; private set; }

        /// <summary>
        ///     2D orthogonal (4-way) movement rule.
        /// </summary>
        public static MovementRule FourDirections2D => new(new[]
        {
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0)
        });

        /// <summary>
        ///     2D 8-way movement rule (orthogonal + diagonal).
        /// </summary>
        public static MovementRule EightDirections2D => new(new[]
        {
            new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0),
            new Vector3Int(1, 1, 0), new Vector3Int(-1, 1, 0), new Vector3Int(1, -1, 0), new Vector3Int(-1, -1, 0)
        });

        /// <summary>
        ///     2D diagonal-only movement rule.
        /// </summary>
        public static MovementRule DiagonalDirections2D => new(new[]
        {
            new Vector3Int(1, 1, 0), new Vector3Int(-1, 1, 0), new Vector3Int(1, -1, 0), new Vector3Int(-1, -1, 0)
        });

        /// <summary>
        ///     3D orthogonal (6-way) movement rule.
        /// </summary>
        public static MovementRule SixDirections3D => new(new[]
        {
            new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0),
            new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1)
        });

        /// <summary>
        ///     3D 18-way movement rule (orthogonal + edge neighbors).
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
        ///     3D full 26-neighbor movement rule.
        /// </summary>
        public static MovementRule TwentySixDirections3D => new(Get26Directions3D());

        /// <summary>
        ///     Hex-like movement rule in X-oriented axial layout.
        /// </summary>
        public static MovementRule HexDirectionsX => new(new[]
        {
            new Vector3Int(+1, 0, 0), new Vector3Int(-1, 0, 0),
            new Vector3Int(0, +1, 0), new Vector3Int(0, -1, 0),
            new Vector3Int(+1, -1, 0), new Vector3Int(-1, +1, 0)
        });

        /// <summary>
        ///     Hex-like movement rule in Y-oriented axial layout.
        /// </summary>
        public static MovementRule HexDirectionsY => new(new[]
        {
            new Vector3Int(0, +1, 0), new Vector3Int(0, -1, 0),
            new Vector3Int(+1, 0, 0), new Vector3Int(-1, 0, 0),
            new Vector3Int(+1, -1, 0), new Vector3Int(-1, +1, 0)
        });

        private static IEnumerable<Vector3Int> Get26Directions3D()
        {
            for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
            for (int z = -1; z <= 1; z++)
            {
                if (x == 0 && y == 0 && z == 0)
                {
                    continue;
                }

                yield return new Vector3Int(x, y, z);
            }
        }
    }
}