using System.Collections.Generic;
using System.Reflection;
using Neo.GridSystem;
using Neo.GridSystem.Match3;
using Neo.GridSystem.SlidingMerge;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public class GridSystemTests
    {
        private GameObject _goGrid;

        [SetUp]
        public void SetUp()
        {
            _goGrid = new GameObject("GridSystemTest");
        }

        [TearDown]
        public void TearDown()
        {
            if (_goGrid != null)
            {
                Object.DestroyImmediate(_goGrid);
            }
        }

        [Test]
        public void FieldGenerator_GenerateField_CreatesCellsCorrectly()
        {
            Grid grid = _goGrid.AddComponent<Grid>();
            FieldGenerator generator = _goGrid.AddComponent<FieldGenerator>();

            var config = new FieldGeneratorConfig();
            config.Size = new Vector3Int(5, 5, 1);
            config.GridType = GridType.Rectangular;

            generator.GenerateField(config);

            Assert.IsNotNull(generator.Cells, "Cells array should not be null");
            Assert.AreEqual(25, generator.Cells.Length, "Should generate exactly 25 cells for 5x5 grid");

            FieldCell cell = generator.GetCell(new Vector3Int(2, 2, 0));
            Assert.IsNotNull(cell, "Middle cell should exist");
            Assert.IsTrue(cell.IsEnabled, "Middle cell should be enabled by default");
        }

        [Test]
        public void GridPathfinder_FindPath_ReturnsValidPath()
        {
            Grid grid = _goGrid.AddComponent<Grid>();
            FieldGenerator generator = _goGrid.AddComponent<FieldGenerator>();

            var config = new FieldGeneratorConfig();
            config.Size = new Vector3Int(3, 3, 1);
            config.GridType = GridType.Rectangular;

            // Allow adjacent directional movements
            config.MovementRule = MovementRule.FourDirections2D;

            generator.GenerateField(config);

            // Create a blocker on (1,0,0) and (1,1,0) to force a path around
            generator.SetWalkable(new Vector3Int(1, 0, 0), false);
            generator.SetWalkable(new Vector3Int(1, 1, 0), false);

            var request = new GridPathRequest
            {
                Start = new Vector3Int(0, 0, 0),
                End = new Vector3Int(2, 0, 0),
                Directions = config.MovementRule.Directions
            };

            GridPathResult result = generator.FindPathDetailed(request);

            Assert.IsNotNull(result, "Result should not be null");
            Assert.IsTrue(result.HasPath, $"Path should be found. Reason: {result.Reason}");
            // Expected path: (0,0)->(0,1)->(0,2)->(1,2)->(2,2)->(2,1)->(2,0)
            Assert.AreEqual(7, result.Path.Count, "Path should be 7 nodes long (including start and end)");
        }

        [Test]
        public void Match3BoardService_TryFindValidSwap_FindsKnownMove()
        {
            FieldGenerator generator = CreateGeneratedField(new Vector3Int(3, 3, 1));
            Match3BoardService match3 = _goGrid.AddComponent<Match3BoardService>();
            InvokeAwake(match3);

            SetRow(generator, 0, Match3TileState.Red, Match3TileState.Blue, Match3TileState.Red);
            SetRow(generator, 1, Match3TileState.Green, Match3TileState.Red, Match3TileState.Green);
            SetRow(generator, 2, Match3TileState.Blue, Match3TileState.Green, Match3TileState.Blue);

            Assert.That(match3.FindMatches(), Is.Empty);
            Assert.That(match3.TryFindValidSwap(out Vector3Int a, out Vector3Int b), Is.True);

            bool resolved = match3.TrySwapAndResolve(a, b);

            Assert.That(resolved, Is.True);
            Assert.That(match3.FindMatches(), Is.Empty);
        }

        [Test]
        public void Match3BoardService_CollapseColumns_CompactsWithinUsableSegments()
        {
            FieldGenerator generator = CreateGeneratedField(new Vector3Int(1, 4, 1));
            Match3BoardService match3 = _goGrid.AddComponent<Match3BoardService>();
            InvokeAwake(match3);

            generator.SetEnabled(new Vector3Int(0, 0, 0), false);
            generator.SetContentId(new Vector3Int(0, 1, 0), (int)Match3TileState.None);
            generator.SetContentId(new Vector3Int(0, 2, 0), (int)Match3TileState.Red);
            generator.SetContentId(new Vector3Int(0, 3, 0), (int)Match3TileState.Blue);

            InvokePrivate(match3, "CollapseColumns");

            Assert.That(generator.GetCell(0, 1).ContentId, Is.EqualTo((int)Match3TileState.Red));
            Assert.That(generator.GetCell(0, 2).ContentId, Is.EqualTo((int)Match3TileState.Blue));
            Assert.That(generator.GetCell(0, 3).ContentId, Is.EqualTo((int)Match3TileState.None));
        }

        [Test]
        public void SlidingMergeResolver_SlideLeft_CompactsAndMergesOnce()
        {
            FieldGenerator generator = CreateGeneratedField(new Vector3Int(4, 1, 1));
            SetContentRow(generator, 0, 2, 0, 2, 4);

            SlidingMergeResult result = SlidingMergeResolver.Slide(generator, SlidingMergeDirection.Left);

            Assert.That(result.Changed, Is.True);
            Assert.That(result.MergeCount, Is.EqualTo(1));
            Assert.That(result.ScoreDelta, Is.EqualTo(4));
            AssertContentRow(generator, 0, 4, 4, 0, 0);
        }

        [Test]
        public void SlidingMergeResolver_SlideLeft_RespectsDisabledSegments()
        {
            FieldGenerator generator = CreateGeneratedField(new Vector3Int(4, 1, 1));
            SetContentRow(generator, 0, 0, 0, 2, 2);
            generator.SetEnabled(new Vector3Int(1, 0, 0), false);

            SlidingMergeResult result = SlidingMergeResolver.Slide(generator, SlidingMergeDirection.Left);

            Assert.That(result.Changed, Is.True);
            Assert.That(result.MergeCount, Is.EqualTo(1));
            Assert.That(generator.GetCell(0, 0).ContentId, Is.EqualTo(0));
            Assert.That(generator.GetCell(1, 0).IsEnabled, Is.False);
            Assert.That(generator.GetCell(2, 0).ContentId, Is.EqualTo(4));
            Assert.That(generator.GetCell(3, 0).ContentId, Is.EqualTo(0));
        }

        [Test]
        public void SlidingMergeResolver_CanSlide_ReturnsFalseWhenBoardIsLocked()
        {
            FieldGenerator generator = CreateGeneratedField(new Vector3Int(4, 1, 1));
            SetContentRow(generator, 0, 2, 4, 8, 16);

            Assert.That(SlidingMergeResolver.CanSlide(generator, SlidingMergeDirection.Left), Is.False);
            AssertContentRow(generator, 0, 2, 4, 8, 16);
        }

        [Test]
        public void GridGameBuilder_EnsureConfigured_AddsSelectedModules()
        {
            GridGameBuilder builder = _goGrid.AddComponent<GridGameBuilder>();
            builder.Features = GridGameFeatures.DebugDrawer | GridGameFeatures.SlidingMerge;

            builder.EnsureConfigured();

            Assert.That(_goGrid.GetComponent<Grid>(), Is.Not.Null);
            Assert.That(_goGrid.GetComponent<FieldGenerator>(), Is.Not.Null);
            Assert.That(_goGrid.GetComponent<FieldDebugDrawer>(), Is.Not.Null);
            Assert.That(_goGrid.GetComponent<SlidingMergeBoardService>(), Is.Not.Null);
            Assert.That(_goGrid.GetComponent<Match3BoardService>(), Is.Null);
        }

        private FieldGenerator CreateGeneratedField(Vector3Int size)
        {
            _goGrid.AddComponent<Grid>();
            FieldGenerator generator = _goGrid.AddComponent<FieldGenerator>();
            generator.GenerateField(new FieldGeneratorConfig(size));
            return generator;
        }

        private static void SetRow(FieldGenerator generator, int y, params Match3TileState[] tiles)
        {
            for (int x = 0; x < tiles.Length; x++)
            {
                generator.SetContentId(new Vector3Int(x, y, 0), (int)tiles[x]);
            }
        }

        private static void SetContentRow(FieldGenerator generator, int y, params int[] values)
        {
            for (int x = 0; x < values.Length; x++)
            {
                generator.SetContentId(new Vector3Int(x, y, 0), values[x]);
            }
        }

        private static void AssertContentRow(FieldGenerator generator, int y, params int[] expected)
        {
            for (int x = 0; x < expected.Length; x++)
            {
                Assert.That(generator.GetCell(x, y).ContentId, Is.EqualTo(expected[x]));
            }
        }

        private static void InvokeAwake(Match3BoardService match3)
        {
            InvokePrivate(match3, "Awake");
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method!.Invoke(target, null);
        }
    }
}
