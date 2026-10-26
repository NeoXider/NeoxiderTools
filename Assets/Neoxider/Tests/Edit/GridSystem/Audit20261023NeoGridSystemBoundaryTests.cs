using System.Collections.Generic;
using System.Linq;
using Neo.GridSystem;
using Neo.GridSystem.Dice;
using Neo.GridSystem.Merge;
using Neo.Merge;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests.GridSystem
{
    [TestFixture]
    public sealed class Audit20261023NeoGridSystemBoundaryTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                {
                    Object.DestroyImmediate(_objects[i]);
                }
            }

            _objects.Clear();
        }

        [TestCase(false, 0)]
        [TestCase(false, 1)]
        [TestCase(false, 2)]
        [TestCase(true, 0)]
        [TestCase(true, 1)]
        [TestCase(true, 2)]
        public void FootprintOverflow_CannotWrapToTheOrigin(bool useService, int axis)
        {
            FieldGenerator field = CreateField(Vector3Int.one);
            Vector3Int anchor = Vector3Int.zero;
            anchor[axis] = int.MinValue;
            GridPlacementEntry[] entries = { new GridPlacementEntry(anchor, 7) };
            GridPlacementRequest request = new GridPlacementRequest { Anchor = anchor, Entries = entries };
            GridPlacementService service = new GridPlacementService(field);
            int notifications = 0;
            field.OnCellStateChanged.AddListener(cell => notifications++);

            Assert.That(useService ? service.CanPlace(request) : field.CanPlaceContentFootprint(anchor, entries), Is.False);
            GridPlacementResult result = useService ? service.Place(request) : field.PlaceContentFootprint(anchor, entries);

            AssertRejectedPlacement(field, result, notifications);
        }

        [TestCase(false, 0)]
        [TestCase(false, 1)]
        [TestCase(false, 2)]
        [TestCase(true, 0)]
        [TestCase(true, 1)]
        [TestCase(true, 2)]
        public void LateFootprintOverflow_LeavesEarlierValidEntriesUntouched(bool useService, int axis)
        {
            FieldGenerator field = CreateField(new Vector3Int(2, 2, 2));
            Vector3Int anchor = Vector3Int.zero;
            Vector3Int validOffset = Vector3Int.zero;
            Vector3Int invalidOffset = Vector3Int.zero;
            anchor[axis] = -int.MaxValue;
            validOffset[axis] = int.MaxValue;
            invalidOffset[axis] = int.MinValue;
            GridPlacementEntry[] entries =
            {
                new GridPlacementEntry(validOffset, 7),
                new GridPlacementEntry(invalidOffset, 8)
            };
            GridPlacementRequest request = new GridPlacementRequest { Anchor = anchor, Entries = entries };
            GridPlacementService service = new GridPlacementService(field);
            int notifications = 0;
            field.OnCellStateChanged.AddListener(cell => notifications++);

            Assert.That(useService ? service.CanPlace(request) : field.CanPlaceContentFootprint(anchor, entries), Is.False);
            GridPlacementResult result = useService ? service.Place(request) : field.PlaceContentFootprint(anchor, entries);

            AssertRejectedPlacement(field, result, notifications);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void NegativeFootprintOffsets_StillPlaceAtTheirResolvedCells(bool useService)
        {
            FieldGenerator field = CreateField(new Vector3Int(2, 2, 2));
            Vector3Int anchor = Vector3Int.one;
            GridPlacementEntry[] entries =
            {
                new GridPlacementEntry(new Vector3Int(-1, -1, -1), 7),
                new GridPlacementEntry(Vector3Int.zero, 8, false)
            };
            GridPlacementRequest request = new GridPlacementRequest { Anchor = anchor, Entries = entries };
            GridPlacementService service = new GridPlacementService(field);

            Assert.That(useService ? service.CanPlace(request) : field.CanPlaceContentFootprint(anchor, entries), Is.True);
            GridPlacementResult result = useService ? service.Place(request) : field.PlaceContentFootprint(anchor, entries);

            Assert.That(result.Placed, Is.True);
            Assert.That(result.Positions, Is.EqualTo(new[] { Vector3Int.zero, Vector3Int.one }));
            Assert.That(field.GetCell(Vector3Int.zero).ContentId, Is.EqualTo(7));
            Assert.That(field.GetCell(Vector3Int.zero).IsOccupied, Is.True);
            Assert.That(field.GetCell(Vector3Int.one).ContentId, Is.EqualTo(8));
            Assert.That(field.GetCell(Vector3Int.one).IsOccupied, Is.False);
        }

        [TestCase(false, 0)]
        [TestCase(false, 1)]
        [TestCase(false, 2)]
        [TestCase(true, 0)]
        [TestCase(true, 1)]
        [TestCase(true, 2)]
        public void ExtremeAnchor_IsAcceptedWhenItsOffsetResolvesIntoTheBoard(bool useService, int axis)
        {
            FieldGenerator field = CreateField(Vector3Int.one);
            Vector3Int anchor = Vector3Int.zero;
            Vector3Int offset = Vector3Int.zero;
            anchor[axis] = int.MaxValue;
            offset[axis] = -int.MaxValue;
            GridPlacementEntry[] entries = { new GridPlacementEntry(offset, 7) };
            GridPlacementRequest request = new GridPlacementRequest { Anchor = anchor, Entries = entries };
            GridPlacementService service = new GridPlacementService(field);

            Assert.That(useService ? service.CanPlace(request) : field.CanPlaceContentFootprint(anchor, entries), Is.True);
            GridPlacementResult result = useService ? service.Place(request) : field.PlaceContentFootprint(anchor, entries);

            Assert.That(result.Placed, Is.True);
            Assert.That(result.Positions, Is.EqualTo(new[] { Vector3Int.zero }));
            Assert.That(field.GetCell(Vector3Int.zero).ContentId, Is.EqualTo(7));
            Assert.That(field.GetCell(Vector3Int.zero).IsOccupied, Is.True);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void DicePlacement_RejectsOverflowWithoutBoardOrCellNotifications(int axis)
        {
            FieldGenerator field = CreateField(Vector3Int.one);
            Vector3Int anchor = Vector3Int.zero;
            anchor[axis] = int.MinValue;
            DicePiece piece = new DicePiece(new[] { new DicePieceCell(anchor, 5) });
            DiceBoard board = new DiceBoard(field);
            int notifications = 0;
            field.OnCellStateChanged.AddListener(cell => notifications++);
            board.BoardChanged += () => notifications++;
            board.MergesResolved += result => notifications++;

            Assert.That(board.CanPlace(piece, anchor), Is.False);
            DicePlacementResult placement = board.Place(piece, anchor, false);

            Assert.That(placement.Placed, Is.False);
            Assert.That(placement.PlacedPositions, Is.Empty);
            Assert.That(field.GetCell(Vector3Int.zero).ContentId, Is.EqualTo(-1));
            Assert.That(field.GetCell(Vector3Int.zero).IsOccupied, Is.False);
            Assert.That(notifications, Is.Zero);
        }

        [Test]
        public void DicePlacement_StillAcceptsAValidNegativeOffset()
        {
            FieldGenerator field = CreateField(new Vector3Int(2, 1, 1));
            DiceBoard board = new DiceBoard(field);
            DicePiece piece = new DicePiece(new[] { new DicePieceCell(Vector3Int.left, 5) });

            Assert.That(board.CanPlace(piece, Vector3Int.right), Is.True);
            DicePlacementResult placement = board.Place(piece, Vector3Int.right, false);

            Assert.That(placement.Placed, Is.True);
            Assert.That(placement.PlacedPositions, Is.EqualTo(new[] { Vector3Int.zero }));
            Assert.That(field.GetCell(Vector3Int.zero).ContentId, Is.EqualTo(5));
            Assert.That(field.GetCell(Vector3Int.zero).IsOccupied, Is.True);
        }

        [TestCase(int.MaxValue, 1, int.MaxValue)]
        [TestCase(int.MaxValue, int.MaxValue, int.MaxValue)]
        [TestCase(int.MinValue, -1, int.MinValue)]
        [TestCase(int.MinValue, int.MinValue, int.MinValue)]
        [TestCase(1, int.MinValue, int.MinValue + 1)]
        [TestCase(int.MinValue, int.MaxValue, -1)]
        [TestCase(-10, 3, -7)]
        public void MergeIncrement_SaturatesSignedRangeBeforePublishingFinalState(int value, int step, int expected)
        {
            FieldGenerator field = CreateUniformMergeField(value);
            GridMergeRequest request = GridMergeRequest.Increment(new[] { Vector3Int.zero }, 0, 2, step);
            request.CascadeMode = MergeCascadeMode.None;
            List<int[]> snapshots = new List<int[]>();
            field.OnCellStateChanged.AddListener(cell =>
                snapshots.Add(field.GetAllCells().Select(item => item.ContentId).ToArray()));

            GridMergeResult result = GridMergeResolver.Resolve(field, request);

            Assert.That(result.Groups.Count, Is.EqualTo(1));
            Assert.That(result.Groups[0].SourceContentId, Is.EqualTo(value));
            Assert.That(result.Groups[0].ResultContentId, Is.EqualTo(expected));
            Assert.That(result.ChangedCells.Count, Is.EqualTo(2));
            Assert.That(snapshots.Count, Is.EqualTo(2));
            Assert.That(field.GetAllCells().Select(cell => cell.ContentId).ToArray(), Is.EqualTo(new[] { expected, 0 }));
            foreach (int[] snapshot in snapshots)
            {
                Assert.That(snapshot, Is.EqualTo(new[] { expected, 0 }));
            }
        }

        [TestCase(int.MaxValue, int.MaxValue)]
        [TestCase(int.MinValue, int.MinValue + 1)]
        [TestCase(4, 5)]
        public void DefaultMergeIncrement_SaturatesAndPreservesOrdinarySignedValues(int value, int expected)
        {
            FieldGenerator field = CreateUniformMergeField(value);
            GridMergeRequest request = new GridMergeRequest
            {
                Seeds = new[] { Vector3Int.zero },
                MinGroupSize = 2
            };

            GridMergeResult result = GridMergeResolver.Resolve(field, request);

            Assert.That(result.Groups.Count, Is.EqualTo(1));
            Assert.That(result.Groups[0].ResultContentId, Is.EqualTo(expected));
            Assert.That(result.CascadeLimitReached, Is.False);
            Assert.That(field.GetAllCells().Select(cell => cell.ContentId).ToArray(), Is.EqualTo(new[] { expected, 0 }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SaturatingMergePreview_DoesNotMutateOrNotify(bool useFactory)
        {
            FieldGenerator field = CreateUniformMergeField(int.MaxValue);
            GridMergeRequest request = useFactory
                ? GridMergeRequest.Increment(new[] { Vector3Int.zero }, 0, 2, int.MaxValue)
                : new GridMergeRequest { Seeds = new[] { Vector3Int.zero }, MinGroupSize = 2 };
            request.Mutate = false;
            int notifications = 0;
            field.OnCellStateChanged.AddListener(cell => notifications++);

            GridMergeResult result = GridMergeResolver.Resolve(field, request);

            Assert.That(result.Groups.Count, Is.EqualTo(1));
            Assert.That(result.Groups[0].ResultContentId, Is.EqualTo(int.MaxValue));
            Assert.That(result.CascadeLimitReached, Is.False);
            Assert.That(field.GetAllCells().All(cell => cell.ContentId == int.MaxValue), Is.True);
            Assert.That(notifications, Is.Zero);
        }

        [TestCase(int.MaxValue, 1)]
        [TestCase(int.MinValue, -1)]
        public void SaturatingMerge_ClearsItsGroupAndTerminatesTheCascade(int value, int step)
        {
            FieldGenerator field = CreateUniformMergeField(value);
            GridMergeRequest request = GridMergeRequest.Increment(new[] { Vector3Int.zero }, 0, 2, step);

            GridMergeResult result = GridMergeResolver.Resolve(field, request);

            Assert.That(result.Groups.Count, Is.EqualTo(1));
            Assert.That(result.Groups[0].ClearedCells, Is.EqualTo(new[] { field.GetCell(Vector3Int.right) }));
            Assert.That(result.CascadeLimitReached, Is.False);
            Assert.That(field.GetAllCells().Select(cell => cell.ContentId).ToArray(), Is.EqualTo(new[] { value, 0 }));
        }

        [Test]
        public void MergeIncrement_OrdinaryCascadeStillResolvesBothGroups()
        {
            FieldGenerator field = CreateField(new Vector3Int(3, 1, 1));
            field.GetCell(0, 0).ContentId = 2;
            field.GetCell(1, 0).ContentId = 1;
            field.GetCell(2, 0).ContentId = 1;
            GridMergeRequest request = GridMergeRequest.Increment(new[] { Vector3Int.right }, 0, 2);

            GridMergeResult result = GridMergeResolver.Resolve(field, request);

            Assert.That(result.Groups.Select(group => group.ResultContentId).ToArray(), Is.EqualTo(new[] { 2, 3 }));
            Assert.That(result.CascadeLimitReached, Is.False);
            Assert.That(field.GetAllCells().Select(cell => cell.ContentId).ToArray(), Is.EqualTo(new[] { 0, 3, 0 }));
        }

        private static void AssertRejectedPlacement(FieldGenerator field, GridPlacementResult result, int notifications)
        {
            Assert.That(result.Placed, Is.False);
            Assert.That(result.FailureReason, Is.Not.Empty);
            Assert.That(result.Cells, Is.Empty);
            Assert.That(result.Positions, Is.Empty);
            Assert.That(field.GetAllCells().All(cell => cell.ContentId == -1 && !cell.IsOccupied), Is.True);
            Assert.That(notifications, Is.Zero);
        }

        private FieldGenerator CreateUniformMergeField(int value)
        {
            FieldGenerator field = CreateField(new Vector3Int(2, 1, 1));
            foreach (FieldCell cell in field.GetAllCells())
            {
                cell.ContentId = value;
            }

            return field;
        }

        private FieldGenerator CreateField(Vector3Int size)
        {
            GameObject owner = new GameObject("Audit20261023GridBoundary");
            _objects.Add(owner);
            FieldGenerator field = owner.AddComponent<FieldGenerator>();
            field.GenerateField(new FieldGeneratorConfig(size));
            return field;
        }
    }
}
