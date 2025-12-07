using System.Collections.Generic;
using UnityEngine;

namespace Flexalon
{
    /// <summary> A drag target allows a layout to accept  dragged FlexalonInteractable objects. </summary>
    [AddComponentMenu("Flexalon/Flexalon Drag Target"), HelpURL("https://www.flexalon.com/docs/dragging"), DisallowMultipleComponent]
    public class FlexalonDragTarget : MonoBehaviour
    {
        [SerializeField]
        private bool _canRemoveObjects = true;
        /// <summary> Whether objects can be removed from the layout by dragging them from this target. </summary>
        public bool CanRemoveObjects {
            get => _canRemoveObjects;
            set => _canRemoveObjects = value;
        }

        [SerializeField]
        private bool _canAddObjects = true;
        /// <summary> Whether objects can be added to the layout by dragging them to this target. </summary>
        public bool CanAddObjects {
            get => _canAddObjects;
            set => _canAddObjects = value;
        }

        [SerializeField]
        private int _minObjects;
        /// <summary> The minimum number of objects that must remain in this layout. </summary>
        public int MinObjects {
            get => _minObjects;
            set => _minObjects = value;
        }

        [SerializeField]
        private int _maxObjects;
        /// <summary> The maximum number of objects that can be added to the layout. </summary>
        public int MaxObjects {
            get => _maxObjects;
            set => _maxObjects = value;
        }

        [SerializeField]
        private Vector3 _margin;
        /// <summary> Extra margin around the layout size to use for hit testing. </summary>
        public Vector3 Margin {
            get => _margin;
            set => _margin = value;
        }

        private FlexalonNode _node;

        private static HashSet<FlexalonDragTarget> _dragTargets = new HashSet<FlexalonDragTarget>();
        public static IReadOnlyCollection<FlexalonDragTarget> DragTargets => _dragTargets;

        void OnEnable()
        {
            _node = Flexalon.GetOrCreateNode(gameObject);
            _dragTargets.Add(this);
        }

        void OnDisable()
        {
            _node = null;
            _dragTargets.Remove(this);
        }

        internal bool OverlapsSphere(Vector3 position, float radius)
        {
            var center = _node.Result.AdapterBounds.center;
            var extents = (_node.Result.AdapterBounds.size + _margin * 2) / 2;
            var min = center - extents;
            var max = center + extents;

            // Transform the sphere center into the OBB's local coordinate system
            Vector3 localSphereCenter = transform.InverseTransformPoint(position);

            // Calculate the closest point on the OBB to the sphere center
            Vector3 closestPointOnOBB = Vector3.Min(Vector3.Max(localSphereCenter, min), max);

            // Calculate the distance between the closest point and the sphere center
            float distanceSquared = (closestPointOnOBB - localSphereCenter).sqrMagnitude;

            // Check if the distance is less than or equal to the sphere's radius squared
            return distanceSquared <= radius * radius;
        }
    }
}