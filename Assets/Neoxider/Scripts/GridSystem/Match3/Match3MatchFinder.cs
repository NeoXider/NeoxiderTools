using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.GridSystem.Match3
{
    /// <summary>
    ///     Finds horizontal and vertical match groups for Match3 boards.
    /// </summary>
    /// <remarks>
    ///     Only enabled, walkable, unoccupied cells with a positive <c>ContentId</c> take part. All queries read
    ///     <see cref="FieldGenerator.GeneratedSize" /> and never change the board.
    /// </remarks>
    public static class Match3MatchFinder
    {
        /// <summary>
        ///     Scans the board and returns all match groups that satisfy minimum length.
        /// </summary>
        /// <remarks>
        ///     Rows are reported before columns for each depth layer; a cell in a cross or T shape appears in both
        ///     groups. The scan allocates only the returned groups. Use <see cref="HasMatch" /> or
        ///     <see cref="CollectMatchedCells" /> when the groups themselves are not needed.
        /// </remarks>
        /// <param name="generator">Grid source containing tile values in <c>ContentId</c>.</param>
        /// <param name="minMatchLength">Minimum run length to treat as a match.</param>
        /// <returns>List of match groups, each group is a list of cells.</returns>
        public static List<List<FieldCell>> FindMatches(FieldGenerator generator, int minMatchLength = 3)
        {
            List<List<FieldCell>> matches = new();
            Scan(generator, minMatchLength, matches, null, false);
            return matches;
        }

        /// <summary>
        ///     Returns whether the board has at least one run of <paramref name="minMatchLength" /> or more.
        /// </summary>
        /// <remarks>Stops at the first run found and does not allocate.</remarks>
        /// <param name="generator">Grid source containing tile values in <c>ContentId</c>.</param>
        /// <param name="minMatchLength">Minimum run length; non-positive values are treated as one.</param>
        /// <returns>True when <see cref="FindMatches" /> would return at least one group.</returns>
        public static bool HasMatch(FieldGenerator generator, int minMatchLength = 3)
        {
            return Scan(generator, minMatchLength, null, null, true);
        }

        /// <summary>
        ///     Fills <paramref name="results" /> with every cell that belongs to at least one match group.
        /// </summary>
        /// <remarks>
        ///     The set is cleared first and holds the same cells as the union of <see cref="FindMatches" />, each once.
        ///     A warm call with a reused set does not allocate.
        /// </remarks>
        /// <param name="generator">Grid source containing tile values in <c>ContentId</c>.</param>
        /// <param name="minMatchLength">Minimum run length; non-positive values are treated as one.</param>
        /// <param name="results">Destination set, reused between calls.</param>
        /// <returns>Number of distinct matched cells.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="results" /> is null.</exception>
        public static int CollectMatchedCells(FieldGenerator generator, int minMatchLength, HashSet<FieldCell> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            results.Clear();
            Scan(generator, minMatchLength, null, results, false);
            return results.Count;
        }

        private static bool Scan(
            FieldGenerator generator,
            int minMatchLength,
            List<List<FieldCell>> groups,
            HashSet<FieldCell> cells,
            bool stopAtFirst)
        {
            if (generator == null || generator.Config == null || generator.Cells == null)
            {
                return false;
            }

            minMatchLength = Math.Max(1, minMatchLength);
            FieldCell[,,] board = generator.Cells;
            Vector3Int size = generator.GeneratedSize;
            bool found = false;
            for (int z = 0; z < size.z; z++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    if (ScanLine(board, groups, cells, minMatchLength, new Vector3Int(0, y, z), Vector3Int.right,
                            size.x))
                    {
                        found = true;
                        if (stopAtFirst)
                        {
                            return true;
                        }
                    }
                }

                for (int x = 0; x < size.x; x++)
                {
                    if (ScanLine(board, groups, cells, minMatchLength, new Vector3Int(x, 0, z), Vector3Int.up,
                            size.y))
                    {
                        found = true;
                        if (stopAtFirst)
                        {
                            return true;
                        }
                    }
                }
            }

            return found;
        }

        private static bool ScanLine(
            FieldCell[,,] board,
            List<List<FieldCell>> groups,
            HashSet<FieldCell> cells,
            int minMatchLength,
            Vector3Int start,
            Vector3Int step,
            int length)
        {
            int runStart = 0;
            int runLength = 0;
            int currentId = -1;
            bool found = false;

            for (int i = 0; i < length; i++)
            {
                Vector3Int position = start + step * i;
                FieldCell cell = board[position.x, position.y, position.z];
                bool valid = cell != null && cell.IsEnabled && cell.IsWalkable && !cell.IsOccupied &&
                             cell.ContentId > 0;
                int id = valid ? cell.ContentId : -1;

                if (valid && id == currentId)
                {
                    runLength++;
                    continue;
                }

                found |= FlushRun(board, groups, cells, minMatchLength, start, step, runStart, runLength);
                runStart = i;
                runLength = valid ? 1 : 0;
                currentId = id;
            }

            found |= FlushRun(board, groups, cells, minMatchLength, start, step, runStart, runLength);
            return found;
        }

        private static bool FlushRun(
            FieldCell[,,] board,
            List<List<FieldCell>> groups,
            HashSet<FieldCell> cells,
            int minMatchLength,
            Vector3Int start,
            Vector3Int step,
            int runStart,
            int runLength)
        {
            if (runLength < minMatchLength || runLength == 0)
            {
                return false;
            }

            List<FieldCell> group = groups != null ? new List<FieldCell>(runLength) : null;
            for (int i = runStart; i < runStart + runLength; i++)
            {
                Vector3Int position = start + step * i;
                FieldCell cell = board[position.x, position.y, position.z];
                group?.Add(cell);
                cells?.Add(cell);
            }

            if (group != null)
            {
                groups.Add(group);
            }

            return true;
        }
    }
}
