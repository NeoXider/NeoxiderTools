using UnityEngine;

namespace Neo.GridSystem
{
    /// <summary>
    ///     One content entry in a multi-cell grid placement footprint.
    /// </summary>
    public sealed class GridPlacementEntry
    {
        public Vector3Int Offset;
        public int ContentId;
        public bool OccupiesCell;

        public GridPlacementEntry(Vector3Int offset, int contentId, bool occupiesCell = true)
        {
            Offset = offset;
            ContentId = contentId;
            OccupiesCell = occupiesCell;
        }
    }
}
