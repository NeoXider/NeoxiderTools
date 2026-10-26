using System;
using System.Collections.Generic;

namespace Neo.Merge
{
    /// <summary>
    ///     Resolves connected merge groups for arbitrary item graphs.
    /// </summary>
    public static class MergeResolver
    {
        /// <summary>
        ///     Resolves merge groups and optionally applies their final values after all seeds have been evaluated.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///     The result selector returned a null item. No values are applied by the resolver in this case.
        /// </exception>
        public static MergeResult<TItem, TValue> Resolve<TItem, TValue>(MergeRequest<TItem, TValue> request)
        {
            ValidateRequest(request);

            MergeResult<TItem, TValue> result = new MergeResult<TItem, TValue>();
            Dictionary<TItem, TValue> valueOverrides = new Dictionary<TItem, TValue>();
            HashSet<TItem> changedSet = new HashSet<TItem>();
            IEnumerable<TItem> seeds = request.Seeds ?? request.Items;

            foreach (TItem seed in seeds)
            {
                ResolveSeed(request, seed, result, valueOverrides, changedSet);
            }

            if (request.Mutate)
            {
                foreach (KeyValuePair<TItem, TValue> pair in valueOverrides)
                {
                    request.SetValue(pair.Key, pair.Value);
                }
            }

            return result;
        }

        private static void ResolveSeed<TItem, TValue>(
            MergeRequest<TItem, TValue> request,
            TItem seed,
            MergeResult<TItem, TValue> result,
            Dictionary<TItem, TValue> valueOverrides,
            HashSet<TItem> changedSet)
        {
            TItem currentSeed = seed;
            int maxIterations = request.MaxCascadeIterations > 0 ? request.MaxCascadeIterations : 1;

            for (int guard = 0; guard < maxIterations; guard++)
            {
                List<TItem> group = FindGroup(request, currentSeed, valueOverrides);
                if (group.Count < request.MinGroupSize)
                {
                    return;
                }

                TValue sourceValue = GetCurrentValue(request, currentSeed, valueOverrides);
                TItem resultItem = request.SelectResultItem != null
                    ? request.SelectResultItem(group, currentSeed)
                    : currentSeed;
                if (resultItem is null)
                {
                    throw new InvalidOperationException("SelectResultItem must return a non-null item.");
                }

                TValue resultValue = request.GetMergedValue != null
                    ? request.GetMergedValue(sourceValue, group.Count)
                    : sourceValue;

                MergeGroupResult<TItem, TValue> groupResult = new MergeGroupResult<TItem, TValue>
                {
                    Items = new List<TItem>(group),
                    SeedItem = currentSeed,
                    ResultItem = resultItem,
                    SourceValue = sourceValue,
                    ResultValue = resultValue
                };

                bool resultInGroup = false;
                for (int i = 0; i < group.Count; i++)
                {
                    TItem item = group[i];
                    bool isResult = EqualityComparer<TItem>.Default.Equals(item, resultItem);
                    resultInGroup |= isResult;

                    valueOverrides[item] = isResult ? resultValue : request.EmptyValue;
                    AddUnique(result.ChangedItems, changedSet, item);

                    if (!isResult)
                    {
                        groupResult.ClearedItems.Add(item);
                    }
                }

                // WHY: SelectResultItem may target an item outside the group (e.g. merge into an adjacent empty
                // slot); the merged value must still land there instead of being silently dropped.
                if (!resultInGroup)
                {
                    valueOverrides[resultItem] = resultValue;
                    AddUnique(result.ChangedItems, changedSet, resultItem);
                }

                result.Groups.Add(groupResult);

                if (request.CascadeMode != MergeCascadeMode.FromResult)
                {
                    return;
                }

                currentSeed = resultItem;
            }

            // WHY: the guard limit was exhausted; flag only when yet another merge was actually possible so a
            // cascade that finishes exactly on the limit is not reported as a configuration bug.
            if (FindGroup(request, currentSeed, valueOverrides).Count >= request.MinGroupSize)
            {
                result.CascadeLimitReached = true;
            }
        }

        private static List<TItem> FindGroup<TItem, TValue>(
            MergeRequest<TItem, TValue> request,
            TItem seed,
            Dictionary<TItem, TValue> valueOverrides)
        {
            List<TItem> group = new List<TItem>();
            if (!CanUse(request, seed, valueOverrides))
            {
                return group;
            }

            TValue seedValue = GetCurrentValue(request, seed, valueOverrides);
            Queue<TItem> queue = new Queue<TItem>();
            HashSet<TItem> visited = new HashSet<TItem>();
            queue.Enqueue(seed);
            visited.Add(seed);

            while (queue.Count > 0)
            {
                TItem current = queue.Dequeue();
                group.Add(current);

                IEnumerable<TItem> neighbors = request.GetNeighbors(current);
                if (neighbors == null)
                {
                    continue;
                }

                foreach (TItem neighbor in neighbors)
                {
                    if (visited.Contains(neighbor) || !CanUse(request, neighbor, valueOverrides))
                    {
                        continue;
                    }

                    TValue neighborValue = GetCurrentValue(request, neighbor, valueOverrides);
                    if (!request.AreValuesEqual(seedValue, neighborValue))
                    {
                        continue;
                    }

                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }

            return group;
        }

        private static bool CanUse<TItem, TValue>(
            MergeRequest<TItem, TValue> request,
            TItem item,
            Dictionary<TItem, TValue> valueOverrides)
        {
            if (item is null)
            {
                return false;
            }

            if (request.CanUseItem != null && !request.CanUseItem(item))
            {
                return false;
            }

            TValue value = GetCurrentValue(request, item, valueOverrides);
            return request.IsEmptyValue == null || !request.IsEmptyValue(value);
        }

        private static TValue GetCurrentValue<TItem, TValue>(
            MergeRequest<TItem, TValue> request,
            TItem item,
            Dictionary<TItem, TValue> valueOverrides)
        {
            return valueOverrides.TryGetValue(item, out TValue value)
                ? value
                : request.GetValue(item);
        }

        private static void ValidateRequest<TItem, TValue>(MergeRequest<TItem, TValue> request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.Items == null)
            {
                throw new ArgumentException("Merge request requires Items.", nameof(request));
            }

            if (request.GetValue == null)
            {
                throw new ArgumentException("Merge request requires GetValue.", nameof(request));
            }

            if (request.GetNeighbors == null)
            {
                throw new ArgumentException("Merge request requires GetNeighbors.", nameof(request));
            }

            request.AreValuesEqual ??= EqualityComparer<TValue>.Default.Equals;
            request.MinGroupSize = Math.Max(1, request.MinGroupSize);

            if (request.Mutate && request.SetValue == null)
            {
                throw new ArgumentException("Mutating merge request requires SetValue.", nameof(request));
            }
        }

        private static void AddUnique<T>(List<T> list, HashSet<T> set, T item)
        {
            if (set.Add(item))
            {
                list.Add(item);
            }
        }
    }
}
