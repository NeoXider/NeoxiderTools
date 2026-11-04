using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Neo.GridSystem
{
    /// <summary>
    ///     Draws grid, paths, and cell info with Gizmos and UnityEngine.Grid.
    /// </summary>
    [ExecuteAlways]
    [NeoDoc("GridSystem/FieldDebugDrawer.md")]
    [RequireComponent(typeof(FieldGenerator))]
    [CreateFromMenu("Neoxider/GridSystem/FieldDebugDrawer")]
    [AddComponentMenu("Neoxider/" + "GridSystem/" + nameof(FieldDebugDrawer))]
    public class FieldDebugDrawer : MonoBehaviour
    {
        [Header("Colors")] public Color GridColor = new(1f, 1f, 0f, 0.3f);
        public Color PathColor = Color.cyan;
        public Color BlockedCellColor = new(1f, 0f, 0f, 0.3f);
        public Color WalkableCellColor = new(0f, 1f, 0f, 0.3f);
        public Color DisabledCellColor = new(0.4f, 0.4f, 0.4f, 0.25f);
        public Color OccupiedCellColor = new(1f, 0.6f, 0f, 0.35f);
        public Color CoordinatesColor = Color.white;

        [Header("Settings")] public bool DrawCoordinates = true;
        public bool DrawPath;

        [Header("Debug")] public List<Vector3Int> DebugPath = new();

        [Header("Text")] public Color TextColor = Color.white;
        public int TextFontSize = 14;

        private FieldGenerator generator;
        private Grid unityGrid;

#if UNITY_EDITOR
        [System.NonSerialized] private GUIStyle _labelStyle;
        [System.NonSerialized] private string[] _labels;
        [System.NonSerialized] private FieldCell[,,] _labelCells;
#endif

        private void OnDrawGizmos()
        {
            if (generator == null)
            {
                generator = GetComponent<FieldGenerator>();
                if (generator == null)
                {
                    return;
                }
            }

            if (unityGrid == null)
            {
                unityGrid = generator.UnityGrid;
            }

            if (!generator.DebugEnabled || generator.Cells == null || unityGrid == null)
            {
                return;
            }

#if UNITY_EDITOR
            GUIStyle labelStyle = null;
            string[] labels = null;
            if (DrawCoordinates)
            {
                labelStyle = GetLabelStyle();
                labels = GetLabels(generator.Cells);
            }
#endif

            Vector3Int size = generator.GeneratedSize;
            Gizmos.color = GridColor;
            for (int x = 0; x <= size.x; x++)
            {
                Vector3 from = generator.GetCellCornerWorld(new Vector3Int(x, 0, 0));
                Vector3 to = generator.GetCellCornerWorld(new Vector3Int(x, size.y, 0));
                Gizmos.DrawLine(from, to);
            }

            for (int y = 0; y <= size.y; y++)
            {
                Vector3 from = generator.GetCellCornerWorld(new Vector3Int(0, y, 0));
                Vector3 to = generator.GetCellCornerWorld(new Vector3Int(size.x, y, 0));
                Gizmos.DrawLine(from, to);
            }

            int labelIndex = -1;
            for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
            for (int z = 0; z < size.z; z++)
            {
                labelIndex++;
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
                if (labels != null)
                {
                    Handles.Label(pos, labels[labelIndex], labelStyle);
                }
#endif
            }

            if (DrawPath && DebugPath != null && DebugPath.Count > 1)
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
        // WHY: one GUIStyle and one coordinate string per cell on every repaint allocated thousands of objects per
        // frame on large boards; the style is refreshed from the fields and labels only change on regeneration.
        private GUIStyle GetLabelStyle()
        {
            _labelStyle ??= new GUIStyle { alignment = TextAnchor.MiddleCenter };
            _labelStyle.normal.textColor = TextColor;
            _labelStyle.fontSize = TextFontSize;
            return _labelStyle;
        }

        private string[] GetLabels(FieldCell[,,] cells)
        {
            if (_labels != null && ReferenceEquals(_labelCells, cells))
            {
                return _labels;
            }

            string[] labels = new string[cells.Length];
            int index = 0;
            for (int x = 0; x < cells.GetLength(0); x++)
            for (int y = 0; y < cells.GetLength(1); y++)
            for (int z = 0; z < cells.GetLength(2); z++)
            {
                FieldCell cell = cells[x, y, z];
                Vector3Int position = cell != null ? cell.Position : new Vector3Int(x, y, z);
                labels[index++] = $"x:{position.x} y:{position.y} z:{position.z}";
            }

            _labels = labels;
            _labelCells = cells;
            return labels;
        }

        private void OnValidate()
        {
            SceneView.RepaintAll();
        }
#endif
    }
}
