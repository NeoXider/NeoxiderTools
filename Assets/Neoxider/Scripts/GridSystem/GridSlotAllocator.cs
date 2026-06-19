using System.Collections.Generic;
using UnityEngine;

namespace Neo.GridSystem
{
    /// <summary>
    ///     Lightweight allocator for one-cell slots on top of FieldGenerator.
    ///     Useful for benches, tactical rows, inventory hotbars, and autobattler boards.
    /// </summary>
    public sealed class GridSlotAllocator
    {
        private static readonly GridPlacementEntry[] SingleCellEntry =
        {
            new(Vector3Int.zero, 0)
        };

        private readonly FieldGenerator _field;

        public GridSlotAllocator(FieldGenerator field)
        {
            _field = field;
        }

        public FieldGenerator Field => _field;

        public bool TryGetSlotPosition(int slotIndex, out Vector3Int position)
        {
            position = default;
            if (!CanUseLinearSlots() || slotIndex < 0)
            {
                return false;
            }

            Vector3Int size = _field.Config.Size;
            int width = size.x;
            int height = size.y;
            if (width <= 0 || height <= 0)
            {
                return false;
            }

            int slotCount = width * height;
            if (slotIndex >= slotCount)
            {
                return false;
            }

            position = new Vector3Int(slotIndex % width, slotIndex / width, 0);
            return _field.InBounds(position);
        }

        public bool TryGetSlotIndex(Vector3Int position, out int slotIndex)
        {
            slotIndex = -1;
            if (!CanUseLinearSlots() || position.z != 0 || !_field.InBounds(position))
            {
                return false;
            }

            Vector3Int size = _field.Config.Size;
            if (size.x <= 0 || size.y <= 0)
            {
                return false;
            }

            slotIndex = position.y * size.x + position.x;
            return true;
        }

        private bool CanUseLinearSlots()
        {
            return _field != null
                   && _field.Config != null
                   && _field.Config.GridType == GridType.Rectangular
                   && _field.Config.Size.x > 0
                   && _field.Config.Size.y > 0
                   && _field.Config.Size.z == 1;
        }

        public bool IsAvailable(Vector3Int position)
        {
            FieldCell cell = _field != null ? _field.GetCell(position) : null;
            return cell != null && cell.IsEnabled && cell.IsWalkable && !cell.IsOccupied;
        }

        public bool IsAvailable(int slotIndex)
        {
            return TryGetSlotPosition(slotIndex, out Vector3Int position) && IsAvailable(position);
        }

        public bool TryFindFirstAvailable(IEnumerable<Vector3Int> preferredPositions, out Vector3Int position)
        {
            position = default;
            if (_field == null || preferredPositions == null)
            {
                return false;
            }

            foreach (Vector3Int preferredPosition in preferredPositions)
            {
                if (!IsAvailable(preferredPosition))
                {
                    continue;
                }

                position = preferredPosition;
                return true;
            }

            return false;
        }

        public bool TryAllocateFirstAvailable(
            IEnumerable<Vector3Int> preferredPositions,
            int contentId,
            out Vector3Int position,
            out GridPlacementResult result)
        {
            result = null;
            if (!TryFindFirstAvailable(preferredPositions, out position))
            {
                return false;
            }

            result = Allocate(position, contentId);
            return result != null && result.Placed;
        }

        public GridPlacementResult Allocate(Vector3Int position, int contentId)
        {
            SingleCellEntry[0].ContentId = contentId;
            return _field != null
                ? _field.PlaceContentFootprint(position, SingleCellEntry)
                : new GridPlacementResult { FailureReason = "Field is null." };
        }

        public GridPlacementResult Allocate(int slotIndex, int contentId)
        {
            return TryGetSlotPosition(slotIndex, out Vector3Int position)
                ? Allocate(position, contentId)
                : new GridPlacementResult { FailureReason = "Invalid slot index." };
        }

        public bool Release(Vector3Int position, int emptyContentId = -1, bool notify = true)
        {
            FieldCell cell = _field != null ? _field.GetCell(position) : null;
            if (cell == null)
            {
                return false;
            }

            cell.ContentId = emptyContentId;
            cell.IsOccupied = false;
            if (notify)
            {
                _field.OnCellStateChanged.Invoke(cell);
            }

            return true;
        }
    }
}
