using UnityEngine;

namespace Neo.GridSystem.TicTacToe
{
    /// <summary>
    ///     Provides winner detection utilities for TicTacToe boards.
    /// </summary>
    public static class TicTacToeWinChecker
    {
        /// <summary>
        ///     Evaluates the board and returns the winning player or <see cref="TicTacToeCellState.Empty" />.
        /// </summary>
        /// <param name="generator">Grid generator that stores board data in <c>ContentId</c>.</param>
        /// <returns>Winner state if a winning line exists; otherwise <c>Empty</c>.</returns>
        public static TicTacToeCellState GetWinner(FieldGenerator generator)
        {
            if (generator == null || generator.Config == null)
            {
                return TicTacToeCellState.Empty;
            }

            Vector3Int size = generator.Config.Size;
            int maxX = size.x;
            int maxY = size.y;
            if (maxX <= 0 || maxY <= 0 || maxX != maxY)
            {
                return TicTacToeCellState.Empty;
            }

            for (int i = 0; i < maxX; i++)
            {
                TicTacToeCellState row = CheckLine(generator, i, true, maxX);
                if (row != TicTacToeCellState.Empty)
                {
                    return row;
                }

                TicTacToeCellState col = CheckLine(generator, i, false, maxY);
                if (col != TicTacToeCellState.Empty)
                {
                    return col;
                }
            }

            TicTacToeCellState diag = CheckDiagonal(generator, true, maxX);
            if (diag != TicTacToeCellState.Empty)
            {
                return diag;
            }

            return CheckDiagonal(generator, false, maxX);
        }

        private static TicTacToeCellState CheckLine(FieldGenerator generator, int fixedIndex, bool row, int len)
        {
            TicTacToeCellState first = TicTacToeCellState.Empty;
            for (int i = 0; i < len; i++)
            {
                Vector3Int pos = row ? new Vector3Int(i, fixedIndex, 0) : new Vector3Int(fixedIndex, i, 0);
                FieldCell cell = generator.GetCell(pos);
                if (cell == null || !cell.IsEnabled)
                {
                    return TicTacToeCellState.Empty;
                }

                TicTacToeCellState state = (TicTacToeCellState)cell.ContentId;
                if (state == TicTacToeCellState.Empty)
                {
                    return TicTacToeCellState.Empty;
                }

                if (i == 0)
                {
                    first = state;
                }
                else if (state != first)
                {
                    return TicTacToeCellState.Empty;
                }
            }

            return first;
        }

        private static TicTacToeCellState CheckDiagonal(FieldGenerator generator, bool main, int len)
        {
            TicTacToeCellState first = TicTacToeCellState.Empty;
            for (int i = 0; i < len; i++)
            {
                int x = i;
                int y = main ? i : len - 1 - i;
                FieldCell cell = generator.GetCell(new Vector3Int(x, y, 0));
                if (cell == null || !cell.IsEnabled)
                {
                    return TicTacToeCellState.Empty;
                }

                TicTacToeCellState state = (TicTacToeCellState)cell.ContentId;
                if (state == TicTacToeCellState.Empty)
                {
                    return TicTacToeCellState.Empty;
                }

                if (i == 0)
                {
                    first = state;
                }
                else if (state != first)
                {
                    return TicTacToeCellState.Empty;
                }
            }

            return first;
        }
    }
}