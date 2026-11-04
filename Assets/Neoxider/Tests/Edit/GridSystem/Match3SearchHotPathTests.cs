using System.Collections.Generic;
using Neo.GridSystem;
using Neo.GridSystem.Match3;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace Neo.Editor.Tests.GridSystem
{
    /// <summary>
    ///     Allocation guards for Match3 swap search and rejected-swap validation, plus parity of the search with an
    ///     independent live-swap oracle on random boards. Allocations are counted by the calibrated GC.Alloc probe of
    ///     <see cref="GcAlloc"/>; when it is not admitted the allocation cases are ignored, not passed.
    /// </summary>
    [TestFixture]
    public sealed class Match3SearchHotPathTests
    {
        private readonly List<GameObject> _roots = new();

        [SetUp]
        public void SetUp()
        {
            Assert.That(ReferenceEquals(FieldGenerator.I, null), Is.True,
                "An owned test board must not replace a borrowed FieldGenerator singleton.");
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject root in _roots)
            {
                if (root != null)
                {
                    Object.DestroyImmediate(root);
                }
            }

            _roots.Clear();
        }

        [TestCase(8, true)]
        [TestCase(8, false)]
        [TestCase(32, true)]
        [TestCase(32, false)]
        public void TryFindValidSwap_WarmQueryOnMeasuredBoard_DoesNotAllocate(int size, bool positive)
        {
            Match3BoardService service = CreateMeasuredBoard(size, positive, out FieldGenerator field);
            Assert.That(service.TryFindValidSwap(out _, out _), Is.EqualTo(positive));
            string before = Describe(field);

            bool found = false;
            Vector3Int a = default;
            Vector3Int b = default;
            long allocationCalls = GcAlloc.TryCount(() =>
            {
                for (int i = 0; i < 20; i++)
                {
                    found = service.TryFindValidSwap(out a, out b);
                }
            }, out string notAdmitted);

            Assert.That(found, Is.EqualTo(positive));
            Assert.That(a, Is.EqualTo(positive ? new Vector3Int(1, 1, 0) : Vector3Int.zero));
            Assert.That(b, Is.EqualTo(positive ? new Vector3Int(1, 0, 0) : Vector3Int.zero));
            Assert.That(Describe(field), Is.EqualTo(before), "Search must not change the live board.");
            GcAlloc.IgnoreIfNotAdmitted(notAdmitted);
            Assert.That(allocationCalls, Is.Zero,
                "A warm search reuses one board copy and one neighbor list.");
        }

        [Test]
        public void TrySwapAndResolve_WarmRejectedSwap_DoesNotAllocateAndKeepsTheBoard()
        {
            Match3BoardService service = CreateMeasuredBoard(16, false, out FieldGenerator field);
            Vector3Int a = new Vector3Int(4, 5, 0);
            Vector3Int b = new Vector3Int(4, 4, 0);
            int changed = 0;
            Assert.That(service.TrySwapAndResolve(a, b), Is.False);
            service.OnBoardChanged.AddListener(() => changed++);
            Assert.That(service.TrySwapAndResolve(a, b), Is.False);
            Assert.That(changed, Is.EqualTo(1), "A rejected swap still publishes its board-changed notification.");
            string before = Describe(field);

            long allocationCalls = GcAlloc.TryCount(() =>
            {
                for (int i = 0; i < 20; i++)
                {
                    service.TrySwapAndResolve(a, b);
                }
            }, out string notAdmitted);

            Assert.That(changed, Is.EqualTo(21));
            Assert.That(Describe(field), Is.EqualTo(before));
            GcAlloc.IgnoreIfNotAdmitted(notAdmitted);
            Assert.That(allocationCalls, Is.Zero,
                "Rejected-swap validation reuses the board copy and the neighbor list.");
        }

        [Test]
        public void TryFindValidSwap_MatchesLiveSwapOracleOnRandomBoards()
        {
            Random random = new Random(20261102);
            Vector3Int[][] rules =
            {
                new[] { Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down },
                new[] { Vector3Int.down },
                new[]
                {
                    Vector3Int.up, Vector3Int.right, Vector3Int.down, Vector3Int.left, new Vector3Int(1, 1, 0),
                    new Vector3Int(-1, -1, 0), new Vector3Int(1, -1, 0), new Vector3Int(-1, 1, 0)
                },
                new[]
                {
                    Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down, new Vector3Int(0, 0, 1),
                    new Vector3Int(0, 0, -1)
                }
            };

            Match3BoardService service = CreateService(out FieldGenerator field);
            int positives = 0;
            for (int board = 0; board < 300; board++)
            {
                Vector3Int size = new Vector3Int(random.Next(2, 8), random.Next(2, 8), random.Next(0, 4) == 0 ? 2 : 1);
                field.GenerateField(new FieldGeneratorConfig(size, new MovementRule(rules[board % rules.Length])));
                int colors = random.Next(3, 9);
                foreach (FieldCell cell in field.GetAllCells())
                {
                    int roll = random.Next(0, 40);
                    cell.IsEnabled = roll != 0;
                    cell.IsWalkable = roll != 1;
                    cell.IsOccupied = roll == 2;
                    cell.ContentId = roll == 3 ? 0 : random.Next(1, colors + 1);
                }

                int minimum = board % 5 == 0 ? 2 : 3;
                SetMinimum(service, minimum);
                bool expected = FindWithOracle(field, minimum, out Vector3Int expectedA, out Vector3Int expectedB);
                string before = Describe(field);

                bool found = service.TryFindValidSwap(out Vector3Int a, out Vector3Int b);

                Assert.That(found, Is.EqualTo(expected), "Board " + board);
                Assert.That(a, Is.EqualTo(expectedA), "Board " + board);
                Assert.That(b, Is.EqualTo(expectedB), "Board " + board);
                Assert.That(Describe(field), Is.EqualTo(before), "Board " + board);
                positives += found ? 1 : 0;
            }

            Assert.That(positives, Is.InRange(30, 270), "The random set must exercise both outcomes (about 78% positive).");
        }

        [Test]
        public void SearchAndValidation_ReadTheLiveBoardAcrossRegenerationsToOtherSizes()
        {
            Match3BoardService service = CreateService(out FieldGenerator field);
            SetMinimum(service, 3);

            GenerateUnique(field, new Vector3Int(3, 2, 1), Vector3Int.down);
            SeedCanonical(field, 0, 0);
            AssertSearch(service, true, new Vector3Int(1, 1, 0), new Vector3Int(1, 0, 0));

            GenerateUnique(field, new Vector3Int(6, 5, 1), Vector3Int.down);
            AssertSearch(service, false, Vector3Int.zero, Vector3Int.zero);
            SeedCanonical(field, 3, 3);
            AssertSearch(service, true, new Vector3Int(4, 4, 0), new Vector3Int(4, 3, 0));
            Assert.That(service.TrySwapAndResolve(new Vector3Int(1, 1, 0), new Vector3Int(1, 0, 0)), Is.False,
                "Validation must not reuse the previous board's contents.");

            GenerateUnique(field, new Vector3Int(3, 2, 2), Vector3Int.down);
            SeedCanonical(field, 0, 0);
            field.Cells[1, 1, 0].ContentId = 900;
            AssertSearch(service, false, Vector3Int.zero, Vector3Int.zero);
            SeedCanonical(field, 0, 0);
            AssertSearch(service, true, new Vector3Int(1, 1, 0), new Vector3Int(1, 0, 0));
        }

        private Match3BoardService CreateService(out FieldGenerator field)
        {
            GameObject root = new GameObject("Match3SearchHotPath");
            _roots.Add(root);
            root.SetActive(false);
            field = root.AddComponent<FieldGenerator>();
            field.DebugEnabled = false;
            Match3BoardService service = root.AddComponent<Match3BoardService>();
            JsonUtility.FromJsonOverwrite("{\"_autoGenerateOnStart\":false,\"_minMatchLength\":3}", service);
            root.SetActive(true);
            return service;
        }

        /// <summary>
        ///     The workload of the 2026-11-02 native baseline: unique tiles, a directed-down rule, one disabled, one
        ///     blocked and one occupied cell, and for the positive board the canonical 3x2 patch at the origin.
        /// </summary>
        private Match3BoardService CreateMeasuredBoard(int size, bool positive, out FieldGenerator field)
        {
            Match3BoardService service = CreateService(out field);
            FieldGeneratorConfig config = new FieldGeneratorConfig(new Vector3Int(size, size, 1),
                new MovementRule(new[] { Vector3Int.down }));
            config.DisabledCells.Add(new Vector3Int(size - 1, size - 1, 0));
            config.BlockedCells.Add(new Vector3Int(size - 2, size - 1, 0));
            field.GenerateField(config);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                field.Cells[x, y, 0].ContentId = 100 + y * size + x;
            }

            field.Cells[size - 1, size - 2, 0].IsOccupied = true;
            if (positive)
            {
                SeedCanonical(field, 0, 0);
            }

            return service;
        }

        private static void GenerateUnique(FieldGenerator field, Vector3Int size, Vector3Int direction)
        {
            field.GenerateField(new FieldGeneratorConfig(size, new MovementRule(new[] { direction })));
            int ordinal = 0;
            foreach (FieldCell cell in field.GetAllCells())
            {
                cell.ContentId = 1000 + ordinal++;
            }
        }

        private static void SeedCanonical(FieldGenerator field, int originX, int originY)
        {
            int[,] content = { { 1, 3, 1 }, { 2, 1, 2 } };
            for (int y = 0; y < 2; y++)
            for (int x = 0; x < 3; x++)
            {
                field.Cells[originX + x, originY + y, 0].ContentId = content[y, x];
            }
        }

        private static void SetMinimum(Match3BoardService service, int minimum)
        {
            JsonUtility.FromJsonOverwrite("{\"_minMatchLength\":" + minimum + "}", service);
        }

        private static void AssertSearch(Match3BoardService service, bool expected, Vector3Int expectedA,
            Vector3Int expectedB)
        {
            bool found = service.TryFindValidSwap(out Vector3Int a, out Vector3Int b);
            Assert.That(found, Is.EqualTo(expected));
            Assert.That(a, Is.EqualTo(expectedA));
            Assert.That(b, Is.EqualTo(expectedB));
        }

        /// <summary>
        ///     Independent reference: public cell and neighbor order, a temporary live swap, and the public match finder
        ///     deciding whether a group now contains either endpoint.
        /// </summary>
        private static bool FindWithOracle(FieldGenerator field, int minimum, out Vector3Int a, out Vector3Int b)
        {
            a = default;
            b = default;
            foreach (FieldCell cell in field.GetAllCells(false))
            {
                if (!Usable(cell))
                {
                    continue;
                }

                foreach (FieldCell neighbor in field.GetNeighbors(cell))
                {
                    if (!Usable(neighbor) || ReferenceEquals(neighbor, cell))
                    {
                        continue;
                    }

                    if (SwapCreatesEndpointMatch(field, cell, neighbor, minimum))
                    {
                        a = cell.Position;
                        b = neighbor.Position;
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool SwapCreatesEndpointMatch(FieldGenerator field, FieldCell first, FieldCell second,
            int minimum)
        {
            int firstContent = first.ContentId;
            int secondContent = second.ContentId;
            try
            {
                first.ContentId = secondContent;
                second.ContentId = firstContent;
                foreach (List<FieldCell> group in Match3MatchFinder.FindMatches(field, minimum))
                {
                    if (group.Contains(first) || group.Contains(second))
                    {
                        return true;
                    }
                }

                return false;
            }
            finally
            {
                first.ContentId = firstContent;
                second.ContentId = secondContent;
            }
        }

        private static bool Usable(FieldCell cell)
        {
            return cell != null && cell.IsEnabled && cell.IsWalkable && !cell.IsOccupied;
        }

        private static string Describe(FieldGenerator field)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            foreach (FieldCell cell in field.GetAllCells())
            {
                builder.Append(cell.Position).Append(':').Append(cell.ContentId).Append(cell.IsEnabled ? 'e' : '-')
                    .Append(cell.IsWalkable ? 'w' : '-').Append(cell.IsOccupied ? 'o' : '-').Append(';');
            }

            return builder.ToString();
        }
    }
}
