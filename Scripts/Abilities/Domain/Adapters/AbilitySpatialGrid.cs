using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Abilities
{
    /// <summary>
    ///     Uniform-grid snapshot of unit positions for fast radius queries in
    ///     <see cref="IAbilityWorldAdapter" /> implementations with hundreds of units and projectiles.
    ///     Fill it with <see cref="Add" /> (typically once per frame), then call <see cref="Query" /> as
    ///     often as needed; <see cref="Move" /> and <see cref="Remove" /> update single entries in place
    ///     between fills. Results keep the insertion order, so a grid-backed adapter returns the same
    ///     ids in the same order as a linear scan over the same snapshot. Pure C#; allocation-free once
    ///     its buffers have grown to the working set.
    /// </summary>
    public sealed class AbilitySpatialGrid
    {
        private const int CellBits = 21;
        private const long CellMask = (1L << CellBits) - 1L;

        // WHY: CellKey never sets the sign bit, so a negative key safely marks a removed entry.
        private const long RemovedKey = -1L;

        private static readonly Comparison<int> AscendingIndex = CompareIndices;

        private readonly Dictionary<long, int> _cellHeads = new Dictionary<long, int>(64);
        private readonly List<int> _hits = new List<int>(32);

        // WHY: built lazily by the first Move/Remove after a fill, so fills that never move or remove an
        // entry (the common per-frame snapshot) pay nothing for it.
        private readonly Dictionary<UnitId, int> _indexById = new Dictionary<UnitId, int>(64);
        private bool _indexBuilt;

        private UnitId[] _ids = new UnitId[64];
        private Vector3[] _positions = new Vector3[64];
        private long[] _keys = new long[64];
        private int[] _next = new int[64];
        private float _cellSize;
        private float _inverseCellSize;

        // WHY: occupied cell bounds clamp query ranges, so flat (2D or ground-plane) worlds scan one
        // layer on the unused axis instead of a full cube of empty cells.
        private int _minCellX;
        private int _maxCellX;
        private int _minCellY;
        private int _maxCellY;
        private int _minCellZ;
        private int _maxCellZ;

        /// <param name="cellSize">Cell edge in world units; about the typical query radius works well.</param>
        public AbilitySpatialGrid(float cellSize = 4f)
        {
            CellSize = cellSize;
        }

        /// <summary>Cell edge length in world units. Changing it clears the grid.</summary>
        public float CellSize
        {
            get => _cellSize;
            set
            {
                float size = float.IsNaN(value) || float.IsInfinity(value) || value <= 0f ? 4f : value;
                if (size.Equals(_cellSize))
                {
                    return;
                }

                _cellSize = size;
                _inverseCellSize = 1f / size;
                Clear();
            }
        }

        /// <summary>
        ///     Number of entries added since the last <see cref="Clear" />. Entries taken out with
        ///     <see cref="Remove" /> keep their slot (and stay counted) until the next <see cref="Clear" />.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>Removes every entry; keeps the buffers for the next fill.</summary>
        public void Clear()
        {
            _cellHeads.Clear();
            _indexBuilt = false;
            Count = 0;
        }

        /// <summary>
        ///     Adds one unit at a position and returns its entry index (its place in the insertion order).
        ///     The same id may be added once per fill.
        /// </summary>
        public int Add(UnitId id, Vector3 position)
        {
            if (Count == _ids.Length)
            {
                int capacity = _ids.Length * 2;
                Array.Resize(ref _ids, capacity);
                Array.Resize(ref _positions, capacity);
                Array.Resize(ref _keys, capacity);
                Array.Resize(ref _next, capacity);
            }

            int cellX = Cell(position.x);
            int cellY = Cell(position.y);
            int cellZ = Cell(position.z);
            if (Count == 0)
            {
                _minCellX = _maxCellX = cellX;
                _minCellY = _maxCellY = cellY;
                _minCellZ = _maxCellZ = cellZ;
            }
            else
            {
                IncludeCell(cellX, cellY, cellZ);
            }

            int index = Count++;
            _ids[index] = id;
            _positions[index] = position;
            Link(index, CellKey(cellX, cellY, cellZ));
            if (_indexBuilt)
            {
                _indexById[id] = index;
            }

            return index;
        }

        /// <summary>
        ///     Moves a unit added since the last <see cref="Clear" /> to <paramref name="position" /> without a
        ///     refill. The entry keeps its insertion index, so query order does not change, and later queries
        ///     return exactly what a grid refilled with the new position would return. Returns false when the
        ///     id was not added since the last <see cref="Clear" /> or was removed. Cost: O(1) plus the entries
        ///     of the old cell; the first <see cref="Move" /> or <see cref="Remove" /> after a fill also indexes
        ///     the ids once (O(<see cref="Count" />)). Allocation-free once warm.
        /// </summary>
        public bool Move(UnitId id, Vector3 position)
        {
            if (!TryGetLiveIndex(id, out int index))
            {
                return false;
            }

            int cellX = Cell(position.x);
            int cellY = Cell(position.y);
            int cellZ = Cell(position.z);
            IncludeCell(cellX, cellY, cellZ);
            long key = CellKey(cellX, cellY, cellZ);
            if (key != _keys[index])
            {
                Unlink(index);
                Link(index, key);
            }

            _positions[index] = position;
            return true;
        }

        /// <summary>
        ///     Takes a unit added since the last <see cref="Clear" /> out of the grid without a refill; later
        ///     queries skip it and keep the order of the remaining entries. Returns false when the id was not
        ///     added since the last <see cref="Clear" /> or was already removed. Same cost as <see cref="Move" />.
        /// </summary>
        public bool Remove(UnitId id)
        {
            if (!TryGetLiveIndex(id, out int index))
            {
                return false;
            }

            Unlink(index);
            _keys[index] = RemovedKey;
            // WHY: the plain-scan path of Query reads every slot; a NaN position never passes the radius test.
            _positions[index] = new Vector3(float.NaN, float.NaN, float.NaN);
            _indexById.Remove(id);
            return true;
        }

        /// <summary>
        ///     Appends every unit within <paramref name="radius" /> of <paramref name="point" /> (inclusive)
        ///     to <paramref name="results" />, in insertion order. Does not clear <paramref name="results" />.
        ///     A negative radius acts like its absolute value; NaN finds nothing. Membership is exactly
        ///     <see cref="IsWithinRadius" />, so any radius (including <see cref="float.MaxValue" /> and infinity)
        ///     and any coordinate return the same ids as a linear scan with that test over the same entries.
        /// </summary>
        public void Query(Vector3 point, float radius, List<UnitId> results)
        {
            if (results == null || Count == 0 || float.IsNaN(radius))
            {
                return;
            }

            if (radius < 0f)
            {
                radius = -radius;
            }

            double sqrRadius = (double)radius * radius;
            int minX = Math.Max(Cell(point.x - radius), _minCellX);
            int maxX = Math.Min(Cell(point.x + radius), _maxCellX);
            int minY = Math.Max(Cell(point.y - radius), _minCellY);
            int maxY = Math.Min(Cell(point.y + radius), _maxCellY);
            int minZ = Math.Max(Cell(point.z - radius), _minCellZ);
            int maxZ = Math.Min(Cell(point.z + radius), _maxCellZ);
            if (!float.IsInfinity(radius) && (minX > maxX || minY > maxY || minZ > maxZ))
            {
                return;
            }

            long spanX = (long)maxX - minX + 1L;
            long spanY = (long)maxY - minY + 1L;
            long spanZ = (long)maxZ - minZ + 1L;

            // WHY: a radius spanning more cells than there are units is cheaper as a plain scan.
            if (float.IsInfinity(radius) || spanX > Count || spanY > Count || spanZ > Count ||
                spanX * spanY * spanZ > Count)
            {
                for (int i = 0; i < Count; i++)
                {
                    if (SqrDistance(_positions[i], point) <= sqrRadius)
                    {
                        results.Add(_ids[i]);
                    }
                }

                return;
            }

            _hits.Clear();

            // WHY: long counters; a saturated bound of int.MaxValue would wrap an int counter forever.
            for (long x = minX; x <= maxX; x++)
            {
                for (long y = minY; y <= maxY; y++)
                {
                    for (long z = minZ; z <= maxZ; z++)
                    {
                        if (!_cellHeads.TryGetValue(CellKey(x, y, z), out int index))
                        {
                            continue;
                        }

                        while (index >= 0)
                        {
                            if (SqrDistance(_positions[index], point) <= sqrRadius)
                            {
                                _hits.Add(index);
                            }

                            index = _next[index];
                        }
                    }
                }
            }

            // WHY: insertion order keeps area effects identical to a linear scan (and deterministic). The
            // cached Comparison overload matters: in Unity's Mono, List.Sort() and Sort(IComparer) wrap the
            // comparer in a new Comparison delegate on every call, Sort(Comparison) does not.
            _hits.Sort(AscendingIndex);
            int previous = -1;
            for (int i = 0; i < _hits.Count; i++)
            {
                int hit = _hits[i];
                if (hit != previous)
                {
                    results.Add(_ids[hit]);
                    previous = hit;
                }
            }
        }

        /// <summary>
        ///     The radius test of <see cref="Query" />: true when <paramref name="position" /> lies within
        ///     <paramref name="radius" /> of <paramref name="center" /> (inclusive; a negative radius acts like its
        ///     absolute value; NaN never matches). Computed in double precision, so it stays exact where float
        ///     squares overflow (a unit at <see cref="float.MaxValue" /> is outside radius 3e38, inside
        ///     <see cref="float.MaxValue" />) and gives the same answer on every runtime. Use it in custom
        ///     <see cref="IAbilityWorldAdapter" /> scans that must agree with the grid.
        /// </summary>
        public static bool IsWithinRadius(Vector3 position, Vector3 center, float radius)
        {
            double r = radius;
            return SqrDistance(position, center) <= r * r;
        }

        /// <summary>
        ///     Squared distance between two points in double precision: always finite for finite inputs, NaN
        ///     when a coordinate is NaN. Rank candidates found with <see cref="IsWithinRadius" /> by this value
        ///     (nearest target, nearest-N) so far-away units stay comparable.
        /// </summary>
        public static double SqrDistance(Vector3 a, Vector3 b)
        {
            // WHY: float squared distances overflow to infinity beyond ~1.8e19 and some runtimes keep float
            // intermediates in double precision, so the same float expression can include a far unit on one
            // platform and exclude it on another. Doubles hold every float square exactly enough to compare.
            double dx = (double)a.x - b.x;
            double dy = (double)a.y - b.y;
            double dz = (double)a.z - b.z;
            return dx * dx + dy * dy + dz * dz;
        }

        // WHY: a huge finite radius (float.MaxValue as "everything") or a far-away position puts the cell
        // index outside the int range, where the double->int conversion is platform-defined (x64 yields
        // int.MinValue, ARM64 saturates). Saturating here keeps the cell range conservative everywhere, so
        // grid queries return exactly what a linear scan returns. NaN maps to cell 0; distance checks
        // reject it anyway.
        private int Cell(float coordinate)
        {
            double cell = Math.Floor((double)coordinate * _inverseCellSize);
            if (cell >= int.MaxValue)
            {
                return int.MaxValue;
            }

            if (cell <= int.MinValue)
            {
                return int.MinValue;
            }

            return double.IsNaN(cell) ? 0 : (int)cell;
        }

        private bool TryGetLiveIndex(UnitId id, out int index)
        {
            if (!_indexBuilt)
            {
                _indexById.Clear();
                for (int i = 0; i < Count; i++)
                {
                    if (_keys[i] != RemovedKey)
                    {
                        _indexById[_ids[i]] = i;
                    }
                }

                _indexBuilt = true;
            }

            return _indexById.TryGetValue(id, out index);
        }

        // WHY: bounds only grow until the next fill; a wider scan range is conservative, never wrong.
        private void IncludeCell(int cellX, int cellY, int cellZ)
        {
            _minCellX = Math.Min(_minCellX, cellX);
            _maxCellX = Math.Max(_maxCellX, cellX);
            _minCellY = Math.Min(_minCellY, cellY);
            _maxCellY = Math.Max(_maxCellY, cellY);
            _minCellZ = Math.Min(_minCellZ, cellZ);
            _maxCellZ = Math.Max(_maxCellZ, cellZ);
        }

        private void Link(int index, long key)
        {
            _keys[index] = key;
            _next[index] = _cellHeads.TryGetValue(key, out int head) ? head : -1;
            _cellHeads[key] = index;
        }

        private void Unlink(int index)
        {
            long key = _keys[index];
            if (!_cellHeads.TryGetValue(key, out int head))
            {
                return;
            }

            if (head == index)
            {
                if (_next[index] < 0)
                {
                    _cellHeads.Remove(key);
                }
                else
                {
                    _cellHeads[key] = _next[index];
                }

                return;
            }

            int previous = head;
            while (previous >= 0 && _next[previous] != index)
            {
                previous = _next[previous];
            }

            if (previous >= 0)
            {
                _next[previous] = _next[index];
            }
        }

        private static int CompareIndices(int a, int b)
        {
            return a.CompareTo(b);
        }

        private static long CellKey(long x, long y, long z)
        {
            return ((x & CellMask) << (CellBits * 2)) | ((y & CellMask) << CellBits) | (z & CellMask);
        }
    }
}
