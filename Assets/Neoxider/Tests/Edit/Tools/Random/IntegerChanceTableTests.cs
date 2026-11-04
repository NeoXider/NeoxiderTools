using System;
using System.Reflection;
using Neo.Tests.Performance;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Neo.Editor.Tests
{
    public sealed class IntegerChanceTableTests
    {
        private Random.State _randomState;

        [SetUp]
        public void SetUp()
        {
            _randomState = Random.state;
        }

        [TearDown]
        public void TearDown()
        {
            Random.state = _randomState;
        }

        [Test]
        public void PerMillionOdds_MapTicketsExactly()
        {
            IntegerChanceTable table = new();
            table.Add(1, "Jackpot");
            table.Add(999_999, "Nothing");

            Assert.That(table.TotalWeight, Is.EqualTo(1_000_000L));
            Assert.That(table.GetIndex(0), Is.EqualTo(0));
            Assert.That(table.GetIndex(1), Is.EqualTo(1));
            Assert.That(table.GetIndex(999_999), Is.EqualTo(1));
            Assert.That(table.GetIndex(1_000_000), Is.EqualTo(-1));
            Assert.That(table.GetIndex(-1), Is.EqualTo(-1));
            Assert.That(table.TryGetOdds(0, out long weight, out long total), Is.True);
            Assert.That(weight, Is.EqualTo(1L));
            Assert.That(total, Is.EqualTo(1_000_000L));
        }

        [Test]
        public void ZeroWeightEntries_AreNeverSelected()
        {
            IntegerChanceTable table = new(0, 5, 0, 5);

            for (long ticket = 0; ticket < 5; ticket++)
            {
                Assert.That(table.GetIndex(ticket), Is.EqualTo(1));
            }

            for (long ticket = 5; ticket < 10; ticket++)
            {
                Assert.That(table.GetIndex(ticket), Is.EqualTo(3));
            }
        }

        [Test]
        public void TotalsAboveIntRange_StayExact()
        {
            IntegerChanceTable table = new(int.MaxValue, int.MaxValue, 1);

            Assert.That(table.TotalWeight, Is.EqualTo(2L * int.MaxValue + 1L));
            Assert.That(table.GetIndex(2L * int.MaxValue - 1L), Is.EqualTo(1));
            Assert.That(table.GetIndex(2L * int.MaxValue), Is.EqualTo(2));
        }

        [Test]
        public void Roll_UsesInjectedSourceAndSkipsEmptyTables()
        {
            IntegerChanceTable table = new(3, 7);
            long requestedBound = 0;
            table.RandomBelow = bound =>
            {
                requestedBound = bound;
                return bound - 1;
            };

            Assert.That(table.TryRoll(out int index, out IntegerChanceTable.Entry entry), Is.True);
            Assert.That(requestedBound, Is.EqualTo(10L));
            Assert.That(index, Is.EqualTo(1));
            Assert.That(entry, Is.SameAs(table.Entries[1]));

            table.Clear();
            Assert.That(table.Roll(), Is.EqualTo(-1));
        }

        [Test]
        public void SetWeightAndRemove_RefreshCachedPrefixSums()
        {
            IntegerChanceTable table = new(1, 1);
            Assert.That(table.GetIndex(1), Is.EqualTo(1));

            table.SetWeight(0, 9);
            Assert.That(table.TotalWeight, Is.EqualTo(10L));
            Assert.That(table.GetIndex(8), Is.EqualTo(0));

            table.RemoveAt(0);
            Assert.That(table.GetIndex(0), Is.EqualTo(0));
            table.SetWeight(0, -4);
            Assert.That(table.GetWeight(0), Is.Zero);
            Assert.That(table.Roll(), Is.EqualTo(-1));
        }

        [Test]
        public void UnityRandomBelow_IsInRangeAndRoughlyUniform()
        {
            Random.InitState(1234);
            int[] counts = new int[3];
            for (int i = 0; i < 3000; i++)
            {
                long value = IntegerChanceTable.UnityRandomBelow(3);
                Assert.That(value, Is.InRange(0L, 2L));
                counts[value]++;
            }

            foreach (int count in counts)
            {
                Assert.That(count, Is.InRange(850, 1150));
            }

            long large = 3L << 40;
            for (int i = 0; i < 200; i++)
            {
                Assert.That(IntegerChanceTable.UnityRandomBelow(large), Is.InRange(0L, large - 1L));
            }
        }

        [Test]
        public void UnityRandomBelow_HandlesBoundsAboveTwoToTheSixtieth()
        {
            Random.InitState(4321);
            long[] bounds = { (1L << 60) + 1L, (1L << 62) + 12345L, long.MaxValue };
            long largestSeen = 0;

            foreach (long bound in bounds)
            {
                for (int i = 0; i < 100; i++)
                {
                    long value = IntegerChanceTable.UnityRandomBelow(bound);
                    Assert.That(value, Is.InRange(0L, bound - 1L));
                    largestSeen = Math.Max(largestSeen, value);
                }
            }

            Assert.That(largestSeen, Is.GreaterThan(1L << 61), "the top bits of the 63-bit draw must be used");
        }

        [Test]
        public void InspectorEditWithSameCount_RefreshesCachedOddsAfterDeserialization()
        {
            IntegerChanceTable table = new(1, 1);
            Assert.That(table.TotalWeight, Is.EqualTo(2L), "positive control: the cache is warm");
            FieldInfo weight = typeof(IntegerChanceTable.Entry).GetField("weight",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(weight, Is.Not.Null);

            // WHY: An Inspector edit writes the serialized row and then deserializes; no table method runs.
            weight.SetValue(table.Entries[0], 9);
            ((ISerializationCallbackReceiver)table).OnAfterDeserialize();

            Assert.That(table.TotalWeight, Is.EqualTo(10L));
            Assert.That(table.GetIndex(8), Is.EqualTo(0));
            Assert.That(table.GetIndex(9), Is.EqualTo(1));
        }

        [Test]
        public void JsonRoundTrip_RebuildsCacheAfterDeserialization()
        {
            IntegerChanceTable table = new(2, 8);
            string json = JsonUtility.ToJson(new Holder { table = table });

            Holder restored = JsonUtility.FromJson<Holder>(json);

            Assert.That(restored.table.TotalWeight, Is.EqualTo(10L));
            Assert.That(restored.table.GetIndex(2), Is.EqualTo(1));
        }

        [Test]
        public void ChanceManagerEntryWeight_IsNotClampedToUnitRangeInTheInspector()
        {
            FieldInfo weight = typeof(ChanceManager.Entry).GetField("weight",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(weight, Is.Not.Null);
            Assert.That(weight.GetCustomAttribute<UnityEngine.RangeAttribute>(), Is.Null,
                "weights above 1 are valid when AutoNormalize is off or NormalizeTarget exceeds 1");
        }

        [Test]
        public void ChanceManagerSetChanceValue_WithAutoNormalize_DoesNotAllocateWhenWarm()
        {
            ChanceManager table = new(1f, 2f, 3f, 4f);
            for (int i = 0; i < 4; i++)
            {
                table.SetChanceValue(i % 4, 1f + i);
                table.GetChanceId();
            }

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < 64; i++)
            {
                table.SetChanceValue(i % 4, 1f + i % 5);
                table.GetId(0.5f);
            }

            long allocated = allocationCounter.End();
            Assert.That(allocated, Is.Zero);
        }

        [Serializable]
        public sealed class Holder
        {
            public IntegerChanceTable table;
        }
    }
}
