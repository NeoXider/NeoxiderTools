using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.GridSystem.SlidingMerge
{
    /// <summary>
    ///     Pure grid resolver for 2048-like slide and merge rules over <see cref="FieldCell.ContentId" />.
    /// </summary>
    /// <remarks>
    ///     Only enabled, walkable cells take part; other cells split a line into independent segments. Each tile merges
    ///     at most once per slide. Cell notifications are raised after every line has been written, so listeners
    ///     always observe the final board. The resolver reuses internal buffers; re-entrant calls from merge
    ///     delegates or listeners get their own. Warm <see cref="CanSlide" /> and <see cref="HasAnyMove" /> calls do
    ///     not allocate; <see cref="Slide" /> allocates its returned result and one step per moved tile.
    /// </remarks>
    public static class SlidingMergeResolver
    {
        // WHY: method-group conversions are not cached by the C# 9 compiler; reusing these keeps the default rules
        // from allocating two delegates per slide or preview.
        private static readonly Func<int, int, bool> s_defaultCanMerge = AreEqualNonEmpty;
        private static readonly Func<int, int, int> s_defaultMerge = DoubleFirstValue;

        [ThreadStatic] private static Scratch s_scratch;

        // WHY: with domain reload disabled the previous session's buffers would otherwise stay referenced forever.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            s_scratch = null;
        }

        /// <summary>
        ///     Returns true when sliding in <paramref name="direction" /> would change the board. Does not write.
        /// </summary>
        /// <param name="generator">Board to inspect.</param>
        /// <param name="direction">Slide direction.</param>
        /// <param name="emptyContentId">Content id of an empty cell.</param>
        /// <param name="canMerge">Optional merge predicate; equal values merge when null.</param>
        /// <param name="merge">Optional merged value calculator; <c>a + b</c> when null.</param>
        public static bool CanSlide(
            FieldGenerator generator,
            SlidingMergeDirection direction,
            int emptyContentId = 0,
            Func<int, int, bool> canMerge = null,
            Func<int, int, int> merge = null)
        {
            Scratch scratch = AcquireScratch();
            try
            {
                // WHY: a preview only needs the change flag; the scratch-owned result keeps CanSlide and the
                // HasAnyMove game-over check that runs after every move from allocating a result per direction.
                SlidingMergeResult preview = scratch.Preview;
                ResetResult(preview);
                Evaluate(generator, direction.ToVector(), emptyContentId, canMerge, merge, false, true, preview,
                    scratch);
                return preview.Changed;
            }
            finally
            {
                ReleaseScratch(scratch);
            }
        }

        /// <summary>
        ///     Returns true when at least one slide direction would change the board (the 2048 "game over" test).
        ///     Depth directions are checked only on boards with more than one layer.
        /// </summary>
        /// <param name="generator">Board to inspect.</param>
        /// <param name="emptyContentId">Content id of an empty cell.</param>
        /// <param name="canMerge">Optional merge predicate; equal values merge when null.</param>
        /// <param name="merge">Optional merged value calculator; <c>a + b</c> when null.</param>
        public static bool HasAnyMove(
            FieldGenerator generator,
            int emptyContentId = 0,
            Func<int, int, bool> canMerge = null,
            Func<int, int, int> merge = null)
        {
            if (generator == null || generator.Cells == null)
            {
                return false;
            }

            if (CanSlide(generator, SlidingMergeDirection.Left, emptyContentId, canMerge, merge) ||
                CanSlide(generator, SlidingMergeDirection.Right, emptyContentId, canMerge, merge) ||
                CanSlide(generator, SlidingMergeDirection.Down, emptyContentId, canMerge, merge) ||
                CanSlide(generator, SlidingMergeDirection.Up, emptyContentId, canMerge, merge))
            {
                return true;
            }

            return generator.GeneratedSize.z > 1 &&
                   (CanSlide(generator, SlidingMergeDirection.Backward, emptyContentId, canMerge, merge) ||
                    CanSlide(generator, SlidingMergeDirection.Forward, emptyContentId, canMerge, merge));
        }

        /// <summary>
        ///     Slides every line toward <paramref name="direction" />, merges equal neighbors once and writes the result.
        /// </summary>
        /// <param name="generator">Board to change.</param>
        /// <param name="direction">Slide direction.</param>
        /// <param name="emptyContentId">Content id of an empty cell.</param>
        /// <param name="canMerge">Optional merge predicate; equal values merge when null.</param>
        /// <param name="merge">Optional merged value calculator; <c>a + b</c> when null.</param>
        /// <returns>Move/merge counts, score delta and per-tile steps for views.</returns>
        public static SlidingMergeResult Slide(
            FieldGenerator generator,
            SlidingMergeDirection direction,
            int emptyContentId = 0,
            Func<int, int, bool> canMerge = null,
            Func<int, int, int> merge = null)
        {
            SlidingMergeResult result = new();
            Scratch scratch = AcquireScratch();
            try
            {
                Evaluate(generator, direction.ToVector(), emptyContentId, canMerge, merge, true, false, result,
                    scratch);
                return result;
            }
            finally
            {
                ReleaseScratch(scratch);
            }
        }

        /// <summary>
        ///     Writes <paramref name="contentId" /> into a random empty, enabled cell and raises
        ///     <see cref="FieldGenerator.OnCellStateChanged" /> for it.
        /// </summary>
        /// <returns>False when no empty cell exists.</returns>
        public static bool TrySpawnRandomContent(
            FieldGenerator generator,
            int contentId,
            int emptyContentId = 0,
            bool requireWalkable = true)
        {
            return TrySpawnRandomContent(generator, contentId, out _, emptyContentId, requireWalkable);
        }

        /// <summary>
        ///     Writes <paramref name="contentId" /> into a random empty, enabled cell, raises
        ///     <see cref="FieldGenerator.OnCellStateChanged" /> for it and reports which cell was used.
        /// </summary>
        /// <param name="generator">Board to change.</param>
        /// <param name="contentId">Value to spawn.</param>
        /// <param name="spawnedCell">The written cell, or null when nothing was spawned.</param>
        /// <param name="emptyContentId">Content id of an empty cell.</param>
        /// <param name="requireWalkable">Only consider walkable cells.</param>
        /// <param name="randomIndex">
        ///     Optional index source: receives the number of candidates and returns an index in <c>[0, count)</c>
        ///     (clamped). Uses <see cref="UnityEngine.Random" /> when null. Inject it for deterministic tests and replays.
        /// </param>
        /// <returns>False when no empty cell exists.</returns>
        public static bool TrySpawnRandomContent(
            FieldGenerator generator,
            int contentId,
            out FieldCell spawnedCell,
            int emptyContentId = 0,
            bool requireWalkable = true,
            Func<int, int> randomIndex = null)
        {
            spawnedCell = null;
            FieldCell[,,] cells = generator != null ? generator.Cells : null;
            if (cells == null)
            {
                return false;
            }

            int candidateCount = 0;
            foreach (FieldCell cell in cells)
            {
                if (IsSpawnCandidate(cell, emptyContentId, requireWalkable))
                {
                    candidateCount++;
                }
            }

            if (candidateCount == 0)
            {
                return false;
            }

            int pick = randomIndex != null
                ? Mathf.Clamp(randomIndex(candidateCount), 0, candidateCount - 1)
                : UnityEngine.Random.Range(0, candidateCount);
            foreach (FieldCell cell in cells)
            {
                if (!IsSpawnCandidate(cell, emptyContentId, requireWalkable) || pick-- > 0)
                {
                    continue;
                }

                cell.ContentId = contentId;
                spawnedCell = cell;
                generator.OnCellStateChanged?.Invoke(cell);
                return true;
            }

            return false;
        }

        private static bool IsSpawnCandidate(FieldCell cell, int emptyContentId, bool requireWalkable)
        {
            return IsUsable(cell, requireWalkable) && cell.ContentId == emptyContentId;
        }

        /// <summary>
        ///     Resolves every line into <paramref name="result" /> using <paramref name="scratch" />, which the caller
        ///     acquired and releases.
        /// </summary>
        private static void Evaluate(
            FieldGenerator generator,
            Vector3Int direction,
            int emptyContentId,
            Func<int, int, bool> canMerge,
            Func<int, int, int> merge,
            bool mutate,
            bool stopOnFirstChange,
            SlidingMergeResult result,
            Scratch scratch)
        {
            FieldCell[,,] cells = generator != null ? generator.Cells : null;
            if (cells == null || generator.Config == null || direction == Vector3Int.zero)
            {
                return;
            }

            canMerge ??= s_defaultCanMerge;
            merge ??= s_defaultMerge;

            Vector3Int size = generator.GeneratedSize;
            int axis = direction.x != 0 ? 0 : direction.y != 0 ? 1 : 2;
            bool positive = GetAxisValue(direction, axis) > 0;
            int length = GetAxisValue(size, axis);
            int aSize = axis == 0 ? size.y : size.x;
            int bSize = axis == 2 ? size.y : size.z;
            List<FieldCell> segment = scratch.Segment;

            for (int a = 0; a < aSize; a++)
            for (int b = 0; b < bSize; b++)
            {
                segment.Clear();
                for (int i = 0; i < length; i++)
                {
                    int axisValue = positive ? length - 1 - i : i;
                    Vector3Int pos = CreatePosition(axis, axisValue, a, b);
                    FieldCell cell = cells[pos.x, pos.y, pos.z];
                    if (!IsUsable(cell, true))
                    {
                        ResolveSegment(scratch, emptyContentId, canMerge, merge, mutate, stopOnFirstChange, result);
                        segment.Clear();
                        continue;
                    }

                    segment.Add(cell);
                }

                ResolveSegment(scratch, emptyContentId, canMerge, merge, mutate, stopOnFirstChange, result);
                if (stopOnFirstChange && result.Changed)
                {
                    return;
                }
            }

            if (mutate)
            {
                List<FieldCell> changed = scratch.Changed;
                for (int i = 0; i < changed.Count; i++)
                {
                    generator.OnCellStateChanged?.Invoke(changed[i]);
                }
            }
        }

        private static void ResolveSegment(
            Scratch scratch,
            int emptyContentId,
            Func<int, int, bool> canMerge,
            Func<int, int, int> merge,
            bool mutate,
            bool stopOnFirstChange,
            SlidingMergeResult result)
        {
            List<FieldCell> segment = scratch.Segment;
            if (segment.Count == 0 || (stopOnFirstChange && result.Changed))
            {
                return;
            }

            List<ContentEntry> entries = scratch.Entries;
            entries.Clear();
            for (int i = 0; i < segment.Count; i++)
            {
                FieldCell cell = segment[i];
                if (cell.ContentId != emptyContentId)
                {
                    entries.Add(new ContentEntry(cell.Position, cell.ContentId));
                }
            }

            List<ResolvedEntry> resolved = scratch.Resolved;
            ResolveEntries(entries, resolved, canMerge, merge, result);
            for (int i = 0; i < segment.Count; i++)
            {
                int value = i < resolved.Count ? resolved[i].Value : emptyContentId;
                FieldCell cell = segment[i];
                if (cell.ContentId != value)
                {
                    result.Changed = true;
                    if (stopOnFirstChange)
                    {
                        return;
                    }

                    if (mutate)
                    {
                        cell.ContentId = value;
                        scratch.Changed.Add(cell);
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
                    if (stopOnFirstChange)
                    {
                        // WHY: a preview never exposes its steps, so it does not create them.
                        continue;
                    }

                    result.Steps.Add(new SlidingMergeStep
                    {
                        From = entry.From,
                        To = cell.Position,
                        Value = entry.SourceValue,
                        IsMerge = entry.IsMerge,
                        ResultValue = entry.Value,
                        MergedFrom = entry.MergedFrom,
                        MergedValue = entry.MergedValue
                    });
                }
            }
        }

        private static void ResolveEntries(
            List<ContentEntry> entries,
            List<ResolvedEntry> resolved,
            Func<int, int, bool> canMerge,
            Func<int, int, int> merge,
            SlidingMergeResult result)
        {
            resolved.Clear();
            for (int i = 0; i < entries.Count; i++)
            {
                ContentEntry current = entries[i];
                if (i + 1 < entries.Count && canMerge(current.Value, entries[i + 1].Value))
                {
                    ContentEntry consumed = entries[i + 1];
                    int mergedValue = merge(current.Value, consumed.Value);
                    result.Changed = true;
                    result.MergeCount++;
                    result.ScoreDelta += mergedValue;
                    resolved.Add(new ResolvedEntry(current.Position, current.Value, mergedValue, true,
                        consumed.Position, consumed.Value));
                    i++;
                    continue;
                }

                resolved.Add(new ResolvedEntry(current.Position, current.Value, current.Value, false,
                    current.Position, current.Value));
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

        private static void ResetResult(SlidingMergeResult result)
        {
            result.Changed = false;
            result.MoveCount = 0;
            result.MergeCount = 0;
            result.ScoreDelta = 0;
            result.Steps.Clear();
        }

        private static Scratch AcquireScratch()
        {
            Scratch scratch = s_scratch;
            if (scratch == null)
            {
                scratch = new Scratch();
                s_scratch = scratch;
            }
            else if (scratch.InUse)
            {
                scratch = new Scratch();
            }

            scratch.InUse = true;
            return scratch;
        }

        private static void ReleaseScratch(Scratch scratch)
        {
            scratch.Segment.Clear();
            scratch.Entries.Clear();
            scratch.Resolved.Clear();
            scratch.Changed.Clear();
            scratch.InUse = false;
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
            public readonly Vector3Int MergedFrom;
            public readonly int MergedValue;

            public ResolvedEntry(
                Vector3Int from,
                int sourceValue,
                int value,
                bool isMerge,
                Vector3Int mergedFrom,
                int mergedValue)
            {
                From = from;
                SourceValue = sourceValue;
                Value = value;
                IsMerge = isMerge;
                MergedFrom = mergedFrom;
                MergedValue = mergedValue;
            }
        }

        private sealed class Scratch
        {
            public readonly List<FieldCell> Changed = new List<FieldCell>();
            public readonly List<ContentEntry> Entries = new List<ContentEntry>();
            public readonly List<ResolvedEntry> Resolved = new List<ResolvedEntry>();
            public readonly List<FieldCell> Segment = new List<FieldCell>();
            public readonly SlidingMergeResult Preview = new SlidingMergeResult();
            public bool InUse;
        }
    }
}
