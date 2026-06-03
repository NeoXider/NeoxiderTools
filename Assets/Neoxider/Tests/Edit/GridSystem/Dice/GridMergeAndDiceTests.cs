using System.Collections.Generic;
using Neo.GridSystem;
using Neo.GridSystem.Dice;
using Neo.GridSystem.Merge;
using Neo.Merge;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.GridSystem
{
    public sealed class GridMergeAndDiceTests
    {
        private readonly List<GameObject> _createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject obj in _createdObjects)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }

            _createdObjects.Clear();
        }

        [Test]
        public void GridMerge_MergesLShapeAndIgnoresBlockedCells()
        {
            FieldGenerator generator = CreateGenerator(3, 3);
            Set(generator, 0, 0, 4);
            Set(generator, 1, 0, 4);
            Set(generator, 0, 1, 4);
            Set(generator, 2, 2, 4);
            generator.SetWalkable(new Vector3Int(2, 2, 0), false);

            GridMergeResult result = GridMergeResolver.Resolve(generator, new GridMergeRequest
            {
                Seeds = new[] { new Vector3Int(0, 0, 0) },
                EmptyContentId = 0,
                MinGroupSize = 3,
                Mutate = true,
                CascadeMode = MergeCascadeMode.None
            });

            Assert.That(result.Groups.Count, Is.EqualTo(1));
            Assert.That(generator.GetCell(0, 0).ContentId, Is.EqualTo(5));
            Assert.That(generator.GetCell(1, 0).ContentId, Is.EqualTo(0));
            Assert.That(generator.GetCell(0, 1).ContentId, Is.EqualTo(0));
            Assert.That(generator.GetCell(2, 2).ContentId, Is.EqualTo(4));
        }

        [Test]
        public void GridMerge_UsesMovementRuleDirectionsAndReturnsChangedPositions()
        {
            FieldGenerator generator = CreateGenerator(3, 3);
            Set(generator, 0, 0, 7);
            Set(generator, 1, 1, 7);
            Set(generator, 2, 2, 7);

            GridMergeResult fourDirectionResult = GridMergeResolver.Resolve(generator, new GridMergeRequest
            {
                Seeds = new[] { new Vector3Int(0, 0, 0) },
                EmptyContentId = 0,
                MinGroupSize = 3,
                Mutate = false
            });

            GridMergeResult diagonalResult = GridMergeResolver.Resolve(generator, new GridMergeRequest
            {
                Seeds = new[] { new Vector3Int(0, 0, 0) },
                Directions = new[] { new Vector3Int(1, 1, 0), new Vector3Int(-1, -1, 0) },
                EmptyContentId = 0,
                MinGroupSize = 3,
                SelectResultCell = (group, seed) => group[group.Count - 1],
                Mutate = true,
                CascadeMode = MergeCascadeMode.None
            });

            Assert.That(fourDirectionResult.Groups.Count, Is.EqualTo(0));
            Assert.That(diagonalResult.Groups.Count, Is.EqualTo(1));
            Assert.That(diagonalResult.Groups[0].Positions, Is.EquivalentTo(new[]
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(1, 1, 0),
                new Vector3Int(2, 2, 0)
            }));
            Assert.That(diagonalResult.ChangedPositions, Is.EquivalentTo(diagonalResult.Groups[0].Positions));
            Assert.That(diagonalResult.Groups[0].ResultCell.Position, Is.EqualTo(new Vector3Int(2, 2, 0)));
            Assert.That(generator.GetCell(2, 2).ContentId, Is.EqualTo(8));
        }

        [Test]
        public void DicePiece_RotatesPairOffsetsAroundAnchor()
        {
            DicePiece piece = DicePiece.Pair(1, 2);

            DicePiece rotated = piece.RotateClockwise();

            Assert.That(rotated.Cells[0].Offset, Is.EqualTo(Vector3Int.zero));
            Assert.That(rotated.Cells[1].Offset, Is.EqualTo(Vector3Int.down));
            Assert.That(rotated.Cells[1].Value, Is.EqualTo(2));
        }

        [Test]
        public void DicePieceGenerator_NeverCreatesEqualPairValues()
        {
            int[] rolls = { 1, 0, 0, 0, 0, 0 };
            int index = 0;
            var generator = new DicePieceGenerator(max => rolls[index++ % rolls.Length] % max);

            DicePiece piece = generator.Generate(new[] { 1, 2, 3, 4, 5 }, true);

            Assert.That(piece.IsPair, Is.True);
            Assert.That(piece.Cells[0].Value, Is.Not.EqualTo(piece.Cells[1].Value));
        }

        [Test]
        public void DicePieceGenerator_CanGenerateSingleAndPairFromPool()
        {
            int[] rolls = { 0, 2, 1, 0, 0 };
            int index = 0;
            var generator = new DicePieceGenerator(max => rolls[index++ % rolls.Length] % max);

            DicePiece single = generator.Generate(new[] { 1, 2, 3, 4, 5 });
            DicePiece pair = generator.Generate(new[] { 1, 2, 3, 4, 5 });

            Assert.That(single.IsPair, Is.False);
            Assert.That(single.Cells[0].Value, Is.EqualTo(3));
            Assert.That(pair.IsPair, Is.True);
            Assert.That(pair.Cells[0].Value, Is.EqualTo(1));
            Assert.That(pair.Cells[1].Value, Is.EqualTo(2));
        }

        [Test]
        public void DiceBoardService_CanPlaceRejectsBoundsAndOccupied_AndPlacementWritesContent()
        {
            FieldGenerator generator = CreateGenerator(2, 2);
            DiceBoardService dice = generator.gameObject.AddComponent<DiceBoardService>();
            DicePiece piece = DicePiece.Pair(1, 2);

            Assert.That(generator.GetCell(0, 0).ContentId, Is.EqualTo(dice.EmptyContentId));

            Assert.That(dice.CanPlace(piece, new Vector3Int(1, 0, 0)), Is.False);
            Assert.That(dice.CanPlace(piece, Vector3Int.zero), Is.True);

            DicePlacementResult result = dice.Place(piece, Vector3Int.zero, false);

            Assert.That(result.Placed, Is.True);
            Assert.That(generator.GetCell(0, 0).ContentId, Is.EqualTo(1));
            Assert.That(generator.GetCell(1, 0).ContentId, Is.EqualTo(2));
            Assert.That(dice.CanPlace(DicePiece.Single(3), Vector3Int.zero), Is.False);
        }

        [Test]
        public void DiceBoardService_UsesGridMergeForDiceMergeAndCascade()
        {
            FieldGenerator generator = CreateGenerator(3, 3);
            DiceBoardService dice = generator.gameObject.AddComponent<DiceBoardService>();
            Set(generator, 1, 0, 1);
            Set(generator, 0, 1, 1);
            Set(generator, 2, 1, 2);
            Set(generator, 1, 2, 2);

            DicePlacementResult result = dice.Place(DicePiece.Single(1), new Vector3Int(1, 1, 0), true);

            Assert.That(result.MergeResult.Groups.Count, Is.EqualTo(2));
            Assert.That(generator.GetCell(1, 1).ContentId, Is.EqualTo(3));
            Assert.That(generator.FindPathDetailed(new Vector3Int(0, 0, 0), new Vector3Int(2, 2, 0)).Path,
                Is.Not.Null);
        }

        [Test]
        public void DiceBoardService_PairPlacementSeedsBothDiceDeterministically()
        {
            FieldGenerator generator = CreateGenerator(4, 3);
            DiceBoardService dice = generator.gameObject.AddComponent<DiceBoardService>();
            Set(generator, 0, 0, 1);
            Set(generator, 0, 1, 1);
            Set(generator, 3, 0, 2);
            Set(generator, 2, 1, 2);

            DicePlacementResult result = dice.Place(DicePiece.Pair(1, 2), new Vector3Int(1, 0, 0), true);

            Assert.That(result.Placed, Is.True);
            Assert.That(result.MergeResult.Groups.Count, Is.EqualTo(2));
            Assert.That(result.MergeResult.Groups[0].ResultCell.Position, Is.EqualTo(new Vector3Int(1, 0, 0)));
            Assert.That(result.MergeResult.Groups[0].ResultContentId, Is.EqualTo(2));
            Assert.That(result.MergeResult.Groups[1].ResultCell.Position, Is.EqualTo(new Vector3Int(1, 0, 0)));
            Assert.That(result.MergeResult.Groups[1].ResultContentId, Is.EqualTo(3));
            Assert.That(generator.GetCell(1, 0).ContentId, Is.EqualTo(3));
            Assert.That(generator.GetCell(2, 0).ContentId, Is.EqualTo(dice.EmptyContentId));
        }

        private FieldGenerator CreateGenerator(int width, int height)
        {
            var owner = new GameObject("GridMergeDiceTest");
            _createdObjects.Add(owner);
            owner.AddComponent<Grid>();
            FieldGenerator generator = owner.AddComponent<FieldGenerator>();
            generator.GenerateField(new FieldGeneratorConfig
            {
                Size = new Vector3Int(width, height, 1),
                GridType = GridType.Rectangular,
                MovementRule = MovementRule.FourDirections2D,
                PassabilityMode = CellPassabilityMode.WalkableEnabledAndUnoccupied
            });
            return generator;
        }

        private static void Set(FieldGenerator generator, int x, int y, int contentId)
        {
            FieldCell cell = generator.GetCell(x, y);
            cell.ContentId = contentId;
            cell.IsOccupied = contentId != 0;
        }
    }
}
