using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Neo.GridSystem
{
    /// <summary>
    ///     Компонент для визуализации сетки, путей и информации о ячейках через Gizmos и UnityEngine.Grid.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(FieldGenerator))]
    public class FieldDebugDrawer : MonoBehaviour
    {
        public Color GridColor = new(1f, 1f, 0f, 0.3f);
        public Color PathColor = Color.cyan;
        public Color BlockedCellColor = new(1f, 0f, 0f, 0.3f); // полупрозрачный красный
        public Color WalkableCellColor = new(0f, 1f, 0f, 0.3f); // полупрозрачный зелёный
        public Color CoordinatesColor = Color.white;
        public bool DrawCoordinates = true;
        public bool DrawPath;
        public List<Vector3Int> DebugPath = new();
        public Color TextColor = Color.white; // Новый параметр для цвета текста
        public int TextFontSize = 14; // Новый параметр для размера текста

        private FieldGenerator generator;
        private Grid unityGrid;

        private void OnDrawGizmos()
        {
            if (generator == null) generator = GetComponent<FieldGenerator>();
            if (unityGrid == null) unityGrid = generator.UnityGrid;
            if (!generator.DebugEnabled || generator.Cells == null || unityGrid == null) return;
            var size = generator.Config.Size;
            // Рисуем сетку
            Gizmos.color = GridColor;
            for (var x = 0; x <= size.x; x++)
            for (var y = 0; y <= size.y; y++)
            {
                var from = unityGrid.CellToWorld(new Vector3Int(x, 0, 0));
                var to = unityGrid.CellToWorld(new Vector3Int(x, size.y, 0));
                Gizmos.DrawLine(from, to);
                from = unityGrid.CellToWorld(new Vector3Int(0, y, 0));
                to = unityGrid.CellToWorld(new Vector3Int(size.x, y, 0));
                Gizmos.DrawLine(from, to);
            }

            // Рисуем ячейки
            for (var x = 0; x < size.x; x++)
            for (var y = 0; y < size.y; y++)
            for (var z = 0; z < size.z; z++)
            {
                var cell = generator.Cells[x, y, z];
                var pos = unityGrid.GetCellCenterWorld(cell.Position);
                Gizmos.color = cell.IsWalkable ? WalkableCellColor : BlockedCellColor;
                Gizmos.DrawCube(pos, unityGrid.cellSize * 0.9f);
#if UNITY_EDITOR
                if (DrawCoordinates)
                {
                    var style = new GUIStyle();
                    style.normal.textColor = TextColor;
                    style.fontSize = TextFontSize;
                    style.alignment = TextAnchor.MiddleCenter;
                    var labelPos = pos;
                    Handles.Label(labelPos, $"x:{cell.Position.x} y:{cell.Position.y} z:{cell.Position.z}", style);
                }
#endif
            }

            // Рисуем путь
            if (DrawPath && DebugPath.Count > 1)
            {
                Gizmos.color = PathColor;
                for (var i = 1; i < DebugPath.Count; i++)
                {
                    var a = generator.GetCellWorldCenter(DebugPath[i - 1]);
                    var b = generator.GetCellWorldCenter(DebugPath[i]);
                    Gizmos.DrawLine(a, b);
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            SceneView.RepaintAll();
        }
#endif
    }
}