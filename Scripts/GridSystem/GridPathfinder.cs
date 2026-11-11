using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.GridSystem
{
    /// <summary>
    ///     Describes why a path request failed.
    /// </summary>
    public enum NoPathReason
    {
        None,
        InvalidStartOrEnd,
        StartNotPassable,
        EndNotPassable,
        NoPathFound
    }

    /// <summary>
    ///     Contains input parameters for a pathfinding query.
    /// </summary>
    [Serializable]
    public class GridPathRequest
    {
        /// <summary>
        ///     Start cell position in grid coordinates.
        /// </summary>
        public Vector3Int Start;

        /// <summary>
        ///     End cell position in grid coordinates. Ignored by reachability queries.
        /// </summary>
        public Vector3Int End;

        /// <summary>
        ///     Ignores occupied state when evaluating passability.
        /// </summary>
        public bool IgnoreOccupied;

        /// <summary>
        ///     Ignores enabled/disabled state when evaluating passability.
        /// </summary>
        public bool IgnoreDisabled;

        /// <summary>
        ///     Ignores walkability flag when evaluating passability.
        /// </summary>
        public bool IgnoreWalkability;

        /// <summary>
        ///     Optional custom passability callback evaluated per cell. It should only inspect state; the search may
        ///     evaluate each cell at most once per query.
        /// </summary>
        public Func<FieldCell, bool> CustomPassabilityPredicate;

        /// <summary>
        ///     Optional movement directions override. When null the field <see cref="FieldGeneratorConfig.MovementRule" />
        ///     is used.
        /// </summary>
        public IEnumerable<Vector3Int> Directions;
    }

    /// <summary>
    ///     Contains result data for a pathfinding query.
    /// </summary>
    public class GridPathResult
    {
        /// <summary>
        ///     Computed path from start to end (inclusive).
        /// </summary>
        public List<FieldCell> Path;

        /// <summary>
        ///     Failure reason when no path is available.
        /// </summary>
        public NoPathReason Reason;

        /// <summary>
        ///     Returns true when a non-empty path has been found.
        /// </summary>
        public bool HasPath => Path != null && Path.Count > 0;
    }

    /// <summary>
    ///     Static breadth-first pathfinding service for <see cref="FieldGenerator" />. Every move costs one step, so the
    ///     returned path has the fewest moves under the request's movement directions and passability rules.
    /// </summary>
    /// <remarks>
    ///     Searches run over flat index buffers that are reused between calls, so a warm
    ///     <see cref="TryFindPath" /> or <see cref="CollectReachableCells" /> call with a caller-owned list allocates
    ///     nothing. Re-entrant calls (for example from a passability predicate) get their own temporary buffers.
    /// </remarks>
    public static class GridPathfinder
    {
        [ThreadStatic] private static SearchBuffers s_buffers;

        // WHY: with domain reload disabled the buffers of the previous play session (sized for its largest board)
        // would otherwise stay referenced forever.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            s_buffers = null;
        }

        /// <summary>
        ///     Finds a path using BFS and returns detailed result metadata.
        /// </summary>
        /// <param name="generator">Grid source.</param>
        /// <param name="request">Path request parameters.</param>
        /// <returns>Path result with either path data or failure reason. The path list is never null.</returns>
        public static GridPathResult FindPath(FieldGenerator generator, GridPathRequest request)
        {
            List<FieldCell> path = new List<FieldCell>();
            TryFindPath(generator, request, path, out NoPathReason reason);
            return new GridPathResult
            {
                Path = path,
                Reason = reason
            };
        }

        /// <summary>
        ///     Non-allocating variant of <see cref="FindPath" />: clears <paramref name="path" /> and fills it with the
        ///     cells from start to end (inclusive) when a path exists.
        /// </summary>
        /// <param name="generator">Grid source.</param>
        /// <param name="request">Path request parameters.</param>
        /// <param name="path">Caller-owned output list. Reuse it between calls to avoid allocations.</param>
        /// <param name="reason"><see cref="NoPathReason.None" /> on success; otherwise the failure reason.</param>
        /// <returns>True when a path was found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path" /> is null.</exception>
        public static bool TryFindPath(
            FieldGenerator generator,
            GridPathRequest request,
            List<FieldCell> path,
            out NoPathReason reason)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            path.Clear();
            if (generator == null || request == null)
            {
                reason = NoPathReason.InvalidStartOrEnd;
                return false;
            }

            FieldCell startCell = generator.GetCell(request.Start);
            FieldCell endCell = generator.GetCell(request.End);
            if (startCell == null || endCell == null)
            {
                reason = NoPathReason.InvalidStartOrEnd;
                return false;
            }

            if (!IsPassable(generator, startCell, request))
            {
                reason = NoPathReason.StartNotPassable;
                return false;
            }

            if (!IsPassable(generator, endCell, request))
            {
                reason = NoPathReason.EndNotPassable;
                return false;
            }

            if (ReferenceEquals(startCell, endCell))
            {
                path.Add(startCell);
                reason = NoPathReason.None;
                return true;
            }

            FieldCell[,,] cells = generator.Cells;
            Vector3Int size = generator.GeneratedSize;
            SearchBuffers buffers = AcquireBuffers(cells.Length);
            try
            {
                ResolveDirections(generator, request.Directions, buffers.Directions);
                int startIndex = ToIndex(request.Start, size);
                int endIndex = ToIndex(request.End, size);
                if (!Search(generator, request, cells, size, buffers, startIndex, endIndex, -1, null, null))
                {
                    reason = NoPathReason.NoPathFound;
                    return false;
                }

                int[] parent = buffers.Parent;
                int index = endIndex;
                while (true)
                {
                    path.Add(GetCellAt(cells, size, index));
                    if (index == startIndex)
                    {
                        break;
                    }

                    index = parent[index];
                }

                for (int left = 0, right = path.Count - 1; left < right; left++, right--)
                {
                    FieldCell swap = path[left];
                    path[left] = path[right];
                    path[right] = swap;
                }

                reason = NoPathReason.None;
                return true;
            }
            finally
            {
                ReleaseBuffers(buffers);
            }
        }

        /// <summary>
        ///     Collects every cell reachable from <see cref="GridPathRequest.Start" /> within
        ///     <paramref name="maxSteps" /> moves, in breadth-first order (non-decreasing step count). Useful for
        ///     tactics movement ranges, flood fills and region queries. <see cref="GridPathRequest.End" /> is ignored.
        /// </summary>
        /// <remarks>
        ///     The origin cell is always the first result and is not checked for passability, because a unit usually
        ///     occupies the cell it moves from. Every other result satisfies the request's passability rules.
        /// </remarks>
        /// <param name="generator">Grid source.</param>
        /// <param name="request">Origin, passability flags, predicate and optional directions.</param>
        /// <param name="maxSteps">Maximum number of moves. Negative means unlimited; zero returns only the origin.</param>
        /// <param name="results">Caller-owned output list; cleared first.</param>
        /// <param name="stepCounts">Optional caller-owned list receiving the step count of each result; cleared first.</param>
        /// <returns>Number of collected cells, or zero when the origin is not a generated cell.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="results" /> is null.</exception>
        public static int CollectReachableCells(
            FieldGenerator generator,
            GridPathRequest request,
            int maxSteps,
            List<FieldCell> results,
            List<int> stepCounts = null)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            results.Clear();
            stepCounts?.Clear();
            if (generator == null || request == null)
            {
                return 0;
            }

            FieldCell originCell = generator.GetCell(request.Start);
            if (originCell == null)
            {
                return 0;
            }

            results.Add(originCell);
            stepCounts?.Add(0);
            if (maxSteps == 0)
            {
                return results.Count;
            }

            FieldCell[,,] cells = generator.Cells;
            Vector3Int size = generator.GeneratedSize;
            SearchBuffers buffers = AcquireBuffers(cells.Length);
            try
            {
                ResolveDirections(generator, request.Directions, buffers.Directions);
                Search(generator, request, cells, size, buffers, ToIndex(request.Start, size), -1, maxSteps, results,
                    stepCounts);
            }
            finally
            {
                ReleaseBuffers(buffers);
            }

            return results.Count;
        }

        private static bool Search(
            FieldGenerator generator,
            GridPathRequest request,
            FieldCell[,,] cells,
            Vector3Int size,
            SearchBuffers buffers,
            int startIndex,
            int endIndex,
            int maxSteps,
            List<FieldCell> reached,
            List<int> reachedSteps)
        {
            List<Vector3Int> directions = buffers.Directions;
            int directionCount = directions.Count;
            int[] visit = buffers.Visit;
            int[] parent = buffers.Parent;
            int[] queue = buffers.Queue;
            int[] depth = buffers.Depth;
            int stamp = buffers.NextStamp();
            int head = 0;
            int tail = 0;

            visit[startIndex] = stamp;
            parent[startIndex] = startIndex;
            depth[startIndex] = 0;
            queue[tail++] = startIndex;

            while (head < tail)
            {
                int current = queue[head++];
                int currentDepth = depth[current];
                if (maxSteps >= 0 && currentDepth >= maxSteps)
                {
                    continue;
                }

                int x = current % size.x;
                int rest = current / size.x;
                int y = rest % size.y;
                int z = rest / size.y;

                for (int d = 0; d < directionCount; d++)
                {
                    Vector3Int direction = directions[d];
                    int nx = x + direction.x;
                    int ny = y + direction.y;
                    int nz = z + direction.z;
                    if (nx < 0 || nx >= size.x || ny < 0 || ny >= size.y || nz < 0 || nz >= size.z)
                    {
                        continue;
                    }

                    int next = nx + size.x * (ny + size.y * nz);
                    if (visit[next] == stamp)
                    {
                        continue;
                    }

                    // WHY: passability does not depend on the cell we arrive from, so a rejected cell is stamped too
                    // and its predicate is never evaluated twice in one query.
                    visit[next] = stamp;
                    FieldCell neighbor = cells[nx, ny, nz];
                    if (neighbor == null || !IsPassable(generator, neighbor, request))
                    {
                        parent[next] = -1;
                        continue;
                    }

                    parent[next] = current;
                    depth[next] = currentDepth + 1;
                    if (next == endIndex)
                    {
                        return true;
                    }

                    queue[tail++] = next;
                    if (reached != null)
                    {
                        reached.Add(neighbor);
                        reachedSteps?.Add(currentDepth + 1);
                    }
                }
            }

            return false;
        }

        private static void ResolveDirections(
            FieldGenerator generator,
            IEnumerable<Vector3Int> overrideDirections,
            List<Vector3Int> output)
        {
            output.Clear();
            if (generator.Config == null)
            {
                return;
            }

            IEnumerable<Vector3Int> source = overrideDirections ?? generator.Config.MovementRule?.Directions;
            if (source == null)
            {
                return;
            }

            if (source is IReadOnlyList<Vector3Int> list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    AddDirection(output, list[i]);
                }

                return;
            }

            foreach (Vector3Int direction in source)
            {
                AddDirection(output, direction);
            }
        }

        private static void AddDirection(List<Vector3Int> output, Vector3Int direction)
        {
            if (direction != Vector3Int.zero)
            {
                output.Add(direction);
            }
        }

        private static int ToIndex(Vector3Int position, Vector3Int size)
        {
            return position.x + size.x * (position.y + size.y * position.z);
        }

        private static FieldCell GetCellAt(FieldCell[,,] cells, Vector3Int size, int index)
        {
            int x = index % size.x;
            int rest = index / size.x;
            return cells[x, rest % size.y, rest / size.y];
        }

        private static bool IsPassable(
            FieldGenerator generator,
            FieldCell cell,
            GridPathRequest request)
        {
            return generator.IsCellPassable(cell, request.IgnoreOccupied, request.IgnoreDisabled,
                       request.IgnoreWalkability) &&
                   (request.CustomPassabilityPredicate == null || request.CustomPassabilityPredicate(cell));
        }

        private static SearchBuffers AcquireBuffers(int cellCount)
        {
            SearchBuffers buffers = s_buffers;
            if (buffers == null)
            {
                buffers = new SearchBuffers();
                s_buffers = buffers;
            }
            else if (buffers.InUse)
            {
                buffers = new SearchBuffers();
            }

            buffers.InUse = true;
            buffers.EnsureCapacity(cellCount);
            return buffers;
        }

        private static void ReleaseBuffers(SearchBuffers buffers)
        {
            buffers.InUse = false;
        }

        private sealed class SearchBuffers
        {
            public readonly List<Vector3Int> Directions = new List<Vector3Int>(8);
            public int[] Depth = Array.Empty<int>();
            public bool InUse;
            public int[] Parent = Array.Empty<int>();
            public int[] Queue = Array.Empty<int>();
            public int[] Visit = Array.Empty<int>();
            private int _stamp;

            public void EnsureCapacity(int cellCount)
            {
                if (Visit.Length >= cellCount)
                {
                    return;
                }

                Visit = new int[cellCount];
                Parent = new int[cellCount];
                Queue = new int[cellCount];
                Depth = new int[cellCount];
                _stamp = 0;
            }

            public int NextStamp()
            {
                if (_stamp == int.MaxValue)
                {
                    Array.Clear(Visit, 0, Visit.Length);
                    _stamp = 0;
                }

                return ++_stamp;
            }
        }
    }
}
