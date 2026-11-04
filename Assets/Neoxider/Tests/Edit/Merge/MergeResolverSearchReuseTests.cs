using System.Collections.Generic;
using Neo.Merge;
using Neo.Tests.Performance;
using NUnit.Framework;

namespace Neo.Editor.Tests.Merge
{
    /// <summary>
    ///     Guards the per-resolve search reuse of <see cref="MergeResolver" />: list and lazy neighbor sources give the
    ///     same result, consecutive searches never inherit visited state, and callbacks receive stable group lists.
    /// </summary>
    public sealed class MergeResolverSearchReuseTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void Resolve_LineGraph_SameResultForListAndLazyNeighbors(bool lazyNeighbors)
        {
            int[] values = { 1, 1, 1, 2, 1, 1, 1, 1, 3, 3, 3, 1 };

            MergeResult<int, int> result = MergeResolver.Resolve(CreateLineRequest(values, lazyNeighbors));

            Assert.That(result.Groups.Count, Is.EqualTo(3));
            Assert.That(result.Groups[0].Items, Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(result.Groups[1].Items, Is.EqualTo(new[] { 4, 5, 6, 7 }));
            Assert.That(result.Groups[2].Items, Is.EqualTo(new[] { 8, 9, 10 }));
            Assert.That(values, Is.EqualTo(new[] { 2, 0, 0, 2, 2, 0, 0, 0, 4, 0, 0, 1 }));
        }

        [Test]
        public void Resolve_LargeGroupFollowedByManySmallSearches_FindsEveryLaterGroup()
        {
            int length = 400;
            int[] values = new int[length];
            for (int i = 0; i < 200; i++)
            {
                values[i] = 5;
            }

            for (int i = 200; i < length; i++)
            {
                values[i] = i % 2 == 0 ? 7 : 8;
            }

            values[396] = 9;
            values[397] = 9;
            values[398] = 9;

            MergeResult<int, int> result = MergeResolver.Resolve(CreateLineRequest(values, false));

            Assert.That(result.Groups.Count, Is.EqualTo(2));
            Assert.That(result.Groups[0].Items.Count, Is.EqualTo(200));
            Assert.That(result.Groups[1].Items, Is.EqualTo(new[] { 396, 397, 398 }));
            Assert.That(values[0], Is.EqualTo(6));
            Assert.That(values[396], Is.EqualTo(10));
        }

        [Test]
        public void Resolve_SelectorReceivesTheSameStableGroupThatIsReported()
        {
            int[] values = { 4, 4, 4, 0, 6, 6, 6 };
            List<IReadOnlyList<int>> seenGroups = new List<IReadOnlyList<int>>();
            MergeRequest<int, int> request = CreateLineRequest(values, false);
            request.SelectResultItem = (group, seed) =>
            {
                seenGroups.Add(group);
                return group[group.Count - 1];
            };

            MergeResult<int, int> result = MergeResolver.Resolve(request);

            Assert.That(result.Groups.Count, Is.EqualTo(2));
            Assert.That(seenGroups.Count, Is.EqualTo(2));
            Assert.That(seenGroups[0], Is.SameAs(result.Groups[0].Items));
            Assert.That(seenGroups[0], Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(seenGroups[1], Is.EqualTo(new[] { 4, 5, 6 }));
            Assert.That(values, Is.EqualTo(new[] { 0, 0, 5, 0, 0, 0, 7 }));
        }

        [Test]
        public void Resolve_ScanWithoutGroups_AllocatesTheSameForAnyNumberOfSeeds()
        {
            MergeRequest<int, int> small = CreateDistinctRingRequest(32);
            MergeRequest<int, int> large = CreateDistinctRingRequest(1024);
            MergeResolver.Resolve(small);
            MergeResolver.Resolve(large);
            int groups = 0;

            long smallCalls = GcAlloc.TryCount(() => groups += MergeResolver.Resolve(small).Groups.Count,
                out string notAdmitted);
            long largeCalls = GcAlloc.TryCount(() => groups += MergeResolver.Resolve(large).Groups.Count,
                out notAdmitted);

            Assert.That(groups, Is.Zero, "Distinct neighbors never form a group.");
            GcAlloc.IgnoreIfNotAdmitted(notAdmitted);
            Assert.That(largeCalls, Is.EqualTo(smallCalls),
                "Search collections are created once per resolve and reused by every seed, so 32x more seeds cost " +
                "no extra allocation.");
            Assert.That(smallCalls, Is.LessThanOrEqualTo(24), "Only the per-resolve result and collections allocate.");
        }

        /// <summary>
        ///     Items 0..count-1 on a ring with distinct values and neighbor lists built once, so a resolve reads only
        ///     cached data and every search ends after its seed.
        /// </summary>
        private static MergeRequest<int, int> CreateDistinctRingRequest(int count)
        {
            List<int> items = new List<int>(count);
            List<int>[] neighbors = new List<int>[count];
            for (int i = 0; i < count; i++)
            {
                items.Add(i);
                neighbors[i] = new List<int> { (i + count - 1) % count, (i + 1) % count };
            }

            return new MergeRequest<int, int>
            {
                Items = items,
                GetValue = item => item + 1,
                GetNeighbors = item => neighbors[item],
                IsEmptyValue = value => value == 0,
                MinGroupSize = 2,
                Mutate = false
            };
        }

        private static MergeRequest<int, int> CreateLineRequest(int[] values, bool lazyNeighbors)
        {
            List<int> items = new List<int>();
            for (int i = 0; i < values.Length; i++)
            {
                items.Add(i);
            }

            return new MergeRequest<int, int>
            {
                Items = items,
                GetValue = item => values[item],
                SetValue = (item, value) => values[item] = value,
                GetNeighbors = item => lazyNeighbors
                    ? LazyNeighbors(item, values.Length)
                    : ListNeighbors(item, values.Length),
                IsEmptyValue = value => value == 0,
                GetMergedValue = (value, count) => value + 1,
                EmptyValue = 0,
                MinGroupSize = 3,
                Mutate = true
            };
        }

        private static List<int> ListNeighbors(int item, int length)
        {
            List<int> neighbors = new List<int>(2);
            if (item > 0)
            {
                neighbors.Add(item - 1);
            }

            if (item + 1 < length)
            {
                neighbors.Add(item + 1);
            }

            return neighbors;
        }

        private static IEnumerable<int> LazyNeighbors(int item, int length)
        {
            if (item > 0)
            {
                yield return item - 1;
            }

            if (item + 1 < length)
            {
                yield return item + 1;
            }
        }
    }
}
