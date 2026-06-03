using System.Collections.Generic;
using Neo.GridSystem;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.GridSystem
{
    [TestFixture]
    public class FieldGeneratorCoverageTests
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
            return CreateGenerator(width, height, GridOrigin2D.Center);
        }

        private FieldGenerator CreateGenerator(int width, int height, GridOrigin2D origin)
        {
            var owner = new GameObject("FieldGeneratorCoverage");
            _createdObjects.Add(owner);
            owner.AddComponent<Grid>();
            FieldGenerator generator = owner.AddComponent<FieldGenerator>();
            var config = new FieldGeneratorConfig
            {
                Size = new Vector3Int(width, height, 0),
                GridType = GridType.Rectangular,
                MovementRule = MovementRule.FourDirections2D,
                PassabilityMode = CellPassabilityMode.WalkableEnabledAndUnoccupied,
                Origin2D = origin,
                BlockedCells = new List<Vector3Int>(),
                DisabledCells = new List<Vector3Int>(),
                ForcedEnabledCells = new List<Vector3Int>(),
                ForcedWalkableCells = new List<Vector3Int>()
            };

            generator.GenerateField(config);
            return generator;
        }

        [Test]
        public void InBounds_ReturnsFalseForOutOfRangePosition()
        {
            FieldGenerator generator = CreateGenerator(3, 3);

            Assert.IsTrue(generator.InBounds(new Vector3Int(1, 1, 0)));
            Assert.IsFalse(generator.InBounds(new Vector3Int(-1, 1, 0)));
            Assert.IsFalse(generator.InBounds(new Vector3Int(3, 1, 0)));
            Assert.IsFalse(generator.InBounds(new Vector3Int(1, 3, 0)));
        }

        [Test]
        public void FindPathDetailed_ReturnsNoPath_WhenThereIsNoValidRoute()
        {
            FieldGenerator generator = CreateGenerator(3, 1);
            var blockedCell = new Vector3Int(1, 0, 0);

            generator.SetWalkable(blockedCell, false);

            var request = new GridPathRequest
            {
                Start = new Vector3Int(0, 0, 0),
                End = new Vector3Int(2, 0, 0),
                Directions = MovementRule.FourDirections2D.Directions,
                IgnoreOccupied = false,
                IgnoreDisabled = false,
                IgnoreWalkability = false
            };

            GridPathResult result = generator.FindPathDetailed(request);

            Assert.IsFalse(result.HasPath);
            Assert.IsNotNull(result.Path);
            Assert.AreEqual(0, result.Path.Count);
        }

        [Test]
        public void CellMutationHelpers_UpdateContentAndPassabilityStates()
        {
            FieldGenerator generator = CreateGenerator(2, 2);
            var position = new Vector3Int(0, 0, 0);

            generator.SetCell(position, 5, true);
            generator.SetContentId(position, 11);
            generator.SetOccupied(position, true);
            generator.SetWalkable(position, false);
            generator.SetEnabled(position, false);

            FieldCell cell = generator.GetCell(position);

            Assert.AreEqual(11, cell.ContentId);
            Assert.AreEqual(false, cell.IsWalkable);
            Assert.AreEqual(false, cell.IsOccupied);
            Assert.AreEqual(false, cell.IsEnabled);
            Assert.IsFalse(generator.IsCellPassable(cell));
            Assert.IsTrue(generator.IsCellPassable(cell, true, true, true));
        }

        [Test]
        public void CenterOrigin_OddSizedField_IsCenteredAroundTransform()
        {
            FieldGenerator generator = CreateGenerator(5, 5, GridOrigin2D.Center);

            Vector3 first = generator.GetCellWorldCenter(new Vector3Int(0, 0, 0));
            Vector3 last = generator.GetCellWorldCenter(new Vector3Int(4, 4, 0));
            Vector3 boardCenter = (first + last) * 0.5f;

            Assert.That(boardCenter.x, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(boardCenter.y, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(generator.GetCellFromWorld(first).Position, Is.EqualTo(new Vector3Int(0, 0, 0)));
            Assert.That(generator.GetCellFromWorld(last).Position, Is.EqualTo(new Vector3Int(4, 4, 0)));
        }

        [Test]
        public void CenterOrigin_EvenSizedField_RemainsCenteredAroundTransform()
        {
            FieldGenerator generator = CreateGenerator(4, 4, GridOrigin2D.Center);

            Vector3 first = generator.GetCellWorldCenter(new Vector3Int(0, 0, 0));
            Vector3 last = generator.GetCellWorldCenter(new Vector3Int(3, 3, 0));
            Vector3 boardCenter = (first + last) * 0.5f;

            Assert.That(boardCenter.x, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(boardCenter.y, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void BottomLeftOrigin_KeepsPositiveBoardOffset()
        {
            FieldGenerator generator = CreateGenerator(5, 5, GridOrigin2D.BottomLeft);

            Vector3 first = generator.GetCellWorldCenter(new Vector3Int(0, 0, 0));
            Vector3 last = generator.GetCellWorldCenter(new Vector3Int(4, 4, 0));
            Vector3 boardCenter = (first + last) * 0.5f;

            Assert.That(boardCenter.x, Is.EqualTo(2.5f).Within(0.0001f));
            Assert.That(boardCenter.y, Is.EqualTo(2.5f).Within(0.0001f));
        }

        [Test]
        public void TryGetCellPositionFromWorld_UsesGeneratorOriginAndGridConversion()
        {
            FieldGenerator generator = CreateGenerator(5, 5, GridOrigin2D.Center);
            Vector3Int expected = new(3, 1, 0);
            Vector3 world = generator.GetCellWorldCenter(expected) + new Vector3(0.2f, -0.2f, 0f);

            bool found = generator.TryGetCellPositionFromWorld(world, out Vector3Int position);

            Assert.That(found, Is.True);
            Assert.That(position, Is.EqualTo(expected));
        }

        [Test]
        public void TrySnapWorldToCellCenter_ReturnsNearestCellCenter()
        {
            FieldGenerator generator = CreateGenerator(5, 5, GridOrigin2D.Center);
            Vector3Int expectedPosition = new(1, 4, 0);
            Vector3 expectedCenter = generator.GetCellWorldCenter(expectedPosition);
            Vector3 pointerWorld = expectedCenter + new Vector3(-0.3f, 0.25f, 0f);

            bool snapped = generator.TrySnapWorldToCellCenter(pointerWorld, out Vector3 snappedWorld);

            Assert.That(snapped, Is.True);
            Assert.That(snappedWorld.x, Is.EqualTo(expectedCenter.x).Within(0.0001f));
            Assert.That(snappedWorld.y, Is.EqualTo(expectedCenter.y).Within(0.0001f));
            Assert.That(snappedWorld.z, Is.EqualTo(expectedCenter.z).Within(0.0001f));
        }

        [Test]
        public void PlaceContentFootprint_WritesMultiCellContentAndRejectsInvalidCells()
        {
            FieldGenerator generator = CreateGenerator(3, 3, GridOrigin2D.Center);
            var entries = new[]
            {
                new GridPlacementEntry(Vector3Int.zero, 2),
                new GridPlacementEntry(Vector3Int.right, 4)
            };

            GridPlacementResult result = generator.PlaceContentFootprint(new Vector3Int(1, 1, 0), entries);

            Assert.That(result.Placed, Is.True);
            Assert.That(result.Positions, Is.EquivalentTo(new[]
            {
                new Vector3Int(1, 1, 0),
                new Vector3Int(2, 1, 0)
            }));
            Assert.That(generator.GetCell(1, 1).ContentId, Is.EqualTo(2));
            Assert.That(generator.GetCell(2, 1).ContentId, Is.EqualTo(4));
            Assert.That(generator.GetCell(1, 1).IsOccupied, Is.True);
            Assert.That(generator.CanPlaceContentFootprint(new Vector3Int(1, 1, 0), entries), Is.False);
            Assert.That(generator.CanPlaceContentFootprint(new Vector3Int(2, 2, 0), entries), Is.False);
        }
    }
}
