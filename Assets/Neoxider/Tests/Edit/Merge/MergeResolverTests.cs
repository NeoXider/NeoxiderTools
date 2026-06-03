using System.Collections.Generic;
using Neo.Merge;
using NUnit.Framework;

namespace Neo.Editor.Tests.Merge
{
    public sealed class MergeResolverTests
    {
        [Test]
        public void Resolve_FindsConnectedGraphGroup_AndSelectsAnchorResult()
        {
            var values = new Dictionary<string, int>
            {
                ["A"] = 1,
                ["B"] = 1,
                ["C"] = 1,
                ["D"] = 2
            };

            MergeResult<string, int> result = MergeResolver.Resolve(new MergeRequest<string, int>
            {
                Items = values.Keys,
                Seeds = new[] { "A" },
                GetValue = item => values[item],
                SetValue = (item, value) => values[item] = value,
                GetNeighbors = item => item switch
                {
                    "A" => new[] { "B" },
                    "B" => new[] { "A", "C" },
                    "C" => new[] { "B", "D" },
                    _ => new string[0]
                },
                SelectResultItem = (group, seed) => seed,
                GetMergedValue = (value, count) => value + 1,
                EmptyValue = 0,
                MinGroupSize = 3,
                Mutate = true
            });

            Assert.That(result.Groups.Count, Is.EqualTo(1));
            Assert.That(result.Groups[0].ResultItem, Is.EqualTo("A"));
            Assert.That(values["A"], Is.EqualTo(2));
            Assert.That(values["B"], Is.EqualTo(0));
            Assert.That(values["C"], Is.EqualTo(0));
            Assert.That(values["D"], Is.EqualTo(2));
        }

        [Test]
        public void Resolve_SupportsCascadeFromResult()
        {
            var values = new Dictionary<string, int>
            {
                ["A"] = 1,
                ["B"] = 1,
                ["C"] = 1,
                ["D"] = 2,
                ["E"] = 2
            };

            MergeResult<string, int> result = MergeResolver.Resolve(new MergeRequest<string, int>
            {
                Items = values.Keys,
                Seeds = new[] { "A" },
                GetValue = item => values[item],
                SetValue = (item, value) => values[item] = value,
                GetNeighbors = item => item switch
                {
                    "A" => new[] { "B", "D", "E" },
                    "B" => new[] { "A", "C" },
                    "C" => new[] { "B" },
                    "D" => new[] { "A", "E" },
                    "E" => new[] { "A", "D" },
                    _ => new string[0]
                },
                GetMergedValue = (value, count) => value + 1,
                EmptyValue = 0,
                MinGroupSize = 3,
                CascadeMode = MergeCascadeMode.FromResult,
                Mutate = true
            });

            Assert.That(result.Groups.Count, Is.EqualTo(2));
            Assert.That(values["A"], Is.EqualTo(3));
            Assert.That(values["D"], Is.EqualTo(0));
            Assert.That(values["E"], Is.EqualTo(0));
        }

        [Test]
        public void Resolve_DryRunDoesNotMutateSourceData()
        {
            var values = new Dictionary<int, int>
            {
                [0] = 5,
                [1] = 5,
                [2] = 5
            };

            MergeResult<int, int> result = MergeResolver.Resolve(new MergeRequest<int, int>
            {
                Items = values.Keys,
                Seeds = new[] { 0 },
                GetValue = item => values[item],
                SetValue = (item, value) => values[item] = value,
                GetNeighbors = item => item switch
                {
                    0 => new[] { 1 },
                    1 => new[] { 0, 2 },
                    2 => new[] { 1 },
                    _ => new int[0]
                },
                GetMergedValue = (value, count) => value + count,
                EmptyValue = 0,
                MinGroupSize = 3,
                Mutate = false
            });

            Assert.That(result.HasChanges, Is.True);
            Assert.That(values[0], Is.EqualTo(5));
            Assert.That(values[1], Is.EqualTo(5));
            Assert.That(values[2], Is.EqualTo(5));
            Assert.That(result.Groups[0].ResultValue, Is.EqualTo(8));
        }

        [Test]
        public void Resolve_UsesCustomNeighborPredicateTargetAndResultValue()
        {
            var values = new Dictionary<int, int>
            {
                [0] = 2,
                [1] = 4,
                [2] = 6,
                [3] = 8,
                [4] = 3
            };

            MergeResult<int, int> result = MergeResolver.Resolve(new MergeRequest<int, int>
            {
                Items = values.Keys,
                Seeds = new[] { 0 },
                GetValue = item => values[item],
                SetValue = (item, value) => values[item] = value,
                GetNeighbors = item => item switch
                {
                    0 => new[] { 1 },
                    1 => new[] { 0, 2 },
                    2 => new[] { 1, 3, 4 },
                    3 => new[] { 2 },
                    4 => new[] { 2 },
                    _ => new int[0]
                },
                CanUseItem = item => item != 3,
                AreValuesEqual = (a, b) => a % 2 == b % 2,
                SelectResultItem = (group, seed) => group[group.Count - 1],
                GetMergedValue = (value, count) => value + count * 10,
                EmptyValue = 0,
                MinGroupSize = 3,
                Mutate = true
            });

            Assert.That(result.Groups.Count, Is.EqualTo(1));
            Assert.That(result.Groups[0].Items, Is.EquivalentTo(new[] { 0, 1, 2 }));
            Assert.That(result.Groups[0].ResultItem, Is.EqualTo(2));
            Assert.That(result.Groups[0].ResultValue, Is.EqualTo(32));
            Assert.That(values[0], Is.EqualTo(0));
            Assert.That(values[1], Is.EqualTo(0));
            Assert.That(values[2], Is.EqualTo(32));
            Assert.That(values[3], Is.EqualTo(8));
            Assert.That(values[4], Is.EqualTo(3));
        }
    }
}
