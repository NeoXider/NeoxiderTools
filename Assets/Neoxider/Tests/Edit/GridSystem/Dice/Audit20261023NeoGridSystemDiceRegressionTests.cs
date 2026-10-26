using System;
using System.Collections.Generic;
using Neo.GridSystem;
using Neo.GridSystem.Dice;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.GridSystem
{
    public sealed class Audit20261023NeoGridSystemDiceRegressionTests
    {
        private readonly List<GameObject> _createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject createdObject in _createdObjects)
            {
                if (createdObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(createdObject);
                }
            }

            _createdObjects.Clear();
        }

        [TestCase(int.MaxValue - 1, int.MaxValue)]
        [TestCase(int.MinValue, int.MinValue + 1)]
        [Timeout(1000)]
        public void CreateSequentialPool_ReturnsBothIntegerBoundaryValues(int minimum, int maximum)
        {
            List<int> pool = DicePieceGenerator.CreateSequentialPool(minimum, maximum);

            CollectionAssert.AreEqual(new[] { minimum, maximum }, pool);
        }

        [Test]
        [Timeout(1000)]
        public void CreateSequentialPool_SingleMaximumValue_ReturnsOneEntry()
        {
            List<int> pool = DicePieceGenerator.CreateSequentialPool(int.MaxValue, int.MaxValue);

            CollectionAssert.AreEqual(new[] { int.MaxValue }, pool);
        }

        [TestCase(int.MinValue, int.MaxValue)]
        [TestCase(int.MinValue, -1)]
        [TestCase(0, int.MaxValue)]
        [Timeout(1000)]
        public void CreateSequentialPool_RejectsUnrepresentableCountBeforeAllocation(int minimum, int maximum)
        {
            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
                () => DicePieceGenerator.CreateSequentialPool(minimum, maximum));

            Assert.That(exception.ParamName, Is.EqualTo("maxValue"));
        }

        [TestCase(true, 3, -2)]
        [TestCase(false, -3, 2)]
        public void Rotate_OffAnchorSingleCell_RotatesAndPreservesSource(bool clockwise, int expectedX, int expectedY)
        {
            Vector3Int originalOffset = new(2, 3, 4);
            DicePiece piece = new(new[] { new DicePieceCell(originalOffset, 7) });

            DicePiece rotated = clockwise ? piece.RotateClockwise() : piece.RotateCounterClockwise();
            DicePiece restored = clockwise ? rotated.RotateCounterClockwise() : rotated.RotateClockwise();

            Assert.That(rotated.Cells[0].Offset, Is.EqualTo(new Vector3Int(expectedX, expectedY, 4)));
            Assert.That(rotated.Cells[0].Value, Is.EqualTo(7));
            Assert.That(piece.Cells[0].Offset, Is.EqualTo(originalOffset));
            Assert.That(restored.Cells[0].Offset, Is.EqualTo(originalOffset));
        }

        [TestCase(false, 1, 2)]
        [TestCase(true, 2, 1)]
        public void Generate_RepeatedPoolEntries_ProducesDifferentPairValues(bool takeLast, int first, int second)
        {
            int[] pool = { 1, 1, 2, 2 };
            DicePieceGenerator generator = new(maximum => takeLast ? maximum - 1 : 0);

            DicePiece piece = generator.Generate(pool, true);

            Assert.That(piece.CellCount, Is.EqualTo(2));
            Assert.That(piece.Cells[0].Value, Is.EqualTo(first));
            Assert.That(piece.Cells[1].Value, Is.EqualTo(second));
            CollectionAssert.AreEqual(new[] { 1, 1, 2, 2 }, pool);
        }

        [Test]
        public void Generate_OnlyOneDistinctValue_FallsBackToSingle()
        {
            DicePieceGenerator generator = new(_ => 0);

            DicePiece piece = generator.Generate(new[] { 7, 7 }, true);

            Assert.That(piece.CellCount, Is.EqualTo(1));
            Assert.That(piece.Cells[0].Value, Is.EqualTo(7));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void GenerateWeighted_RejectsOverflowingTotalBeforeRandomDraw(bool duplicateValue)
        {
            int randomDraws = 0;
            DicePieceGenerator generator = new(_ =>
            {
                randomDraws++;
                return 0;
            });
            DiceValueWeight[] weights =
            {
                new(4, int.MaxValue),
                new(duplicateValue ? 4 : 5, 1)
            };

            ArgumentException exception = Assert.Throws<ArgumentException>(() => generator.GenerateWeighted(weights));

            Assert.That(exception.ParamName, Is.EqualTo("weights"));
            Assert.That(randomDraws, Is.EqualTo(0));
        }

        [Test]
        public void GenerateWeighted_MaximumTotalAndDuplicateValues_KeepBothValuesSelectable()
        {
            DiceValueWeight[] weights =
            {
                new(4, int.MaxValue - 2),
                new(4, 1),
                new(5, 1)
            };
            int[] rolls = { 0, int.MaxValue - 1 };
            int[] selectedValues = new int[rolls.Length];
            for (int i = 0; i < rolls.Length; i++)
            {
                int roll = rolls[i];
                int requestedMaximum = 0;
                DicePieceGenerator generator = new(maximum =>
                {
                    requestedMaximum = maximum;
                    return roll;
                });

                DicePiece piece = generator.GenerateWeighted(weights, false);

                Assert.That(requestedMaximum, Is.EqualTo(int.MaxValue));
                Assert.That(piece.CellCount, Is.EqualTo(1));
                selectedValues[i] = piece.Cells[0].Value;
            }

            CollectionAssert.AreEquivalent(new[] { 4, 5 }, selectedValues);
        }

        [TestCase(0, int.MaxValue)]
        [TestCase(int.MaxValue, int.MaxValue)]
        [TestCase(6, 6)]
        public void Place_OverflowingMerge_SaturatesAndPublishesFinalBoard(int maximumContentId, int expectedValue)
        {
            FieldGenerator field = CreateField();
            DiceBoard board = new(field) { MergeStep = int.MaxValue, MaxContentId = maximumContentId };
            int sourceValue = int.MaxValue - 1;
            Assert.That(board.Place(DicePiece.Single(sourceValue), Vector3Int.zero, false).Placed, Is.True);
            Assert.That(board.Place(DicePiece.Single(sourceValue), Vector3Int.right, false).Placed, Is.True);
            int boardChanges = 0;
            int merges = 0;
            int notifiedContentId = 0;
            board.BoardChanged += () =>
            {
                boardChanges++;
                notifiedContentId = field.GetCell(2, 0).ContentId;
            };
            board.MergesResolved += _ => merges++;

            DicePlacementResult result = board.Place(DicePiece.Single(sourceValue), new Vector3Int(2, 0, 0));

            Assert.That(result.Placed, Is.True);
            Assert.That(result.MergeResult.Groups.Count, Is.EqualTo(1));
            Assert.That(result.MergeResult.Groups[0].ResultContentId, Is.EqualTo(expectedValue));
            Assert.That(field.GetCell(2, 0).ContentId, Is.EqualTo(expectedValue));
            Assert.That(field.GetCell(2, 0).IsOccupied, Is.True);
            Assert.That(field.GetCell(0, 0).ContentId, Is.EqualTo(board.EmptyContentId));
            Assert.That(field.GetCell(0, 0).IsOccupied, Is.False);
            Assert.That(field.GetCell(1, 0).ContentId, Is.EqualTo(board.EmptyContentId));
            Assert.That(field.GetCell(1, 0).IsOccupied, Is.False);
            Assert.That(boardChanges, Is.EqualTo(1));
            Assert.That(merges, Is.EqualTo(1));
            Assert.That(notifiedContentId, Is.EqualTo(expectedValue));
        }

        private FieldGenerator CreateField()
        {
            GameObject owner = new(nameof(Audit20261023NeoGridSystemDiceRegressionTests));
            _createdObjects.Add(owner);
            owner.AddComponent<Grid>();
            FieldGenerator field = owner.AddComponent<FieldGenerator>();
            field.GenerateField(new FieldGeneratorConfig
            {
                Size = new Vector3Int(3, 1, 1),
                GridType = GridType.Rectangular,
                MovementRule = MovementRule.FourDirections2D,
                PassabilityMode = CellPassabilityMode.WalkableEnabledAndUnoccupied,
                Origin2D = GridOrigin2D.BottomLeft
            });
            foreach (FieldCell cell in field.GetAllCells(false))
            {
                cell.ContentId = -1;
                cell.IsOccupied = false;
            }

            return field;
        }
    }
}
