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
