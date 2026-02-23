using System.Collections.Generic;
using UnityEngine;

namespace Neo.GridSystem.TicTacToe
{
    /// <summary>
    ///     Provides winner detection utilities for TicTacToe boards.
    ///     Uses <see cref="FieldGenerator.Config.MovementRule" /> to determine which lines count as wins.
    /// </summary>
    public static class TicTacToeWinChecker
    {
        private const int DefaultWinLineLength = 3;

        /// <summary>
        ///     Evaluates the board and returns the winning player or <see cref="TicTacToeCellState.Empty" />.
        ///     Checks lines along each direction from <see cref="MovementRule" /> (default: 3 in a row).
        /// </summary>
        /// <param name="generator">Grid generator that stores board data in <c>ContentId</c>.</param>
        /// <returns>Winner state if a winning line exists; otherwise <c>Empty</c>.</returns>
        public static TicTacToeCellState GetWinner(FieldGenerator generator)
        {
            if (generator == null || generator.Config == null)
            {
                return TicTacToeCellState.Empty;
            }

            IReadOnlyList<Vector3Int> directions = generator.Config.MovementRule != null
                ? generator.Config.MovementRule.Directions
                : MovementRule.FourDirections2D.Directions;

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

                TicTacToeCellState state = (TicTacToeCellState)cell.ContentId;
                if (state == TicTacToeCellState.Empty)
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