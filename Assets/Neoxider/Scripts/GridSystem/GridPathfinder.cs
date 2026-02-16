using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.GridSystem
{
    /// <summary>
    /// Describes why a path request failed.
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
    /// Contains input parameters for a pathfinding query.
    /// </summary>
    [Serializable]
    public class GridPathRequest
    {
        /// <summary>
        /// Start cell position in grid coordinates.
        /// </summary>
        public Vector3Int Start;
        /// <summary>
        /// End cell position in grid coordinates.
        /// </summary>
        public Vector3Int End;
        /// <summary>
        /// Optional movement directions override.
        /// </summary>
        public IEnumerable<Vector3Int> Directions;
        /// <summary>
        /// Ignores occupied state when evaluating passability.
        /// </summary>
        public bool IgnoreOccupied;
        /// <summary>
        /// Ignores enabled/disabled state when evaluating passability.
        /// </summary>
        public bool IgnoreDisabled;
        /// <summary>
        /// Ignores walkability flag when evaluating passability.
        /// </summary>
        public bool IgnoreWalkability;
        /// <summary>
        /// Optional custom passability callback evaluated per cell.
        /// </summary>
        public Func<FieldCell, bool> CustomPassabilityPredicate;
    }

    /// <summary>
    /// Contains result data for a pathfinding query.
    /// </summary>
    public class GridPathResult
    {
        /// <summary>
        /// Computed path from start to end (inclusive).
        /// </summary>
        public List<FieldCell> Path;
        /// <summary>
        /// Failure reason when no path is available.
        /// </summary>
        public NoPathReason Reason;
        /// <summary>
        /// Returns true when a non-empty path has been found.
        /// </summary>
        public bool HasPath => Path != null && Path.Count > 0;
    }

    /// <summary>
    /// Static BFS pathfinding service for <see cref="FieldGenerator"/>.
    /// </summary>
    public static class GridPathfinder
    {
        /// <summary>
        /// Finds a path using BFS and returns detailed result metadata.
        /// </summary>
        /// <param name="generator">Grid source.</param>
        /// <param name="request">Path request parameters.</param>
        /// <returns>Path result with either path data or failure reason.</returns>
        public static GridPathResult FindPath(FieldGenerator generator, GridPathRequest request)
        {
            if (generator == null || request == null)
            {
                return new GridPathResult { Reason = NoPathReason.InvalidStartOrEnd };
            }

            FieldCell startCell = generator.GetCell(request.Start);
            FieldCell endCell = generator.GetCell(request.End);
            if (startCell == null || endCell == null)
            {
                return new GridPathResult { Reason = NoPathReason.InvalidStartOrEnd };
            }

            if (!IsPassable(generator, startCell, request, true))
            {
                return new GridPathResult { Reason = NoPathReason.StartNotPassable };
            }

            if (!IsPassable(generator, endCell, request, true))
            {
                return new GridPathResult { Reason = NoPathReason.EndNotPassable };
            }

            Queue<FieldCell> queue = new();
            HashSet<FieldCell> visited = new();
            Dictionary<FieldCell, FieldCell> previous = new();

            queue.Enqueue(startCell);
            visited.Add(startCell);

            while (queue.Count > 0)
            {
                FieldCell current = queue.Dequeue();
                if (current == endCell)
                {
                    break;
                }

                foreach (FieldCell neighbor in generator.GetNeighbors(current, request.Directions))
                {
                    if (visited.Contains(neighbor) || !IsPassable(generator, neighbor, request, false))
                    {
                        continue;
                    }

                    visited.Add(neighbor);
                    previous[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }

            if (!visited.Contains(endCell))
            {
                return new GridPathResult { Reason = NoPathReason.NoPathFound };
            }

            List<FieldCell> path = new();
            FieldCell at = endCell;
            while (at != null && previous.ContainsKey(at))
            {
                path.Add(at);
                at = previous[at];
            }

            path.Add(startCell);
            path.Reverse();

            return new GridPathResult
            {
                Path = path,
                Reason = NoPathReason.None
            };
        }

        private static bool IsPassable(
            FieldGenerator generator,
            FieldCell cell,
            GridPathRequest request,
            bool isEndpoint)
        {
            if (cell == null)
            {
                return false;
            }

            if (!request.IgnoreDisabled && !cell.IsEnabled)
            {
                return false;
            }

            if (!request.IgnoreWalkability && !cell.IsWalkable)
            {
                return false;
            }

            if (!request.IgnoreOccupied && cell.IsOccupied && !isEndpoint)
            {
                return false;
            }

            if (request.CustomPassabilityPredicate != null && !request.CustomPassabilityPredicate(cell))
            {
                return false;
            }

            return generator.IsCellPassable(cell, request.IgnoreOccupied, request.IgnoreDisabled, request.IgnoreWalkability);
        }
    }
}
