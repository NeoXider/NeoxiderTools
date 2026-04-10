using System.Collections.Generic;
using Neo.GridSystem;
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
    }
}
