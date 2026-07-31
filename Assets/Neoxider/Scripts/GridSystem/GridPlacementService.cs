using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.GridSystem
{
    /// <summary>What happens when a placement request hits an occupied cell.</summary>
    public enum GridOverwritePolicy
    {
        /// <summary>Occupied cells fail validation (subject to <see cref="GridPlacementRequest.RequireUnoccupied"/>).</summary>
        Reject = 0,

        /// <summary>Occupied cells are allowed and their content is replaced by the new entry.</summary>
        Overwrite = 1
    }

    /// <summary>
    ///     Reusable placement rule set for one <see cref="GridPlacementService"/> call: the anchor,
    ///     footprint entries, the standard cell requirements, an optional custom predicate, and the
    ///     overwrite policy. Gameplay services share this shape instead of growing new
    ///     <see cref="FieldGenerator"/> overloads.
    /// </summary>
    public sealed class GridPlacementRequest
    {
        /// <summary>Base cell the footprint offsets are relative to.</summary>
        public Vector3Int Anchor;

        /// <summary>Footprint cells (offset + content id). A single-cell request uses one entry.</summary>
        public IReadOnlyList<GridPlacementEntry> Entries;

        /// <summary>Reject disabled cells (holes, masked-out shape cells).</summary>
        public bool RequireEnabled = true;

        /// <summary>Reject non-walkable cells.</summary>
        public bool RequireWalkable = true;

        /// <summary>Reject occupied cells (ignored when <see cref="OverwritePolicy"/> is Overwrite).</summary>
        public bool RequireUnoccupied = true;

        /// <summary>Optional custom rule evaluated per target cell after the standard checks.</summary>
        public Func<FieldCell, bool> CellPredicate;

        /// <summary>Behaviour on occupied cells.</summary>
        public GridOverwritePolicy OverwritePolicy = GridOverwritePolicy.Reject;

        /// <summary>Raise <see cref="FieldGenerator.OnCellStateChanged"/> per written cell.</summary>
        public bool Notify = true;

        /// <summary>Convenience factory for the common single-cell request.</summary>
        public static GridPlacementRequest Single(Vector3Int anchor, int contentId, bool occupiesCell = true)
        {
            return new GridPlacementRequest
            {
                Anchor = anchor,
                Entries = new[] { new GridPlacementEntry(Vector3Int.zero, contentId, occupiesCell) }
            };
        }
    }

    /// <summary>
    ///     Rule-driven placement layer over the <see cref="FieldGenerator"/> placement API. Plain C#
    ///     (no MonoBehaviour): construct it with a generated field and feed it
    ///     <see cref="GridPlacementRequest"/> values. Validation covers bounds, enabled/walkable/
    ///     occupied state, the custom predicate, and the overwrite policy; a successful
    ///     <see cref="Place"/> writes content ids and occupancy exactly like
    ///     <see cref="FieldGenerator.PlaceContentFootprint"/>.
    /// </summary>
    public sealed class GridPlacementService
    {
        private readonly FieldGenerator _field;

        public GridPlacementService(FieldGenerator field)
        {
            _field = field != null ? field : throw new ArgumentNullException(nameof(field));
        }

        /// <summary>The field this service places into.</summary>
        public FieldGenerator Field => _field;

        /// <summary>True when every footprint cell satisfies the request rules.</summary>
        public bool CanPlace(GridPlacementRequest request)
        {
            return Validate(request, null, out _);
        }

        /// <summary>Same as <see cref="CanPlace"/> with a human-readable failure reason.</summary>
        public bool CanPlace(GridPlacementRequest request, out string failureReason)
        {
            return Validate(request, null, out failureReason);
        }

        /// <summary>
        ///     Validates and writes the footprint. On failure nothing is written and
        ///     <see cref="GridPlacementResult.FailureReason"/> explains why.
        /// </summary>
        public GridPlacementResult Place(GridPlacementRequest request)
        {
            var result = new GridPlacementResult();
            var resolved = new List<(GridPlacementEntry Entry, FieldCell Cell)>();
            if (!Validate(request, resolved, out string failureReason))
            {
                result.FailureReason = failureReason;
                return result;
            }

            foreach ((GridPlacementEntry entry, FieldCell cell) in resolved)
            {
                cell.ContentId = entry.ContentId;
                cell.IsOccupied = entry.OccupiesCell;
                result.Cells.Add(cell);
                result.Positions.Add(cell.Position);
                if (request.Notify)
                {
                    _field.OnCellStateChanged?.Invoke(cell);
                }
            }

            result.Placed = true;
            return result;
        }

        private bool Validate(
            GridPlacementRequest request,
            List<(GridPlacementEntry, FieldCell)> resolvedOrNull,
            out string failureReason)
        {
            failureReason = null;

            if (request == null)
            {
                failureReason = "Request is null.";
                return false;
            }

            if (request.Entries == null || request.Entries.Count == 0)
            {
                failureReason = "Request has no footprint entries.";
                return false;
            }

            for (int i = 0; i < request.Entries.Count; i++)
            {
                GridPlacementEntry entry = request.Entries[i];
                if (entry == null)
                {
                    failureReason = $"Entry {i} is null.";
                    return false;
                }

                Vector3Int position = request.Anchor + entry.Offset;
                FieldCell cell = _field.GetCell(position);
                if (cell == null)
                {
                    failureReason = $"Cell {position} is out of bounds or missing.";
                    return false;
                }

                if (request.RequireEnabled && !cell.IsEnabled)
                {
                    failureReason = $"Cell {position} is disabled.";
                    return false;
                }

                if (request.RequireWalkable && !cell.IsWalkable)
                {
                    failureReason = $"Cell {position} is not walkable.";
                    return false;
                }

                if (cell.IsOccupied &&
                    request.OverwritePolicy == GridOverwritePolicy.Reject &&
                    request.RequireUnoccupied)
                {
                    failureReason = $"Cell {position} is occupied.";
                    return false;
                }

                if (request.CellPredicate != null && !request.CellPredicate(cell))
                {
                    failureReason = $"Cell {position} was rejected by the custom predicate.";
                    return false;
                }

                resolvedOrNull?.Add((entry, cell));
            }

            return true;
        }
    }
}
