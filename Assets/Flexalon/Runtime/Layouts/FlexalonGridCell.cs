using UnityEngine;

namespace Flexalon
{
    /// <summary> Specifies which cell a gameObject should occupy in a grid layout. </summary>
    [AddComponentMenu("Flexalon/Flexalon Grid Cell"), HelpURL("https://www.flexalon.com/docs/gridLayout")]
    public class FlexalonGridCell : FlexalonComponent
    {
        [SerializeField, Min(0)]
        private int _column;
        /// <summary> The column of the cell. </summary>
        public int Column
        {
            get => _column;
            set
            {
                _column = Mathf.Max(0, value);
                MarkDirty();
            }
        }

        [SerializeField, Min(0)]
        private int _row;
        /// <summary> The row of the cell. </summary>
        public int Row
        {
            get => _row;
            set
            {
                _row = Mathf.Max(0, value);
                MarkDirty();
            }
        }

        [SerializeField, Min(0)]
        private int _layer;
        /// <summary> The layer of the cell. </summary>
        public int Layer
        {
            get => _layer;
            set
            {
                _layer = Mathf.Max(0, value);
                MarkDirty();
            }
        }

        /// <summary> The cell to occupy. </summary>
        public Vector3Int Cell
        {
            get => new Vector3Int(_column, _row, _layer);
            set
            {
                _column = Mathf.Max(0, value.x);
                _row = Mathf.Max(0, value.y);
                _layer = Mathf.Max(0, value.z);
                MarkDirty();
            }
        }
    }
}