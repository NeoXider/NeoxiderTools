using System.Collections.Generic;
using Neo.GridSystem;
using Neo.GridSystem.Match3;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests.GridSystem
{
    /// <summary>
    ///     EditMode timing benchmark for <see cref="Match3BoardService.TryFindValidSwap"/>, rejected-swap validation and
    ///     an accepted swap with its resolve cascade on the 2026-11-02 baseline boards. Prints one <c>NEO_PERF</c> line
    ///     per metric through <see cref="NeoPerf"/>; asserts outcomes only, never timing.
    /// </summary>
    /// <remarks>
    ///     The <c>legacy_emulated</c> rows re-run the pre-optimization algorithm (a full board copy per probed pair and
    ///     a neighbor list per cell) through public APIs, so they give a same-harness before/after for CPU and GC.Alloc
    ///     calls. <c>gc_allocs_per_sample</c> counts the same GC.Alloc events as the call column of
    ///     Docs/Audits/2026-11-02/Performance/MeasuredGridSelectorBaseline.md (one call per sample); the authoritative
    ///     timing comparison with that PlayMode Profiler baseline is the PlayMode producer
    ///     <c>Neo.Tests.Play.Audit20261102LibraryHotpathMeasurementTests</c>, not these EditMode Stopwatch numbers.
    /// </remarks>
    [TestFixture]
    [Category(NeoPerf.Category)]
    [Explicit(NeoPerf.ExplicitReason)]
    public sealed class Match3SearchPerformanceTests
    {
        private const int Samples = 120;
        private const int SlowSamples = 40;

        private readonly List<GameObject> _roots = new();

        [OneTimeSetUp]
        public void AdmitAllocationProbe()
        {
            NeoPerf.EmitCalibration("grid.match3", Samples);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject root in _roots)
            {
                if (root != null)
                {
                    Object.DestroyImmediate(root);
                }
            }

            _roots.Clear();
        }

        [TestCase(8)]
        [TestCase(16)]
        [TestCase(32)]
        public void TryFindValidSwap_BaselineBoards(int size)
        {
            Match3BoardService positive = CreateMeasuredBoard(size, true, out FieldGenerator positiveField);
            Match3BoardService control = CreateMeasuredBoard(size, false, out FieldGenerator controlField);
            Vector3Int expectedA = new Vector3Int(1, 1, 0);
            Vector3Int expectedB = new Vector3Int(1, 0, 0);
            Assert.That(LegacyFindValidSwap(positiveField, out Vector3Int legacyA, out Vector3Int legacyB), Is.True);
            Assert.That(legacyA, Is.EqualTo(expectedA));
            Assert.That(legacyB, Is.EqualTo(expectedB));
            Assert.That(LegacyFindValidSwap(controlField, out _, out _), Is.False);

            // WHY: the legacy 32x32 control costs about 80 ms per call, so the slow rows take fewer samples, as the
            // PlayMode producer does.
            int samples = size >= 32 ? SlowSamples : Samples;
            string grid = "grid.match3.find_valid_swap.n" + size;
            bool positiveFound = false;
            bool controlFound = true;
            NeoPerf.MeasureSamples(grid + ".positive", 1, samples,
                () => positiveFound = positive.TryFindValidSwap(out _, out _)).Emit();
            NeoPerf.MeasureSamples(grid + ".control", 1, samples,
                () => controlFound = control.TryFindValidSwap(out _, out _)).Emit();
            NeoPerf.MeasureSamples(grid + ".positive.legacy_emulated", 1, samples,
                () => LegacyFindValidSwap(positiveField, out _, out _)).Emit();
            NeoPerf.MeasureSamples(grid + ".control.legacy_emulated", 1, samples,
                () => LegacyFindValidSwap(controlField, out _, out _)).Emit();

            Assert.That(positiveFound, Is.True);
            Assert.That(controlFound, Is.False);
            Assert.That(positive.TryFindValidSwap(out Vector3Int a, out Vector3Int b), Is.True);
            Assert.That(a, Is.EqualTo(expectedA));
            Assert.That(b, Is.EqualTo(expectedB));
        }

        [TestCase(8)]
        [TestCase(32)]
        public void TrySwapAndResolve_RejectedSwap(int size)
        {
            Match3BoardService control = CreateMeasuredBoard(size, false, out FieldGenerator field);
            Vector3Int a = new Vector3Int(2, 3, 0);
            Vector3Int b = new Vector3Int(2, 2, 0);
            int before = field.GetCell(a).ContentId;
            bool accepted = true;

            NeoPerf.MeasureSamples("grid.match3.try_swap_and_resolve.rejected.n" + size, 1, Samples,
                () => accepted = control.TrySwapAndResolve(a, b)).Emit();

            Assert.That(accepted, Is.False);
            Assert.That(field.GetCell(a).ContentId, Is.EqualTo(before));
        }

        [TestCase(8)]
        [TestCase(32)]
        public void TrySwapAndResolve_AcceptedCascade(int size)
        {
            Match3BoardService service = CreateMeasuredBoard(size, true, out FieldGenerator field);
            int next = 0;
            service.RandomIndexProvider = count => next++ % count;
            int[] content = CaptureContent(field);
            Vector3Int a = new Vector3Int(1, 1, 0);
            Vector3Int b = new Vector3Int(1, 0, 0);
            bool accepted = true;

            // WHY: every sample restores the seeded board and the provider position, so each one resolves the same
            // swap and cascade; the restore is an O(cells) array copy included in the sample.
            NeoPerf.MeasureSamples("grid.match3.try_swap_and_resolve.accepted.n" + size, 1, Samples, () =>
                {
                    RestoreContent(field, content);
                    next = 0;
                    accepted &= service.TrySwapAndResolve(a, b);
                })
                .Add("role", "current")
                .Emit();

            Assert.That(accepted, Is.True);
        }

        private static int[] CaptureContent(FieldGenerator field)
        {
            FieldCell[,,] cells = field.Cells;
            int[] content = new int[cells.Length];
            int index = 0;
            for (int x = 0; x < cells.GetLength(0); x++)
            for (int y = 0; y < cells.GetLength(1); y++)
            for (int z = 0; z < cells.GetLength(2); z++)
            {
                FieldCell cell = cells[x, y, z];
                content[index++] = cell != null ? cell.ContentId : 0;
            }

            return content;
        }

        private static void RestoreContent(FieldGenerator field, int[] content)
        {
            FieldCell[,,] cells = field.Cells;
            int index = 0;
            for (int x = 0; x < cells.GetLength(0); x++)
            for (int y = 0; y < cells.GetLength(1); y++)
            for (int z = 0; z < cells.GetLength(2); z++)
            {
                FieldCell cell = cells[x, y, z];
                if (cell != null)
                {
                    cell.ContentId = content[index];
                }

                index++;
            }
        }

        private Match3BoardService CreateMeasuredBoard(int size, bool positive, out FieldGenerator field)
        {
            GameObject root = new GameObject("Match3SearchPerformance" + size + (positive ? "Positive" : "Control"));
            _roots.Add(root);
            root.SetActive(false);
            field = root.AddComponent<FieldGenerator>();
            field.DebugEnabled = false;
            Match3BoardService service = root.AddComponent<Match3BoardService>();
            JsonUtility.FromJsonOverwrite("{\"_autoGenerateOnStart\":false,\"_minMatchLength\":3}", service);
            root.SetActive(true);

            FieldGeneratorConfig config = new FieldGeneratorConfig(new Vector3Int(size, size, 1),
                new MovementRule(new[] { Vector3Int.down }));
            config.DisabledCells.Add(new Vector3Int(size - 1, size - 1, 0));
            config.BlockedCells.Add(new Vector3Int(size - 2, size - 1, 0));
            field.GenerateField(config);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                FieldCell cell = field.Cells[x, y, 0];
                cell.ContentId = 100 + y * size + x;
                cell.Type = (x + y) % 4;
                cell.Flags = (x + y) % 2 == 0 ? FieldCellFlags.Reserved1 : FieldCellFlags.Reserved2;
                cell.UserData = new object();
            }

            field.Cells[size - 1, size - 2, 0].IsOccupied = true;
            if (positive)
            {
                int[,] patch = { { 1, 3, 1 }, { 2, 1, 2 } };
                for (int y = 0; y < 2; y++)
                for (int x = 0; x < 3; x++)
                {
                    field.Cells[x, y, 0].ContentId = patch[y, x];
                }
            }

            return service;
        }

        /// <summary>
        ///     The search as it was before 2026-11-02: public cell and neighbor enumeration and a fresh full board copy for
        ///     every probed pair. Kept only as the same-harness "before" reference.
        /// </summary>
        private static bool LegacyFindValidSwap(FieldGenerator field, out Vector3Int a, out Vector3Int b)
        {
            a = default;
            b = default;
            foreach (FieldCell cell in field.GetAllCells(false))
            {
                if (!Usable(cell))
                {
                    continue;
                }

                foreach (FieldCell neighbor in field.GetNeighbors(cell))
                {
                    if (!Usable(neighbor) || ReferenceEquals(neighbor, cell))
                    {
                        continue;
                    }

                    LegacySnapshot snapshot = new LegacySnapshot(field);
                    if (snapshot.SwapCreatesMatch(cell.Position, neighbor.Position, 3))
                    {
                        a = cell.Position;
                        b = neighbor.Position;
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool Usable(FieldCell cell)
        {
            return cell != null && cell.IsEnabled && cell.IsWalkable && !cell.IsOccupied;
        }

        private sealed class LegacySnapshot
        {
            private readonly int[,,] _content;
            private readonly Vector3Int _size;
            private readonly bool[,,] _usable;

            public LegacySnapshot(FieldGenerator field)
            {
                _size = field.GeneratedSize;
                _usable = new bool[_size.x, _size.y, _size.z];
                _content = new int[_size.x, _size.y, _size.z];
                for (int z = 0; z < _size.z; z++)
                for (int y = 0; y < _size.y; y++)
                for (int x = 0; x < _size.x; x++)
                {
                    FieldCell cell = field.GetCell(new Vector3Int(x, y, z));
                    bool usable = Usable(cell);
                    _usable[x, y, z] = usable;
                    _content[x, y, z] = usable ? cell.ContentId : 0;
                }
            }

            public bool SwapCreatesMatch(Vector3Int a, Vector3Int b, int minimum)
            {
                if (!IsUsable(a) || !IsUsable(b))
                {
                    return false;
                }

                int temp = _content[a.x, a.y, a.z];
                _content[a.x, a.y, a.z] = _content[b.x, b.y, b.z];
                _content[b.x, b.y, b.z] = temp;
                return CreatesMatch(a, minimum) || CreatesMatch(b, minimum);
            }

            private bool CreatesMatch(Vector3Int position, int minimum)
            {
                int value = _content[position.x, position.y, position.z];
                if (value <= 0)
                {
                    return false;
                }

                return Count(position, Vector3Int.left, value) + Count(position, Vector3Int.right, value) + 1 >= minimum ||
                       Count(position, Vector3Int.down, value) + Count(position, Vector3Int.up, value) + 1 >= minimum;
            }

            private int Count(Vector3Int origin, Vector3Int direction, int value)
            {
                int count = 0;
                Vector3Int current = origin + direction;
                while (IsUsable(current) && _content[current.x, current.y, current.z] == value)
                {
                    count++;
                    current += direction;
                }

                return count;
            }

            private bool IsUsable(Vector3Int position)
            {
                return position.x >= 0 && position.x < _size.x && position.y >= 0 && position.y < _size.y &&
                       position.z >= 0 && position.z < _size.z && _usable[position.x, position.y, position.z];
            }
        }
    }
}
