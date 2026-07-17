using System.Collections.Generic;
using Neo.GridSystem;
using Neo.GridSystem.Dice;
using Neo.GridSystem.Merge;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.GridSystem
{
    /// <summary>
    ///     Covers the plain C# <see cref="DiceBoard"/> core: placement, merge resolution with cap,
    ///     events, clearing, and the <see cref="DiceBoardService"/> wrapper delegation (settings
    ///     forwarding and UnityEvent re-raising).
    /// </summary>
    [TestFixture]
    public class DiceBoardCoreTests
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
            var owner = new GameObject("DiceBoardCoreTests");
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

            foreach (FieldCell cell in generator.GetAllCells(false))
            {
                cell.ContentId = -1; // empty marker used by the board
            }

            return generator;
        }

        [Test]
        public void Place_Single_WritesContentAndRaisesBoardChanged()
        {
            var board = new DiceBoard(CreateField(3, 3));
            int boardChanged = 0;
            board.BoardChanged += () => boardChanged++;

            DicePlacementResult result = board.Place(DicePiece.Single(2), new Vector3Int(1, 1, 0));

            Assert.IsTrue(result.Placed);
            Assert.AreEqual(2, board.Generator.GetCell(new Vector3Int(1, 1, 0)).ContentId);
            Assert.AreEqual(1, boardChanged, "One placement = one BoardChanged.");
        }

        [Test]
        public void Place_OccupiedCell_Fails()
        {
            var board = new DiceBoard(CreateField(2, 2));
            Assert.IsTrue(board.Place(DicePiece.Single(1), Vector3Int.zero).Placed);

            Assert.IsFalse(board.CanPlace(DicePiece.Single(2), Vector3Int.zero));
            Assert.IsFalse(board.Place(DicePiece.Single(2), Vector3Int.zero).Placed);
        }

        [Test]
        public void Place_TriggersMerge_WithCapAndEvents()
        {
            var board = new DiceBoard(CreateField(3, 1))
            {
                MinMergeGroupSize = 3,
                MergeStep = 1,
                MaxContentId = 3
            };

            Assert.IsTrue(board.Place(DicePiece.Single(3), new Vector3Int(0, 0, 0), resolveMerges: false).Placed);
            Assert.IsTrue(board.Place(DicePiece.Single(3), new Vector3Int(1, 0, 0), resolveMerges: false).Placed);

            GridMergeResult merged = null;
            board.MergesResolved += r => merged = r;

            DicePlacementResult result = board.Place(DicePiece.Single(3), new Vector3Int(2, 0, 0));

            Assert.IsTrue(result.Placed);
            Assert.IsNotNull(merged, "Completing a group of three must resolve a merge.");
            Assert.IsTrue(result.MergeResult.HasChanges);

            int survivors = 0;
            foreach (FieldCell cell in board.Generator.GetAllCells(false))
            {
                if (cell.ContentId != -1)
                {
                    survivors++;
                    Assert.AreEqual(3, cell.ContentId, "MaxContentId caps 3+1 back to 3.");
                    Assert.IsTrue(cell.IsOccupied);
                }
            }

            Assert.AreEqual(1, survivors, "A merged group collapses into one cell.");
        }

        [Test]
        public void ClearBoard_EmptiesEverything()
        {
            var board = new DiceBoard(CreateField(2, 2));
            board.Place(DicePiece.Pair(1, 2), Vector3Int.zero);

            board.ClearBoard();

            foreach (FieldCell cell in board.Generator.GetAllCells(false))
            {
                Assert.AreEqual(-1, cell.ContentId);
                Assert.IsFalse(cell.IsOccupied);
            }
        }

        [Test]
        public void Service_DelegatesToCore_AndForwardsSettingsAndEvents()
        {
            FieldGenerator field = CreateField(3, 1);
            DiceBoardService service = field.gameObject.AddComponent<DiceBoardService>();
            service.MinMergeGroupSize = 3;
            service.MaxContentId = 3;

            Assert.IsNotNull(service.Board);
            Assert.AreEqual(3, service.Board.MinMergeGroupSize, "Setters forward into the core.");
            Assert.AreEqual(3, service.Board.MaxContentId);

            int unityBoardChanged = 0;
            int unityMerges = 0;
            service.OnBoardChanged.AddListener(() => unityBoardChanged++);
            service.OnMergesResolved.AddListener(_ => unityMerges++);

            service.Place(DicePiece.Single(1), new Vector3Int(0, 0, 0), resolveMerges: false);
            service.Place(DicePiece.Single(1), new Vector3Int(1, 0, 0), resolveMerges: false);
            service.Place(DicePiece.Single(1), new Vector3Int(2, 0, 0));

            Assert.AreEqual(3, unityBoardChanged, "Core events re-raise as UnityEvents.");
            Assert.AreEqual(1, unityMerges);
        }
    }
}
