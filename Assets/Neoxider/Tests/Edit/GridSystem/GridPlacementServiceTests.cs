using System.Collections.Generic;
using Neo.GridSystem;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.GridSystem
{
    /// <summary>
    ///     Covers the rule-driven <see cref="GridPlacementService"/> over
    ///     <see cref="FieldGenerator"/>: requirement toggles, custom predicate, overwrite policy,
    ///     multi-cell footprints, failure reasons, and change notifications.
    /// </summary>
    [TestFixture]
    public class GridPlacementServiceTests
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

        private FieldGenerator CreateField(int width, int height)
        {
            var owner = new GameObject("GridPlacementServiceTests");
            _createdObjects.Add(owner);
            owner.AddComponent<Grid>();
            FieldGenerator generator = owner.AddComponent<FieldGenerator>();
            generator.GenerateField(new FieldGeneratorConfig
            {
                Size = new Vector3Int(width, height, 0),
                GridType = GridType.Rectangular,
                MovementRule = MovementRule.FourDirections2D,
                PassabilityMode = CellPassabilityMode.WalkableEnabledAndUnoccupied,
                Origin2D = GridOrigin2D.BottomLeft,
                BlockedCells = new List<Vector3Int>(),
                DisabledCells = new List<Vector3Int>(),
                ForcedEnabledCells = new List<Vector3Int>(),
                ForcedWalkableCells = new List<Vector3Int>()
            });
            return generator;
        }

        [Test]
        public void Place_SingleCell_WritesContentAndOccupancy()
        {
            FieldGenerator field = CreateField(3, 3);
            var service = new GridPlacementService(field);

            GridPlacementResult result = service.Place(GridPlacementRequest.Single(new Vector3Int(1, 1, 0), 7));

            Assert.IsTrue(result.Placed);
            FieldCell cell = field.GetCell(new Vector3Int(1, 1, 0));
            Assert.AreEqual(7, cell.ContentId);
            Assert.IsTrue(cell.IsOccupied);
        }

        [Test]
        public void Place_OutOfBounds_FailsWithReason()
        {
            FieldGenerator field = CreateField(2, 2);
            var service = new GridPlacementService(field);

            GridPlacementResult result = service.Place(GridPlacementRequest.Single(new Vector3Int(5, 0, 0), 1));

            Assert.IsFalse(result.Placed);
            StringAssert.Contains("out of bounds", result.FailureReason);
        }

        [Test]
        public void Requirements_CanBeRelaxedIndividually()
        {
            FieldGenerator field = CreateField(3, 3);
            var service = new GridPlacementService(field);
            var pos = new Vector3Int(0, 0, 0);
            field.SetWalkable(pos, false);

            GridPlacementRequest strict = GridPlacementRequest.Single(pos, 1);
            Assert.IsFalse(service.CanPlace(strict, out string reason));
            StringAssert.Contains("not walkable", reason);

            GridPlacementRequest relaxed = GridPlacementRequest.Single(pos, 1);
            relaxed.RequireWalkable = false;
            Assert.IsTrue(service.CanPlace(relaxed));

            field.SetEnabled(pos, false);
            Assert.IsFalse(service.CanPlace(relaxed, out reason));
            StringAssert.Contains("disabled", reason);

            relaxed.RequireEnabled = false;
            Assert.IsTrue(service.CanPlace(relaxed));
        }

        [Test]
        public void OccupiedCell_RejectedByDefault_OverwritePolicyAllows()
        {
            FieldGenerator field = CreateField(3, 3);
            var service = new GridPlacementService(field);
            var pos = new Vector3Int(1, 0, 0);

            Assert.IsTrue(service.Place(GridPlacementRequest.Single(pos, 1)).Placed);

            GridPlacementRequest second = GridPlacementRequest.Single(pos, 2);
            GridPlacementResult rejected = service.Place(second);
            Assert.IsFalse(rejected.Placed);
            StringAssert.Contains("occupied", rejected.FailureReason);

            second.OverwritePolicy = GridOverwritePolicy.Overwrite;
            GridPlacementResult overwritten = service.Place(second);
            Assert.IsTrue(overwritten.Placed);
            Assert.AreEqual(2, field.GetCell(pos).ContentId);
        }

        [Test]
        public void CustomPredicate_RejectsCells()
        {
            FieldGenerator field = CreateField(3, 3);
            var service = new GridPlacementService(field);
            field.GetCell(new Vector3Int(2, 2, 0)).Type = 9;

            GridPlacementRequest request = GridPlacementRequest.Single(new Vector3Int(2, 2, 0), 1);
            request.CellPredicate = cell => cell.Type != 9;

            Assert.IsFalse(service.CanPlace(request, out string reason));
            StringAssert.Contains("predicate", reason);

            request.CellPredicate = null;
            Assert.IsTrue(service.CanPlace(request));
        }

        [Test]
        public void MultiCellFootprint_IsAtomic()
        {
            FieldGenerator field = CreateField(3, 3);
            var service = new GridPlacementService(field);
            field.SetOccupied(new Vector3Int(1, 1, 0), true);

            var request = new GridPlacementRequest
            {
                Anchor = new Vector3Int(0, 1, 0),
                Entries = new[]
                {
                    new GridPlacementEntry(Vector3Int.zero, 5),
                    new GridPlacementEntry(new Vector3Int(1, 0, 0), 5) // lands on the occupied cell
                }
            };

            int originalContentId = field.GetCell(new Vector3Int(0, 1, 0)).ContentId;
            GridPlacementResult result = service.Place(request);

            Assert.IsFalse(result.Placed);
            Assert.AreEqual(originalContentId, field.GetCell(new Vector3Int(0, 1, 0)).ContentId,
                "A failed footprint must not partially write cells.");
        }

        [Test]
        public void Place_NotifiesCellStateChanged_UnlessDisabled()
        {
            FieldGenerator field = CreateField(3, 3);
            var service = new GridPlacementService(field);
            int notifications = 0;
            field.OnCellStateChanged.AddListener(_ => notifications++);

            service.Place(GridPlacementRequest.Single(new Vector3Int(0, 0, 0), 1));
            Assert.AreEqual(1, notifications);

            GridPlacementRequest silent = GridPlacementRequest.Single(new Vector3Int(1, 0, 0), 1);
            silent.Notify = false;
            service.Place(silent);
            Assert.AreEqual(1, notifications, "Notify=false must not raise cell events.");
        }

        [Test]
        public void EmptyOrNullRequest_FailsGracefully()
        {
            FieldGenerator field = CreateField(2, 2);
            var service = new GridPlacementService(field);

            Assert.IsFalse(service.CanPlace(null, out string nullReason));
            StringAssert.Contains("null", nullReason);

            var empty = new GridPlacementRequest { Anchor = Vector3Int.zero, Entries = new GridPlacementEntry[0] };
            Assert.IsFalse(service.CanPlace(empty, out string emptyReason));
            StringAssert.Contains("entries", emptyReason);
        }
    }
}
