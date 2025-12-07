using System.Collections.Generic;
using UnityEngine;

namespace Flexalon
{
    /// <summary> Use a random layout to position, rotate, and size children randomly within bounds. </summary>
    [AddComponentMenu("Flexalon/Flexalon Random Layout"), HelpURL("https://www.flexalon.com/docs/randomLayout")]
    public class FlexalonRandomLayout : LayoutBase
    {
        [SerializeField]
        private int _randomSeed = 1;
        /// <summary> Seed value used to determine random values. This ensures
        /// results remain consistent each time layout is computed. </summary>
        public int RandomSeed
        {
            get => _randomSeed;
            set { _randomSeed = value; MarkDirty(); }
        }

        [SerializeField]
        private bool _randomizePositionX = true;
        /// <summary> Randomizes the X position within bounds. </summary>
        public bool RandomizePositionX
        {
            get => _randomizePositionX;
            set { _randomizePositionX = value; MarkDirty(); }
        }

        [SerializeField]
        private float _positionMinX = -0.5f;
        /// <summary> Minimum X position. </summary>
        public float PositionMinX
        {
            get => _positionMinX;
            set { _positionMinX = value; MarkDirty(); }
        }

        [SerializeField]
        private float _positionMaxX = 0.5f;
        /// <summary> Maximum X position. </summary>
        public float PositionMaxX
        {
            get => _positionMaxX;
            set { _positionMaxX = value; MarkDirty(); }
        }

        [SerializeField]
        private bool _randomizePositionY = true;
        /// <summary> Randomizes the Y position within bounds. </summary>
        public bool RandomizePositionY
        {
            get => _randomizePositionY;
            set { _randomizePositionY = value; MarkDirty(); }
        }

        [SerializeField]
        private float _positionMinY = -0.5f;
        /// <summary> Minimum Y position. </summary>
        public float PositionMinY
        {
            get => _positionMinY;
            set { _positionMinY = value; MarkDirty(); }
        }

        [SerializeField]
        private float _positionMaxY = 0.5f;
        /// <summary> Maximum Y position. </summary>
        public float PositionMaxY
        {
            get => _positionMaxY;
            set { _positionMaxY = value; MarkDirty(); }
        }

        [SerializeField]
        private bool _randomizePositionZ = true;
        /// <summary> Randomizes the Z position within bounds. </summary>
        public bool RandomizePositionZ
        {
            get => _randomizePositionZ;
            set { _randomizePositionZ = value; MarkDirty(); }
        }

        [SerializeField]
        private float _positionMinZ = -0.5f;
        /// <summary> Minimum Z position. </summary>
        public float PositionMinZ
        {
            get => _positionMinZ;
            set { _positionMinZ = value; MarkDirty(); }
        }

        [SerializeField]
        private float _positionMaxZ = 0.5f;
        /// <summary> Maximum Z position. </summary>
        public float PositionMaxZ
        {
            get => _positionMaxZ;
            set { _positionMaxZ = value; MarkDirty(); }
        }

        private Vector3 _positionMin => new Vector3(_positionMinX, _positionMinY, _positionMinZ);
        /// <summary> Minimum position. </summary>
        public Vector3 PositionMin
        {
            get => _positionMin;
            set
            {
                _positionMinX = value.x;
                _positionMinY = value.y;
                _positionMinZ = value.z;
                MarkDirty();
            }
        }

        private Vector3 _positionMax => new Vector3(_positionMaxX, _positionMaxY, _positionMaxZ);
        /// <summary> Maximum position. </summary>
        public Vector3 PositionMax
        {
            get => _positionMax;
            set
            {
                _positionMaxX = value.x;
                _positionMaxY = value.y;
                _positionMaxZ = value.z;
                MarkDirty();
            }
        }

        [SerializeField]
        private bool _randomizeRotationX = true;
        /// <summary> Randomizes the X rotation within bounds. </summary>
        public bool RandomizeRotationX
        {
            get => _randomizeRotationX;
            set { _randomizeRotationX = value; MarkDirty(); }
        }

        [SerializeField, Range(-360, 360)]
        private float _rotationMinX = -180;
        /// <summary> Minimum X rotation. </summary>
        public float RotationMinX
        {
            get => _rotationMinX;
            set { _rotationMinX = value; MarkDirty(); }
        }

        [SerializeField, Range(-360, 360)]
        private float _rotationMaxX = 180;
        /// <summary> Maximum X rotation. </summary>
        public float RotationMaxX
        {
            get => _rotationMaxX;
            set { _rotationMaxX = value; MarkDirty(); }
        }

        [SerializeField]
        private bool _randomizeRotationY = true;
        /// <summary> Randomizes the Y rotation within bounds. </summary>
        public bool RandomizeRotationY
        {
            get => _randomizeRotationY;
            set { _randomizeRotationY = value; MarkDirty(); }
        }

        [SerializeField, Range(-360, 360)]
        private float _rotationMinY = -180;
        /// <summary> Minimum Y rotation. </summary>
        public float RotationMinY
        {
            get => _rotationMinY;
            set { _rotationMinY = value; MarkDirty(); }
        }

        [SerializeField, Range(-360, 360)]
        private float _rotationMaxY = 180;
        /// <summary> Maximum Y rotation. </summary>
        public float RotationMaxY
        {
            get => _rotationMaxY;
            set { _rotationMaxY = value; MarkDirty(); }
        }

        [SerializeField]
        private bool _randomizeRotationZ = true;
        /// <summary> Randomizes the Z rotation within bounds. </summary>
        public bool RandomizeRotationZ
        {
            get => _randomizeRotationZ;
            set { _randomizeRotationZ = value; MarkDirty(); }
        }

        [SerializeField, Range(-360, 360)]
        private float _rotationMinZ = -180;
        /// <summary> Minimum Z rotation. </summary>
        public float RotationMinZ
        {
            get => _rotationMinZ;
            set { _rotationMinZ = value; MarkDirty(); }
        }

        [SerializeField, Range(-360, 360)]
        private float _rotationMaxZ = 180;
        /// <summary> Maximum Z rotation. </summary>
        public float RotationMaxZ
        {
            get => _rotationMaxZ;
            set { _rotationMaxZ = value; MarkDirty(); }
        }

        private Quaternion _rotationMin => Quaternion.Euler(_rotationMinX, _rotationMinY, _rotationMinZ);
        /// <summary> Minimum rotation. </summary>
        public Quaternion RotationMin
        {
            get => _rotationMin;
            set
            {
                var euler = value.eulerAngles;
                _rotationMinX = euler.x;
                _rotationMinY = euler.y;
                _rotationMinZ = euler.z;
                MarkDirty();
            }
        }

        private Quaternion _rotationMax => Quaternion.Euler(_rotationMaxX, _rotationMaxY, _rotationMaxZ);
        /// <summary> Maximum rotation. </summary>
        public Quaternion RotationMax
        {
            get => _rotationMax;
            set
            {
                var euler = value.eulerAngles;
                _rotationMaxX = euler.x;
                _rotationMaxY = euler.y;
                _rotationMaxZ = euler.z;
                MarkDirty();
            }
        }

        [SerializeField]
        private bool _randomizeSizeX = true;
        /// <summary> Randomizes the X size within bounds. You must set child size to "Fill" for this to have effect.</summary>
        public bool RandomizeSizeX
        {
            get => _randomizeSizeX;
            set { _randomizeSizeX = value; MarkDirty(); }
        }

        [SerializeField]
        private float _sizeMinX = 0;
        /// <summary> Minimum X size. </summary>
        public float SizeMinX
        {
            get => _sizeMinX;
            set { _sizeMinX = value; MarkDirty(); }
        }

        [SerializeField]
        private float _sizeMaxX = 1;
        /// <summary> Maximum X size. </summary>
        public float SizeMaxX
        {
            get => _sizeMaxX;
            set { _sizeMaxX = value; MarkDirty(); }
        }

        [SerializeField]
        private bool _randomizeSizeY = true;
        /// <summary> Randomizes the Y size within bounds. You must set child size to "Fill" for this to have effect.</summary>
        public bool RandomizeSizeY
        {
            get => _randomizeSizeY;
            set { _randomizeSizeY = value; MarkDirty(); }
        }

        [SerializeField]
        private float _sizeMinY = 0;
        /// <summary> Minimum Y size. </summary>
        public float SizeMinY
        {
            get => _sizeMinY;
            set { _sizeMinY = value; MarkDirty(); }
        }

        [SerializeField]
        private float _sizeMaxY = 1;
        /// <summary> Maximum Y size. </summary>
        public float SizeMaxY
        {
            get => _sizeMaxY;
            set { _sizeMaxY = value; MarkDirty(); }
        }

        [SerializeField]
        private bool _randomizeSizeZ = true;
        /// <summary> Randomizes the Z size within bounds. You must set child size to "Fill" for this to have effect.</summary>
        public bool RandomizeSizeZ
        {
            get => _randomizeSizeZ;
            set { _randomizeSizeZ = value; MarkDirty(); }
        }

        [SerializeField]
        private float _sizeMinZ = 0;
        /// <summary> Minimum Z size. </summary>
        public float SizeMinZ
        {
            get => _sizeMinZ;
            set { _sizeMinZ = value; MarkDirty(); }
        }

        [SerializeField]
        private float _sizeMaxZ = 1;
        /// <summary> Maximum Z size. </summary>
        public float SizeMaxZ
        {
            get => _sizeMaxZ;
            set { _sizeMaxZ = value; MarkDirty(); }
        }

        private Vector3 _sizeMin => new Vector3(_sizeMinX, _sizeMinY, _sizeMinZ);
        /// <summary> Minimum size. </summary>
        public Vector3 SizeMin
        {
            get => _sizeMin;
            set
            {
                _sizeMinX = value.x;
                _sizeMinY = value.y;
                _sizeMinZ = value.z;
                MarkDirty();
            }
        }

        private Vector3 _sizeMax => new Vector3(_sizeMaxX, _sizeMaxY, _sizeMaxZ);
        /// <summary> Maximum size. </summary>
        public Vector3 SizeMax
        {
            get => _sizeMax;
            set
            {
                _sizeMaxX = value.x;
                _sizeMaxY = value.y;
                _sizeMaxZ = value.z;
                MarkDirty();
            }
        }

        [SerializeField]
        private Align _horizontalAlign = Align.Center;
        /// <summary> Horizontal alignment of all children within layout bounds. </summary>
        public Align HorizontalAlign
        {
            get { return _horizontalAlign; }
            set
            {
                _horizontalAlign = value;
                _randomizePositionX = false;
                Node.MarkDirty();
            }
        }

        [SerializeField]
        private Align _verticalAlign = Align.Center;
        /// <summary> Vertical alignment of all children within layout bounds. </summary>
        public Align VerticalAlign
        {
            get { return _verticalAlign; }
            set
            {
                _verticalAlign = value;
                _randomizePositionY = false;
                Node.MarkDirty();
            }
        }

        [SerializeField]
        private Align _depthAlign = Align.Center;
        /// <summary> Depth alignment of all children within layout bounds. </summary>
        public Align DepthAlign
        {
            get { return _depthAlign; }
            set
            {
                _depthAlign = value;
                _randomizePositionZ = false;
                Node.MarkDirty();
            }
        }

        private List<Vector3> _positions = new List<Vector3>();
        private List<Quaternion> _rotations = new List<Quaternion>();
        private List<Vector3> _fillSizes = new List<Vector3>();

        /// <inheritdoc />
        public override Bounds Measure(FlexalonNode node, Vector3 size, Vector3 min, Vector3 max)
        {
            var randomGen = new System.Random(_randomSeed);
            var randomGenPositionX = new System.Random(randomGen.Next());
            var randomGenPositionY = new System.Random(randomGen.Next());
            var randomGenPositionZ = new System.Random(randomGen.Next());
            var randomGenRotationX = new System.Random(randomGen.Next());
            var randomGenRotationY = new System.Random(randomGen.Next());
            var randomGenRotationZ = new System.Random(randomGen.Next());
            var randomGenSizeX = new System.Random(randomGen.Next());
            var randomGenSizeY = new System.Random(randomGen.Next());
            var randomGenSizeZ = new System.Random(randomGen.Next());
            _positions.Clear();
            _rotations.Clear();
            _fillSizes.Clear();

            foreach (var child in node.Children)
            {
                Vector3 position = Vector3.zero;
                Vector3 rotation = Vector3.zero;
                Vector3 fillSize = Vector3.one;

                if (_randomizePositionX)
                {
                    position.x = _positionMinX + (float)randomGenPositionX.NextDouble() * (_positionMaxX - _positionMinX);
                }

                if (_randomizePositionY)
                {
                    position.y = _positionMinY + (float)randomGenPositionY.NextDouble() * (_positionMaxY - _positionMinY);
                }

                if (_randomizePositionZ)
                {
                    position.z = _positionMinZ + (float)randomGenPositionZ.NextDouble() * (_positionMaxZ - _positionMinZ);
                }

                if (_randomizeRotationX)
                {
                    rotation.x = _rotationMinX + (float)randomGenRotationX.NextDouble() * (_rotationMaxX - _rotationMinX);
                }

                if (_randomizeRotationY)
                {
                    rotation.y = _rotationMinY + (float)randomGenRotationY.NextDouble() * (_rotationMaxY - _rotationMinY);
                }

                if (_randomizeRotationZ)
                {
                    rotation.z = _rotationMinZ + (float)randomGenRotationZ.NextDouble() * (_rotationMaxZ - _rotationMinZ);
                }

                if (_randomizeSizeX)
                {
                    fillSize.x = _sizeMinX + (float)randomGenSizeX.NextDouble() * (_sizeMaxX - _sizeMinX);
                }

                if (_randomizeSizeY)
                {
                    fillSize.y = _sizeMinY + (float)randomGenSizeY.NextDouble() * (_sizeMaxY - _sizeMinY);
                }

                if (_randomizeSizeZ)
                {
                    fillSize.z = _sizeMinZ + (float)randomGenSizeZ.NextDouble() * (_sizeMaxZ - _sizeMinZ);
                }

                _positions.Add(position);
                _rotations.Add(Quaternion.Euler(rotation));
                _fillSizes.Add(fillSize);
            }

            var center = Vector3.zero;
            if (node.Children.Count > 0)
            {
                var bounds = Math.CreateRotatedBounds(_positions[0], _fillSizes[0],  _rotations[0]);
                for (int i = 1; i < node.Children.Count; i++)
                {
                    bounds.Encapsulate(Math.CreateRotatedBounds(_positions[i], _fillSizes[i],  _rotations[i]));
                }

                for (int i = 0; i < 3; i++)
                {
                    if (node.GetSizeType(i) == SizeType.Layout)
                    {
                        size[i] = bounds.size[i];
                        center[i] = bounds.center[i];
                    }
                }
            }

            size = Math.Clamp(size, min, max);
            for (int i = 0; i < node.Children.Count; i++)
            {
                node.Children[i].SetShrinkFillSize(_fillSizes[i], size);
            }

            return new Bounds(center, size);
        }

        /// <inheritdoc />
        public override void Arrange(FlexalonNode node, Vector3 layoutSize)
        {
            for (int i = 0; i < node.Children.Count; i++)
            {
                var position = _positions[i];
                var child = node.Children[i];
                var childSize = child.GetArrangeSize();
                var bounds = Math.CreateRotatedBounds(position, childSize, _rotations[i]);
                if (!_randomizePositionX)
                {
                    position.x = Math.Align(bounds.size, layoutSize, 0, _horizontalAlign);
                }

                if (!_randomizePositionY)
                {
                    position.y = Math.Align(bounds.size, layoutSize, 1, _verticalAlign);
                }

                if (!_randomizePositionZ)
                {
                    position.z = Math.Align(bounds.size, layoutSize, 2, _depthAlign);
                }

                child.SetPositionResult(position);
                child.SetRotationResult(_rotations[i]);
            }
        }

        protected override void Initialize()
        {
            base.Initialize();
            if (transform is RectTransform || (transform.parent && transform.parent is RectTransform))
            {
                _positionMinX *= 100;
                _positionMaxX *= 100;
                _positionMinY *= 100;
                _positionMaxY *= 100;
                _positionMinZ *= 100;
                _positionMaxZ *= 100;
                _sizeMinX *= 100;
                _sizeMaxX *= 100;
                _sizeMinY *= 100;
                _sizeMaxY *= 100;
                _sizeMinZ *= 100;
                _sizeMaxZ *= 100;
            }
        }
    }
}