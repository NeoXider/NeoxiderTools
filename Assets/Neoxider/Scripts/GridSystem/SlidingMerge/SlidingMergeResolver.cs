using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.GridSystem.SlidingMerge
{
    /// <summary>
    ///     Pure grid resolver for 2048-like slide and merge rules.
    /// </summary>
    public static class SlidingMergeResolver
    {
        public static bool CanSlide(
            FieldGenerator generator,
            SlidingMergeDirection direction,
            int emptyContentId = 0,
            Func<int, int, bool> canMerge = null,
            Func<int, int, int> merge = null)
        {
            return Evaluate(generator, direction.ToVector(), emptyContentId, canMerge, merge, false).Changed;
        }

        public static SlidingMergeResult Slide(
            FieldGenerator generator,
            SlidingMergeDirection direction,
            int emptyContentId = 0,
            Func<int, int, bool> canMerge = null,
            Func<int, int, int> merge = null)
        {
            return Evaluate(generator, direction.ToVector(), emptyContentId, canMerge, merge, true);
        }

        public static bool TrySpawnRandomContent(
            FieldGenerator generator,
            int contentId,
            int emptyContentId = 0,
            bool requireWalkable = true)
        {
            if (generator == null)
            {
                return false;
            }

            List<FieldCell> emptyCells = new();
            foreach (FieldCell cell in generator.GetAllCells(false))
            {
                if (!IsUsable(cell, requireWalkable) || cell.ContentId != emptyContentId)
                {
                    continue;
                }

                emptyCells.Add(cell);
            }

            if (emptyCells.Count == 0)
            {
                return false;
            }

            FieldCell target = emptyCells[UnityEngine.Random.Range(0, emptyCells.Count)];
            target.ContentId = contentId;
            generator.OnCellStateChanged.Invoke(target);
            return true;
        }

        private static SlidingMergeResult Evaluate(
            FieldGenerator generator,
            Vector3Int direction,
            int emptyContentId,
            Func<int, int, bool> canMerge,
            Func<int, int, int> merge,
            bool mutate)
        {
            SlidingMergeResult result = new();
            if (generator == null || generator.Config == null || generator.Cells == null ||
                direction == Vector3Int.zero)
            {
                return result;
            }

            canMerge ??= AreEqualNonEmpty;
            merge ??= DoubleFirstValue;

            foreach (List<FieldCell> line in EnumerateLines(generator, direction))
            {
                List<FieldCell> segment = new();
                foreach (FieldCell cell in line)
                {
                    if (!IsUsable(cell, true))
                    {
                        ResolveSegment(segment, emptyContentId, canMerge, merge, mutate, result, generator);
                        segment.Clear();
                        continue;
                    }

                    segment.Add(cell);
                }

                ResolveSegment(segment, emptyContentId, canMerge, merge, mutate, result, generator);
            }

            return result;
        }

        private static void ResolveSegment(
            List<FieldCell> segment,
            int emptyContentId,
            Func<int, int, bool> canMerge,
            Func<int, int, int> merge,
            bool mutate,
            SlidingMergeResult result,
            FieldGenerator generator)
        {
            if (segment.Count == 0)
            {
                return;
            }

            List<ContentEntry> entries = new();
            foreach (FieldCell cell in segment)
            {
                if (cell.ContentId != emptyContentId)
                {
                    entries.Add(new ContentEntry(cell.Position, cell.ContentId));
                }
            }

            List<ResolvedEntry> resolved = ResolveEntries(entries, canMerge, merge, result);
            for (int i = 0; i < segment.Count; i++)
            {
                int value = i < resolved.Count ? resolved[i].Value : emptyContentId;
                FieldCell cell = segment[i];
                if (cell.ContentId != value)
                {
                    result.Changed = true;
                    if (mutate)
                    {
                        cell.ContentId = value;
                        generator.OnCellStateChanged.Invoke(cell);
                    }
                }

                if (i >= resolved.Count)
                {
                    continue;
                }

                ResolvedEntry entry = resolved[i];
                if (entry.From != cell.Position || entry.IsMerge)
                {
                    result.MoveCount++;
                    result.Steps.Add(new SlidingMergeStep
                    {
                        From = entry.From,
                        To = cell.Position,
                        Value = entry.SourceValue,
                        IsMerge = entry.IsMerge,
                        ResultValue = entry.Value
                    });
                }
            }
        }

        private static List<ResolvedEntry> ResolveEntries(
            List<ContentEntry> entries,
            Func<int, int, bool> canMerge,
            Func<int, int, int> merge,
            SlidingMergeResult result)
        {
            List<ResolvedEntry> resolved = new(entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                ContentEntry current = entries[i];
                if (i + 1 < entries.Count && canMerge(current.Value, entries[i + 1].Value))
                {
                    int mergedValue = merge(current.Value, entries[i + 1].Value);
                    result.Changed = true;
                    result.MergeCount++;
                    result.ScoreDelta += mergedValue;
                    resolved.Add(new ResolvedEntry(current.Position, current.Value, mergedValue, true));
                    i++;
                    continue;
                }

                resolved.Add(new ResolvedEntry(current.Position, current.Value, current.Value, false));
            }

            return resolved;
        }

        private static IEnumerable<List<FieldCell>> EnumerateLines(FieldGenerator generator, Vector3Int direction)
        {
            Vector3Int size = generator.Config.Size;
            int axis = direction.x != 0 ? 0 : direction.y != 0 ? 1 : 2;
            bool positive = GetAxisValue(direction, axis) > 0;

            int aSize = axis == 0 ? size.y : size.x;
            int bSize = axis == 2 ? size.y : size.z;
            for (int a = 0; a < aSize; a++)
            for (int b = 0; b < bSize; b++)
            {
                List<FieldCell> line = new();
                int length = GetAxisValue(size, axis);
                for (int i = 0; i < length; i++)
                {
                    int axisValue = positive ? length - 1 - i : i;
                    Vector3Int pos = CreatePosition(axis, axisValue, a, b);
                    FieldCell cell = generator.GetCell(pos);
                    if (cell != null)
                    {
                        line.Add(cell);
                    }
                }

                yield return line;
            }
        }

        private static Vector3Int CreatePosition(int axis, int axisValue, int a, int b)
        {
            switch (axis)
            {
                case 0:
                    return new Vector3Int(axisValue, a, b);
                case 1:
                    return new Vector3Int(a, axisValue, b);
                default:
                    return new Vector3Int(a, b, axisValue);
            }
        }

        private static int GetAxisValue(Vector3Int value, int axis)
        {
            switch (axis)
            {
                case 0:
                    return value.x;
                case 1:
                    return value.y;
                default:
                    return value.z;
            }
        }

        private static bool IsUsable(FieldCell cell, bool requireWalkable)
        {
            return cell != null && cell.IsEnabled && (!requireWalkable || cell.IsWalkable);
        }

        private static bool AreEqualNonEmpty(int a, int b)
        {
            return a == b;
        }

        private static int DoubleFirstValue(int a, int b)
        {
            return a + b;
        }

        private readonly struct ContentEntry
        {
            public readonly Vector3Int Position;
            public readonly int Value;

            public ContentEntry(Vector3Int position, int value)
            {
                Position = position;
                Value = value;
            }
        }

        private readonly struct ResolvedEntry
        {
            public readonly Vector3Int From;
            public readonly int SourceValue;
            public readonly int Value;
            public readonly bool IsMerge;

            public ResolvedEntry(Vector3Int from, int sourceValue, int value, bool isMerge)
            {
                From = from;
                SourceValue = sourceValue;
                Value = value;
                IsMerge = isMerge;
            }
        }
    }
}
