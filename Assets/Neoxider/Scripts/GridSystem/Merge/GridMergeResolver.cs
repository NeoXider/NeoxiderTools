using System.Collections.Generic;
using Neo.Merge;
using UnityEngine;

namespace Neo.GridSystem.Merge
{
    /// <summary>
    ///     Adapter that applies the generic merge resolver to FieldGenerator cells.
    /// </summary>
    public static class GridMergeResolver
    {
        /// <summary>
        ///     Resolves connected equal-content groups on the field and, when <see cref="GridMergeRequest.Mutate" /> is
        ///     set, writes the merged content and raises cell notifications after every write has been applied.
        /// </summary>
        /// <param name="generator">Field whose <see cref="FieldCell.ContentId" /> values are merged.</param>
        /// <param name="request">Merge rules; a default request is used when null.</param>
        /// <returns>Resolved groups and changed cells; empty when the field has no cells.</returns>
        public static GridMergeResult Resolve(FieldGenerator generator, GridMergeRequest request)
        {
            GridMergeResult result = new GridMergeResult();
            if (generator == null || generator.Cells == null)
            {
                return result;
            }

            request ??= new GridMergeRequest();
            List<FieldCell> seeds = ResolveSeedCells(generator, request);
            IEnumerable<Vector3Int> directionSource = request.Directions ??
                                                      generator.Config?.MovementRule?.Directions ??
                                                      MovementRule.FourDirections2D.Directions;
            Vector3Int[] directions = CopyDirections(directionSource);

            // WHY: the generic core consumes each neighbor list before it asks for the next one, so one buffer per
            // resolve replaces an iterator allocation per visited cell.
            List<FieldCell> neighborBuffer = new List<FieldCell>(directions.Length);

            MergeRequest<FieldCell, int> mergeRequest = new MergeRequest<FieldCell, int>
            {
                // WHY: the generic core only uses Items as a fallback seed source; since we always supply explicit
                // seeds we pass the same list instead of allocating a copy of the whole board on every resolve.
                Items = seeds,
                Seeds = seeds,
                GetValue = cell => cell.ContentId,
                SetValue = (cell, value) => cell.ContentId = value,
                GetNeighbors = cell => FillNeighbors(generator, cell, directions, neighborBuffer),
                CanUseItem = cell => CanUseCell(cell, request),
                IsEmptyValue = value => request.IsEmptyContent != null
                    ? request.IsEmptyContent(value)
                    : value == request.EmptyContentId,
                AreValuesEqual = request.AreContentEqual ?? ((a, b) => a == b),
                SelectResultItem = request.SelectResultCell ?? ((group, seed) => seed),
                GetMergedValue = request.GetMergedContent ?? ((value, count) => GridMergeRequest.SaturatingAdd(value, 1)),
                EmptyValue = request.EmptyContentId,
                MinGroupSize = request.MinGroupSize,
                CascadeMode = request.CascadeMode,
                Mutate = request.Mutate,
                MaxCascadeIterations = request.MaxCascadeIterations
            };

            MergeResult<FieldCell, int> mergeResult = MergeResolver.Resolve(mergeRequest);
            result.CascadeLimitReached = mergeResult.CascadeLimitReached;
            foreach (MergeGroupResult<FieldCell, int> group in mergeResult.Groups)
            {
                GridMergeGroupResult gridGroup = new GridMergeGroupResult
                {
                    SeedCell = group.SeedItem,
                    ResultCell = group.ResultItem,
                    SourceContentId = group.SourceValue,
                    ResultContentId = group.ResultValue
                };

                gridGroup.Cells.AddRange(group.Items);
                gridGroup.ClearedCells.AddRange(group.ClearedItems);
                foreach (FieldCell cell in gridGroup.Cells)
                {
                    gridGroup.Positions.Add(cell.Position);
                }

                result.Groups.Add(gridGroup);
            }

            foreach (FieldCell cell in mergeResult.ChangedItems)
            {
                result.ChangedCells.Add(cell);
                result.ChangedPositions.Add(cell.Position);
            }

            if (request.Mutate && request.NotifyOnContentChanged)
            {
                foreach (FieldCell cell in result.ChangedCells)
                {
                    generator.OnCellStateChanged?.Invoke(cell);
                }
            }

            return result;
        }

        private static List<FieldCell> ResolveSeedCells(FieldGenerator generator, GridMergeRequest request)
        {
            List<FieldCell> seeds = new List<FieldCell>();
            if (request.Seeds == null)
            {
                seeds.AddRange(generator.GetAllCells(!request.RequireEnabled));
                return seeds;
            }

            foreach (Vector3Int position in request.Seeds)
            {
                FieldCell cell = generator.GetCell(position);
                if (cell != null)
                {
                    seeds.Add(cell);
                }
            }

            return seeds;
        }

        private static Vector3Int[] CopyDirections(IEnumerable<Vector3Int> source)
        {
            if (source is IReadOnlyList<Vector3Int> list)
            {
                Vector3Int[] copy = new Vector3Int[list.Count];
                for (int i = 0; i < copy.Length; i++)
                {
                    copy[i] = list[i];
                }

                return copy;
            }

            return new List<Vector3Int>(source).ToArray();
        }

        private static List<FieldCell> FillNeighbors(
            FieldGenerator generator,
            FieldCell cell,
            Vector3Int[] directions,
            List<FieldCell> buffer)
        {
            buffer.Clear();
            for (int i = 0; i < directions.Length; i++)
            {
                FieldCell neighbor = generator.GetCell(cell.Position + directions[i]);
                if (neighbor != null)
                {
                    buffer.Add(neighbor);
                }
            }

            return buffer;
        }

        private static bool CanUseCell(FieldCell cell, GridMergeRequest request)
        {
            if (cell == null)
            {
                return false;
            }

            if (request.RequireEnabled && !cell.IsEnabled)
            {
                return false;
            }

            if (request.RequireWalkable && !cell.IsWalkable)
            {
                return false;
            }

            if (!request.IgnoreOccupied && cell.IsOccupied)
            {
                return false;
            }

            return request.CustomCellFilter == null || request.CustomCellFilter(cell);
        }
    }
}
