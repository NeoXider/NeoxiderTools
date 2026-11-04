using System.Collections.Generic;
using Neo.GridSystem;
using Neo.GridSystem.Dice;
using Neo.GridSystem.Match3;
using Neo.GridSystem.SlidingMerge;
using Neo.GridSystem.TicTacToe;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.GridSystem
{
    /// <summary>
    ///     Regression and contract tests for the GridSystem rule layers: sliding merge, TicTacToe, Match3 search,
    ///     movement presets, shape masks and the object spawner registry.
    /// </summary>
    [TestFixture]
    public sealed class GridSystemGameplayRulesTests
    {
        private readonly List<Object> _created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created)
            {
                if (created != null)
                {
                    Object.DestroyImmediate(created);
                }
            }

            _created.Clear();
        }

        [Test]
        public void Slide_NotifiesOnlyAfterTheWholeBoardIsWritten()
        {
            FieldGenerator field = CreateField(4, 2);
            int[] values = { 2, 2, 2, 2, 4, 0, 4, 8 };
            WriteRowMajor(field, values);
            List<string> observedBoards = new List<string>();
            field.OnCellStateChanged.AddListener(cell => observedBoards.Add(Snapshot(field)));

            SlidingMergeResult result = SlidingMergeResolver.Slide(field, SlidingMergeDirection.Left);

            string finalBoard = Snapshot(field);
            Assert.That(finalBoard, Is.EqualTo("4,4,0,0|8,8,0,0"));
            Assert.That(result.Changed, Is.True);
            Assert.That(observedBoards.Count, Is.EqualTo(8));
            foreach (string observed in observedBoards)
            {
                Assert.That(observed, Is.EqualTo(finalBoard));
            }
        }

        [Test]
        public void Slide_ReportsBothSourcesOfAMerge()
        {
            FieldGenerator field = CreateField(4, 1);
            WriteRowMajor(field, new[] { 0, 2, 0, 2 });

            SlidingMergeResult result = SlidingMergeResolver.Slide(field, SlidingMergeDirection.Left);

            Assert.That(result.Steps.Count, Is.EqualTo(1));
            SlidingMergeStep step = result.Steps[0];
            Assert.That(step.IsMerge, Is.True);
            Assert.That(step.From, Is.EqualTo(new Vector3Int(1, 0, 0)));
            Assert.That(step.MergedFrom, Is.EqualTo(new Vector3Int(3, 0, 0)));
            Assert.That(step.To, Is.EqualTo(Vector3Int.zero));
            Assert.That(step.Value, Is.EqualTo(2));
            Assert.That(step.MergedValue, Is.EqualTo(2));
            Assert.That(step.ResultValue, Is.EqualTo(4));

            WriteRowMajor(field, new[] { 0, 0, 0, 8 });
            SlidingMergeStep move = SlidingMergeResolver.Slide(field, SlidingMergeDirection.Left).Steps[0];
            Assert.That(move.IsMerge, Is.False);
            Assert.That(move.MergedFrom, Is.EqualTo(move.From));
            Assert.That(move.MergedValue, Is.EqualTo(move.Value));
        }

        [Test]
        public void CanSlide_DoesNotWriteOrNotify()
        {
            FieldGenerator field = CreateField(3, 1);
            WriteRowMajor(field, new[] { 0, 2, 2 });
            int notifications = 0;
            field.OnCellStateChanged.AddListener(cell => notifications++);

            Assert.That(SlidingMergeResolver.CanSlide(field, SlidingMergeDirection.Left), Is.True);
            Assert.That(SlidingMergeResolver.CanSlide(field, SlidingMergeDirection.Right), Is.True);
            Assert.That(Snapshot(field), Is.EqualTo("0,2,2"));
            Assert.That(notifications, Is.EqualTo(0));
        }

        [Test]
        public void HasAnyMove_DetectsLockedBoards()
        {
            FieldGenerator field = CreateField(2, 2);
            WriteRowMajor(field, new[] { 2, 4, 4, 2 });
            Assert.That(SlidingMergeResolver.HasAnyMove(field), Is.False);

            WriteRowMajor(field, new[] { 2, 2, 4, 8 });
            Assert.That(SlidingMergeResolver.HasAnyMove(field), Is.True);

            WriteRowMajor(field, new[] { 2, 4, 4, 0 });
            Assert.That(SlidingMergeResolver.HasAnyMove(field), Is.True);
        }

        [Test]
        public void TrySpawnRandomContent_UsesTheInjectedIndexAndReportsTheCell()
        {
            FieldGenerator field = CreateField(3, 1);
            WriteRowMajor(field, new[] { 0, 4, 0 });
            int candidates = -1;

            bool spawned = SlidingMergeResolver.TrySpawnRandomContent(field, 2, out FieldCell cell, 0, true,
                count =>
                {
                    candidates = count;
                    return count - 1;
                });

            Assert.That(spawned, Is.True);
            Assert.That(candidates, Is.EqualTo(2));
            Assert.That(cell.Position, Is.EqualTo(new Vector3Int(2, 0, 0)));
            Assert.That(Snapshot(field), Is.EqualTo("0,4,2"));
        }

        [Test]
        public void SlidingMergeBoardService_ReportsSpawnSlideAndNoMovesLeft()
        {
            FieldGenerator field = CreateField(2, 1);
            WriteRowMajor(field, new[] { 0, 4 });
            SlidingMergeBoardService board = field.gameObject.AddComponent<SlidingMergeBoardService>();
            board.EmptyContentId = 0;
            board.DefaultSpawnContentId = 2;
            board.RandomIndexProvider = count => 0;
            List<Vector3Int> spawned = new List<Vector3Int>();
            int slid = 0;
            int noMoves = 0;
            board.OnContentSpawned.AddListener(spawned.Add);
            board.OnNoMovesLeft.AddListener(() => noMoves++);
            board.Slid += result => slid++;

            Assert.That(board.HasAnyMove(), Is.True);
            SlidingMergeResult slide = board.Slide(SlidingMergeDirection.Left);

            Assert.That(slide.Changed, Is.True);
            Assert.That(Snapshot(field), Is.EqualTo("4,2"));
            Assert.That(slid, Is.EqualTo(1));
            Assert.That(spawned, Is.EqualTo(new[] { new Vector3Int(1, 0, 0) }));
            Assert.That(noMoves, Is.EqualTo(1));
            Assert.That(board.HasAnyMove(), Is.False);
            Assert.That(board.Slide(SlidingMergeDirection.Right).Changed, Is.False);
            Assert.That(noMoves, Is.EqualTo(1));
        }

        [Test]
        public void TicTacToe_MovesAndResetNotifyTheGeneratorWithFinalState()
        {
            FieldGenerator field = CreateField(3, 3);
            TicTacToeBoardService service = field.gameObject.AddComponent<TicTacToeBoardService>();
            service.ResetBoard();
            List<Vector3Int> notified = new List<Vector3Int>();
            bool finishedSeenByWinningNotification = false;
            field.OnCellStateChanged.AddListener(cell =>
            {
                notified.Add(cell.Position);
                if (cell.Position == new Vector3Int(2, 0, 0))
                {
                    finishedSeenByWinningNotification = service.IsFinished &&
                                                        service.Winner == TicTacToeCellState.PlayerX &&
                                                        cell.ContentId == (int)TicTacToeCellState.PlayerX;
                }
            });

            service.TryMakeMove(new Vector2Int(0, 0));
            service.TryMakeMove(new Vector2Int(0, 1));
            service.TryMakeMove(new Vector2Int(1, 0));
            service.TryMakeMove(new Vector2Int(1, 1));
            service.TryMakeMove(new Vector2Int(2, 0));

            Assert.That(notified.Count, Is.EqualTo(5));
            Assert.That(finishedSeenByWinningNotification, Is.True);
            Assert.That(service.WinningLine, Is.EqualTo(new[]
            {
                new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(2, 0, 0)
            }));

            notified.Clear();
            service.ResetBoard();
            Assert.That(notified.Count, Is.EqualTo(5));
            Assert.That(service.WinningLine.Count, Is.EqualTo(0));
            foreach (FieldCell cell in field.GetAllCells())
            {
                Assert.That(cell.ContentId, Is.EqualTo((int)TicTacToeCellState.Empty));
            }
        }

        [Test]
        public void TicTacToe_ReplyFromACellListenerIsMadeForTheNextPlayer()
        {
            FieldGenerator field = CreateField(3, 3);
            TicTacToeBoardService service = field.gameObject.AddComponent<TicTacToeBoardService>();
            service.ResetBoard();
            List<int> playerEvents = new List<int>();
            service.OnPlayerChanged.AddListener(playerEvents.Add);
            TicTacToeCellState playerDuringNotification = TicTacToeCellState.Empty;
            bool replied = false;
            bool replyAccepted = false;
            field.OnCellStateChanged.AddListener(cell =>
            {
                if (replied)
                {
                    return;
                }

                replied = true;
                playerDuringNotification = service.CurrentPlayer;
                replyAccepted = service.TryMakeMove(new Vector2Int(1, 1));
            });

            Assert.That(service.TryMakeMove(new Vector2Int(0, 0)), Is.True);

            Assert.That(playerDuringNotification, Is.EqualTo(TicTacToeCellState.PlayerO));
            Assert.That(replyAccepted, Is.True);
            Assert.That(field.GetCell(0, 0).ContentId, Is.EqualTo((int)TicTacToeCellState.PlayerX));
            Assert.That(field.GetCell(1, 1).ContentId, Is.EqualTo((int)TicTacToeCellState.PlayerO));
            Assert.That(service.CurrentPlayer, Is.EqualTo(TicTacToeCellState.PlayerX));
            Assert.That(playerEvents, Is.EqualTo(new[] { (int)TicTacToeCellState.PlayerX }),
                "the outer move must not publish its stale player change");
        }

        [Test]
        public void TicTacToe_ResetFromACellListenerSuppressesTheStaleWinnerEvent()
        {
            FieldGenerator field = CreateField(3, 3);
            TicTacToeBoardService service = field.gameObject.AddComponent<TicTacToeBoardService>();
            service.ResetBoard();
            service.TryMakeMove(new Vector2Int(0, 0));
            service.TryMakeMove(new Vector2Int(0, 1));
            service.TryMakeMove(new Vector2Int(1, 0));
            service.TryMakeMove(new Vector2Int(1, 1));
            int winners = 0;
            int resets = 0;
            service.OnWinnerDetected.AddListener(winner => winners++);
            service.OnBoardReset.AddListener(() => resets++);
            field.OnCellStateChanged.AddListener(cell =>
            {
                if (service.IsFinished)
                {
                    service.ResetBoard();
                }
            });

            Assert.That(service.TryMakeMove(new Vector2Int(2, 0)), Is.True);

            Assert.That(resets, Is.EqualTo(1));
            Assert.That(winners, Is.EqualTo(0));
            Assert.That(service.IsFinished, Is.False);
            Assert.That(service.Winner, Is.EqualTo(TicTacToeCellState.Empty));
            Assert.That(service.CurrentPlayer, Is.EqualTo(TicTacToeCellState.PlayerX));
            Assert.That(service.WinningLine.Count, Is.EqualTo(0));
        }

        [Test]
        public void TicTacToe_ConfigurableWinLengthSupportsGomokuStyleBoards()
        {
            FieldGenerator field = CreateField(6, 6);
            Assert.That(TicTacToeWinChecker.ResolveWinLineLength(field), Is.EqualTo(3));
            for (int x = 0; x < 3; x++)
            {
                field.GetCell(x, 0).ContentId = (int)TicTacToeCellState.PlayerO;
            }

            Assert.That(TicTacToeWinChecker.GetWinner(field), Is.EqualTo(TicTacToeCellState.PlayerO));
            Assert.That(TicTacToeWinChecker.GetWinner(field, 4), Is.EqualTo(TicTacToeCellState.Empty));

            TicTacToeBoardService service = field.gameObject.AddComponent<TicTacToeBoardService>();
            service.WinLineLength = 4;
            service.ResetBoard();
            Vector2Int[] moves =
            {
                new(0, 0), new(0, 5), new(1, 1), new(1, 5), new(2, 2), new(2, 5), new(3, 3)
            };
            foreach (Vector2Int move in moves)
            {
                Assert.That(service.IsFinished, Is.False);
                Assert.That(service.TryMakeMove(move), Is.True);
            }

            Assert.That(service.IsFinished, Is.True);
            Assert.That(service.Winner, Is.EqualTo(TicTacToeCellState.PlayerX));
            Assert.That(service.WinningLine.Count, Is.EqualTo(4));
            Assert.That(service.WinningLine[0], Is.EqualTo(new Vector3Int(0, 0, 0)));
            Assert.That(service.WinningLine[3], Is.EqualTo(new Vector3Int(3, 3, 0)));
        }

        [Test]
        public void MatchFinder_ReportsRowsThenColumnsAndSplitsOnUnusableCells()
        {
            FieldGenerator field = CreateField(5, 3);
            int[] values =
            {
                1, 1, 1, 2, 2,
                3, 1, 2, 2, 2,
                3, 1, 4, 4, 5
            };
            WriteRowMajor(field, values);
            field.GetCell(4, 1).IsOccupied = true;

            List<List<FieldCell>> matches = Match3MatchFinder.FindMatches(field);

            Assert.That(matches.Count, Is.EqualTo(2));
            Assert.That(Positions(matches[0]), Is.EqualTo(new[]
            {
                new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(2, 0, 0)
            }));
            Assert.That(Positions(matches[1]), Is.EqualTo(new[]
            {
                new Vector3Int(1, 0, 0), new Vector3Int(1, 1, 0), new Vector3Int(1, 2, 0)
            }));
            Assert.That(Match3MatchFinder.FindMatches(field, 2).Count, Is.EqualTo(7));
        }

        [Test]
        public void MovementPreset_AppliesOnGenerationAndUseMovementRuleKeepsTheCodeRule()
        {
            GameObject owner = new GameObject("MovementPreset");
            _created.Add(owner);
            FieldGenerator field = owner.AddComponent<FieldGenerator>();
            FieldGeneratorConfig config = new FieldGeneratorConfig(new Vector3Int(3, 3, 1), MovementRule.HexDirectionsX);

            field.GenerateField(config);
            Assert.That(field.GetNeighbors(field.GetCell(1, 1)).Count, Is.EqualTo(6));

            config.MovementPreset = GridMovementPreset.EightDirections2D;
            field.GenerateField();
            Assert.That(config.MovementRule.Directions.Count, Is.EqualTo(8));
            Assert.That(field.GetNeighbors(field.GetCell(1, 1)).Count, Is.EqualTo(8));
            Assert.That(MovementRule.FromPreset(GridMovementPreset.UseMovementRule), Is.Null);
            Assert.That(MovementRule.FromPreset(GridMovementPreset.TwentySixDirections3D).Directions.Count,
                Is.EqualTo(26));
        }

        [Test]
        public void ShapeMask_CanBeBuiltAndChangedFromCode()
        {
            GridShapeMask mask = GridShapeMask.Create(GridShapeMaskMode.EnabledCells,
                new[] { new Vector3Int(0, 0, 0), new Vector3Int(1, 1, 0) });
            _created.Add(mask);
            GameObject owner = new GameObject("ShapeMask");
            _created.Add(owner);
            FieldGenerator field = owner.AddComponent<FieldGenerator>();
            FieldGeneratorConfig config = new FieldGeneratorConfig(new Vector3Int(3, 3, 1), null, GridType.Custom)
            {
                ShapeMask = mask
            };

            field.GenerateField(config);
            Assert.That(CountEnabled(field), Is.EqualTo(2));
            Assert.That(mask.Contains(new Vector3Int(1, 1, 0)), Is.True);

            mask.SetCells(new[] { new Vector3Int(2, 2, 0) });
            field.ApplyShapeMask();
            Assert.That(CountEnabled(field), Is.EqualTo(1));
            Assert.That(field.GetCell(2, 2).IsEnabled, Is.True);

            mask.Mode = GridShapeMaskMode.DisabledCells;
            config.GridType = GridType.Rectangular;
            field.GenerateField();
            Assert.That(CountEnabled(field), Is.EqualTo(8));
        }

        [Test]
        public void ObjectSpawner_UnregisterAndPruneFreeTheCellWithoutDestroyingTwice()
        {
            FieldGenerator field = CreateField(2, 1);
            FieldObjectSpawner spawner = field.gameObject.AddComponent<FieldObjectSpawner>();
            GameObject prefab = new GameObject("SpawnerPrefab");
            _created.Add(prefab);
            spawner.Prefabs = new[] { prefab };
            int removed = 0;
            int freed = 0;
            spawner.OnObjectRemoved.AddListener(info => removed++);
            spawner.OnCellFreed.AddListener(cell => freed++);

            SpawnedObjectInfo pooled = spawner.SpawnAt(Vector3Int.zero);
            _created.Add(pooled.GameObject);
            Assert.That(field.GetCell(0, 0).IsOccupied, Is.True);

            Assert.That(spawner.Unregister(pooled.GameObject), Is.True);
            Assert.That(pooled.GameObject != null, Is.True, "Unregister must not destroy the instance");
            Assert.That(field.GetCell(0, 0).IsOccupied, Is.False);
            Assert.That(spawner.Unregister(pooled.GameObject), Is.False);
            Assert.That(removed, Is.EqualTo(1));
            Assert.That(freed, Is.EqualTo(1));

            SpawnedObjectInfo destroyedExternally = spawner.SpawnAt(new Vector3Int(1, 0, 0));
            Object.DestroyImmediate(destroyedExternally.GameObject);
            Assert.That(field.GetCell(1, 0).IsOccupied, Is.True);

            Assert.That(spawner.PruneDestroyedObjects(), Is.EqualTo(1));
            Assert.That(field.GetCell(1, 0).IsOccupied, Is.False);
            Assert.That(spawner.GetAllObjects().Count, Is.EqualTo(0));
            Assert.That(spawner.PruneDestroyedObjects(), Is.EqualTo(0));
            Assert.That(removed, Is.EqualTo(2));
            Assert.That(freed, Is.EqualTo(2));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void ClearOperations_NotifyOnlyAfterEveryCellIsCleared(int clearer)
        {
            FieldGenerator field = CreateField(3, 2);
            WriteRowMajor(field, new[] { 2, 4, 8, 16, 32, 64 });
            foreach (FieldCell cell in field.GetAllCells())
            {
                cell.IsOccupied = true;
            }

            // WHY: the sliding-merge clear only resets content; the dice and slot clears also free occupancy.
            bool expectOccupied = clearer == 0;
            int expectedEmpty = clearer == 0 ? 0 : -1;
            int notifications = 0;
            bool sawPartialBoard = false;
            field.OnCellStateChanged.AddListener(changed =>
            {
                notifications++;
                foreach (FieldCell cell in field.GetAllCells())
                {
                    sawPartialBoard |= cell.ContentId != expectedEmpty || cell.IsOccupied != expectOccupied;
                }
            });

            switch (clearer)
            {
                case 0:
                    SlidingMergeBoardService sliding = field.gameObject.AddComponent<SlidingMergeBoardService>();
                    sliding.EmptyContentId = 0;
                    sliding.ClearBoard();
                    break;
                case 1:
                    new DiceBoard(field).ClearBoard();
                    break;
                default:
                    new GridSlotAllocator(field).Clear();
                    break;
            }

            Assert.That(notifications, Is.EqualTo(6));
            Assert.That(sawPartialBoard, Is.False);
        }

        // WHY: contract test for re-entrant allocation across boards; the per-instance entry it exercises is hygiene,
        // the previous shared entry produced no observable failure here.
        [Test]
        public void SlotAllocators_ReentrantAllocationOnAnotherBoardKeepsBothPlacements()
        {
            FieldGenerator first = CreateField(2, 1);
            FieldGenerator second = CreateField(2, 1);
            GridSlotAllocator firstSlots = new GridSlotAllocator(first);
            GridSlotAllocator secondSlots = new GridSlotAllocator(second);
            first.OnCellStateChanged.AddListener(cell => secondSlots.Allocate(0, 9));

            GridPlacementResult result = firstSlots.Allocate(0, 7);

            Assert.That(result.Placed, Is.True);
            Assert.That(first.GetCell(0, 0).ContentId, Is.EqualTo(7));
            Assert.That(second.GetCell(0, 0).ContentId, Is.EqualTo(9));
            Assert.That(firstSlots.Allocate(1, 3).Placed, Is.True);
            Assert.That(first.GetCell(1, 0).ContentId, Is.EqualTo(3));
        }

        private FieldGenerator CreateField(int width, int height)
        {
            GameObject owner = new GameObject("GridSystemGameplayRulesTests");
            _created.Add(owner);
            owner.AddComponent<Grid>();
            FieldGenerator field = owner.AddComponent<FieldGenerator>();
            field.GenerateField(new FieldGeneratorConfig(new Vector3Int(width, height, 1)));
            return field;
        }

        private static void WriteRowMajor(FieldGenerator field, int[] values)
        {
            Vector3Int size = field.GeneratedSize;
            for (int i = 0; i < values.Length; i++)
            {
                field.GetCell(i % size.x, i / size.x).ContentId = values[i];
            }
        }

        private static string Snapshot(FieldGenerator field)
        {
            Vector3Int size = field.GeneratedSize;
            List<string> rows = new List<string>();
            for (int y = 0; y < size.y; y++)
            {
                List<string> row = new List<string>();
                for (int x = 0; x < size.x; x++)
                {
                    row.Add(field.GetCell(x, y).ContentId.ToString());
                }

                rows.Add(string.Join(",", row));
            }

            return string.Join("|", rows);
        }

        private static List<Vector3Int> Positions(List<FieldCell> cells)
        {
            List<Vector3Int> positions = new List<Vector3Int>();
            foreach (FieldCell cell in cells)
            {
                positions.Add(cell.Position);
            }

            return positions;
        }

        private static int CountEnabled(FieldGenerator field)
        {
            int count = 0;
            foreach (FieldCell cell in field.GetAllCells(false))
            {
                count++;
            }

            return count;
        }
    }
}
