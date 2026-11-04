using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.GridSystem;
using Neo.GridSystem.Dice;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests.GridSystem
{
    /// <summary>
    ///     Covers the Dice placement search (game-over detection), allocation-free placement checks and Inspector
    ///     setting synchronisation of <see cref="DiceBoardService" />.
    /// </summary>
    [TestFixture]
    public sealed class DiceBoardPlacementSearchTests
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
        public void CanPlaceAnywhere_FindsRotatedFitsAndReportsTheRotation()
        {
            FieldGenerator field = CreateField(1, 2);
            DiceBoard board = new DiceBoard(field);
            DicePiece pair = DicePiece.Pair(3, 4);

            Assert.That(board.CanPlaceAnywhere(pair, false), Is.False, "a horizontal pair cannot fit a 1x2 column");
            Assert.That(board.TryFindPlacement(pair, true, out Vector3Int anchor, out int turns), Is.True);
            Assert.That(turns, Is.EqualTo(1));
            Assert.That(anchor, Is.EqualTo(new Vector3Int(0, 1, 0)));

            DicePiece rotated = pair;
            for (int i = 0; i < turns; i++)
            {
                rotated = rotated.RotateClockwise();
            }

            DicePlacementResult placement = board.Place(rotated, anchor, false);
            Assert.That(placement.Placed, Is.True);
            Assert.That(field.GetCell(0, 1).ContentId, Is.EqualTo(3));
            Assert.That(field.GetCell(0, 0).ContentId, Is.EqualTo(4));
            Assert.That(board.CanPlaceAnywhere(DicePiece.Single(1)), Is.False, "board is full");
        }

        [Test]
        public void CanPlaceAnywhere_MatchesAnExhaustiveSearch()
        {
            FieldGenerator field = CreateField(4, 3);
            DiceBoard board = new DiceBoard(field);
            Vector3Int[] occupied = { new(1, 0, 0), new(3, 0, 0), new(0, 1, 0), new(2, 1, 0), new(1, 2, 0), new(3, 2, 0) };
            foreach (Vector3Int position in occupied)
            {
                FieldCell cell = field.GetCell(position);
                cell.ContentId = 6;
                cell.IsOccupied = true;
            }

            DicePiece pair = DicePiece.Pair(1, 2);
            Assert.That(board.CanPlaceAnywhere(pair), Is.EqualTo(ExhaustiveFits(board, field, pair)));
            Assert.That(board.CanPlaceAnywhere(pair), Is.False, "checkerboard leaves no adjacent free pair");
            Assert.That(board.CanPlaceAnywhere(DicePiece.Single(1)), Is.True);

            field.GetCell(3, 2).IsOccupied = false;
            field.GetCell(3, 2).ContentId = -1;
            Assert.That(board.CanPlaceAnywhere(pair), Is.True);
            Assert.That(board.CanPlaceAnywhere(pair), Is.EqualTo(ExhaustiveFits(board, field, pair)));
        }

        [Test]
        public void CanPlace_WarmCall_DoesNotAllocate()
        {
            FieldGenerator field = CreateField(5, 5);
            DiceBoard board = new DiceBoard(field);
            DicePiece pair = DicePiece.Pair(2, 5);
            board.CanPlace(pair, Vector3Int.zero);
            board.CanPlaceAnywhere(pair);

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            int fits = 0;
            for (int x = 0; x < 5; x++)
            for (int y = 0; y < 5; y++)
            {
                if (board.CanPlace(pair, new Vector3Int(x, y, 0)))
                {
                    fits++;
                }
            }

            bool anywhere = board.CanPlaceAnywhere(pair);
            long allocationCalls = allocationCounter.End();

            Assert.That(allocationCalls, Is.EqualTo(0L));
            Assert.That(fits, Is.EqualTo(20));
            Assert.That(anywhere, Is.True);
        }

        [Test]
        public void DiceBoardService_InspectorEditsReachTheLiveCore()
        {
            FieldGenerator field = CreateField(3, 1);
            DiceBoardService service = field.gameObject.AddComponent<DiceBoardService>();
            DiceBoard core = service.Board;
            Assert.That(core.MaxContentId, Is.EqualTo(0));

            SetPrivateField(service, "_maxContentId", 6);
            SetPrivateField(service, "_mergeStep", 0);
            SetPrivateField(service, "_requireWalkable", false);
            InvokeOnValidate(service);

            Assert.That(core.MaxContentId, Is.EqualTo(6));
            Assert.That(core.MergeStep, Is.EqualTo(1));
            Assert.That(service.MergeStep, Is.EqualTo(1));
            Assert.That(core.RequireWalkable, Is.False);
            Assert.That(service.CanPlaceAnywhere(DicePiece.Pair(1, 1)), Is.True);
        }

        private FieldGenerator CreateField(int width, int height)
        {
            GameObject owner = new GameObject("DiceBoardPlacementSearchTests");
            _createdObjects.Add(owner);
            owner.AddComponent<Grid>();
            FieldGenerator field = owner.AddComponent<FieldGenerator>();
            field.GenerateField(new FieldGeneratorConfig(new Vector3Int(width, height, 1))
            {
                Origin2D = GridOrigin2D.BottomLeft
            });
            return field;
        }

        private static bool ExhaustiveFits(DiceBoard board, FieldGenerator field, DicePiece piece)
        {
            DicePiece orientation = piece;
            for (int turn = 0; turn < 4; turn++)
            {
                for (int x = -2; x < field.GeneratedSize.x + 2; x++)
                for (int y = -2; y < field.GeneratedSize.y + 2; y++)
                {
                    if (board.CanPlace(orientation, new Vector3Int(x, y, 0)))
                    {
                        return true;
                    }
                }

                orientation = orientation.RotateClockwise();
            }

            return false;
        }

        private static void SetPrivateField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private static void InvokeOnValidate(object target)
        {
            MethodInfo method = target.GetType().GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(target, null);
        }
    }
}
