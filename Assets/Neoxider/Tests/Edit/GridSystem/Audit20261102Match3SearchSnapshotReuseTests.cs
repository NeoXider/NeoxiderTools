using System;
using System.Collections.Generic;
using System.Globalization;
using Neo.GridSystem;
using Neo.GridSystem.Match3;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests.GridSystem
{
    public sealed class Audit20261102Match3SearchSnapshotReuseTests
    {
        private GameObject _root;
        private FieldGenerator _field;
        private Match3BoardService _service;
        private bool _createdRoot;
        private int _minimum;
        private int _changed;
        private int _resolved;
        private int _shuffled;
        private int _phases;

        [SetUp]
        public void SetUp()
        {
            _createdRoot = false;
            _changed = 0;
            _resolved = 0;
            _shuffled = 0;
            _phases = 0;
            Assert.That(ReferenceEquals(FieldGenerator.I, null), Is.True,
                "An owned test board must not replace a borrowed FieldGenerator singleton.");
            _root = new GameObject("Audit20261102Match3SearchSnapshotReuse");
            _createdRoot = true;
            _root.SetActive(false);
            _field = _root.AddComponent<FieldGenerator>();
            _field.DebugEnabled = false;
            _service = _root.AddComponent<Match3BoardService>();
            JsonUtility.FromJsonOverwrite("{\"_autoGenerateOnStart\":false}", _service);
            SetMinimum(3);
            _root.SetActive(true);
            GenerateBoard(new Vector3Int(3, 2, 1), new[] { Vector3Int.down });
            SeedCanonical(0);
            _service.OnBoardChanged.AddListener(ObserveChanged);
            _service.OnMatchesResolved.AddListener(ObserveResolved);
            _service.OnBoardShuffled.AddListener(ObserveShuffled);
            _service.OnResolvePhase += ObservePhase;
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                Object.DestroyImmediate(_root);
            }
            _root = null;
            _field = null;
            _service = null;
            if (_createdRoot)
            {
                Assert.That(ReferenceEquals(FieldGenerator.I, null), Is.True);
            }
            _createdRoot = false;
        }

        [Test]
        public void TryFindValidSwap_RestoresFailedProbeAndReadsFreshContentEachCall()
        {
            Vector3Int expectedA = new Vector3Int(1, 1, 0);
            Vector3Int expectedB = Vector3Int.right;
            Assert.That(Match3MatchFinder.FindMatches(_field, 3), Is.Empty);
            AssertEndpointOracle(expectedA, expectedB, true);
            // WHY: in x/y/z order the earlier x=0 down probe fails; it must be undone before the x=1 hit.
            AssertQuery(true, expectedA, expectedB);
            AssertQuery(true, expectedA, expectedB);

            FillUnique();
            Assert.That(Match3MatchFinder.FindMatches(_field, 3), Is.Empty);
            AssertQuery(false, Vector3Int.zero, Vector3Int.zero);
            AssertEndpointOracle(expectedA, expectedB, false);

            BoardState beforeResolve = new BoardState(_field, _service);
            int changedBefore = _changed;
            int resolvedBefore = _resolved;
            int shuffledBefore = _shuffled;
            int phasesBefore = _phases;
            Assert.That(_service.TrySwapAndResolve(expectedA, expectedB), Is.False,
                "Standalone validation must read the changed board, not the preceding query snapshot.");
            beforeResolve.AssertUnchanged(_field, _service);
            Assert.That(_changed, Is.EqualTo(changedBefore + 1), "Retain the existing failed-match notification.");
            Assert.That(_resolved, Is.EqualTo(resolvedBefore));
            Assert.That(_shuffled, Is.EqualTo(shuffledBefore));
            Assert.That(_phases, Is.EqualTo(phasesBefore));

            SeedCanonical(0);
            AssertQuery(true, expectedA, expectedB);
        }

        [Test]
        public void TryFindValidSwap_ReadsLiveGatesRulesAndGeneratedDimensions()
        {
            Vector3Int a = new Vector3Int(1, 1, 0);
            Vector3Int b = Vector3Int.right;
            FieldCell endpoint = _field.GetCell(a);
            AssertQuery(true, a, b);
            endpoint.IsEnabled = false;
            AssertQuery(false, Vector3Int.zero, Vector3Int.zero);
            endpoint.IsEnabled = true;
            AssertQuery(true, a, b);
            endpoint.IsWalkable = false;
            AssertQuery(false, Vector3Int.zero, Vector3Int.zero);
            endpoint.IsWalkable = true;
            AssertQuery(true, a, b);
            endpoint.IsOccupied = true;
            AssertQuery(false, Vector3Int.zero, Vector3Int.zero);
            endpoint.IsOccupied = false;
            AssertQuery(true, a, b);

            _field.Config.Size = Vector3Int.one;
            Assert.That(_field.GeneratedSize, Is.EqualTo(new Vector3Int(3, 2, 1)));
            AssertQuery(true, a, b);
            SetDirections(new[] { Vector3Int.up });
            AssertQuery(true, b, a);

            // WHY: a replaced config without regeneration has a pending size that must not replace the array dimensions.
            _field.Config = new FieldGeneratorConfig(Vector3Int.one, new MovementRule(new[] { Vector3Int.down }));
            AssertQuery(true, a, b);
            GenerateBoard(Vector3Int.one, new[] { Vector3Int.down });
            AssertQuery(false, Vector3Int.zero, Vector3Int.zero);
            GenerateBoard(new Vector3Int(3, 2, 1), new[] { Vector3Int.down });
            SeedCanonical(0);
            AssertQuery(true, a, b);
        }

        [Test]
        public void TryFindValidSwap_UsesGeneratedShapeInsteadOfPendingConfigMasks()
        {
            Vector3Int a = new Vector3Int(1, 1, 0);
            Vector3Int b = Vector3Int.right;
            _field.Config.DisabledCells.Add(a);
            _field.Config.BlockedCells.Add(b);
            AssertQuery(true, a, b);

            _field.GenerateField(_field.Config);
            FillUnique();
            SeedCanonical(0);
            Assert.That(_field.GetCell(a).IsEnabled, Is.False);
            Assert.That(_field.GetCell(b).IsWalkable, Is.False);
            AssertQuery(false, Vector3Int.zero, Vector3Int.zero);

            _field.Config.DisabledCells.Clear();
            _field.Config.BlockedCells.Clear();
            _field.GenerateField(_field.Config);
            FillUnique();
            SeedCanonical(0);
            AssertQuery(true, a, b);
        }

        [TestCase(3, true)]
        [TestCase(4, false)]
        public void TryFindValidSwap_PreservesOrderedEqualIdEndpointMatches(int minimum, bool expected)
        {
            GenerateBoard(new Vector3Int(3, 3, 1), new[] { Vector3Int.zero, Vector3Int.up, Vector3Int.up, Vector3Int.right });
            FillContent(7);
            SetMinimum(minimum);
            AssertEndpointOracle(Vector3Int.zero, Vector3Int.up, expected);
            AssertQuery(expected, Vector3Int.zero, expected ? Vector3Int.up : Vector3Int.zero);

            SetDirections(new[] { Vector3Int.right, Vector3Int.up });
            AssertQuery(expected, Vector3Int.zero, expected ? Vector3Int.right : Vector3Int.zero);

            Vector3Int longOffset = new Vector3Int(2, 0, 0);
            SetDirections(new[] { Vector3Int.zero, longOffset, longOffset, Vector3Int.up });
            AssertEndpointOracle(Vector3Int.zero, longOffset, expected);
            AssertQuery(expected, Vector3Int.zero, expected ? longOffset : Vector3Int.zero);
        }

        [TestCase(2, true)]
        [TestCase(3, false)]
        public void TryFindValidSwap_TwoUsableCellsDistinguishRawMinimumTwoFromThree(int minimum, bool expected)
        {
            GenerateBoard(new Vector3Int(2, 1, 1), new[] { Vector3Int.right });
            FillContent(7);
            SetMinimum(minimum);
            Assert.That(_field.Cells.Length, Is.EqualTo(2));
            Assert.That(_field.GetCell(0, 0).IsEnabled && _field.GetCell(0, 0).IsWalkable &&
                !_field.GetCell(0, 0).IsOccupied, Is.True);
            Assert.That(_field.GetCell(1, 0).IsEnabled && _field.GetCell(1, 0).IsWalkable &&
                !_field.GetCell(1, 0).IsOccupied, Is.True);
            AssertEndpointOracle(Vector3Int.zero, Vector3Int.right, expected);
            AssertQuery(expected, Vector3Int.zero, expected ? Vector3Int.right : Vector3Int.zero);
        }

        [TestCase(0)]
        [TestCase(-7)]
        public void TryFindValidSwap_NonpositiveOriginsRemainEligible(int content)
        {
            GenerateBoard(new Vector3Int(3, 3, 1), new[] { Vector3Int.zero, Vector3Int.up, Vector3Int.up, Vector3Int.right });
            FillContent(7);
            _field.GetCell(Vector3Int.zero).ContentId = content;
            AssertEndpointOracle(Vector3Int.zero, Vector3Int.up, true);
            AssertQuery(true, Vector3Int.zero, Vector3Int.up);
        }

        [TestCase(1, true)]
        [TestCase(2, false)]
        public void TryFindValidSwap_SinglePositiveTileDistinguishesRawMinimumOneFromTwo(int minimum, bool expected)
        {
            GenerateBoard(new Vector3Int(2, 1, 1), new[] { Vector3Int.right });
            _field.GetCell(0, 0).ContentId = 7;
            _field.GetCell(1, 0).ContentId = 0;
            SetMinimum(minimum);
            AssertEndpointOracle(Vector3Int.zero, Vector3Int.right, expected);
            AssertQuery(expected, Vector3Int.zero, expected ? Vector3Int.right : Vector3Int.zero);
        }

        [Test]
        public void TryFindValidSwap_PreservesDistinctReferencesWithEqualPublicPositions()
        {
            GenerateBoard(new Vector3Int(3, 3, 1), new[] { Vector3Int.up });
            FillContent(7);
            FieldCell origin = _field.GetCell(Vector3Int.zero);
            FieldCell neighbor = _field.GetCell(Vector3Int.up);
            Assert.That(ReferenceEquals(origin, neighbor), Is.False);
            neighbor.Position = Vector3Int.zero;
            // WHY: public positions may alias while the same-reference gate still admits this pair.
            AssertEndpointOracle(Vector3Int.zero, Vector3Int.zero, true);
            AssertQuery(true, Vector3Int.zero, Vector3Int.zero);
        }

        [Test]
        public void TryFindValidSwap_PreservesDepthSliceAndSparseBackingCells()
        {
            GenerateBoard(new Vector3Int(3, 2, 2), new[] { Vector3Int.down });
            SeedCanonical(1);
            _field.Cells[2, 1, 0] = null;
            _field.Config.Size = Vector3Int.one;
            Vector3Int a = new Vector3Int(1, 1, 1);
            Vector3Int b = new Vector3Int(1, 0, 1);
            Assert.That(Match3MatchFinder.FindMatches(_field, 3), Is.Empty);
            AssertEndpointOracle(a, b, true);
            AssertQuery(true, a, b);
        }

        [Test]
        public void TryFindValidSwap_ZeroEmptyRulesAndUnusableBoardsReturnDefault()
        {
            GenerateBoard(new Vector3Int(3, 3, 1), new[] { Vector3Int.zero, Vector3Int.zero });
            FillContent(7);
            AssertQuery(false, Vector3Int.zero, Vector3Int.zero);
            SetDirections(new Vector3Int[0]);
            AssertQuery(false, Vector3Int.zero, Vector3Int.zero);
            _field.Config.MovementRule = null;
            AssertQuery(false, Vector3Int.zero, Vector3Int.zero);
            _field.Config.MovementRule = new MovementRule(new[] { Vector3Int.right });
            foreach (FieldCell cell in _field.GetAllCells())
            {
                cell.IsOccupied = true;
            }
            AssertQuery(false, Vector3Int.zero, Vector3Int.zero);
        }

        private void GenerateBoard(Vector3Int size, Vector3Int[] directions)
        {
            _field.GenerateField(new FieldGeneratorConfig(size, new MovementRule(directions)));
            FillUnique();
        }

        private void FillUnique()
        {
            Vector3Int size = _field.GeneratedSize;
            int ordinal = 0;
            for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
            for (int z = 0; z < size.z; z++)
            {
                FieldCell cell = _field.Cells[x, y, z];
                if (cell != null)
                {
                    cell.ContentId = 1000 + ordinal;
                    cell.Type = ordinal % 4;
                    cell.Flags = ordinal % 2 == 0 ? FieldCellFlags.Reserved1 : FieldCellFlags.Reserved2;
                    cell.UserData = new object();
                }
                ordinal++;
            }
        }

        private void FillContent(int content)
        {
            foreach (FieldCell cell in _field.GetAllCells())
            {
                cell.ContentId = content;
            }
        }

        private void SeedCanonical(int z)
        {
            int[,] content = { { 1, 3, 1 }, { 2, 1, 2 } };
            for (int y = 0; y < 2; y++)
            for (int x = 0; x < 3; x++)
            {
                _field.Cells[x, y, z].ContentId = content[y, x];
            }
        }

        private void SetDirections(Vector3Int[] directions)
        {
            List<Vector3Int> live = _field.Config.MovementRule.Directions;
            live.Clear();
            live.AddRange(directions);
        }

        private void SetMinimum(int minimum)
        {
            JsonUtility.FromJsonOverwrite("{\"_minMatchLength\":" + minimum.ToString(CultureInfo.InvariantCulture) + "}", _service);
            MinimumSettings settings = JsonUtility.FromJson<MinimumSettings>(JsonUtility.ToJson(_service));
            Assert.That(settings._minMatchLength, Is.EqualTo(minimum));
            _minimum = minimum;
        }

        private void AssertQuery(bool expected, Vector3Int expectedA, Vector3Int expectedB)
        {
            BoardState before = new BoardState(_field, _service);
            int changedBefore = _changed;
            int resolvedBefore = _resolved;
            int shuffledBefore = _shuffled;
            int phasesBefore = _phases;
            bool found = _service.TryFindValidSwap(out Vector3Int a, out Vector3Int b);
            Assert.That(found, Is.EqualTo(expected));
            Assert.That(a, Is.EqualTo(expectedA));
            Assert.That(b, Is.EqualTo(expectedB));
            before.AssertUnchanged(_field, _service);
            Assert.That(_changed, Is.EqualTo(changedBefore));
            Assert.That(_resolved, Is.EqualTo(resolvedBefore));
            Assert.That(_shuffled, Is.EqualTo(shuffledBefore));
            Assert.That(_phases, Is.EqualTo(phasesBefore));
        }

        private void AssertEndpointOracle(Vector3Int a, Vector3Int b, bool expected)
        {
            BoardState before = new BoardState(_field, _service);
            FieldCell first = _field.GetCell(a);
            FieldCell second = _field.GetCell(b);
            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.Not.Null);
            int firstContent = first.ContentId;
            int secondContent = second.ContentId;
            bool endpointMatch = false;
            try
            {
                first.ContentId = secondContent;
                second.ContentId = firstContent;
                List<List<FieldCell>> matches = Match3MatchFinder.FindMatches(_field, _minimum);
                foreach (List<FieldCell> match in matches)
                {
                    if (match.Contains(first) || match.Contains(second))
                    {
                        endpointMatch = true;
                        break;
                    }
                }
            }
            finally
            {
                first.ContentId = firstContent;
                second.ContentId = secondContent;
            }
            Assert.That(endpointMatch, Is.EqualTo(expected));
            before.AssertUnchanged(_field, _service);
        }

        private void ObserveChanged() { _changed++; }
        private void ObserveResolved(int count) { _resolved++; }
        private void ObserveShuffled() { _shuffled++; }
        private void ObservePhase(Match3BoardService.Match3ResolvePhase phase, int count) { _phases++; }

        [Serializable]
        private sealed class MinimumSettings
        {
            public int _minMatchLength = int.MinValue;
        }

        private sealed class BoardState
        {
            private readonly FieldCell[,,] _array;
            private readonly FieldGeneratorConfig _config;
            private readonly MovementRule _rule;
            private readonly List<Vector3Int> _directions;
            private readonly Vector3Int[] _directionValues;
            private readonly List<Vector3Int> _disabled;
            private readonly List<Vector3Int> _forcedEnabled;
            private readonly List<Vector3Int> _blocked;
            private readonly List<Vector3Int> _forcedWalkable;
            private readonly GridShapeMask _shapeMask;
            private readonly FieldGenerator _singleton;
            private readonly Vector3Int _generated;
            private readonly string _configJson;
            private readonly string _serviceJson;
            private readonly bool _active;
            private readonly bool _fieldEnabled;
            private readonly bool _serviceEnabled;
            private readonly CellState[] _cells;

            public BoardState(FieldGenerator field, Match3BoardService service)
            {
                _array = field.Cells;
                _config = field.Config;
                _rule = _config.MovementRule;
                _directions = _rule == null ? null : _rule.Directions;
                _directionValues = _directions == null ? null : _directions.ToArray();
                _disabled = _config.DisabledCells;
                _forcedEnabled = _config.ForcedEnabledCells;
                _blocked = _config.BlockedCells;
                _forcedWalkable = _config.ForcedWalkableCells;
                _shapeMask = _config.ShapeMask;
                _singleton = FieldGenerator.I;
                _generated = field.GeneratedSize;
                _configJson = JsonUtility.ToJson(_config);
                _serviceJson = JsonUtility.ToJson(service);
                _active = field.gameObject.activeSelf;
                _fieldEnabled = field.enabled;
                _serviceEnabled = service.enabled;
                _cells = new CellState[_array.Length];
                int ordinal = 0;
                for (int x = 0; x < _array.GetLength(0); x++)
                for (int y = 0; y < _array.GetLength(1); y++)
                for (int z = 0; z < _array.GetLength(2); z++)
                {
                    _cells[ordinal++] = new CellState(_array[x, y, z]);
                }
            }

            public void AssertUnchanged(FieldGenerator field, Match3BoardService service)
            {
                Assert.That(ReferenceEquals(field.Cells, _array), Is.True);
                Assert.That(ReferenceEquals(field.Config, _config), Is.True);
                Assert.That(ReferenceEquals(_config.MovementRule, _rule), Is.True);
                if (_rule != null)
                {
                    Assert.That(ReferenceEquals(_rule.Directions, _directions), Is.True);
                    Assert.That(_directions, Is.EqualTo(_directionValues));
                }
                Assert.That(ReferenceEquals(_config.DisabledCells, _disabled), Is.True);
                Assert.That(ReferenceEquals(_config.ForcedEnabledCells, _forcedEnabled), Is.True);
                Assert.That(ReferenceEquals(_config.BlockedCells, _blocked), Is.True);
                Assert.That(ReferenceEquals(_config.ForcedWalkableCells, _forcedWalkable), Is.True);
                Assert.That(ReferenceEquals(_config.ShapeMask, _shapeMask), Is.True);
                Assert.That(ReferenceEquals(FieldGenerator.I, _singleton), Is.True);
                Assert.That(field.GeneratedSize, Is.EqualTo(_generated));
                Assert.That(JsonUtility.ToJson(_config), Is.EqualTo(_configJson));
                Assert.That(JsonUtility.ToJson(service), Is.EqualTo(_serviceJson));
                Assert.That(field.gameObject.activeSelf, Is.EqualTo(_active));
                Assert.That(field.enabled, Is.EqualTo(_fieldEnabled));
                Assert.That(service.enabled, Is.EqualTo(_serviceEnabled));
                int ordinal = 0;
                for (int x = 0; x < _array.GetLength(0); x++)
                for (int y = 0; y < _array.GetLength(1); y++)
                for (int z = 0; z < _array.GetLength(2); z++)
                {
                    _cells[ordinal++].AssertUnchanged(_array[x, y, z]);
                }
            }
        }

        private readonly struct CellState
        {
            private readonly FieldCell _cell;
            private readonly Vector3Int _position;
            private readonly int _type;
            private readonly int _content;
            private readonly bool _enabled;
            private readonly bool _walkable;
            private readonly bool _occupied;
            private readonly FieldCellFlags _flags;
            private readonly object _payload;

            public CellState(FieldCell cell)
            {
                _cell = cell;
                _position = cell == null ? default : cell.Position;
                _type = cell == null ? 0 : cell.Type;
                _content = cell == null ? 0 : cell.ContentId;
                _enabled = cell != null && cell.IsEnabled;
                _walkable = cell != null && cell.IsWalkable;
                _occupied = cell != null && cell.IsOccupied;
                _flags = cell == null ? FieldCellFlags.None : cell.Flags;
                _payload = cell == null ? null : cell.UserData;
            }

            public void AssertUnchanged(FieldCell cell)
            {
                Assert.That(ReferenceEquals(cell, _cell), Is.True);
                if (cell == null) { return; }
                Assert.That(cell.Position, Is.EqualTo(_position));
                Assert.That(cell.Type, Is.EqualTo(_type));
                Assert.That(cell.ContentId, Is.EqualTo(_content));
                Assert.That(cell.IsEnabled, Is.EqualTo(_enabled));
                Assert.That(cell.IsWalkable, Is.EqualTo(_walkable));
                Assert.That(cell.IsOccupied, Is.EqualTo(_occupied));
                Assert.That(cell.Flags, Is.EqualTo(_flags));
                Assert.That(ReferenceEquals(cell.UserData, _payload), Is.True);
            }
        }
    }
}
