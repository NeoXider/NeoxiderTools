using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.GridSystem.Match3
{
    /// <summary>
    ///     Finds horizontal and vertical match groups for Match3 boards.
    /// </summary>
    public static class Match3MatchFinder
    {
        /// <summary>
        ///     Scans the board and returns all match groups that satisfy minimum length.
        /// </summary>
        /// <param name="generator">Grid source containing tile values in <c>ContentId</c>.</param>
        /// <param name="minMatchLength">Minimum run length to treat as a match.</param>
        /// <returns>List of match groups, each group is a list of cells.</returns>
        public static List<List<FieldCell>> FindMatches(FieldGenerator generator, int minMatchLength = 3)
        {
            List<List<FieldCell>> matches = new();
            if (generator == null || generator.Config == null)
            {
                return matches;
            }

            Vector3Int size = generator.Config.Size;
            for (int z = 0; z < size.z; z++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    ScanLine(generator, matches, minMatchLength, size.x, index => new Vector3Int(index, y, z));
                }

                for (int x = 0; x < size.x; x++)
                {
                    ScanLine(generator, matches, minMatchLength, size.y, index => new Vector3Int(x, index, z));
                }
            }

            return matches;
        }

        private static void ScanLine(
            FieldGenerator generator,
            List<List<FieldCell>> matches,
            int minMatchLength,
            int length,
            Func<int, Vector3Int> posFactory)
        {
            List<FieldCell> run = new();
            int currentId = -1;

            for (int i = 0; i < length; i++)
            {
                FieldCell cell = generator.GetCell(posFactory(i));
                bool valid = cell != null && cell.IsEnabled && cell.IsWalkable && !cell.IsOccupied &&
                             cell.ContentId > 0;
                int id = valid ? cell.ContentId : -1;

                if (valid && id == currentId)
                {
                    run.Add(cell);
                    continue;
                }

                FlushRun(run, minMatchLength, matches);

                run.Clear();
                if (valid)
                {
                    run.Add(cell);
                }

                currentId = id;
            }

            FlushRun(run, minMatchLength, matches);
        }

        private static void FlushRun(List<FieldCell> run, int minMatchLength, List<List<FieldCell>> matches)
        {
            if (run.Count >= minMatchLength)
            {
                matches.Add(new List<FieldCell>(run));
            }
        }
    }
}