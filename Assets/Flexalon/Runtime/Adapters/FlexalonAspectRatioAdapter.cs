using UnityEngine;

namespace Flexalon
{
    // A simple adapter which maintains a specified aspect ratio
    [ExecuteAlways]
    public class FlexalonAspectRatioAdapter : FlexalonComponent, Adapter
    {
        [SerializeField]
        private float _width;
        public float Width
        {
            get => _width;
            set
            {
                _width = value;
                MarkDirty();
            }
        }

        [SerializeField]
        private float _height;
        public float Height
        {
            get => _height;
            set
            {
                _height = value;
                MarkDirty();
            }
        }

        public Bounds Measure(FlexalonNode node, Vector3 size, Vector3 min, Vector3 max)
        {
            return Math.MeasureComponentBounds2D(new Bounds(Vector3.zero, new Vector3(_width, _height, 1)), node, size, min, max);
        }

        public bool TryGetScale(FlexalonNode node, out Vector3 scale)
        {
            scale = Vector3.one;
            return true;
        }

        public bool TryGetRectSize(FlexalonNode node, out Vector2 rectSize)
        {
            rectSize = node.Result.AdapterBounds.size;
            return true;
        }

        protected override void UpdateProperties()
        {
            Node.SetAdapter(this);
        }

        protected override void ResetProperties()
        {
            Node.SetAdapter(null);
        }
    }
}