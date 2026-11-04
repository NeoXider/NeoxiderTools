using System;
using System.Collections.Generic;
using Neo.GridSystem;
using Neo.GridSystem.SlidingMerge;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests.GridSystem
{
    [TestFixture]
    public class SlidingMergeResolverCoverageTests
    {
        private readonly List<GameObject> _createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject createdObject in _createdObjects)
            {
                if (createdObject != null)
                {
                    Object.DestroyImmediate(createdObject);
                }
            }

            _createdObjects.Clear();
        }

        private static readonly SlidingMergeDirection[] AllDirections =
        {
            SlidingMergeDirection.Left, SlidingMergeDirection.Right, SlidingMergeDirection.Up,
            SlidingMergeDirection.Down, SlidingMergeDirection.Forward, SlidingMergeDirection.Backward
        };

        private FieldGenerator CreateGenerator(int width, int height, int depth = 0)
        {
            GameObject owner = new GameObject("SlidingMergeResolverCoverage");
            _createdObjects.Add(owner);
            FieldGenerator generator = owner.AddComponent<FieldGenerator>();
            FieldGeneratorConfig config = new FieldGeneratorConfig
            {
                Size = new Vector3Int(width, height, depth),
                GridType = GridType.Rectangular,
                MovementRule = MovementRule.FourDirections2D,
                PassabilityMode = CellPassabilityMode.WalkableEnabledAndUnoccupied,
                BlockedCells = new List<Vector3Int>(),
                DisabledCells = new List<Vector3Int>(),
                ForcedEnabledCells = new List<Vector3Int>(),
                ForcedWalkableCells = new List<Vector3Int>()
            };

            generator.GenerateField(config);
            return generator;
        }

        [Test]
        public void TrySpawnRandomContent_ReturnsFalse_WhenNoEmptyCellExists()
        {
            FieldGenerator generator = CreateGenerator(1, 2);
            generator.SetCell(0, 0, 1, true);
            generator.SetCell(0, 1, 2, true);

            Assert.IsFalse(SlidingMergeResolver.TrySpawnRandomContent(generator, 7, 0));
        }

        [Test]
        public void TrySpawnRandomContent_SetsOneEmptyCell()
        {
            FieldGenerator generator = CreateGenerator(2, 1);
            generator.SetContentId(new Vector3Int(0, 0, 0), 5);
            generator.SetContentId(new Vector3Int(1, 0, 0), 0);

            Assert.IsTrue(SlidingMergeResolver.TrySpawnRandomContent(generator, 7, 0));

            int filledCount = 0;
            foreach (FieldCell cell in generator.Cells)
            {
                if (cell.ContentId == 7)
                {
                    filledCount++;
                }
            }

            Assert.AreEqual(1, filledCount);
        }

        [Test]
        public void Slide_DoesNotMoveWhenNoMovesAvailable()
        {
            FieldGenerator generator = CreateGenerator(2, 1);
            generator.SetCell(new Vector3Int(0, 0, 0), 1, true);
            generator.SetCell(new Vector3Int(1, 0, 0), 1, true);

            SlidingMergeResult result = SlidingMergeResolver.Slide(
                generator,
                SlidingMergeDirection.Left,
                0,
                (_, _) => false,
                (_, _) => 0);

            Assert.IsFalse(result.Changed);
            Assert.AreEqual(0, result.MoveCount);
            Assert.AreEqual(0, result.MergeCount);
        }

        [Test]
        public void CanSlide_MatchesSlideOnRandomBoardsWithHolesLayersAndCustomRules()
        {
            System.Random random = new System.Random(20261104);
            Func<int, int, bool> sumsUpToEight = (a, b) => a + b <= 8;
            Func<int, int, int> sum = (a, b) => a + b;
            int changedPreviews = 0;
            int checkedPreviews = 0;
            for (int board = 0; board < 120; board++)
            {
                FieldGenerator generator = CreateGenerator(random.Next(1, 6), random.Next(1, 6),
                    random.Next(0, 3) == 0 ? 2 : 1);
                foreach (FieldCell cell in generator.Cells)
                {
                    int roll = random.Next(0, 12);
                    cell.IsEnabled = roll != 0;
                    cell.IsWalkable = roll != 1;
                    cell.ContentId = roll < 5 ? 0 : 2 * random.Next(1, 4);
                }

                bool custom = board % 2 == 1;
                int[] before = Capture(generator);
                foreach (SlidingMergeDirection direction in AllDirections)
                {
                    bool preview = custom
                        ? SlidingMergeResolver.CanSlide(generator, direction, 0, sumsUpToEight, sum)
                        : SlidingMergeResolver.CanSlide(generator, direction);
                    CollectionAssert.AreEqual(before, Capture(generator), "A preview never writes.");
                    SlidingMergeResult slide = custom
                        ? SlidingMergeResolver.Slide(generator, direction, 0, sumsUpToEight, sum)
                        : SlidingMergeResolver.Slide(generator, direction);

                    Assert.That(preview, Is.EqualTo(slide.Changed), "Board " + board + " " + direction);
                    changedPreviews += preview ? 1 : 0;
                    checkedPreviews++;
                    Restore(generator, before);
                }
            }

            Assert.That(changedPreviews, Is.GreaterThan(0).And.LessThan(checkedPreviews),
                "The random set must exercise both outcomes.");
        }

        [Test]
        public void CanSlideAndHasAnyMove_WarmCalls_DoNotAllocate()
        {
            FieldGenerator locked = CreateLockedBoard();
            FieldGenerator open = CreateGenerator(4, 4);
            foreach (FieldCell cell in open.Cells)
            {
                cell.ContentId = cell.Position.x == 3 ? 0 : 2 + cell.Position.x * 10 + cell.Position.y * 100;
            }

            Func<int, int, bool> canMerge = (a, b) => a == b;
            Func<int, int, int> merge = (a, b) => a * 2;
            int moves = 0;
            Action poll = () =>
            {
                moves += SlidingMergeResolver.HasAnyMove(locked) ? 1 : 0;
                moves += SlidingMergeResolver.HasAnyMove(locked, 0, canMerge, merge) ? 1 : 0;
                moves += SlidingMergeResolver.HasAnyMove(open) ? 1 : 0;
                moves += SlidingMergeResolver.CanSlide(open, SlidingMergeDirection.Right) ? 1 : 0;
                moves += SlidingMergeResolver.CanSlide(open, SlidingMergeDirection.Left, 0, canMerge, merge) ? 1 : 0;
            };
            poll();
            moves = 0;

            long calls = GcAlloc.TryCount(() =>
            {
                for (int i = 0; i < 20; i++)
                {
                    poll();
                }
            }, out string notAdmitted);

            Assert.That(moves, Is.EqualTo(40), "Only the open board has moves: HasAnyMove and Right.");
            GcAlloc.IgnoreIfNotAdmitted(notAdmitted);
            Assert.That(calls, Is.Zero, "Previews reuse the resolver's scratch result and its cached default rules.");
        }

        [Test]
        public void Slide_WithoutMovement_AllocatesOnlyItsResult()
        {
            FieldGenerator locked = CreateLockedBoard();
            SlidingMergeResolver.Slide(locked, SlidingMergeDirection.Left);
            bool changed = false;

            long calls = GcAlloc.TryCount(
                () => changed |= SlidingMergeResolver.Slide(locked, SlidingMergeDirection.Left).Changed,
                out string notAdmitted);

            Assert.That(changed, Is.False);
            GcAlloc.IgnoreIfNotAdmitted(notAdmitted);
            Assert.That(calls, Is.LessThanOrEqualTo(2),
                "A slide allocates its result and steps list; the default rules are cached delegates.");
        }

        /// <summary>A 4x4 checkerboard of 2 and 4: no direction can move or merge.</summary>
        private FieldGenerator CreateLockedBoard()
        {
            FieldGenerator locked = CreateGenerator(4, 4);
            foreach (FieldCell cell in locked.Cells)
            {
                cell.ContentId = (cell.Position.x + cell.Position.y) % 2 == 0 ? 2 : 4;
            }

            return locked;
        }

        private static int[] Capture(FieldGenerator generator)
        {
            List<int> values = new List<int>();
            foreach (FieldCell cell in generator.Cells)
            {
                values.Add(cell.ContentId);
            }

            return values.ToArray();
        }

        private static void Restore(FieldGenerator generator, int[] values)
        {
            int index = 0;
            foreach (FieldCell cell in generator.Cells)
            {
                cell.ContentId = values[index++];
            }
        }
    }
}
