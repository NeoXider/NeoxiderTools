using UnityEngine;

namespace Flexalon
{
    /// <summary> Represents an axis and direction. </summary>
    public enum Direction
    {
        PositiveX = 0,
        NegativeX = 1,
        PositiveY = 2,
        NegativeY = 3,
        PositiveZ = 4,
        NegativeZ = 5
    };

    /// <summary> Represents an axis. </summary>
    public enum Axis
    {
        X = 0,
        Y = 1,
        Z = 2
    };

    /// <summary> Represents a direction to align. </summary>
    public enum Align
    {
        Start = 0,
        Center = 1,
        End = 2
    };

    /// <summary> Represents a plane along two axes. </summary>
    public enum Plane
    {
        XY = 0,
        XZ = 1,
        ZY = 2
    }

    /// <summary> Determines how a FlexalonObject should be sized. </summary>
    public enum SizeType
    {
        /// <summary> Specify a fixed size value. </summary>
        Fixed = 0,

        /// <summary> Specify a factor of the space allocated by the parent layout.
        /// For example, 0.5 will fill half of the space. </summary>
        Fill = 1,

        /// <summary> The size is determined by the Adapter and attached Unity
        /// components such as MeshRenderer, SpriteRenderer, TMP_Text, RectTransform, and Colliders.
        /// An empty GameObject gets a size of 1. </summary>
        Component = 2,

        /// <summary> The size determined by the layout's algorithm. </summary>
        Layout = 3
    };

    /// <summary> Determines how a FlexalonObject min or max should be determined. </summary>
    public enum MinMaxSizeType
    {
        /// <summary> For min, the object cannot shrink. For max, this is infinity. </summary>
        None = 0,

        /// <summary> Specify a fixed min or max size value. </summary>
        Fixed = 1,

        /// <summary> Specify a factor of the space allocated by the parent layout.
        /// For example, 0.5 will fill half of the space. </summary>
        Fill = 2
    };

    /// <summary> Six floats representing right, left, top, bottom, back, front.</summary>
    [System.Serializable]
    public struct Directions
    {
        private static Directions _zero = new Directions(new float[] { 0, 0, 0, 0, 0, 0 });
        public static Directions zero => _zero;

        private float[] _values;

        public float Right
        {
            get => _values[0];
            set => _values[0] = value;
        }

        public float Left
        {
            get => _values[1];
            set => _values[1] = value;
        }

        public float Top
        {
            get => _values[2];
            set => _values[2] = value;
        }

        public float Bottom
        {
            get => _values[3];
            set => _values[3] = value;
        }

        public float Back
        {
            get => _values[4];
            set => _values[4] = value;
        }

        public float Front
        {
            get => _values[5];
            set => _values[5] = value;
        }

        public Directions(params float[] values)
        {
            _values = values;
        }

        public float this[int key]
        {
            get => _values[key];
        }

        public float this[Direction key]
        {
            get => _values[(int)key];
        }

        public Vector3 Size => new Vector3(
            _values[0] + _values[1], _values[2] + _values[3], _values[4] + _values[5]);

        public Vector3 Center => new Vector3(
            (_values[0] - _values[1]) * 0.5f, (_values[2] - _values[3]) * 0.5f, (_values[4] - _values[5]) * 0.5f);

        public override bool Equals(object obj)
        {
            if (obj is Directions other)
            {
                return _values[0] == other._values[0] && _values[1] == other._values[1] && _values[2] == other._values[2] &&
                    _values[3] == other._values[3] && _values[4] == other._values[4] && _values[5] == other._values[5];
            }

            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(Directions a, Directions b)
        {
            return Mathf.Approximately(a._values[0], b._values[0]) &&
                Mathf.Approximately(a._values[1], b._values[1]) &&
                Mathf.Approximately(a._values[2], b._values[2]) &&
                Mathf.Approximately(a._values[3], b._values[3]) &&
                Mathf.Approximately(a._values[4], b._values[4]) &&
                Mathf.Approximately(a._values[5], b._values[5]);
        }

        public static bool operator !=(Directions a, Directions b)
        {
            return !(a == b);
        }

        public override string ToString()
        {
            return $"({_values[0]}, {_values[1]}, {_values[2]}, {_values[3]}, {_values[4]}, {_values[5]})";
        }
    }
}