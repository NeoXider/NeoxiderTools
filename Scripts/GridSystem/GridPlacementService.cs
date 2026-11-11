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

        // WHY: CanPlace runs per frame for previews and once per anchor in placement searches; reusing the duplicate
        // set keeps those checks allocation-free. A predicate that re-enters the service gets a temporary set.
        private readonly HashSet<Vector3Int> _positionScratch = new HashSet<Vector3Int>();
        private bool _positionScratchInUse;

        /// <summary>Creates a placement service over a generated field.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="field"/> is null.</exception>
        public GridPlacementService(FieldGenerator field)
        {
            _field = field != null ? field : throw new ArgumentNullException(nameof(field));
        }

        /// <summary>The field this service places into.</summary>
        public FieldGenerator Field => _field;

        /// <summary>
        ///     True when every footprint cell satisfies the request rules. Allocation-free unless the custom predicate
        ///     allocates.
        /// </summary>
        public bool CanPlace(GridPlacementRequest request)
        {
            return Validate(request, null, false, out _);
        }

        /// <summary>Same as <see cref="CanPlace(GridPlacementRequest)"/> with a human-readable failure reason.</summary>
        public bool CanPlace(GridPlacementRequest request, out string failureReason)
        {
            return Validate(request, null, true, out failureReason);
        }

        /// <summary>
        ///     Validates and writes the footprint. On failure nothing is written and
        ///     <see cref="GridPlacementResult.FailureReason"/> explains why.
        /// </summary>
        public GridPlacementResult Place(GridPlacementRequest request)
        {
            GridPlacementResult result = new GridPlacementResult();
            List<(GridPlacementEntry Entry, FieldCell Cell)> resolved = new List<(GridPlacementEntry Entry, FieldCell Cell)>();
            if (!Validate(request, resolved, true, out string failureReason))
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
            }

            if (request.Notify)
            {
                foreach (FieldCell cell in result.Cells)
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
            bool describeFailure,
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

            bool sharedPositions = !_positionScratchInUse;
            HashSet<Vector3Int> positions = sharedPositions ? _positionScratch : new HashSet<Vector3Int>();
            _positionScratchInUse = true;
            try
            {
                for (int i = 0; i < request.Entries.Count; i++)
                {
                    GridPlacementEntry entry = request.Entries[i];
                    if (entry == null)
                    {
                        failureReason = describeFailure ? $"Entry {i} is null." : null;
                        return false;
                    }

                    if (!FieldGenerator.TryAddCoordinates(request.Anchor, entry.Offset, out Vector3Int position))
                    {
                        failureReason = describeFailure ? $"Footprint entry {i} exceeds the coordinate range." : null;
                        return false;
                    }

                    if (!positions.Add(position))
                    {
                        failureReason = describeFailure ? $"Footprint contains duplicate cell {position}." : null;
                        return false;
                    }

                    FieldCell cell = _field.GetCell(position);
                    if (cell == null)
                    {
                        failureReason = describeFailure ? $"Cell {position} is out of bounds or missing." : null;
                        return false;
                    }

                    if (request.RequireEnabled && !cell.IsEnabled)
                    {
                        failureReason = describeFailure ? $"Cell {position} is disabled." : null;
                        return false;
                    }

                    if (request.RequireWalkable && !cell.IsWalkable)
                    {
                        failureReason = describeFailure ? $"Cell {position} is not walkable." : null;
                        return false;
                    }

                    if (cell.IsOccupied &&
                        request.OverwritePolicy == GridOverwritePolicy.Reject &&
                        request.RequireUnoccupied)
                    {
                        failureReason = describeFailure ? $"Cell {position} is occupied." : null;
                        return false;
                    }

                    if (request.CellPredicate != null && !request.CellPredicate(cell))
                    {
                        failureReason = describeFailure
                            ? $"Cell {position} was rejected by the custom predicate."
                            : null;
                        return false;
                    }

                    resolvedOrNull?.Add((entry, cell));
                }

                return true;
            }
            finally
            {
                positions.Clear();
                if (sharedPositions)
                {
                    _positionScratchInUse = false;
                }
            }
        }
    }
}
