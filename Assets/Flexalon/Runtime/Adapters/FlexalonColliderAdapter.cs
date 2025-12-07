#if UNITY_PHYSICS

using UnityEngine;

namespace Flexalon
{
    // An adapter that resizes a collider to match the size of the FlexalonNode.
    [ExecuteAlways]
    public class FlexalonColliderAdapter : FlexalonComponent, Adapter
    {
        private Collider _collider;
        private Collider2D _collider2D;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _collider2D = GetComponent<Collider2D>();
        }

        public Bounds Measure(FlexalonNode node, Vector3 size, Vector3 min, Vector3 max)
        {
            return Math.MeasureComponentBounds(new Bounds(Vector3.zero, Vector3.one), node, size, min, max);
        }

        public bool TryGetScale(FlexalonNode node, out Vector3 scale)
        {
            var size = node.Result.AdapterBounds.size;

            if (_collider is BoxCollider boxCollider)
            {
                boxCollider.size = size;
            }
            else if (_collider is SphereCollider sphereCollider)
            {
                sphereCollider.radius = Mathf.Min(size.x, size.y, size.z) * 0.5f;
            }
            else if (_collider is CapsuleCollider capsuleCollider)
            {
                switch (capsuleCollider.direction)
                {
                    case 0:
                        capsuleCollider.radius = Mathf.Min(size.y, size.z) * 0.5f;
                        capsuleCollider.height = size.x;
                        break;
                    case 1:
                        capsuleCollider.radius = Mathf.Min(size.x, size.z) * 0.5f;
                        capsuleCollider.height = size.y;
                        break;
                    case 2:
                        capsuleCollider.radius = Mathf.Min(size.x, size.y) * 0.5f;
                        capsuleCollider.height = size.z;
                        break;
                }
            }

            if (_collider2D is BoxCollider2D boxCollider2D)
            {
                boxCollider2D.size = size;
            }
            else if (_collider2D is CircleCollider2D circleCollider2D)
            {
                circleCollider2D.radius = Mathf.Min(size.x, size.y) * 0.5f;
            }
            else if (_collider2D is CapsuleCollider2D capsuleCollider2D)
            {
                switch (capsuleCollider2D.direction)
                {
                    case CapsuleDirection2D.Vertical:
                        capsuleCollider2D.size = new Vector2(size.x, size.y);
                        capsuleCollider2D.offset = new Vector2(0, size.z * 0.5f);
                        break;
                    case CapsuleDirection2D.Horizontal:
                        capsuleCollider2D.size = new Vector2(size.y, size.x);
                        capsuleCollider2D.offset = new Vector2(0, size.z * 0.5f);
                        break;
                }
            }

            scale = Vector3.one;
            return true;
        }

        public bool TryGetRectSize(FlexalonNode node, out Vector2 rectSize)
        {
            rectSize = Vector2.zero;
            return false;
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

#endif // UNITY_PHYSICS