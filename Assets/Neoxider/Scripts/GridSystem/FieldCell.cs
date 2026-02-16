using System;
using UnityEngine;

namespace Neo.GridSystem
{
    /// <summary>
    /// Bit flags for optional gameplay markers on a cell.
    /// </summary>
    [Flags]
    public enum FieldCellFlags
    {
        None = 0,
        SpawnPoint = 1 << 0,
        Goal = 1 << 1,
        Reserved1 = 1 << 2,
        Reserved2 = 1 << 3
    }

    /// <summary>
    ///     Описывает одну ячейку поля. Универсален для 2D/3D, поддерживает пользовательские типы и данные.
    /// </summary>
    [Serializable]
    public class FieldCell
    {
        /// <summary>
        /// Grid coordinates of this cell.
        /// </summary>
        public Vector3Int Position;

        /// <summary>
        /// Custom terrain/type identifier.
        /// </summary>
        public int Type;

        /// <summary>
        /// Whether this cell can be traversed.
        /// </summary>
        public bool IsWalkable;

        /// <summary>
        /// Whether this cell is enabled in current board shape.
        /// </summary>
        public bool IsEnabled;

        /// <summary>
        /// Whether this cell is currently occupied by gameplay object.
        /// </summary>
        public bool IsOccupied;

        /// <summary>
        /// Integer content state (for example Match3 tile or TicTacToe mark).
        /// </summary>
        public int ContentId;

        /// <summary>
        /// Optional gameplay markers.
        /// </summary>
        public FieldCellFlags Flags;

        /// <summary>
        /// Optional user payload for custom systems.
        /// </summary>
        public object UserData;

        /// <summary>
        /// Creates a grid cell with optional state values.
        /// </summary>
        /// <param name="position">Cell coordinates.</param>
        /// <param name="type">Custom terrain/type identifier.</param>
        /// <param name="isWalkable">Initial walkability flag.</param>
        /// <param name="userData">Optional user payload.</param>
        /// <param name="isEnabled">Initial shape enabled state.</param>
        /// <param name="isOccupied">Initial occupied state.</param>
        /// <param name="contentId">Initial content id.</param>
        /// <param name="flags">Initial gameplay flags.</param>
        public FieldCell(
            Vector3Int position,
            int type = 0,
            bool isWalkable = true,
            object userData = null,
            bool isEnabled = true,
            bool isOccupied = false,
            int contentId = -1,
            FieldCellFlags flags = FieldCellFlags.None)
        {
            Position = position;
            Type = type;
            IsWalkable = isWalkable;
            IsEnabled = isEnabled;
            IsOccupied = isOccupied;
            ContentId = contentId;
            Flags = flags;
            UserData = userData;
        }
    }
}