using System.Collections.Generic;
using Neo.GridSystem;
using Neo.GridSystem.Dice;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.GridSystem
{
    public sealed class Audit20261023TestGameplayPlacementTests
    {
        private GameObject _root;
        private FieldGenerator _field;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject(nameof(Audit20261023TestGameplayPlacementTests));
            // Public generation works without activating the component or replacing FieldGenerator.I.
            _root.SetActive(false);
            _field = _root.AddComponent<FieldGenerator>();
            _field.GenerateField(new FieldGeneratorConfig
            {
                Size = new Vector3Int(2, 1, 1),
                GridType = GridType.Rectangular,
                MovementRule = MovementRule.FourDirections2D,
                Origin2D = GridOrigin2D.BottomLeft
            });
            _field.GetCell(Vector3Int.zero).ContentId = 17;
            _field.GetCell(Vector3Int.right).ContentId = 23;
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                Object.DestroyImmediate(_root);
            }
        }

        [TestCase("disabled")]
        [TestCase("blocked")]
        [TestCase("occupied")]
        [TestCase("out-of-bounds")]
        [TestCase("duplicate")]
        [TestCase("null")]
        [TestCase("predicate")]
        public void Place_RejectedLaterEntry_PreservesCellsAndPublishesNothing(string rejection)
        {
            FieldCell first = _field.GetCell(Vector3Int.zero);
            FieldCell second = _field.GetCell(Vector3Int.right);
            GridPlacementEntry[] entries =
            {
                new GridPlacementEntry(Vector3Int.zero, 41),
                new GridPlacementEntry(Vector3Int.right, 42)
            };
            GridPlacementRequest request = new GridPlacementRequest { Entries = entries };
            switch (rejection)
            {
                case "disabled":
                    second.IsEnabled = false;
                    break;
                case "blocked":
                    second.IsWalkable = false;
                    break;
                case "occupied":
                    second.IsOccupied = true;
                    break;
                case "out-of-bounds":
                    entries[1].Offset = new Vector3Int(2, 0, 0);
                    break;
                case "duplicate":
                    entries[1].Offset = Vector3Int.zero;
                    break;
                case "null":
                    entries[1] = null;
                    break;
                case "predicate":
                    request.CellPredicate = cell => cell.Position != Vector3Int.right;
                    break;
                default:
                    Assert.Fail("Unknown rejection scenario.");
                    break;
            }

            bool secondEnabled = second.IsEnabled;
            bool secondWalkable = second.IsWalkable;
            bool secondOccupied = second.IsOccupied;
            List<FieldCell> notifications = new List<FieldCell>();
            _field.OnCellStateChanged.AddListener(notifications.Add);
            GridPlacementService placement = new GridPlacementService(_field);

            Assert.That(placement.CanPlace(request, out string reason), Is.False);
            Assert.That(reason, Is.Not.Null.And.Not.Empty);
            GridPlacementResult result = placement.Place(request);

            Assert.That(result.Placed, Is.False);
            Assert.That(result.FailureReason, Is.Not.Null.And.Not.Empty);
            Assert.That(result.Cells, Is.Empty);
            Assert.That(result.Positions, Is.Empty);
            Assert.That(first.ContentId, Is.EqualTo(17));
            Assert.That(first.IsOccupied, Is.False);
            Assert.That(first.IsEnabled, Is.True);
            Assert.That(first.IsWalkable, Is.True);
            Assert.That(second.ContentId, Is.EqualTo(23));
            Assert.That(second.IsEnabled, Is.EqualTo(secondEnabled));
            Assert.That(second.IsWalkable, Is.EqualTo(secondWalkable));
            Assert.That(second.IsOccupied, Is.EqualTo(secondOccupied));
            Assert.That(notifications, Is.Empty);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Place_CommitsWholeFootprintBeforeOptionalNotifications(bool notify)
        {
            FieldCell first = _field.GetCell(Vector3Int.zero);
            FieldCell second = _field.GetCell(Vector3Int.right);
            GridPlacementRequest request = new GridPlacementRequest
            {
                Entries = new[]
                {
                    new GridPlacementEntry(Vector3Int.zero, 41),
                    new GridPlacementEntry(Vector3Int.right, 42, occupiesCell: false)
                },
                Notify = notify
            };
            List<Vector3Int> notifications = new List<Vector3Int>();
            _field.OnCellStateChanged.AddListener(cell =>
            {
                Assert.That(first.ContentId, Is.EqualTo(41));
                Assert.That(first.IsOccupied, Is.True);
                Assert.That(second.ContentId, Is.EqualTo(42));
                Assert.That(second.IsOccupied, Is.False);
                notifications.Add(cell.Position);
            });
            GridPlacementService placement = new GridPlacementService(_field);

            Assert.That(placement.CanPlace(request), Is.True);
            Assert.That(first.ContentId, Is.EqualTo(17));
            Assert.That(second.ContentId, Is.EqualTo(23));
            Assert.That(first.IsOccupied, Is.False);
            Assert.That(second.IsOccupied, Is.False);
            Assert.That(notifications, Is.Empty);
            GridPlacementResult result = placement.Place(request);

            Assert.That(result.Placed, Is.True);
            Assert.That(result.FailureReason, Is.Null);
            Assert.That(result.Cells, Is.EqualTo(new[] { first, second }));
            Assert.That(result.Positions, Is.EqualTo(new[] { Vector3Int.zero, Vector3Int.right }));
            Assert.That(first.ContentId, Is.EqualTo(41));
            Assert.That(first.IsOccupied, Is.True);
            Assert.That(second.ContentId, Is.EqualTo(42));
            Assert.That(second.IsOccupied, Is.False);
            Assert.That(notifications, Has.Count.EqualTo(notify ? 2 : 0));
            if (notify)
            {
                Assert.That(notifications, Is.EqualTo(new[] { Vector3Int.zero, Vector3Int.right }));
            }
        }

        [Test]
        public void DiceBoard_MergingPlacement_NotifiesCellStateAndOneLogicalBoardChange()
        {
            _field.GenerateField(new FieldGeneratorConfig
            {
                Size = new Vector3Int(3, 1, 1),
                GridType = GridType.Rectangular,
                MovementRule = MovementRule.FourDirections2D
            });
            DiceBoard board = new DiceBoard(_field);
            FieldCell first = _field.GetCell(Vector3Int.zero);
            FieldCell second = _field.GetCell(Vector3Int.right);
            FieldCell anchor = _field.GetCell(new Vector3Int(2, 0, 0));
            first.ContentId = 1;
            first.IsOccupied = true;
            second.ContentId = 1;
            second.IsOccupied = true;
            anchor.ContentId = board.EmptyContentId;
            List<Vector3Int> notifiedPositions = new List<Vector3Int>();
            int clearedCellReceipts = 0;
            int boardChanges = 0;
            int mergeReceipts = 0;
            _field.OnCellStateChanged.AddListener(cell =>
            {
                notifiedPositions.Add(cell.Position);
                Assert.That(cell.IsOccupied, Is.EqualTo(cell.ContentId != board.EmptyContentId));
                if (cell.ContentId == board.EmptyContentId)
                {
                    clearedCellReceipts++;
                }
            });
            board.BoardChanged += () => boardChanges++;
            board.MergesResolved += result => mergeReceipts++;

            DicePlacementResult placement = board.Place(DicePiece.Single(1), anchor.Position);

            Assert.That(placement.Placed, Is.True);
            Assert.That(placement.MergeResult.Groups, Has.Count.EqualTo(1));
            Assert.That(notifiedPositions, Does.Contain(first.Position));
            Assert.That(notifiedPositions, Does.Contain(second.Position));
            Assert.That(notifiedPositions, Does.Contain(anchor.Position));
            Assert.That(clearedCellReceipts, Is.EqualTo(2));
            Assert.That(boardChanges, Is.EqualTo(1));
            Assert.That(mergeReceipts, Is.EqualTo(1));
            Assert.That(first.ContentId, Is.EqualTo(board.EmptyContentId));
            Assert.That(first.IsOccupied, Is.False);
            Assert.That(second.ContentId, Is.EqualTo(board.EmptyContentId));
            Assert.That(second.IsOccupied, Is.False);
            Assert.That(anchor.ContentId, Is.EqualTo(2));
            Assert.That(anchor.IsOccupied, Is.True);
        }
    }
}
