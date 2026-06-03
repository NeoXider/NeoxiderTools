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
        public static GridMergeResult Resolve(FieldGenerator generator, GridMergeRequest request)
        {
            var result = new GridMergeResult();
            if (generator == null || generator.Cells == null)
            {
                return result;
            }

            request ??= new GridMergeRequest();
            List<FieldCell> seeds = ResolveSeedCells(generator, request);
            IEnumerable<Vector3Int> directions = request.Directions ??
                                                 generator.Config?.MovementRule?.Directions ??
                                                 MovementRule.FourDirections2D.Directions;

            var mergeRequest = new MergeRequest<FieldCell, int>
            {
                // The generic core only uses Items as a fallback seed source; since we always supply explicit seeds we
                // pass the same list instead of allocating a copy of the whole board on every resolve.
                Items = seeds,
                Seeds = seeds,
                GetValue = cell => cell.ContentId,
                SetValue = (cell, value) =>
                {
                    cell.ContentId = value;
                    if (request.NotifyOnContentChanged)
                    {
                        generator.OnCellStateChanged.Invoke(cell);
                    }
                },
                GetNeighbors = cell => GetNeighbors(generator, cell, directions),
                CanUseItem = cell => CanUseCell(cell, request),
                IsEmptyValue = value => request.IsEmptyContent != null
                    ? request.IsEmptyContent(value)
                    : value == request.EmptyContentId,
                AreValuesEqual = request.AreContentEqual ?? ((a, b) => a == b),
                SelectResultItem = request.SelectResultCell ?? ((group, seed) => seed),
                GetMergedValue = request.GetMergedContent ?? ((value, count) => value + 1),
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
                var gridGroup = new GridMergeGroupResult
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

            return result;
        }

        private static List<FieldCell> ResolveSeedCells(FieldGenerator generator, GridMergeRequest request)
        {
            var seeds = new List<FieldCell>();
            if (request.Seeds == null)
            {
                seeds.AddRange(generator.GetAllCells(false));
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

        private static IEnumerable<FieldCell> GetNeighbors(
            FieldGenerator generator,
            FieldCell cell,
            IEnumerable<Vector3Int> directions)
        {
            foreach (Vector3Int direction in directions)
            {
                FieldCell neighbor = generator.GetCell(cell.Position + direction);
                if (neighbor != null)
                {
                    yield return neighbor;
                }
            }
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
