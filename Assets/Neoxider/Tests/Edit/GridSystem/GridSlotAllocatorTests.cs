using System.Collections.Generic;
using Neo.GridSystem;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.GridSystem
{
    [TestFixture]
    public sealed class GridSlotAllocatorTests
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

        [Test]
        public void TryAllocateFirstAvailable_UsesPreferenceOrderAndMarksCellOccupied()
        {
            FieldGenerator field = CreateField(3, 2);
            GridSlotAllocator allocator = new(field);
            field.SetOccupied(new Vector3Int(0, 0, 0), true);

            bool allocated = allocator.TryAllocateFirstAvailable(
                new[]
                {
                    new Vector3Int(0, 0, 0),
                    new Vector3Int(1, 0, 0),
                    new Vector3Int(2, 0, 0)
                },
                42,
                out Vector3Int position,
                out GridPlacementResult result);

            Assert.That(allocated, Is.True);
            Assert.That(result.Placed, Is.True);
            Assert.That(position, Is.EqualTo(new Vector3Int(1, 0, 0)));
            Assert.That(field.GetCell(position).ContentId, Is.EqualTo(42));
            Assert.That(field.GetCell(position).IsOccupied, Is.True);
        }

        [Test]
        public void SlotIndexMapping_MapsRectangularBoardInRowMajorOrder()
        {
            FieldGenerator field = CreateField(3, 2);
            GridSlotAllocator allocator = new(field);

            Assert.That(allocator.TryGetSlotPosition(0, out Vector3Int slot0), Is.True);
            Assert.That(slot0, Is.EqualTo(new Vector3Int(0, 0, 0)));
            Assert.That(allocator.TryGetSlotPosition(1, out Vector3Int slot1), Is.True);
            Assert.That(slot1, Is.EqualTo(new Vector3Int(1, 0, 0)));
            Assert.That(allocator.TryGetSlotPosition(2, out Vector3Int slot2), Is.True);
            Assert.That(slot2, Is.EqualTo(new Vector3Int(2, 0, 0)));
            Assert.That(allocator.TryGetSlotPosition(3, out Vector3Int slot3), Is.True);
            Assert.That(slot3, Is.EqualTo(new Vector3Int(0, 1, 0)));
            Assert.That(allocator.TryGetSlotPosition(4, out Vector3Int slot4), Is.True);
            Assert.That(slot4, Is.EqualTo(new Vector3Int(1, 1, 0)));
            Assert.That(allocator.TryGetSlotPosition(5, out Vector3Int slot5), Is.True);
            Assert.That(slot5, Is.EqualTo(new Vector3Int(2, 1, 0)));
        }

        [Test]
        public void SlotIndexMapping_RejectsInvalidIndicesAndPositions()
        {
            FieldGenerator field = CreateField(3, 2);
            GridSlotAllocator allocator = new(field);

            Assert.That(allocator.TryGetSlotPosition(-1, out _), Is.False);
            Assert.That(allocator.TryGetSlotPosition(6, out _), Is.False);
            Assert.That(allocator.TryGetSlotIndex(new Vector3Int(-1, 0, 0), out _), Is.False);
            Assert.That(allocator.TryGetSlotIndex(new Vector3Int(3, 0, 0), out _), Is.False);
            Assert.That(allocator.TryGetSlotIndex(new Vector3Int(0, 2, 0), out _), Is.False);
            Assert.That(allocator.TryGetSlotIndex(new Vector3Int(0, 0, 1), out _), Is.False);
        }

        [Test]
        public void SlotIndexMapping_RejectsNonRectangularOr3DFields()
        {
            FieldGenerator hexField = CreateField(3, 2);
            hexField.Config.GridType = GridType.Hexagonal;
            GridSlotAllocator hexAllocator = new(hexField);

            Assert.That(hexAllocator.TryGetSlotPosition(0, out _), Is.False);
            Assert.That(hexAllocator.TryGetSlotIndex(Vector3Int.zero, out _), Is.False);

            FieldGenerator threeDimensionalField = CreateField(3, 2);
            threeDimensionalField.GenerateField(new FieldGeneratorConfig
            {
                Size = new Vector3Int(3, 2, 2),
                GridType = GridType.Rectangular,
                MovementRule = MovementRule.FourDirections2D,
                Origin2D = GridOrigin2D.BottomLeft,
                DisabledCells = new List<Vector3Int>(),
                ForcedEnabledCells = new List<Vector3Int>(),
                BlockedCells = new List<Vector3Int>(),
                ForcedWalkableCells = new List<Vector3Int>()
            });
            GridSlotAllocator threeDimensionalAllocator = new(threeDimensionalField);

            Assert.That(threeDimensionalAllocator.TryGetSlotPosition(0, out _), Is.False);
            Assert.That(threeDimensionalAllocator.TryGetSlotIndex(Vector3Int.zero, out _), Is.False);
        }

        [Test]
        public void SlotIndexMapping_ReturnsReverseIndexForValidPositions()
        {
            FieldGenerator field = CreateField(3, 2);
            GridSlotAllocator allocator = new(field);

            Assert.That(allocator.TryGetSlotIndex(new Vector3Int(0, 0, 0), out int slot0), Is.True);
            Assert.That(slot0, Is.EqualTo(0));
            Assert.That(allocator.TryGetSlotIndex(new Vector3Int(2, 1, 0), out int slot5), Is.True);
            Assert.That(slot5, Is.EqualTo(5));
        }

        [Test]
        public void AllocateBySlotIndex_MarksContentAndRejectsOutOfBounds()
        {
            FieldGenerator field = CreateField(3, 2);
            GridSlotAllocator allocator = new(field);

            GridPlacementResult result = allocator.Allocate(4, 77);
            FieldCell cell = field.GetCell(new Vector3Int(1, 1, 0));

            Assert.That(result.Placed, Is.True);
            Assert.That(cell.ContentId, Is.EqualTo(77));
            Assert.That(cell.IsOccupied, Is.True);
            Assert.That(allocator.IsAvailable(4), Is.False);

            GridPlacementResult invalid = allocator.Allocate(6, 88);
            Assert.That(invalid.Placed, Is.False);
            Assert.That(invalid.FailureReason, Is.EqualTo("Invalid slot index."));
        }

        [Test]
        public void Release_ClearsContentAndOccupancy()
        {
            FieldGenerator field = CreateField(2, 1);
            GridSlotAllocator allocator = new(field);
            Vector3Int position = new(0, 0, 0);
            allocator.Allocate(position, 7);

            bool released = allocator.Release(position);

            Assert.That(released, Is.True);
            Assert.That(field.GetCell(position).ContentId, Is.EqualTo(-1));
            Assert.That(field.GetCell(position).IsOccupied, Is.False);
            Assert.That(allocator.IsAvailable(position), Is.True);
        }

        [Test]
        public void TryFindFirstAvailable_ReturnsFalseWhenPreferencesAreBlocked()
        {
            FieldGenerator field = CreateField(2, 1);
            GridSlotAllocator allocator = new(field);
            field.SetWalkable(new Vector3Int(0, 0, 0), false);
            field.SetOccupied(new Vector3Int(1, 0, 0), true);

            bool found = allocator.TryFindFirstAvailable(
                new[]
                {
                    new Vector3Int(0, 0, 0),
                    new Vector3Int(1, 0, 0)
                },
                out _);

            Assert.That(found, Is.False);
        }

        private FieldGenerator CreateField(int width, int height)
        {
            GameObject owner = new("GridSlotAllocatorTests");
            _createdObjects.Add(owner);
            owner.AddComponent<Grid>();
            FieldGenerator field = owner.AddComponent<FieldGenerator>();
            field.GenerateField(new FieldGeneratorConfig
            {
                Size = new Vector3Int(width, height, 1),
                GridType = GridType.Rectangular,
                MovementRule = MovementRule.FourDirections2D,
                Origin2D = GridOrigin2D.BottomLeft,
                DisabledCells = new List<Vector3Int>(),
                ForcedEnabledCells = new List<Vector3Int>(),
                BlockedCells = new List<Vector3Int>(),
                ForcedWalkableCells = new List<Vector3Int>()
            });
            return field;
        }
    }
}
