using System.Collections.Generic;
using UnityEngine;

namespace Neo.GridSystem.TicTacToe
{
    /// <summary>
    ///     Provides winner detection utilities for TicTacToe and other n-in-a-row boards (Gomoku, custom shapes).
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
            return GetWinner(generator, 0, winDirections);
        }

        /// <summary>
        ///     Evaluates the board with an explicit win length, for example 5 for Gomoku on a 15x15 board.
        /// </summary>
        /// <param name="generator">Grid generator that stores board data in <c>ContentId</c>.</param>
        /// <param name="winLineLength">Marks in a row needed to win; zero or negative uses the automatic rule.</param>
        /// <param name="winDirections">Optional custom win-line axes (see the other overload).</param>
        /// <returns>Winner state if a winning line exists; otherwise <c>Empty</c>.</returns>
        public static TicTacToeCellState GetWinner(
            FieldGenerator generator,
            int winLineLength,
            IReadOnlyList<Vector3Int> winDirections = null)
        {
            return TryGetWinningLine(generator, null, out TicTacToeCellState winner, winLineLength, winDirections)
                ? winner
                : TicTacToeCellState.Empty;
        }

        /// <summary>
        ///     Finds the first winning line and reports its cells, so views can highlight it.
        /// </summary>
        /// <param name="generator">Grid generator that stores board data in <c>ContentId</c>.</param>
        /// <param name="line">
        ///     Optional caller-owned list, cleared first. Receives every cell of the winning run from one end to the
        ///     other; the run can be longer than the win length.
        /// </param>
        /// <param name="winner">Winning player, or <c>Empty</c>.</param>
        /// <param name="winLineLength">Marks in a row needed to win; zero or negative uses the automatic rule.</param>
        /// <param name="winDirections">Optional custom win-line axes.</param>
        /// <returns>True when a winning line exists.</returns>
        public static bool TryGetWinningLine(
            FieldGenerator generator,
            List<Vector3Int> line,
            out TicTacToeCellState winner,
            int winLineLength = 0,
            IReadOnlyList<Vector3Int> winDirections = null)
        {
            line?.Clear();
            winner = TicTacToeCellState.Empty;
            if (generator == null || generator.Config == null || generator.Cells == null)
            {
                return false;
            }

            IReadOnlyList<Vector3Int> directions = winDirections ?? StraightWinAxes;
            if (directions.Count == 0)
            {
                return false;
            }

            int lineLength = ResolveWinLineLength(generator, winLineLength);
            foreach (FieldCell cell in generator.Cells)
            {
                if (!IsPlayable(cell))
                {
                    continue;
                }

                TicTacToeCellState state = (TicTacToeCellState)cell.ContentId;
                // WHY: only real player marks form a win; skip Empty and any unset default (e.g. -1).
                if (state != TicTacToeCellState.PlayerX && state != TicTacToeCellState.PlayerO)
                {
                    continue;
                }

                for (int i = 0; i < directions.Count; i++)
                {
                    Vector3Int dir = directions[i];
                    if (dir == Vector3Int.zero)
                    {
                        continue;
                    }

                    int backward = CountInDirection(generator, cell.Position, -dir, state);
                    int forward = CountInDirection(generator, cell.Position, dir, state);
                    if (1 + backward + forward < lineLength)
                    {
                        continue;
                    }

                    winner = state;
                    if (line != null)
                    {
                        Vector3Int position = cell.Position - dir * backward;
                        for (int step = 0; step <= backward + forward; step++)
                        {
                            line.Add(position);
                            position += dir;
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Returns the effective win length: <paramref name="requestedLength" /> when positive; otherwise 3, raised to
        ///     the smallest generated dimension only when every dimension exceeds 3 (for example 4 on a 4x4x4 cube).
        /// </summary>
        public static int ResolveWinLineLength(FieldGenerator generator, int requestedLength = 0)
        {
            if (requestedLength > 0)
            {
                return requestedLength;
            }

            if (generator == null)
            {
                return DefaultWinLineLength;
            }

            Vector3Int size = generator.GeneratedSize;
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

        private static bool IsPlayable(FieldCell cell)
        {
            return cell != null && cell.IsEnabled && cell.IsWalkable && !cell.IsOccupied;
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
                if (!IsPlayable(c) || (TicTacToeCellState)c.ContentId != state)
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
