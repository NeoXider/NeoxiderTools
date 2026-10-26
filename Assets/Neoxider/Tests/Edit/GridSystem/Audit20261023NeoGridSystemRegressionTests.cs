using System.Collections.Generic;
using System.Linq;
using Neo.GridSystem;
using Neo.GridSystem.Match3;
using Neo.GridSystem.Merge;
using Neo.GridSystem.SlidingMerge;
using Neo.GridSystem.TicTacToe;
using Neo.Merge;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests.GridSystem
{
    [TestFixture]
    public sealed class Audit20261023NeoGridSystemRegressionTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private UnityEngine.Random.State _randomState;

        [SetUp]
        public void SetUp()
        {
            _randomState = UnityEngine.Random.state;
            UnityEngine.Random.InitState(20261023);
        }

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
            UnityEngine.Random.state = _randomState;
        }

        [TestCase(6, 4, 3)]
        [TestCase(1, 1, 1)]
        [TestCase(-1, 0, -2)]
        public void PendingResize_PreservesGeneratedCellsProjectionSlotsAndCenters(int x, int y, int z)
        {
            FieldGenerator field = CreateField(3, 2);
            FieldCell edge = field.GetCell(2, 1);
            Vector3 center = field.GetCellWorldCenter(edge.Position);
            field.Config.Size = new Vector3Int(x, y, z);

            Assert.That(field.GeneratedSize, Is.EqualTo(new Vector3Int(3, 2, 1)));
            Assert.That(field.GetCell(2, 1), Is.SameAs(edge));
            Assert.That(field.GetCell(3, 0), Is.Null);
            Assert.That(field.InBounds(edge.Position), Is.True);
            Assert.That(field.InBounds(new Vector3Int(3, 0, 0)), Is.False);
            Assert.That(field.GetAllCells().Count(), Is.EqualTo(6));
            FieldCell[,] projection = field.Cells2D;
            Assert.That(projection.GetLength(0), Is.EqualTo(3));
            Assert.That(projection.GetLength(1), Is.EqualTo(2));
            Assert.That(projection[2, 1], Is.SameAs(edge));
            Assert.That(field.GetCellWorldCenter(edge.Position), Is.EqualTo(center));

            GridSlotAllocator slots = new GridSlotAllocator(field);
            Assert.That(slots.Capacity, Is.EqualTo(6));
            Assert.That(slots.TryGetSlotPosition(5, out Vector3Int position), Is.True);
            Assert.That(position, Is.EqualTo(edge.Position));
            Assert.That(slots.TryGetSlotIndex(position, out int index), Is.True);
            Assert.That(index, Is.EqualTo(5));
        }

        [Test]
        public void CellEnumeration_KeepsItsArrayWhenTheFieldIsRegeneratedBetweenSteps()
        {
            FieldGenerator field = CreateField(3, 2);
            FieldCell[] original = field.GetAllCells().ToArray();
            List<FieldCell> enumerated = new List<FieldCell>();
            using (IEnumerator<FieldCell> iterator = field.GetAllCells().GetEnumerator())
            {
                Assert.That(iterator.MoveNext(), Is.True);
                enumerated.Add(iterator.Current);
                field.GenerateField(new FieldGeneratorConfig(Vector3Int.one));
                while (iterator.MoveNext())
                {
                    enumerated.Add(iterator.Current);
                }
            }

            Assert.That(enumerated, Is.EqualTo(original));
            Assert.That(field.GetAllCells().Count(), Is.EqualTo(1));
            Assert.That(field.GetCell(0, 0), Is.Not.SameAs(original[0]));
        }

        [Test]
        public void Regeneration_AppliesPendingSizeAndNormalizesZeroDepth()
        {
            FieldGenerator field = CreateField(3, 2);
            FieldCell previous = field.GetCell(0, 0);
            field.Config.Size = new Vector3Int(2, 3, 0);

            field.GenerateField();

            Assert.That(field.GeneratedSize, Is.EqualTo(new Vector3Int(2, 3, 1)));
            Assert.That(field.Config.Size, Is.EqualTo(field.GeneratedSize));
            Assert.That(field.GetCell(1, 2), Is.Not.Null);
            Assert.That(field.GetCell(2, 1), Is.Null);
            Assert.That(field.GetCell(0, 0), Is.Not.SameAs(previous));
        }

        [Test]
        public void GenerateField_WithNullConfig_RestoresUsableDefaultConfiguration()
        {
            FieldGenerator field = CreateField(1, 1);
            field.Config = null;

            field.GenerateField();

            Assert.That(field.Config, Is.Not.Null);
            Assert.That(field.GeneratedSize, Is.EqualTo(field.Config.Size));
            Assert.That(field.GetAllCells().Count(), Is.EqualTo(field.Cells.Length));
            Assert.That(field.GetCell(0, 0), Is.SameAs(field.Cells[0, 0, 0]));
            Assert.That(field.GetNeighbors(Vector3Int.zero), Is.Not.Empty);
        }

        [TestCase(CellPassabilityMode.WalkableOnly, true)]
        [TestCase(CellPassabilityMode.WalkableEnabledAndUnoccupied, false)]
        public void Pathfinding_UsesConfiguredOccupancyPolicy(CellPassabilityMode mode, bool expectedPath)
        {
            FieldGenerator field = CreateField(3, 1);
            field.Config.PassabilityMode = mode;
            field.SetOccupied(Vector3Int.right, true);

            GridPathResult result = field.FindPathDetailed(Vector3Int.zero, new Vector3Int(2, 0, 0));

            Assert.That(field.IsCellPassable(field.GetCell(1, 0)), Is.EqualTo(expectedPath));
            Assert.That(result.HasPath, Is.EqualTo(expectedPath));
            Assert.That(result.Path.Count, Is.EqualTo(expectedPath ? 3 : 0));
            Assert.That(result.Reason, Is.EqualTo(expectedPath ? NoPathReason.None : NoPathReason.NoPathFound));
        }

        [Test]
        public void Pathfinding_PreservesEndpointRulesIgnoreFlagsAndCustomPredicate()
        {
            FieldGenerator field = CreateField(3, 1);
            field.SetOccupied(new Vector3Int(2, 0, 0), true);
            GridPathRequest request = new GridPathRequest { Start = Vector3Int.zero, End = new Vector3Int(2, 0, 0) };

            Assert.That(field.FindPathDetailed(request).Reason, Is.EqualTo(NoPathReason.EndNotPassable));
            request.IgnoreOccupied = true;
            Assert.That(field.FindPathDetailed(request).Path.Count, Is.EqualTo(3));
            request.CustomPassabilityPredicate = cell => cell.Position.x != 1;
            Assert.That(field.FindPathDetailed(request).Reason, Is.EqualTo(NoPathReason.NoPathFound));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void NullCellEndpoints_DoNotBecomeAnOriginPath(int nullEndpoint)
        {
            FieldGenerator field = CreateField(3, 1);
            FieldCell start = nullEndpoint == 0 || nullEndpoint == 2 ? null : field.GetCell(0, 0);
            FieldCell end = nullEndpoint == 1 || nullEndpoint == 2 ? null : field.GetCell(2, 0);

            Assert.That(field.FindPath(start, end), Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DuplicateFootprint_IsRejectedWithoutWritesOrNotifications(bool useService)
        {
            FieldGenerator field = CreateField(2, 1);
            GridPlacementEntry[] entries =
            {
                new GridPlacementEntry(Vector3Int.zero, 7),
                new GridPlacementEntry(Vector3Int.right, 8),
                new GridPlacementEntry(Vector3Int.right, 9)
            };
            int notifications = 0;
            field.OnCellStateChanged.AddListener(cell => notifications++);
            GridPlacementService service = new GridPlacementService(field);
            GridPlacementRequest request = new GridPlacementRequest { Entries = entries };

            Assert.That(useService ? service.CanPlace(request) : field.CanPlaceContentFootprint(Vector3Int.zero, entries),
                Is.False);
            GridPlacementResult result = useService ? service.Place(request) : field.PlaceContentFootprint(Vector3Int.zero, entries);

            Assert.That(result.Placed, Is.False);
            Assert.That(result.FailureReason, Is.Not.Empty);
            Assert.That(result.Cells, Is.Empty);
            Assert.That(result.Positions, Is.Empty);
            Assert.That(notifications, Is.Zero);
            Assert.That(field.GetAllCells().All(cell => cell.ContentId == -1 && !cell.IsOccupied), Is.True);
        }

        [TestCase(false, true)]
        [TestCase(true, true)]
        [TestCase(false, false)]
        [TestCase(true, false)]
        public void FootprintNotifications_ObserveTheEntireCommittedPlacement(bool useService, bool notify)
        {
            FieldGenerator field = CreateField(2, 1);
            GridPlacementEntry[] entries =
            {
                new GridPlacementEntry(Vector3Int.zero, 7),
                new GridPlacementEntry(Vector3Int.right, 8, false)
            };
            List<int[]> observedContent = new List<int[]>();
            List<bool[]> observedOccupancy = new List<bool[]>();
            field.OnCellStateChanged.AddListener(cell =>
            {
                observedContent.Add(new[] { field.GetCell(0, 0).ContentId, field.GetCell(1, 0).ContentId });
                observedOccupancy.Add(new[] { field.GetCell(0, 0).IsOccupied, field.GetCell(1, 0).IsOccupied });
            });

            GridPlacementResult result = useService
                ? new GridPlacementService(field).Place(new GridPlacementRequest { Entries = entries, Notify = notify })
                : field.PlaceContentFootprint(Vector3Int.zero, entries, notify: notify);

            Assert.That(result.Placed, Is.True);
            Assert.That(result.Positions, Is.EqualTo(new[] { Vector3Int.zero, Vector3Int.right }));
            Assert.That(field.GetCell(0, 0).ContentId, Is.EqualTo(7));
            Assert.That(field.GetCell(1, 0).ContentId, Is.EqualTo(8));
            Assert.That(observedContent.Count, Is.EqualTo(notify ? 2 : 0));
            foreach (int[] content in observedContent)
            {
                Assert.That(content, Is.EqualTo(new[] { 7, 8 }));
            }

            foreach (bool[] occupancy in observedOccupancy)
            {
                Assert.That(occupancy, Is.EqualTo(new[] { true, false }));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Merge_WholeBoardScanHonorsDisabledCellOptInAndPreview(bool mutate)
        {
            FieldGenerator field = CreateField(3, 1);
            foreach (FieldCell cell in field.GetAllCells())
            {
                cell.IsEnabled = false;
                cell.IsWalkable = false;
                cell.ContentId = 4;
            }

            GridMergeRequest request = new GridMergeRequest
            {
                RequireWalkable = false,
                CascadeMode = MergeCascadeMode.None,
                Mutate = mutate
            };
            Assert.That(GridMergeResolver.Resolve(field, request).Groups, Is.Empty);
            request.RequireEnabled = false;
            int notifications = 0;
            field.OnCellStateChanged.AddListener(cell => notifications++);

            GridMergeResult result = GridMergeResolver.Resolve(field, request);

            Assert.That(result.Groups.Count, Is.EqualTo(1));
            Assert.That(result.Groups[0].SourceContentId, Is.EqualTo(4));
            Assert.That(result.Groups[0].ResultContentId, Is.EqualTo(5));
            Assert.That(result.ChangedPositions.Count, Is.EqualTo(3));
            Assert.That(field.GetAllCells().Select(cell => cell.ContentId).ToArray(),
                Is.EqualTo(mutate ? new[] { 5, 0, 0 } : new[] { 4, 4, 4 }));
            Assert.That(notifications, Is.EqualTo(mutate ? 3 : 0));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Match3_SearchAndResolveHonorsDirectedRulesAndGeneratedDimensions(bool pendingResize)
        {
            FieldGenerator field = CreateField(3, 2);
            field.Config.MovementRule = new MovementRule(new[] { Vector3Int.down });
            Match3BoardService service = field.gameObject.AddComponent<Match3BoardService>();
            int[] initial = { 1, 3, 1, 2, 1, 2 };
            for (int i = 0; i < initial.Length; i++)
            {
                field.GetCell(i % 3, i / 3).ContentId = initial[i];
            }

            if (pendingResize)
            {
                field.Config.Size = Vector3Int.one;
            }

            Assert.That(service.FindMatches(), Is.Empty);
            Assert.That(service.TryFindValidSwap(out Vector3Int a, out Vector3Int b), Is.True);
            Assert.That(a, Is.EqualTo(new Vector3Int(1, 1, 0)));
            Assert.That(b, Is.EqualTo(Vector3Int.right));
            for (int i = 0; i < initial.Length; i++)
            {
                Assert.That(field.GetCell(i % 3, i / 3).ContentId, Is.EqualTo(initial[i]));
            }

            int cleared = 0;
            service.OnMatchesResolved.AddListener(count => cleared += count);
            Assert.That(service.TrySwapAndResolve(a, b), Is.True);
            Assert.That(cleared, Is.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void FailedMatch3Shuffle_RestoresCustomContentAndDoesNotPublishAChange()
        {
            FieldGenerator field = CreateField(2, 1);
            field.GetCell(0, 0).ContentId = 11;
            field.GetCell(1, 0).ContentId = 12;
            Match3BoardService service = field.gameObject.AddComponent<Match3BoardService>();
            int notifications = 0;
            service.OnBoardChanged.AddListener(() => notifications++);
            service.OnBoardShuffled.AddListener(() => notifications++);

            Assert.That(service.ShuffleIfNoMoves(), Is.False);

            Assert.That(field.GetCell(0, 0).ContentId, Is.EqualTo(11));
            Assert.That(field.GetCell(1, 0).ContentId, Is.EqualTo(12));
            Assert.That(notifications, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MergeNotifications_ObserveTheFinalGroupAndHonorSuppression(bool notify)
        {
            FieldGenerator field = CreateField(3, 1);
            foreach (FieldCell cell in field.GetAllCells())
            {
                cell.ContentId = 4;
            }

            List<int[]> snapshots = new List<int[]>();
            field.OnCellStateChanged.AddListener(cell =>
                snapshots.Add(field.GetAllCells().Select(item => item.ContentId).ToArray()));

            GridMergeResult result = GridMergeResolver.Resolve(field, new GridMergeRequest
            {
                CascadeMode = MergeCascadeMode.None,
                NotifyOnContentChanged = notify
            });

            Assert.That(result.Groups.Count, Is.EqualTo(1));
            Assert.That(result.ChangedCells.Count, Is.EqualTo(3));
            Assert.That(snapshots.Count, Is.EqualTo(notify ? 3 : 0));
            foreach (int[] snapshot in snapshots)
            {
                Assert.That(snapshot, Is.EqualTo(new[] { 5, 0, 0 }));
            }
        }

        [Test]
        public void MatchFinder_UsesGeneratedDimensionsWhileResizeIsPending()
        {
            FieldGenerator field = CreateField(3, 1);
            foreach (FieldCell cell in field.GetAllCells())
            {
                cell.ContentId = 1;
            }

            field.Config.Size = Vector3Int.one;

            List<List<FieldCell>> matches = Match3MatchFinder.FindMatches(field);

            Assert.That(matches.Count, Is.EqualTo(1));
            Assert.That(matches[0].Select(cell => cell.Position).ToArray(),
                Is.EqualTo(new[] { Vector3Int.zero, Vector3Int.right, new Vector3Int(2, 0, 0) }));
        }

        [TestCase(0)]
        [TestCase(-3)]
        public void MatchFinder_NonPositiveThresholdNeverProducesEmptyMatchGroups(int minMatchLength)
        {
            FieldGenerator field = CreateField(2, 1);

            Assert.That(Match3MatchFinder.FindMatches(field, minMatchLength), Is.Empty);
            field.GetCell(0, 0).ContentId = 1;
            List<List<FieldCell>> matches = Match3MatchFinder.FindMatches(field, minMatchLength);
            Assert.That(matches.Count, Is.EqualTo(2));
            Assert.That(matches.All(group => group.Count == 1 && group[0] == field.GetCell(0, 0)), Is.True);
        }

        [Test]
        public void SlidingMerge_UsesGeneratedDimensionsWhileResizeIsPending()
        {
            FieldGenerator field = CreateField(3, 1);
            field.GetCell(0, 0).ContentId = 2;
            field.GetCell(1, 0).ContentId = 0;
            field.GetCell(2, 0).ContentId = 2;
            field.Config.Size = Vector3Int.one;

            SlidingMergeResult result = SlidingMergeResolver.Slide(field, SlidingMergeDirection.Left);

            Assert.That(result.MergeCount, Is.EqualTo(1));
            Assert.That(result.ScoreDelta, Is.EqualTo(4));
            Assert.That(field.GetAllCells().Select(cell => cell.ContentId).ToArray(), Is.EqualTo(new[] { 4, 0, 0 }));
        }

        [Test]
        public void TicTacToe_ZeroAxesAreIgnoredAndValidAxesStillDetectTheWinner()
        {
            FieldGenerator field = CreateField(3, 1);
            field.GetCell(0, 0).ContentId = (int)TicTacToeCellState.PlayerX;
            Vector3Int[] axes = { Vector3Int.zero, Vector3Int.right };

            Assert.That(TicTacToeWinChecker.GetWinner(field, axes), Is.EqualTo(TicTacToeCellState.Empty));
            field.GetCell(1, 0).ContentId = (int)TicTacToeCellState.PlayerX;
            field.GetCell(2, 0).ContentId = (int)TicTacToeCellState.PlayerX;
            Assert.That(TicTacToeWinChecker.GetWinner(field, new[] { Vector3Int.zero }), Is.EqualTo(TicTacToeCellState.Empty));
            Assert.That(TicTacToeWinChecker.GetWinner(field, axes), Is.EqualTo(TicTacToeCellState.PlayerX));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TicTacToe_BlockersArePreservedAndDoNotPreventTheDraw(bool occupied)
        {
            FieldGenerator field = CreateField(3, 1);
            FieldCell blocker = field.GetCell(2, 0);
            blocker.IsOccupied = occupied;
            blocker.IsWalkable = occupied;
            blocker.ContentId = 42;
            TicTacToeBoardService service = field.gameObject.AddComponent<TicTacToeBoardService>();
            service.ResetBoard();
            int draws = 0;
            service.OnDrawDetected.AddListener(() => draws++);

            Assert.That(blocker.ContentId, Is.EqualTo(42));
            Assert.That(service.TryMakeMove(blocker.Position), Is.False);
            Assert.That(service.TryMakeMove(Vector3Int.zero), Is.True);
            Assert.That(service.IsFinished, Is.False);
            Assert.That(service.TryMakeMove(Vector3Int.right), Is.True);

            Assert.That(field.GetCell(0, 0).ContentId, Is.EqualTo((int)TicTacToeCellState.PlayerX));
            Assert.That(field.GetCell(1, 0).ContentId, Is.EqualTo((int)TicTacToeCellState.PlayerO));
            Assert.That(service.IsBoardFull(), Is.True);
            Assert.That(service.IsFinished, Is.True);
            Assert.That(service.Winner, Is.EqualTo(TicTacToeCellState.Empty));
            Assert.That(draws, Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TicTacToe_BlockersBreakWinLines(bool occupied)
        {
            FieldGenerator field = CreateField(3, 1);
            foreach (FieldCell cell in field.GetAllCells())
            {
                cell.ContentId = (int)TicTacToeCellState.PlayerX;
            }

            FieldCell blocker = field.GetCell(1, 0);
            blocker.IsOccupied = occupied;
            blocker.IsWalkable = occupied;

            Assert.That(TicTacToeWinChecker.GetWinner(field), Is.EqualTo(TicTacToeCellState.Empty));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Spawners_NullPrefabReturnsFailureWithoutObjectsStateChangesOrEvents(bool tracked)
        {
            FieldGenerator field = CreateField(1, 1);
            int notifications = 0;
            if (tracked)
            {
                FieldObjectSpawner spawner = field.gameObject.AddComponent<FieldObjectSpawner>();
                spawner.Prefabs = new GameObject[] { null };
                spawner.OnObjectSpawned.AddListener(info => notifications++);
                Assert.That(spawner.SpawnAt(Vector3Int.zero), Is.Null);
                Assert.That(spawner.GetObjectsInCell(Vector3Int.zero), Is.Empty);
                Assert.That(spawner.GetAllObjects(), Is.Empty);
                Assert.That(spawner.IsCellOccupied(Vector3Int.zero), Is.False);
            }
            else
            {
                FieldSpawner spawner = field.gameObject.AddComponent<FieldSpawner>();
                spawner.Prefabs = new GameObject[] { null };
                spawner.OnObjectSpawned.AddListener((go, cell) => notifications++);
                Assert.That(spawner.SpawnAt(Vector3Int.zero), Is.Null);
            }

            Assert.That(notifications, Is.Zero);
            Assert.That(field.transform.childCount, Is.Zero);
            Assert.That(field.GetCell(0, 0).IsOccupied, Is.False);
            Assert.That(field.GetCell(0, 0).ContentId, Is.EqualTo(-1));
        }

        [Test]
        public void ObjectSpawner_DirectPublicCallsTrackTheSpawnAndOccupancy()
        {
            FieldGenerator field = CreateField(2, 1);
            FieldObjectSpawner spawner = field.gameObject.AddComponent<FieldObjectSpawner>();
            spawner.Prefabs = new[] { CreateObject("TrackedTemplate") };

            SpawnedObjectInfo info = spawner.SpawnAt(Vector3Int.right);

            Assert.That(info, Is.Not.Null);
            Assert.That(info.GameObject.transform.parent, Is.SameAs(field.transform));
            Assert.That(info.GameObject.transform.position, Is.EqualTo(field.GetCellWorldCenter(Vector3Int.right)));
            Assert.That(spawner.GetObjectsInCell(Vector3Int.right), Is.EqualTo(new[] { info }));
            Assert.That(spawner.GetAllObjects(), Is.EqualTo(new[] { info }));
            Assert.That(field.GetCell(1, 0).IsOccupied, Is.True);
            Assert.That(spawner.IsCellOccupied(Vector3Int.right), Is.True);
        }

        [Test]
        public void FieldSpawner_WalkableSpawnUsesGeneratedCellsAndSkipsDisabledCells()
        {
            FieldGenerator field = CreateField(3, 1);
            field.SetEnabled(Vector3Int.right, false);
            field.Config.Size = new Vector3Int(5, 2, 1);
            FieldSpawner spawner = field.gameObject.AddComponent<FieldSpawner>();
            spawner.Prefabs = new[] { CreateObject("WalkableTemplate") };
            List<Vector3Int> positions = new List<Vector3Int>();
            spawner.OnObjectSpawned.AddListener((go, cell) => positions.Add(cell.Position));

            spawner.SpawnOnAllWalkable();

            Assert.That(positions, Is.EqualTo(new[] { Vector3Int.zero, new Vector3Int(2, 0, 0) }));
            Assert.That(field.transform.childCount, Is.EqualTo(2));
        }

        private FieldGenerator CreateField(int width, int height)
        {
            GameObject owner = CreateObject("Audit20261023Grid");
            FieldGenerator field = owner.AddComponent<FieldGenerator>();
            field.GenerateField(new FieldGeneratorConfig(new Vector3Int(width, height, 1)));
            return field;
        }

        private GameObject CreateObject(string name)
        {
            GameObject owner = new GameObject(name);
            _objects.Add(owner);
            return owner;
        }
    }
}
