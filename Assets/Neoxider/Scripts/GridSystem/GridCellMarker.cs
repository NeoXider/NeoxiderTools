using UnityEngine;

namespace Neo.GridSystem
{
    /// <summary>
    ///     Scene-side marker that binds a GameObject or collider to a FieldGenerator cell.
    ///     Useful for board views, click targets, drag/drop targets, and custom cell prefabs.
    /// </summary>
    [NeoDoc("GridSystem/GridCellMarker.md")]
    [CreateFromMenu("Neoxider/GridSystem/GridCellMarker")]
    [AddComponentMenu("Neoxider/GridSystem/GridCellMarker")]
    public sealed class GridCellMarker : MonoBehaviour
    {
        [SerializeField] private FieldGenerator _generator;
        [SerializeField] private Vector3Int _position;

        public FieldGenerator Generator
        {
            get => _generator;
            set => _generator = value;
        }

        public Vector3Int Position
        {
            get => _position;
            set => _position = value;
        }

        public FieldCell Cell => _generator != null ? _generator.GetCell(_position) : null;

        public void Bind(FieldGenerator generator, Vector3Int position)
        {
            _generator = generator;
            _position = position;
        }
    }
}
