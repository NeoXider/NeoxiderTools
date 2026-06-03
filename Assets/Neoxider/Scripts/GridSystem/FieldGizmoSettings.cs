using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.GridSystem
{
    [Serializable]
    public class FieldGizmoSettings
    {
        public bool DrawGizmos = true;
        public bool DrawGrid = true;
        public bool DrawCells = true;
        public bool DrawCoordinates;
        public bool DrawPath;

        public Color GridColor = new(1f, 1f, 0f, 0.3f);
        public Color PathColor = Color.cyan;
        public Color BlockedCellColor = new(1f, 0f, 0f, 0.3f);
        public Color WalkableCellColor = new(0f, 1f, 0f, 0.25f);
        public Color DisabledCellColor = new(0.4f, 0.4f, 0.4f, 0.25f);
        public Color OccupiedCellColor = new(1f, 0.6f, 0f, 0.35f);
        public Color TextColor = Color.white;

        public float CellFillScale = 0.9f;
        public int TextFontSize = 14;
        public List<Vector3Int> DebugPath = new();
    }
}
