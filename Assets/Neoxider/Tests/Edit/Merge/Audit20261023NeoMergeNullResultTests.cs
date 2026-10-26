using System;
using System.Collections.Generic;
using Neo.Merge;
using NUnit.Framework;

namespace Neo.Editor.Tests.Merge
{
    public sealed class Audit20261023NeoMergeNullResultTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void Resolve_NullResultItem_PreservesSourceValues(bool mutate)
        {
            Dictionary<string, int> values = new Dictionary<string, int>
            {
                ["A"] = 1,
                ["B"] = 1,
                ["C"] = 1
            };
            Dictionary<string, int> originalValues = new Dictionary<string, int>(values);
            int writes = 0;
            MergeRequest<string, int> request = new MergeRequest<string, int>
            {
                Items = values.Keys,
                Seeds = new[] { "A" },
                GetValue = item => values[item],
                SetValue = (item, value) =>
                {
                    writes++;
                    values[item] = value;
                },
                GetNeighbors = item => values.Keys,
                IsEmptyValue = value => value == 0,
                SelectResultItem = (group, seed) => null,
                GetMergedValue = (value, count) => value + 1,
                EmptyValue = 0,
                Mutate = mutate
            };

            Assert.Throws<InvalidOperationException>(() => MergeResolver.Resolve(request));

            Assert.That(writes, Is.Zero);
            Assert.That(values, Is.EquivalentTo(originalValues));
        }

        [Test]
        public void Resolve_NullResultAfterEarlierSeed_DoesNotCommitAnyGroup()
        {
            Dictionary<string, int> values = new Dictionary<string, int>
            {
                ["A"] = 1,
                ["B"] = 1,
                ["C"] = 1,
                ["D"] = 2,
                ["E"] = 2,
                ["F"] = 2
            };
            Dictionary<string, int> originalValues = new Dictionary<string, int>(values);
            int writes = 0;
            MergeRequest<string, int> request = new MergeRequest<string, int>
            {
                Items = values.Keys,
                Seeds = new[] { "A", "D" },
                GetValue = item => values[item],
                SetValue = (item, value) =>
                {
                    writes++;
                    values[item] = value;
                },
                GetNeighbors = item => item == "A" || item == "B" || item == "C"
                    ? new[] { "A", "B", "C" }
                    : new[] { "D", "E", "F" },
                IsEmptyValue = value => value == 0,
                SelectResultItem = (group, seed) => seed == "A" ? seed : null,
                GetMergedValue = (value, count) => value + 1,
                EmptyValue = 0,
                Mutate = true
            };

            Assert.Throws<InvalidOperationException>(() => MergeResolver.Resolve(request));

            Assert.That(writes, Is.Zero);
            Assert.That(values, Is.EquivalentTo(originalValues));
        }

        [Test]
        public void Resolve_NullResultDuringCascade_DoesNotCommitEarlierIterations()
        {
            Dictionary<string, int> values = new Dictionary<string, int>
            {
                ["A"] = 1,
                ["B"] = 1,
                ["C"] = 1,
                ["D"] = 2,
                ["E"] = 2
            };
            Dictionary<string, int> originalValues = new Dictionary<string, int>(values);
            int writes = 0;
            int selections = 0;
            MergeRequest<string, int> request = new MergeRequest<string, int>
            {
                Items = values.Keys,
                Seeds = new[] { "A" },
                GetValue = item => values[item],
                SetValue = (item, value) =>
                {
                    writes++;
                    values[item] = value;
                },
                GetNeighbors = item => item == "A"
                    ? new[] { "B", "C", "D", "E" }
                    : new[] { "A" },
                IsEmptyValue = value => value == 0,
                SelectResultItem = (group, seed) => ++selections == 1 ? seed : null,
                GetMergedValue = (value, count) => value + 1,
                EmptyValue = 0,
                CascadeMode = MergeCascadeMode.FromResult,
                Mutate = true
            };

            Assert.Throws<InvalidOperationException>(() => MergeResolver.Resolve(request));

            Assert.That(selections, Is.EqualTo(2));
            Assert.That(writes, Is.Zero);
            Assert.That(values, Is.EquivalentTo(originalValues));
        }
    }
}
