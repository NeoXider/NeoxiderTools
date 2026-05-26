using System.Collections.Generic;
using Neo.GridSystem;
using Neo.GridSystem.SlidingMerge;
using NUnit.Framework;
using UnityEngine;

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

        private FieldGenerator CreateGenerator(int width, int height)
        {
            var owner = new GameObject("SlidingMergeResolverCoverage");
            _createdObjects.Add(owner);
            FieldGenerator generator = owner.AddComponent<FieldGenerator>();
            var config = new FieldGeneratorConfig
            {
                Size = new Vector3Int(width, height, 0),
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
    }
}
