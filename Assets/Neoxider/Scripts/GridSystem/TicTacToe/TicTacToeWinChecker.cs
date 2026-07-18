using System.Collections.Generic;
using UnityEngine;

namespace Neo.GridSystem.TicTacToe
{
    /// <summary>
    ///     Provides winner detection utilities for TicTacToe boards.
    /// </summary>
    public static class TicTacToeWinChecker
    {
        private const int DefaultWinLineLength = 3;

        // WHY: win lines are straight rows/columns/diagonals, independent of the pathfinding movement
        // rule. Scanning each axis in both directions covers all 8 directions from these 4.
        private static readonly Vector3Int[] StraightWinAxes =
        {
            new(1, 0, 0),
            new(0, 1, 0),
            new(1, 1, 0),
            new(-1, 1, 0)
        };

        /// <summary>
        ///     Evaluates the board and returns the winning player or <see cref="TicTacToeCellState.Empty" />.
        ///     Checks rows, columns and both diagonals (default: 3 in a row on a 3x3 board).
        /// </summary>
        /// <param name="generator">Grid generator that stores board data in <c>ContentId</c>.</param>
        /// <param name="winDirections">
        ///     Optional custom win-line axes. When null the canonical straight lines (horizontal, vertical and
        ///     both diagonals) are used. Each axis is scanned in both directions, so only one of each opposing
        ///     pair is needed.
        /// </param>
        /// <returns>Winner state if a winning line exists; otherwise <c>Empty</c>.</returns>
        public static TicTacToeCellState GetWinner(
            FieldGenerator generator,
            IReadOnlyList<Vector3Int> winDirections = null)
        {
            if (generator == null || generator.Config == null)
            {
                return TicTacToeCellState.Empty;
            }

            IReadOnlyList<Vector3Int> directions = winDirections ?? StraightWinAxes;

            if (directions == null || directions.Count == 0)
            {
                return TicTacToeCellState.Empty;
            }

            int lineLength = GetWinLineLength(generator);

            foreach (FieldCell cell in generator.GetAllCells(false))
            {
                if (cell == null || !cell.IsEnabled)
                {
                    continue;
                }

                var state = (TicTacToeCellState)cell.ContentId;
                // WHY: only real player marks form a win; skip Empty and any unset default (e.g. -1).
                if (state != TicTacToeCellState.PlayerX && state != TicTacToeCellState.PlayerO)
                {
                    continue;
                }

                foreach (Vector3Int dir in directions)
                {
                    int count = 1
                                + CountInDirection(generator, cell.Position, dir, state)
                                + CountInDirection(generator, cell.Position, -dir, state);
                    if (count >= lineLength)
                    {
                        return state;
                    }
                }
            }

            return TicTacToeCellState.Empty;
        }

        private static int GetWinLineLength(FieldGenerator generator)
        {
            Vector3Int size = generator.Config.Size;
            int min = size.x;
            if (size.y < min)
            {
                min = size.y;
            }

            if (size.z > 0 && size.z < min)
            {
                min = size.z;
            }

            return min >= DefaultWinLineLength ? min : DefaultWinLineLength;
        }

        private static int CountInDirection(
            FieldGenerator generator,
            Vector3Int start,
            Vector3Int dir,
            TicTacToeCellState state)
        {
            int count = 0;
            Vector3Int pos = start + dir;
            while (true)
            {
                FieldCell c = generator.GetCell(pos);
                if (c == null || !c.IsEnabled || (TicTacToeCellState)c.ContentId != state)
                {
                    break;
                }

                count++;
                pos += dir;
            }

            return count;
        }
    }
}
