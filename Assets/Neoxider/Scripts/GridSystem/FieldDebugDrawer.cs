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
    [NeoDoc("GridSystem/FieldDebugDrawer.md")]
    [RequireComponent(typeof(FieldGenerator))]
    [AddComponentMenu("Neoxider/" + "GridSystem/" + nameof(FieldDebugDrawer))]
    public class FieldDebugDrawer : MonoBehaviour
    {
        [Header("Colors")] public Color GridColor = new(1f, 1f, 0f, 0.3f);
        public Color PathColor = Color.cyan;
        public Color BlockedCellColor = new(1f, 0f, 0f, 0.3f); // полупрозрачный красный
        public Color WalkableCellColor = new(0f, 1f, 0f, 0.3f); // полупрозрачный зелёный
        public Color DisabledCellColor = new(0.4f, 0.4f, 0.4f, 0.25f);
        public Color OccupiedCellColor = new(1f, 0.6f, 0f, 0.35f);
        public Color CoordinatesColor = Color.white;

        [Header("Settings")] public bool DrawCoordinates = true;
        public bool DrawPath;

        [Header("Debug")] public List<Vector3Int> DebugPath = new();

        [Header("Text")] public Color TextColor = Color.white; // Новый параметр для цвета текста
        public int TextFontSize = 14; // Новый параметр для размера текста

        private FieldGenerator generator;
        private Grid unityGrid;

        private void OnDrawGizmos()
        {
            if (generator == null)
            {
                generator = GetComponent<FieldGenerator>();
            }

            if (unityGrid == null)
            {
                unityGrid = generator.UnityGrid;
            }

            if (!generator.DebugEnabled || generator.Cells == null || unityGrid == null)
            {
                return;
            }

            Vector3Int size = generator.Config.Size;
            // Рисуем сетку
            Gizmos.color = GridColor;
            for (int x = 0; x <= size.x; x++)
            for (int y = 0; y <= size.y; y++)
            {
                Vector3 from = generator.GetCellCornerWorld(new Vector3Int(x, 0, 0));
                Vector3 to = generator.GetCellCornerWorld(new Vector3Int(x, size.y, 0));
                Gizmos.DrawLine(from, to);
                from = generator.GetCellCornerWorld(new Vector3Int(0, y, 0));
                to = generator.GetCellCornerWorld(new Vector3Int(size.x, y, 0));
                Gizmos.DrawLine(from, to);
            }

            // Рисуем ячейки
            for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
            for (int z = 0; z < size.z; z++)
            {
                FieldCell cell = generator.Cells[x, y, z];
                if (cell == null)
                {
                    continue;
                }

                Vector3 pos = generator.GetCellWorldCenter(cell.Position);
                if (!cell.IsEnabled)
                {
                    Gizmos.color = DisabledCellColor;
                }
                else if (cell.IsOccupied)
                {
                    Gizmos.color = OccupiedCellColor;
                }
                else
                {
                    Gizmos.color = cell.IsWalkable ? WalkableCellColor : BlockedCellColor;
                }

                Gizmos.DrawCube(pos, unityGrid.cellSize * 0.9f);
#if UNITY_EDITOR
                if (DrawCoordinates)
                {
                    GUIStyle style = new();
                    style.normal.textColor = TextColor;
                    style.fontSize = TextFontSize;
                    style.alignment = TextAnchor.MiddleCenter;
                    Vector3 labelPos = pos;
                    Handles.Label(labelPos, $"x:{cell.Position.x} y:{cell.Position.y} z:{cell.Position.z}", style);
                }
#endif
            }

            // Рисуем путь
            if (DrawPath && DebugPath.Count > 1)
            {
                Gizmos.color = PathColor;
                for (int i = 1; i < DebugPath.Count; i++)
                {
                    Vector3 a = generator.GetCellWorldCenter(DebugPath[i - 1]);
                    Vector3 b = generator.GetCellWorldCenter(DebugPath[i]);
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
